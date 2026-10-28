using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ACE.Server.Commands.PlayerCommands;
using ACE.Server.Entity;
using ACE.Server.Managers;
using ACE.Server.Network.Handlers;
using ACE.Server.WorldObjects;

namespace ACE.Server.Lfg;

/// <summary>
/// /lfgroup (looking for a group: join, list, stop) and /lfmore (looking for more: invite, list, stop)
/// </summary>
public static partial class LfgManager
{
    // Each entry is kept on a line of its own, and sections are split by a blank line, so the chat window's own wrapping stays readable.
    public const string Usage =
        "Find a group, or more players for your fellowship.\n"
        + "\n"
        + "-- Looking for a group --\n"
        + "/lfgroup <activity>: you join the first fellowship that is looking for more, as soon as there is one.\n"
        + "/lfgroup join <name>: join the fellowship that player is in, if it's looking for more and you fit (or ask, if it's "
        + "manual).\n"
        + "/lfgroup list: the players looking for a group.\n"
        + "/lfgroup stop: stop looking.\n"
        + "\n"
        + "-- Looking for more (fellowship leaders) --\n"
        + "/lfmore <activity> [manual]: players looking for it join at once, or with manual you're asked about each. "
        + "It lasts until your fellowship is full, locked or disbanded.\n"
        + "/lfmore invite <name>: invite a player to your fellowship, the same as /invite.\n"
        + "/lfmore list: the fellowships looking for more.\n"
        + "/lfmore stop: stop looking for more.\n"
        + "\n"
        + "-- Activities --\n"
        + "dungeon [level] [d5]: any capstone (/lfgroup: up to that level, of at least that difficulty).\n"
        + "<capstone> [d10]: one capstone, by name or short name (gmg, hoth, eg, mod...), at that difficulty.\n"
        + "leveling: a fellowship that shares XP evenly with you.\n"
        + "quest [town]: quests (in that town).\n"
        + "Anything else: matched by its words, so /lfgroup olthoi finds /lfmore olthoi north.";

    /// <summary>
    /// Does what /lfgroup (lfm false) or /lfmore (lfm true) asks
    /// </summary>
    /// <param name="words">The words after the command</param>
    public static void HandleCommand(Player player, bool lfm, IReadOnlyList<string> words)
    {
        var rest = words.Where(w => !string.IsNullOrWhiteSpace(w)).ToList();
        var first = rest.Count > 0 ? rest[0].ToLowerInvariant() : "";

        // TownEventManager alone adds members to a Town Defense fellowship
        if (lfm && player.Fellowship is { IsTownDefense: true })
        {
            Tell(player, "The Town Defense takes its defenders from the town itself, so it can't look for more.");
            return;
        }

        // inviting is the game's own, so it works whether or not looking for group is turned on
        if (lfm && first == "invite" && rest.Count > 1)
        {
            Invite.AddFellow(player.Session, rest.Skip(1).ToArray());
            return;
        }

        if (!Enabled)
        {
            Tell(player, "Looking for group is turned off.");
            return;
        }

        // list, stop and help only on their own, so an activity can still start with one of them
        var only = rest.Count == 1 ? first : null;

        lock (sync)
        {
            if (rest.Count == 0 || only is "help" or "?")
            {
                Tell(player, Usage + "\n\n" + Status(player));
            }
            else if (only == "list")
            {
                Tell(player, lfm ? ListListings() : ListSeekers());
            }
            else if (only is "stop" or "cancel")
            {
                if (lfm)
                {
                    StopListing(player);
                }
                else
                {
                    StopSeeking(player);
                }
            }
            else if (!lfm && first == "join" && rest.Count > 1)
            {
                JoinListing(player, string.Join(" ", rest.Skip(1)));
            }
            else if (lfm)
            {
                StartListing(player, rest);
            }
            else
            {
                StartSeeking(player, rest);
            }
        }
    }

    /// <summary>
    /// Why the player can't look for a group, or null if they can
    /// </summary>
    private static string WhyCantSeek(Player player)
    {
        return player.Fellowship != null
            ? "You're in a fellowship already. Leave it to look for another, or have its leader look for more with /lfmore."
            : null;
    }

