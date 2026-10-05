using System;
using System.Collections.Generic;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Entity.Models;
using ACE.Server.WorldObjects;

namespace ACE.Server.Factories;

public static partial class LootGenerationFactory
{
    private static readonly float WeaponModMaxBonus = 0.1f;

    // Quest item mutation bonus ranges (flat, additive on top of the item's SQL-authored base value).
    // Shared with AppraiseInfo so the appraisal roll-range text can't drift from the actual roll.
    public const float QuestCritFrequencyBonusRange = 0.05f;
    public const float QuestCritMultiplierBonusRange = 0.5f;
    public const float QuestIgnoreArmorBonusRange = 0.1f;
    public const float QuestIgnoreWardBonusRange = 0.1f;

    /// <summary>
    /// Rolls a quest item's stats the first time a player gets it. Its main stats (weapon damage, armor level and
    /// ward level) roll from the middle of its tier's range up to the top, then get the tinks a fully tinkered loot
    /// item of the same tier would carry, since quest items can't be tinkered.
    /// </summary>
    public static void MutateQuestItem(WorldObject wo)
    {
        wo.SetProperty(PropertyBool.MutableQuestItem, false);

        var tier = GetQuestItemTierIndex(wo);
        var lootQuality = (float)(wo.LootQualityMod ?? 0.0f);
        var armorSlots = wo.ArmorSlots ?? 1;

        // Weapon Stats
        if (IsQuestItemWeapon(wo))
        {
            if (wo.WeaponSubtype == null)
            {
                _log.Error($"MutateQuestItem() - WeaponSubType is null for ({wo.Name})");
                return;
            }

            if (RollQuestItemWeaponMainStat(wo, (LootTables.WeaponSubtype)wo.WeaponSubtype, tier, lootQuality))
            {
                SetQuestItemTinks(wo, LootTables.QuestItemWeaponTinksPerTier[tier]);
            }

            if (wo.WeaponOffense != null)
            {
                var baseStat = wo.WeaponOffense.Value;
                var bonusRange = WeaponModMaxBonus;
                var roll = GetDiminishingRoll(null, lootQuality);
                var bonus = bonusRange * roll;
                var final = baseStat + bonus;

                wo.SetProperty(PropertyFloat.WeaponOffense, final);
            }

            if (wo.WeaponPhysicalDefense != null)
            {
                var baseStat = wo.WeaponPhysicalDefense.Value;
                var bonusRange = WeaponModMaxBonus;
                var roll = GetDiminishingRoll(null, lootQuality);
                var bonus = bonusRange * roll;
                var final = baseStat + bonus;

                wo.SetProperty(PropertyFloat.WeaponPhysicalDefense, final);
            }

            if (wo.WeaponMagicalDefense != null)
            {
                var baseStat = wo.WeaponMagicalDefense.Value;
                var bonusRange = WeaponModMaxBonus;
                var roll = GetDiminishingRoll(null, lootQuality);
                var bonus = bonusRange * roll;
                var final = baseStat + bonus;

                wo.SetProperty(PropertyFloat.WeaponMagicalDefense, final);
            }

            if (wo.WeaponLifeMagicMod != null)
            {
                var baseStat = wo.WeaponLifeMagicMod.Value;
                var bonusRange = WeaponModMaxBonus;
                var roll = GetDiminishingRoll(null, lootQuality);
                var bonus = bonusRange * roll;
                var final = baseStat + bonus;

                wo.SetProperty(PropertyFloat.WeaponLifeMagicMod, final);
            }

            if (wo.WeaponWarMagicMod != null)
            {
                var baseStat = wo.WeaponWarMagicMod.Value;
                var bonusRange = WeaponModMaxBonus;
                var roll = GetDiminishingRoll(null, lootQuality);
                var bonus = bonusRange * roll;
                var final = baseStat + bonus;

                wo.SetProperty(PropertyFloat.WeaponWarMagicMod, final);
            }

            //if (wo.WeaponRestorationSpellsMod != null)
            //{
            //    var baseStat = wo.WeaponRestorationSpellsMod.Value;
            //    var bonusRange = WeaponModMaxBonus;
            //    var roll =GetDiminishingRoll(null, lootQuality);
            //    var bonus = bonusRange * roll;
            //    var final = baseStat + bonus;

            //    wo.SetProperty(PropertyFloat.WeaponRestorationSpellsMod, final);
            //}

            if (wo.CriticalFrequency != null)
            {
                var baseStat = wo.CriticalFrequency.Value;
                var bonusRange = QuestCritFrequencyBonusRange;
                var roll = GetDiminishingRoll(null, lootQuality);
                var bonus = bonusRange * roll;
                var final = baseStat + bonus;

                wo.SetProperty(PropertyFloat.CriticalFrequency, final);
            }

            if (wo.GetProperty(PropertyFloat.CriticalMultiplier) != null)
            {
                var baseStat = wo.GetProperty(PropertyFloat.CriticalMultiplier) ?? 1.0f;
                var bonusRange = QuestCritMultiplierBonusRange;
                var roll = GetDiminishingRoll(null, lootQuality);
                var bonus = bonusRange * roll;
                var final = baseStat + bonus;

                wo.SetProperty(PropertyFloat.CriticalMultiplier, final);
            }

            if (wo.IgnoreArmor != null)
            {
                var baseStat = wo.IgnoreArmor.Value;
                var bonusRange = QuestIgnoreArmorBonusRange;
                var roll = GetDiminishingRoll(null, lootQuality);
                var bonus = bonusRange * roll;
                var final = baseStat - bonus;

                wo.SetProperty(PropertyFloat.IgnoreArmor, final);
            }

            if (wo.IgnoreWard != null)
            {
                var baseStat = wo.IgnoreWard.Value;
                var bonusRange = QuestIgnoreWardBonusRange;
                var roll = GetDiminishingRoll(null, lootQuality);
                var bonus = bonusRange * roll;
                var final = baseStat - bonus;

                wo.SetProperty(PropertyFloat.IgnoreWard, final);
            }
        }

        // Armor Base Stats
        if (IsQuestItemArmor(wo))
        {
            var rolledArmorLevel = RollQuestItemArmorLevel(wo, tier, lootQuality);
            var rolledWardLevel = RollQuestItemWardLevel(wo, tier, lootQuality);

            if (wo.ArmorModVsAcid != null)
            {
                var baseStat = wo.ArmorModVsAcid.Value;
                var bonusRange = (baseStat * 1.1f) - baseStat;
                var roll = GetDiminishingRoll(null, lootQuality);
                var bonus = bonusRange * roll;
                var final = baseStat + bonus;

                wo.SetProperty(PropertyFloat.ArmorModVsAcid, final);
            }

            if (wo.ArmorModVsBludgeon != null)
            {
                var baseStat = wo.ArmorModVsBludgeon.Value;
                var bonusRange = (baseStat * 1.1f) - baseStat;
                var roll = GetDiminishingRoll(null, lootQuality);
                var bonus = bonusRange * roll;
                var final = baseStat + bonus;

                wo.SetProperty(PropertyFloat.ArmorModVsBludgeon, final);
            }

            if (wo.ArmorModVsCold != null)
            {
                var baseStat = wo.ArmorModVsCold.Value;
                var bonusRange = (baseStat * 1.1f) - baseStat;
                var roll = GetDiminishingRoll(null, lootQuality);
                var bonus = bonusRange * roll;
                var final = baseStat + bonus;

                wo.SetProperty(PropertyFloat.ArmorModVsCold, final);
            }

            if (wo.ArmorModVsElectric != null)
            {
                var baseStat = wo.ArmorModVsElectric.Value;
                var bonusRange = (baseStat * 1.1f) - baseStat;
                var roll = GetDiminishingRoll(null, lootQuality);
                var bonus = bonusRange * roll;
                var final = baseStat + bonus;

                wo.SetProperty(PropertyFloat.ArmorModVsElectric, final);
            }

            if (wo.ArmorModVsFire != null)
            {
                var baseStat = wo.ArmorModVsFire.Value;
                var bonusRange = (baseStat * 1.1f) - baseStat;
                var roll = GetDiminishingRoll(null, lootQuality);
                var bonus = bonusRange * roll;
                var final = baseStat + bonus;

                wo.SetProperty(PropertyFloat.ArmorModVsFire, final);
            }

            if (wo.ArmorModVsPierce != null)
            {
                var baseStat = wo.ArmorModVsPierce.Value;
                var bonusRange = (baseStat * 1.1f) - baseStat;
                var roll = GetDiminishingRoll(null, lootQuality);
                var bonus = bonusRange * roll;
                var final = baseStat + bonus;

                wo.SetProperty(PropertyFloat.ArmorModVsPierce, final);
            }

            if (wo.ArmorModVsSlash != null)
            {
                var baseStat = wo.ArmorModVsSlash.Value;
                var bonusRange = (baseStat * 1.1f) - baseStat;
                var roll = GetDiminishingRoll(null, lootQuality);
                var bonus = bonusRange * roll;
                var final = baseStat + bonus;

                wo.SetProperty(PropertyFloat.ArmorModVsSlash, final);
            }

            var armorModBonusRange = GetQuestItemArmorModBonusRange(wo, armorSlots);

            // Armor Mods
            if (wo.ArmorAttackMod != null)
            {
                var baseStat = wo.ArmorAttackMod.Value;
                var bonusRange = armorModBonusRange;
                var roll = GetDiminishingRoll(null, lootQuality);
                var bonus = bonusRange * roll;
                var final = baseStat + bonus;

                wo.SetProperty(PropertyFloat.ArmorAttackMod, final);
            }

            if (wo.ArmorDeceptionMod != null)
            {
                var baseStat = wo.ArmorDeceptionMod.Value;
                var bonusRange = armorModBonusRange;
                var roll = GetDiminishingRoll(null, lootQuality);
                var bonus = bonusRange * roll;
                var final = baseStat + bonus;

                wo.SetProperty(PropertyFloat.ArmorDeceptionMod, final);
            }

            if (wo.ArmorDualWieldMod != null)
            {
                var baseStat = wo.ArmorDualWieldMod.Value;
                var bonusRange = armorModBonusRange;
                var roll = GetDiminishingRoll(null, lootQuality);
                var bonus = bonusRange * roll;
                var final = baseStat + bonus;

                wo.SetProperty(PropertyFloat.ArmorDualWieldMod, final);
            }

            if (wo.ArmorHealthMod != null)
            {
                var baseStat = wo.ArmorHealthMod.Value;
                var bonusRange = armorModBonusRange;
                var roll = GetDiminishingRoll(null, lootQuality);
                var bonus = bonusRange * roll;
                var final = baseStat + bonus;

                wo.SetProperty(PropertyFloat.ArmorHealthMod, final);
            }

            if (wo.ArmorHealthRegenMod != null)
            {
                var baseStat = wo.ArmorHealthRegenMod.Value;
                var bonusRange = armorModBonusRange;
                var roll = GetDiminishingRoll(null, lootQuality);
                var bonus = bonusRange * roll;
                var final = baseStat + bonus;

                wo.SetProperty(PropertyFloat.ArmorHealthRegenMod, final);
            }

            if (wo.ArmorLifeMagicMod != null)
            {
                var baseStat = wo.ArmorLifeMagicMod.Value;
                var bonusRange = armorModBonusRange;
                var roll = GetDiminishingRoll(null, lootQuality);
                var bonus = bonusRange * roll;
                var final = baseStat + bonus;

                wo.SetProperty(PropertyFloat.ArmorLifeMagicMod, final);
            }

            if (wo.ArmorMagicDefMod != null)
            {
                var baseStat = wo.ArmorMagicDefMod.Value;
                var bonusRange = armorModBonusRange;
                var roll = GetDiminishingRoll(null, lootQuality);
                var bonus = bonusRange * roll;
                var final = baseStat + bonus;

                wo.SetProperty(PropertyFloat.ArmorMagicDefMod, final);
            }

            if (wo.ArmorManaMod != null)
            {
                var baseStat = wo.ArmorManaMod.Value;
                var bonusRange = armorModBonusRange;
                var roll = GetDiminishingRoll(null, lootQuality);
                var bonus = bonusRange * roll;
                var final = baseStat + bonus;

                wo.SetProperty(PropertyFloat.ArmorManaMod, final);
            }

            if (wo.ArmorManaRegenMod != null)
            {
                var baseStat = wo.ArmorManaRegenMod.Value;
                var bonusRange = armorModBonusRange;
                var roll = GetDiminishingRoll(null, lootQuality);
                var bonus = bonusRange * roll;
                var final = baseStat + bonus;

                wo.SetProperty(PropertyFloat.ArmorManaRegenMod, final);
            }

            if (wo.ArmorPerceptionMod != null)
            {
                var baseStat = wo.ArmorPerceptionMod.Value;
                var bonusRange = armorModBonusRange;
                var roll = GetDiminishingRoll(null, lootQuality);
                var bonus = bonusRange * roll;
                var final = baseStat + bonus;

                wo.SetProperty(PropertyFloat.ArmorPerceptionMod, final);
            }

            if (wo.ArmorPhysicalDefMod != null)
            {
                var baseStat = wo.ArmorPhysicalDefMod.Value;
                var bonusRange = armorModBonusRange;
                var roll = GetDiminishingRoll(null, lootQuality);
                var bonus = bonusRange * roll;
                var final = baseStat + bonus;

                wo.SetProperty(PropertyFloat.ArmorPhysicalDefMod, final);
            }

            if (wo.ArmorRunMod != null)
            {
                var baseStat = wo.ArmorRunMod.Value;
                var bonusRange = armorModBonusRange;
                var roll = GetDiminishingRoll(null, lootQuality);
                var bonus = bonusRange * roll;
                var final = baseStat + bonus;

                wo.SetProperty(PropertyFloat.ArmorRunMod, final);
            }

            if (wo.ArmorShieldMod != null)
            {
                var baseStat = wo.ArmorShieldMod.Value;
                var bonusRange = armorModBonusRange;
                var roll = GetDiminishingRoll(null, lootQuality);
                var bonus = bonusRange * roll;
                var final = baseStat + bonus;

                wo.SetProperty(PropertyFloat.ArmorShieldMod, final);
            }

            if (wo.ArmorStaminaMod != null)
            {
                var baseStat = wo.ArmorStaminaMod.Value;
                var bonusRange = armorModBonusRange;
                var roll = GetDiminishingRoll(null, lootQuality);
                var bonus = bonusRange * roll;
                var final = baseStat + bonus;

                wo.SetProperty(PropertyFloat.ArmorStaminaMod, final);
            }

            if (wo.ArmorStaminaRegenMod != null)
            {
                var baseStat = wo.ArmorStaminaRegenMod.Value;
                var bonusRange = armorModBonusRange;
                var roll = GetDiminishingRoll(null, lootQuality);
                var bonus = bonusRange * roll;
                var final = baseStat + bonus;

                wo.SetProperty(PropertyFloat.ArmorStaminaRegenMod, final);
            }

            if (wo.ArmorThieveryMod != null)
            {
                var baseStat = wo.ArmorThieveryMod.Value;
                var bonusRange = armorModBonusRange;
                var roll = GetDiminishingRoll(null, lootQuality);
                var bonus = bonusRange * roll;
                var final = baseStat + bonus;

                wo.SetProperty(PropertyFloat.ArmorThieveryMod, final);
            }

            if (wo.ArmorTwohandedCombatMod != null)
            {
                var baseStat = wo.ArmorTwohandedCombatMod.Value;
                var bonusRange = armorModBonusRange;
                var roll = GetDiminishingRoll(null, lootQuality);
                var bonus = bonusRange * roll;
                var final = baseStat + bonus;

                wo.SetProperty(PropertyFloat.ArmorTwohandedCombatMod, final);
            }

            if (wo.ArmorWarMagicMod != null)
            {
                var baseStat = wo.ArmorWarMagicMod.Value;
                var bonusRange = armorModBonusRange;
                var roll = GetDiminishingRoll(null, lootQuality);
                var bonus = bonusRange * roll;
                var final = baseStat + bonus;

                wo.SetProperty(PropertyFloat.ArmorWarMagicMod, final);
            }

            if (wo.ItemType is ItemType.Armor)
            {
                NormalizeProtectionLevels(wo);
            }

            // tinks are sized off the Armor Level NormalizeProtectionLevels leaves, as with loot
            if (rolledArmorLevel || rolledWardLevel)
            {
                SetQuestItemTinks(wo, LootTables.QuestItemArmorTinksPerTier[tier]);
            }
        }
    }

