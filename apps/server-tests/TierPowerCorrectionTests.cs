using System;
using System.Collections.Generic;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Entity.Models;
using ACE.Server.Entity;
using ACE.Server.Factories;
using ACE.Server.WorldObjects;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Correction = ACE.Server.Factories.LootGenerationFactory.TierPowerCorrection;

namespace ACE.Server.Tests;

[TestClass]
public class TierPowerCorrectionTests
{
    // loot weenies as world-db has them now; their speeds before #1106 were 10 (daggers) and 20 (Dirks)
    private const uint Dagger = 314;
    private const uint Jambiya = 319;
    private const uint FlamingDirk = 22443;
    private const uint Longbow = 306;
    private const uint Shuriken = 343;

    // created after the cleave overhaul went live, so it's corrected
    private const int Created = LootGenerationFactory.CorrectWeaponsCreatedSince + 86400;

    [TestMethod]
    public void Band_IsWhatT6LootRolledInBeforeTheRework()
    {
        // the old formula: (1 + (1 - 10/100) + 160/600) / 2.62 x 2 hits a second, for 50 DPS at 0.6 variance
        var dagger = LootGenerationFactory.GetPreTimePerHitDamageBand(CreateMelee(Dagger, 6, 40, AttackType.DoubleStrike), 6).Value;
        Assert.AreEqual(31.22, dagger.Minimum, 0.01);
        Assert.AreEqual(36.42, dagger.Middle, 0.01);
        Assert.AreEqual(41.63, dagger.Maximum, 0.01);

        // a Dirk at 20, one hit per 1.33s swing
        var dirk = LootGenerationFactory.GetPreTimePerHitDamageBand(CreateMelee(FlamingDirk, 6, 43, AttackType.ThrustSlash), 6).Value;
        Assert.AreEqual(33.23, dirk.Minimum, 0.01);
        Assert.AreEqual(38.77, dirk.Middle, 0.01);
    }

    [TestMethod]
    public void TopDaggerRollBeforeTheFix_LandsOnTheTopOfItsNewRange()
    {
        // a T6 dagger that rolled 36, the best it could before #1276: the migration put it at 166
        var dagger = Migrate(CreateMelee(Dagger, 6, 40, AttackType.DoubleStrike), damage: 36);
        Assert.AreEqual(166, dagger.Damage);

        var changes = new List<string>();
        Assert.AreEqual(Correction.Corrected, LootGenerationFactory.CorrectTierPowerDamage(dagger, Backup(Dagger, 6, 36), changes));

        var range = LootGenerationFactory.GetMeleeDamageRollRange(dagger, 6);
        Assert.AreEqual((int)Math.Round(range.Maximum, MidpointRounding.AwayFromZero), dagger.Damage);
        Assert.AreEqual(1, changes.Count);
    }

    [TestMethod]
    public void TinkedDirk_KeepsItsTinksShareAtItsRealPlace()
    {
        // Maribel's Flaming Dirk: rolled 38 (of 33-39), tinked to 46; the migration made it 171, tinked 207
        var dirk = Migrate(CreateMelee(FlamingDirk, 6, 43, AttackType.ThrustSlash), damage: 46, baseDamage: 38);
        Assert.AreEqual(207, dirk.Damage);
        Assert.AreEqual(171, dirk.BaseDamage);

        Assert.AreEqual(
            Correction.Corrected,
            LootGenerationFactory.CorrectTierPowerDamage(dirk, Backup(FlamingDirk, 6, 46, 38), new List<string>())
        );

        var range = LootGenerationFactory.GetMeleeDamageRollRange(dirk, 6);
        var corrected = range.Minimum + (range.Maximum - range.Minimum) * 5 / 6;
        Assert.AreEqual((int)Math.Round(corrected, MidpointRounding.AwayFromZero), dirk.BaseDamage);
        Assert.AreEqual((int)Math.Round(corrected + 36 * corrected / 171, MidpointRounding.AwayFromZero), dirk.Damage);
    }

    [TestMethod]
    public void RollAboveTheMiddle_CameAfterTheFixAndReadsAgainstTheFullBand()
    {
        // 41 is out of reach of the half band (31-36), so it rolled in the full one (31-42)
        var dagger = Migrate(CreateMelee(Dagger, 6, 40, AttackType.DoubleStrike), damage: 41);

        var changes = new List<string>();
        var result = LootGenerationFactory.CorrectTierPowerDamage(dagger, Backup(Dagger, 6, 41), changes);

        var range = LootGenerationFactory.GetMeleeDamageRollRange(dagger, 6);
        var expected = (int)Math.Round(range.Minimum + (range.Maximum - range.Minimum) * 10 / 11, MidpointRounding.AwayFromZero);
        Assert.AreEqual(expected, dagger.Damage, $"{result}: {string.Join(", ", changes)}");
    }

