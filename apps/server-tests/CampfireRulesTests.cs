using ACE.Server.Entity;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ACE.Server.Tests;

[TestClass]
public class CampfireRulesTests
{
    [TestMethod]
    public void RankIsNeverNegative()
    {
        Assert.AreEqual(0, CampfireRules.Rank(null));
        Assert.AreEqual(0, CampfireRules.Rank(-3));
        Assert.AreEqual(7, CampfireRules.Rank(7));
    }

    [TestMethod]
    public void FireGrowsTwoPercentARankUpToTen()
    {
        Assert.AreEqual(1.0f, CampfireRules.ScaleMod(0), 1e-6f);
        Assert.AreEqual(1.02f, CampfireRules.ScaleMod(1), 1e-6f);
        Assert.AreEqual(1.1f, CampfireRules.ScaleMod(5), 1e-6f);
        Assert.AreEqual(1.2f, CampfireRules.ScaleMod(10), 1e-6f);
        Assert.AreEqual(1.2f, CampfireRules.ScaleMod(14), 1e-6f);
    }

    [TestMethod]
    public void RestedGivesOnePercentForEveryTwoRanks()
    {
        Assert.AreEqual(0f, CampfireRules.XpBonus(0), 1e-6f);
        Assert.AreEqual(0f, CampfireRules.XpBonus(1), 1e-6f);
        Assert.AreEqual(0.01f, CampfireRules.XpBonus(2), 1e-6f);
        Assert.AreEqual(0.01f, CampfireRules.XpBonus(3), 1e-6f);
        Assert.AreEqual(0.05f, CampfireRules.XpBonus(10), 1e-6f);
        Assert.AreEqual(0.06f, CampfireRules.XpBonus(12), 1e-6f);
    }

    [TestMethod]
    public void StatModMultiplierAddsTheBonusToTheBuff()
    {
        // a multiplicative buff of 1.10 (+10%) at rank 4 is worth 1.12 (+12%)
        Assert.AreEqual(1.12f, 1.10f * CampfireRules.StatModMultiplier(1.10f, 0.02f), 1e-6f);

        // an additive buff of 0.10 at rank 4 is worth 0.12
        Assert.AreEqual(0.12f, 0.10f * CampfireRules.StatModMultiplier(0.10f, 0.02f), 1e-6f);

        // no rank bonus, or nothing to scale, leaves the buff alone
        Assert.AreEqual(1.0f, CampfireRules.StatModMultiplier(1.10f, 0f), 1e-6f);
        Assert.AreEqual(1.0f, CampfireRules.StatModMultiplier(0f, 0.02f), 1e-6f);
    }
}
