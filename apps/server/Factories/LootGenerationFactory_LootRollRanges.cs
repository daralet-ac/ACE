// Rebuilds the range each stat of a loot item rolled in, at its tier, and where in that range it landed - the loot
// counterpart of GetQuestItemRollRanges, shown on appraisal. Loot doesn't store its rolls, so each one is read back
// from the item's untinkered stats (its Base snapshots) with the same formulas the Mutate methods roll them with.
using System;
using System.Collections.Generic;
using ACE.Database;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Entity.Models;
using ACE.Server.Entity;
using ACE.Server.WorldObjects;

namespace ACE.Server.Factories;

public static partial class LootGenerationFactory
{
    private enum LootSubtypeBonus
    {
        None,
        CritChance,
        CritDamage,
        StaminaCostReduction,
        ArmorCleaving,
        WardCleaving,
        StaffDefense,
    }

    /// <summary>
    /// The roll ranges of every stat loot generation rolled on this item, at its tier. Quality is where the untinkered
    /// stat sits in its range, 0 to 1.
    /// </summary>
    public static List<QuestItemRollRange> GetLootItemRollRanges(WorldObject wo)
    {
        var ranges = new List<QuestItemRollRange>();
        var tier = GetLootItemTier(wo);

        if (tier < 1)
        {
            return ranges;
        }

        switch (wo.ItemType)
        {
            case ItemType.MeleeWeapon:
            case ItemType.MissileWeapon:
            case ItemType.Weapon:
            case ItemType.Caster:
                AddLootWeaponMainStatRanges(wo, tier, ranges);
                AddLootWeaponModRanges(wo, tier, ranges);
                AddLootWeaponSubtypeBonusRange(wo, tier, ranges);
                break;

            case ItemType.Armor:
            case ItemType.Clothing:
                AddLootArmorLevelRanges(wo, tier, ranges);
                AddLootArmorModRanges(wo, tier, ranges);
                AddLootArmorGearRatingRanges(wo, tier, ranges);
                break;

            case ItemType.Jewelry:
                AddLootJewelryRanges(wo, tier, ranges);
                break;
        }

        return ranges;
    }

    private static int GetLootItemTier(WorldObject wo)
    {
        if (wo.Tier is { } tier)
        {
            return Math.Clamp(tier, 0, 8);
        }

        if (wo.WieldRequirements == WieldRequirement.RawAttrib && wo.WieldDifficulty is { } wieldDifficulty)
        {
            return GetTierFromWieldDifficulty(wieldDifficulty);
        }

        return 0;
    }

    /// <summary>
    /// A stat as it rolled: tinkering moves the stat but leaves its Base snapshot where it rolled
    /// </summary>
    private static double? GetUntinkeredStat(WorldObject wo, double? current, double? snapshot)
    {
        return wo.NumTimesTinkered > 0 && snapshot is > 0 ? snapshot : current;
    }

    private static int? GetUntinkeredStat(WorldObject wo, int? current, int? snapshot)
    {
        return wo.NumTimesTinkered > 0 && snapshot is > 0 ? snapshot : current;
    }

    /// <summary>
    /// Adds a stat's range, shown through display, with its quality: where value sits between minimum and maximum
    /// </summary>
    private static void AddLootRange(
        List<QuestItemRollRange> ranges,
        string stat,
        double? value,
        double minimum,
        double maximum,
        bool isPercent,
        Func<double, double> display
    )
    {
        if (value == null || maximum <= minimum)
        {
            return;
        }

        var quality = Math.Clamp((value.Value - minimum) / (maximum - minimum), 0, 1);
        var low = display(minimum);
        var high = display(maximum);

        ranges.Add(new(stat, Math.Min(low, high), Math.Max(low, high), isPercent, quality));
    }

    private static double LootBonusPercent(double mod) => (mod - 1) * 100;

    private static double LootPercent(double mod) => mod * 100;

    private static double LootWhole(double value) => value;