    /// <summary>
    /// The 0-7 tier index for a quest item's wield requirement (its current one if none is given).
    /// </summary>
    public static int GetQuestItemTierIndex(WorldObject wo, int? requirement = null)
    {
        return QuestItemMutation.GetTierIndex(UsesRequiredLevelTiering(wo), requirement ?? wo.WieldDifficulty);
    }

    /// <summary>
    /// Jewelry and level-gated clothing tier by required character level rather than attribute amount.
    /// </summary>
    public static bool UsesRequiredLevelTiering(WorldObject wo)
    {
        return QuestItemMutation.UsesRequiredLevelTiering(wo.ItemType, wo.WeenieType, wo.WieldRequirements);
    }

    /// <summary>
    /// Takes a quest item's baked-in tinks back off, so an Upgrade Kit rescales only its rolled stats.
    /// </summary>
    internal static void RemoveQuestItemTinks(WorldObject wo)
    {
        if (wo.QuestItemTinks is { } tinks)
        {
            AddQuestItemTinkBonus(wo, -tinks);
        }
    }

    /// <summary>
    /// Bakes in the tinks for a quest item's tier once an Upgrade Kit has rescaled its stats. An item that mutated
    /// under the old rules has no tinks yet, so its main stats first get a fresh roll at the new tier wherever that
    /// beats the upgraded value - converting never lowers a stat.
    /// </summary>
    internal static void ApplyQuestItemTinksForTier(WorldObject wo, int tier)
    {
        var converting = wo.QuestItemTinks == null;
        var lootQuality = (float)(wo.LootQualityMod ?? 0.0f);

        if (IsQuestItemWeapon(wo))
        {
            if (wo.WeaponSubtype == null)
            {
                return;
            }

            if (!converting || RollQuestItemWeaponMainStat(wo, (LootTables.WeaponSubtype)wo.WeaponSubtype, tier, lootQuality))
            {
                SetQuestItemTinks(wo, LootTables.QuestItemWeaponTinksPerTier[tier]);
            }
        }
        else if (IsQuestItemArmor(wo))
        {
            if (converting)
            {
                var rolledArmorLevel = RollQuestItemArmorLevel(wo, tier, lootQuality);
                var rolledWardLevel = RollQuestItemWardLevel(wo, tier, lootQuality);

                if (!rolledArmorLevel && !rolledWardLevel)
                {
                    return;
                }
            }

            SetQuestItemTinks(wo, LootTables.QuestItemArmorTinksPerTier[tier]);
        }
    }

