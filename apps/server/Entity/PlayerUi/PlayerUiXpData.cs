using System;
using System.Collections.Generic;
using ACE.DatLoader;
using ACE.Server.WorldObjects;

namespace ACE.Server.Entity.PlayerUi;

/// <summary>
/// @playerui xp: the player's level and the total XP their level starts and ends at, for the plugin's XP bar, with their
/// rested experience. The client knows its total XP and level but not the XP table, which is the server's (portal.dat's,
/// and max_level).
/// </summary>
public static class PlayerUiXpData
{
    public static PlayerUiXp GetXp(Player player)
    {
        var level = player.Level ?? 1;
        var (levelXp, nextLevelXp, maxLevel) = Bounds(
            DatManager.PortalDat.XpTable.CharacterLevelXPList,
            level,
            Player.GetMaxLevel()
        );

        return new PlayerUiXp(
            level,
            player.TotalExperience ?? 0,
            levelXp,
            nextLevelXp,
            maxLevel,
            player.GetPlayerUiRested()
        );
    }

    /// <summary>
    /// The total XP a level starts and ends at, from the XP table (the total for each level, by level). At the highest
    /// level, or past the table's end, both are the level's start, and the bar is full.
    /// </summary>
    public static (long LevelXp, long NextLevelXp, bool MaxLevel) Bounds(IList<ulong> table, int level, uint maxLevel)
    {
        if (table == null || table.Count == 0)
        {
            return (0, 0, true);
        }

        var at = Math.Clamp(level, 0, table.Count - 1);
        var levelXp = (long)table[at];

        if (level >= maxLevel || at + 1 >= table.Count)
        {
            return (levelXp, levelXp, true);
        }

        return (levelXp, (long)table[at + 1], false);
    }
}
