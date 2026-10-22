using System;
using System.Linq;
using ACE.Server.Entity;
using ACE.Server.Entity.PlayerUi;
using ACE.Server.Managers;
using ACE.Server.WorldObjects;

namespace ACE.Server.Lfg;

/// <summary>
/// What the Daralet plugin's Find a group tab shows (@playerui lfg): what the player or their fellowship is looking for, every
/// lfm and lfg, and the capstones to pick from. The lists name other players, unlike the rest of @playerui, but only with what
/// their own lfg and lfm lines already told the LFG channel, and what /lfgroup list and /lfmore list tell anyone.
/// </summary>
public static partial class LfgManager
{
    public static PlayerUiLfg GetPlayerUiReport(Player player)
    {
        var now = DateTime.UtcNow;
        var guid = player.Guid.Full;

        var capstones = LfgCapstones.All.Select(c => new PlayerUiLfgCapstone(c.Name, c.Short, c.Level)).ToList();

        lock (sync)
        {
            var mine = seekers.FirstOrDefault(s => s.Guid == guid);
            var fellowship = player.Fellowship;
            var ours = fellowship == null ? null : listings.FirstOrDefault(l => l.Fellowship == fellowship);

            var listed = listings
                .Where(l => WhyClosed(l, out _) == null)
                .OrderBy(l => l.Since)
                .Select(l =>
                {
                    WhyClosed(l, out var leader);
                    var (count, low, high) = LevelRange(l.Fellowship);

                    return new PlayerUiLfgListing(
                        leader.Name,
                        l.Activity.Describe(lfm: true),
                        count,
                        Fellowship.MaxFellows,
                        low,
                        high,
                        l.AutoJoin,
                        Seconds(now - l.Since),
                        fellowship == null && CanMeet(player, leader) && Fits(player, l),
                        l == ours
                    );
                })
                .ToList();

            var evenShareLevel = PropertyManager.GetLong("fellowship_even_share_level").Item;
            var fellows = ours?.Fellowship.GetFellowshipMembers().Values.Select(Levels).ToList();

            var looking = OnlineSeekers()
                .Select(s =>
                {
                    var (level, shrouded) = Levels(s.Player);
                    var fits = ours != null
                        && LfgMatching.Matches(s.Seeker.Activity, level, shrouded, ours.Activity, fellows, evenShareLevel);

                    return new PlayerUiLfgSeeker(
                        s.Player.Name,
                        level,
                        s.Seeker.Activity.Describe(lfm: false),
                        Seconds(now - s.Seeker.Since),
                        fits,
                        s.Seeker.Guid == guid
                    );
                })
                .ToList();

            string askedBy = null;

            if (mine?.AskedBy != null && WhyClosed(mine.AskedBy, out var asker) == null)
            {
                askedBy = asker.Name;
            }

            return new PlayerUiLfg(
                Enabled,
                mine != null ? "seeking" : ours != null ? "listing" : "none",
                mine?.Activity.Describe(lfm: false) ?? ours?.Activity.Describe(lfm: true),
                mine != null ? Seconds(now - mine.Since) : ours != null ? Seconds(now - ours.Since) : 0,
                ours?.AutoJoin ?? false,
                askedBy,
                WhyCantSeek(player),
                WhyCantList(player),
                fellowship != null && fellowship.FellowshipLeaderGuid == guid,
                listed,
                looking,
                capstones
            );
        }
    }

    private static double Seconds(TimeSpan span)
    {
        return Math.Max(0, Math.Floor(span.TotalSeconds));
    }
}