    [TestMethod]
    public void BottomRoll_NeedsNoChange()
    {
        var jambiya = Migrate(CreateMelee(Jambiya, 6, 40, AttackType.DoubleStrike), damage: 31);
        var damage = jambiya.Damage;

        Assert.AreEqual(Correction.NoChange, LootGenerationFactory.CorrectTierPowerDamage(jambiya, Backup(Jambiya, 6, 31), new List<string>()));
        Assert.AreEqual(damage, jambiya.Damage);
    }

    [TestMethod]
    public void SecondRun_FindsItAlreadyCorrected()
    {
        var dagger = Migrate(CreateMelee(Dagger, 6, 40, AttackType.DoubleStrike), damage: 36);
        LootGenerationFactory.CorrectTierPowerDamage(dagger, Backup(Dagger, 6, 36), new List<string>());
        var damage = dagger.Damage;

        Assert.AreEqual(
            Correction.AlreadyCorrected,
            LootGenerationFactory.CorrectTierPowerDamage(dagger, Backup(Dagger, 6, 36), new List<string>())
        );
        Assert.AreEqual(damage, dagger.Damage);
    }

    [TestMethod]
    public void WeaponChangedSinceTheMigration_IsLeftAlone()
    {
        // not where the migration put it (re-rolled, edited, ...): left for a look by hand
        var dagger = Migrate(CreateMelee(Dagger, 6, 40, AttackType.DoubleStrike), damage: 36);
        dagger.Damage = 200;

        var changes = new List<string>();
        Assert.AreEqual(Correction.ChangedSinceMigration, LootGenerationFactory.CorrectTierPowerDamage(dagger, Backup(Dagger, 6, 36), changes));
        Assert.AreEqual(200, dagger.Damage);
        Assert.AreEqual(1, changes.Count);

        // upgraded to another tier
        var upgraded = Migrate(CreateMelee(Dagger, 6, 40, AttackType.DoubleStrike), damage: 36);
        upgraded.Tier = 7;
        Assert.AreEqual(Correction.TierChanged, LootGenerationFactory.CorrectTierPowerDamage(upgraded, Backup(Dagger, 6, 36), new List<string>()));
    }

    [TestMethod]
    public void TopLauncherRollBeforeTheFix_LandsOnTheTopOfTheMultiplierLadder()
    {
        var bow = CreateItem(
            WeenieType.MissileLauncher,
            ItemType.MissileWeapon,
            new()
            {
                [PropertyInt.Tier] = 6,
                [PropertyInt.WeaponTime] = 57,
                [PropertyInt.DefaultCombatStyle] = (int)CombatStyle.Bow,
                [PropertyInt.WeaponSkill] = (int)Skill.Bow,
                [PropertyInt.WeaponType] = (int)WeaponType.Bow,
            },
            new() { [PropertyFloat.DamageMod] = 1.0 },
            Longbow
        );

        // the best a bow could roll before #1276: the middle of its old band
        var band = LootGenerationFactory.GetPreTimePerHitDamageBand(bow, 6).Value;
        bow.DamageMod = band.Middle;
        Assert.IsTrue(LootGenerationFactory.MigrateToTierPower(bow, new List<string>()));

        var backup = new LootGenerationFactory.TierPowerBackupWeapon(1, Longbow, 6, null, null, band.Middle, null, Created);
        Assert.AreEqual(Correction.Corrected, LootGenerationFactory.CorrectTierPowerDamage(bow, backup, new List<string>()));
        Assert.AreEqual(TierPower.GetMultiplierRange(6).Maximum, bow.DamageMod.Value, 1e-4);
    }

    [TestMethod]
    public void WeaponFromBeforeTheCleaveOverhaul_IsLeftAsMigrated()
    {
        // rolled on an earlier formula: left alone, as is a weapon with no creation time
        var dagger = Migrate(CreateMelee(Dagger, 6, 40, AttackType.DoubleStrike), damage: 36);

        Assert.AreEqual(
            Correction.TooOld,
            LootGenerationFactory.CorrectTierPowerDamage(dagger, Backup(Dagger, 6, 36, created: 1735689600), new List<string>())
        );
        Assert.AreEqual(Correction.TooOld, LootGenerationFactory.CorrectTierPowerDamage(dagger, Backup(Dagger, 6, 36, created: null), new List<string>()));
        Assert.AreEqual(166, dagger.Damage);
    }

