using System;
using System.Collections.Generic;
using System.Linq;

namespace ACE.Server.Lfg;

/// <summary>
/// Whether a player looking for a group (lfg) fits a fellowship looking for more (lfm). Kept apart from players and fellowships
/// so it can be tested.
/// </summary>
public static class LfgMatching
{
    /// <param name="seeking">What the player asked for</param>
    /// <param name="seekerLevel">Their level</param>
    /// <param name="seekerShrouded">Whether they're Shrouded (for leveling: see SharesEvenly)</param>
    /// <param name="offered">What the fellowship asked for</param>
    /// <param name="fellows">The fellowship's members now</param>
    /// <param name="evenShareLevel">fellowship_even_share_level</param>
    public static bool Matches(
        LfgActivity seeking,
        int seekerLevel,
        bool seekerShrouded,
        LfgActivity offered,
        IReadOnlyCollection<(int Level, bool Shrouded)> fellows,
        long evenShareLevel
    )
    {
        if (seeking.Kind != offered.Kind)
        {
            return false;
        }

        switch (seeking.Kind)
        {
            case LfgActivityKind.Capstone:
                return CapstoneMatches(seeking, seekerLevel, offered);
            case LfgActivityKind.Leveling:
                return SharesEvenly(fellows.Append((seekerLevel, seekerShrouded)), evenShareLevel);
            case LfgActivityKind.Quest:
                // "quest" on its own is any quest, on either side
                return seeking.Words.Count == 0 || offered.Words.Count == 0 || WordsMatch(seeking.Words, offered.Words);
            default:
                return WordsMatch(seeking.Words, offered.Words);
        }
    }

    private static bool CapstoneMatches(LfgActivity seeking, int seekerLevel, LfgActivity offered)
    {
        // someone after one capstone isn't put in a fellowship that hasn't said which
        if (seeking.Dungeon != null && offered.Dungeon != seeking.Dungeon)
        {
            return false;
        }

        var dungeonLevel = offered.Dungeon?.Level ?? LfgCapstones.LowestLevel;

        if (seekerLevel < dungeonLevel)
        {
            return false;
        }

        if (offered.Dungeon != null && seeking.MaxLevel != null && offered.Dungeon.Level > seeking.MaxLevel)
        {
            return false;
        }

        var difficulty = offered.Difficulty;

        return (seeking.DifficultyMin == null || difficulty >= seeking.DifficultyMin)
            && (seeking.DifficultyMax == null || difficulty <= seeking.DifficultyMax);
    }

    /// <summary>
    /// The same words either way round: everything one asked for is in what the other did ("olthoi" and "olthoi north")
    /// </summary>
    private static bool WordsMatch(IReadOnlyList<string> a, IReadOnlyList<string> b)
    {
        if (a.Count == 0 || b.Count == 0)
        {
            return false;
        }

        return a.All(b.Contains) || b.All(a.Contains);
    }

    /// <summary>
    /// Whether a fellowship of these levels shares XP evenly, by Fellowship.CalculateXPSharing's rules: everyone at
    /// fellowship_even_share_level or above, or everyone within 5 levels of the lowest, a Shrouded player up to 25 levels above it
    /// counting as the lowest.
    /// </summary>
    public static bool SharesEvenly(IEnumerable<(int Level, bool Shrouded)> fellows, long evenShareLevel)
    {
        var list = fellows.ToList();

        if (list.Count == 0)
        {
            return true;
        }

        if (list.All(f => f.Level >= evenShareLevel))
        {
            return true;
        }

        var lowest = list.Min(f => f.Level);

        var maxDifference = list.Max(f =>
            f.Shrouded && f.Level - lowest <= 25 ? 0 : Math.Abs(f.Level - lowest)
        );

        return maxDifference <= 5;
    }
}
