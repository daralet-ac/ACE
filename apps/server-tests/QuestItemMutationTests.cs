using System;
using System.Collections.Generic;
using System.Linq;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Entity.Models;
using ACE.Server.Factories;
using ACE.Server.WorldObjects;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ACE.Server.Tests;

[TestClass]
public class QuestItemMutationTests
{
    private const int Rolls = 300;

    [TestMethod]
    public void TierIndex_LevelGatedItemsUseTheRequiredLevelLadder()
    {
        Assert.IsTrue(QuestItemMutation.UsesRequiredLevelTiering(ItemType.Jewelry, WeenieType.Generic, WieldRequirement.Invalid));
        Assert.IsTrue(QuestItemMutation.UsesRequiredLevelTiering(ItemType.Armor, WeenieType.Clothing, WieldRequirement.Level));
        Assert.IsFalse(QuestItemMutation.UsesRequiredLevelTiering(ItemType.Armor, WeenieType.Clothing, WieldRequirement.RawAttrib));
        Assert.IsFalse(QuestItemMutation.UsesRequiredLevelTiering(ItemType.MeleeWeapon, WeenieType.MeleeWeapon, WieldRequirement.RawAttrib));

        // a level 50 requirement is tier 6 - read on the attribute ladder it used to come out as tier 1
        Assert.AreEqual(5, QuestItemMutation.GetTierIndex(true, 50));
        Assert.AreEqual(0, QuestItemMutation.GetTierIndex(false, 50));

        Assert.AreEqual(3, QuestItemMutation.GetTierIndex(true, 30));
        Assert.AreEqual(1, QuestItemMutation.GetTierIndex(false, 125));
        Assert.AreEqual(5, QuestItemMutation.GetTierIndex(false, 230));
        Assert.AreEqual(0, QuestItemMutation.GetTierIndex(false, null));
        Assert.AreEqual(0, QuestItemMutation.GetTierIndex(true, null));
    }

    [TestMethod]
    public void RollMainStat_StartsAtTheMedianAndTopsOutAtTheTierMax()
    {
        Assert.AreEqual(9.5, QuestItemMutation.RollMainStat(8, 8, 11, 0.0), 1e-9);
        Assert.AreEqual(11.0, QuestItemMutation.RollMainStat(8, 8, 11, 1.0), 1e-9);

        // an authored value above the median is the floor, and the roll can still reach the max
        Assert.AreEqual(10.5, QuestItemMutation.RollMainStat(10.5, 8, 11, 0.0), 1e-9);
        Assert.AreEqual(11.0, QuestItemMutation.RollMainStat(10.5, 8, 11, 1.0), 1e-9);

        // ...and one above the max stays put
        Assert.AreEqual(15.0, QuestItemMutation.RollMainStat(15, 8, 11, 1.0), 1e-9);
    }

    [TestMethod]
    public void RollWholeMainStat_RoundsAHalfMedianUp()
    {
        Assert.AreEqual(55, QuestItemMutation.RollWholeMainStat(47, 47, 62, 0.0));
        Assert.AreEqual(62, QuestItemMutation.RollWholeMainStat(47, 47, 62, 1.0));
    }

    [TestMethod]
    public void PercentTink_MatchesIronAndNeverAddsLessThanOne()
    {
        Assert.AreEqual(1, QuestItemMutation.GetPercentTinkBonus(10, Salvage.IronTinkPercent));
        Assert.AreEqual(6, QuestItemMutation.GetPercentTinkBonus(80, Salvage.IronTinkPercent));
        Assert.AreEqual(6, QuestItemMutation.GetPercentTinkBonus(85, Salvage.IronTinkPercent));
        Assert.AreEqual(48, QuestItemMutation.GetPercentTinkBonus(640, Salvage.IronTinkPercent));
    }

    [TestMethod]
    public void CasterScales_MatchTheAuthoredQuestCasters()
    {
        // Staff of Clarity (life, 1.84) and Zefir Wing (war, 2.68 elemental / 1.42 restoration) are both T6
        Assert.AreEqual(1.84, QuestItemMutation.ToLifeCasterScale(2.68), 1e-9);
        Assert.AreEqual(1.42, QuestItemMutation.GetWarCasterRestorationMod(2.68), 1e-9);
    }

