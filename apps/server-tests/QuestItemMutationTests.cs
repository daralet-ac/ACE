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
            Assert.AreEqual(rolled * (1 + 4 * Salvage.LavenderJadeTinkPercent), staff.WeaponRestorationSpellsMod.Value, 1e-6);
        }
    }

    [TestMethod]
    public void MutateQuestItem_WarCaster_TinksElementalAndDerivesRestoration()
    {
        var wand = CreateCaster(Skill.WarMagic, 230, 2.68, 1.42);

        LootGenerationFactory.MutateQuestItem(wand);

        var rolled = wand.BaseElementalDamageMod.Value;
        AssertBetween(3.125, 3.75, rolled);
        Assert.AreEqual(rolled * (1 + 4 * Salvage.GreenGarnetTinkPercent), wand.ElementalDamageMod.Value, 1e-6);
        Assert.AreEqual(QuestItemMutation.GetWarCasterRestorationMod(rolled), wand.WeaponRestorationSpellsMod.Value, 1e-6);
    }

    [TestMethod]
    public void MutateQuestItem_MissileLauncher_BakesInMahoganyTinksSizedOffItsRoll()
    {
        // T6 large bow (3.02-4.02), authored at the tier minimum
        for (var i = 0; i < Rolls; i++)
        {
            var bow = CreateMissileLauncher(LootTables.WeaponSubtype.BowLarge, 3.02, 230);

            LootGenerationFactory.MutateQuestItem(bow);

            var rolled = bow.BaseDamageMod.Value;
            Assert.AreEqual(4, bow.QuestItemTinks);
            AssertBetween(3.52, 4.02, rolled);
            Assert.AreEqual(rolled * (1 + 4 * Salvage.MahoganyTinkPercent), bow.DamageMod.Value, 1e-6);
        }
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

            // T7 AxeLarge is 69-92; the stored roll quality lands in the same place there, plus the rounding it had
            var quality = axe.QuestItemRollQuality.Value;
            var expected = QuestItemMutation.GetRollValue(69, 92, quality) + (t6Roll - QuestItemMutation.GetRollValue(47, 62, quality));

            Assert.AreEqual(250, axe.WieldDifficulty);
            Assert.AreEqual(5, axe.QuestItemTinks);
            Assert.AreEqual((int)Math.Round(expected, MidpointRounding.AwayFromZero), axe.BaseDamage);
            AssertBetween(80, 92, axe.BaseDamage.Value);
            Assert.AreEqual(axe.BaseDamage + 5 * (int)(axe.BaseDamage * 0.075), axe.Damage);
        }
    }

    [TestMethod]
    public void UpgradeItem_KeepsALowTierRollsQualityThatItsRoundedValueLost()
    {
        // T1 AxeLarge (5-7) only rolls 6 or 7, so the whole number alone can't say how good the roll was
        for (var i = 0; i < Rolls; i++)
        {
            var axe = CreateMeleeWeapon(LootTables.WeaponSubtype.AxeLarge, 6, 50);
            LootGenerationFactory.MutateQuestItem(axe);
            var quality = axe.QuestItemRollQuality.Value;

            Assert.IsTrue(UpgradeKit.UpgradeItem(null, axe, 250));

            // within a point of where that quality lands at T7 (69-92), not snapped to its middle or top
            AssertBetween(QuestItemMutation.GetRollValue(69, 92, quality) - 1, QuestItemMutation.GetRollValue(69, 92, quality) + 1, axe.BaseDamage.Value);
        }
    }

    [TestMethod]
    public void UpgradeItem_KeepsAJewelryWardRollsQuality()
    {
        for (var i = 0; i < Rolls; i++)
        {
            var ring = CreateItem(
                WeenieType.Generic,
                ItemType.Jewelry,
                new()
                {
                    [PropertyInt.WardLevel] = 9,
                    [PropertyInt.WieldRequirements] = (int)WieldRequirement.Level,
                    [PropertyInt.WieldDifficulty] = 20,
                    [PropertyInt.ValidLocations] = (int)EquipMask.FingerWear,
                }
            );
            LootGenerationFactory.MutateQuestItem(ring);
            var quality = ring.QuestItemWardRollQuality.Value;

            // a ring's T6 ward is 18-25
            Assert.IsTrue(UpgradeKit.UpgradeItemToRequirement(ring, 50));

            AssertBetween(QuestItemMutation.GetRollValue(18, 25, quality) - 1, QuestItemMutation.GetRollValue(18, 25, quality) + 1, ring.BaseWard.Value);
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

    [TestMethod]
    public void RollRanges_ShowTheFinalSpreadOfAnUnrolledItem()
    {
        // Obsidian Axe on a vendor: T6 AxeLarge, authored at its 55 floor, 4 tinks of +4 at either end
        var axe = CreateMeleeWeapon(LootTables.WeaponSubtype.AxeLarge, 55, 230);

        var ranges = LootGenerationFactory.GetQuestItemRollRanges(axe, null);

        Assert.AreEqual(1, ranges.Count);
        Assert.AreEqual(new LootGenerationFactory.QuestItemRollRange("Damage", 55 + 4 * 4, 62 + 4 * 4, false, null), ranges[0]);
    }

    [TestMethod]
    public void RollRanges_CoverWhatARolledItemRolledAndShowItsQuality()
    {
        for (var i = 0; i < Rolls; i++)
        {
            var axe = CreateMeleeWeapon(LootTables.WeaponSubtype.AxeLarge, 55, 230);
            LootGenerationFactory.MutateQuestItem(axe);

            var range = LootGenerationFactory.GetQuestItemRollRanges(axe, axe.Weenie).Single();

            AssertBetween(range.Low, range.High, axe.Damage.Value);
            Assert.AreEqual(axe.QuestItemRollQuality, range.Quality);
        }
    }

    [TestMethod]
    public void UpgradeItem_MissileLauncher_RescalesTheUntinkeredRollAndResizesItsTinks()
    {
        for (var i = 0; i < Rolls; i++)
        {
            var bow = CreateMissileLauncher(LootTables.WeaponSubtype.BowLarge, 3.02, 230);
            LootGenerationFactory.MutateQuestItem(bow);
            var t6Roll = bow.BaseDamageMod.Value;

            Assert.IsTrue(UpgradeKit.UpgradeItem(null, bow, 250));

            // the T6 tinks come off before the rescale, so only the roll moves up to T7, and the new tinks size off it
            Assert.AreEqual(5, bow.QuestItemTinks);
            Assert.AreEqual(ScaleUpBowLargeDamageModToT7(t6Roll), bow.BaseDamageMod.Value, 1e-5);
            Assert.AreEqual(bow.BaseDamageMod.Value * (1 + 5 * Salvage.MahoganyTinkPercent), bow.DamageMod.Value, 1e-6);
        }
    }

    [TestMethod]
    public void RollRanges_ArmorScalesArmorLevelByProtectionAndWardBySlots()
    {
        // Platemail Hauberk of the Ogre: T2, 4 slots, authored at its 143 / 32 floors
        var hauberk = CreateArmor(ArmorStyle.Platemail, ArmorWeightClass.Heavy, armorLevel: 143, wardLevel: 32, armorSlots: 4, wieldDifficulty: 125);

        var ranges = LootGenerationFactory.GetQuestItemRollRanges(hauberk, null, 0.5, 0.55);

        Assert.AreEqual(new LootGenerationFactory.QuestItemRollRange("Armor Level", 71 + 2 * 5, 104 + 2 * 7, false, null), ranges[0]);
        Assert.AreEqual(new LootGenerationFactory.QuestItemRollRange("Ward Level", 32 + 2 * 12, 35 + 2 * 12, false, null), ranges[1]);
    }

    [TestMethod]
    public void RollRanges_LifeCasterShowsRestorationAsAPercent()
    {
        var staff = CreateCaster(Skill.LifeMagic, 230, 2.0625, 2.0625);

        var range = LootGenerationFactory.GetQuestItemRollRanges(staff, null).Single();

        Assert.AreEqual("Restoration Healing Bonus", range.Stat);
        Assert.AreEqual((2.0625 * (1 + 4 * Salvage.LavenderJadeTinkPercent) - 1) * 100, range.Low, 1e-6);
        Assert.AreEqual((2.375 * (1 + 4 * Salvage.LavenderJadeTinkPercent) - 1) * 100, range.High, 1e-6);
        Assert.IsTrue(range.IsPercent);
    }

    [TestMethod]
    public void RollRanges_ListEverySecondaryStatTheRollTouches()
    {
        for (var i = 0; i < Rolls; i++)
        {
            var spear = CreateMeleeWeapon(
                LootTables.WeaponSubtype.SpearMedium,
                10,
                125,
                new()
                {
                    [PropertyFloat.WeaponOffense] = 1.11,
                    [PropertyFloat.CriticalFrequency] = 0.1,
                    [PropertyFloat.IgnoreArmor] = 0.9,
                }
            );
            LootGenerationFactory.MutateQuestItem(spear);

            var ranges = LootGenerationFactory.GetQuestItemRollRanges(spear, spear.Weenie).ToDictionary(range => range.Stat);

            AssertRange(ranges["Bonus to Attack Skill"], 11, 21, (spear.WeaponOffense.Value - 1) * 100);
            AssertRange(ranges["Biting Strike"], 0, 5, (spear.CriticalFrequency.Value - 0.1) * 100);
            AssertRange(ranges["Armor Cleaving"], 10, 20, (1 - spear.IgnoreArmor.Value) * 100);
        }
    }

    [TestMethod]
    public void UpgradeItem_WarCaster_ResizesGreenGarnetTinksForTheNewRoll()
    {
        for (var i = 0; i < Rolls; i++)
        {
            var wand = CreateCaster(Skill.WarMagic, 230, 2.68, 1.42);
            LootGenerationFactory.MutateQuestItem(wand);
            var t6Roll = wand.BaseElementalDamageMod.Value;

            Assert.IsTrue(UpgradeKit.UpgradeItem(null, wand, 250));

            var t7Roll = wand.BaseElementalDamageMod.Value;
            Assert.AreEqual(5, wand.QuestItemTinks);
            Assert.AreEqual(ScaleUpCasterDamageModToT7(t6Roll), t7Roll, 1e-5);
            Assert.AreEqual(t7Roll * (1 + 5 * Salvage.GreenGarnetTinkPercent), wand.ElementalDamageMod.Value, 1e-6);
            Assert.AreEqual(QuestItemMutation.GetWarCasterRestorationMod(t7Roll), wand.WeaponRestorationSpellsMod.Value, 1e-6);
        }
    }

    [TestMethod]
    public void RollRanges_FollowAnUpgradeKitsTierShift()
    {
        // an Upgrade Kit from T6 to T7 adds 2.5% to the attack mod (WeaponOffenseModBonusPerTier)
        var axe = CreateMeleeWeapon(LootTables.WeaponSubtype.AxeLarge, 55, 230, new() { [PropertyFloat.WeaponOffense] = 1.15 });
        LootGenerationFactory.MutateQuestItem(axe);
        var t6Quality = LootGenerationFactory.GetQuestItemRollRanges(axe, axe.Weenie).Single(range => range.Stat == "Bonus to Attack Skill").Quality.Value;

        Assert.IsTrue(UpgradeKit.UpgradeItem(null, axe, 250));

        AssertRange(LootGenerationFactory.GetQuestItemRollRanges(axe, axe.Weenie).Single(range => range.Stat == "Bonus to Attack Skill"), 17.5, 27.5, (axe.WeaponOffense.Value - 1) * 100);
        Assert.AreEqual(t6Quality, LootGenerationFactory.GetQuestItemRollRanges(axe, axe.Weenie).Single(range => range.Stat == "Bonus to Attack Skill").Quality.Value, 1e-6);
    }

    [TestMethod]
    public void UpgradeItem_LifeCaster_ResizesLavenderJadeTinksForTheNewRoll()
    {
        for (var i = 0; i < Rolls; i++)
        {
            var staff = CreateCaster(Skill.LifeMagic, 230, 1.84, 1.84);
            LootGenerationFactory.MutateQuestItem(staff);
            var t6Roll = staff.BaseWeaponRestorationSpellsMod.Value;

            Assert.IsTrue(UpgradeKit.UpgradeItem(null, staff, 250));

            // the life roll is rescaled on the war scale, so the restoration tinks must be off it first
            var t7Roll = staff.BaseWeaponRestorationSpellsMod.Value;
            var expectedT7Roll = QuestItemMutation.ToLifeCasterScale(ScaleUpCasterDamageModToT7(1 + (t6Roll - 1) * 2));
            Assert.AreEqual(5, staff.QuestItemTinks);
            Assert.AreEqual(expectedT7Roll, t7Roll, 1e-5);
            Assert.AreEqual(t7Roll, staff.ElementalDamageMod.Value, 1e-6);
            Assert.AreEqual(t7Roll * (1 + 5 * Salvage.LavenderJadeTinkPercent), staff.WeaponRestorationSpellsMod.Value, 1e-6);
        }
    }

    private static void AssertRange(LootGenerationFactory.QuestItemRollRange range, double low, double high, double current)
    {
        Assert.AreEqual(low, range.Low, 1e-6, range.Stat);
        Assert.AreEqual(high, range.High, 1e-6, range.Stat);
        AssertBetween(low, high, current);
        Assert.AreEqual((current - low) / (high - low), range.Quality.Value, 1e-6, range.Stat);
    }

    // ScaleUpDamage's arithmetic for AxeLarge, T6 (47-62) to T7 (69-92)
    private static int ScaleUpAxeLargeDamageToT7(int t6Damage)
    {
        var rollPercentile = (float)(t6Damage - 47) / 15;
        return Convert.ToInt32(69 + 23 * rollPercentile);
    }

    // ScaleUpDamageMod's arithmetic for BowLarge, T6 (3.02-4.02) to T7 (4.01-5.34)
    private static double ScaleUpBowLargeDamageModToT7(double t6DamageMod)
    {
        var rollPercentile = (float)(t6DamageMod - 3.02f) / (4.02f - 3.02f);
        return 4.01f + (5.34f - 4.01f) * rollPercentile;
    }

    // ScaleUpElementalAndRestoMod's arithmetic for a war-scale caster mod, T6 (2.5-3.75) to T7 (3.5-4.75)
    private static double ScaleUpCasterDamageModToT7(double t6WarScaleMod)
    {
        var rollPercentile = (float)(t6WarScaleMod - 2.5f) / (3.75f - 2.5f);
        return 3.5f + (4.75f - 3.5f) * rollPercentile;
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

    private static WorldObject CreateMissileLauncher(LootTables.WeaponSubtype subtype, double damageMod, int wieldDifficulty)
    {
        return CreateItem(
            WeenieType.MissileLauncher,
            ItemType.MissileWeapon,
            new()
            {
                [PropertyInt.WeaponSubtype] = (int)subtype,
                [PropertyInt.WieldRequirements] = (int)WieldRequirement.RawAttrib,
                [PropertyInt.WieldDifficulty] = wieldDifficulty,
            },
            new() { [PropertyFloat.DamageMod] = damageMod }
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