    [TestMethod]
    public void ThrownMigratedAtTheOldVariance_TakesTheNewOneWithTheCorrection()
    {
        // a T6 shuriken migrated while its weenie still had 0.75 variance, before world-db lowered it to 0.6
        var shuriken = CreateItem(
            WeenieType.Missile,
            ItemType.MissileWeapon,
            new()
            {
                [PropertyInt.Tier] = 6,
                [PropertyInt.WeaponTime] = 80,
                [PropertyInt.Damage] = 1,
                [PropertyInt.DefaultCombatStyle] = (int)CombatStyle.ThrownWeapon,
                [PropertyInt.WeaponSkill] = (int)Skill.ThrownWeapon,
                [PropertyInt.WeaponType] = (int)WeaponType.Thrown,
            },
            new() { [PropertyFloat.DamageVariance] = 0.75 },
            Shuriken
        );
        shuriken.WeaponTime = 112;
        shuriken.DamageVariance = 0.8;
        var band = LootGenerationFactory.GetPreTimePerHitDamageBand(shuriken, 6).Value;
        var rolled = (int)Math.Round(band.Middle);
        Migrate(shuriken, damage: rolled);
        Assert.AreEqual(0.8, shuriken.DamageVariance.Value, 1e-4);

        shuriken.Weenie.PropertiesFloat[PropertyFloat.DamageVariance] = 0.6;

        var changes = new List<string>();
        Assert.AreEqual(
            Correction.Corrected,
            LootGenerationFactory.CorrectTierPowerDamage(shuriken, Backup(Shuriken, 6, rolled), changes),
            string.Join(", ", changes)
        );

        // its share of the variance, and the top of the range at 0.6
        Assert.AreEqual(0.64, shuriken.DamageVariance.Value, 1e-4);
        var range = LootGenerationFactory.GetMeleeDamageRollRange(shuriken, 6);
        Assert.AreEqual((int)Math.Round(range.Maximum, MidpointRounding.AwayFromZero), shuriken.Damage);
    }

    /// <summary>
    /// A weapon as the backup had it, run through migrate-tier-power as live was
    /// </summary>
    private static WorldObject Migrate(WorldObject wo, int damage, int? baseDamage = null)
    {
        wo.Damage = damage;
        wo.BaseDamage = baseDamage;
        LootGenerationFactory.MigrateToTierPower(wo, new List<string>());

        return wo;
    }

    private static LootGenerationFactory.TierPowerBackupWeapon Backup(
        uint wcid,
        int tier,
        int damage,
        int? baseDamage = null,
        int? created = Created
    )
    {
        return new LootGenerationFactory.TierPowerBackupWeapon(1, wcid, tier, damage, baseDamage, null, null, created);
    }

    private static WorldObject CreateMelee(uint wcid, int tier, int weenieTime, AttackType attackType)
    {
        return CreateItem(
            WeenieType.MeleeWeapon,
            ItemType.MeleeWeapon,
            new()
            {
                [PropertyInt.Tier] = tier,
                [PropertyInt.WeaponTime] = weenieTime,
                [PropertyInt.Damage] = 1,
                [PropertyInt.AttackType] = (int)attackType,
                [PropertyInt.DefaultCombatStyle] = (int)CombatStyle.OneHanded,
                [PropertyInt.WeaponSkill] = (int)Skill.Dagger,
            },
            new() { [PropertyFloat.DamageVariance] = 0.6 },
            wcid
        );
    }

    private static WorldObject CreateItem(
        WeenieType weenieType,
        ItemType itemType,
        Dictionary<PropertyInt, int> ints,
        Dictionary<PropertyFloat, double> floats,
        uint wcid
    )
    {
        ints[PropertyInt.ItemType] = (int)itemType;

        var weenie = new Weenie
        {
            WeenieClassId = wcid,
            ClassName = "tierpowercorrectiontest",
            WeenieType = weenieType,
            PropertiesInt = ints,
            PropertiesFloat = floats,
            PropertiesBool = new Dictionary<PropertyBool, bool>(),
            PropertiesString = new Dictionary<PropertyString, string> { [PropertyString.Name] = "Tier Power Correction Item" },
        };

        return new GenericObject(weenie, new ObjectGuid(0x80000002));
    }
}