    /// <summary>
    /// Why the player can't look for more for their fellowship, or null if they can
    /// </summary>
    private static string WhyCantList(Player player)
    {
        var fellowship = player.Fellowship;

        if (fellowship == null)
        {
            return "You need a fellowship to look for more: make one in the Fellowship panel first.";
        }

        if (fellowship.FellowshipLeaderGuid != player.Guid.Full)
        {
            return "Only your fellowship's leader can look for more.";
        }

        if (fellowship.IsLocked)
        {
            return "Your fellowship is locked, so nobody new can join it.";
        }

        if (fellowship.FellowshipMembers.Count >= Fellowship.MaxFellows)
        {
            return "Your fellowship is full.";
        }

        return null;
    }

    private static void StartSeeking(Player player, List<string> words)
    {
        var cant = WhyCantSeek(player);

        if (cant != null)
        {
            Tell(player, cant);
            return;
        }

        var activity = LfgParser.Parse(words, lfm: false, out _, out var error);

        if (activity == null)
        {
            Tell(player, error);
            return;
        }

        var level = player.Level ?? 1;

        if (activity.Kind == LfgActivityKind.Capstone)
        {
            var needed = activity.Dungeon?.Level ?? LfgCapstones.LowestLevel;

            if (level < needed)
            {
                Tell(player, $"You need to be level {needed} for {activity.Dungeon?.Name ?? "any capstone"}.");
                return;
            }
        }

        Seek(player, activity);

        var what = activity.Describe(lfm: false);

        Tell(
            player,
            $"You're looking for a group for {what}. You'll join the first fellowship that is looking for more, as soon as there is one. "
                + "/lfgroup stop to stop."
        );

        Post(player, "lfg " + what, $"LFG {what} (level {level}) - to invite: /lfmore invite {player.Name}");

        Match();
    }

    /// <summary>
    /// Makes this the player's lfg, in place of any they had
    /// </summary>
    private static LfgSeeker Seek(Player player, LfgActivity activity)
    {
        seekers.RemoveAll(s => s.Guid == player.Guid.Full);

        var seeker = new LfgSeeker
        {
            Guid = player.Guid.Full,
            Name = player.Name,
            Activity = activity,
            Since = DateTime.UtcNow,
        };

        seekers.Add(seeker);
        return seeker;
    }

    private static void StartListing(Player player, List<string> words)
    {
        var cant = WhyCantList(player);

        if (cant != null)
        {
            Tell(player, cant);
            return;
        }

        var fellowship = player.Fellowship;
        var activity = LfgParser.Parse(words, lfm: true, out var autoJoin, out var error);

        if (activity == null)
        {
            Tell(player, error);
            return;
        }

        if (activity.Dungeon != null && (player.Level ?? 1) < activity.Dungeon.Level)
        {
            Tell(player, $"You need to be level {activity.Dungeon.Level} for {activity.Dungeon.Name}.");
            return;
        }

        listings.RemoveAll(l => l.Fellowship == fellowship);

        var listing = new LfgListing
        {
            Fellowship = fellowship,
            Activity = activity,
            PostedBy = player.Name,
            AutoJoin = autoJoin,
            Since = DateTime.UtcNow,
        };

        listings.Add(listing);

        var what = activity.Describe(lfm: true);

        Tell(
            player,
            $"Your fellowship is looking for more for {what}. "
                + (autoJoin ? "Players looking for it join at once." : "You'll be asked about each player looking for it.")
                + " /lfmore stop to stop."
        );

        Post(player, "lfm " + what, $"LFM {what} ({Describe(fellowship)}) - to join: /lfgroup join {player.Name}");

        Match();
    }

