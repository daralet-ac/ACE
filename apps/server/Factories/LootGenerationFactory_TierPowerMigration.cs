using System;
using System.Collections.Generic;
using System.Linq;
using ACE.Database;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Entity.Models;
using ACE.Server.Entity;
using ACE.Server.WorldObjects;

namespace ACE.Server.Factories;

/// <summary>
/// Moves an item rolled before the tier rework onto the new scale (TierPower): each rolled stat keeps its place in its
/// tier's range, and its tinks keep their share of the untinkered value. Flat ward tinks (Silver +3, Pyreal +2,
/// White Jade +1) come off and go back on as the share each now adds, and Pyreal's armor share drops from 5% to 3%.
/// Maces and staves take their weenies' lower variance (0.75 to 0.6), keeping their own share of it. Tier 1 didn't change. Run once, with migrate-tier-power, after the world-db import; a migrated item is marked so a
/// second run skips it.
/// </summary>
public static partial class LootGenerationFactory
{
    // the flat ward each tink added before the tier rework
    private const int PreviousSilverTinkWard = 3;
    private const int PreviousPyrealTinkWard = 2;
    private const int PreviousWhiteJadeTinkWard = 1;
    private const double PreviousPyrealTinkArmorPercent = 0.05;

    /// <summary>
    /// The tier an item rolled at: its loot tier, or a rolled quest item's tier from its wield requirement.
    /// 0 for an item the migration leaves alone.
    /// </summary>
    internal static int GetTierPowerMigrationTier(WorldObject wo)
    {
        if (wo.Tier is > 0)
        {
            return wo.Tier.Value;
        }

        if (wo.QuestItemRollQuality != null)
        {
            return GetQuestItemTierIndex(wo) + 1;
        }

        return 0;
    }

    /// <summary>
    /// Migrates one item, adding a line to changes for each stat it moves. Returns false if the item was already
    /// migrated or has nothing to migrate. Ammo takes its weenie's damage, so it needs no tier.
    /// </summary>
    internal static bool MigrateToTierPower(WorldObject wo, List<string> changes)
    {
        if (wo.GetProperty(PropertyBool.TierPowerMigrated) == true)
        {
            return false;
        }

        var before = changes.Count;

        if (wo.WeenieType == WeenieType.Ammunition)
        {
            MigrateAmmo(wo, changes);
        }
        else
        {
            var tier = GetTierPowerMigrationTier(wo);
            var quest = wo.Tier is not > 0;
            var meleeOrThrown = wo.WeenieType is not (WeenieType.Caster or WeenieType.MissileLauncher)
                && (wo.ItemType == ItemType.MeleeWeapon || wo.WeenieType == WeenieType.Missile);

            // a weapon whose weenie's speed or variance changed takes the new one, whatever its tier
            var previousWeaponTime = MigrateWeaponSpeed(wo, changes);
            var previousVariance = MigrateWeaponVariance(wo, changes);

            // melee and thrown weapons rolled tier 1 in a narrower band too
            if (meleeOrThrown && tier >= 1)
            {
                MigrateWeaponDamage(wo, tier, quest, previousWeaponTime, previousVariance, changes);
            }

            if (tier >= 2)
            {
                switch (wo.WeenieType)
                {
                    case WeenieType.Caster:
                        MigrateCaster(wo, tier, changes);
                        break;
                    case WeenieType.MissileLauncher:
                        MigrateLauncher(wo, tier, changes);
                        break;
                    default:
                        if (wo.ItemType == ItemType.Jewelry)
                        {
                            MigrateJewelryWard(wo, tier, quest, changes);
                        }
                        else if (wo.ItemType is ItemType.Armor or ItemType.Clothing)
                        {
                            MigrateArmorLevel(wo, tier, quest, changes);
                            MigrateArmorWard(wo, tier, quest, changes);
                        }
                        break;
                }
            }

            if (changes.Count == before && tier < 2)
            {
                return false;
            }
        }

        wo.SetProperty(PropertyBool.TierPowerMigrated, true);

        return changes.Count > before;
    }

