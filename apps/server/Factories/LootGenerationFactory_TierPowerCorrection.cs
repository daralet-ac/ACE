using System;
using System.Collections.Generic;
using ACE.Database;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Entity.Models;
using ACE.Server.Entity;
using ACE.Server.Factories.Tables;
using ACE.Server.WorldObjects;

namespace ACE.Server.Factories;

/// <summary>
/// Puts back the melee, thrown and launcher damage migrate-tier-power placed too low. Before #1276 (Oct 6, 2026) loot
/// damage rolled from the bottom of its tier's 25% band only up to the band's middle, at the attack speed formula from
/// before #1288 and the weenie speeds from before world-db #1106. The migration read every weapon's roll against the
/// full band at the newer formula, so a top roll landed near the middle of its new range. Both rolls used the same
/// squared random number, so a weapon's place in the half it could reach is as rare as the same place in the full
/// range now: this puts it there. Each weapon's roll comes from a shard backup taken before the migration.
/// </summary>
public static partial class LootGenerationFactory
{
    public enum TierPowerCorrection
    {
        Corrected,
        AlreadyCorrected,
        NoChange,
        ChangedSinceMigration,
        TierChanged,
        NotSameItem,
        UnknownWeenie,
        NotALootWeapon,
        TooOld,
    }

    /// <summary>
    /// A weapon's values in the shard backup from before the migration
    /// </summary>
    public record TierPowerBackupWeapon(
        uint Id,
        uint Wcid,
        int Tier,
        int? Damage,
        int? BaseDamage,
        double? DamageMod,
        double? BaseDamageMod,
        int? CreationTimestamp
    );

    /// <summary>
    /// Weapons created before this (Unix time) are left as the migration made them. Loot rolled on an earlier damage
    /// formula until the cleave overhaul (#676) went live in late Feb 2025; the shard has no weapon after it whose roll
    /// doesn't fit the formula this reads them on.
    /// </summary>
    internal const int CorrectWeaponsCreatedSince = 1740787200; // 2025-03-01 00:00 UTC

    // the reload part of each launcher's attack animation, which alone sped up with the old speed formula
    private const double PreviousBowReload = 0.32;
    private const double PreviousCrossbowReload = 0.26;
    private const double PreviousAtlatlReload = 0.73;
    private const double PreviousThrownReload = 0.9777778;

    /// <summary>
    /// The 25% band a loot weapon's Damage (or a launcher's DamageMod) was built on before the tier rework, at the old
    /// attack speed formula and its weenie's speed and variance then. Before #1276 it rolled from the bottom only up to
    /// the middle. Null if its weenie's old speed isn't known.
    /// </summary>
    internal static (double Minimum, double Middle, double Maximum)? GetPreTimePerHitDamageBand(WorldObject wo, int tier)
    {
        if (!PreTimePerHitWeaponTime.ByWcid.TryGetValue(wo.WeenieClassId, out var weaponTime) || weaponTime <= 0)
        {
            return null;
        }

        var t = Math.Clamp(tier, 1, 8);
        var quickness = AvgQuicknessPerTier[t - 1];
        var speedMod = 1.0 + (1 - weaponTime / 100.0) + quickness / 600.0;
        var targetDps = TierPower.GetPreviousWeaponDps(t);
        var animLength = WeaponAnimationLength.GetWeaponAnimLength(wo);
        double middle;

        if (wo.WeenieType == WeenieType.MissileLauncher)
        {
            var reload = wo.WeaponSkill switch
            {
                Skill.Bow => PreviousBowReload,
                Skill.Crossbow => PreviousCrossbowReload,
                _ => PreviousAtlatlReload,
            };
            var attacksPerSecond = 1 / (animLength - reload + reload / speedMod);

            var ammoMaxDamage = TierPower.GetPreviousAmmoMaxDamage(wo.WeaponSkill, t);
            var ammoMinDamage = ammoMaxDamage * (1 - GetAmmoVariance(wo.WeaponSkill));
            var ammoAverageDamage = (ammoMaxDamage + ammoMinDamage) / 2;

            middle = targetDps / attacksPerSecond / (ammoAverageDamage * 0.9 + ammoMaxDamage * 0.2);
        }
        else
        {
            var attacksPerSecond = speedMod / animLength;

            if (wo.IsTwoHanded || wo.W_AttackType == AttackType.DoubleStrike)
            {
                attacksPerSecond *= 2;
            }
            else if (wo.W_AttackType == AttackType.TripleStrike)
            {
                attacksPerSecond *= 3;
            }
            else if (wo.W_WeaponType == WeaponType.Thrown)
            {
                // the old formula multiplied the reload by the speed bonus
                attacksPerSecond = 1 / (animLength - PreviousThrownReload + PreviousThrownReload * speedMod);
            }

            // an item restored from the shard has no Weenie of its own
            var weenie = wo.Weenie ?? DatabaseManager.World?.GetCachedWeenie(wo.WeenieClassId);
            var variance = PreviousWeenieDamageVariance.TryGetValue(wo.WeenieClassId, out var previousVariance)
                ? previousVariance
                : weenie?.GetProperty(PropertyFloat.DamageVariance) ?? wo.DamageVariance ?? 0.0;

            middle = targetDps / attacksPerSecond / ((((1 - variance) + 1) / 2 * 0.9) + 0.2);
        }

        var (minimum, maximum) = GetTierDamageRange(middle);

        return (minimum, middle, maximum);
    }

