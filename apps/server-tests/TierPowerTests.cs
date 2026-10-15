using System.Collections.Generic;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Entity.Models;
using ACE.Server.Entity;
using ACE.Server.Factories;
using ACE.Server.WorldObjects;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ACE.Server.Tests;

[TestClass]
public class TierPowerTests
{
    private static readonly int[] TierStarts = [1, 10, 20, 30, 40, 50, 75, 100, 125];

    [TestMethod]
    public void ReferencePlayer_ArmorAndWardDoubleEveryTierFromT2()
    {
        // the bottom Covenant roll at each tier's first level: 100 at T2, doubling to 6400 at T8
        Assert.AreEqual(100f, TierPower.GetReferenceArmor(10), 1e-3);
        Assert.AreEqual(6400f, TierPower.GetReferenceArmor(100), 1e-3);

        for (var tier = 2; tier < TierStarts.Length; tier++)
        {
            var armor = TierPower.GetReferenceArmor(TierStarts[tier]);
            var ward = TierPower.GetReferenceWard(TierStarts[tier]);

            Assert.AreEqual(2 * TierPower.GetReferenceArmor(TierStarts[tier - 1]), armor, 1e-3, $"tier {tier + 1}");

            // a full kit's ward is about 97% of its armor
            Assert.AreEqual(0.97, ward / armor, 0.01, $"tier {tier + 1}");
        }
    }

    [TestMethod]
    public void Targets_IncomingDamageAndKillTimeClimbStepByStep()
    {
        for (var tier = 0; tier < TierStarts.Length - 1; tier++)
        {
            Assert.AreEqual(tier + 1, TierPower.GetIncomingPercent(tier, 0), 1e-4, $"incoming, tier {tier + 1}");
        }

        // T1 weights by level / 9 from level 0, as the archetype system does
        Assert.AreEqual(5f, TierPower.GetKillTime(0), 1e-3);
        Assert.AreEqual(11.43f, TierPower.GetKillTime(10), 1e-3);
        Assert.AreEqual(20f, TierPower.GetKillTime(100), 1e-3);

        // fellowship dungeons: the T6 targets
        Assert.AreEqual(6f, TierPower.FellowshipIncomingPercent, 1e-4);
        Assert.AreEqual(17.14f, TierPower.FellowshipKillTime, 1e-3);
    }

    [TestMethod]
    public void Monsters_CleanHealthWithArmorAndWardClimbingEveryTier()
    {
        float[] health = [20, 100, 175, 300, 500, 800, 1250, 2000, 3000];

        for (var i = 0; i < TierStarts.Length; i++)
        {
            Assert.AreEqual(health[i], TierPower.GetMonsterHealth(TierStarts[i] == 1 ? 0 : TierStarts[i]), 1e-3, $"L{TierStarts[i]}");

            if (i > 0)
            {
                Assert.IsTrue(TierPower.GetMonsterArmorWard(TierStarts[i]) > TierPower.GetMonsterArmorWard(TierStarts[i - 1]), $"L{TierStarts[i]}");
            }
        }
    }

    [TestMethod]
    public void Weapons_TargetDpsDoublesAndTheLaddersClimbByTheSameStep()
    {
        for (var tier = 2; tier <= 8; tier++)
        {
            Assert.AreEqual(2 * TierPower.GetWeaponTargetDps(tier - 1), TierPower.GetWeaponTargetDps(tier), 1e-4, $"tier {tier}");

            // like melee, each tier rolls x1-x2, and the ranges climb x1.41 a tier (spells and ammo carry the rest)
            var (minimum, maximum) = TierPower.GetMultiplierRange(tier);
            Assert.AreEqual(2.0, maximum / minimum, 0.02, $"multiplier, tier {tier}");

            if (tier > 2)
            {
                Assert.AreEqual(1.41, minimum / TierPower.GetMultiplierRange(tier - 1).Minimum, 0.03, $"multiplier, tier {tier}");
            }
        }

        Assert.AreEqual((1.2f, 2.4f), TierPower.GetMultiplierRange(2));
        Assert.AreEqual((9.6f, 19.2f), TierPower.GetMultiplierRange(8));
        Assert.AreEqual((0.75f, 1.0f), TierPower.GetMultiplierRange(1));
    }