    /// <summary>
    /// One rolled stat's spread for appraisal, in the units the item shows it in (whole numbers, or percents when
    /// IsPercent), with the baked-in tinks included. Quality is where this copy rolled, 0 to 1 - null until it has.
    /// </summary>
    public readonly record struct QuestItemRollRange(string Stat, double Low, double High, bool IsPercent, double? Quality);

    private static readonly (PropertyFloat Property, string Label)[] QuestItemArmorMods =
    [
        (PropertyFloat.ArmorWarMagicMod, "Bonus to War Magic Skill"),
        (PropertyFloat.ArmorLifeMagicMod, "Bonus to Life Magic Skill"),
        (PropertyFloat.ArmorAttackMod, "Bonus to Attack Skill"),
        (PropertyFloat.ArmorPhysicalDefMod, "Bonus to Physical Defense"),
        (PropertyFloat.ArmorMagicDefMod, "Bonus to Magic Defense"),
        (PropertyFloat.ArmorDualWieldMod, "Bonus to Dual Wield Skill"),
        (PropertyFloat.ArmorTwohandedCombatMod, "Bonus to Two-handed Combat Skill"),
        (PropertyFloat.ArmorRunMod, "Bonus to Run Skill"),
        (PropertyFloat.ArmorThieveryMod, "Bonus to Thievery Skill"),
        (PropertyFloat.ArmorShieldMod, "Bonus to Shield Skill"),
        (PropertyFloat.ArmorPerceptionMod, "Bonus to Perception Skill"),
        (PropertyFloat.ArmorDeceptionMod, "Bonus to Deception Skill"),
        (PropertyFloat.ArmorHealthMod, "Bonus to Maximum Health"),
        (PropertyFloat.ArmorHealthRegenMod, "Bonus to Health Regen"),
        (PropertyFloat.ArmorStaminaMod, "Bonus to Maximum Stamina"),
        (PropertyFloat.ArmorStaminaRegenMod, "Bonus to Stamina Regen"),
        (PropertyFloat.ArmorManaMod, "Bonus to Maximum Mana"),
        (PropertyFloat.ArmorManaRegenMod, "Bonus to Mana Regen"),
    ];