    private static void AddLootWeaponMainStatRanges(WorldObject wo, int tier, List<QuestItemRollRange> ranges)
    {
        if (wo.WeenieType == WeenieType.Caster)
        {
            // tier 1 casters don't roll a damage or restoration mod (TryMutateCasterWeaponDamage)
            if (tier < 2)
            {
                return;
            }

            var (minimum, maximum) = TierPower.GetMultiplierRange(tier);
            var (restorationMinimum, restorationMaximum) = IsQuestItemWarCaster(wo)
                ? TierPower.GetWarRestorationRange(tier)
                : TierPower.GetNetherRestorationRange(tier);

            AddLootRange(ranges, "Elemental Damage Bonus", GetUntinkeredStat(wo, wo.ElementalDamageMod, wo.BaseElementalDamageMod), minimum, maximum, true, LootBonusPercent);
            AddLootRange(ranges, "Restoration Healing Bonus", GetUntinkeredStat(wo, wo.WeaponRestorationSpellsMod, wo.BaseWeaponRestorationSpellsMod), restorationMinimum, restorationMaximum, true, LootBonusPercent);
            return;
        }

        if (wo.WeenieType == WeenieType.MissileLauncher)
        {
            var damageMod = GetUntinkeredStat(wo, wo.DamageMod, wo.BaseDamageMod);

            if (tier >= 2)
            {
                var (minimum, maximum) = TierPower.GetMultiplierRange(tier);
                AddLootRange(ranges, "Damage Modifier", damageMod, minimum, maximum, true, LootBonusPercent);
            }
            else
            {
                // tier 1 launchers roll in the range from before the tier rework, sized by the launcher's speed as it
                // dropped
                var weenie = wo.Weenie ?? DatabaseManager.World?.GetCachedWeenie(wo.WeenieClassId);
                var weaponTime = weenie?.GetProperty(PropertyInt.WeaponTime) ?? wo.WeaponTime ?? 0;
                var hitsPerSecond = GetWeaponHitsPerSecond(weaponTime, true, tier);
                var (minimum, maximum) = GetPreviousLauncherDamageModRange(wo.WeaponSkill, tier, hitsPerSecond);

                AddLootRange(ranges, "Damage Modifier", damageMod, minimum, maximum, true, LootBonusPercent);
            }

            return;
        }

        // melee and thrown weapons roll whole Damage (TryMutateMeleeWeaponDamage)
        if (wo.Damage == null || wo.WeaponTime == null || wo.DamageVariance == null)
        {
            return;
        }

        var (damageMinimum, damageMaximum) = GetMeleeDamageRollRange(wo, tier);
        var damage = GetUntinkeredStat(wo, wo.Damage, wo.BaseDamage);

        AddLootRange(ranges, "Damage", damage, Math.Round(damageMinimum), Math.Round(damageMaximum), false, LootWhole);
    }

    /// <summary>
    /// The attack (or caster skill) and defense mods TryMutateWeaponMods rolls. Each is 10-20% plus the tier's bonus,
    /// scaled down when both rolled.
    /// </summary>
    private static void AddLootWeaponModRanges(WorldObject wo, int tier, List<QuestItemRollRange> ranges)
    {
        var caster = wo.WeenieType == WeenieType.Caster;
        var staff = caster && GetLootSubtypeBonus(wo) == LootSubtypeBonus.StaffDefense;

        var offense = GetUntinkeredStat(wo, wo.WeaponOffense, wo.BaseWeaponOffense);
        var warMagic = GetUntinkeredStat(wo, wo.WeaponWarMagicMod, wo.BaseWeaponWarMagicMod);
        var lifeMagic = GetUntinkeredStat(wo, wo.WeaponLifeMagicMod, wo.BaseWeaponLifeMagicMod);
        var physicalDefense = GetUntinkeredStat(wo, wo.WeaponPhysicalDefense, wo.BaseWeaponPhysicalDefense);
        var magicalDefense = GetUntinkeredStat(wo, wo.WeaponMagicalDefense, wo.BaseWeaponMagicalDefense);

        var rolledOffense = caster ? warMagic is > 0 || lifeMagic is > 0 : offense is > 1;
        var rolledDefense = physicalDefense is > 1 || magicalDefense is > 1;
        var multiplier = GetRolledTypesMultiplier((rolledOffense ? 1 : 0) + (rolledDefense ? 1 : 0));

        var index = Math.Clamp(tier - 1, 0, 7);
        var minimum = (LootTables.WeaponBaseOffenseMod + LootTables.WeaponOffenseModBonusPerTier[index]) * multiplier;
        var maximum = (LootTables.WeaponBaseOffenseMod * 2 + LootTables.WeaponOffenseModBonusPerTier[index]) * multiplier;

        if (caster)
        {
            AddLootRange(ranges, "Bonus to War Magic Skill", warMagic is > 0 ? warMagic : null, minimum, maximum, true, LootPercent);
            AddLootRange(ranges, "Bonus to Life Magic Skill", lifeMagic is > 0 ? lifeMagic : null, minimum, maximum, true, LootPercent);
        }
        else
        {
            AddLootRange(ranges, "Bonus to Attack Skill", offense is > 1 ? offense - 1 : null, minimum, maximum, true, LootPercent);
        }

        // a caster staff's defense mods also carry its subtype bonus on top, rolled on its own (RollBonusDefenseMod)
        var staffMinimum = staff ? LootTables.WeaponDefenseModBonusPerTier[index] : 0;
        var staffMaximum = staff ? LootTables.WeaponDefenseModBonusPerTier[index] + 0.1 : 0;

        AddLootRange(ranges, "Bonus to Physical Defense", physicalDefense is > 1 ? physicalDefense - 1 : null, minimum + staffMinimum, maximum + staffMaximum, true, LootPercent);
        AddLootRange(ranges, "Bonus to Magic Defense", magicalDefense is > 1 ? magicalDefense - 1 : null, minimum + staffMinimum, maximum + staffMaximum, true, LootPercent);
    }