    [TestMethod]
    public void Ranges_MatchLootAndTheAuthoredTierMinimums()
    {
        // T2 platemail
        Assert.AreEqual((95, 190), QuestItemMutation.GetArmorLevelRange(95, 1));

        // Platemail Hauberk of the Ogre (T2, 4 slots) is authored at 28 ward, Shroud of Cazamal (T5 robe, 8 slots) at 192
        Assert.AreEqual((28, 35), QuestItemMutation.GetWardLevelRange(7, 1, 4));
        Assert.AreEqual((192, 198), QuestItemMutation.GetWardLevelRange(6, 4, 8));

        // a T6 ring, and a T4 necklace like the Amulet of Impulse (authored at 22)
        Assert.AreEqual((18, 25), QuestItemMutation.GetJewelryWardLevelRange(5, false));
        Assert.AreEqual((22, 28), QuestItemMutation.GetJewelryWardLevelRange(3, true));
        Assert.AreEqual((38, 50), QuestItemMutation.GetJewelryWardLevelRange(7, false));
    }

    [TestMethod]
    public void ArmorModBonusRange_ScalesWithSlotsLikeLoot()
    {
        Assert.AreEqual(0.08f, QuestItemMutation.GetArmorModBonusRange(8, false), 1e-6f);
        Assert.AreEqual(0.01f, QuestItemMutation.GetArmorModBonusRange(1, false), 1e-6f);
        Assert.AreEqual(0.02f, QuestItemMutation.GetArmorModBonusRange(1, true), 1e-6f);
    }

    [TestMethod]
    public void MeleeTables_CoverEverySubtypeAtEveryTier()
    {
        // the two-handed subtypes used to return 0 here, breaking Upgrade Kit math for them
        Assert.AreEqual(10, LootTables.GetMeleeSubtypeMinimumDamage(LootTables.WeaponSubtype.TwohandSword, 2));

        foreach (var subtype in Enum.GetValues<LootTables.WeaponSubtype>().Where(LootTables.IsMeleeOrThrownSubtype))
        {
            for (var tier = 0; tier < 8; tier++)
            {
                Assert.IsTrue(LootTables.GetMeleeSubtypeMinimumDamage(subtype, tier) > 0, $"{subtype} T{tier + 1} minimum");
                Assert.IsTrue(LootTables.GetMeleeSubtypeDamageRange(subtype, tier) > 0, $"{subtype} T{tier + 1} range");
            }
        }
    }

    [TestMethod]
    public void SubtypeClassification_PutsEverySubtypeInExactlyOneFamily()
    {
        foreach (var subtype in Enum.GetValues<LootTables.WeaponSubtype>())
        {
            var families = new[]
            {
                LootTables.IsMeleeOrThrownSubtype(subtype),
                LootTables.IsMissileLauncherSubtype(subtype),
                subtype == LootTables.WeaponSubtype.Caster,
            }.Count(isFamily => isFamily);

            Assert.AreEqual(subtype == LootTables.WeaponSubtype.Undef ? 0 : 1, families, subtype.ToString());
        }
    }

    [TestMethod]
    public void TinkTables_CoverEveryTierAndNeverShrink()
    {
        foreach (var table in new[] { LootTables.QuestItemWeaponTinksPerTier, LootTables.QuestItemArmorTinksPerTier })
        {
            Assert.AreEqual(8, table.Length);
            Assert.IsTrue(table[0] >= 1);

            for (var tier = 1; tier < table.Length; tier++)
            {
                Assert.IsTrue(table[tier] >= table[tier - 1]);
            }
        }
    }

    [TestMethod]
    public void ArmorStyleTables_MatchLoot()
    {
        Assert.AreEqual(95, LootTables.GetArmorStyleBaseArmorLevel((int)ArmorStyle.Platemail));
        Assert.AreEqual(100, LootTables.GetArmorStyleBaseArmorLevel((int)ArmorStyle.Covenant));
        Assert.AreEqual(90, LootTables.GetArmorStyleBaseArmorLevel((int)ArmorStyle.LargeShield));
        Assert.AreEqual(75, LootTables.GetArmorStyleBaseArmorLevel((int)ArmorStyle.Cloth));
        Assert.AreEqual(75, LootTables.GetArmorStyleBaseArmorLevel(null));

        Assert.AreEqual(10, LootTables.GetArmorStyleBaseWardLevel((int)ArmorStyle.CovenantShield, null));
        Assert.AreEqual(6, LootTables.GetArmorStyleBaseWardLevel((int)ArmorStyle.Amuli, (int)ArmorWeightClass.Light));
        Assert.AreEqual(7, LootTables.GetArmorStyleBaseWardLevel((int)ArmorStyle.Platemail, (int)ArmorWeightClass.Heavy));
        Assert.AreEqual(6, LootTables.GetArmorStyleBaseWardLevel((int)ArmorStyle.Cloth, (int)ArmorWeightClass.Cloth));
        Assert.AreEqual(5, LootTables.GetArmorStyleBaseWardLevel((int)ArmorStyle.Leather, (int)ArmorWeightClass.Light));
    }