    /// <summary>
    /// The roll ranges of every stat MutateQuestItem rolls, at the item's current tier and with its baked-in tinks.
    /// authored is the item's weenie: secondary stats roll up from their authored values, so without it only the main
    /// stats are listed. An unrolled item's current main stats are their floors; a rolled one is shown from the tier
    /// median. Armor Level is scaled by the average protection NormalizeProtectionLevels folds into it - for armor,
    /// pass the lowest and highest that average can come out after protections roll.
    /// </summary>
    public static List<QuestItemRollRange> GetQuestItemRollRanges(WorldObject wo, Weenie authored, double protectionScaleMin = 1.0, double protectionScaleMax = 1.0)
    {
        var ranges = new List<QuestItemRollRange>();
        var tier = GetQuestItemTierIndex(wo);
        var rolled = !wo.MutableQuestItem;

        if (IsQuestItemWeapon(wo))
        {
            AddQuestItemWeaponMainStatRange(wo, tier, rolled, ranges);
            AddQuestItemWeaponModRanges(wo, authored, tier, rolled, ranges);
            return ranges;
        }

        if (!IsQuestItemArmor(wo))
        {
            return ranges;
        }

        var tinks = LootTables.QuestItemArmorTinksPerTier[tier];

        if (wo.ArmorLevel is > 0)
        {
            var (minimum, maximum) = GetQuestItemArmorLevelRange(wo, tier);
            var floor = rolled ? 0 : wo.ArmorLevel.Value;
            var low = (int)(QuestItemMutation.RollWholeMainStat(floor, minimum, maximum, 0) * protectionScaleMin);
            var high = (int)(QuestItemMutation.RollWholeMainStat(floor, minimum, maximum, 1) * protectionScaleMax);
            int Tinked(int value) => value + tinks * QuestItemMutation.GetPercentTinkBonus(value, Salvage.IronTinkPercent);

            ranges.Add(new("Armor Level", Tinked(low), Tinked(high), false, rolled ? wo.QuestItemRollQuality : null));
        }

        if (wo.WardLevel is > 0)
        {
            var (minimum, maximum) = GetQuestItemWardLevelRange(wo, tier);
            var floor = rolled ? 0 : wo.WardLevel.Value;
            var wardTinks = tinks * (wo.ItemType == ItemType.Jewelry ? Salvage.WhiteJadeTinkWardLevel : Salvage.SilverTinkWardLevel * (wo.ArmorSlots ?? 1));

            ranges.Add(new(
                "Ward Level",
                QuestItemMutation.RollWholeMainStat(floor, minimum, maximum, 0) + wardTinks,
                QuestItemMutation.RollWholeMainStat(floor, minimum, maximum, 1) + wardTinks,
                false,
                rolled ? wo.QuestItemWardRollQuality : null
            ));
        }

        if (authored == null)
        {
            return ranges;
        }

        // Upgrade Kits shift every armor skill mod by the tier bonus difference (jewelry only its perception and deception)
        var scaledByUpgrades = wo.WeenieType == WeenieType.Clothing || wo.ItemType == ItemType.Armor;
        var tierShift = LootTables.ArmorSkillModBonusPerTier[tier] - LootTables.ArmorSkillModBonusPerTier[GetQuestItemAuthoredTier(wo, authored)];
        var span = GetQuestItemArmorModBonusRange(wo, wo.ArmorSlots ?? 1);

        foreach (var (property, label) in QuestItemArmorMods)
        {
            var shifted = scaledByUpgrades || property is PropertyFloat.ArmorPerceptionMod or PropertyFloat.ArmorDeceptionMod;
            AddQuestItemFlatRange(ranges, label, wo.GetProperty(property), authored.GetProperty(property), shifted ? tierShift : 0, span, rolled, value => value * 100);
        }

        return ranges;
    }