    /// <summary>
    /// The WeaponTime of the weenies whose speed the rework changed, before it (world-db): the two-handers' loot and
    /// quest weapons, and the thrown loot weapons
    /// </summary>
    internal static readonly Dictionary<uint, int> PreviousWeenieWeaponTime = new[]
        {
            (83, new uint[]
            {
                40618, 40619, 40620, 40621, 40622, 40760, 40761, 40762, 40763, 40764, 40818, 40819, 40820, 40821, 40822,
                41036, 41037, 41038, 41039, 41040, 41046, 41047, 41048, 41049, 41050, 41067, 41068, 41069, 41070, 41071,
                1054204, // Warden's Greatsword
            }),
            (95, new uint[]
            {
                40623, 40624, 40625, 40626, 40627, 40635, 40636, 40637, 40638, 40639, 41041, 41042, 41043, 41044, 41045,
                41052, 41053, 41054, 41055, 41056, 41057, 41058, 41059, 41060, 41061, 41062, 41063, 41064, 41065, 41066,
            }),
            (111, new uint[] { 317, 3790, 3791, 3792, 3793 }),
            (112, new uint[] { 315, 343, 3782, 3783, 3784, 3785, 3786, 3787, 3788, 3789, 3861, 3862, 3863, 3864 }),
            (120, new uint[] { 304, 310, 316, 320, 3758, 3759, 3760, 3761, 3770, 3771, 3772, 3773, 3798, 3799, 3800, 3801 }),
        }
        .SelectMany(group => group.Item2.Select(wcid => (Wcid: wcid, WeaponTime: group.Item1)))
        .ToDictionary(entry => entry.Wcid, entry => entry.WeaponTime);

    /// <summary>
    /// A weapon whose weenie's speed changed takes the new speed, keeping its own share of it (loot's +-5% roll, Iron
    /// tinks). Returns the weenie's speed before the rework, which the weapon's damage was rolled for.
    /// </summary>
    private static int MigrateWeaponSpeed(WorldObject wo, List<string> changes)
    {
        // an item restored from the shard has no Weenie of its own
        var weenie = wo.Weenie ?? DatabaseManager.World?.GetCachedWeenie(wo.WeenieClassId);
        var weenieTime = weenie?.GetProperty(PropertyInt.WeaponTime) ?? 0;

        if (!PreviousWeenieWeaponTime.TryGetValue(wo.WeenieClassId, out var previousTime) || weenieTime <= 0 || weenieTime == previousTime)
        {
            return weenieTime > 0 ? weenieTime : wo.WeaponTime ?? 0;
        }

        var ratio = (double)weenieTime / previousTime;

        if (wo.WeaponTime is > 0)
        {
            var weaponTime = (int)Math.Round(wo.WeaponTime.Value * ratio);
            changes.Add($"WeaponTime {wo.WeaponTime} => {weaponTime}");
            wo.WeaponTime = weaponTime;
        }

        if (wo.BaseWeaponTime is > 0)
        {
            wo.BaseWeaponTime = (int)Math.Round(wo.BaseWeaponTime.Value * ratio);
        }

        return previousTime;
    }

    /// <summary>
    /// The DamageVariance of the weenies whose variance the rework changed, before it (world-db): the mace and staff
    /// loot weapons, lowered to the axes' and daggers' 0.6 so every crit weapon type gets the same share of its damage
    /// from critical hits
    /// </summary>
    internal static readonly Dictionary<uint, double> PreviousWeenieDamageVariance = new uint[]
        {
            309, 325, 331, 332, 356, 3766, 3767, 3768, 3769, 3774, 3775, 3776, 3777, 3814, 3815, 3816, 3817, 3834, 3835,
            3836, 3837, 3897, 3898, 3899, 3900, 3937, 3938, 3939, 3940, 7768, 7787, 7788, 7789, 7790, // maces
            7771, 7795, 7796, 7797, 7798, 22154, 22155, 22156, 22157, 22158, 22159, 22160, 22161, 22162, 22163, 22164,
            22165, 22166, 22167, 22168, // staves
        }
        .ToDictionary(wcid => wcid, _ => 0.75);

