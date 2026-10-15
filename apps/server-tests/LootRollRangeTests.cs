using System;
using System.Collections.Generic;
using System.Linq;
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
public class LootRollRangeTests
{
    [TestMethod]
    public void MeleeWeapon_ShowsDamageModsAndSubtypeBonusAtTheirRolls()
    {
        const int tier = 6;
        var axe = CreateMeleeAxe(tier);

        var (minimum, maximum) = LootGenerationFactory.GetMeleeDamageRollRange(axe, tier);
        var low = Math.Round(minimum);
        var high = Math.Round(maximum);
        axe.Damage = (int)Math.Round(low + (high - low) * 0.25);

        // attack and defense both rolled, so each is scaled to 75%, here at the middle of its range
        var tierBonus = LootTables.WeaponOffenseModBonusPerTier[tier - 1];
        var modLow = (0.1 + tierBonus) * 0.75;
        var modHigh = (0.2 + tierBonus) * 0.75;
        var mod = (modLow + modHigh) / 2;
        axe.WeaponOffense = 1 + mod;
        axe.WeaponPhysicalDefense = 1 + mod;
        axe.WeaponMagicalDefense = 1 + mod;

        var critLow = 0.1 + LootTables.BonusCritChancePerTier[tier - 1];
        axe.CriticalFrequency = critLow + 0.05 * 0.4;

        var ranges = LootGenerationFactory.GetLootItemRollRanges(axe).ToDictionary(r => r.Stat);

        AssertRange(ranges["Damage"], low, high, (axe.Damage.Value - low) / (high - low));
        AssertRange(ranges["Bonus to Attack Skill"], modLow * 100, modHigh * 100, 0.5);
        AssertRange(ranges["Bonus to Physical Defense"], modLow * 100, modHigh * 100, 0.5);
        AssertRange(ranges["Bonus to Magic Defense"], modLow * 100, modHigh * 100, 0.5);
        AssertRange(ranges["Biting Strike"], (critLow - 0.1) * 100, (critLow - 0.05) * 100, 0.4);
    }

    [TestMethod]
    public void MeleeWeapon_SingleModIsNotScaledDown()
    {
        const int tier = 3;
        var axe = CreateMeleeAxe(tier);
        var tierBonus = LootTables.WeaponOffenseModBonusPerTier[tier - 1];
        axe.WeaponOffense = 1 + 0.1 + tierBonus;

        var range = LootGenerationFactory.GetLootItemRollRanges(axe).Single(r => r.Stat == "Bonus to Attack Skill");

        AssertRange(range, (0.1 + tierBonus) * 100, (0.2 + tierBonus) * 100, 0);
    }

    [TestMethod]
    public void TinkeredWeapon_ShowsTheRollItDroppedWith()
    {
        const int tier = 6;
        var axe = CreateMeleeAxe(tier);

        var (minimum, maximum) = LootGenerationFactory.GetMeleeDamageRollRange(axe, tier);
        var low = Math.Round(minimum);
        var high = Math.Round(maximum);
        var rolled = (int)high;

        axe.BaseDamage = rolled;
        axe.Damage = rolled + 20;
        axe.NumTimesTinkered = 3;

        var range = LootGenerationFactory.GetLootItemRollRanges(axe).Single(r => r.Stat == "Damage");

        AssertRange(range, low, high, 1);
    }

    [TestMethod]
    public void HeavyArmor_ShowsArmorWardModsAndRatings()
    {
        const int tier = 7;
        const int slots = 4;
        var armor = CreateItem(
            WeenieType.Clothing,
            ItemType.Armor,
            new()
            {
                [PropertyInt.ArmorType] = (int)LootTables.ArmorType.Undef,
                [PropertyInt.ArmorStyle] = (int)ArmorStyle.Platemail,
                [PropertyInt.ArmorWeightClass] = (int)ArmorWeightClass.Heavy,
                [PropertyInt.ArmorSlots] = slots,
                [PropertyInt.Tier] = tier,
                [PropertyInt.ItemWorkmanship] = 5,
            }
        );

        var (armorMinimum, armorMaximum) = TierPower.GetArmorRollRange(95, tier);
        armor.ArmorLevel = (int)(armorMinimum + (armorMaximum - armorMinimum) * 0.5);

        var (wardMinimum, wardMaximum) = TierPower.GetWardRollRange(7, tier);
        armor.WardLevel = (int)(wardMaximum * slots);

        // attack and the defense pair rolled: two types, so 75%, and four slots of ten
        var scale = 0.75 * slots / 10.0;
        var tierBonus = LootTables.ArmorSkillModBonusPerTier[tier - 1];
        var modLow = (0.1 + tierBonus) * scale;
        var modHigh = (0.2 + tierBonus) * scale;
        armor.ArmorAttackMod = modHigh;
        armor.ArmorPhysicalDefMod = modLow;
        armor.ArmorMagicDefMod = modLow;

        armor.GearDamageResist = 1 * slots;
        armor.GearCritResist = (3 + 1) * slots;

        var ranges = LootGenerationFactory.GetLootItemRollRanges(armor).ToDictionary(r => r.Stat);

        Assert.AreEqual((int)armorMinimum, ranges["Armor Level"].Low);
        Assert.AreEqual((int)armorMaximum, ranges["Armor Level"].High);
        Assert.AreEqual(0.5, ranges["Armor Level"].Quality.Value, 0.01);
        Assert.AreEqual(1.0, ranges["Ward Level"].Quality.Value, 1e-6);

        AssertRange(ranges["Bonus to Attack Skill"], modLow * 100, modHigh * 100, 1);
        AssertRange(ranges["Bonus to Physical Defense"], modLow * 100, modHigh * 100, 0);
        AssertRange(ranges["Bonus to Magic Defense"], modLow * 100, modHigh * 100, 0);

        AssertRange(ranges["Damage Resist Rating"], 0, 3 * slots, 1 / 3.0);
        AssertRange(ranges["Crit Resist Rating"], slots, 4 * slots, 1);
    }

