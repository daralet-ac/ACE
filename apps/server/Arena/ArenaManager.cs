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
public static class ArenaManager
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

    private static bool Enabled => PropertyManager.GetBool("arena_enabled").Item;

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

    public static int RatingOf(IPlayer player) =>
        player.GetProperty(PropertyInt.ArenaRating) ?? ArenaElo.StartingRating;

    public static (int Wins, int Losses, int Draws) RecordOf(IPlayer player) =>
        (
            player.GetProperty(PropertyInt.ArenaWins) ?? 0,
            player.GetProperty(PropertyInt.ArenaLosses) ?? 0,
            player.GetProperty(PropertyInt.ArenaDraws) ?? 0
        );

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
            return "The arena is closed.";
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
    /// Puts a player in the queue, or changes the level band of someone who is in it already (they keep their place)
    /// </summary>
    /// <param name="levelBand">How many levels apart their opponent may be at most, 0 for any. Null for the server's default.</param>
    public static void JoinQueue(Player player, int? levelBand)
    {
        var why = WhyCantDuel(player);
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

            var band = Math.Max(0, levelBand ?? DefaultLevelBand);
            var already = queue.Entries.FirstOrDefault(e => e.Guid == guid);

            queue.Add(
                new ArenaQueueEntry
                {
                    Guid = guid,
                    Name = player.Name,
                    Level = player.Level ?? 1,
                    LevelBand = band,
                    Address = AddressOf(player),
                    JoinedAt = already?.JoinedAt ?? UtcNow()
                }
            );

            var bandText = band > 0 ? $" for an opponent within {band} level{(band == 1 ? "" : "s")} of you" : "";
            var others = queue.Count - 1;

            player.SendMessage(
                already != null
                    ? $"You are still in the arena queue{bandText}, and kept your place."
                    : $"You are in the arena queue{bandText}. {others} other{(others == 1 ? " is" : "s are")} waiting. /arena leave takes you out of it."
            );
        }
    }

    /// <summary>
    /// Asks another player to a duel
    /// </summary>
    public static void Challenge(Player challenger, string targetName)
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

        var why = WhyCantDuel(challenger) ?? WhyCantDuel(target, self: false);
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
                rated: RatedChallenges && !(BlockSameAddress && sameAddress),
                now,
                NewFighter(challenger, side: 0, accepted: true),
                NewFighter(target, side: 1, accepted: false)
            );

            if (
                !Ask(
                    match,
                    match.Fighters[1],
                    target,
                    $"{Introduce(challenger)} challenges you to a duel in the arena. Nobody loses anything by being defeated. Will you fight?"
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
                $"You challenge {target.Name} to a duel{(match.Rated ? "" : " (it won't change your arena ratings)")}. They have {AcceptTime.TotalSeconds:N0} seconds to answer."
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
            if (queue.Remove(guid))
            {
                player.SendMessage("You have left the arena queue.");
                return;
            }

            var match = FindMatch(guid);
            if (match == null)
            {
                player.SendMessage("You are not in the arena queue or in a duel.");
                return;
            }

            var fighter = match.Get(guid);

            if (match.State == ArenaMatchState.Fighting)
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
        var guid = player.Guid.Full;

        lock (sync)
        {
            var place = queue.PositionOf(guid);
            if (place > 0)
            {
                return $"You are number {place} of {queue.Count} in the arena queue.";
            }

            var match = FindMatch(guid);
            if (match == null)
            {
                return "You are not in the arena queue or in a duel.";
            }

            var fighter = match.Get(guid);
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
                $"{queue.Count} in the queue{(queue.Count > 0 ? ": " + string.Join(", ", queue.Entries.Select(e => e.Name)) : "")}"
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
            return match.Get(player.Guid.Full) != null;
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

            // first, so that they get to finish falling. If they were sent home already (the duel ended a moment ago),
            // that will stand them up again when it happens.
            SendHome(match, fighter, player, homeDelaySeconds);

            if (match.State == ArenaMatchState.Fighting && !fighter.Eliminated)
            {
                var by = topDamager?.Name;
                Eliminate(
                    match,
                    fighter,
                    player,
                    by != null && by != fighter.Name
                        ? $"{fighter.Name} has been defeated by {by}!"
                        : $"{fighter.Name} has been defeated!"
                );
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
            queue.Remove(guid);

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

            PairQueue(now);

            foreach (var match in matches.ToList())
            {
                Update(match, now);
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

    /// <param name="queueEntry">Their place in the queue, if the queue paired them. Someone who is challenged while they wait in the queue keeps theirs too.</param>
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
            QueueEntry = queueEntry ?? queue.Entries.FirstOrDefault(e => e.Guid == player.Guid.Full)
        };
    }

    private static ArenaMatch NewMatch(ArenaMatchKind kind, bool rated, DateTime now, params ArenaFighter[] fighters)
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
                player?.SendMessage("You said yes. Waiting for your opponent...");
            }
        }
    }

    private static void PairQueue(DateTime now)
    {
        // whoever has logged out is dropped
        foreach (var entry in queue.Entries.Where(e => PlayerManager.GetOnlinePlayer(e.Guid) == null).ToList())
        {
            queue.Remove(entry.Guid);
        }

        while (queue.TryTakePair(BlockSameAddress, out var first, out var second))
        {
            var players = new[]
            {
                PlayerManager.GetOnlinePlayer(first.Guid),
                PlayerManager.GetOnlinePlayer(second.Guid)
            };
            var entries = new[] { first, second };
            var problems = players.Select(p => p == null ? "you are gone" : WhyCantDuel(p)).ToArray();

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
                        players[i]?.SendMessage($"You have been taken out of the arena queue. {problems[i]}");
                    }
                }

                continue;
            }

            var sameAddress = first.Address != null && first.Address == second.Address;

            var match = NewMatch(
                ArenaMatchKind.Queue,
                rated: !sameAddress,
                now,
                NewFighter(players[0], side: 0, accepted: false, first),
                NewFighter(players[1], side: 1, accepted: false, second)
            );

            for (var i = 0; i < 2; i++)
            {
                var opponent = players[1 - i];

                if (
                    !Ask(
                        match,
                        match.Fighters[i],
                        players[i],
                        $"An opponent has been found: {Introduce(opponent)}. Will you fight?"
                    )
                )
                {
                    players[i]
                        .SendMessage(
                            "An opponent was found for you, but you have another question open, so you have been taken out of the arena queue. Join it again when you have answered it."
                        );
                    CallOff(match, $"{players[i].Name} can't be asked right now.", match.Fighters[i]);
                    break;
                }
            }
        }
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

        // every fighter gets a different place to start, at random
        var starts = match.Map.Starts.OrderBy(_ => ThreadSafeRandom.Next(0, int.MaxValue - 1)).ToList();

        for (var i = 0; i < match.Fighters.Count; i++)
        {
            var fighter = match.Fighters[i];
            var player = players[i];

            // this is what makes sure they go back to where they were, as they were
            fighter.Home = new Position(player.Location);
            fighter.OriginalStatus = player.PlayerKillerStatus;
            fighter.OriginalLastPkAttack = player.LastPkAttackTimestamp;

            fighter.Start = new Position(starts[i % starts.Count]);
            WorldObject.AdjustDungeon(fighter.Start, match.Instance.Id);

            var opponents = string.Join(", ", match.Opponents(fighter).Select(f => f.Name));
            player.SendMessage($"You are going to {match.Map.Description} to duel {opponents}.");

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
                $"The duel begins in {match.LastAnnounced} seconds. Nobody can be harmed until then. Your own enchantments stay with you."
            );
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

            foreach (var fighter in match.Fighters)
            {
                var player = PlayerManager.GetOnlinePlayer(fighter.Guid);
                OnPlayer(player, () => player.BeginArenaDuel());
            }

            Tell(match, $"Fight! You have {TimeLimit.TotalMinutes:N0} minutes.");

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

        var characters = match.Fighters.ToDictionary(f => f.Guid, f => PlayerManager.FindByGuid(f.Guid));

        if (match.WinningSide == null)
        {
            foreach (var fighter in match.Fighters)
            {
                if (characters[fighter.Guid] is IPlayer character)
                {
                    character.SetProperty(PropertyInt.ArenaDraws, RecordOf(character).Draws + 1);
                }

                results[fighter.Guid] = "Nobody won.";
            }

            return results;
        }

        var winners = match.Fighters.Where(f => f.Side == match.WinningSide).ToList();
        var losers = match.Fighters.Where(f => f.Side != match.WinningSide).ToList();

        foreach (var fighter in winners)
        {
            if (characters[fighter.Guid] is IPlayer character)
            {
                character.SetProperty(PropertyInt.ArenaWins, RecordOf(character).Wins + 1);
            }

            results[fighter.Guid] = $"You have won the duel against {string.Join(", ", losers.Select(f => f.Name))}!";
        }

        foreach (var fighter in losers)
        {
            if (characters[fighter.Guid] is IPlayer character)
            {
                character.SetProperty(PropertyInt.ArenaLosses, RecordOf(character).Losses + 1);
            }

            results[fighter.Guid] = $"You have lost the duel against {string.Join(", ", winners.Select(f => f.Name))}.";
        }

        // only one against one is rated
        if (match.Rated && winners.Count == 1 && losers.Count == 1)
        {
            var winner = characters[winners[0].Guid];
            var loser = characters[losers[0].Guid];

            if (winner != null && loser != null)
            {
                var before = (Winner: RatingOf(winner), Loser: RatingOf(loser));
                var after = ArenaElo.Rate(before.Winner, before.Loser, EloK);

                winner.SetProperty(PropertyInt.ArenaRating, after.Winner);
                loser.SetProperty(PropertyInt.ArenaRating, after.Loser);

                results[winners[0].Guid] +=
                    $" Your arena rating is now {after.Winner} (+{after.Winner - before.Winner}).";
                results[losers[0].Guid] += $" Your arena rating is now {after.Loser} ({after.Loser - before.Loser}).";
            }
        }

        return results;
    }

    /// <summary>
    /// Calls a duel off. Nothing is recorded. Fighters who were sent to the arena already are taken home, and if they had not been sent yet,
    /// those who were waiting in the queue are put back in it, in the place they had: all but whoever it was called off because of
    /// (saying no to a duel the queue found is leaving the queue, and so is calling a duel off with /arena leave).
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

            if (beforeTheArena && fighter.QueueEntry != null && (!culprit || culpritsKeepTheirPlace))
            {
                queue.Add(fighter.QueueEntry);
                player.SendMessage("You are back in the arena queue, in the place you had.");
            }

            // also someone who is still on their way in: once they get there, this takes them straight back
            // (and if they are still in portal space then, closing the instance does)
            if (fighter.Home != null)
            {
                SendHome(match, fighter, player, 2);
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

    private static void Tell(ArenaMatch match, string message, ArenaFighter except = null)
    {
        foreach (var fighter in match.Fighters.Where(f => f != except))
        {
            PlayerManager.GetOnlinePlayer(fighter.Guid)?.SendMessage(message);
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

    private static string Introduce(Player player)
    {
        return $"{player.Name} (level {player.Level ?? 1}, arena rating {RatingOf(player)})";
    }

    private static string AddressOf(Player player)
    {
        return player.Session?.EndPointC2S?.Address?.ToString();
    }

    #endregion
}
