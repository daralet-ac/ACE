using System;
using System.Collections.Generic;
using System.Linq;
using ACE.Common;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Entity;
using ACE.Server.Entity.Actions;
using ACE.Server.Managers;
using ACE.Server.WorldObjects;
using Serilog;

namespace ACE.Server.Arena;

/// <summary>
/// What the arena has to say about one player doing something to another
/// </summary>
public enum ArenaVerdict
{
    /// <summary>Neither of them is in a duel: the usual rules apply</summary>
    NotArena,

    /// <summary>A duel allows it, whatever the usual rules would say</summary>
    Allowed,

    /// <summary>A duel doesn't allow it</summary>
    Refused
}

/// <summary>
/// The arena: duels between players, each in an instance of its own of an arena map (ArenaMaps).<para />
/// A duel comes from a challenge (/arena challenge) or from the queue (/arena queue). Everyone has to say yes, and then the fighters are taken
/// to the arena, where they can't be harmed until a countdown ends. Then they fight as player killer lites until one side is defeated,
/// leaves, or time runs out. Being defeated is not dying: nothing is lost, and the loser is taken home with three quarters of their vitals.
/// Everyone goes back to exactly where they were before the duel, with the player killer status they had.<para />
/// Everything in here is changed while holding one lock. Whatever is done to a player is queued on that player
/// (an ActionChain, or WorldManager.ThreadSafeTeleport), so it happens on the player's own landblock thread.
/// </summary>
public static partial class ArenaManager
{
    private static readonly ILogger _log = Log.ForContext(typeof(ArenaManager));

    private static readonly object sync = new object();

    private static readonly ArenaQueue queue = new ArenaQueue();

    private static readonly List<ArenaMatch> matches = new List<ArenaMatch>();

    /// <summary>
    /// Who can't challenge whom again yet, because their last challenge was turned down
    /// </summary>
    private static readonly Dictionary<(uint Challenger, uint Target), DateTime> challengeCooldowns =
        new Dictionary<(uint Challenger, uint Target), DateTime>();

    private static int lastMatchId;

    private static DateTime nextTick;

    /// <summary>
    /// The current time, replaceable so tests can move it
    /// </summary>
    internal static Func<DateTime> UtcNow = () => DateTime.UtcNow;

    private static readonly TimeSpan TickInterval = TimeSpan.FromMilliseconds(250);

    /// <summary>How long the fighters have to get into the arena, portal space included</summary>
    private static readonly TimeSpan ArriveTime = TimeSpan.FromSeconds(60);

    /// <summary>How long after a challenge is turned down the same player can't challenge the same player again</summary>
    private static readonly TimeSpan ChallengeCooldown = TimeSpan.FromSeconds(60);

    /// <summary>How long after the end the fighters who are still standing are taken home</summary>
    private static readonly double HomeDelaySeconds = 5;

    /// <summary>How long after the end the instance is closed, which sends out anyone who is somehow still in it</summary>
    private static readonly TimeSpan CloseDelay = TimeSpan.FromSeconds(20);

    /// <summary>How long a fighter can be outside the radius of an outdoor arena before they are disqualified</summary>
    private static readonly TimeSpan BoundaryGrace = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Whether arena dueling is on at all (arena_dueling_enabled). Turning it off calls off everything that is going on (ShutDown).
    /// </summary>
    private static bool Enabled => PropertyManager.GetBool("arena_dueling_enabled").Item;

    /// <summary>
    /// The lowest level a player can be to duel (arena_dueling_minimum_level). 1 when there is no minimum.
    /// </summary>
    public static int MinimumLevel =>
        (int)Math.Clamp(PropertyManager.GetLong("arena_dueling_minimum_level").Item, 1, int.MaxValue);

    private static TimeSpan AcceptTime =>
        TimeSpan.FromSeconds(Math.Max(5, PropertyManager.GetLong("arena_accept_seconds").Item));

    private static TimeSpan CountdownTime =>
        TimeSpan.FromSeconds(Math.Max(3, PropertyManager.GetLong("arena_countdown_seconds").Item));

    private static TimeSpan TimeLimit =>
        TimeSpan.FromMinutes(Math.Max(1, PropertyManager.GetLong("arena_time_limit_minutes").Item));

    private static int EloK => (int)Math.Max(0, PropertyManager.GetLong("arena_elo_k").Item);

    private static bool BlockSameAddress => PropertyManager.GetBool("arena_block_same_ip").Item;

    private static bool RatedChallenges => PropertyManager.GetBool("arena_rated_challenges").Item;

    private static int DefaultLevelBand => (int)Math.Max(0, PropertyManager.GetLong("arena_queue_level_band").Item);

    #region Ratings

    /// <summary>
    /// Whether two players are fighting each other in a duel, and if they are, whether it is scaled. Null if they are not.
    /// LevelScaling asks this for every hit, so it takes no lock: the fighters of a duel never change once it is made.
    /// </summary>
    public static bool? IsScaledDuel(Player a, Player b)
    {
        if (a.InstanceId == Landblock.PersistentInstance || a.InstanceId != b.InstanceId)
        {
            return null;
        }

        if (InstanceManager.Get(a.InstanceId)?.Owner is not ArenaMatch match)
        {
            return null;
        }

        if (match.Get(a.Guid.Full) == null || match.Get(b.Guid.Full) == null)
        {
            return null;
        }

        return match.Scaled;
    }

    #endregion

    #region Commands

    /// <summary>
    /// Why this player can't go to the arena now, or null if they can
    /// </summary>
    /// <param name="self">True to say it to the player themself, false to say it about them to someone else</param>
    public static string WhyCantDuel(Player player, bool self = true)
    {
        string Say(string you, string them) => self ? you : $"{player.Name} {them}";

        if (!Enabled)
        {
            return "Arena dueling is turned off.";
        }

        if (ArenaMaps.Enabled.Count == 0)
        {
            return "There is no arena to duel in.";
        }

        if (player.IsOlthoiPlayer)
        {
            return Say("Olthoi can't duel in the arena.", "is an Olthoi, and Olthoi can't duel in the arena.");
        }

        if (
            player.PlayerKillerStatus != PlayerKillerStatus.NPK
            && player.PlayerKillerStatus != PlayerKillerStatus.PKLite
        )
        {
            return Say(
                "Only non-player killers and player killer lites can duel in the arena.",
                "is not a non-player killer or a player killer lite, and only they can duel in the arena."
            );
        }

        var minimumLevel = MinimumLevel;

        if ((player.Level ?? 1) < minimumLevel)
        {
            return Say(
                $"You have to be at least level {minimumLevel} to duel in the arena.",
                $"has to be at least level {minimumLevel} to duel in the arena."
            );
        }

        if (player.RecallsDisabled)
        {
            return Say(
                "You can't duel until you have left the training academy.",
                "has not left the training academy yet."
            );
        }

        if (player.InstanceId != Landblock.PersistentInstance)
        {
            return Say("You can't go to the arena from inside an instance.", "is inside an instance.");
        }

        if (player.IsDead || player.IsInDeathProcess)
        {
            return Say("You can't duel while you are dead.", "is dead.");
        }

        if (player.PKTimerActive)
        {
            return Say(
                "You have been in a player killer battle too recently.",
                "has been in a player killer battle too recently."
            );
        }

        if (player.Teleporting || player.IsLoggingOut || player.suicideInProgress)
        {
            return Say("You can't go to the arena right now.", "can't go to the arena right now.");
        }

        return null;
    }