    [TestMethod]
    public void RollRanges_ArmorDoublesButShieldsKeepTheirOldSteps()
    {
        Assert.AreEqual((100f, 200f), TierPower.GetArmorRollRange(100, 2));
        Assert.AreEqual((6400f, 12800f), TierPower.GetArmorRollRange(100, 8));
        Assert.AreEqual((700f, 800f), TierPower.GetShieldArmorRollRange(100, 8));
        Assert.AreEqual((224f, 448f), TierPower.GetWardRollRange(7, 7));

        Assert.AreEqual(4, TierPower.GetJewelryBaseWard(2));
        Assert.AreEqual(8, TierPower.GetJewelryBaseWard(3));
        Assert.AreEqual(512, TierPower.GetJewelryBaseWard(9));
    }

    [TestMethod]
    public void Ammo_MaxDamageByTierAndAtlatlsUseDarts()
    {
        Assert.AreEqual(80, TierPower.GetAmmoMaxDamage(Skill.Bow, 8));
        Assert.AreEqual(112, TierPower.GetAmmoMaxDamage(Skill.Crossbow, 8));
        Assert.AreEqual(140, TierPower.GetAmmoMaxDamage(Skill.MissileWeapons, 8));
        Assert.AreEqual(10, TierPower.GetAmmoMaxDamage(Skill.Bow, 2));

        // before the rework atlatls were sized against arrows
        Assert.AreEqual(TierPower.GetPreviousAmmoMaxDamage(Skill.Bow, 8), TierPower.GetPreviousAmmoMaxDamage(Skill.MissileWeapons, 8));
    }

    [TestMethod]
    public void MapToRange_KeepsThePlaceAndDoesNotClamp()
    {
        Assert.AreEqual(150.0, TierPower.MapToRange(5, (0, 10), (100, 200)), 1e-9);
        Assert.AreEqual(250.0, TierPower.MapToRange(15, (0, 10), (100, 200)), 1e-9);
    }

    // ---- migrate-tier-power

    [TestMethod]
    public void Migration_ArmorKeepsItsPlaceInTheDoubledRange()
    {
        // T8 Covenant halfway through its old range (700-800) lands halfway through 6400-12800
        // a loot piece's old range was built on its weenie's armor level (100 here)
        var armor = CreateArmor(ArmorStyle.Covenant, ArmorWeightClass.Heavy, tier: 8, armorLevel: 100, wardLevel: 0, armorSlots: 1);
        armor.ArmorLevel = 750;
        armor.BaseArmor = 750;

        Assert.IsTrue(LootGenerationFactory.MigrateToTierPower(armor, new List<string>()));
        Assert.AreEqual(9600, armor.ArmorLevel);
        Assert.AreEqual(9600, armor.BaseArmor);
    }

    [TestMethod]
    public void Migration_WardTinksComeOffAndGoBackOnAsAShare()
    {
        // T8 heavy platemail, 4 slots, halfway through its old ward range (196-224), with 2 Silver tinks (+3 each)
        var armor = CreateArmor(ArmorStyle.Platemail, ArmorWeightClass.Heavy, tier: 8, armorLevel: 0, wardLevel: 216, armorSlots: 4);
        armor.BaseWard = 210;
        armor.TinkerLog = "Silver,Silver";

        Assert.IsTrue(LootGenerationFactory.MigrateToTierPower(armor, new List<string>()));

        // halfway through 1792-3584, plus 5% of it per Silver
        Assert.AreEqual(2688, armor.BaseWard);
        Assert.AreEqual(2688 + 2 * QuestItemMutation.GetPercentTinkBonus(2688, Salvage.SilverTinkWardPercent), armor.WardLevel);
    }

    [TestMethod]
    public void Migration_MeleeDamageKeepsItsPlaceInTheTiersRange()
    {
        // a T5 battle axe (speed 59) whose speed didn't change: the bottom of its old band lands on the bottom of the
        // new x1-x2 range, the top on the top, and the middle in the middle
        foreach (var place in new[] { 0.0, 0.5, 1.0 })
        {
            var axe = CreateMelee(wcid: 999998, tier: 5, weenieTime: 59, variance: 0.6);
            var previous = LootGenerationFactory.GetPreviousMeleeDamageRange(axe, 5, 59);
            var rolled = previous.Minimum + (previous.Maximum - previous.Minimum) * place;
            axe.Damage = (int)System.Math.Round(rolled);
            axe.BaseDamage = axe.Damage;
            var actualPlace = TierPower.GetRollPosition(axe.BaseDamage.Value, previous);

            Assert.IsTrue(LootGenerationFactory.MigrateToTierPower(axe, new List<string>()));

            var range = LootGenerationFactory.GetMeleeDamageRollRange(axe, 5);
            var expected = range.Minimum + (range.Maximum - range.Minimum) * System.Math.Clamp(actualPlace, 0.0, 1.0);
            Assert.AreEqual((int)System.Math.Round(expected, System.MidpointRounding.AwayFromZero), axe.Damage, $"place {place}");
            Assert.AreEqual(59, axe.WeaponTime);
        }
    }

