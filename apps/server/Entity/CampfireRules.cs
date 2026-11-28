using System;

namespace ACE.Server.Entity;

/// <summary>
/// How the placer's Allegiance Rank improves a campfire built from a Build Campfire device. The rank is captured when the
/// fire is placed (Pet.Init), not read later, so it doesn't change while the fire burns. A higher rank makes the fire and its
/// resting hotspot a little bigger, and adds XP to the Rested buff (CampfireRest1-3) that anyone who rests there gets.
/// Kept apart from Pet/Hotspot so tests can reach the sums.
/// </summary>
public static class CampfireRules
{
    /// <summary>How much bigger the fire and its hotspot get for each rank: 2%.</summary>
    public const float ScalePerRank = 0.02f;

    /// <summary>Ranks past this don't make the fire any bigger: at most +20%.</summary>
    public const int MaxScaledRank = 10;

    /// <summary>XP added to the Rested buff for every 2 ranks: 1%.</summary>
    public const float XpBonusPerTwoRanks = 0.01f;

    /// <summary>A rank to use for the sums: none (not in an allegiance) is 0.</summary>
    public static int Rank(int? allegianceRank)
    {
        return Math.Max(allegianceRank ?? 0, 0);
    }

    /// <summary>The scale multiplier for a fire placed at this rank: 1 + 2% a rank, up to rank 10.</summary>
    public static float ScaleMod(int rank)
    {
        return 1.0f + ScalePerRank * Math.Clamp(rank, 0, MaxScaledRank);
    }

    /// <summary>The extra XP a Rested buff from a fire placed at this rank gives: 1% for every 2 ranks, rounded down.</summary>
    public static float XpBonus(int rank)
    {
        return Math.Max(rank, 0) / 2 * XpBonusPerTwoRanks;
    }

    /// <summary>
    /// The SpellStatModVal multiplier that makes a Rested spell whose stat mod is baseStatMod give xpBonus more XP.
    /// EnchantmentManager.GetXpBonus counts a multiplicative mod as (value - 1) and an additive one as its value, so
    /// either way the buff is worth xpBonus more when its value goes from baseStatMod to baseStatMod + xpBonus.
    /// </summary>
    public static float StatModMultiplier(float baseStatMod, float xpBonus)
    {
        if (xpBonus <= 0 || baseStatMod <= 0)
        {
            return 1.0f;
        }

        return (baseStatMod + xpBonus) / baseStatMod;
    }
}
