using ACE.Entity.Enum;
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
    public void EveryTwoRanksCastsTheNextRestedSpell()
    {
        Assert.IsNull(CampfireRules.RankedRestSpell(0));
        Assert.IsNull(CampfireRules.RankedRestSpell(1));
        Assert.AreEqual(SpellId.CampfireRest6Percent, CampfireRules.RankedRestSpell(2));
        Assert.AreEqual(SpellId.CampfireRest6Percent, CampfireRules.RankedRestSpell(3));
        Assert.AreEqual(SpellId.CampfireRest8Percent, CampfireRules.RankedRestSpell(6));
        Assert.AreEqual(SpellId.CampfireRest10Percent, CampfireRules.RankedRestSpell(10));
        Assert.AreEqual(SpellId.CampfireRest10Percent, CampfireRules.RankedRestSpell(14));
    }

    [TestMethod]
    public void RankedRestSpellsAreTheOnesTheWorldDbAdds()
    {
        Assert.AreEqual(6865u, (uint)SpellId.CampfireRest6Percent);
        Assert.AreEqual(6869u, (uint)SpellId.CampfireRest10Percent);
    }

    [TestMethod]
    public void BuffXpCountsTheWayTheXpBonusDoes()
    {
        Assert.AreEqual(0.12f, CampfireRules.BuffXp(1.12f, true), 1e-6f);
        Assert.AreEqual(0.12f, CampfireRules.BuffXp(0.12f, false), 1e-6f);
    }

    [TestMethod]
    public void RankXpIsWhatTheSpellGivesOverRested()
    {
        Assert.AreEqual(0.01f, CampfireRules.RankXp((uint)SpellId.CampfireRest6Percent), 1e-6f);
        Assert.AreEqual(0.05f, CampfireRules.RankXp((uint)SpellId.CampfireRest10Percent), 1e-6f);

        // Rested, Well Rested and Very Well Rested owe nothing to a rank
        Assert.AreEqual(0f, CampfireRules.RankXp((uint)SpellId.CampfireRest1));
        Assert.AreEqual(0f, CampfireRules.RankXp((uint)SpellId.CampfireRest3));
    }

    [TestMethod]
    public void MessagesSayTheXpAndTheRankShare()
    {
        Assert.AreEqual("You feel well rested by the fire: +10% XP.", CampfireRules.AppliedMessage(0.10f, 0f));
        Assert.AreEqual(
            "You feel well rested by the fire: +15% XP (+5% of it from the fire's Allegiance Rank).",
            CampfireRules.AppliedMessage(0.15f, 0.05f)
        );
        Assert.AreEqual(
            "Rested by a campfire: +12.5% XP for 45m (+2.5% of it from the fire's Allegiance Rank).",
            CampfireRules.RestedLine(0.125f, 0.025f, 45 * 60)
        );
        Assert.AreEqual("Rested by a campfire: +10% XP.", CampfireRules.RestedLine(0.10f, 0f, null));
    }
}