    [TestMethod]
    public void MutateQuestItem_MeleeWeapon_RollsFromTheMedianAndBakesInIronTinks()
    {
        // Obsidian Axe: T6 AxeLarge (47-62), authored at the tier minimum
        for (var i = 0; i < Rolls; i++)
        {
            var axe = CreateMeleeWeapon(LootTables.WeaponSubtype.AxeLarge, 47, 230);

            LootGenerationFactory.MutateQuestItem(axe);

            Assert.IsFalse(axe.MutableQuestItem);
            Assert.AreEqual(4, axe.QuestItemTinks);
            AssertBetween(55, 62, axe.BaseDamage.Value);
            Assert.AreEqual(axe.BaseDamage + 4 * (int)(axe.BaseDamage * 0.075), axe.Damage);
        }
    }

    [TestMethod]
    public void MutateQuestItem_MeleeWeapon_IgnoresAStrayDamageMod()
    {
        var katar = CreateMeleeWeapon(LootTables.WeaponSubtype.Ua, 25, 230, new() { [PropertyFloat.DamageMod] = 1.0 });

        LootGenerationFactory.MutateQuestItem(katar);

        Assert.AreEqual(1.0, katar.DamageMod.Value, 1e-9);
        Assert.AreEqual(4, katar.QuestItemTinks);
    }

    [TestMethod]
    public void MutateQuestItem_LevelGatedJewelry_UsesItsRequiredLevelTier()
    {
        // Rytheran's Jeweled Ring: level 50 (T6) ring, authored at 18 ward. The old tier-1 reading let it reach 36.
        for (var i = 0; i < Rolls; i++)
        {
            var ring = CreateItem(
                WeenieType.Generic,
                ItemType.Jewelry,
                new()
                {
                    [PropertyInt.WardLevel] = 18,
                    [PropertyInt.WieldRequirements] = (int)WieldRequirement.Level,
                    [PropertyInt.WieldDifficulty] = 50,
                    [PropertyInt.ValidLocations] = (int)EquipMask.FingerWear,
                }
            );

            LootGenerationFactory.MutateQuestItem(ring);

            Assert.AreEqual(6, ring.QuestItemTinks);
            AssertBetween(22, 25, ring.BaseWard.Value);
            Assert.AreEqual(ring.BaseWard + 6 * Salvage.WhiteJadeTinkWardLevel, ring.WardLevel);
        }
    }

    [TestMethod]
    public void MutateQuestItem_LifeCaster_IsOnlyHalvedOnce()
    {
        // Staff of Clarity: T6 life caster authored at 1.84 / 1.84 (half of a 2.68 war roll)
        var lifeMedian = QuestItemMutation.ToLifeCasterScale((LootTables.CasterMinDamageMod[5] + LootTables.CasterMaxDamageMod[5]) / 2);
        var lifeMax = QuestItemMutation.ToLifeCasterScale(LootTables.CasterMaxDamageMod[5]);

        for (var i = 0; i < Rolls; i++)
        {
            var staff = CreateCaster(Skill.LifeMagic, 230, 1.84, 1.84);

            LootGenerationFactory.MutateQuestItem(staff);

            var rolled = staff.BaseWeaponRestorationSpellsMod.Value;
            AssertBetween(lifeMedian, lifeMax, rolled);
            Assert.AreEqual(rolled, staff.ElementalDamageMod.Value, 1e-6);
            Assert.AreEqual(rolled + 4 * Salvage.LavenderJadeTinkRestorationMod, staff.WeaponRestorationSpellsMod.Value, 1e-6);
        }
    }

    [TestMethod]
    public void MutateQuestItem_WarCaster_TinksElementalAndDerivesRestoration()
    {
        var wand = CreateCaster(Skill.WarMagic, 230, 2.68, 1.42);

        LootGenerationFactory.MutateQuestItem(wand);

        var rolled = wand.BaseElementalDamageMod.Value;
        AssertBetween(3.125, 3.75, rolled);
        Assert.AreEqual(rolled + 4 * Salvage.GreenGarnetTinkElementalDamageMod, wand.ElementalDamageMod.Value, 1e-6);
        Assert.AreEqual(QuestItemMutation.GetWarCasterRestorationMod(rolled), wand.WeaponRestorationSpellsMod.Value, 1e-6);
    }