    /// <summary>
    /// Which bonus TryMutateWeaponSubtypeBonuses rolls for this weapon
    /// </summary>
    private static LootSubtypeBonus GetLootSubtypeBonus(WorldObject wo)
    {
        switch (wo.WeaponSkill)
        {
            case Skill.Axe:
            case Skill.Dagger:
            case Skill.Bow:
                return LootSubtypeBonus.CritChance;
            case Skill.Mace:
            case Skill.Staff:
            case Skill.MissileWeapons:
                return LootSubtypeBonus.CritDamage;
            case Skill.Sword:
            case Skill.UnarmedCombat:
                return LootSubtypeBonus.StaminaCostReduction;
            case Skill.Spear:
            case Skill.Crossbow:
                return LootSubtypeBonus.ArmorCleaving;
            case Skill.ThrownWeapon:
                return GetThrownWeaponsSubType(wo) switch
                {
                    0 or 2 => LootSubtypeBonus.CritChance,
                    1 or 5 => LootSubtypeBonus.CritDamage,
                    3 or 4 => LootSubtypeBonus.ArmorCleaving,
                    _ => LootSubtypeBonus.None,
                };
            case Skill.WarMagic:
            case Skill.LifeMagic:
                return GetCasterSubType(wo) switch
                {
                    0 => LootSubtypeBonus.WardCleaving,
                    1 => LootSubtypeBonus.CritChance,
                    2 => LootSubtypeBonus.CritDamage,
                    3 => LootSubtypeBonus.StaffDefense,
                    _ => LootSubtypeBonus.None,
                };
            default:
                return LootSubtypeBonus.None;
        }
    }

    private static void AddLootWeaponSubtypeBonusRange(WorldObject wo, int tier, List<QuestItemRollRange> ranges)
    {
        var index = Math.Clamp(tier - 1, 0, 7);

        switch (GetLootSubtypeBonus(wo))
        {
            case LootSubtypeBonus.CritChance:
            {
                // 10% base critical chance, plus 0-5% on top of the tier's bonus (RollBonusCritChance)
                var minimum = 0.1 + LootTables.BonusCritChancePerTier[index];
                AddLootRange(ranges, "Biting Strike", wo.CriticalFrequency, minimum, minimum + 0.05, true, value => (value - 0.1) * 100);
                break;
            }
            case LootSubtypeBonus.CritDamage:
            {
                // 0-50% on top of the tier's bonus (RollBonusCritDamage)
                var minimum = 1.0 + LootTables.BonusCritMultiplierPerTier[index];
                AddLootRange(ranges, "Crushing Blow", wo.GetProperty(PropertyFloat.CriticalMultiplier), minimum, minimum + 0.5, true, value => WorldObject.GetCrushingBlowBonus(value) * 100);
                break;
            }
            case LootSubtypeBonus.StaminaCostReduction:
            {
                // 0-10% on top of the tier's bonus (RollBonusStaminaCostReduction)
                var minimum = (double)LootTables.StaminaCostReductionPerTier[index];
                AddLootRange(ranges, "Stamina Cost Reduction", wo.StaminaCostReductionMod, minimum, minimum + 0.1, true, LootPercent);
                break;
            }
            case LootSubtypeBonus.ArmorCleaving:
                // 10-20% at every tier, stored as the share of armor left (RollBonusArmorCleaving)
                AddLootRange(ranges, "Armor Cleaving", wo.IgnoreArmor is { } ignoreArmor ? 1 - ignoreArmor : null, 0.1, 0.2, true, LootPercent);
                break;
            case LootSubtypeBonus.WardCleaving:
                AddLootRange(ranges, "Ward Cleaving", wo.IgnoreWard is { } ignoreWard ? 1 - ignoreWard : null, 0.1, 0.2, true, LootPercent);
                break;
        }
    }

