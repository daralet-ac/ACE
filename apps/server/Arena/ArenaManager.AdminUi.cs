using System;
using System.Collections.Generic;
using System.Linq;
using ACE.Server.Entity.AdminUi;
using ACE.Server.Managers;

namespace ACE.Server.Arena;

/// <summary>
/// What the admin plugin's Arena tab shows: the duels going on, the queue, and how the latest duels ended
/// </summary>
public static partial class ArenaManager
{
    /// <summary>
    /// How many finished duels are remembered for the Arena tab. Only in memory, so the list starts empty when the server does.
    /// </summary>
    private const int RecentResultsKept = 25;

    private sealed record ArenaResult(
        int Id,
        ArenaMatchKind Kind,
        int SideSize,
        bool Rated,
        bool Scaled,
        string Map,
        DateTime EndedAt,
        DateTime? FightStartedAt,
        int? WinningSide,
        bool CalledOff,
        List<List<string>> Sides
    );

    /// <summary>
    /// Newest first. Only used while holding sync, like the matches themselves.
    /// </summary>
    private static readonly List<ArenaResult> recentResults = new List<ArenaResult>();

    /// <summary>
    /// Called (holding sync) when a duel ends, won, drawn or called off
    /// </summary>
    private static void RememberResult(ArenaMatch match)
    {
        recentResults.Insert(
            0,
            new ArenaResult(
                match.Id,
                match.Kind,
                match.SideSize,
                match.Rated,
                match.Scaled,
                match.Map?.Name,
                match.EndedAt,
                match.FightStartedAt == default ? null : match.FightStartedAt,
                match.WinningSide,
                match.CalledOff,
                Sides(match).Select(side => side.Select(f => f.Name).ToList()).ToList()
            )
        );

        if (recentResults.Count > RecentResultsKept)
        {
            recentResults.RemoveRange(RecentResultsKept, recentResults.Count - RecentResultsKept);
        }
    }

    private static IEnumerable<List<ArenaFighter>> Sides(ArenaMatch match)
    {
        return match.Fighters.GroupBy(f => f.Side).OrderBy(g => g.Key).Select(g => g.ToList());
    }

    public static AdminUiArenaReport GetAdminUiReport()
    {
        var now = UtcNow();

        lock (sync)
        {
            var active = matches
                .Where(m => m.State != ArenaMatchState.Ended)
                .OrderBy(m => m.Id)
                .Select(m => new AdminUiArenaMatch(
                    m.Id,
                    m.Kind.ToString(),
                    m.State.ToString(),
                    m.Map?.Name,
                    m.Rated,
                    m.Scaled,
                    m.SideSize,
                    m.Instance?.Id,
                    m.State == ArenaMatchState.Fighting ? (now - m.FightStartedAt).TotalSeconds : null,
                    m.Deadline > now ? (m.Deadline - now).TotalSeconds : null,
                    Sides(m).Select(side => side.Select(f => DescribeFighter(m, f)).ToList()).ToList(),
                    m.Spectators.Where(s => !s.Returned).Select(s => s.Name).ToList()
                ))
                .ToList();

            var queued = queue.Entries.Select(e => $"{e.Describe()} ({DescribeDuel(e.Scaled, e.Rated)})").ToList();

            var recent = recentResults
                .Select(r => new AdminUiArenaResult(
                    r.Id,
                    r.Kind.ToString(),
                    r.Map,
                    r.Rated,
                    r.Scaled,
                    r.SideSize,
                    (now - r.EndedAt).TotalSeconds,
                    r.FightStartedAt.HasValue ? (r.EndedAt - r.FightStartedAt.Value).TotalSeconds : null,
                    r.WinningSide,
                    r.CalledOff,
                    r.Sides
                ))
                .ToList();

            return new AdminUiArenaReport(active, queued, recent);
        }
    }

    private static AdminUiArenaFighter DescribeFighter(ArenaMatch match, ArenaFighter fighter)
    {
        var player = PlayerManager.GetOnlinePlayer(fighter.Guid);

        string status;

        if (fighter.Eliminated)
        {
            status = "defeated";
        }
        else if (fighter.Returned)
        {
            status = "left";
        }
        else if (match.State == ArenaMatchState.Accepting)
        {
            status = fighter.Accepted ? "accepted" : "deciding";
        }
        else if (!fighter.Arrived)
        {
            status = "on the way";
        }
        else
        {
            status = "fighting";
        }

        return new AdminUiArenaFighter(
            fighter.Guid,
            fighter.Name,
            fighter.Level,
            status,
            player != null ? AdminUiSnapshots.GetFellow(player) : null
        );
    }
}