    /// <summary>
    /// Where a roll sits in the part of its band it could reach, 0 to 1. Whole-number Damage counts from the lowest
    /// to the highest whole number it could roll, so a top roll is 100%, as Roll Quality reads it.
    /// </summary>
    internal static double GetPlaceInReachableBand(double rolled, double minimum, double top, bool wholeNumbers)
    {
        if (wholeNumbers)
        {
            var lowest = Math.Round(minimum);
            var highest = Math.Round(top);

            if (highest > lowest)
            {
                return Math.Clamp((rolled - lowest) / (highest - lowest), 0.0, 1.0);
            }
        }

        return Math.Clamp(TierPower.GetRollPosition(rolled, (minimum, top)), 0.0, 1.0);
    }

    /// <summary>
    /// Moves one weapon's damage to the place it rolled in, from its value in the backup. Leaves it alone, and says
    /// why, if it isn't where the migration would have put it (tinkered, upgraded or changed since).
    /// </summary>
    internal static TierPowerCorrection CorrectTierPowerDamage(WorldObject wo, TierPowerBackupWeapon backup, List<string> changes)
    {
        if (wo.WeenieClassId != backup.Wcid)
        {
            return TierPowerCorrection.NotSameItem;
        }

        if (backup.Tier < 1 || wo.Tier is not > 0 || wo.QuestItemRollQuality != null)
        {
            return TierPowerCorrection.NotALootWeapon;
        }

        if (wo.Tier != backup.Tier)
        {
            changes.Add($"tier {backup.Tier} => {wo.Tier} since the backup");
            return TierPowerCorrection.TierChanged;
        }

        if (backup.CreationTimestamp is not >= CorrectWeaponsCreatedSince)
        {
            return TierPowerCorrection.TooOld;
        }

        var band = GetPreTimePerHitDamageBand(wo, backup.Tier);
        if (band == null)
        {
            changes.Add("its weenie has no WeaponTime from before #1106");
            return TierPowerCorrection.UnknownWeenie;
        }

        if (wo.WeenieType == WeenieType.MissileLauncher)
        {
            return CorrectLauncherDamageMod(wo, backup, band.Value, changes);
        }

        if (wo.WeenieType is WeenieType.Caster || (wo.ItemType != ItemType.MeleeWeapon && wo.WeenieType != WeenieType.Missile))
        {
            return TierPowerCorrection.NotALootWeapon;
        }

        return CorrectMeleeDamage(wo, backup, band.Value, changes);
    }