    /// <summary>
    /// The armor and ward levels AssignArmorLevel rolls, from tier 2 up
    /// </summary>
    private static void AddLootArmorLevelRanges(WorldObject wo, int tier, List<QuestItemRollRange> ranges)
    {
        if (tier < 2 || wo.ArmorType == null)
        {
            return;
        }

        var armorSlots = wo.ArmorSlots ?? 1;
        var (styleArmorLevel, baseWardLevel) = GetArmorBaseLevels(wo);

        // a style without an armor level of its own builds on its weenie's
        var weenie = wo.Weenie ?? DatabaseManager.World?.GetCachedWeenie(wo.WeenieClassId);
        var baseArmorLevel = styleArmorLevel ?? weenie?.GetProperty(PropertyInt.ArmorLevel) ?? 75;

        var (armorMinimum, armorMaximum) = wo.IsShield
            ? TierPower.GetShieldArmorRollRange(baseArmorLevel, tier)
            : TierPower.GetArmorRollRange(baseArmorLevel, tier);
        var (wardMinimum, wardMaximum) = TierPower.GetWardRollRange(baseWardLevel, tier);

        var armorLevel = GetUntinkeredStat(wo, wo.ArmorLevel, wo.BaseArmor);
        var wardLevel = GetUntinkeredStat(wo, wo.WardLevel, wo.BaseWard);

        AddLootRange(ranges, "Armor Level", armorLevel is > 0 ? armorLevel : null, (int)armorMinimum, (int)armorMaximum, false, LootWhole);
        AddLootRange(ranges, "Ward Level", wardLevel is > 0 ? wardLevel : null, (int)(wardMinimum * armorSlots), (int)(wardMaximum * armorSlots), false, LootWhole);
    }

    /// <summary>
    /// The skill and vital mods TryMutateArmorSkillMod rolls: each 10-20% (20-40% on headwear) plus the tier's bonus,
    /// scaled down by how many rolled and by the armor's slots (or halved on misc clothing)
    /// </summary>
    private static void AddLootArmorModRanges(WorldObject wo, int tier, List<QuestItemRollRange> ranges)
    {
        var mods = new List<(string Label, double Value)>();

        foreach (var (property, label) in QuestItemArmorMods)
        {
            var value = GetUntinkeredStat(wo, wo.GetProperty(property), wo.GetProperty(GetArmorModSnapshot(property)));

            if (value is > 0)
            {
                mods.Add((label, value.Value));
            }
        }

        if (wo.ManaConversionMod is > 0 && wo.ArmorWeightClass == (int)ArmorWeightClass.Cloth)
        {
            mods.Add(("Bonus to Mana Conversion Skill", wo.ManaConversionMod.Value));
        }

        if (mods.Count == 0)
        {
            return;
        }

        var rolledTypeCount = GetLootArmorRolledTypeCount(wo);
        if (rolledTypeCount == 0)
        {
            return;
        }

        var miscClothing = wo.ArmorType == (int)LootTables.ArmorType.MiscClothing && wo.ArmorWeightClass == 0;
        var scale = GetRolledTypesMultiplier(rolledTypeCount) * (miscClothing ? 0.5 : (wo.ArmorSlots ?? 1) / 10.0);

        var baseMod = LootTables.ArmorBaseSkillMod * (wo.ValidLocations == EquipMask.HeadWear ? 2.0 : 1.0);
        var tierBonus = LootTables.ArmorSkillModBonusPerTier[Math.Clamp(tier - 1, 0, 7)];
        var minimum = (baseMod + tierBonus) * scale;
        var maximum = (baseMod * 2 + tierBonus) * scale;

        foreach (var (label, value) in mods)
        {
            AddLootRange(ranges, label, value, minimum, maximum, true, LootPercent);
        }
    }