    [TestMethod]
    public void MutateQuestItem_HeavyArmor_GetsArmorTinksAndWardTinksPerSlot()
    {
        // Platemail Hauberk of the Ogre: T2, 4 slots
        for (var i = 0; i < Rolls; i++)
        {
            var hauberk = CreateArmor(ArmorStyle.Platemail, ArmorWeightClass.Heavy, armorLevel: 120, wardLevel: 28, armorSlots: 4, wieldDifficulty: 125);

            LootGenerationFactory.MutateQuestItem(hauberk);

            Assert.AreEqual(2, hauberk.QuestItemTinks);
            AssertBetween(143, 190, hauberk.BaseArmor.Value);
            AssertBetween(32, 35, hauberk.BaseWard.Value);
            Assert.AreEqual(hauberk.BaseArmor + 2 * QuestItemMutation.GetPercentTinkBonus(hauberk.BaseArmor.Value, Salvage.IronTinkPercent), hauberk.ArmorLevel);
            Assert.AreEqual(hauberk.BaseWard + 2 * Salvage.SilverTinkWardLevel * 4, hauberk.WardLevel);
        }
    }

    [TestMethod]
    public void MutateQuestItem_ArmorWithoutArmorLevel_OnlyTinksWard()
    {
        var shirt = CreateArmor(ArmorStyle.Cloth, ArmorWeightClass.None, armorLevel: 0, wardLevel: 18, armorSlots: 3, wieldDifficulty: 125);

        LootGenerationFactory.MutateQuestItem(shirt);

        Assert.AreEqual(0, shirt.ArmorLevel);
        Assert.IsNull(shirt.BaseArmor);
        Assert.AreEqual(shirt.BaseWard + 2 * Salvage.SilverTinkWardLevel * 3, shirt.WardLevel);
    }

    [TestMethod]
    public void UpgradeItem_RescalesTheRollAndReappliesTinksForTheNewTier()
    {
        for (var i = 0; i < Rolls; i++)
        {
            var axe = CreateMeleeWeapon(LootTables.WeaponSubtype.AxeLarge, 47, 230);
            LootGenerationFactory.MutateQuestItem(axe);
            var t6Roll = axe.BaseDamage.Value;

            Assert.IsTrue(UpgradeKit.UpgradeItem(null, axe, 250));

            // T7 AxeLarge is 69-92; the T6 roll keeps its place in the range
            Assert.AreEqual(250, axe.WieldDifficulty);
            Assert.AreEqual(5, axe.QuestItemTinks);
            Assert.AreEqual(ScaleUpAxeLargeDamageToT7(t6Roll), axe.BaseDamage);
            AssertBetween(80, 92, axe.BaseDamage.Value);
            Assert.AreEqual(axe.BaseDamage + 5 * (int)(axe.BaseDamage * 0.075), axe.Damage);
        }
    }

    [TestMethod]
    public void UpgradeItem_RollsAnUnrolledQuestItemBeforeUpgradingIt()
    {
        // RecipeManager hands out Silifi of Crimson Stars by upgrading a freshly created one before the player
        // gets it, so the pickup afterwards must not roll it (and bake in tinks) a second time
        var silifi = CreateMeleeWeapon(LootTables.WeaponSubtype.AxeLarge, 10, 125);

        Assert.IsTrue(UpgradeKit.UpgradeItem(null, silifi, 230));

        Assert.IsFalse(silifi.MutableQuestItem);
        Assert.AreEqual(4, silifi.QuestItemTinks);
        AssertBetween(54, 62, silifi.BaseDamage.Value);
        Assert.AreEqual(silifi.BaseDamage + 4 * (int)(silifi.BaseDamage * 0.075), silifi.Damage);
    }

    [TestMethod]
    public void UpgradeItem_ConvertsAnItemRolledUnderTheOldRulesWithoutLoweringIt()
    {
        for (var i = 0; i < Rolls; i++)
        {
            // rolled under the old rules: no tinks, not mutable any more
            var axe = CreateMeleeWeapon(LootTables.WeaponSubtype.AxeLarge, 50, 230);
            axe.MutableQuestItem = false;
            var scaled = ScaleUpAxeLargeDamageToT7(50);

            Assert.IsTrue(UpgradeKit.UpgradeItem(null, axe, 250));

            Assert.AreEqual(5, axe.QuestItemTinks);
            Assert.IsTrue(axe.BaseDamage >= scaled);
            AssertBetween(81, 92, axe.BaseDamage.Value);
            Assert.AreEqual(axe.BaseDamage + 5 * (int)(axe.BaseDamage * 0.075), axe.Damage);
        }
    }