    private static TierPowerCorrection CorrectMeleeDamage(
        WorldObject wo,
        TierPowerBackupWeapon backup,
        (double Minimum, double Middle, double Maximum) band,
        List<string> changes
    )
    {
        if (wo.Damage is not > 0 || backup.Damage is not > 0)
        {
            return TierPowerCorrection.NotALootWeapon;
        }

        var tier = backup.Tier;
        var newRange = GetMeleeDamageRollRange(wo, tier);
        if (newRange.Maximum <= 0)
        {
            return TierPowerCorrection.NotALootWeapon;
        }

        var rolled = backup.BaseDamage is > 0 ? backup.BaseDamage.Value : backup.Damage.Value;

        // a roll above the middle can only have come after #1276
        var fullBand = rolled > Math.Round(band.Middle);
        var top = fullBand ? band.Maximum : band.Middle;
        var place = GetPlaceInReachableBand(rolled, band.Minimum, top, true);

        var corrected = newRange.Minimum + (newRange.Maximum - newRange.Minimum) * place;
        var correctedWhole = (int)Math.Round(corrected, MidpointRounding.AwayFromZero);

        var migratedRange = newRange;
        var migratedWhole = (int)Math.Round(GetTierPowerMigratedBaseDamage(wo, tier, rolled, null, out _), MidpointRounding.AwayFromZero);
        var liveBase = wo.BaseDamage is > 0 ? wo.BaseDamage.Value : wo.Damage.Value;

        var rolledIn = $"rolled {rolled} in {Math.Round(band.Minimum)}-{Math.Round(top)} ({place:P0}{(fullBand ? ", full band" : "")})";

        if (liveBase == correctedWhole)
        {
            return migratedWhole == correctedWhole ? TierPowerCorrection.NoChange : TierPowerCorrection.AlreadyCorrected;
        }

        // thrown weapons migrated before world-db lowered their weenies' variance kept the old variance and its range
        // (#1315 came too late for them): they take the lower variance now, with their damage at its place
        var takesNewVariance = false;
        if (Math.Abs(liveBase - migratedWhole) > 1 && PreviousWeenieDamageVariance.TryGetValue(wo.WeenieClassId, out var oldVariance))
        {
            var atOldVariance = (int)Math.Round(
                GetTierPowerMigratedBaseDamage(wo, tier, rolled, oldVariance, out var oldVarianceRange),
                MidpointRounding.AwayFromZero
            );

            if (Math.Abs(liveBase - atOldVariance) <= 1)
            {
                migratedWhole = atOldVariance;
                migratedRange = oldVarianceRange;
                takesNewVariance = true;
            }
        }

        if (Math.Abs(liveBase - migratedWhole) > 1)
        {
            changes.Add($"{rolledIn}, migration would have made it {migratedWhole} but it's {liveBase} now");
            return TierPowerCorrection.ChangedSinceMigration;
        }

        if (takesNewVariance)
        {
            MigrateWeaponVariance(wo, changes);
        }

        var damage = (int)Math.Round(ApplyTinkShare(wo.Damage.Value, liveBase, corrected), MidpointRounding.AwayFromZero);
        var migratedPlace = TierPower.GetRollPosition(liveBase, migratedRange);

        changes.Add($"{rolledIn}, migrated to {migratedPlace:P0}: Damage {wo.Damage} => {damage}");
        wo.Damage = damage;

        if (wo.BaseDamage is > 0)
        {
            wo.BaseDamage = correctedWhole;
        }

        return TierPowerCorrection.Corrected;
    }

    /// <summary>
    /// The untinkered Damage migrate-tier-power gave a melee or thrown weapon that rolled this, as it ran: with the
    /// weenie's variance now, or with the one given if it ran while the weenie still had that (both ranges at it)
    /// </summary>
    private static double GetTierPowerMigratedBaseDamage(
        WorldObject wo,
        int tier,
        int rolled,
        double? weenieVarianceThen,
        out (double Minimum, double Maximum) newRange
    )
    {
        // the Weapon Time shard update before it doubled two-handers' damage (one hit per swing)
        var converted = wo.IsTwoHanded && wo.WeenieType == WeenieType.MeleeWeapon ? rolled * 2 : rolled;

        // an item restored from the shard has no Weenie of its own
        var weenie = wo.Weenie ?? DatabaseManager.World?.GetCachedWeenie(wo.WeenieClassId);
        var weenieTime = weenie?.GetProperty(PropertyInt.WeaponTime) ?? wo.WeaponTime ?? 0;
        var previousTime = PreviousWeenieWeaponTime.TryGetValue(wo.WeenieClassId, out var time) ? time : weenieTime;
        double? previousVariance = PreviousWeenieDamageVariance.TryGetValue(wo.WeenieClassId, out var variance) ? variance : null;

        var previousRange = GetPreviousMeleeDamageRange(wo, tier, previousTime, weenieVarianceThen ?? previousVariance);
        newRange = weenieVarianceThen is { } varianceThen
            ? GetMeleeDamageRollRange(weenieTime, varianceThen, wo.IsRanged, tier)
            : GetMeleeDamageRollRange(wo, tier);
        var place = Math.Clamp(TierPower.GetRollPosition(converted, previousRange), 0.0, 1.0);

        return newRange.Minimum + (newRange.Maximum - newRange.Minimum) * place;
    }