    private static PropertyFloat GetArmorModSnapshot(PropertyFloat property)
    {
        return property switch
        {
            PropertyFloat.ArmorWarMagicMod => PropertyFloat.BaseArmorWarMagicMod,
            PropertyFloat.ArmorLifeMagicMod => PropertyFloat.BaseArmorLifeMagicMod,
            PropertyFloat.ArmorAttackMod => PropertyFloat.BaseArmorAttackMod,
            PropertyFloat.ArmorPhysicalDefMod => PropertyFloat.BaseArmorPhysicalDefMod,
            PropertyFloat.ArmorMagicDefMod => PropertyFloat.BaseArmorMagicDefMod,
            PropertyFloat.ArmorDualWieldMod => PropertyFloat.BaseArmorDualWieldMod,
            PropertyFloat.ArmorTwohandedCombatMod => PropertyFloat.BaseArmorTwohandedCombatMod,
            PropertyFloat.ArmorRunMod => PropertyFloat.BaseArmorRunMod,
            PropertyFloat.ArmorThieveryMod => PropertyFloat.BaseArmorThieveryMod,
            PropertyFloat.ArmorShieldMod => PropertyFloat.BaseArmorShieldMod,
            PropertyFloat.ArmorPerceptionMod => PropertyFloat.BaseArmorPerceptionMod,
            PropertyFloat.ArmorDeceptionMod => PropertyFloat.BaseArmorDeceptionMod,
            PropertyFloat.ArmorHealthMod => PropertyFloat.BaseArmorHealthMod,
            PropertyFloat.ArmorHealthRegenMod => PropertyFloat.BaseArmorHealthRegenMod,
            PropertyFloat.ArmorStaminaMod => PropertyFloat.BaseArmorStaminaMod,
            PropertyFloat.ArmorStaminaRegenMod => PropertyFloat.BaseArmorStaminaRegenMod,
            PropertyFloat.ArmorManaMod => PropertyFloat.BaseArmorManaMod,
            PropertyFloat.ArmorManaRegenMod => PropertyFloat.BaseArmorManaRegenMod,
            _ => property,
        };
    }

    /// <summary>
    /// How many mod types TryMutateArmorSkillMod rolled, from the untinkered mods: a tink can add a mod that never
    /// rolled
    /// </summary>
    private static int GetLootArmorRolledTypeCount(WorldObject wo)
    {
        bool Rolled(PropertyFloat property) => GetUntinkeredStat(wo, wo.GetProperty(property), wo.GetProperty(GetArmorModSnapshot(property))) is > 0;

        if (wo.ArmorType == (int)LootTables.ArmorType.MiscClothing && wo.ArmorWeightClass == 0)
        {
            return CountTrue(
                Rolled(PropertyFloat.ArmorHealthMod),
                Rolled(PropertyFloat.ArmorStaminaMod),
                Rolled(PropertyFloat.ArmorManaMod)
            );
        }

        return (ArmorWeightClass)(wo.ArmorWeightClass ?? 0) switch
        {
            ArmorWeightClass.Cloth => CountTrue(
                Rolled(PropertyFloat.ArmorWarMagicMod),
                Rolled(PropertyFloat.ArmorLifeMagicMod),
                Rolled(PropertyFloat.ArmorPerceptionMod) || Rolled(PropertyFloat.ArmorDeceptionMod),
                Rolled(PropertyFloat.ArmorManaRegenMod),
                wo.ManaConversionMod is > 0
            ),
            ArmorWeightClass.Light => CountTrue(
                Rolled(PropertyFloat.ArmorAttackMod),
                Rolled(PropertyFloat.ArmorDualWieldMod) || Rolled(PropertyFloat.ArmorShieldMod),
                Rolled(PropertyFloat.ArmorThieveryMod) || Rolled(PropertyFloat.ArmorRunMod),
                Rolled(PropertyFloat.ArmorStaminaRegenMod),
                Rolled(PropertyFloat.ArmorPerceptionMod) || Rolled(PropertyFloat.ArmorDeceptionMod)
            ),
            ArmorWeightClass.Heavy => CountTrue(
                Rolled(PropertyFloat.ArmorAttackMod),
                Rolled(PropertyFloat.ArmorPhysicalDefMod) || Rolled(PropertyFloat.ArmorMagicDefMod),
                Rolled(PropertyFloat.ArmorShieldMod) || Rolled(PropertyFloat.ArmorTwohandedCombatMod),
                Rolled(PropertyFloat.ArmorPerceptionMod) || Rolled(PropertyFloat.ArmorDeceptionMod),
                Rolled(PropertyFloat.ArmorHealthRegenMod)
            ),
            _ => 0,
        };
    }