    [TestMethod]
    public void UpgradeItem_ArmorKeepsWardTinksPerSlot()
    {
        var hauberk = CreateArmor(ArmorStyle.Platemail, ArmorWeightClass.Heavy, armorLevel: 120, wardLevel: 28, armorSlots: 4, wieldDifficulty: 125);
        LootGenerationFactory.MutateQuestItem(hauberk);
        var t2Ward = hauberk.BaseWard.Value;

        Assert.IsTrue(UpgradeKit.UpgradeItem(null, hauberk, 250));

        // ward climbs one per-slot base (7 x 4 slots) per tier, from T2 to T7
        Assert.AreEqual(7, hauberk.QuestItemTinks);
        Assert.AreEqual(t2Ward + 7 * 4 * 5, hauberk.BaseWard);
        Assert.AreEqual(hauberk.BaseWard + 7 * Salvage.SilverTinkWardLevel * 4, hauberk.WardLevel);
        Assert.AreEqual(hauberk.BaseArmor + 7 * QuestItemMutation.GetPercentTinkBonus(hauberk.BaseArmor.Value, Salvage.IronTinkPercent), hauberk.ArmorLevel);
    }

    // ScaleUpDamage's arithmetic for AxeLarge, T6 (47-62) to T7 (69-92)
    private static int ScaleUpAxeLargeDamageToT7(int t6Damage)
    {
        var rollPercentile = (float)(t6Damage - 47) / 15;
        return Convert.ToInt32(69 + 23 * rollPercentile);
    }

    private static void AssertBetween(double minimum, double maximum, double value)
    {
        Assert.IsTrue(value >= minimum - 1e-6 && value <= maximum + 1e-6, $"{value} is outside {minimum}-{maximum}");
    }

    private static WorldObject CreateMeleeWeapon(
        LootTables.WeaponSubtype subtype,
        int damage,
        int wieldDifficulty,
        Dictionary<PropertyFloat, double> floats = null
    )
    {
        return CreateItem(
            WeenieType.MeleeWeapon,
            ItemType.MeleeWeapon,
            new()
            {
                [PropertyInt.Damage] = damage,
                [PropertyInt.WeaponSubtype] = (int)subtype,
                [PropertyInt.WieldRequirements] = (int)WieldRequirement.RawAttrib,
                [PropertyInt.WieldDifficulty] = wieldDifficulty,
            },
            floats
        );
    }

    private static WorldObject CreateCaster(Skill school, int wieldDifficulty, double elementalDamageMod, double restorationMod)
    {
        return CreateItem(
            WeenieType.Caster,
            ItemType.Caster,
            new()
            {
                [PropertyInt.WeaponSubtype] = (int)LootTables.WeaponSubtype.Caster,
                [PropertyInt.WieldRequirements] = (int)WieldRequirement.RawAttrib,
                [PropertyInt.WieldDifficulty] = wieldDifficulty,
                [PropertyInt.WieldSkillType2] = (int)school,
            },
            new() { [PropertyFloat.ElementalDamageMod] = elementalDamageMod, [PropertyFloat.WeaponRestorationSpellsMod] = restorationMod }
        );
    }

    private static WorldObject CreateArmor(
        ArmorStyle style,
        ArmorWeightClass weightClass,
        int armorLevel,
        int wardLevel,
        int armorSlots,
        int wieldDifficulty
    )
    {
        return CreateItem(
            WeenieType.Clothing,
            ItemType.Armor,
            new()
            {
                [PropertyInt.ArmorLevel] = armorLevel,
                [PropertyInt.WardLevel] = wardLevel,
                [PropertyInt.ArmorSlots] = armorSlots,
                [PropertyInt.ArmorStyle] = (int)style,
                [PropertyInt.ArmorWeightClass] = (int)weightClass,
                [PropertyInt.WieldRequirements] = (int)WieldRequirement.RawAttrib,
                [PropertyInt.WieldDifficulty] = wieldDifficulty,
            }
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
            ClassName = "questitemmutationtest",
            WeenieType = weenieType,
            PropertiesInt = ints,
            PropertiesFloat = floats ?? new Dictionary<PropertyFloat, double>(),
            PropertiesBool = new Dictionary<PropertyBool, bool>
            {
                [PropertyBool.MutableQuestItem] = true,
                [PropertyBool.UpgradeableQuestItem] = true,
            },
            PropertiesString = new Dictionary<PropertyString, string> { [PropertyString.Name] = "Quest Item" },
        };

        return new GenericObject(weenie, new ObjectGuid(0x80000001));
    }
}
