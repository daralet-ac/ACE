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
    public void CasterRanges_FollowTheMultiplierAndRestorationLadders()
    {
        // war and Nether casters share the T6 multiplier range, with restoration at the same place on its own ladder
        Assert.AreEqual(TierPower.GetMultiplierRange(6), ((float, float))QuestItemMutation.GetCasterDamageModRange(5));
        Assert.AreEqual(TierPower.GetWarRestorationRange(6), ((float, float))QuestItemMutation.GetCasterRestorationRange(5, false));
        Assert.AreEqual(TierPower.GetNetherRestorationRange(6), ((float, float))QuestItemMutation.GetCasterRestorationRange(5, true));

        var (minimum, maximum) = QuestItemMutation.GetCasterDamageModRange(5);
        var (netherMinimum, netherMaximum) = QuestItemMutation.GetCasterRestorationRange(5, true);

        Assert.AreEqual(netherMinimum, QuestItemMutation.GetCasterRestorationMod(5, minimum, true), 1e-5);
        Assert.AreEqual(netherMaximum, QuestItemMutation.GetCasterRestorationMod(5, maximum, true), 1e-5);
    }

    [TestMethod]
    public void Ranges_MatchLootAndTheAuthoredTierMinimums()
    {
        // T2 platemail
        Assert.AreEqual((95, 190), QuestItemMutation.GetArmorLevelRange(95, 1));

        // Platemail Hauberk of the Ogre (T2, 4 slots) is authored at 28 ward; Shroud of Cazamal (T5 robe, 8 slots),
        // authored at 192, now rolls in the doubled T5 range
        Assert.AreEqual((28, 56), QuestItemMutation.GetWardLevelRange(7, 1, 4));
        Assert.AreEqual((384, 768), QuestItemMutation.GetWardLevelRange(6, 4, 8));

        // a T6 ring, a T4 necklace like the Amulet of Impulse, and a T8 ring: jewelry ward doubles every tier
        Assert.AreEqual((64, 128), QuestItemMutation.GetJewelryWardLevelRange(5, false));
        Assert.AreEqual((32, 64), QuestItemMutation.GetJewelryWardLevelRange(3, true));
        Assert.AreEqual((256, 512), QuestItemMutation.GetJewelryWardLevelRange(7, false));
    }

    [TestMethod]
    public void ArmorModBonusRange_ScalesWithSlotsLikeLoot()
    {
        Assert.AreEqual(0.08f, QuestItemMutation.GetArmorModBonusRange(8, false), 1e-6f);
        Assert.AreEqual(0.01f, QuestItemMutation.GetArmorModBonusRange(1, false), 1e-6f);
        Assert.AreEqual(0.02f, QuestItemMutation.GetArmorModBonusRange(1, true), 1e-6f);
    }

    [TestMethod]
    public void MeleeDamageRollRange_FollowsTheWeaponsSpeed()
    {
        // quest items and Upgrade Kits roll in the range loot of the same speed and variance rolls
        var slow = CreateMeleeWeapon(LootTables.WeaponSubtype.TwohandMace, 10, 230, weaponTime: 68, variance: 0.75);
        var fast = CreateMeleeWeapon(LootTables.WeaponSubtype.TwohandMace, 10, 230, weaponTime: 34, variance: 0.75);

        for (var tier = 1; tier <= 8; tier++)
        {
            var (slowMinimum, slowMaximum) = LootGenerationFactory.GetMeleeDamageRollRange(slow, tier);
            var (fastMinimum, fastMaximum) = LootGenerationFactory.GetMeleeDamageRollRange(fast, tier);

            Assert.IsTrue(slowMinimum > 0 && slowMaximum > slowMinimum, $"T{tier}");

            // twice as fast, half the damage per hit: the same damage per second
            Assert.AreEqual(slowMaximum / 2, fastMaximum, 1e-6, $"T{tier}");
            Assert.AreEqual(slowMinimum / 2, fastMinimum, 1e-6, $"T{tier}");

            // like armor: from the tier's target DPS up to double it, the next tier's target
            Assert.AreEqual(0.5, slowMinimum / slowMaximum, 1e-9, $"T{tier}");

            // workmanship scores a roll at its place in the 25% band weapons rolled in before
            var previous = LootGenerationFactory.GetTierDamageRange(slowMinimum);
            Assert.AreEqual(previous.Minimum, LootGenerationFactory.GetPreviousBandDamage(slowMinimum, (slowMinimum, slowMaximum)), 1e-6);
            Assert.AreEqual(previous.Maximum, LootGenerationFactory.GetPreviousBandDamage(slowMaximum, (slowMinimum, slowMaximum)), 1e-6);
        }

        // a weapon without a speed has no range rather than a broken one
        Assert.AreEqual((0.0, 0.0), LootGenerationFactory.GetMeleeDamageRollRange(CreateMeleeWeapon(LootTables.WeaponSubtype.AxeLarge, 10, 230, weaponTime: 0), 5));
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
        // Obsidian Axe: T6 AxeLarge, authored at the old tier minimum (47), under the new one
        var (minimum, maximum) = AxeLargeRange(5);

        for (var i = 0; i < Rolls; i++)
        {
            var axe = CreateMeleeWeapon(LootTables.WeaponSubtype.AxeLarge, 47, 230);

            LootGenerationFactory.MutateQuestItem(axe);

            Assert.IsFalse(axe.MutableQuestItem);
            Assert.AreEqual(4, axe.QuestItemTinks);
            AssertBetween(QuestItemMutation.RollWholeMainStat(minimum, minimum, maximum, 0), maximum, axe.BaseDamage.Value);
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

            var (minimum, maximum) = QuestItemMutation.GetJewelryWardLevelRange(5, false);
            Assert.AreEqual(6, ring.QuestItemTinks);
            AssertBetween(QuestItemMutation.RollWholeMainStat(minimum, minimum, maximum, 0), maximum, ring.BaseWard.Value);
            Assert.AreEqual(ring.BaseWard + 6 * QuestItemMutation.GetPercentTinkBonus(ring.BaseWard.Value, Salvage.WhiteJadeTinkWardPercent), ring.WardLevel);
        }
    }

    [TestMethod]
    public void MutateQuestItem_LifeCaster_RollsTheLadderWithNetherRestoration()
    {
        // Staff of Clarity: T6 life caster authored at 1.84 / 1.84
        var (minimum, maximum) = QuestItemMutation.GetCasterDamageModRange(5);

        for (var i = 0; i < Rolls; i++)
        {
            var staff = CreateCaster(Skill.LifeMagic, 230, 1.84, 1.84);

            LootGenerationFactory.MutateQuestItem(staff);

            var rolled = staff.BaseElementalDamageMod.Value;
            var restoration = staff.BaseWeaponRestorationSpellsMod.Value;
            AssertBetween((minimum + maximum) / 2, maximum, rolled);
            Assert.AreEqual(rolled, staff.ElementalDamageMod.Value, 1e-6);
            Assert.AreEqual(QuestItemMutation.GetCasterRestorationMod(5, rolled, true), restoration, 1e-5);
            Assert.AreEqual(restoration * (1 + 4 * Salvage.LavenderJadeTinkPercent), staff.WeaponRestorationSpellsMod.Value, 1e-5);
        }
    }

    [TestMethod]
    public void MutateQuestItem_WarCaster_TinksElementalAndDerivesRestoration()
    {
        var wand = CreateCaster(Skill.WarMagic, 230, 2.68, 1.42);

        LootGenerationFactory.MutateQuestItem(wand);

        var (minimum, maximum) = QuestItemMutation.GetCasterDamageModRange(5);
        var rolled = wand.BaseElementalDamageMod.Value;
        AssertBetween((minimum + maximum) / 2, maximum, rolled);
        Assert.AreEqual(rolled * (1 + 4 * Salvage.GreenGarnetTinkPercent), wand.ElementalDamageMod.Value, 1e-6);
        Assert.AreEqual(QuestItemMutation.GetCasterRestorationMod(5, rolled, false), wand.WeaponRestorationSpellsMod.Value, 1e-5);
    }

    [TestMethod]
    public void MutateQuestItem_MissileLauncher_BakesInMahoganyTinksSizedOffItsRoll()
    {
        // T6 large bow, authored at the old tier minimum (3.02), under the new one
        var (minimum, maximum) = BowLargeRange(5);

        for (var i = 0; i < Rolls; i++)
        {
            var bow = CreateMissileLauncher(LootTables.WeaponSubtype.BowLarge, 3.02, 230);

            LootGenerationFactory.MutateQuestItem(bow);

            var rolled = bow.BaseDamageMod.Value;
            Assert.AreEqual(4, bow.QuestItemTinks);
            AssertBetween((minimum + maximum) / 2, maximum, rolled);
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
            AssertBetween(42, 56, hauberk.BaseWard.Value);
            Assert.AreEqual(hauberk.BaseArmor + 2 * QuestItemMutation.GetPercentTinkBonus(hauberk.BaseArmor.Value, Salvage.IronTinkPercent), hauberk.ArmorLevel);
            Assert.AreEqual(hauberk.BaseWard + 2 * QuestItemMutation.GetPercentTinkBonus(hauberk.BaseWard.Value, Salvage.SilverTinkWardPercent), hauberk.WardLevel);
        }
    }

    [TestMethod]
    public void MutateQuestItem_ArmorWithoutArmorLevel_OnlyTinksWard()
    {
        var shirt = CreateArmor(ArmorStyle.Cloth, ArmorWeightClass.None, armorLevel: 0, wardLevel: 18, armorSlots: 3, wieldDifficulty: 125);

        LootGenerationFactory.MutateQuestItem(shirt);

        Assert.AreEqual(0, shirt.ArmorLevel);
        Assert.IsNull(shirt.BaseArmor);
        Assert.AreEqual(shirt.BaseWard + 2 * QuestItemMutation.GetPercentTinkBonus(shirt.BaseWard.Value, Salvage.SilverTinkWardPercent), shirt.WardLevel);
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

            // the stored roll quality lands in the same place in the T7 range, plus the rounding it had at T6
            var (t6Minimum, t6Maximum) = AxeLargeRange(5);
            var (t7Minimum, t7Maximum) = AxeLargeRange(6);
            var quality = axe.QuestItemRollQuality.Value;
            var expected = QuestItemMutation.GetRollValue(t7Minimum, t7Maximum, quality) + (t6Roll - QuestItemMutation.GetRollValue(t6Minimum, t6Maximum, quality));

            Assert.AreEqual(250, axe.WieldDifficulty);
            Assert.AreEqual(5, axe.QuestItemTinks);
            Assert.AreEqual((int)Math.Round(expected, MidpointRounding.AwayFromZero), axe.BaseDamage);
            AssertBetween((t7Minimum + t7Maximum) / 2.0 - 1, t7Maximum, axe.BaseDamage.Value);
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

            // within a point of where that quality lands at T7, not snapped to its middle or top
            var (t7Minimum, t7Maximum) = AxeLargeRange(6);
            var expected = QuestItemMutation.GetRollValue(t7Minimum, t7Maximum, quality);
            AssertBetween(expected - 1, expected + 1, axe.BaseDamage.Value);
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

            Assert.IsTrue(UpgradeKit.UpgradeItemToRequirement(ring, 50));

            // within a point of where that quality lands in a ring's T6 ward range
            var (minimum, maximum) = QuestItemMutation.GetJewelryWardLevelRange(5, false);
            var expected = QuestItemMutation.GetRollValue(minimum, maximum, quality);
            AssertBetween(expected - 1, expected + 1, ring.BaseWard.Value);
        }
    }

    [TestMethod]
    public void UpgradeItem_RollsAnUnrolledQuestItemBeforeUpgradingIt()
    {
        // RecipeManager hands out Silifi of Crimson Stars by upgrading a freshly created one before the player
        // gets it, so the pickup afterwards must not roll it (and bake in tinks) a second time
        var silifi = CreateMeleeWeapon(LootTables.WeaponSubtype.AxeLarge, 10, 125);

        Assert.IsTrue(UpgradeKit.UpgradeItem(null, silifi, 230));

        var (minimum, maximum) = AxeLargeRange(5);
        Assert.IsFalse(silifi.MutableQuestItem);
        Assert.AreEqual(4, silifi.QuestItemTinks);
        AssertBetween((minimum + maximum) / 2.0 - 1, maximum, silifi.BaseDamage.Value);
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

            var (t7Minimum, t7Maximum) = AxeLargeRange(6);
            Assert.AreEqual(5, axe.QuestItemTinks);
            Assert.IsTrue(axe.BaseDamage >= scaled);
            AssertBetween(QuestItemMutation.RollWholeMainStat(t7Minimum, t7Minimum, t7Maximum, 0), t7Maximum, axe.BaseDamage.Value);
            Assert.AreEqual(axe.BaseDamage + 5 * (int)(axe.BaseDamage * 0.075), axe.Damage);
        }
    }

    [TestMethod]
    public void UpgradeItem_ArmorKeepsItsWardRollAndRetinksWard()
    {
        var hauberk = CreateArmor(ArmorStyle.Platemail, ArmorWeightClass.Heavy, armorLevel: 120, wardLevel: 28, armorSlots: 4, wieldDifficulty: 125);
        LootGenerationFactory.MutateQuestItem(hauberk);
        var t2Ward = hauberk.BaseWard.Value;
        var quality = hauberk.QuestItemWardRollQuality.Value;

        Assert.IsTrue(UpgradeKit.UpgradeItem(null, hauberk, 250));

        // the stored ward roll quality lands in the same place in the doubled T7 range (7 per slot, 4 slots), plus
        // the rounding it had at T2
        var (t2Minimum, t2Maximum) = QuestItemMutation.GetWardLevelRange(7, 1, 4);
        var (t7Minimum, t7Maximum) = QuestItemMutation.GetWardLevelRange(7, 6, 4);
        var expectedWard = (int)Math.Round(
            QuestItemMutation.GetRollValue(t7Minimum, t7Maximum, quality) + (t2Ward - QuestItemMutation.GetRollValue(t2Minimum, t2Maximum, quality)),
            MidpointRounding.AwayFromZero
        );

        Assert.AreEqual(7, hauberk.QuestItemTinks);
        Assert.AreEqual(expectedWard, hauberk.BaseWard);
        Assert.AreEqual(hauberk.BaseWard + 7 * QuestItemMutation.GetPercentTinkBonus(hauberk.BaseWard.Value, Salvage.SilverTinkWardPercent), hauberk.WardLevel);
        Assert.AreEqual(hauberk.BaseArmor + 7 * QuestItemMutation.GetPercentTinkBonus(hauberk.BaseArmor.Value, Salvage.IronTinkPercent), hauberk.ArmorLevel);
    }

    [TestMethod]
    public void RollRanges_ShowTheFinalSpreadOfAnUnrolledItem()
    {
        // Obsidian Axe on a vendor: T6 AxeLarge, authored under the new range's median, with 4 Iron tinks at either end
        var axe = CreateMeleeWeapon(LootTables.WeaponSubtype.AxeLarge, 55, 230);
        var (minimum, maximum) = AxeLargeRange(5);
        int Tinked(int value) => value + 4 * QuestItemMutation.GetPercentTinkBonus(value, Salvage.IronTinkPercent);

        var ranges = LootGenerationFactory.GetQuestItemRollRanges(axe, null);

        Assert.AreEqual(1, ranges.Count);
        Assert.AreEqual(
            new LootGenerationFactory.QuestItemRollRange(
                "Damage",
                Tinked(QuestItemMutation.RollWholeMainStat(55, minimum, maximum, 0)),
                Tinked(maximum),
                false,
                null
            ),
            ranges[0]
        );
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
    public void RollRanges_ArmorShowsArmorLevelAsItRollsAndWardBySlots()
    {
        // Platemail Hauberk of the Ogre: T2 (95-190), 4 slots, authored at its 143 armor level floor; its 32 ward sits
        // under the 42 median. Protections average 1.0 now, so the armor level isn't scaled by them
        var hauberk = CreateArmor(ArmorStyle.Platemail, ArmorWeightClass.Heavy, armorLevel: 143, wardLevel: 32, armorSlots: 4, wieldDifficulty: 125);

        var ranges = LootGenerationFactory.GetQuestItemRollRanges(hauberk, null);

        // each Iron tink adds 7.5% of the armor level
        Assert.AreEqual(new LootGenerationFactory.QuestItemRollRange("Armor Level", 143 + 2 * 10, 190 + 2 * 14, false, null), ranges[0]);
        // each Silver tink adds 5% of the ward
        Assert.AreEqual(new LootGenerationFactory.QuestItemRollRange("Ward Level", 42 + 2 * 2, 56 + 2 * 2, false, null), ranges[1]);
    }

    [TestMethod]
    public void RollRanges_LifeCasterShowsRestorationAsAPercent()
    {
        var staff = CreateCaster(Skill.LifeMagic, 230, 2.0625, 2.0625);

        var range = LootGenerationFactory.GetQuestItemRollRanges(staff, null).Single();

        // a life caster's tinks go on restoration, so it shows the T6 Nether restoration range from its median
        var (minimum, maximum) = QuestItemMutation.GetCasterRestorationRange(5, true);
        Assert.AreEqual("Restoration Healing Bonus", range.Stat);
        Assert.AreEqual((QuestItemMutation.RollMainStat(2.0625, minimum, maximum, 0) * (1 + 4 * Salvage.LavenderJadeTinkPercent) - 1) * 100, range.Low, 1e-4);
        Assert.AreEqual((maximum * (1 + 4 * Salvage.LavenderJadeTinkPercent) - 1) * 100, range.High, 1e-4);
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
            Assert.AreEqual(QuestItemMutation.GetCasterRestorationMod(6, t7Roll, false), wand.WeaponRestorationSpellsMod.Value, 1e-5);
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
            var t6Roll = staff.BaseElementalDamageMod.Value;

            Assert.IsTrue(UpgradeKit.UpgradeItem(null, staff, 250));

            // the elemental roll is rescaled on the ladder, and restoration follows it on the Nether ladder, so the
            // restoration tinks must be off it first
            var t7Roll = staff.BaseElementalDamageMod.Value;
            var t7Restoration = staff.BaseWeaponRestorationSpellsMod.Value;
            Assert.AreEqual(5, staff.QuestItemTinks);
            Assert.AreEqual(ScaleUpCasterDamageModToT7(t6Roll), t7Roll, 1e-5);
            Assert.AreEqual(t7Roll, staff.ElementalDamageMod.Value, 1e-6);
            Assert.AreEqual(QuestItemMutation.GetCasterRestorationMod(6, t7Roll, true), t7Restoration, 1e-5);
            Assert.AreEqual(t7Restoration * (1 + 5 * Salvage.LavenderJadeTinkPercent), staff.WeaponRestorationSpellsMod.Value, 1e-5);
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
        var (t6Minimum, t6Maximum) = AxeLargeRange(5);
        var (t7Minimum, t7Maximum) = AxeLargeRange(6);
        var rollPercentile = (float)(t6Damage - t6Minimum) / (t6Maximum - t6Minimum);
        return Convert.ToInt32(t7Minimum + (t7Maximum - t7Minimum) * rollPercentile);
    }

    // ScaleUpDamageMod's arithmetic for BowLarge, T6 to T7
    private static double ScaleUpBowLargeDamageModToT7(double t6DamageMod)
    {
        var t6Minimum = LootTables.GetMissileCasterSubtypeMinimumDamage(LootTables.WeaponSubtype.BowLarge, 5);
        var t6Range = LootTables.GetMissileCasterSubtypeDamageRange(LootTables.WeaponSubtype.BowLarge, 5);
        var t7Minimum = LootTables.GetMissileCasterSubtypeMinimumDamage(LootTables.WeaponSubtype.BowLarge, 6);
        var t7Range = LootTables.GetMissileCasterSubtypeDamageRange(LootTables.WeaponSubtype.BowLarge, 6);
        var rollPercentile = (float)(t6DamageMod - t6Minimum) / t6Range;
        return t7Minimum + t7Range * rollPercentile;
    }

    // the loot range of a large axe (CreateMeleeWeapon's Battle Axe speed and variance) at tier index t
    private static (int Minimum, int Maximum) AxeLargeRange(int tier)
    {
        var (minimum, maximum) = LootGenerationFactory.GetMeleeDamageRollRange(CreateMeleeWeapon(LootTables.WeaponSubtype.AxeLarge, 10, 230), tier + 1);

        return ((int)Math.Round(minimum), (int)Math.Round(maximum));
    }

    // the loot damage mod range of a large bow at tier index t
    private static (double Minimum, double Maximum) BowLargeRange(int tier)
    {
        var minimum = LootTables.GetMissileCasterSubtypeMinimumDamage(LootTables.WeaponSubtype.BowLarge, tier);

        return (minimum, minimum + LootTables.GetMissileCasterSubtypeDamageRange(LootTables.WeaponSubtype.BowLarge, tier));
    }

    // ScaleUpElementalAndRestoMod's arithmetic for a war-scale caster mod, T6 (2.5-3.75) to T7 (3.5-4.75)
    private static double ScaleUpCasterDamageModToT7(double t6Mod)
    {
        var (t6Minimum, t6Maximum) = QuestItemMutation.GetCasterDamageModRange(5);
        var (t7Minimum, t7Maximum) = QuestItemMutation.GetCasterDamageModRange(6);
        var rollPercentile = (float)(t6Mod - t6Minimum) / (float)(t6Maximum - t6Minimum);
        return t7Minimum + (t7Maximum - t7Minimum) * rollPercentile;
    }

    private static void AssertBetween(double minimum, double maximum, double value)
    {
        Assert.IsTrue(value >= minimum - 1e-6 && value <= maximum + 1e-6, $"{value} is outside {minimum}-{maximum}");
    }

    // a Battle Axe's speed and variance by default
    private static WorldObject CreateMeleeWeapon(
        LootTables.WeaponSubtype subtype,
        int damage,
        int wieldDifficulty,
        Dictionary<PropertyFloat, double> floats = null,
        int weaponTime = 59,
        double variance = 0.6
    )
    {
        floats ??= new Dictionary<PropertyFloat, double>();
        floats.TryAdd(PropertyFloat.DamageVariance, variance);

        return CreateItem(
            WeenieType.MeleeWeapon,
            ItemType.MeleeWeapon,
            new()
            {
                [PropertyInt.Damage] = damage,
                [PropertyInt.WeaponSubtype] = (int)subtype,
                [PropertyInt.WeaponTime] = weaponTime,
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