    private static void AddQuestItemWeaponMainStatRange(WorldObject wo, int tier, bool rolled, List<QuestItemRollRange> ranges)
    {
        if (wo.WeaponSubtype is not { } subtypeValue)
        {
            return;
        }

        var subtype = (LootTables.WeaponSubtype)subtypeValue;
        var tinks = LootTables.QuestItemWeaponTinksPerTier[tier];

        // a mod is shown as a bonus percent, e.g. 1.25 as 25%
        static double Percent(double mod) => (mod - 1) * 100;

        // a rolled float stat's quality is where it sits in the range (Upgrade Kits keep that place exactly)
        static double? Quality(bool rolled, double? value, double minimum, double maximum) =>
            rolled && value != null ? Math.Clamp((value.Value - (minimum + maximum) / 2) / ((maximum - minimum) / 2), 0, 1) : null;

        if (wo.WeenieType == WeenieType.Caster)
        {
            if (subtype != LootTables.WeaponSubtype.Caster || (wo.ElementalDamageMod == null && wo.WeaponRestorationSpellsMod == null))
            {
                return;
            }

            double minimum = LootTables.CasterMinDamageMod[tier];
            double maximum = LootTables.CasterMaxDamageMod[tier];
            var isWar = IsQuestItemWarCaster(wo);

            if (!isWar)
            {
                minimum = QuestItemMutation.ToLifeCasterScale(minimum);
                maximum = QuestItemMutation.ToLifeCasterScale(maximum);
            }

            var floor = rolled ? 0 : isWar ? wo.ElementalDamageMod ?? wo.WeaponRestorationSpellsMod.Value : wo.WeaponRestorationSpellsMod ?? wo.ElementalDamageMod.Value;
            var tinksElemental = QuestItemCasterTinksElementalDamageMod(wo);
            var tinkPercent = tinksElemental ? Salvage.GreenGarnetTinkPercent : Salvage.LavenderJadeTinkPercent;
            double Tinked(double value) => value + tinks * Salvage.GetModTinkBonus(value, tinkPercent);
            var rolledBase = isWar ? wo.BaseElementalDamageMod ?? wo.BaseWeaponRestorationSpellsMod : wo.BaseWeaponRestorationSpellsMod ?? wo.BaseElementalDamageMod;

            ranges.Add(new(
                tinksElemental ? "Elemental Damage Bonus" : "Restoration Healing Bonus",
                Percent(Tinked(QuestItemMutation.RollMainStat(floor, minimum, maximum, 0))),
                Percent(Tinked(QuestItemMutation.RollMainStat(floor, minimum, maximum, 1))),
                true,
                Quality(rolled, rolledBase, minimum, maximum)
            ));
        }
        else if (wo.WeenieType == WeenieType.MissileLauncher)
        {
            if (wo.DamageMod == null || !LootTables.IsMissileLauncherSubtype(subtype))
            {
                return;
            }

            double minimum = LootTables.GetMissileCasterSubtypeMinimumDamage(subtype, tier);
            var maximum = minimum + LootTables.GetMissileCasterSubtypeDamageRange(subtype, tier);
            var floor = rolled ? 0 : wo.DamageMod.Value;
            double Tinked(double value) => value + tinks * Salvage.GetModTinkBonus(value, Salvage.MahoganyTinkPercent);

            ranges.Add(new(
                "Damage Modifier",
                Percent(Tinked(QuestItemMutation.RollMainStat(floor, minimum, maximum, 0))),
                Percent(Tinked(QuestItemMutation.RollMainStat(floor, minimum, maximum, 1))),
                true,
                Quality(rolled, wo.BaseDamageMod, minimum, maximum)
            ));
        }
        else if (IsQuestItemDamageWeapon(wo, out _))
        {
            var (minimum, maximum) = GetQuestItemDamageRange(subtype, tier);
            var floor = rolled ? 0 : wo.Damage.Value;
            int Tinked(int value) => value + tinks * QuestItemMutation.GetPercentTinkBonus(value, Salvage.IronTinkPercent);

            ranges.Add(new(
                "Damage",
                Tinked(QuestItemMutation.RollWholeMainStat(floor, minimum, maximum, 0)),
                Tinked(QuestItemMutation.RollWholeMainStat(floor, minimum, maximum, 1)),
                false,
                rolled ? wo.QuestItemRollQuality : null
            ));
        }
    }

