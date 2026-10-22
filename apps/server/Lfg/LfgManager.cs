using System;
using System.Collections.Generic;
using System.Linq;
using ACE.Entity.Enum;
using ACE.Server.Arena;
using ACE.Server.Entity;
using ACE.Server.Managers;
using ACE.Server.Network.Handlers;
using ACE.Server.WorldObjects;
using Serilog;

namespace ACE.Server.Lfg;

/// <summary>
/// Looking for group: players looking for a group (/lfgroup) and fellowships looking for more (/lfmore). A player
/// who fits a fellowship's lfm joins it at once, or once its leader says yes. Each new lfg or lfm is posted in the LFG channel, under
/// the player's name, in a form everyone reads the same way. See docs/lfg.md.
///
/// Nothing is saved: an lfg lasts until the player stops it, joins a fellowship or logs out, and an lfm until the leader stops it,
/// or the fellowship is full, locked (as it is once a capstone starts) or disbanded. Everything in here is changed while holding one
/// lock. Fellowships are only changed from the world thread (commands and a yes come from a client's message, and Tick from
/// WorldManager), the same as the Fellowship panel's own buttons.
/// </summary>
public static partial class LfgManager
{
    private static readonly ILogger _log = Log.ForContext(typeof(LfgManager));

    private static readonly object sync = new();

    private static readonly List<LfgSeeker> seekers = new();

    private static readonly List<LfgListing> listings = new();

    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(1);

    private static DateTime nextTick;

    /// <summary>
    /// When each player last posted a line in the LFG channel for each thing they looked for ("lfg Green Mire Grave d10"), so they
    /// can't fill it with the same one (lfg_post_interval_seconds), and when they last posted any (lfg_post_any_interval_seconds),
    /// so switching between them can't either. Their lfg or lfm still changes as often as they like: only the line waits.
    /// </summary>
    private static readonly Dictionary<(uint Guid, string What), DateTime> lastPosts = new();

    private static readonly Dictionary<uint, DateTime> lastAnyPosts = new();

    private static TimeSpan PostInterval => Seconds("lfg_post_interval_seconds");

    private static TimeSpan AnyPostInterval => Seconds("lfg_post_any_interval_seconds");

    private static TimeSpan Seconds(string property) => TimeSpan.FromSeconds(PropertyManager.GetLong(property).Item);

    public static bool Enabled => PropertyManager.GetBool("lfg_matching_enabled").Item;

    /// <summary>
    /// Called every world tick. Drops what has ended, and puts players into fellowships that fit them.
    /// </summary>
    public static void Tick()
    {
        var now = DateTime.UtcNow;

        if (now < nextTick)
        {
            return;
        }

        nextTick = now + TickInterval;

        lock (sync)
        {
            if (seekers.Count == 0 && listings.Count == 0)
            {
                return;
            }

            if (!Enabled)
            {
                seekers.Clear();
                listings.Clear();
                return;
            }

            Tidy();
            Match();
        }
    }

    /// <summary>
    /// Drops the lfgs of players who have logged out or found a fellowship, and the lfms of fellowships that are full, locked or gone
    /// </summary>
    private static void Tidy()
    {
        var now = DateTime.UtcNow;

        foreach (var key in lastPosts.Where(p => !TooSoon(p.Value, now, PostInterval, out _)).Select(p => p.Key).ToList())
        {
            lastPosts.Remove(key);
        }

        foreach (var guid in lastAnyPosts.Where(p => !TooSoon(p.Value, now, AnyPostInterval, out _)).Select(p => p.Key).ToList())
        {
            lastAnyPosts.Remove(guid);
        }

        foreach (var seeker in seekers.ToList())
        {
            var player = PlayerManager.GetOnlinePlayer(seeker.Guid);

            if (player == null)
            {
                seekers.Remove(seeker);
            }
            else if (player.Fellowship != null)
            {
                seekers.Remove(seeker);
                Tell(player, "You're in a fellowship now, so you're no longer looking for a group.");
            }
        }

        foreach (var listing in listings.ToList())
        {
            var why = WhyClosed(listing, out var leader);

            if (why != null)
            {
                listings.Remove(listing);
                if (leader != null)
                {
                    Tell(leader, $"Your fellowship is no longer looking for more: {why}.");
                }
            }
        }
    }

    /// <summary>
    /// Why an lfm has ended, or null if it hasn't
    /// </summary>
    /// <param name="leader">The fellowship's leader, if they're online and still in it</param>
    private static string WhyClosed(LfgListing listing, out Player leader)
    {
        var fellowship = listing.Fellowship;

        leader = PlayerManager.GetOnlinePlayer(fellowship.FellowshipLeaderGuid);

        if (leader == null || leader.Fellowship != fellowship)
        {
            leader = null;
            return "it has disbanded";
        }

        if (fellowship.IsLocked)
        {
            return "it is locked";
        }

        if (fellowship.FellowshipMembers.Count >= Fellowship.MaxFellows)
        {
            return "it is full";
        }

        return null;
    }

