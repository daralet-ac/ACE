using System;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
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

        var minimumDamage = LootTables.GetMeleeSubtypeMinimumDamage(weaponSubtype, tier);
        var maximumDamage = minimumDamage + LootTables.GetMeleeSubtypeDamageRange(weaponSubtype, tier);

        wo.Damage = QuestItemMutation.RollWholeMainStat(wo.Damage.Value, minimumDamage, maximumDamage, GetDiminishingRoll(null, lootQuality));
        return true;
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

        var (minimum, maximum) = QuestItemMutation.GetArmorLevelRange(LootTables.GetArmorStyleBaseArmorLevel(wo.ArmorStyle), tier);

        wo.ArmorLevel = QuestItemMutation.RollWholeMainStat(wo.ArmorLevel.Value, minimum, maximum, GetDiminishingRoll(null, lootQuality));
        return true;
    }

    private static bool RollQuestItemWardLevel(WorldObject wo, int tier, float lootQuality)
    {
        if (wo.WardLevel is null or 0)
        {
            return false;
        }

        var (minimum, maximum) = wo.ItemType == ItemType.Jewelry
            ? QuestItemMutation.GetJewelryWardLevelRange(tier, wo.ValidLocations is EquipMask.NeckWear)
            : QuestItemMutation.GetWardLevelRange(
                LootTables.GetArmorStyleBaseWardLevel(wo.ArmorStyle, wo.ArmorWeightClass),
                tier,
                wo.ArmorSlots ?? 1
            );

        wo.WardLevel = QuestItemMutation.RollWholeMainStat(wo.WardLevel.Value, minimum, maximum, GetDiminishingRoll(null, lootQuality));
        return true;
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
                    wo.ElementalDamageMod += tinks * Salvage.GreenGarnetTinkElementalDamageMod;
                }
                else
                {
                    wo.WeaponRestorationSpellsMod += tinks * Salvage.LavenderJadeTinkRestorationMod;
                }
            }
            else if (wo.WeenieType == WeenieType.MissileLauncher)
            {
                wo.DamageMod += tinks * Salvage.MahoganyTinkDamageMod;
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