    private static void AddQuestItemWeaponModRanges(WorldObject wo, Weenie authored, int tier, bool rolled, List<QuestItemRollRange> ranges)
    {
        if (authored == null)
        {
            return;
        }

        // Upgrade Kits shift the attack, defense and magic skill mods by the tier bonus difference
        var authoredTier = GetQuestItemAuthoredTier(wo, authored);
        double Shift(float[] bonusPerTier) => bonusPerTier[tier] - bonusPerTier[authoredTier];

        static double Bonus(double mod) => (mod - 1) * 100;
        static double Skill(double mod) => mod * 100;

        AddQuestItemFlatRange(ranges, "Bonus to Attack Skill", wo.WeaponOffense, authored.GetProperty(PropertyFloat.WeaponOffense), Shift(LootTables.WeaponOffenseModBonusPerTier), WeaponModMaxBonus, rolled, Bonus);
        AddQuestItemFlatRange(ranges, "Bonus to Physical Defense", wo.WeaponPhysicalDefense, authored.GetProperty(PropertyFloat.WeaponPhysicalDefense), Shift(LootTables.WeaponDefenseModBonusPerTier), WeaponModMaxBonus, rolled, Bonus);
        AddQuestItemFlatRange(ranges, "Bonus to Magic Defense", wo.WeaponMagicalDefense, authored.GetProperty(PropertyFloat.WeaponMagicalDefense), Shift(LootTables.WeaponDefenseModBonusPerTier), WeaponModMaxBonus, rolled, Bonus);
        AddQuestItemFlatRange(ranges, "Bonus to Life Magic Skill", wo.WeaponLifeMagicMod, authored.GetProperty(PropertyFloat.WeaponLifeMagicMod), Shift(LootTables.WeaponSkillModBonusPerTier), WeaponModMaxBonus, rolled, Skill);
        AddQuestItemFlatRange(ranges, "Bonus to War Magic Skill", wo.WeaponWarMagicMod, authored.GetProperty(PropertyFloat.WeaponWarMagicMod), Shift(LootTables.WeaponSkillModBonusPerTier), WeaponModMaxBonus, rolled, Skill);

        AddQuestItemFlatRange(ranges, "Biting Strike", wo.CriticalFrequency, authored.GetProperty(PropertyFloat.CriticalFrequency), 0, QuestCritFrequencyBonusRange, rolled, value => (value - 0.1) * 100);
        AddQuestItemFlatRange(ranges, "Crushing Blow", wo.GetProperty(PropertyFloat.CriticalMultiplier), authored.GetProperty(PropertyFloat.CriticalMultiplier), 0, QuestCritMultiplierBonusRange, rolled, Bonus);

        // cleaving rolls downward: less armor or ward left after the strike
        AddQuestItemFlatRange(ranges, "Armor Cleaving", wo.IgnoreArmor, authored.GetProperty(PropertyFloat.IgnoreArmor), 0, -QuestIgnoreArmorBonusRange, rolled, value => (1 - value) * 100);
        AddQuestItemFlatRange(ranges, "Ward Cleaving", wo.IgnoreWard, authored.GetProperty(PropertyFloat.IgnoreWard), 0, -QuestIgnoreWardBonusRange, rolled, value => (1 - value) * 100);
    }

    /// <summary>
    /// A secondary stat rolls from its authored value (plus any Upgrade Kit shift) by up to span. Shown as percents.
    /// </summary>
    private static void AddQuestItemFlatRange(
        List<QuestItemRollRange> ranges,
        string stat,
        double? current,
        double? authoredValue,
        double tierShift,
        double span,
        bool rolled,
        Func<double, double> toPercent
    )
    {
        if (current == null || authoredValue == null)
        {
            return;
        }

        var start = authoredValue.Value + tierShift;
        var end = start + span;
        double? quality = rolled ? Math.Clamp((current.Value - start) / span, 0, 1) : null;

        ranges.Add(new(stat, Math.Min(toPercent(start), toPercent(end)), Math.Max(toPercent(start), toPercent(end)), true, quality));
    }

    /// <summary>
    /// The tier the item was authored at, before any Upgrade Kit.
    /// </summary>
    private static int GetQuestItemAuthoredTier(WorldObject wo, Weenie authored)
    {
        return QuestItemMutation.GetTierIndex(UsesRequiredLevelTiering(wo), authored.GetProperty(PropertyInt.WieldDifficulty));
    }

    private static bool IsQuestItemWeapon(WorldObject wo)
    {
        return wo.ItemType is ItemType.Weapon or ItemType.MissileWeapon or ItemType.MeleeWeapon or ItemType.Caster;
    }

    private static bool IsQuestItemArmor(WorldObject wo)
    {
        return wo.ItemType is ItemType.Armor or ItemType.Clothing or ItemType.Jewelry;
    }

    /// <summary>
    /// Rolls a weapon's main stat for its tier - Damage, a launcher's DamageMod, or a caster's elemental or
    /// restoration mod - never below its current value. Returns false if it has none to roll.
    /// </summary>
    private static bool RollQuestItemWeaponMainStat(WorldObject wo, LootTables.WeaponSubtype weaponSubtype, int tier, float lootQuality)
    {
        if (wo.WeenieType == WeenieType.Caster)
        {
            return RollQuestItemCasterMainStat(wo, weaponSubtype, tier, lootQuality);
        }

        if (wo.WeenieType == WeenieType.MissileLauncher)
        {
            if (wo.DamageMod == null)
            {
                return false;
            }

            if (!LootTables.IsMissileLauncherSubtype(weaponSubtype))
            {
                _log.Warning("MutateQuestItem() - {Name} ({Wcid}) is a missile launcher with WeaponSubtype {WeaponSubtype}.", wo.Name, wo.WeenieClassId, weaponSubtype);
                return false;
            }

            var minimumDamageMod = LootTables.GetMissileCasterSubtypeMinimumDamage(weaponSubtype, tier);
            var maximumDamageMod = minimumDamageMod + LootTables.GetMissileCasterSubtypeDamageRange(weaponSubtype, tier);

            wo.DamageMod = QuestItemMutation.RollMainStat(wo.DamageMod.Value, minimumDamageMod, maximumDamageMod, GetDiminishingRoll(null, lootQuality));
            return true;
        }

        // melee and thrown weapons roll Damage only, even when the weenie also carries a DamageMod
        if (wo.Damage == null)
        {
            return false;
        }

        if (!LootTables.IsMeleeOrThrownSubtype(weaponSubtype))
        {
            _log.Warning("MutateQuestItem() - {Name} ({Wcid}) has Damage but WeaponSubtype {WeaponSubtype}.", wo.Name, wo.WeenieClassId, weaponSubtype);
            return false;
        }

        var (minimumDamage, maximumDamage) = GetQuestItemDamageRange(weaponSubtype, tier);
        var roll = GetDiminishingRoll(null, lootQuality);

        wo.Damage = QuestItemMutation.RollWholeMainStat(wo.Damage.Value, minimumDamage, maximumDamage, roll);
        wo.QuestItemRollQuality = roll;
        return true;
    }