    private static TierPowerCorrection CorrectLauncherDamageMod(
        WorldObject wo,
        TierPowerBackupWeapon backup,
        (double Minimum, double Middle, double Maximum) band,
        List<string> changes
    )
    {
        if (wo.DamageMod is not > 0 || backup.DamageMod is not > 0)
        {
            return TierPowerCorrection.NotALootWeapon;
        }

        var tier = backup.Tier;
        var rolled = backup.BaseDamageMod is > 0 ? backup.BaseDamageMod.Value : backup.DamageMod.Value;

        // a roll above the middle can only have come after #1276
        var fullBand = rolled > band.Middle + 0.0001;
        var top = fullBand ? band.Maximum : band.Middle;
        var place = GetPlaceInReachableBand(rolled, band.Minimum, top, false);

        // from tier 2 up, launchers roll on the multiplier ladder; tier 1 still rolls the old band, at the new speeds
        var weenie = wo.Weenie ?? DatabaseManager.World?.GetCachedWeenie(wo.WeenieClassId);
        var weenieTime = weenie?.GetProperty(PropertyInt.WeaponTime) ?? wo.WeaponTime ?? 0;
        (double Minimum, double Maximum) newRange = tier >= 2
            ? TierPower.GetMultiplierRange(tier)
            : GetPreviousLauncherDamageModRange(wo.WeaponSkill, tier, GetWeaponHitsPerSecond(weenieTime, true, tier));
        var corrected = newRange.Minimum + (newRange.Maximum - newRange.Minimum) * place;

        // the migration moved tier 2 up from the old full band at the launcher's speed then, and left tier 1 alone
        var migrated = tier >= 2
            ? TierPower.MapToRange(
                rolled,
                GetPreviousLauncherDamageModRange(wo.WeaponSkill, tier, GetWeaponHitsPerSecond(wo, tier)),
                newRange
            )
            : rolled;
        var liveBase = wo.BaseDamageMod is > 0 ? wo.BaseDamageMod.Value : wo.DamageMod.Value;

        var rolledIn = $"rolled {rolled:0.###} in {band.Minimum:0.###}-{top:0.###} ({place:P0}{(fullBand ? ", full band" : "")})";

        if (Math.Abs(liveBase - corrected) < 0.0005)
        {
            return Math.Abs(migrated - corrected) < 0.0005 ? TierPowerCorrection.NoChange : TierPowerCorrection.AlreadyCorrected;
        }

        if (Math.Abs(liveBase - migrated) > 0.01)
        {
            changes.Add($"{rolledIn}, migration would have made it {migrated:0.###} but it's {liveBase:0.###} now");
            return TierPowerCorrection.ChangedSinceMigration;
        }

        var damageMod = ApplyTinkShare(wo.DamageMod.Value, liveBase, corrected);
        var migratedPlace = TierPower.GetRollPosition(liveBase, newRange);

        changes.Add($"{rolledIn}, migrated to {migratedPlace:P0}: DamageMod {wo.DamageMod:0.###} => {damageMod:0.###}");
        wo.DamageMod = damageMod;

        if (wo.BaseDamageMod is > 0)
        {
            wo.BaseDamageMod = corrected;
        }

        return TierPowerCorrection.Corrected;
    }
}
