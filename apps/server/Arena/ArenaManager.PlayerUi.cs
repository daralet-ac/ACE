using System;
using System.Linq;
using ACE.Server.Entity.PlayerUi;
using ACE.Server.WorldObjects;

namespace ACE.Server.Arena;

/// <summary>
/// What the Daralet plugin's Arena tab shows (@playerui arena): the player's ratings and where they stand, the duels
/// they could watch, and how the latest duels ended. Only what /arena stats, /arena watch and the duel announcements already
/// tell anyone.
/// </summary>
public static partial class ArenaManager
{
    private const int PlayerUiRecentShown = 10;

    public static PlayerUiArena GetPlayerUiReport(Player player)
    {
        var boards = ArenaBoards
            .All(player)
            .Select(b => new PlayerUiArenaBoard(b.Board.Name, b.Board.Size, b.Board.Scaled, b.Standing.Rating, b.Standing.Wins, b.Standing.Losses, b.Standing.Draws))
            .ToList();

        var maps = ArenaMaps.Enabled.Select(m => m.Description).ToList();
        var status = Status(player);
        var now = UtcNow();
        var guid = player.Guid.Full;

        lock (sync)
        {
            var place = queue.PositionOf(guid);

            var duels = matches
                .Where(IsWatchable)
                .OrderBy(m => m.Id)
                .Select(m => new PlayerUiArenaDuel(m.Id, DescribeSides(m), m.SideSize, m.Scaled, m.Rated, m.Map?.Name, m.State.ToString()))
                .ToList();

            var recent = recentResults
                .Take(PlayerUiRecentShown)
                .Select(r => new PlayerUiArenaResult(
                    r.Id,
                    string.Join(" vs ", r.Sides.Select(side => string.Join(", ", side))),
                    r.SideSize,
                    r.Scaled,
                    r.Rated,
                    r.Map,
                    !r.CalledOff && r.WinningSide is int side && side >= 0 && side < r.Sides.Count ? string.Join(", ", r.Sides[side]) : null,
                    r.CalledOff,
                    Math.Max(0, Math.Floor((now - r.EndedAt).TotalSeconds))
                ))
                .ToList();

            return new PlayerUiArena(
                Enabled,
                MinimumLevel,
                ArenaElo.StartingRating,
                PlayerUiState(guid, place),
                status,
                place,
                queue.Count,
                boards,
                duels,
                recent,
                maps
            );
        }
    }

    /// <summary>
    /// The player's place in the arena in one word, for the plugin to choose its buttons by. Called holding sync.
    /// </summary>
    private static string PlayerUiState(uint guid, int queuePlace)
    {
        if (!Enabled)
        {
            return "off";
        }

        if (queuePlace > 0)
        {
            return "queued";
        }

        if (FindWatched(guid) != null)
        {
            return "watching";
        }

        var match = FindMatch(guid);

        if (match == null)
        {
            return "idle";
        }

        var fighter = match.Get(guid);

        if (fighter.Spectating && !fighter.Returned)
        {
            return "defeated";
        }

        return match.State switch
        {
            ArenaMatchState.Accepting => "accepting",
            ArenaMatchState.Fighting => "fighting",
            _ => "starting",
        };
    }

    private static string DescribeSides(ArenaMatch match)
    {
        return string.Join(" vs ", Sides(match).Select(side => string.Join(", ", side.Select(f => f.Name))));
    }
}