    /// <summary>
    /// Puts a player in the queue, or changes what they are waiting for if they are in it already (they keep their place)
    /// </summary>
    /// <param name="levelBand">How many levels apart their opponent may be at most, 0 for any. Null for the server's default.</param>
    /// <param name="scaled">To wait for a scaled duel, in which the higher-level fighter fights at their opponent's level</param>
    /// <param name="rated">False to wait for an unrated duel. Players are only paired with someone who wants the same kind of duel.</param>
    public static void JoinQueue(Player player, int? levelBand, bool scaled = false, bool rated = true)
    {
        var why = WhyCantDuel(player) ?? (scaled ? WhyCantScale(player) : null);
        if (why != null)
        {
            player.SendMessage(why);
            return;
        }

        var guid = player.Guid.Full;

        lock (sync)
        {
            if (FindMatch(guid) != null)
            {
                player.SendMessage("You are already in a duel.");
                return;
            }

            var already = queue.Find(guid);

            if (already != null && already.Size > 1)
            {
                player.SendMessage(
                    $"You are waiting in the arena queue with {already.Describe()}. /arena leave takes it out of the queue."
                );
                return;
            }

            var band = Math.Max(0, levelBand ?? DefaultLevelBand);

            queue.Add(
                new ArenaQueueEntry
                {
                    Guid = guid,
                    Name = player.Name,
                    Level = player.Level ?? 1,
                    LevelBand = band,
                    Scaled = scaled,
                    Rated = rated,
                    Address = AddressOf(player),
                    JoinedAt = already?.JoinedAt ?? UtcNow()
                }
            );

            var bandText = band > 0 ? $", with an opponent within {band} level{(band == 1 ? "" : "s")} of you" : "";
            var waitingFor = $" for {DescribeDuel(scaled, rated)}{bandText}";
            var others = queue.Entries.Count(e =>
                e.Guid != guid && e.Size == 1 && e.Scaled == scaled && e.Rated == rated
            );

            player.SendMessage(
                already != null
                    ? $"You are still in the arena queue{waitingFor}, and kept your place."
                    : $"You are in the arena queue{waitingFor}. {others} other{(others == 1 ? " is" : "s are")} waiting for the same. /arena leave takes you out of it."
            );
        }
    }

