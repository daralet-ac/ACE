using System;
using ACE.Entity.Enum;
using ACE.Server.Entity;

namespace ACE.Server.Factories;

/// <summary>
/// The math behind quest item mutation (LootGenerationFactory.MutateQuestItem), kept free of WorldObject so it
/// can be unit tested.
/// </summary>
public static class QuestItemMutation
{
    /// <summary>
    /// Jewelry and level-gated clothing tier by required character level; everything else by attribute amount.
    /// </summary>
    public static bool UsesRequiredLevelTiering(ItemType itemType, WeenieType weenieType, WieldRequirement wieldRequirements)
    {
        return itemType == ItemType.Jewelry
            || (weenieType == WeenieType.Clothing && wieldRequirements == WieldRequirement.Level);
    }

    /// <summary>
    /// The 0-7 tier index for a quest item's wield requirement.
    /// </summary>
    public static int GetTierIndex(bool usesRequiredLevelTiering, int? requirement)
    {
        var tier = usesRequiredLevelTiering
            ? LootGenerationFactory.GetTierFromRequiredLevel(requirement ?? 1)
            : LootGenerationFactory.GetTierFromWieldDifficulty(requirement ?? 50);

        return Math.Clamp(tier - 1, 0, 7);
    }

    /// <summary>
    /// Rolls a main stat from the middle of its tier range up to the top, never below floor (the item's authored
    /// value, or its current value when an item mutated under the old rules is converted).
    /// </summary>
    public static double RollMainStat(double floor, double tierMin, double tierMax, double roll)
    {
        return Math.Max(floor, GetRollValue(tierMin, tierMax, roll));
    }

    /// <summary>
    /// Where a roll of 0 to 1 lands between the tier median and the tier max.
    /// </summary>
    public static double GetRollValue(double tierMin, double tierMax, double roll)
    {
        var median = (tierMin + tierMax) / 2;

        return median + (tierMax - median) * roll;
    }

    /// <summary>
    /// RollMainStat for whole-number stats. Rounds half up, so a median that falls on a half still counts as the floor.
    /// </summary>
    public static int RollWholeMainStat(int floor, int tierMin, int tierMax, double roll)
    {
        return (int)Math.Round(RollMainStat(floor, tierMin, tierMax, roll), MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// What one percentage tink (Iron) adds: a share of the untinkered value, but never less than 1.
    /// </summary>
    public static int GetPercentTinkBonus(int baseValue, double percent)
    {
        return Math.Max(1, (int)(baseValue * percent));
    }

    /// <summary>
    /// The range a caster's elemental damage mod rolls in at tier index t: the multiplier ladder (TierPower), the
    /// same for war and Nether (Life) casters.
    /// </summary>
    public static (double Min, double Max) GetCasterDamageModRange(int tier)
    {
        return TierPower.GetMultiplierRange(tier + 1);
    }

    /// <summary>
    /// The range a caster's restoration mod rolls in at tier index t, on the war or Nether ladder (TierPower)
    /// </summary>
    public static (double Min, double Max) GetCasterRestorationRange(int tier, bool nether)
    {
        return nether ? TierPower.GetNetherRestorationRange(tier + 1) : TierPower.GetWarRestorationRange(tier + 1);
    }

    /// <summary>
    /// A caster's restoration mod, at the same place in its range as its elemental damage mod is in its own.
    /// </summary>
    public static double GetCasterRestorationMod(int tier, double elementalDamageMod, bool nether)
    {
        var position = TierPower.GetRollPosition(elementalDamageMod, GetCasterDamageModRange(tier));

        return TierPower.GetRestorationForRoll(tier + 1, position, nether);
    }

    /// <summary>
    /// The Armor Level loot armor rolls in at tier index t (AssignArmorLevel): doubling every tier from tier 2
    /// (TierPower). Tier 1 keeps 0 to base.
    /// </summary>
    public static (int Min, int Max) GetArmorLevelRange(int baseArmorLevel, int tier)
    {
        if (tier <= 0)
        {
            return (0, baseArmorLevel);
        }

        var (minimum, maximum) = TierPower.GetArmorRollRange(baseArmorLevel, tier + 1);

        return ((int)minimum, (int)maximum);
    }

    /// <summary>
    /// The Armor Level a loot shield rolls in at tier index t: base x t to base x (t + 1), as before the tier rework,
    /// since a shield's armor feeds the block check rather than damage mitigation.
    /// </summary>
    public static (int Min, int Max) GetShieldArmorLevelRange(int baseArmorLevel, int tier)
    {
        return (baseArmorLevel * tier, baseArmorLevel * (tier + 1));
    }

    /// <summary>
    /// The Ward Level loot armor rolls in at tier index t, per armor slot (AssignArmorLevel): doubling every tier
    /// from tier 2 (TierPower). Tier 1 keeps 0 to one base per slot.
    /// </summary>
    public static (int Min, int Max) GetWardLevelRange(int baseWardLevel, int tier, int armorSlots)
    {
        if (tier <= 0)
        {
            return (0, baseWardLevel * armorSlots);
        }

        var (minimum, maximum) = TierPower.GetWardRollRange(baseWardLevel, tier + 1);

        return ((int)minimum * armorSlots, (int)maximum * armorSlots);
    }

    /// <summary>
    /// Loot jewelry rolls between this tier's and the next tier's base Ward Level; necklaces carry double.
    /// </summary>
    public static (int Min, int Max) GetJewelryWardLevelRange(int tier, bool isNecklace)
    {
        var wardPerTier = LootTables.JewelryBaseWardLeverPerTier; // indexed by 1-based tier
        var multiplier = isNecklace ? 2 : 1;

        return (wardPerTier[tier + 1] * multiplier, wardPerTier[Math.Min(tier + 2, wardPerTier.Length - 1)] * multiplier);
    }

    /// <summary>
    /// How far an armor piece's skill mods roll above their authored values: the same span as a loot piece's
    /// mod roll, which scales with armor slots and doubles on helms.
    /// </summary>
    public static float GetArmorModBonusRange(int armorSlots, bool isHelm)
    {
        return 0.1f * armorSlots / 10 * (isHelm ? 2 : 1);
    }
}