    /// <summary>
    /// /lfgroup join: into the named player's fellowship, at once for an automatic lfm. For a manual one the player is looking for
    /// what it is, and its leader is asked about them now.
    /// </summary>
    /// <param name="name">Anyone in the fellowship, or whoever posted its lfm: a line in the LFG channel names whoever led it then,
    /// and stays there after leadership changes hands or they leave</param>
    private static void JoinListing(Player player, string name)
    {
        var cant = WhyCantSeek(player);

        if (cant != null)
        {
            Tell(player, cant);
            return;
        }

        var open = listings.Where(l => WhyClosed(l, out _) == null).ToList();

        var listing = open.FirstOrDefault(l => l.Fellowship.GetFellowshipMembers().Values.Any(m => SameName(m.Name, name)))
            ?? open.FirstOrDefault(l => SameName(l.PostedBy, name));

        if (listing == null)
        {
            Tell(player, $"{name}'s fellowship isn't looking for more. /lfmore list shows the ones that are.");
            return;
        }

        WhyClosed(listing, out var leader);

        var what = listing.Activity.Describe(lfm: true);

        if (!CanMeet(player, leader))
        {
            Tell(player, $"You can't join {leader.Name}'s fellowship.");
            return;
        }

        if (!Fits(player, listing))
        {
            var needed = listing.Activity.Dungeon?.Level;

            Tell(
                player,
                needed != null && (player.Level ?? 1) < needed
                    ? $"You need to be level {needed} for {listing.Activity.Dungeon.Name}."
                    : listing.Activity.Kind == LfgActivityKind.Leveling
                        ? $"{leader.Name}'s fellowship wouldn't share XP evenly with you in it."
                        : $"You don't fit {leader.Name}'s fellowship, which is looking for more for {what}."
            );
            return;
        }

        if (listing.AutoJoin)
        {
            Join(listing, leader, player);
            return;
        }

        if (listing.Asking != null)
        {
            Tell(player, $"{leader.Name} is answering someone else. Try again in a moment.");
            return;
        }

        var seeker = Seek(player, AsLfg(listing.Activity));

        if (!Ask(listing, leader, seeker, player))
        {
            Tell(player, $"{leader.Name} is answering something else. Try again in a moment.");
            return;
        }

        Tell(player, $"{leader.Name} has been asked whether you can join, for {what}. Until then, you're looking for a group for it.");
    }

    /// <summary>
    /// Whether two names are the same character's, ignoring case and the + a staff character's name shows with
    /// (Player.Name), so "join Sorc" finds +Sorc
    /// </summary>
    internal static bool SameName(string a, string b)
    {
        return string.Equals(
            (a ?? "").TrimStart('+').Trim(),
            (b ?? "").TrimStart('+').Trim(),
            StringComparison.OrdinalIgnoreCase
        );
    }

    private static void StopSeeking(Player player)
    {
        if (seekers.RemoveAll(s => s.Guid == player.Guid.Full) == 0)
        {
            Tell(player, "You weren't looking for a group.");
            return;
        }

        Tell(player, "You're no longer looking for a group.");
    }

    private static void StopListing(Player player)
    {
        var fellowship = player.Fellowship;

        if (fellowship == null || listings.All(l => l.Fellowship != fellowship))
        {
            Tell(player, "Your fellowship wasn't looking for more.");
            return;
        }

        if (fellowship.FellowshipLeaderGuid != player.Guid.Full)
        {
            Tell(player, "Only your fellowship's leader can stop it looking for more.");
            return;
        }

        listings.RemoveAll(l => l.Fellowship == fellowship);
        Tell(player, "Your fellowship is no longer looking for more.");
    }

    /// <summary>
    /// Posts a line in the LFG channel under the player's name, or tells them why it couldn't be
    /// </summary>
    /// <param name="what">What it's for ("lfg Green Mire Grave d10"): the same one waits longer than a different one</param>
    private static void Post(Player player, string what, string text)
    {
        var now = DateTime.UtcNow;
        var key = (player.Guid.Full, what);

        lastPosts.TryGetValue(key, out var lastSame);
        lastAnyPosts.TryGetValue(player.Guid.Full, out var lastAny);

        if (TooSoon(lastSame, now, PostInterval, out var wait))
        {
            Tell(
                player,
                $"It wasn't posted in the LFG channel: the same one can be posted once every {Minutes(PostInterval)}, "
                    + $"so again in {Minutes(wait)}."
            );
            return;
        }

        if (TooSoon(lastAny, now, AnyPostInterval, out wait))
        {
            Tell(
                player,
                $"It wasn't posted in the LFG channel: you can post there once every {Minutes(AnyPostInterval)}, "
                    + $"so again in {Minutes(wait)}."
            );
            return;
        }

        var refusal = TurbineChatHandler.PostToLfgChannel(player, text);

        if (refusal != null)
        {
            Tell(player, $"It wasn't posted in the LFG channel: {refusal}");
            return;
        }

        lastPosts[key] = now;
        lastAnyPosts[player.Guid.Full] = now;
    }

    /// <summary>
    /// Whether a post now would come sooner than interval after the last (none: DateTime.MinValue), and how long until one
    /// wouldn't. An interval of zero or less never is.
    /// </summary>
    internal static bool TooSoon(DateTime last, DateTime now, TimeSpan interval, out TimeSpan wait)
    {
        wait = last + interval - now;

        if (interval <= TimeSpan.Zero || last == DateTime.MinValue || wait <= TimeSpan.Zero)
        {
            wait = TimeSpan.Zero;
            return false;
        }

        return true;
    }