    private static (int Minimum, int Maximum) GetQuestItemDamageRange(LootTables.WeaponSubtype weaponSubtype, int tier)
    {
        var minimum = LootTables.GetMeleeSubtypeMinimumDamage(weaponSubtype, tier);

        return (minimum, minimum + LootTables.GetMeleeSubtypeDamageRange(weaponSubtype, tier));
    }

    private static bool RollQuestItemCasterMainStat(WorldObject wo, LootTables.WeaponSubtype weaponSubtype, int tier, float lootQuality)
    {
        if (wo.ElementalDamageMod == null && wo.WeaponRestorationSpellsMod == null)
        {
            return false;
        }

        if (weaponSubtype != LootTables.WeaponSubtype.Caster)
        {
            _log.Warning("MutateQuestItem() - {Name} ({Wcid}) is a caster with WeaponSubtype {WeaponSubtype}.", wo.Name, wo.WeenieClassId, weaponSubtype);
            return false;
        }

        double minimum = LootTables.CasterMinDamageMod[tier];
        double maximum = LootTables.CasterMaxDamageMod[tier];
        var roll = GetDiminishingRoll(null, lootQuality);

        if (IsQuestItemWarCaster(wo))
        {
            var rolled = QuestItemMutation.RollMainStat(wo.ElementalDamageMod ?? wo.WeaponRestorationSpellsMod.Value, minimum, maximum, roll);

            if (wo.ElementalDamageMod == null)
            {
                wo.WeaponRestorationSpellsMod = rolled;
                return true;
            }

            wo.ElementalDamageMod = rolled;

            if (wo.WeaponRestorationSpellsMod != null)
            {
                wo.WeaponRestorationSpellsMod = QuestItemMutation.GetWarCasterRestorationMod(rolled);
            }

            return true;
        }

        // life casters are authored on the life scale, with elemental matching restoration as on loot
        var lifeRolled = QuestItemMutation.RollMainStat(
            wo.WeaponRestorationSpellsMod ?? wo.ElementalDamageMod.Value,
            QuestItemMutation.ToLifeCasterScale(minimum),
            QuestItemMutation.ToLifeCasterScale(maximum),
            roll
        );

        if (wo.WeaponRestorationSpellsMod != null)
        {
            wo.WeaponRestorationSpellsMod = lifeRolled;
        }

        if (wo.ElementalDamageMod != null)
        {
            wo.ElementalDamageMod = lifeRolled;
        }

        return true;
    }

    private static bool IsQuestItemWarCaster(WorldObject wo)
    {
        return wo.WieldSkillType2 == (int)Skill.WarMagic;
    }

    /// <summary>
    /// War casters take their tinks on the elemental mod (Green Garnet), life casters on the restoration mod
    /// (Lavender Jade) - or on the other one, if that's all the caster has.
    /// </summary>
    private static bool QuestItemCasterTinksElementalDamageMod(WorldObject wo)
    {
        return IsQuestItemWarCaster(wo) ? wo.ElementalDamageMod != null : wo.WeaponRestorationSpellsMod == null;
    }

    private static bool RollQuestItemArmorLevel(WorldObject wo, int tier, float lootQuality)
    {
        // Armor Level 0 means the item deliberately carries no physical armor
        if (wo.ArmorLevel is null or 0)
        {
            return false;
        }

        var (minimum, maximum) = GetQuestItemArmorLevelRange(wo, tier);
        var roll = GetDiminishingRoll(null, lootQuality);

        wo.ArmorLevel = QuestItemMutation.RollWholeMainStat(wo.ArmorLevel.Value, minimum, maximum, roll);
        wo.QuestItemRollQuality = roll;
        return true;
    }

    private static (int Minimum, int Maximum) GetQuestItemArmorLevelRange(WorldObject wo, int tier)
    {
        return QuestItemMutation.GetArmorLevelRange(LootTables.GetArmorStyleBaseArmorLevel(wo.ArmorStyle), tier);
    }

    private static bool RollQuestItemWardLevel(WorldObject wo, int tier, float lootQuality)
    {
        if (wo.WardLevel is null or 0)
        {
            return false;
        }

        var (minimum, maximum) = GetQuestItemWardLevelRange(wo, tier);
        var roll = GetDiminishingRoll(null, lootQuality);

        wo.WardLevel = QuestItemMutation.RollWholeMainStat(wo.WardLevel.Value, minimum, maximum, roll);
        wo.QuestItemWardRollQuality = roll;
        return true;
    }

    private static (int Minimum, int Maximum) GetQuestItemWardLevelRange(WorldObject wo, int tier)
    {
        return wo.ItemType == ItemType.Jewelry
            ? QuestItemMutation.GetJewelryWardLevelRange(tier, wo.ValidLocations is EquipMask.NeckWear)
            : QuestItemMutation.GetWardLevelRange(
                LootTables.GetArmorStyleBaseWardLevel(wo.ArmorStyle, wo.ArmorWeightClass),
                tier,
                wo.ArmorSlots ?? 1
            );
    }

    /// <summary>
    /// How far each of a quest item's whole-number main stats sits from what its stored roll quality gives at its
    /// tier - the rounding, plus any authored floor or protection normalization. Null where nothing was stored.
    /// </summary>
    internal readonly record struct QuestItemRollOffsets(double? Damage, double? ArmorLevel, double? WardLevel);

    /// <summary>
    /// Call on the untinkered stats before an Upgrade Kit rescales them; ApplyQuestItemRollOffsets then re-rolls each
    /// stat at the new tier from its stored quality, which a rounded whole number can't carry on its own.
    /// </summary>
    internal static QuestItemRollOffsets CaptureQuestItemRollOffsets(WorldObject wo, int tier)
    {
        double? damage = null, armorLevel = null, wardLevel = null;

        if (wo.QuestItemRollQuality is { } quality)
        {
            if (IsQuestItemDamageWeapon(wo, out var subtype))
            {
                var (minimum, maximum) = GetQuestItemDamageRange(subtype, tier);
                damage = wo.Damage.Value - QuestItemMutation.GetRollValue(minimum, maximum, quality);
            }
            else if (IsQuestItemArmor(wo) && wo.ArmorLevel is > 0)
            {
                var (minimum, maximum) = GetQuestItemArmorLevelRange(wo, tier);
                armorLevel = wo.ArmorLevel.Value - QuestItemMutation.GetRollValue(minimum, maximum, quality);
            }
        }

        if (wo.QuestItemWardRollQuality is { } wardQuality && IsQuestItemArmor(wo) && wo.WardLevel is > 0)
        {
            var (minimum, maximum) = GetQuestItemWardLevelRange(wo, tier);
            wardLevel = wo.WardLevel.Value - QuestItemMutation.GetRollValue(minimum, maximum, wardQuality);
        }

        return new(damage, armorLevel, wardLevel);
    }