    [TestMethod]
    public void Migration_TwoHanderTakesItsNewSpeedAndTheDamageForIt()
    {
        // a T6 Quadrelle rolled at speed 95 (+4% on the loot speed roll: 99), now 68: it keeps its roll, and its damage
        // keeps its place, from its old band at 95 to the new range at 68
        var quadrelle = CreateMelee(wcid: 40623, tier: 6, weenieTime: 68, variance: 0.75);
        quadrelle.WeaponTime = 99;
        quadrelle.BaseWeaponTime = 99;
        var previous = LootGenerationFactory.GetPreviousMeleeDamageRange(quadrelle, 6, 95);
        quadrelle.Damage = (int)System.Math.Round((previous.Minimum + previous.Maximum) / 2);
        quadrelle.BaseDamage = quadrelle.Damage;
        var place = TierPower.GetRollPosition(quadrelle.Damage.Value, previous);

        Assert.IsTrue(LootGenerationFactory.MigrateToTierPower(quadrelle, new List<string>()));

        Assert.AreEqual((int)System.Math.Round(99 * 68 / 95.0), quadrelle.WeaponTime);
        Assert.AreEqual(quadrelle.WeaponTime, quadrelle.BaseWeaponTime);
        var range = LootGenerationFactory.GetMeleeDamageRollRange(quadrelle, 6);
        Assert.AreEqual((int)System.Math.Round(range.Minimum + (range.Maximum - range.Minimum) * place, System.MidpointRounding.AwayFromZero), quadrelle.Damage);
    }

    [TestMethod]
    public void Migration_MaceTakesItsNewVarianceAndTheDamageForIt()
    {
        // a T6 mace rolled at its weenie's old 0.75 variance (+0.05 on the loot roll: 0.8), now 0.6: it keeps its share
        // of the variance, and its damage keeps its place, from its old band at 0.75 to the new range at 0.6
        var mace = CreateMelee(wcid: 331, tier: 6, weenieTime: 50, variance: 0.6);
        mace.DamageVariance = 0.8;
        var previous = LootGenerationFactory.GetPreviousMeleeDamageRange(mace, 6, 50, 0.75);
        mace.Damage = (int)System.Math.Round((previous.Minimum + previous.Maximum) / 2);
        mace.BaseDamage = mace.Damage;
        var place = TierPower.GetRollPosition(mace.Damage.Value, previous);

        Assert.IsTrue(LootGenerationFactory.MigrateToTierPower(mace, new List<string>()));

        Assert.AreEqual(0.64, mace.DamageVariance.Value, 1e-4);
        var range = LootGenerationFactory.GetMeleeDamageRollRange(mace, 6);
        Assert.AreEqual((int)System.Math.Round(range.Minimum + (range.Maximum - range.Minimum) * place, System.MidpointRounding.AwayFromZero), mace.Damage);
    }

    [TestMethod]
    public void Migration_SpeedChangesEvenWithoutATier()
    {
        // a two-hander bought off a vendor has no tier, but takes the new speed
        var greataxe = CreateMelee(wcid: 41052, tier: 0, weenieTime: 68, variance: 0.75);
        greataxe.WeaponTime = 95;

        Assert.IsTrue(LootGenerationFactory.MigrateToTierPower(greataxe, new List<string>()));
        Assert.AreEqual(68, greataxe.WeaponTime);
    }

    [TestMethod]
    public void Migration_LifeCasterRecoversItsFullRollForTheLadder()
    {
        // a T6 life caster halfway through the old 2.5-3.75 range kept half of it: 2.0625
        var staff = CreateItem(
            WeenieType.Caster,
            ItemType.Caster,
            new() { [PropertyInt.Tier] = 6, [PropertyInt.WieldSkillType2] = (int)Skill.LifeMagic },
            new() { [PropertyFloat.ElementalDamageMod] = 2.0625, [PropertyFloat.WeaponRestorationSpellsMod] = 2.0625 }
        );

        Assert.IsTrue(LootGenerationFactory.MigrateToTierPower(staff, new List<string>()));

        var (minimum, maximum) = TierPower.GetMultiplierRange(6);
        Assert.AreEqual((minimum + maximum) / 2, staff.ElementalDamageMod.Value, 1e-4);
        Assert.AreEqual(TierPower.GetRestorationForRoll(6, 0.5f, true), staff.WeaponRestorationSpellsMod.Value, 1e-4);
    }