    [TestMethod]
    public void Ring_ShowsWardAndRating()
    {
        const int tier = 5;
        var ring = CreateItem(
            WeenieType.Generic,
            ItemType.Jewelry,
            new()
            {
                [PropertyInt.ValidLocations] = (int)EquipMask.FingerWear,
                [PropertyInt.Tier] = tier,
                [PropertyInt.ItemWorkmanship] = 5,
            }
        );

        var wardLow = TierPower.GetJewelryBaseWard(tier);
        var wardHigh = TierPower.GetJewelryBaseWard(tier + 1);
        ring.WardLevel = wardLow;
        ring.GearCritDamage = 4;

        var ranges = LootGenerationFactory.GetLootItemRollRanges(ring).ToDictionary(r => r.Stat);

        AssertRange(ranges["Ward Level"], wardLow, wardHigh, 0);

        // the rating rolls from 2 to 4 at tier 5
        Assert.AreEqual(2, ranges["Crit Damage Rating"].Low);
        Assert.AreEqual(4, ranges["Crit Damage Rating"].High);
        Assert.AreEqual(1.0, ranges["Crit Damage Rating"].Quality.Value, 1e-6);
    }

    [TestMethod]
    public void Caster_ShowsItsDamageAndRestorationMods()
    {
        const int tier = 6;
        var orb = CreateItem(
            WeenieType.Caster,
            ItemType.Caster,
            new()
            {
                [PropertyInt.WieldSkillType2] = (int)Skill.WarMagic,
                [PropertyInt.Tier] = tier,
                [PropertyInt.ItemWorkmanship] = 5,
            }
        );

        var (minimum, maximum) = TierPower.GetMultiplierRange(tier);
        orb.ElementalDamageMod = minimum + (maximum - minimum) * 0.3;
        orb.WeaponRestorationSpellsMod = TierPower.GetRestorationForRoll(tier, 0.3f, false);

        var ranges = LootGenerationFactory.GetLootItemRollRanges(orb).ToDictionary(r => r.Stat);

        AssertRange(ranges["Elemental Damage Bonus"], (minimum - 1) * 100, (maximum - 1) * 100, 0.3);
        Assert.AreEqual(0.3, ranges["Restoration Healing Bonus"].Quality.Value, 1e-5);
    }

    private static void AssertRange(LootGenerationFactory.QuestItemRollRange range, double low, double high, double quality)
    {
        Assert.AreEqual(low, range.Low, 1e-4, range.Stat);
        Assert.AreEqual(high, range.High, 1e-4, range.Stat);
        Assert.AreEqual(quality, range.Quality.Value, 1e-4, range.Stat);
    }

    // a Battle Axe's speed and variance
    private static WorldObject CreateMeleeAxe(int tier)
    {
        return CreateItem(
            WeenieType.MeleeWeapon,
            ItemType.MeleeWeapon,
            new()
            {
                [PropertyInt.Damage] = 10,
                [PropertyInt.WeaponTime] = 59,
                [PropertyInt.WeaponSkill] = (int)Skill.Axe,
                [PropertyInt.Tier] = tier,
                [PropertyInt.ItemWorkmanship] = 5,
            },
            new() { [PropertyFloat.DamageVariance] = 0.6 }
        );
    }

    private static WorldObject CreateItem(
        WeenieType weenieType,
        ItemType itemType,
        Dictionary<PropertyInt, int> ints,
        Dictionary<PropertyFloat, double> floats = null
    )
    {
        ints[PropertyInt.ItemType] = (int)itemType;

        var weenie = new Weenie
        {
            WeenieClassId = 999999,
            ClassName = "lootrollrangetest",
            WeenieType = weenieType,
            PropertiesInt = ints,
            PropertiesFloat = floats ?? new Dictionary<PropertyFloat, double>(),
            PropertiesString = new Dictionary<PropertyString, string> { [PropertyString.Name] = "Loot Item" },
        };

        return new GenericObject(weenie, new ObjectGuid(0x80000001));
    }
}
