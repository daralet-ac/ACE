using System;
using ACE.Entity.Enum;

namespace ACE.Server.Entity;

/// <summary>
/// How the placer's Allegiance Rank improves a campfire built from a Build Campfire device. The rank is captured when the
/// fire is placed (Pet.Init), not read later, so it doesn't change while the fire burns. A higher rank makes the fire and its
/// resting hotspot a little bigger, and its Rested buff, which anyone who rests there gets, a stronger copy of Rested.
/// Kept apart from Pet/Hotspot so tests can reach the sums.
/// </summary>
public static class CampfireRules
{
    /// <summary>How much bigger the fire and its hotspot get for each rank: 2%.</summary>
    public const float ScalePerRank = 0.02f;

    /// <summary>Ranks past this don't make the fire any bigger: at most +20%.</summary>
    public const int MaxScaledRank = 10;

    /// <summary>XP each ranked Rested spell adds over the last, one for every 2 ranks: 1%.</summary>
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

    /// <summary>
    /// The Rested spell a fire placed at this rank casts in place of Rested (CampfireRest1, 5%): one of the copies of it that
    /// give 1% more for every 2 ranks, 6% at rank 2 up to 10% at rank 10. Null below rank 2, where plain Rested stays. Each
    /// is its own spell, with its own description in the client and a higher power than the last, so the strongest wins.
    /// </summary>
    public static SpellId? RankedRestSpell(int rank)
    {
        var steps = Math.Min(Math.Max(rank, 0) / 2, RankedRestSpells.Length);
        return steps == 0 ? null : RankedRestSpells[steps - 1];
    }

    /// <summary>The XP a Rested spell gives over plain Rested because of the fire's rank: 0 for any but the ranked copies.</summary>
    public static float RankXp(uint spellId)
    {
        var index = Array.IndexOf(RankedRestSpells, (SpellId)spellId);
        return index < 0 ? 0.0f : (index + 1) * XpBonusPerTwoRanks;
    }

    private static readonly SpellId[] RankedRestSpells =
    [
        SpellId.CampfireRest6Percent,
        SpellId.CampfireRest7Percent,
        SpellId.CampfireRest8Percent,
        SpellId.CampfireRest9Percent,
        SpellId.CampfireRest10Percent,
    ];

    /// <summary>
    /// What a Rested buff with this stat mod adds to XP, counted as EnchantmentManager.GetXpBonus counts it: a multiplicative
    /// mod as (value - 1), an additive one as its value.
    /// </summary>
    public static float BuffXp(float statModValue, bool multiplicative)
    {
        return multiplicative ? statModValue - 1.0f : statModValue;
    }

    /// <summary>An XP share as a percent: 0.12 is "12%", 0.015 is "1.5%".</summary>
    public static string Percent(float xp)
    {
        return $"{xp * 100:0.#}%";
    }

    /// <summary>The chat line a player gets when a Rested buff goes on.</summary>
    public static string AppliedMessage(float buffXp, float rankXp)
    {
        return $"You feel well rested by the fire: +{Percent(buffXp)} XP{RankPart(rankXp)}.";
    }

    /// <summary>The /rested line for a Rested buff; secondsLeft null for one that doesn't run out.</summary>
    public static string RestedLine(float buffXp, float rankXp, double? secondsLeft)
    {
        var time = secondsLeft == null ? "" : $" for {RestedXpRules.Duration(secondsLeft.Value)}";
        return $"Rested by a campfire: +{Percent(buffXp)} XP{time}{RankPart(rankXp)}.";
    }

    /// <summary>The /rested line when no Rested buff is on.</summary>
    public const string NotRestedLine =
        "Not rested by a campfire. Sit, crouch or sleep by one for 10 seconds, then stand up, for bonus XP.";

    private static string RankPart(float rankXp)
    {
        return rankXp > 0 ? $" (+{Percent(rankXp)} of it from the fire's Allegiance Rank)" : "";
    }
}