    /// <summary>
    /// "5 minutes", "a minute", "30 seconds"
    /// </summary>
    internal static string Minutes(TimeSpan span)
    {
        if (span.TotalSeconds < 60)
        {
            var seconds = Math.Max(1, (int)Math.Ceiling(span.TotalSeconds));
            return seconds == 1 ? "a second" : $"{seconds} seconds";
        }

        var minutes = (int)Math.Ceiling(span.TotalMinutes);
        return minutes == 1 ? "a minute" : $"{minutes} minutes";
    }

    /// <summary>
    /// "3/9, levels 20-24"
    /// </summary>
    private static string Describe(Fellowship fellowship)
    {
        var (count, low, high) = LevelRange(fellowship);
        var size = $"{count}/{Fellowship.MaxFellows}";

        if (count == 0)
        {
            return size;
        }

        return low == high ? $"{size}, level {low}" : $"{size}, levels {low}-{high}";
    }

    /// <summary>
    /// How many are in the fellowship, and the lowest and highest level
    /// </summary>
    private static (int Count, int Low, int High) LevelRange(Fellowship fellowship)
    {
        var levels = fellowship.GetFellowshipMembers().Values.Select(p => p.Level ?? 1).ToList();

        return levels.Count == 0 ? (0, 0, 0) : (levels.Count, levels.Min(), levels.Max());
    }

    /// <summary>
    /// What the player is looking for, or their fellowship is
    /// </summary>
    private static string Status(Player player)
    {
        var seeker = seekers.FirstOrDefault(s => s.Guid == player.Guid.Full);

        if (seeker != null)
        {
            return $"You're looking for a group for {seeker.Activity.Describe(lfm: false)} ({Waited(seeker.Since)}).";
        }

        var listing = player.Fellowship == null ? null : listings.FirstOrDefault(l => l.Fellowship == player.Fellowship);

        if (listing != null)
        {
            return $"Your fellowship is looking for more for {listing.Activity.Describe(lfm: true)} ({Waited(listing.Since)}"
                + (listing.AutoJoin ? ")." : ", manual).");
        }

        return "You aren't looking for anything now.";
    }

    private static string ListListings()
    {
        var open = listings.Where(l => WhyClosed(l, out _) == null).OrderBy(l => l.Since).ToList();

        if (open.Count == 0)
        {
            return "No fellowship is looking for more now. /lfgroup <activity> waits for one.";
        }

        var text = new StringBuilder("Fellowships looking for more:");

        foreach (var listing in open)
        {
            WhyClosed(listing, out var leader);

            text.Append(
                $"\n{leader.Name} ({Describe(listing.Fellowship)}): {listing.Activity.Describe(lfm: true)}"
                    + $" - /lfgroup join {leader.Name}"
                    + (listing.AutoJoin ? "" : " (the leader is asked)")
            );
        }

        return text.ToString();
    }

    private static string ListSeekers()
    {
        var online = OnlineSeekers();

        if (online.Count == 0)
        {
            return "Nobody is looking for a group now. /lfmore <activity> waits for them.";
        }

        var text = new StringBuilder("Players looking for a group:");

        foreach (var (seeker, player) in online)
        {
            text.Append(
                $"\n{player.Name} (level {player.Level ?? 1}): {seeker.Activity.Describe(lfm: false)}, {Waited(seeker.Since)}"
                    + $" - /lfmore invite {player.Name}"
            );
        }

        return text.ToString();
    }

    /// <summary>
    /// The lfgs whose players are online and still without a fellowship, the longest waiting first
    /// </summary>
    private static List<(LfgSeeker Seeker, Player Player)> OnlineSeekers()
    {
        return seekers
            .OrderBy(s => s.Since)
            .Select(s => (Seeker: s, Player: PlayerManager.GetOnlinePlayer(s.Guid)))
            .Where(s => s.Player != null && s.Player.Fellowship == null)
            .ToList();
    }

    /// <summary>
    /// "for 5 minutes"
    /// </summary>
    private static string Waited(DateTime since)
    {
        var minutes = (int)(DateTime.UtcNow - since).TotalMinutes;

        return minutes < 1 ? "just now" : minutes == 1 ? "for a minute" : $"for {minutes} minutes";
    }
}