    /// <summary>
    /// A weapon whose weenie's variance changed takes the new variance, keeping its own share of it (loot's +-0.1 roll,
    /// Granite tinks). Returns the weenie's variance before the rework, which the weapon's damage was rolled for, or
    /// null if it didn't change.
    /// </summary>
    private static double? MigrateWeaponVariance(WorldObject wo, List<string> changes)
    {
        // an item restored from the shard has no Weenie of its own
        var weenie = wo.Weenie ?? DatabaseManager.World?.GetCachedWeenie(wo.WeenieClassId);
        var weenieVariance = weenie?.GetProperty(PropertyFloat.DamageVariance) ?? 0.0;

        if (
            !PreviousWeenieDamageVariance.TryGetValue(wo.WeenieClassId, out var previousVariance)
            || weenieVariance <= 0
            || Math.Abs(weenieVariance - previousVariance) < 0.0001
        )
        {
            return null;
        }

        if (wo.DamageVariance is > 0)
        {
            var variance = Math.Round(wo.DamageVariance.Value * weenieVariance / previousVariance, 4);
            changes.Add($"DamageVariance {wo.DamageVariance:0.###} => {variance:0.###}");
            wo.DamageVariance = variance;
        }

        return previousVariance;
    }

    /// <summary>
    /// Melee and thrown weapons: the untinkered damage keeps its place in its tier's range, from the 25% band it rolled
    /// in at its old speed and variance to the x1-x2 range at its new ones (GetMeleeDamageRollRange), with its tinks'
    /// share on top. A rolled quest weapon rolls again from its stored roll quality.
    /// </summary>
    private static void MigrateWeaponDamage(
        WorldObject wo,
        int tier,
        bool quest,
        int previousWeaponTime,
        double? previousVariance,
        List<string> changes
    )
    {
        if (wo.Damage is not > 0)
        {
            return;
        }

        var newRange = GetMeleeDamageRollRange(wo, tier);
        if (newRange.Maximum <= 0)
        {
            return;
        }

        var baseDamage = wo.BaseDamage is > 0 ? wo.BaseDamage.Value : wo.Damage.Value;
        double newBaseDamage;

        if (quest && wo.QuestItemRollQuality is { } quality)
        {
            newBaseDamage = QuestItemMutation.GetRollValue(Math.Round(newRange.Minimum), Math.Round(newRange.Maximum), quality);
        }
        else
        {
            var previousRange = GetPreviousMeleeDamageRange(wo, tier, previousWeaponTime, previousVariance);
            var position = Math.Clamp(TierPower.GetRollPosition(baseDamage, previousRange), 0.0, 1.0);
            newBaseDamage = newRange.Minimum + (newRange.Maximum - newRange.Minimum) * position;
        }

        var damage = (int)Math.Round(ApplyTinkShare(wo.Damage.Value, baseDamage, newBaseDamage), MidpointRounding.AwayFromZero);

        changes.Add($"Damage {wo.Damage} => {damage}");
        wo.Damage = damage;

        if (wo.BaseDamage is > 0)
        {
            wo.BaseDamage = (int)Math.Round(newBaseDamage, MidpointRounding.AwayFromZero);
        }
    }

    /// <summary>
    /// Launchers: the untinkered damage mod by its place in the previous range, onto the multiplier ladder
    /// </summary>
    private static void MigrateLauncher(WorldObject wo, int tier, List<string> changes)
    {
        if (wo.DamageMod == null)
        {
            return;
        }

        var previousRange = GetPreviousLauncherDamageModRange(wo.WeaponSkill, tier, GetWeaponHitsPerSecond(wo, tier));
        var (minimum, maximum) = TierPower.GetMultiplierRange(tier);

        var damageMod = MapTinkedValue(wo.DamageMod.Value, wo.BaseDamageMod, previousRange, (minimum, maximum), out var baseDamageMod);

        changes.Add($"DamageMod {wo.DamageMod:0.###} => {damageMod:0.###}");
        wo.DamageMod = damageMod;

        if (wo.BaseDamageMod is > 0)
        {
            wo.BaseDamageMod = baseDamageMod;
        }
    }

