using System.Linq;
using ACE.Server.Entity;
using ACE.Server.Entity.PlayerUi;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ACE.Server.Tests;

[TestClass]
public class RestedXpTests
{
    private static readonly RestedXpRules.Settings Defaults =
        new(true, RestedXpCurve.FromText(RestedXpCurve.Default), 0, 1.5, 0.5, 1.0);

    [TestMethod]
    public void MaxAndRateAreFractionsOfTheLevel()
    {
        Assert.AreEqual(15_000, RestedXpRules.Max(10_000, Defaults));
        Assert.AreEqual(100, RestedXpRules.PerHour(10_000, 50, Defaults));
    }

    [TestMethod]
    public void TheDefaultCurve()
    {
        // 5% to level 10
        Assert.AreEqual(0.05, RestedXpRules.LevelsPerHour(1, Defaults), 1e-9);
        Assert.AreEqual(0.05, RestedXpRules.LevelsPerHour(10, Defaults), 1e-9);

        // then 0.1% less for each level past 10, to 1% at 50
        Assert.AreEqual(0.049, RestedXpRules.LevelsPerHour(11, Defaults), 1e-9);
        Assert.AreEqual(0.03, RestedXpRules.LevelsPerHour(30, Defaults), 1e-9);
        Assert.AreEqual(0.01, RestedXpRules.LevelsPerHour(50, Defaults), 1e-9);

        // then 0.02% less for each level past 50, to nothing at 100
        Assert.AreEqual(0.0098, RestedXpRules.LevelsPerHour(51, Defaults), 1e-9);
        Assert.AreEqual(0.005, RestedXpRules.LevelsPerHour(75, Defaults), 1e-9);
        Assert.AreEqual(0.0002, RestedXpRules.LevelsPerHour(99, Defaults), 1e-9);
        Assert.AreEqual(0, RestedXpRules.LevelsPerHour(100, Defaults), 1e-9);
        Assert.AreEqual(0, RestedXpRules.LevelsPerHour(126, Defaults), 1e-9);
    }

    [TestMethod]
    public void Curve_ReadsPiecesInAnyOrder()
    {
        var curve = RestedXpCurve.FromText("20:4:0.1; 0:5");

        Assert.AreEqual(0.05, curve.LevelsPerHour(20), 1e-9);
        Assert.AreEqual(0.039, curve.LevelsPerHour(21), 1e-9);
    }

    [TestMethod]
    public void Curve_ThatCantBeReadIsTheDefault()
    {
        Assert.IsNull(RestedXpCurve.Parse(""));
        Assert.IsNull(RestedXpCurve.Parse("10"));
        Assert.IsNull(RestedXpCurve.Parse("0:ten:0"));
        Assert.IsNull(RestedXpCurve.Parse("0:10:-1"));
        Assert.IsNull(RestedXpCurve.Parse("0:10, 0:5"));

        Assert.AreEqual(0.01, RestedXpCurve.FromText("nonsense").LevelsPerHour(50), 1e-9);
    }

    [TestMethod]
    public void TheLeastKeepsHighLevelsResting()
    {
        var withLeast = Defaults with { MinLevelsPerHour = 0.01 };

        Assert.AreEqual(0.01, RestedXpRules.LevelsPerHour(90, withLeast), 1e-9);
        Assert.AreEqual(0.01, RestedXpRules.LevelsPerHour(275, withLeast), 1e-9);
    }

    [TestMethod]
    public void Lines_SayRestHasEndedAtHighLevels()
    {
        var none = RestedXpRules.Lines(new PlayerUiRested(true, RestedXpRules.NoRest, 0, 3000, 1.0, 0, 0, 2000, 0.5));

        Assert.AreEqual(2, none.Count);
        StringAssert.Contains(none[1], "no longer build up");
    }

    [TestMethod]
    public void Accrue_AddsTheTimeRestedUpToTheMost()
    {
        Assert.AreEqual(150, RestedXpRules.Accrue(100, 1800, 100, 1000, 0, 0.5), 0.0001);
        Assert.AreEqual(1000, RestedXpRules.Accrue(900, 36000, 100, 1000, 0, 0.5), 0.0001);
    }