    /// <summary>
    /// Asks another player to a duel
    /// </summary>
    /// <param name="scaled">A scaled duel: the higher-level fighter fights at their opponent's level</param>
    /// <param name="rated">False for an unrated duel. A challenge is only rated if arena_rated_challenges is on, whatever is asked for.</param>
    public static void Challenge(Player challenger, string targetName, bool scaled = false, bool rated = true)
    {
        var target = PlayerManager.GetOnlinePlayer(targetName);

        if (target == null)
        {
            challenger.SendMessage($"There is nobody called {targetName} online.");
            return;
        }

        if (target == challenger)
        {
            challenger.SendMessage("You can't challenge yourself.");
            return;
        }

        var why =
            WhyCantDuel(challenger)
            ?? WhyCantDuel(target, self: false)
            ?? (scaled ? WhyCantScale(challenger) ?? WhyCantScale(target, self: false) : null);
        if (why != null)
        {
            challenger.SendMessage(why);
            return;
        }

        if (target.SquelchManager.Squelches.Contains(challenger, ChatMessageType.Tell))
        {
            challenger.SendMessage($"{target.Name} is not accepting challenges from you.");
            return;
        }

        lock (sync)
        {
            var now = UtcNow();

            if (FindMatch(challenger.Guid.Full) != null)
            {
                challenger.SendMessage("You are already in a duel.");
                return;
            }

            if (FindMatch(target.Guid.Full) != null)
            {
                challenger.SendMessage($"{target.Name} is already in a duel.");
                return;
            }

            var waitingWithFellowship = WhoWaitsWithAFellowship(new[] { challenger, target });
            if (waitingWithFellowship != null)
            {
                challenger.SendMessage(waitingWithFellowship);
                return;
            }

            if (challengeCooldowns.TryGetValue((challenger.Guid.Full, target.Guid.Full), out var until) && now < until)
            {
                challenger.SendMessage(
                    $"{target.Name} turned down your last challenge. You can challenge them again in {(until - now).TotalSeconds:N0} seconds."
                );
                return;
            }

            var sameAddress = AddressOf(challenger) != null && AddressOf(challenger) == AddressOf(target);

            var match = NewMatch(
                ArenaMatchKind.Challenge,
                rated: rated && RatedChallenges && !(BlockSameAddress && sameAddress),
                scaled,
                now,
                NewFighter(challenger, side: 0, accepted: true),
                NewFighter(target, side: 1, accepted: false)
            );

            if (
                !Ask(
                    match,
                    match.Fighters[1],
                    target,
                    $"{Introduce(challenger, scaled)} challenges you to {DescribeDuel(match.Scaled, match.Rated)} in the arena.{ExplainScaling(match.Scaled)} Nobody loses anything by being defeated. Will you fight?"
                )
            )
            {
                CallOff(
                    match,
                    $"{target.Name} has another question open, and can't be asked right now.",
                    new[] { match.Fighters[1] },
                    culpritsKeepTheirPlace: true
                );
                return;
            }

            challenger.SendMessage(
                $"You challenge {target.Name} to {DescribeDuel(match.Scaled, match.Rated)}{(match.Rated || !rated ? "" : " (it can't be rated)")}. They have {AcceptTime.TotalSeconds:N0} seconds to answer."
            );
        }
    }

    /// <summary>
    /// Takes a player out of the queue, or out of the duel they are in: calls it off if it has not started, or gives it up if it has
    /// </summary>
    public static void Leave(Player player)
    {
        var guid = player.Guid.Full;

        lock (sync)
        {
            var waiting = queue.Take(guid);

            if (waiting != null)
            {
                if (waiting.Size == 1)
                {
                    player.SendMessage("You have left the arena queue.");
                }
                else
                {
                    player.SendMessage("You have taken your fellowship out of the arena queue.");
                    TellQueued(
                        waiting,
                        $"{player.Name} has taken your fellowship out of the arena queue.",
                        except: guid
                    );
                }

                return;
            }

            var watched = FindWatched(guid);

            if (watched != null)
            {
                player.SendMessage("You stop watching the duel.");
                SendSpectatorHome(watched, watched.GetSpectator(guid), player, 1);
                return;
            }

            var match = FindMatch(guid);
            if (match == null)
            {
                player.SendMessage("You are not in the arena queue, in a duel, or watching one.");
                return;
            }

            var fighter = match.Get(guid);

            if (match.State == ArenaMatchState.Fighting && fighter.Spectating)
            {
                // defeated, and watching the rest of it
                player.SendMessage("You stop watching the duel.");
                SendHome(match, fighter, player, 1);
            }
            else if (match.State == ArenaMatchState.Fighting)
            {
                player.SendMessage("You give up the duel.");
                Eliminate(match, fighter, player, $"{fighter.Name} has given up.");
                SendHome(match, fighter, player, HomeDelaySeconds);
            }
            else
            {
                CallOff(match, $"{fighter.Name} has called it off.", fighter);
            }
        }
    }

    /// <summary>
    /// A line about where a player stands with the arena: the queue, or the duel they are in
    /// </summary>
    public static string Status(Player player)
    {
        if (!Enabled)
        {
            return "Arena dueling is turned off right now.";
        }

        var guid = player.Guid.Full;

        lock (sync)
        {
            var place = queue.PositionOf(guid);
            if (place > 0)
            {
                var waiting = queue.Find(guid);

                return waiting.Size == 1
                    ? $"You are number {place} of {queue.Count} in the arena queue, for {DescribeDuel(waiting.Scaled, waiting.Rated)}."
                    : $"You are waiting with {waiting.Describe()}, number {place} of {queue.Count} in the arena queue, for {DescribeDuel(waiting.Scaled, waiting.Rated)}.";
            }

            var watched = FindWatched(guid);
            if (watched != null)
            {
                return $"You are watching duel #{watched.Id}. /arena leave takes you back.";
            }

            var match = FindMatch(guid);
            if (match == null)
            {
                return "You are not in the arena queue or in a duel.";
            }

            var fighter = match.Get(guid);

            if (fighter.Spectating && !fighter.Returned)
            {
                return "You have been defeated, and are watching the rest of the duel. /arena leave takes you back.";
            }

            var opponents = string.Join(", ", match.Opponents(fighter).Select(f => f.Name));

            return match.State switch
            {
                ArenaMatchState.Accepting => $"Your duel with {opponents} is waiting for everyone to say yes.",
                ArenaMatchState.Fighting
                    => $"You are fighting {opponents}. {Math.Max(0, (match.Deadline - UtcNow()).TotalMinutes):N0} minute(s) are left.",
                _ => $"Your duel with {opponents} is about to begin."
            };
        }
    }

    public static List<string> DescribeAll()
    {
        lock (sync)
        {
            var lines = matches.Select(m => m.Describe()).ToList();
            lines.Add(
                $"{queue.Count} in the queue{(queue.Count > 0 ? ": " + string.Join(", ", queue.Entries.Select(e => $"{e.Describe()} ({DescribeDuel(e.Scaled, e.Rated)})")) : "")}"
            );
            return lines;
        }
    }

    /// <summary>
    /// Calls off a duel, for an admin. Fighters already in the arena are taken home.
    /// </summary>
    public static bool CallOff(int matchId, string by)
    {
        lock (sync)
        {
            var match = matches.FirstOrDefault(m => m.Id == matchId && m.State != ArenaMatchState.Ended);
            if (match == null)
            {
                return false;
            }

            CallOff(match, $"it was called off by {by}.");
            return true;
        }
    }

    #endregion

    #region Hooks

    /// <summary>
    /// Whether one player may harm (or help) another, as far as the arena is concerned. In an arena, fighters can only harm their opponents,
    /// and only once the fight has begun, and nobody can help an opponent or anybody who is not in the duel.
    /// </summary>
    public static ArenaVerdict CheckPlayerVsPlayer(Player source, Player target, bool harmful)
    {
        if (source == target)
        {
            return ArenaVerdict.NotArena;
        }

        // nearly everyone is in the persistent world, and that is all there is to know about them
        if (source.InstanceId == Landblock.PersistentInstance && target.InstanceId == Landblock.PersistentInstance)
        {
            return ArenaVerdict.NotArena;
        }

        var match =
            InstanceManager.Get(source.InstanceId)?.Owner as ArenaMatch
            ?? InstanceManager.Get(target.InstanceId)?.Owner as ArenaMatch;

        if (match == null)
        {
            return ArenaVerdict.NotArena;
        }

        lock (sync)
        {
            var a = match.Get(source.Guid.Full);
            var b = match.Get(target.Guid.Full);

            if (a == null || b == null || a.Eliminated || b.Eliminated)
            {
                return ArenaVerdict.Refused;
            }

            if (a.Side == b.Side)
            {
                return harmful ? ArenaVerdict.Refused : ArenaVerdict.Allowed;
            }

            return harmful && match.State == ArenaMatchState.Fighting ? ArenaVerdict.Allowed : ArenaVerdict.Refused;
        }
    }

    /// <summary>
    /// True if this player is a fighter in the arena, where being killed is a defeat instead of a death: no vitae,
    /// no lost enchantments, no kill credit, and they are taken home instead of to their lifestone
    /// </summary>
    public static bool IsDefeatNotDeath(Player player)
    {
        if (player.InstanceId == Landblock.PersistentInstance)
        {
            return false;
        }

        if (!(InstanceManager.Get(player.InstanceId)?.Owner is ArenaMatch match))
        {
            return false;
        }

        lock (sync)
        {
            return match.Get(player.Guid.Full) != null || match.GetSpectator(player.Guid.Full) != null;
        }
    }

    /// <summary>
    /// A fighter has been defeated (Player.Die, when IsDefeatNotDeath). They are taken home once their death animation is over.
    /// </summary>
    public static void OnFighterDefeated(Player player, DamageHistoryInfo topDamager, double homeDelaySeconds)
    {
        var guid = player.Guid.Full;
        var match = InstanceManager.Get(player.InstanceId)?.Owner as ArenaMatch;

        lock (sync)
        {
            var fighter = match?.Get(guid);
            var spectator = match?.GetSpectator(guid);

            if (spectator != null)
            {
                // someone who came to watch can't be harmed, but /die still works: they are just taken back
                SendSpectatorHome(match, spectator, player, homeDelaySeconds);
                return;
            }

            if (fighter == null)
            {
                // the arena has nothing on them any more: they still have to stand up again somewhere
                player.ReturnFromArena(
                    null,
                    player.PlayerKillerStatus,
                    player.LastPkAttackTimestamp,
                    player.InstanceId,
                    homeDelaySeconds
                );
                return;
            }

            var by = topDamager?.Name;
            var news =
                by != null && by != fighter.Name
                    ? $"{fighter.Name} has been defeated by {by}!"
                    : $"{fighter.Name} has been defeated!";

            // a fellowship fights on while anybody on it is standing: whoever is defeated watches the rest
            if (
                match.State == ArenaMatchState.Fighting
                && !fighter.Eliminated
                && !fighter.Returned
                && SpectatingEnabled
                && match.Fighters.Any(f => f != fighter && f.Side == fighter.Side && !f.Eliminated)
            )
            {
                Spectate(match, fighter, player, homeDelaySeconds);
                Eliminate(match, fighter, player, news);
                return;
            }

            // first, so that they get to finish falling. If they were sent home already (the duel ended a moment ago),
            // that will stand them up again when it happens.
            SendHome(match, fighter, player, homeDelaySeconds);

            if (match.State == ArenaMatchState.Fighting && !fighter.Eliminated)
            {
                Eliminate(match, fighter, player, news);
            }
            else if (match.State is ArenaMatchState.Arriving or ArenaMatchState.Countdown)
            {
                CallOff(match, $"{fighter.Name} fell before the duel began.", fighter);
            }
        }
    }

    /// <summary>
    /// Called as a player logs out, before they are saved. Someone in a duel gives it up, or calls it off if it had not begun,
    /// and gets their own player killer status back. Where they are saved is where they were before the duel (ArenaMatch.GetReturnPosition).
    /// </summary>
    public static void OnPlayerLoggingOut(Player player)
    {
        var guid = player.Guid.Full;

        lock (sync)
        {
            var waiting = queue.Take(guid);

            if (waiting?.Size > 1)
            {
                TellQueued(
                    waiting,
                    $"{player.Name} has logged out, so your fellowship has left the arena queue.",
                    except: guid
                );
            }

            var watched = matches.FirstOrDefault(m => m.GetSpectator(guid) != null);
            if (watched != null)
            {
                watched.GetSpectator(guid).Returned = true;
                player.StopArenaSpectating(reveal: false);
                return;
            }

            var match = FindMatch(guid) ?? matches.FirstOrDefault(m => m.Get(guid) != null && m.InInstance(player));
            if (match == null)
            {
                return;
            }

            var fighter = match.Get(guid);
            fighter.Confirmation = null;

            if (match.InInstance(player) && !fighter.Returned)
            {
                fighter.Returned = true;
                player.RestoreAfterArena(fighter.OriginalStatus, fighter.OriginalLastPkAttack);
            }

            if (match.State == ArenaMatchState.Fighting)
            {
                if (!fighter.Eliminated)
                {
                    Eliminate(match, fighter, player, $"{fighter.Name} has logged out.");
                }
            }
            else if (match.State != ArenaMatchState.Ended)
            {
                CallOff(match, $"{fighter.Name} has logged out.", fighter);
            }
        }
    }

    /// <summary>
    /// Called every world tick. Pairs the queue, and moves every duel along.
    /// </summary>
    public static void Tick()
    {
        var now = UtcNow();

        if (now < nextTick)
        {
            return;
        }

        nextTick = now + TickInterval;

        List<ArenaMatch> toOpen;

        lock (sync)
        {
            foreach (var expired in challengeCooldowns.Where(c => c.Value <= now).Select(c => c.Key).ToList())
            {
                challengeCooldowns.Remove(expired);
            }

            if (matches.Count == 0 && queue.Count == 0)
            {
                return;
            }

            if (!Enabled)
            {
                ShutDown();
            }

            PairQueue(now);

            foreach (var match in matches.ToList())
            {
                Update(match, now);
                UpdateSpectators(match, now);
            }

            matches.RemoveAll(m => m.State == ArenaMatchState.Ended && now >= m.EndedAt + CloseDelay);

            toOpen = matches.Where(m => m.State == ArenaMatchState.Starting).ToList();

            foreach (var match in toOpen)
            {
                match.State = ArenaMatchState.Opening;
            }
        }

        // not while holding the lock: making an instance loads its landblocks
        foreach (var match in toOpen)
        {
            Open(match);
        }
    }

    #endregion

    #region The life of a duel

    /// <param name="queueEntry">
    /// Their place in the queue (theirs, or their fellowship's), if the queue paired them. Someone who is challenged while they wait
    /// in the queue on their own keeps theirs too.
    /// </param>
    private static ArenaFighter NewFighter(Player player, int side, bool accepted, ArenaQueueEntry queueEntry = null)
    {
        return new ArenaFighter
        {
            Guid = player.Guid.Full,
            Name = player.Name,
            Level = player.Level ?? 1,
            Side = side,
            Address = AddressOf(player),
            Accepted = accepted,
            QueueEntry = queueEntry ?? queue.Find(player.Guid.Full)
        };
    }

    private static ArenaMatch NewMatch(
        ArenaMatchKind kind,
        bool rated,
        bool scaled,
        DateTime now,
        params ArenaFighter[] fighters
    )
    {
        // whoever was waiting in the queue is taken out of it, and put back if this duel is called off before it begins
        foreach (var fighter in fighters)
        {
            queue.Remove(fighter.Guid);
        }

        var match = new ArenaMatch
        {
            Id = ++lastMatchId,
            Kind = kind,
            Rated = rated,
            Scaled = scaled,
            Fighters = fighters.ToList(),
            State = ArenaMatchState.Accepting,
            Deadline = now + AcceptTime
        };

        matches.Add(match);

        _log.Information("[ARENA] {Match}", match.Describe());

        return match;
    }

    /// <summary>
    /// Asks a fighter whether they will fight. False if they could not be asked (they have another yes/no question open).
    /// </summary>
    private static bool Ask(ArenaMatch match, ArenaFighter fighter, Player player, string question)
    {
        Confirmation_Arena confirmation = null;
        confirmation = new Confirmation_Arena(player.Guid, yes => OnAnswer(match, fighter, confirmation, yes));

        fighter.Confirmation = confirmation;

        if (!player.ConfirmationManager.EnqueueSend(confirmation, question))
        {
            fighter.Confirmation = null;
            return false;
        }

        return true;
    }

    private static void OnAnswer(ArenaMatch match, ArenaFighter fighter, Confirmation_Arena confirmation, bool yes)
    {
        lock (sync)
        {
            // an answer to a question that has been taken back, or answered already
            if (fighter.Confirmation != confirmation)
            {
                return;
            }

            fighter.Confirmation = null;

            var player = PlayerManager.GetOnlinePlayer(fighter.Guid);

            if (match.State != ArenaMatchState.Accepting)
            {
                if (yes)
                {
                    player?.SendMessage("That duel is off.");
                }

                return;
            }

            if (!yes)
            {
                if (match.Kind == ArenaMatchKind.Challenge)
                {
                    foreach (var challenger in match.Fighters.Where(f => f != fighter))
                    {
                        challengeCooldowns[(challenger.Guid, fighter.Guid)] = UtcNow() + ChallengeCooldown;
                    }
                }

                CallOff(
                    match,
                    $"{fighter.Name} said no.",
                    new[] { fighter },
                    culpritsKeepTheirPlace: match.Kind == ArenaMatchKind.Challenge
                );
                return;
            }

            fighter.Accepted = true;

            if (match.Fighters.All(f => f.Accepted))
            {
                match.State = ArenaMatchState.Starting;
                Tell(match, "Everyone is ready. You are being taken to the arena...");
            }
            else
            {
                player?.SendMessage(
                    match.Fighters.Count > 2
                        ? "You said yes. Waiting for everyone else..."
                        : "You said yes. Waiting for your opponent..."
                );
            }
        }
    }

    private static void PairQueue(DateTime now)
    {
        // whoever has gone is dropped: a player who has logged out, or a fellowship that has changed since it was put in the queue
        foreach (var entry in queue.Entries.ToList())
        {
            var gone = WhyEntryIsGone(entry);

            if (gone != null)
            {
                queue.Remove(entry.Guid);
                TellQueued(entry, gone);
            }
        }

        while (queue.TryTakePair(BlockSameAddress, out var first, out var second))
        {
            var entries = new[] { first, second };
            var problems = entries.Select(WhyEntryCantDuel).ToArray();

            if (problems.Any(p => p != null))
            {
                // whoever can't duel any more is dropped, and whoever can is put back where they were
                for (var i = 0; i < 2; i++)
                {
                    if (problems[i] == null)
                    {
                        queue.Add(entries[i]);
                    }
                    else
                    {
                        TellQueued(
                            entries[i],
                            $"{(entries[i].Size == 1 ? "You have" : "Your fellowship has")} been taken out of the arena queue. {problems[i]}"
                        );
                    }
                }

                continue;
            }

            var sides = entries
                .Select(e => e.Members.Select(m => PlayerManager.GetOnlinePlayer(m.Guid)).ToList())
                .ToArray();

            var fighters = new List<ArenaFighter>();

            for (var side = 0; side < 2; side++)
            {
                fighters.AddRange(sides[side].Select(p => NewFighter(p, side, accepted: false, entries[side])));
            }

            var sameAddress = ArenaQueue.SharesAnAddress(
                first.Members.Select(m => m.Address),
                second.Members.Select(m => m.Address)
            );

            // the queue only pairs entries that asked for the same kind of duel, and are the same size
            var match = NewMatch(
                ArenaMatchKind.Queue,
                rated: first.Rated && !sameAddress,
                first.Scaled,
                now,
                fighters.ToArray()
            );

            var kind = DescribeDuel(match.Scaled, match.Rated, match.SideSize);

            foreach (var fighter in match.Fighters)
            {
                var player = PlayerManager.GetOnlinePlayer(fighter.Guid);
                var opponents = entries[1 - fighter.Side];

                var question =
                    match.SideSize == 1
                        ? $"An opponent has been found for {kind}: {Introduce(sides[1 - fighter.Side][0], match.Scaled)}.{ExplainScaling(match.Scaled)} Will you fight?"
                        : $"Opponents have been found for your fellowship, for {kind}: {IntroduceFellowship(opponents.Name, sides[1 - fighter.Side], match.Scaled)}.{ExplainScaling(match.Scaled)} Will you fight?";

                if (!Ask(match, fighter, player, question))
                {
                    player.SendMessage(
                        "Opponents were found, but you have another question open, so you have been taken out of the arena queue. Join it again when you have answered it."
                    );
                    CallOff(match, $"{player.Name} can't be asked right now.", fighter);
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Why an entry of the queue has to go, or null if it can stay: someone in it has logged out, or the fellowship is not the one that
    /// was put in the queue any more (someone left it or joined it)
    /// </summary>
    private static string WhyEntryIsGone(ArenaQueueEntry entry)
    {
        foreach (var member in entry.Members)
        {
            if (PlayerManager.GetOnlinePlayer(member.Guid) == null)
            {
                return $"{member.Name} has logged out, so your fellowship has left the arena queue.";
            }
        }

        if (entry.Size == 1)
        {
            return null;
        }

        var fellowship = PlayerManager.GetOnlinePlayer(entry.Guid)?.Fellowship;
        var now = fellowship?.GetFellowshipMembers().Keys.ToHashSet();

        if (now == null || now.Count != entry.Size || !entry.Members.All(m => now.Contains(m.Guid)))
        {
            return "Your fellowship has changed, so it has left the arena queue. Its leader can put it back with /arena queue fellowship.";
        }

        return null;
    }

    /// <summary>
    /// Why an entry of the queue can't fight the duel it has been paired for, or null if it can
    /// </summary>
    private static string WhyEntryCantDuel(ArenaQueueEntry entry)
    {
        var self = entry.Size == 1;

        foreach (var member in entry.Members)
        {
            var player = PlayerManager.GetOnlinePlayer(member.Guid);

            var why =
                player == null
                    ? $"{member.Name} is gone."
                    : WhyCantDuel(player, self) ?? (entry.Scaled ? WhyCantScale(player, self) : null);

            if (why != null)
            {
                return why;
            }
        }

        return null;
    }

    private static void Update(ArenaMatch match, DateTime now)
    {
        switch (match.State)
        {
            case ArenaMatchState.Accepting:
                if (now >= match.Deadline)
                {
                    var silent = match.Fighters.Where(f => !f.Accepted).ToList();

                    // closes their question: "You waited too long to answer the question!"
                    foreach (var fighter in silent)
                    {
                        CloseQuestion(fighter, quiet: false);
                    }

                    if (match.Kind == ArenaMatchKind.Challenge)
                    {
                        foreach (var fighter in silent)
                        {
                            foreach (var challenger in match.Fighters.Where(f => f.Accepted))
                            {
                                challengeCooldowns[(challenger.Guid, fighter.Guid)] = now + ChallengeCooldown;
                            }
                        }
                    }

                    CallOff(
                        match,
                        $"{string.Join(" and ", silent.Select(f => f.Name))} did not answer.",
                        silent.ToArray(),
                        culpritsKeepTheirPlace: match.Kind == ArenaMatchKind.Challenge
                    );
                }
                break;

            case ArenaMatchState.Arriving:
                UpdateArriving(match, now);
                break;

            case ArenaMatchState.Countdown:
                UpdateCountdown(match, now);
                break;

            case ArenaMatchState.Fighting:
                UpdateFighting(match, now);
                break;

            case ArenaMatchState.Ended:
                if (match.Instance != null && now >= match.EndedAt + CloseDelay)
                {
                    // whoever is still in there is sent home by the instance (ArenaMatch.GetReturnPosition)
                    InstanceManager.Close(match.Instance.Id);
                }
                break;
        }
    }

    /// <summary>
    /// Makes the duel's instance, and sends the fighters into it. Not called while holding the lock, because making the instance loads its landblocks.
    /// </summary>
    private static void Open(ArenaMatch match)
    {
        var map = ArenaMaps.PickRandom();
        WorldInstance instance = null;

        if (map != null)
        {
            try
            {
                instance = InstanceManager.Create(map.Template, match);
            }
            catch (Exception ex)
            {
                _log.Error(ex, "[ARENA] Could not make an instance of {Map} for duel #{Match}", map.Name, match.Id);
            }
        }

        lock (sync)
        {
            if (match.State != ArenaMatchState.Opening)
            {
                // called off while the instance was being made
                if (instance != null)
                {
                    InstanceManager.Close(instance.Id);
                }

                return;
            }

            if (instance == null)
            {
                CallOff(match, map == null ? "there is no arena to duel in." : "the arena could not be opened.");
                return;
            }

            match.Map = map;
            match.Instance = instance;

            SendIn(match, UtcNow());
        }
    }

    private static void SendIn(ArenaMatch match, DateTime now)
    {
        var players = new List<Player>();

        foreach (var fighter in match.Fighters)
        {
            var player = PlayerManager.GetOnlinePlayer(fighter.Guid);
            var why = player == null ? $"{fighter.Name} is gone." : WhyCantDuel(player, self: false);

            if (why != null)
            {
                CallOff(match, why, fighter);
                return;
            }

            players.Add(player);
        }

        // every side gets a different place to start, at random, and a fellowship starts together: players can stand
        // in the same place, as they do when a portal or a lifestone puts many of them there at once
        var starts = match.Map.Starts.OrderBy(_ => ThreadSafeRandom.Next(0, int.MaxValue - 1)).ToList();

        for (var i = 0; i < match.Fighters.Count; i++)
        {
            var fighter = match.Fighters[i];
            var player = players[i];

            // this is what makes sure they go back to where they were, as they were
            fighter.Home = new Position(player.Location);
            fighter.OriginalStatus = player.PlayerKillerStatus;
            fighter.OriginalLastPkAttack = player.LastPkAttackTimestamp;

            fighter.Start = new Position(starts[fighter.Side % starts.Count]);
            WorldObject.AdjustDungeon(fighter.Start, match.Instance.Id);

            var opponents = string.Join(", ", match.Opponents(fighter).Select(f => f.Name));
            var team = match.Fighters.Where(f => f.Side == fighter.Side && f != fighter).Select(f => f.Name).ToList();

            player.SendMessage(
                $"You are going to {match.Map.Description} to duel {opponents}{(team.Count > 0 ? $", with {string.Join(", ", team)} on your side" : "")}."
            );

            InstanceManager.Enter(player, match.Instance, fighter.Start);
        }

        match.State = ArenaMatchState.Arriving;
        match.Deadline = now + ArriveTime;

        _log.Information("[ARENA] {Match}", match.Describe());
    }

    private static void UpdateArriving(ArenaMatch match, DateTime now)
    {
        foreach (var fighter in match.Fighters.Where(f => !f.Arrived))
        {
            var player = PlayerManager.GetOnlinePlayer(fighter.Guid);

            if (player == null)
            {
                CallOff(match, $"{fighter.Name} is gone.", fighter);
                return;
            }

            if (match.InInstance(player) && !player.Teleporting)
            {
                fighter.Arrived = true;
                OnPlayer(player, () => player.PrepareForArenaDuel());
            }
        }

        if (match.Fighters.All(f => f.Arrived))
        {
            match.State = ArenaMatchState.Countdown;
            match.Deadline = now + CountdownTime;
            match.LastAnnounced = (int)Math.Ceiling(CountdownTime.TotalSeconds);

            Tell(
                match,
                $"The duel begins in {match.LastAnnounced} seconds. Nobody can be harmed until then. Your own enchantments stay with you.",
                spectators: false
            );
            TellSpectators(match, $"The duel begins in {match.LastAnnounced} seconds.");

            if (match.Scaled)
            {
                TellScaling(match);
            }
        }
        else if (now >= match.Deadline)
        {
            var late = match.Fighters.Where(f => !f.Arrived).ToList();
            CallOff(match, $"{string.Join(" and ", late.Select(f => f.Name))} did not arrive.", late.ToArray());
        }
    }

    private static void UpdateCountdown(ArenaMatch match, DateTime now)
    {
        foreach (var fighter in match.Fighters)
        {
            var player = PlayerManager.GetOnlinePlayer(fighter.Guid);

            if (player == null || !match.InInstance(player))
            {
                CallOff(match, $"{fighter.Name} has left the arena.", fighter);
                return;
            }
        }

        var left = (int)Math.Ceiling((match.Deadline - now).TotalSeconds);

        if (left <= 0)
        {
            match.State = ArenaMatchState.Fighting;
            match.Deadline = now + TimeLimit;
            match.FightStartedAt = now;

            foreach (var fighter in match.Fighters)
            {
                var player = PlayerManager.GetOnlinePlayer(fighter.Guid);
                OnPlayer(player, () => player.BeginArenaDuel());
            }

            Tell(match, $"Fight! You have {TimeLimit.TotalMinutes:N0} minutes.", spectators: false);
            TellSpectators(match, "Fight!");

            _log.Information("[ARENA] {Match}", match.Describe());
        }
        else if (left < match.LastAnnounced && (left <= 5 || left % 5 == 0))
        {
            match.LastAnnounced = left;
            Tell(match, $"{left}...");
        }
    }

    private static void UpdateFighting(ArenaMatch match, DateTime now)
    {
        foreach (var fighter in match.Fighters.Where(f => !f.Eliminated).ToList())
        {
            // the one before this ended it
            if (match.State != ArenaMatchState.Fighting)
            {
                return;
            }

            var player = PlayerManager.GetOnlinePlayer(fighter.Guid);

            // they went through a portal, recalled, or were moved by an admin: that is giving up
            if (player == null || !match.InInstance(player))
            {
                Eliminate(match, fighter, player, $"{fighter.Name} has left the arena.");

                if (player != null && !fighter.Returned)
                {
                    fighter.Returned = true;
                    OnPlayer(
                        player,
                        () => player.RestoreAfterArena(fighter.OriginalStatus, fighter.OriginalLastPkAttack)
                    );
                }

                continue;
            }

            CheckBoundary(match, fighter, player, now);
        }

        // a defeated fighter who is watching and leaves by themselves (a portal, a recall) gets their own status back where they are
        foreach (var fighter in match.Fighters.Where(f => f.Eliminated && f.Spectating && !f.Returned))
        {
            var player = PlayerManager.GetOnlinePlayer(fighter.Guid);

            if (player == null || !match.InInstance(player))
            {
                fighter.Returned = true;
                OnPlayer(player, () => player.RestoreAfterArena(fighter.OriginalStatus, fighter.OriginalLastPkAttack));
            }
        }

        if (match.State == ArenaMatchState.Fighting && now >= match.Deadline)
        {
            Finish(match, winningSide: null, "Time is up. The duel is a draw.");
        }
    }

    /// <summary>
    /// A fighter who stays outside the radius of an outdoor arena for too long is disqualified
    /// </summary>
    private static void CheckBoundary(ArenaMatch match, ArenaFighter fighter, Player player, DateTime now)
    {
        if (match.Map.DistanceOutside(player.Location) <= 0)
        {
            fighter.OutsideSince = null;
            return;
        }

        fighter.OutsideSince ??= now;

        var left = (int)Math.Ceiling((fighter.OutsideSince.Value + BoundaryGrace - now).TotalSeconds);

        if (left <= 0)
        {
            player.SendMessage("You stayed outside the arena, and have been disqualified.");
            Eliminate(match, fighter, player, $"{fighter.Name} has been disqualified for leaving the arena.");
            SendHome(match, fighter, player, HomeDelaySeconds);
        }
        else if (fighter.OutsideSince == now || left < fighter.NextBoundaryWarning)
        {
            fighter.NextBoundaryWarning = left;
            player.SendMessage(
                $"You are outside the arena! Go back in within {left} second{(left == 1 ? "" : "s")}, or you will be disqualified."
            );
        }
    }

    /// <summary>
    /// Takes a fighter out of the fight, and ends the duel if their side has nobody left
    /// </summary>
    private static void Eliminate(ArenaMatch match, ArenaFighter fighter, Player player, string news)
    {
        if (fighter.Eliminated)
        {
            return;
        }

        fighter.Eliminated = true;

        if (match.State != ArenaMatchState.Fighting)
        {
            return;
        }

        Tell(match, news, except: fighter);

        var sidesStanding = match.Fighters.Where(f => !f.Eliminated).Select(f => f.Side).Distinct().ToList();

        if (sidesStanding.Count <= 1)
        {
            Finish(match, sidesStanding.Count == 1 ? sidesStanding[0] : null, null);
        }
    }

    /// <summary>
    /// The duel is over: the result is recorded, and everyone who is still in the arena is taken home
    /// </summary>
    /// <param name="headline">What everyone is told first, if anything</param>
    private static void Finish(ArenaMatch match, int? winningSide, string headline)
    {
        match.State = ArenaMatchState.Ended;
        match.WinningSide = winningSide;
        match.EndedAt = UtcNow();

        if (headline != null)
        {
            Tell(match, headline);
        }

        var results = Record(match);

        foreach (var fighter in match.Fighters)
        {
            var player = PlayerManager.GetOnlinePlayer(fighter.Guid);
            if (player == null)
            {
                continue;
            }

            player.SendMessage(results.TryGetValue(fighter.Guid, out var result) ? result : "The duel is over.");

            if (!fighter.Returned && match.InInstance(player))
            {
                player.SendMessage($"You will be taken back in {HomeDelaySeconds:N0} seconds.");
                SendHome(match, fighter, player, HomeDelaySeconds);
            }
        }

        SendSpectatorsHome(
            match,
            winningSide == null
                ? "The duel is over. Nobody won."
                : $"The duel is over: {string.Join(", ", match.Fighters.Where(f => f.Side == winningSide).Select(f => f.Name))} won.",
            HomeDelaySeconds
        );

        _log.Information(
            "[ARENA] {Match}: {Result}",
            match.Describe(),
            winningSide == null
                ? "draw"
                : $"won by {string.Join(", ", match.Fighters.Where(f => f.Side == winningSide).Select(f => f.Name))}"
        );
    }

    /// <summary>
    /// Records the result on the fighters' characters (online or not), and says what it means for each of them
    /// </summary>
    private static Dictionary<uint, string> Record(ArenaMatch match)
    {
        var results = new Dictionary<uint, string>();

        var winners = match.Fighters.Where(f => match.WinningSide != null && f.Side == match.WinningSide).ToList();
        var losers = match.Fighters.Where(f => match.WinningSide != null && f.Side != match.WinningSide).ToList();

        foreach (var fighter in match.Fighters)
        {
            results[fighter.Guid] =
                match.WinningSide == null
                    ? "Nobody won."
                    : winners.Contains(fighter)
                        ? $"You have won the duel against {string.Join(", ", losers.Select(f => f.Name))}!"
                        : $"You have lost the duel against {string.Join(", ", winners.Select(f => f.Name))}.";
        }

        // an unrated duel is not on any board: no rating, and no record
        if (!match.Rated)
        {
            return results;
        }

        var board = match.Board;

        var characters = match.Fighters.ToDictionary(f => f.Guid, f => PlayerManager.FindByGuid(f.Guid));
        var standings = match.Fighters.ToDictionary(
            f => f.Guid,
            f => characters[f.Guid] is IPlayer character ? ArenaBoards.Get(character, board) : null
        );

        foreach (var fighter in match.Fighters)
        {
            var standing = standings[fighter.Guid];

            if (standing == null)
            {
                continue;
            }

            if (match.WinningSide == null)
            {
                standing.Draws++;
            }
            else if (winners.Contains(fighter))
            {
                standing.Wins++;
            }
            else
            {
                standing.Losses++;
            }
        }

        // every fighter is rated against the average of the other side; a draw changes no rating
        if (match.WinningSide != null && match.Fighters.All(f => standings[f.Guid] != null))
        {
            var after = ArenaElo.RateTeams(
                winners.Select(f => standings[f.Guid].Rating).ToList(),
                losers.Select(f => standings[f.Guid].Rating).ToList(),
                EloK
            );

            foreach (var (side, ratings) in new[] { (winners, after.Winners), (losers, after.Losers) })
            {
                for (var i = 0; i < side.Count; i++)
                {
                    var standing = standings[side[i].Guid];
                    var change = ratings[i] - standing.Rating;

                    standing.Rating = ratings[i];

                    results[side[i].Guid] +=
                        $" Your {board.Name} arena rating is now {standing.Rating} ({(change >= 0 ? "+" : "")}{change}).";
                }
            }
        }

        foreach (var fighter in match.Fighters)
        {
            if (standings[fighter.Guid] != null)
            {
                ArenaBoards.Set(characters[fighter.Guid], board, standings[fighter.Guid]);
            }
        }

        return results;
    }

    /// <summary>
    /// Arena dueling has been turned off (arena_dueling_enabled): every duel that is going on is called off, which takes the fighters
    /// who are in the arena already home, and everyone waiting in the queue is taken out of it. Duels that have ended already
    /// finish as usual: their instances are closed a little later.
    /// </summary>
    private static void ShutDown()
    {
        var calledOff = 0;

        foreach (var match in matches.Where(m => m.State != ArenaMatchState.Ended).ToList())
        {
            CallOff(match, "arena dueling has been turned off.");
            calledOff++;
        }

        var waiting = queue.Entries.ToList();

        foreach (var entry in waiting)
        {
            queue.Remove(entry.Guid);

            TellQueued(
                entry,
                $"Arena dueling has been turned off, so {(entry.Size == 1 ? "you have" : "your fellowship has")} been taken out of the arena queue."
            );
        }

        if (calledOff > 0 || waiting.Count > 0)
        {
            _log.Information(
                "[ARENA] Arena dueling has been turned off: {Duels} duel(s) called off, {Waiting} taken out of the queue",
                calledOff,
                waiting.Count
            );
        }
    }

    /// <summary>
    /// Calls a duel off. Nothing is recorded. Fighters who were sent to the arena already are taken home, and if they had not been sent yet,
    /// those who were waiting in the queue are put back in it, in the place they had: all but whoever it was called off because of
    /// (saying no to a duel the queue found is leaving the queue, and so is calling a duel off with /arena leave). A fellowship goes back
    /// only as a whole: not if any of them is why it is off.
    /// </summary>
    /// <param name="culprits">Whoever it is called off because of: they are just told it is off, and not put back in the queue</param>
    private static void CallOff(ArenaMatch match, string reason, params ArenaFighter[] culprits)
    {
        CallOff(match, reason, culprits, culpritsKeepTheirPlace: false);
    }

    /// <param name="culpritsKeepTheirPlace">
    /// True when it was only a challenge that was turned down: someone who was waiting in the queue when they were challenged
    /// goes on waiting in it
    /// </param>
    private static void CallOff(ArenaMatch match, string reason, ArenaFighter[] culprits, bool culpritsKeepTheirPlace)
    {
        if (match.State == ArenaMatchState.Ended)
        {
            return;
        }

        var beforeTheArena =
            match.State is ArenaMatchState.Accepting or ArenaMatchState.Starting or ArenaMatchState.Opening;

        match.State = ArenaMatchState.Ended;
        match.CalledOff = true;
        match.EndedAt = UtcNow();

        foreach (var fighter in match.Fighters)
        {
            CloseQuestion(fighter, quiet: true);

            var player = PlayerManager.GetOnlinePlayer(fighter.Guid);
            if (player == null)
            {
                continue;
            }

            var culprit = culprits.Contains(fighter);

            player.SendMessage(culprit ? "The duel is off." : $"The duel is off: {reason}");

            // also someone who is still on their way in: once they get there, this takes them straight back
            // (and if they are still in portal space then, closing the instance does)
            if (fighter.Home != null)
            {
                SendHome(match, fighter, player, 2);
            }
        }

        SendSpectatorsHome(match, $"The duel is off: {reason}", 2);

        // nobody is put back in the queue while arena dueling is turned off: it is being emptied
        if (Enabled && beforeTheArena)
        {
            foreach (var entry in match.Fighters.Select(f => f.QueueEntry).Where(e => e != null).Distinct().ToList())
            {
                var theirs = match.Fighters.Where(f => f.QueueEntry == entry).ToList();

                if (!culpritsKeepTheirPlace && theirs.Any(culprits.Contains))
                {
                    continue;
                }

                if (entry.Members.Any(m => PlayerManager.GetOnlinePlayer(m.Guid) == null))
                {
                    continue;
                }

                queue.Add(entry);

                TellQueued(
                    entry,
                    entry.Size == 1
                        ? "You are back in the arena queue, in the place you had."
                        : "Your fellowship is back in the arena queue, in the place it had."
                );
            }
        }

        _log.Information("[ARENA] {Match}: called off, {Reason}", match.Describe(), reason);
    }

    /// <summary>
    /// Takes a fighter back to where they were before the duel, with the player killer status they had, after a delay.
    /// A defeated fighter is stood up again on the way, as they would be after dying.
    /// </summary>
    private static void SendHome(ArenaMatch match, ArenaFighter fighter, Player player, double delaySeconds)
    {
        if (fighter.Returned)
        {
            return;
        }

        fighter.Returned = true;

        var instanceId = match.Instance?.Id ?? Landblock.PersistentInstance;

        player.ReturnFromArena(
            fighter.Home,
            fighter.OriginalStatus,
            fighter.OriginalLastPkAttack,
            instanceId,
            delaySeconds
        );
    }

    private static void CloseQuestion(ArenaFighter fighter, bool quiet)
    {
        var confirmation = fighter.Confirmation;
        if (confirmation == null)
        {
            return;
        }

        fighter.Confirmation = null;

        PlayerManager
            .GetOnlinePlayer(fighter.Guid)
            ?.ConfirmationManager.EnqueueAbort(confirmation.ConfirmationType, confirmation.ContextId, quiet);
    }

    #endregion

    #region Helpers

    /// <summary>
    /// The duel this player is in that has not ended yet, if any
    /// </summary>
    private static ArenaMatch FindMatch(uint guid)
    {
        return matches.FirstOrDefault(m => m.State != ArenaMatchState.Ended && m.Get(guid) != null);
    }

    /// <param name="spectators">Whether whoever has come to watch is told too</param>
    private static void Tell(ArenaMatch match, string message, ArenaFighter except = null, bool spectators = true)
    {
        foreach (var fighter in match.Fighters.Where(f => f != except))
        {
            PlayerManager.GetOnlinePlayer(fighter.Guid)?.SendMessage(message);
        }

        if (spectators)
        {
            TellSpectators(match, message);
        }
    }

    private static void TellSpectators(ArenaMatch match, string message)
    {
        foreach (var spectator in match.Spectators.Where(s => !s.Returned))
        {
            PlayerManager.GetOnlinePlayer(spectator.Guid)?.SendMessage(message);
        }
    }

    private static void OnPlayer(Player player, Action action)
    {
        if (player == null)
        {
            return;
        }

        var chain = new ActionChain();
        chain.AddAction(player, action);
        chain.EnqueueChain();
    }

    /// <param name="scaled">Whose rating to show: the scaled board's for a scaled duel</param>
    private static string Introduce(Player player, bool scaled)
    {
        var board = new ArenaBoard(1, scaled);

        return $"{player.Name} (level {player.Level ?? 1}, {board.Name} arena rating {ArenaBoards.Get(player, board).Rating})";
    }

    /// <summary>
    /// "Bob's fellowship: Bob (level 120), Al (level 95), average 2v2 arena rating 1412"
    /// </summary>
    private static string IntroduceFellowship(string leaderName, IReadOnlyList<Player> players, bool scaled)
    {
        var board = new ArenaBoard(players.Count, scaled);
        var average = (int)Math.Round(players.Average(p => ArenaBoards.Get(p, board).Rating));

        return $"{leaderName}'s fellowship: {string.Join(", ", players.Select(p => $"{p.Name} (level {p.Level ?? 1})"))}, average {board.Name} arena rating {average}";
    }

    /// <summary>
    /// "a raw duel", "a scaled, unrated duel", "a 3v3 raw duel"
    /// </summary>
    public static string DescribeDuel(bool scaled, bool rated, int size = 1)
    {
        return $"a {(size > 1 ? $"{size}v{size} " : "")}{(scaled ? "scaled" : "raw")}{(rated ? "" : ", unrated")} duel";
    }

    /// <summary>
    /// Tells everyone in an entry of the queue something
    /// </summary>
    private static void TellQueued(ArenaQueueEntry entry, string message, uint? except = null)
    {
        foreach (var member in entry.Members.Where(m => m.Guid != except))
        {
            PlayerManager.GetOnlinePlayer(member.Guid)?.SendMessage(message);
        }
    }

    /// <summary>
    /// If one of these players is waiting in the queue with their fellowship, says so: they can't be taken into another duel
    /// without taking the fellowship out of the queue first. Null if none of them is.
    /// </summary>
    private static string WhoWaitsWithAFellowship(IEnumerable<Player> players)
    {
        foreach (var player in players)
        {
            var entry = queue.Find(player.Guid.Full);

            if (entry != null && entry.Size > 1)
            {
                return $"{player.Name} is waiting in the arena queue with {entry.Describe()}. /arena leave takes it out of the queue.";
            }
        }

        return null;
    }

    private static string ExplainScaling(bool scaled)
    {
        return scaled ? " In a scaled duel the higher-level fighter fights at the other one's level." : "";
    }

    /// <summary>
    /// Why this player can't fight a scaled duel, or null if they can. Scaling only goes down to level 10 (LevelScaling),
    /// so below that a scaled duel would be a raw one.
    /// </summary>
    private static string WhyCantScale(Player player, bool self = true)
    {
        if ((player.Level ?? 1) >= LevelScaling.MinimumScaledLevel)
        {
            return null;
        }

        return self
            ? $"Scaled duels are for level {LevelScaling.MinimumScaledLevel} and up."
            : $"{player.Name} is not level {LevelScaling.MinimumScaledLevel} yet, and scaled duels are for level {LevelScaling.MinimumScaledLevel} and up.";
    }

    /// <summary>
    /// Tells the fighters of a scaled duel who fights at whose level
    /// </summary>
    private static void TellScaling(ArenaMatch match)
    {
        if (match.SideSize > 1)
        {
            Tell(
                match,
                "This duel is scaled: of any two fighters, the higher-level one fights the other at their level."
            );
            return;
        }

        var lowest = match.Fighters.Min(f => f.Level);

        foreach (var fighter in match.Fighters)
        {
            var message =
                fighter.Level > lowest
                    ? $"This duel is scaled: you fight as if you were level {lowest}."
                    : "This duel is scaled: your opponent fights as if they were your level.";

            PlayerManager.GetOnlinePlayer(fighter.Guid)?.SendMessage(message);
        }
    }

    private static string AddressOf(Player player)
    {
        return player.Session?.EndPointC2S?.Address?.ToString();
    }

    #endregion
}