    /// <summary>
    /// Casters: the full roll (a life caster kept half of it) by its place in the previous range, onto the
    /// multiplier ladder, with restoration at the same place on its war or Nether ladder
    /// </summary>
    private static void MigrateCaster(WorldObject wo, int tier, List<string> changes)
    {
        var nether = wo.WieldSkillType2 != (int)Skill.WarMagic;
        var previousRange = TierPower.GetPreviousCasterModRange(tier);

        double? baseElemental = wo.BaseElementalDamageMod is > 0 ? wo.BaseElementalDamageMod : wo.ElementalDamageMod;
        double? baseRestoration = wo.BaseWeaponRestorationSpellsMod is > 0 ? wo.BaseWeaponRestorationSpellsMod : wo.WeaponRestorationSpellsMod;

        // the full roll: a war caster's elemental mod, a life caster's half doubled, or from restoration alone
        double fullRoll;
        if (baseElemental != null)
        {
            fullRoll = nether ? 1 + (baseElemental.Value - 1) * 2 : baseElemental.Value;
        }
        else if (baseRestoration != null)
        {
            fullRoll = 1 + (baseRestoration.Value - 1) * (nether ? 2 : 4);
        }
        else
        {
            return;
        }

        var position = TierPower.GetRollPosition(fullRoll, previousRange);
        var (minimum, maximum) = TierPower.GetMultiplierRange(tier);
        var newElemental = minimum + (maximum - minimum) * position;
        var newRestoration = TierPower.GetRestorationForRoll(tier, position, nether);

        if (wo.ElementalDamageMod != null)
        {
            var elemental = ApplyTinkShare(wo.ElementalDamageMod.Value, baseElemental.Value, newElemental);

            changes.Add($"ElementalDamageMod {wo.ElementalDamageMod:0.###} => {elemental:0.###}");
            wo.ElementalDamageMod = elemental;

            if (wo.BaseElementalDamageMod is > 0)
            {
                wo.BaseElementalDamageMod = newElemental;
            }
        }

        if (wo.WeaponRestorationSpellsMod != null)
        {
            var restoration = ApplyTinkShare(wo.WeaponRestorationSpellsMod.Value, baseRestoration.Value, newRestoration);

            changes.Add($"WeaponRestorationSpellsMod {wo.WeaponRestorationSpellsMod:0.###} => {restoration:0.###}");
            wo.WeaponRestorationSpellsMod = restoration;

            if (wo.BaseWeaponRestorationSpellsMod is > 0)
            {
                wo.BaseWeaponRestorationSpellsMod = newRestoration;
            }
        }
    }

    /// <summary>
    /// Body armor's armor level: the untinkered armor by its place in the previous range, onto the doubling range,
    /// with the tinks' share of it and any armor patch on top. Shields didn't change.
    /// </summary>
    private static void MigrateArmorLevel(WorldObject wo, int tier, bool quest, List<string> changes)
    {
        if (wo.ArmorLevel is not > 0 || wo.IsShield)
        {
            return;
        }

        var patch = wo.ArmorPatchAmount ?? 0;
        var armorLevel = wo.ArmorLevel.Value - patch;
        var baseArmor = wo.BaseArmor is > 0 ? wo.BaseArmor.Value : armorLevel;

        // loot armor rolled on its weenie's armor level before the rework, quest armor on its style's
        var (styleArmorLevel, _) = GetArmorBaseLevels(wo);
        // an item restored from the shard has no Weenie of its own
        var weenie = wo.Weenie ?? (quest ? null : DatabaseManager.World?.GetCachedWeenie(wo.WeenieClassId));
        var weenieArmorLevel = weenie?.GetProperty(PropertyInt.ArmorLevel);
        var previousBase = quest ? LootTables.GetArmorStyleBaseArmorLevel(wo.ArmorStyle) : weenieArmorLevel ?? 75;
        var newBase = quest ? LootTables.GetArmorStyleBaseArmorLevel(wo.ArmorStyle) : styleArmorLevel ?? weenieArmorLevel ?? 75;

        // effective armor counts the protections: an item whose protections were tinked up keeps that edge
        var protection = GetAverageProtection(wo);
        var newBaseArmor = TierPower.MapToRange(
            baseArmor * protection,
            TierPower.GetPreviousArmorRollRange(previousBase, tier),
            TierPower.GetArmorRollRange(newBase, tier)
        ) / protection;

        // Pyreal now adds 3% of the untinkered armor, not 5%
        var pyreals = quest ? 0 : new TinkerLog(wo.TinkerLog).NumTinkers(MaterialType.Pyreal);
        var tinked = armorLevel - baseArmor
            - pyreals * QuestItemMutation.GetPercentTinkBonus(baseArmor, PreviousPyrealTinkArmorPercent)
            + pyreals * QuestItemMutation.GetPercentTinkBonus(baseArmor, Salvage.PyrealTinkArmorPercent);

        var newArmorLevel = (int)Math.Round(newBaseArmor + tinked * newBaseArmor / baseArmor) + patch;

        changes.Add($"ArmorLevel {wo.ArmorLevel} => {newArmorLevel}");
        wo.ArmorLevel = newArmorLevel;

        if (wo.BaseArmor is > 0)
        {
            wo.BaseArmor = (int)Math.Round(newBaseArmor);
        }
    }

