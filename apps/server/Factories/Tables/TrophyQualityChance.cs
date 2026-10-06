using System;
using System.Collections.Generic;
using ACE.Server.Factories.Entity;

namespace ACE.Server.Factories.Tables;

/// <summary>
/// The quality (1-10) a trophy rolls per tier. Trophies used to share the item workmanship table, and keep its
/// original odds now that item workmanship has its own per-tier ranges.
/// </summary>
public static class TrophyQualityChance
{
    private static ChanceTable<int> T1_Chances = [(1, 1.0f)];

    private static ChanceTable<int> T2_Chances = [(1, 0.9f), (2, 0.1f)];

    private static ChanceTable<int> T3_Chances = [(1, 0.4f), (2, 0.5f), (3, 0.1f)];

    private static ChanceTable<int> T4_Chances = [(2, 0.4f), (3, 0.5f), (4, 0.1f)];

    private static ChanceTable<int> T5_Chances = [(3, 0.4f), (4, 0.5f), (5, 0.1f)];

    private static ChanceTable<int> T6_Chances = [(4, 0.4f), (5, 0.59f), (6, 0.009f), (7, 0.001f)];

    private static ChanceTable<int> T7_Chances = [(5, 0.4f), (6, 0.59f), (7, 0.009f), (8, 0.001f)];

    private static ChanceTable<int> T8_Chances = [(6, 0.4f), (7, 0.5f), (8, 0.09f), (9, 0.009f), (10, 0.001f)];

    private static readonly List<ChanceTable<int>> qualityChances =
    [
        T1_Chances,
        T2_Chances,
        T3_Chances,
        T4_Chances,
        T5_Chances,
        T6_Chances,
        T7_Chances,
        T8_Chances
    ];

    /// <summary>
    /// Rolls a 1-10 quality for a trophy of this tier (1-8)
    /// </summary>
    public static int Roll(int tier)
    {
        tier = Math.Clamp(tier, 1, 8);

        return qualityChances[tier - 1].Roll();
    }
}
