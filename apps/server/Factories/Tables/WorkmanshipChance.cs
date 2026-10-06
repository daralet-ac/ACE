using System;
using ACE.Common;

namespace ACE.Server.Factories.Tables;

public static class WorkmanshipChance
{
    // The power score (an item's rolls against the best its type can roll, 0 to 1) each workmanship from 2 to 10
    // starts at. A higher score never gets a lower workmanship, so a stronger item never shows less than a weaker one.
    // Stats grow much faster at the top tiers than the bottom, so the steps are placed to land each tier's typical
    // drop on that tier (t0 1, t1 1-2, t2 2-3, t3 3-4, t4 4-5, t5 5-7, t6 6-8, t7 7-10) rather than spaced evenly.
    private static readonly double[] PowerScoreThresholds = [0.165, 0.22, 0.28, 0.35, 0.45, 0.575, 0.76, 0.83, 0.89];

    // The workmanship each tier (t0 - t7) rolls for items with no stats to judge them by.
    private static readonly int[] MinWorkmanshipPerTier = [1, 1, 2, 3, 4, 5, 6, 7];
    private static readonly int[] MaxWorkmanshipPerTier = [1, 2, 3, 4, 5, 7, 8, 10];

    // How good such an item's roll has to be, 0 to 1, for each point above its tier's minimum
    private static readonly double[] BonusRollThresholds = [0.4, 0.6, 0.8];

    /// <summary>
    /// Returns the workmanship for an item whose rolls scored powerScore against the best its type can roll
    /// </summary>
    public static int FromPowerScore(double powerScore)
    {
        var workmanship = 1;

        foreach (var threshold in PowerScoreThresholds)
        {
            if (powerScore >= threshold)
            {
                workmanship++;
            }
        }

        return workmanship;
    }

    /// <summary>
    /// Rolls a workmanship for an item of this tier (1-8) that has no stat rolls of its own to judge it by
    /// </summary>
    public static int Roll(int tier, float qualityMod = 0.0f, int cantripLevel = 0)
    {
        // Loot quality raises the floor of the roll, the same as it does for the stat rolls on gear,
        // and a higher spell level on the item nudges it up a little further.
        var lootQuality = Math.Max(qualityMod, 0.0f) + cantripLevel * 0.05f;

        // two rolls averaged, so most land on the tier's minimum and about 1 in 3 reach +1, 1 in 10 +2 and 1 in 40 +3
        var roll = (RollDiminished(lootQuality) + RollDiminished(lootQuality)) / 2;

        var tierIndex = Math.Clamp(tier, 1, 8) - 1;

        var workmanship = MinWorkmanshipPerTier[tierIndex];

        foreach (var threshold in BonusRollThresholds)
        {
            if (roll >= threshold)
            {
                workmanship++;
            }
        }

        return Math.Min(workmanship, MaxWorkmanshipPerTier[tierIndex]);
    }

    private static double RollDiminished(float lootQuality)
    {
        var minimum = (float)(1 - Math.Exp(-lootQuality));
        var roll = ThreadSafeRandom.Next(minimum, 1.0f);

        return roll * roll;
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
