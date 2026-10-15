using System.Collections.Generic;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Entity.Models;
using ACE.Server.Factories;
using ACE.Server.WorldObjects;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TreasureDeath = ACE.Database.Models.World.TreasureDeath;

namespace ACE.Server.Tests;

/// <summary>
/// Loot fixes that shipped without a test
/// </summary>
[TestClass]
public class LootRegressionTests
{
    #region Weapon damage range (#1276)

    [TestMethod]
    public void TierDamage_ARollOfOneReachesTheTopOfTheRange()
    {
        // #1276: the range ran from the bottom to the average, so no weapon rolled above its tier's average
        var (minimum, maximum) = LootGenerationFactory.GetTierDamageRange(100);

        Assert.AreEqual(minimum, LootGenerationFactory.RollTierDamage(100, 0.0f), 1e-9);
        Assert.AreEqual(maximum, LootGenerationFactory.RollTierDamage(100, 1.0f), 1e-9);
        Assert.IsTrue(LootGenerationFactory.RollTierDamage(100, 1.0f) > 100);
    }

    [TestMethod]
    public void TierDamage_TheRangeIsCenteredOnTheAverageAndRunsFrom25PercentBelowTheTop()
    {
        foreach (var average in new[] { 1.1, 10.0, 47.5, 100.0 })
        {
            var (minimum, maximum) = LootGenerationFactory.GetTierDamageRange(average);

            Assert.AreEqual(average, (minimum + maximum) / 2, 1e-9, $"average {average}");
            Assert.AreEqual(0.75, minimum / maximum, 1e-9, $"average {average}");
        }
    }

    [TestMethod]
    public void TierDamage_ABetterRollNeverGivesLessDamage()
    {
        var previous = LootGenerationFactory.RollTierDamage(50, 0.0f);

        for (var roll = 0.01f; roll <= 1.0f; roll += 0.01f)
        {
            var damage = LootGenerationFactory.RollTierDamage(50, roll);

            Assert.IsTrue(damage >= previous, $"roll {roll}");
            previous = damage;
        }
    }

    [DataTestMethod]
    [DataRow(0.0f)]
    [DataRow(0.5f)]
    [DataRow(2.0f)]
    public void DiminishingRoll_StaysBetweenZeroAndOneAndLootQualityRaisesTheFloor(float lootQualityMod)
    {
        var floor = 1 - System.Math.Exp(-lootQualityMod);

        for (var i = 0; i < 10000; i++)
        {
            var roll = LootGenerationFactory.GetDiminishingRoll(new TreasureDeath { LootQualityMod = lootQualityMod });

            Assert.IsTrue(roll >= floor * floor - 1e-6 && roll <= 1.0f, $"{roll} with loot quality {lootQualityMod}");
        }
    }

    #endregion

    #region Scouring Stone (#1246)

    [TestMethod]
    public void ArmorProtections_AverageOneAndLeaveTheArmorLevelAsItRolled()
    {
        for (var i = 0; i < 500; i++)
        {
            var armor = CreateArmor(armorLevel: 200);

            LootGenerationFactory.MutateArmorProtections(armor, new TreasureDeath { Tier = 5, LootQualityMod = 0.5f });

            // the armor level is the armor against the average damage type
            Assert.AreEqual(200, armor.ArmorLevel);
            Assert.AreEqual(armor.ArmorLevel, armor.BaseArmor);

            var protections = new List<double>();
            foreach (var protection in LootGenerationFactory.ProtectionProperties)
            {
                var value = armor.GetProperty(protection).Value;
                Assert.IsTrue(value >= 0.7 - 1e-9 && value <= 1.3 + 1e-9, $"{protection} {value}");
                protections.Add(value);
            }

            Assert.AreEqual(1.0, protections.Average(), 1e-9);
        }
    }

    [TestMethod]
    public void ArmorProtections_ClampingStillBalancesToOne()
    {
        // one offset far out past the range: it clamps, and what it loses spreads over the others
        var protections = LootGenerationFactory.BalanceProtections([0.3, 0.3, 0.3, 0.3, 0.3, 0.3, -3.0]);

        Assert.AreEqual(1.0, protections.Average(), 1e-9);
        Assert.AreEqual(0.7, protections[6], 1e-9);
        Assert.IsTrue(protections.All(value => value >= 0.7 - 1e-9 && value <= 1.3 + 1e-9));
    }

    [TestMethod]
    public void QuestArmorProtections_KeepTheirShapeAndAverageOne()
    {
        var armor = CreateArmor(armorLevel: 200);
        armor.SetProperty(PropertyFloat.ArmorModVsSlash, 2.0);

        LootGenerationFactory.BalanceAuthoredProtections(armor);

        // 8 / 7 was the average: every protection is divided by it, and the armor level is untouched
        Assert.AreEqual(2.0 * 7 / 8, armor.GetProperty(PropertyFloat.ArmorModVsSlash).Value, 1e-9);
        Assert.AreEqual(1.0 * 7 / 8, armor.GetProperty(PropertyFloat.ArmorModVsFire).Value, 1e-9);
        Assert.AreEqual(200, armor.ArmorLevel);
    }

    [TestMethod]
    public void ScouringStone_PutsLootArmorBackToTheArmorLevelItRolled()
    {
        // #1246: BaseArmor was kept before the protections were folded into the armor level, so scouring dropped the
        // armor level of anything whose protections rolled above average (they don't fold in any more)
        for (var i = 0; i < 500; i++)
        {
            var armor = CreateArmor(armorLevel: 200);

            LootGenerationFactory.MutateArmorProtections(armor, new TreasureDeath { Tier = 5 });
            var rolled = armor.ArmorLevel;

            // tinkered twice with steel
            armor.ArmorLevel += 40;
            armor.NumTimesTinkered = 2;

            Salvage.RevertTinkeredStats(armor);

            Assert.AreEqual(rolled, armor.ArmorLevel);
            Assert.AreEqual(0, armor.NumTimesTinkered);
        }
    }

    private static WorldObject CreateArmor(int armorLevel)
    {
        var weenie = new Weenie
        {
            WeenieClassId = 999999,
            ClassName = "lootregressiontest",
            WeenieType = WeenieType.Clothing,
            PropertiesInt = new Dictionary<PropertyInt, int>
            {
                [PropertyInt.ItemType] = (int)ItemType.Armor,
                [PropertyInt.ArmorLevel] = armorLevel,
            },
            PropertiesFloat = new Dictionary<PropertyFloat, double>
            {
                [PropertyFloat.ArmorModVsSlash] = 1.0,
                [PropertyFloat.ArmorModVsPierce] = 1.0,
                [PropertyFloat.ArmorModVsBludgeon] = 1.0,
                [PropertyFloat.ArmorModVsAcid] = 1.0,
                [PropertyFloat.ArmorModVsFire] = 1.0,
                [PropertyFloat.ArmorModVsCold] = 1.0,
                [PropertyFloat.ArmorModVsElectric] = 1.0,
            },
            PropertiesString = new Dictionary<PropertyString, string> { [PropertyString.Name] = "Armor" },
        };

        return new GenericObject(weenie, new ObjectGuid(0x80000001));
    }

    #endregion
}