    /// <summary>
    /// Armor's (and shields') ward: the untinkered ward by its place in the previous range, onto the doubling range,
    /// with each ward tink re-applied as the share of it the tink now adds
    /// </summary>
    private static void MigrateArmorWard(WorldObject wo, int tier, bool quest, List<string> changes)
    {
        if (wo.WardLevel is not > 0)
        {
            return;
        }

        var armorSlots = wo.ArmorSlots ?? 1;
        var (_, baseWardLevel) = GetArmorBaseLevels(wo);
        if (quest)
        {
            baseWardLevel = LootTables.GetArmorStyleBaseWardLevel(wo.ArmorStyle, wo.ArmorWeightClass);
        }

        var (silver, pyreal, whiteJade) = GetWardTinks(wo, quest);
        var flatTinks = quest
            ? wo.WardLevel.Value - (wo.BaseWard ?? wo.WardLevel.Value)
            : silver * PreviousSilverTinkWard + pyreal * PreviousPyrealTinkWard + whiteJade * PreviousWhiteJadeTinkWard;
        var baseWard = wo.BaseWard is > 0 ? wo.BaseWard.Value : wo.WardLevel.Value - flatTinks;

        var (previousMinimum, previousMaximum) = TierPower.GetPreviousArmorRollRange(baseWardLevel, tier);
        var (minimum, maximum) = TierPower.GetWardRollRange(baseWardLevel, tier);
        var newBaseWard = (int)Math.Round(TierPower.MapToRange(
            baseWard,
            (previousMinimum * armorSlots, previousMaximum * armorSlots),
            (minimum * armorSlots, maximum * armorSlots)
        ));

        var wardLevel = newBaseWard
            + silver * QuestItemMutation.GetPercentTinkBonus(newBaseWard, Salvage.SilverTinkWardPercent)
            + pyreal * QuestItemMutation.GetPercentTinkBonus(newBaseWard, Salvage.PyrealTinkWardPercent)
            + whiteJade * QuestItemMutation.GetPercentTinkBonus(newBaseWard, Salvage.WhiteJadeTinkWardPercent);

        changes.Add($"WardLevel {wo.WardLevel} => {wardLevel}");
        wo.WardLevel = wardLevel;

        if (wo.BaseWard is > 0)
        {
            wo.BaseWard = newBaseWard;
        }
    }