    [TestMethod]
    public void Accrue_KeepsTheFractionsAHeartbeatAdds()
    {
        // 5 seconds at 10 an hour is well under a point; a long pool is kept as a float so it isn't lost each time
        var pool = 0.0;

        for (var i = 0; i < 720; i++)
        {
            pool = RestedXpRules.Accrue(pool, 5, 10, 1000, 0, 0.5);
        }

        Assert.AreEqual(10, pool, 0.0001);
    }

    [TestMethod]
    public void Accrue_NeverShrinksAPoolOverTheMost()
    {
        // the server properties lowered since it was built up
        Assert.AreEqual(5000, RestedXpRules.Accrue(5000, 3600, 100, 1000, 0, 0.5), 0.0001);
    }

    [TestMethod]
    public void Accrue_IgnoresTimeGoingBackwards()
    {
        Assert.AreEqual(100, RestedXpRules.Accrue(100, -600, 100, 1000, 0, 0.5), 0.0001);
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
        var lines = RestedXpRules.Lines(
            new PlayerUiRested(true, RestedXpRules.Resting, 1000, 3000, 1.0, 1000, 0, 2000, 0.5)
        );

        Assert.AreEqual("Rested experience: 1,000 of 3,000 (33%).", lines[0]);
        StringAssert.Contains(lines[1], "100% more experience");
        StringAssert.Contains(lines[2], "full in 3h 00m");
        Assert.AreEqual(3, lines.Count);
    }

    [TestMethod]
    public void Lines_SayWhyYoureNotResting()
    {
        var away = RestedXpRules.Lines(new PlayerUiRested(true, RestedXpRules.Away, 0, 3000, 1.0, 1000, 0, 2000, 0.5));
        var combat = RestedXpRules.Lines(new PlayerUiRested(true, RestedXpRules.InCombat, 0, 3000, 1.0, 1000, 0, 2000, 0.5));

        StringAssert.Contains(away[2], "In a town out of combat mode, or logged out");
        StringAssert.Contains(combat[2], "combat mode");
    }

    [TestMethod]
    public void Lines_CountWhatKillsEarnedSinceLogin()
    {
        var lines = RestedXpRules.Lines(
            new PlayerUiRested(true, RestedXpRules.Full, 3000, 3000, 1.0, 1000, 4500, 2000, 0.5)
        );

        Assert.IsTrue(lines.Any(l => l.Contains("4,500")));
    }