    /// <summary>
    /// Puts players into the fellowships that fit them, the longest waiting first on both sides
    /// </summary>
    private static void Match()
    {
        var evenShareLevel = PropertyManager.GetLong("fellowship_even_share_level").Item;

        foreach (var listing in listings.OrderBy(l => l.Since).ToList())
        {
            if (listing.Asking != null || WhyClosed(listing, out var leader) != null)
            {
                continue;
            }

            foreach (var seeker in seekers.OrderBy(s => s.Since).ToList())
            {
                if (seeker.AskedBy != null || seeker.Declined.Contains(listing))
                {
                    continue;
                }

                var player = PlayerManager.GetOnlinePlayer(seeker.Guid);

                if (player == null || player.Fellowship != null || !CanMeet(player, leader))
                {
                    continue;
                }

                var fellows = listing.Fellowship.GetFellowshipMembers().Values.Select(Levels).ToList();
                var (level, shrouded) = Levels(player);

                if (!LfgMatching.Matches(seeker.Activity, level, shrouded, listing.Activity, fellows, evenShareLevel))
                {
                    continue;
                }

                if (!listing.AutoJoin)
                {
                    // one question at a time: the next player waits for this answer
                    if (Ask(listing, leader, seeker, player))
                    {
                        break;
                    }

                    continue;
                }

                Join(listing, leader, player);

                if (WhyClosed(listing, out _) != null)
                {
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Whether this player would be put in this lfm's fellowship if they asked for what it's looking for (/lfgroup join, and the
    /// plugin's "fits you"): the words its line tells people to type, read as an lfg, so the two can't disagree
    /// </summary>
    private static bool Fits(Player player, LfgListing listing)
    {
        var seeking = AsLfg(listing.Activity);

        if (seeking == null)
        {
            return false;
        }

        var (level, shrouded) = Levels(player);
        var fellows = listing.Fellowship.GetFellowshipMembers().Values.Select(Levels).ToList();
        var evenShareLevel = PropertyManager.GetLong("fellowship_even_share_level").Item;

        return LfgMatching.Matches(seeking, level, shrouded, listing.Activity, fellows, evenShareLevel);
    }

    /// <summary>
    /// What someone joining this lfm is taken to be looking for
    /// </summary>
    private static LfgActivity AsLfg(LfgActivity lfm)
    {
        return LfgParser.Parse(lfm.JoinWords().Split(' '), lfm: false, out _, out _);
    }

    /// <summary>
    /// Command replies go out as System chat, which the plugin keeps as a command's reply (PlayerFeed.SendCommand)
    /// </summary>
    private static void Tell(Player player, string text)
    {
        player.SendMessage(text, ChatMessageType.System);
    }

    /// <summary>
    /// Whether these two can be put in a fellowship together: neither has squelched the other, and neither is in the arena
    /// </summary>
    private static bool CanMeet(Player player, Player leader)
    {
        if (ArenaManager.WhyCantChat(player) != null || ArenaManager.WhyCantChat(leader) != null)
        {
            return false;
        }

        return !player.SquelchManager.Squelches.Contains(leader, ChatMessageType.Tell)
            && !leader.SquelchManager.Squelches.Contains(player, ChatMessageType.Tell);
    }

    private static (int Level, bool Shrouded) Levels(Player player)
    {
        return (player.Level ?? 1, player.EnchantmentManager.HasSpell((uint)SpellId.Shrouded));
    }

    /// <summary>
    /// Asks a manual lfm's leader whether this player can join. False if the leader has a yes/no question open already.
    /// </summary>
    private static bool Ask(LfgListing listing, Player leader, LfgSeeker seeker, Player player)
    {
        var confirmation = new Confirmation_Lfg(leader.Guid, yes => OnAnswer(listing, seeker, yes));

        var question =
            $"{player.Name} (level {player.Level ?? 1}) is looking for a group for {seeker.Activity.Describe(lfm: false)}. "
            + "Add them to your fellowship?";

        if (!leader.ConfirmationManager.EnqueueSend(confirmation, question))
        {
            return false;
        }

        listing.Asking = seeker;
        seeker.AskedBy = listing;
        return true;
    }

    /// <summary>
    /// A manual lfm's leader answered (or didn't in time, which comes on the leader's landblock thread, so a no changes nothing but
    /// this). A yes comes from the client's answer, on the world thread.
    /// </summary>
    private static void OnAnswer(LfgListing listing, LfgSeeker seeker, bool yes)
    {
        lock (sync)
        {
            if (listing.Asking == seeker)
            {
                listing.Asking = null;
            }

            if (seeker.AskedBy == listing)
            {
                seeker.AskedBy = null;
            }

            if (!yes)
            {
                seeker.Declined.Add(listing);
                return;
            }

            var player = PlayerManager.GetOnlinePlayer(seeker.Guid);
            var why = WhyClosed(listing, out var leader);

            if (leader == null)
            {
                return;
            }

            if (!listings.Contains(listing) || why != null)
            {
                Tell(leader, $"{seeker.Name} can't join: your fellowship is no longer looking for more.");
                return;
            }

            if (!seekers.Contains(seeker) || player == null || player.Fellowship != null)
            {
                Tell(leader, $"{seeker.Name} is no longer looking for a group.");
                return;
            }

            Join(listing, leader, player);
        }
    }

    /// <summary>
    /// Puts the player in the fellowship, as if the leader had recruited them and they'd said yes: asking for the group was their yes
    /// </summary>
    private static void Join(LfgListing listing, Player leader, Player player)
    {
        var fellowship = listing.Fellowship;

        fellowship.AddConfirmedMember(leader, player, true);

        if (player.Fellowship != fellowship)
        {
            return;
        }

        seekers.RemoveAll(s => s.Guid == player.Guid.Full);

        var what = listing.Activity.Describe(lfm: true);

        Tell(player, $"You've joined {leader.Name}'s fellowship for {what}.");
        fellowship.BroadcastToFellow($"{player.Name} (level {player.Level ?? 1}) joined through LFG.");

        _log.Information(
            "[LFG] {Player} joined {Leader}'s fellowship for {Activity}",
            player.Name,
            leader.Name,
            what
        );
    }
}