    [TestMethod]
    public void Migration_AmmoTakesItsWeeniesDamage()
    {
        // a T3 arrow stack from before the rework (10), and a T4 dart stack with the old data bug (32, not 24)
        var arrows = CreateAmmo(AmmoType.Arrow, weenieDamage: 14, stackDamage: 10);
        var darts = CreateAmmo(AmmoType.Atlatl, weenieDamage: 35, stackDamage: 32);

        Assert.IsTrue(LootGenerationFactory.MigrateToTierPower(arrows, new List<string>()));
        Assert.IsTrue(LootGenerationFactory.MigrateToTierPower(darts, new List<string>()));
        Assert.AreEqual(14, arrows.Damage);
        Assert.AreEqual(35, darts.Damage);
    }

    [TestMethod]
    public void Migration_RunsOnceAndLeavesTier1Alone()
    {
        var arrows = CreateAmmo(AmmoType.Arrow, weenieDamage: 14, stackDamage: 10);

        Assert.IsTrue(LootGenerationFactory.MigrateToTierPower(arrows, new List<string>()));
        Assert.IsTrue(arrows.GetProperty(PropertyBool.TierPowerMigrated) == true);
        Assert.IsFalse(LootGenerationFactory.MigrateToTierPower(arrows, new List<string>()));
        Assert.AreEqual(14, arrows.Damage);

        var axe = CreateItem(WeenieType.MeleeWeapon, ItemType.MeleeWeapon, new() { [PropertyInt.Tier] = 1, [PropertyInt.Damage] = 6 });

        Assert.IsFalse(LootGenerationFactory.MigrateToTierPower(axe, new List<string>()));
        Assert.AreEqual(6, axe.Damage);
    }

    private static WorldObject CreateAmmo(AmmoType ammoType, int weenieDamage, int stackDamage)
    {
        var ammo = CreateItem(
            WeenieType.Ammunition,
            ItemType.MissileWeapon,
            new() { [PropertyInt.Damage] = weenieDamage, [PropertyInt.AmmoType] = (int)ammoType }
        );
        ammo.Damage = stackDamage;

        return ammo;
    }

    private static WorldObject CreateMelee(uint wcid, int tier, int weenieTime, double variance)
    {
        var ints = new Dictionary<PropertyInt, int> { [PropertyInt.WeaponTime] = weenieTime, [PropertyInt.Damage] = 1 };
        if (tier > 0)
        {
            ints[PropertyInt.Tier] = tier;
        }

        return CreateItem(WeenieType.MeleeWeapon, ItemType.MeleeWeapon, ints, new() { [PropertyFloat.DamageVariance] = variance }, wcid);
    }

    private static WorldObject CreateArmor(ArmorStyle style, ArmorWeightClass weightClass, int tier, int armorLevel, int wardLevel, int armorSlots)
    {
        return CreateItem(
            WeenieType.Clothing,
            ItemType.Armor,
            new()
            {
                [PropertyInt.Tier] = tier,
                [PropertyInt.ArmorLevel] = armorLevel,
                [PropertyInt.WardLevel] = wardLevel,
                [PropertyInt.ArmorSlots] = armorSlots,
                [PropertyInt.ArmorStyle] = (int)style,
                [PropertyInt.ArmorWeightClass] = (int)weightClass,
            }
        );
    }

    private static WorldObject CreateItem(
        WeenieType weenieType,
        ItemType itemType,
        Dictionary<PropertyInt, int> ints,
        Dictionary<PropertyFloat, double> floats = null,
        uint wcid = 999998
    )
    {
        ints[PropertyInt.ItemType] = (int)itemType;

        var weenie = new Weenie
        {
            WeenieClassId = wcid,
            ClassName = "tierpowertest",
            WeenieType = weenieType,
            PropertiesInt = ints,
            PropertiesFloat = floats ?? new Dictionary<PropertyFloat, double>(),
            PropertiesBool = new Dictionary<PropertyBool, bool>(),
            PropertiesString = new Dictionary<PropertyString, string> { [PropertyString.Name] = "Tier Power Item" },
        };

        return new GenericObject(weenie, new ObjectGuid(0x80000002));
    }
}
