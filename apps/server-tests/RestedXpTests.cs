using System.Linq;
using ACE.Server.Entity;
using ACE.Server.Entity.PlayerUi;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ACE.Server.Tests;

[TestClass]
public class RestedXpTests
{
    private static readonly RestedXpRules.Settings Defaults = new(true, 0.01, 1.5, 1.0);

    [TestMethod]
    public void MaxAndRateAreFractionsOfTheLevel()
    {
        Assert.AreEqual(15_000, RestedXpRules.Max(10_000, Defaults));
        Assert.AreEqual(100, RestedXpRules.PerHour(10_000, Defaults));
    }

    [TestMethod]
    public void Accrue_AddsTheTimeRestedUpToTheMost()
    {
        Assert.AreEqual(150, RestedXpRules.Accrue(100, 1800, 100, 1000), 0.0001);
        Assert.AreEqual(1000, RestedXpRules.Accrue(900, 36000, 100, 1000), 0.0001);
    }

    [TestMethod]
    public void Accrue_KeepsTheFractionsAHeartbeatAdds()
    {
        // 5 seconds at 10 an hour is well under a point; a long pool is kept as a float so it isn't lost each time
        var pool = 0.0;

        for (var i = 0; i < 720; i++)
        {
            pool = RestedXpRules.Accrue(pool, 5, 10, 1000);
        }

        Assert.AreEqual(10, pool, 0.0001);
    }

    [TestMethod]
    public void Accrue_NeverShrinksAPoolOverTheMost()
    {
        // the server properties lowered since it was built up
        Assert.AreEqual(5000, RestedXpRules.Accrue(5000, 3600, 100, 1000), 0.0001);
    }

    [TestMethod]
    public void Accrue_IgnoresTimeGoingBackwards()
    {
        Assert.AreEqual(100, RestedXpRules.Accrue(100, -600, 100, 1000), 0.0001);
    }

    [TestMethod]
    public void Bonus_IsTheKillTimesTheBonusUntilThePoolRunsOut()
    {
        Assert.AreEqual(500, RestedXpRules.Bonus(10_000, 500, 1.0));
        Assert.AreEqual(250, RestedXpRules.Bonus(10_000, 500, 0.5));
        Assert.AreEqual(120, RestedXpRules.Bonus(120.7, 500, 1.0));
        Assert.AreEqual(0, RestedXpRules.Bonus(0.5, 500, 1.0));
        Assert.AreEqual(0, RestedXpRules.Bonus(10_000, 500, 0));
    }

    [TestMethod]
    public void Lines_SayHowLongUntilFull()
    {
        var lines = RestedXpRules.Lines(new PlayerUiRested(true, RestedXpRules.Resting, 1000, 3000, 1.0, 1000, 0));

        Assert.AreEqual("Rested experience: 1,000 of 3,000 (33%).", lines[0]);
        StringAssert.Contains(lines[1], "100% more experience");
        StringAssert.Contains(lines[2], "full in 2h 00m");
        Assert.AreEqual(3, lines.Count);
    }

    [TestMethod]
    public void Lines_SayWhyYoureNotResting()
    {
        var away = RestedXpRules.Lines(new PlayerUiRested(true, RestedXpRules.Away, 0, 3000, 1.0, 1000, 0));
        var combat = RestedXpRules.Lines(new PlayerUiRested(true, RestedXpRules.InCombat, 0, 3000, 1.0, 1000, 0));

        StringAssert.Contains(away[2], "In a town out of combat mode, or logged out");
        StringAssert.Contains(combat[2], "combat mode");
    }

    [TestMethod]
    public void Lines_CountWhatKillsEarnedSinceLogin()
    {
        var lines = RestedXpRules.Lines(new PlayerUiRested(true, RestedXpRules.Full, 3000, 3000, 1.0, 1000, 4500));

        Assert.IsTrue(lines.Any(l => l.Contains("4,500")));
    }

    [TestMethod]
    public void Lines_WhenOff()
    {
        var lines = RestedXpRules.Lines(new PlayerUiRested(false, RestedXpRules.Off, 0, 0, 1.0, 0, 0));

        Assert.AreEqual(1, lines.Count);
        StringAssert.Contains(lines[0], "turned off");
    }

    [TestMethod]
    public void Duration_IsShort()
    {
        Assert.AreEqual("45s", RestedXpRules.Duration(45));
        Assert.AreEqual("12m", RestedXpRules.Duration(12 * 60));
        Assert.AreEqual("3h 07m", RestedXpRules.Duration(3 * 3600 + 7 * 60));
        Assert.AreEqual("2d 04h", RestedXpRules.Duration(2 * 86400 + 4 * 3600));
    }
}