    /// <summary>
    /// Jewelry ward: the untinkered ward by its place in the previous range (a necklace's is double), onto the new
    /// one, with White Jade re-applied as the share it now adds
    /// </summary>
    private static void MigrateJewelryWard(WorldObject wo, int tier, bool quest, List<string> changes)
    {
        if (wo.WardLevel is not > 0)
        {
            return;
        }

        var necklace = wo.ValidLocations == EquipMask.NeckWear ? 2 : 1;
        var t = Math.Clamp(tier, 1, 8);

        var (_, _, whiteJade) = GetWardTinks(wo, quest);
        var flatTinks = quest ? wo.WardLevel.Value - (wo.BaseWard ?? wo.WardLevel.Value) : whiteJade * PreviousWhiteJadeTinkWard;
        var baseWard = wo.BaseWard is > 0 ? wo.BaseWard.Value : wo.WardLevel.Value - flatTinks;

        var newBaseWard = (int)Math.Round(TierPower.MapToRange(
            baseWard,
            (TierPower.GetPreviousJewelryBaseWard(t) * necklace, TierPower.GetPreviousJewelryBaseWard(t + 1) * necklace),
            (TierPower.GetJewelryBaseWard(t) * necklace, TierPower.GetJewelryBaseWard(t + 1) * necklace)
        ));
        newBaseWard = Math.Max(1, newBaseWard);

        var wardLevel = newBaseWard + whiteJade * QuestItemMutation.GetPercentTinkBonus(newBaseWard, Salvage.WhiteJadeTinkWardPercent);

        changes.Add($"WardLevel {wo.WardLevel} => {wardLevel}");
        wo.WardLevel = wardLevel;

        if (wo.BaseWard is > 0)
        {
            wo.BaseWard = newBaseWard;
        }
    }

    /// <summary>
    /// Ammo: a stack takes its weenie's damage, which world-db sets by tier (and which fixes the T4 darts' 32). Run the
    /// migration after the world-db import.
    /// </summary>
    private static void MigrateAmmo(WorldObject wo, List<string> changes)
    {
        // an item restored from the shard has no Weenie of its own
        var weenie = wo.Weenie ?? DatabaseManager.World?.GetCachedWeenie(wo.WeenieClassId);
        var damage = weenie?.GetProperty(PropertyInt.Damage);

        if (wo.Damage is not > 0 || damage is not > 0 || damage == wo.Damage)
        {
            return;
        }

        changes.Add($"Damage {wo.Damage} => {damage}");
        wo.Damage = damage;
    }

    /// <summary>
    /// Silver, Pyreal and White Jade tinks on an item: from its tinker log, or a quest item's baked-in tinks, which are
    /// Silver on armor and White Jade on jewelry
    /// </summary>
    private static (int Silver, int Pyreal, int WhiteJade) GetWardTinks(WorldObject wo, bool quest)
    {
        if (quest)
        {
            var tinks = wo.QuestItemTinks ?? 0;

            return wo.ItemType == ItemType.Jewelry ? (0, 0, tinks) : (tinks, 0, 0);
        }

        var log = new TinkerLog(wo.TinkerLog);

        return (log.NumTinkers(MaterialType.Silver), log.NumTinkers(MaterialType.Pyreal), log.NumTinkers(MaterialType.WhiteJade));
    }

    /// <summary>
    /// The average of an armor piece's seven protections, 1.0 if it's missing any
    /// </summary>
    private static double GetAverageProtection(WorldObject wo)
    {
        double?[] protections =
        [
            wo.ArmorModVsSlash,
            wo.ArmorModVsPierce,
            wo.ArmorModVsBludgeon,
            wo.ArmorModVsAcid,
            wo.ArmorModVsFire,
            wo.ArmorModVsCold,
            wo.ArmorModVsElectric,
        ];

        if (protections.Any(p => p is not > 0))
        {
            return 1.0;
        }

        return protections.Average(p => p.Value);
    }

    /// <summary>
    /// A tinked value moved onto a new range: the untinkered value by its place in the old range, with the tinks'
    /// share of it on top
    /// </summary>
    private static double MapTinkedValue(
        double value,
        double? baseValue,
        (double Minimum, double Maximum) from,
        (double Minimum, double Maximum) to,
        out double newBaseValue
    )
    {
        var untinked = baseValue is > 0 ? baseValue.Value : value;
        newBaseValue = TierPower.MapToRange(untinked, from, to);

        return ApplyTinkShare(value, untinked, newBaseValue);
    }

    /// <summary>
    /// What tinks added to the old untinkered value, scaled to the new one, on top of it
    /// </summary>
    private static double ApplyTinkShare(double value, double oldBase, double newBase)
    {
        return oldBase > 0 ? newBase + (value - oldBase) * newBase / oldBase : newBase;
    }
}
