using System.Linq;
using ACE.Server.Factories.Tables;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ACE.Server.Tests;

[TestClass]
public class LootWorkmanshipTests
{
    private const int Rolls = 20000;

    // t0 - t7, indexed by tier - 1
    private static readonly int[] TierMinimums = [1, 1, 2, 3, 4, 5, 6, 7];
    private static readonly int[] TierMaximums = [1, 2, 3, 4, 5, 7, 8, 10];

    [TestMethod]
    public void FromPowerScore_RunsFromOneToTen()
    {
        Assert.AreEqual(1, WorkmanshipChance.FromPowerScore(0.0));
        Assert.AreEqual(10, WorkmanshipChance.FromPowerScore(1.0));

        // scores a little outside 0 - 1 (armor can roll past its listed max) stay in range
        Assert.AreEqual(1, WorkmanshipChance.FromPowerScore(-0.1));
        Assert.AreEqual(10, WorkmanshipChance.FromPowerScore(1.2));
    }

    [TestMethod]
    public void FromPowerScore_AStrongerItemNeverGetsLessWorkmanship()
    {
        var previous = WorkmanshipChance.FromPowerScore(0.0);

        for (var score = 0.001; score <= 1.0; score += 0.001)
        {
            var workmanship = WorkmanshipChance.FromPowerScore(score);

            Assert.IsTrue(workmanship >= previous, $"score {score}");
            Assert.IsTrue(workmanship - previous <= 1, $"score {score} skipped a workmanship");

            previous = workmanship;
        }
    }

    [TestMethod]
    public void FromPowerScore_TypicalWeaponsLandOnTheirTier()
    {
        // median power scores of loot melee weapons per tier, from simulating the loot rolls
        double[] medianScorePerTier = [0.11, 0.16, 0.21, 0.26, 0.32, 0.41, 0.55, 0.73];

        for (var tier = 0; tier < 8; tier++)
        {
            Assert.AreEqual(TierMinimums[tier], WorkmanshipChance.FromPowerScore(medianScorePerTier[tier]), $"t{tier}");
        }
    }

    [TestMethod]
    public void Roll_StaysInTheTiersRangeAndMostOftenMatchesTheTier()
    {
        for (var tier = 1; tier <= 8; tier++)
        {
            var counts = Enumerable.Range(0, Rolls)
                .Select(_ => WorkmanshipChance.Roll(tier))
                .GroupBy(workmanship => workmanship)
                .ToDictionary(group => group.Key, group => group.Count());

            Assert.IsTrue(counts.Keys.All(workmanship => workmanship >= TierMinimums[tier - 1]), $"tier {tier}");
            Assert.IsTrue(counts.Keys.All(workmanship => workmanship <= TierMaximums[tier - 1]), $"tier {tier}");

            var mostCommon = counts.OrderByDescending(pair => pair.Value).First().Key;
            Assert.AreEqual(TierMinimums[tier - 1], mostCommon, $"tier {tier}");
        }
    }

    [TestMethod]
    public void Roll_BetterDropsReachTheTopOfT7()
    {
        var rolls = Enumerable.Range(0, Rolls).Select(_ => WorkmanshipChance.Roll(8)).ToList();

        Assert.IsTrue(rolls.Contains(9));
        Assert.IsTrue(rolls.Contains(10));
    }

    [TestMethod]
    public void Roll_LootQualityRaisesTheAverage()
    {
        var plain = Enumerable.Range(0, Rolls).Average(_ => WorkmanshipChance.Roll(8));
        var quality = Enumerable.Range(0, Rolls).Average(_ => WorkmanshipChance.Roll(8, 0.5f));

        Assert.IsTrue(quality > plain + 0.2, $"plain {plain}, quality {quality}");
    }

    [TestMethod]
    public void TrophyQuality_KeepsItsOriginalRanges()
    {
        var t1 = Enumerable.Range(0, Rolls).Select(_ => TrophyQualityChance.Roll(1)).Distinct().ToList();
        CollectionAssert.AreEquivalent(new[] { 1 }, t1);

        var t8 = Enumerable.Range(0, Rolls).Select(_ => TrophyQualityChance.Roll(8)).ToList();
        Assert.IsTrue(t8.All(quality => quality is >= 6 and <= 10));
        Assert.IsTrue(t8.Count(quality => quality == 7) > t8.Count(quality => quality == 6));
    }
}