    [TestMethod]
    public void Lines_WhenOff()
    {
        var lines = RestedXpRules.Lines(new PlayerUiRested(false, RestedXpRules.Off, 0, 0, 1.0, 0, 0, 2000, 0.5));

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

    [TestMethod]
    public void XpBounds_AreTheLevelAndTheNext()
    {
        ulong[] table = [0, 0, 1000, 2500, 5000];

        Assert.AreEqual((1000L, 2500L, false), PlayerUiXpData.Bounds(table, 2, 4));
    }

    [TestMethod]
    public void XpBounds_AtTheHighestLevelAreFull()
    {
        ulong[] table = [0, 0, 1000, 2500, 5000];

        Assert.AreEqual((2500L, 2500L, true), PlayerUiXpData.Bounds(table, 3, 3));
        Assert.AreEqual((5000L, 5000L, true), PlayerUiXpData.Bounds(table, 9, 99));
    }

    [TestMethod]
    public void Accrue_SlowsPastEachFullLevel()
    {
        // 500 an hour to a level's cost (1,000), then half that to the most (1,500)
        Assert.AreEqual(1000, RestedXpRules.Accrue(0, 2 * 3600, 500, 1500, 1000, 0.5), 0.0001);
        Assert.AreEqual(1250, RestedXpRules.Accrue(0, 3 * 3600, 500, 1500, 1000, 0.5), 0.0001);
        Assert.AreEqual(1500, RestedXpRules.Accrue(0, 99 * 3600, 500, 1500, 1000, 0.5), 0.0001);
        Assert.AreEqual(250, RestedXpRules.RateAt(1200, 500, 1000, 0.5), 0.0001);
        Assert.AreEqual(500, RestedXpRules.RateAt(999, 500, 1000, 0.5), 0.0001);
    }

    [TestMethod]
    public void Accrue_HeartbeatsAddUpLikeOneLongRest()
    {
        var pool = 0.0;

        for (var i = 0; i < 3 * 720; i++)
        {
            pool = RestedXpRules.Accrue(pool, 5, 500, 1500, 1000, 0.5);
        }

        Assert.AreEqual(1250, pool, 0.001);
    }

    [TestMethod]
    public void SecondsToFull_CountsTheSlowerStretch()
    {
        Assert.AreEqual(4 * 3600, RestedXpRules.SecondsToFull(0, 500, 1500, 1000, 0.5).Value, 0.001);
        Assert.AreEqual(2 * 3600, RestedXpRules.SecondsToFull(1000, 500, 1500, 1000, 0.5).Value, 0.001);
        Assert.AreEqual(0, RestedXpRules.SecondsToFull(1500, 500, 1500, 1000, 0.5).Value, 0.001);
    }

    [TestMethod]
    public void AFullSlowdownStopsAtALevel()
    {
        Assert.AreEqual(1000, RestedXpRules.Accrue(0, 10 * 3600, 500, 1500, 1000, 1), 0.0001);
        Assert.IsNull(RestedXpRules.SecondsToFull(0, 500, 1500, 1000, 1));
    }

    [TestMethod]
    public void Lines_SayItsSlowerPastALevel()
    {
        var lines = RestedXpRules.Lines(
            new PlayerUiRested(true, RestedXpRules.Resting, 2500, 3000, 1.0, 1000, 0, 2000, 0.5)
        );

        // half speed past the first level's worth
        StringAssert.Contains(lines[2], "you gain 500 an hour");
        StringAssert.Contains(lines[2], "full in 1h 00m");
        Assert.IsTrue(lines.Any(l => l.Contains("50% slower")));
    }

    [TestMethod]
    public void NoMost_KeepsGrowingEverSlower()
    {
        Assert.AreEqual(0, RestedXpRules.Max(10_000, Defaults with { MaxLevels = 0 }));

        // a level's worth (1,000) in 2 hours, the next in 4, the next in 8
        Assert.AreEqual(1000, RestedXpRules.Accrue(0, 2 * 3600, 500, 0, 1000, 0.5), 0.0001);
        Assert.AreEqual(2000, RestedXpRules.Accrue(0, 6 * 3600, 500, 0, 1000, 0.5), 0.0001);
        Assert.AreEqual(3000, RestedXpRules.Accrue(0, 14 * 3600, 500, 0, 1000, 0.5), 0.0001);
        Assert.IsNull(RestedXpRules.SecondsToFull(0, 500, 0, 1000, 0.5));
    }

    [TestMethod]
    public void Lines_WithNoMostSayLevelsWorth()
    {
        var lines = RestedXpRules.Lines(
            new PlayerUiRested(true, RestedXpRules.Resting, 1500, 0, 1.0, 1000, 0, 1000, 0.5)
        );

        Assert.AreEqual("Rested experience: 1,500, 1.5 levels' worth.", lines[0]);
        StringAssert.Contains(lines[2], "you gain 500 an hour here.");
    }

    [TestMethod]
    public void NothingBuildsUpBelowTheLeastLevel()
    {
        var fromTen = Defaults with { MinLevel = 10 };

        Assert.AreEqual(0, RestedXpRules.LevelsPerHour(9, fromTen), 1e-9);
        Assert.AreEqual(0.05, RestedXpRules.LevelsPerHour(10, fromTen), 1e-9);
        Assert.AreEqual(0, RestedXpRules.PerHour(10_000, 5, fromTen));
    }

    [TestMethod]
    public void LoggedOutBuildsAtTheOfflineRate()
    {
        Assert.AreEqual(500, RestedXpRules.OfflinePerHour(1000, Defaults with { OfflineRate = 0.5 }));
        Assert.AreEqual(1000, RestedXpRules.OfflinePerHour(1000, Defaults));
    }

    [TestMethod]
    public void Lines_SayWhenRestStartsAndTheLoggedOutRate()
    {
        var low = RestedXpRules.Lines(
            new PlayerUiRested(true, RestedXpRules.LowLevel, 0, 0, 1.0, 0, 0, 1000, 0.5, 10, 0.5)
        );
        var away = RestedXpRules.Lines(
            new PlayerUiRested(true, RestedXpRules.Away, 0, 0, 1.0, 1000, 0, 2000, 0.5, 10, 0.5)
        );

        Assert.AreEqual(2, low.Count);
        StringAssert.Contains(low[1], "at level 10");
        StringAssert.Contains(away[2], "1,000 an hour, and 500 an hour logged out");
    }
}