    internal static void ApplyQuestItemRollOffsets(WorldObject wo, int tier, QuestItemRollOffsets offsets)
    {
        static int Round(double value) => (int)Math.Round(value, MidpointRounding.AwayFromZero);

        if (offsets.Damage is { } damage && IsQuestItemDamageWeapon(wo, out var subtype))
        {
            var (minimum, maximum) = GetQuestItemDamageRange(subtype, tier);
            wo.Damage = Round(QuestItemMutation.GetRollValue(minimum, maximum, wo.QuestItemRollQuality.Value) + damage);
        }

        if (offsets.ArmorLevel is { } armorLevel)
        {
            var (minimum, maximum) = GetQuestItemArmorLevelRange(wo, tier);
            wo.ArmorLevel = Round(QuestItemMutation.GetRollValue(minimum, maximum, wo.QuestItemRollQuality.Value) + armorLevel);
        }

        if (offsets.WardLevel is { } wardLevel)
        {
            var (minimum, maximum) = GetQuestItemWardLevelRange(wo, tier);
            wo.WardLevel = Round(QuestItemMutation.GetRollValue(minimum, maximum, wo.QuestItemWardRollQuality.Value) + wardLevel);
        }
    }

    private static bool IsQuestItemDamageWeapon(WorldObject wo, out LootTables.WeaponSubtype subtype)
    {
        subtype = (LootTables.WeaponSubtype)(wo.WeaponSubtype ?? 0);

        return IsQuestItemWeapon(wo)
            && wo.WeenieType is not (WeenieType.Caster or WeenieType.MissileLauncher)
            && wo.Damage != null
            && LootTables.IsMeleeOrThrownSubtype(subtype);
    }

    /// <summary>
    /// Armor with a weight class rolls its skill mods over the span a loot piece would. Jewelry and clothing without
    /// a weight class have no loot counterpart and keep a flat span.
    /// </summary>
    private static float GetQuestItemArmorModBonusRange(WorldObject wo, int armorSlots)
    {
        if (wo.ArmorWeightClass is (int)ArmorWeightClass.Cloth or (int)ArmorWeightClass.Light or (int)ArmorWeightClass.Heavy)
        {
            return QuestItemMutation.GetArmorModBonusRange(armorSlots, wo.ValidLocations is EquipMask.HeadWear);
        }

        return 0.1f / armorSlots;
    }

    /// <summary>
    /// Records a quest item's untinkered main stats in their Base properties, then bakes in the given number of tinks.
    /// </summary>
    private static void SetQuestItemTinks(WorldObject wo, int tinks)
    {
        if (IsQuestItemWeapon(wo))
        {
            if (wo.WeenieType == WeenieType.Caster)
            {
                wo.BaseElementalDamageMod = wo.ElementalDamageMod;
                wo.BaseWeaponRestorationSpellsMod = wo.WeaponRestorationSpellsMod;
            }
            else if (wo.WeenieType == WeenieType.MissileLauncher)
            {
                wo.BaseDamageMod = wo.DamageMod;
            }
            else
            {
                wo.BaseDamage = wo.Damage;
            }
        }
        else
        {
            wo.BaseArmor = wo.ArmorLevel is > 0 ? wo.ArmorLevel : null;
            wo.BaseWard = wo.WardLevel is > 0 ? wo.WardLevel : null;
        }

        AddQuestItemTinkBonus(wo, tinks);
        wo.QuestItemTinks = tinks;
    }

    /// <summary>
    /// Adds the main-stat bonus of the given number of tinks, sized off the Base snapshots the same way real tinks
    /// are; a negative count takes it back off.
    /// </summary>
    private static void AddQuestItemTinkBonus(WorldObject wo, int tinks)
    {
        if (IsQuestItemWeapon(wo))
        {
            if (wo.WeenieType == WeenieType.Caster)
            {
                if (QuestItemCasterTinksElementalDamageMod(wo))
                {
                    wo.ElementalDamageMod += tinks * Salvage.GetModTinkBonus(wo.BaseElementalDamageMod, Salvage.GreenGarnetTinkPercent);
                }
                else
                {
                    wo.WeaponRestorationSpellsMod += tinks * Salvage.GetModTinkBonus(wo.BaseWeaponRestorationSpellsMod, Salvage.LavenderJadeTinkPercent);
                }
            }
            else if (wo.WeenieType == WeenieType.MissileLauncher)
            {
                wo.DamageMod += tinks * Salvage.GetModTinkBonus(wo.BaseDamageMod, Salvage.MahoganyTinkPercent);
            }
            else if (wo.BaseDamage != null)
            {
                wo.Damage += tinks * QuestItemMutation.GetPercentTinkBonus(wo.BaseDamage.Value, Salvage.IronTinkPercent);
            }

            return;
        }

        if (wo.BaseArmor is > 0)
        {
            wo.ArmorLevel += tinks * QuestItemMutation.GetPercentTinkBonus(wo.BaseArmor.Value, Salvage.IronTinkPercent);
        }

        if (wo.BaseWard is > 0)
        {
            // ward scales with armor slots, so armor gets Silver's ward once per slot
            wo.WardLevel += wo.ItemType == ItemType.Jewelry
                ? tinks * Salvage.WhiteJadeTinkWardLevel
                : tinks * Salvage.SilverTinkWardLevel * (wo.ArmorSlots ?? 1);
        }
    }
}
