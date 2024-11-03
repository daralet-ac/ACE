using System;
using System.Collections.Generic;
using ACE.Server.Factories.Entity;

namespace ACE.Server.Factories.Tables;

public static class WorkmanshipChance
{
    private static ChanceTable<int> T1_Chances = [(1, 1.0f)];

    private static ChanceTable<int> T2_Chances = [(1, 0.7f), (2, 0.29f), (3, 0.009f), (4, 0.001f)];

    private static ChanceTable<int> T3_Chances = [(1, 0.6f), (2, 0.3f), (3, 0.09f), (4, 0.009f), (5, 0.001f)];

    private static ChanceTable<int> T4_Chances = [(2, 0.6f), (3, 0.3f), (4, 0.09f), (5, 0.009f), (6, 0.001f)];

    private static ChanceTable<int> T5_Chances = [(3, 0.6f), (4, 0.3f), (5, 0.09f), (6, 0.009f), (7, 0.001f)];

    private static ChanceTable<int> T6_Chances = [(4, 0.6f), (5, 0.3f), (6, 0.09f), (7, 0.009f), (8, 0.001f)];

    private static ChanceTable<int> T7_Chances = [(5, 0.6f), (6, 0.3f), (7, 0.09f), (8, 0.009f), (9, 0.001f)];

    private static ChanceTable<int> T8_Chances = [(6, 0.6f), (7, 0.3f), (8, 0.09f), (9, 0.009f), (10, 0.001f)];

    private static readonly List<ChanceTable<int>> workmanshipChances =
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
    /// Rolls for a 1-10 workmanship for an item
    /// </summary>
    public static int Roll(int tier, float qualityMod = 0.0f, int cantripLevel = 0)
    {
        // https://asheron.fandom.com/wiki/Quality_Flag - The Quality Flag also reduces the maximum worksmanship of items in the tier by 2. For example, in Wealth 6, the worksmanship range is 4 - 10. In Wealth 6(Quality), the range is 4 - 8.
        // From the above combined with the fact that the non-zero loot_quality_mod values in the database ranges from 0.2 to 0.25 we can deduce that it's an inverted quality mod roll, capping the top instead of the bottom.

        // todo: add t7 / t8
        tier = Math.Clamp(tier, 1, 8);

        var workmanshipChance = workmanshipChances[tier - 1];

        var baseWorkmanship = workmanshipChance.Roll(qualityMod, true);

        var cantripBonus = 0;
        if (cantripLevel > 0)
        {
            cantripBonus = Math.Clamp(cantripLevel - 2, 1, 10);
        }

        return baseWorkmanship + cantripBonus;
    }

    /// <summary>
    /// Returns the workmanship modifier for an item
    /// </summary>
    public static float GetModifier(int? workmanship)
    {
        var modifier = 1.0f;

        if (workmanship != null)
        {
            modifier += workmanship.Value / 9.0f;
        }

        return modifier;
    }
}