    /// <summary>
    /// The two ratings TryMutateGearRating rolls from tier 6 up: 0 to (tier - 4) per armor slot each, heavy armor's
    /// crit resist one more
    /// </summary>
    private static void AddLootArmorGearRatingRanges(WorldObject wo, int tier, List<QuestItemRollRange> ranges)
    {
        if (tier < 6)
        {
            return;
        }

        var armorSlots = wo.ArmorSlots ?? 1;
        var maximum = (tier - 4) * armorSlots;

        void AddRating(string stat, int? value, int offset)
        {
            AddLootRange(ranges, stat, value, offset * armorSlots, maximum + offset * armorSlots, false, LootWhole);
        }

        switch ((ArmorWeightClass)(wo.ArmorWeightClass ?? 0))
        {
            case ArmorWeightClass.Cloth:
                AddRating("Damage Rating", wo.GearDamage, 0);
                AddRating("Healing Boost Rating", wo.GearHealingBoost, 0);
                break;
            case ArmorWeightClass.Light:
                AddRating("Crit Damage Rating", wo.GearCritDamage, 0);
                AddRating("Crit Rating", wo.GearCrit, 0);
                break;
            case ArmorWeightClass.Heavy:
                AddRating("Damage Resist Rating", wo.GearDamageResist, 0);
                AddRating("Crit Resist Rating", wo.GearCritResist, 1);
                break;
        }
    }

    /// <summary>
    /// The ward and rating TryMutateJewelryMods rolls: ward up to the next tier's bottom (doubled on necklaces), and a
    /// rating from (tier - 1) / 2 up to tier - 1 (on necklaces, from 2 x (tier - 1) up to 4 x (tier - 1))
    /// </summary>
    private static void AddLootJewelryRanges(WorldObject wo, int tier, List<QuestItemRollRange> ranges)
    {
        var necklace = wo.ValidLocations is EquipMask.NeckWear;
        var necklaceMultiplier = necklace ? 2 : 1;

        var wardMinimum = GetBaseWardOfTier(tier) * necklaceMultiplier;
        var wardMaximum = GetBaseWardOfTier(Math.Clamp(tier + 1, 1, 9)) * necklaceMultiplier;
        var wardLevel = GetUntinkeredStat(wo, wo.WardLevel, wo.BaseWard);

        AddLootRange(ranges, "Ward Level", wardLevel is > 0 ? wardLevel : null, wardMinimum, wardMaximum, false, LootWhole);

        // a rating rolls as a fraction and drops the remainder, so the lowest it can show is its minimum's whole part
        void AddRating(string stat, int? value, double minimum, double maximum)
        {
            AddLootRange(ranges, stat, value is > 0 ? value : null, minimum, maximum, false, Math.Floor);
        }

        if (necklace)
        {
            AddRating("Healing Boost Rating", wo.GearHealingBoost, 2 * (tier - 1), 4 * (tier - 1));
            AddRating("Max Health Rating", wo.GearMaxHealth, 2 * (tier - 1), 4 * (tier - 1));
            return;
        }

        var ratingMinimum = (tier - 1) / 2.0;
        var ratingMaximum = tier - 1.0;

        if (wo.ValidLocations == EquipMask.FingerWear)
        {
            AddRating("Crit Damage Rating", wo.GearCritDamage, ratingMinimum, ratingMaximum);
            AddRating("Crit Damage Resist Rating", wo.GearCritDamageResist, ratingMinimum, ratingMaximum);
        }
        else if (wo.ValidLocations == EquipMask.WristWear)
        {
            AddRating("Damage Rating", wo.GearDamage, ratingMinimum, ratingMaximum);
            AddRating("Damage Resist Rating", wo.GearDamageResist, ratingMinimum, ratingMaximum);
        }
    }
}
