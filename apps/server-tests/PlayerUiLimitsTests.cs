using System;
using ACE.Server.Entity.PlayerUi;
using ACE.Server.Managers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ACE.Server.Tests;

[TestClass]
public class PlayerUiLimitsTests
{
    private static readonly DateTime Start = new(2026, 10, 21, 12, 0, 0, DateTimeKind.Utc);

    private static readonly PlayerUiLimits.Settings Defaults = new()
    {
        KindInterval = TimeSpan.FromMilliseconds(400),
        HelloInterval = TimeSpan.FromSeconds(2),
        PerSecond = 10,
        Burst = 20,
    };

    [TestMethod]
    public void SameKindTooSoonIsRefused()
    {
        var limits = new PlayerUiLimits();

        Assert.IsTrue(limits.Allow(1, "fellowship", Start, Defaults));
        Assert.IsFalse(limits.Allow(1, "fellowship", Start.AddMilliseconds(399), Defaults));
        Assert.IsTrue(limits.Allow(1, "fellowship", Start.AddMilliseconds(400), Defaults));
    }

    [TestMethod]
    public void HelloHasItsOwnLongerInterval()
    {
        var limits = new PlayerUiLimits();

        Assert.IsTrue(limits.Allow(1, "hello", Start, Defaults));
        Assert.IsFalse(limits.Allow(1, "hello", Start.AddSeconds(1), Defaults));
        Assert.IsTrue(limits.Allow(1, "hello", Start.AddSeconds(2), Defaults));
    }

    [TestMethod]
    public void EveryKindTogetherSharesTheBudget()
    {
        var limits = new PlayerUiLimits();

        // a different kind each time passes the per-kind check, so only the budget stops it
        for (var i = 0; i < 20; i++)
        {
            Assert.IsTrue(limits.Allow(1, "kind" + i, Start, Defaults), $"request {i}");
        }

        Assert.IsFalse(limits.Allow(1, "kind20", Start, Defaults));

        // 10 a second come back: one in 100ms
        Assert.IsTrue(limits.Allow(1, "kind21", Start.AddMilliseconds(100), Defaults));
        Assert.IsFalse(limits.Allow(1, "kind22", Start.AddMilliseconds(100), Defaults));
    }

    [TestMethod]
    public void TheBudgetIsPerPlayer()
    {
        var limits = new PlayerUiLimits();

        for (var i = 0; i < 20; i++)
        {
            limits.Allow(1, "kind" + i, Start, Defaults);
        }

        Assert.IsFalse(limits.Allow(1, "other", Start, Defaults));
        Assert.IsTrue(limits.Allow(2, "other", Start, Defaults));
    }

    [TestMethod]
    public void TheBudgetRefillsNoHigherThanTheBurst()
    {
        var limits = new PlayerUiLimits();

        limits.Allow(1, "first", Start, Defaults);
        var later = Start.AddMinutes(10);

        for (var i = 0; i < 20; i++)
        {
            Assert.IsTrue(limits.Allow(1, "kind" + i, later, Defaults), $"request {i}");
        }

        Assert.IsFalse(limits.Allow(1, "kind20", later, Defaults));
    }

    [TestMethod]
    public void ZeroTurnsTheLimitsOff()
    {
        var limits = new PlayerUiLimits();
        var off = new PlayerUiLimits.Settings { KindInterval = TimeSpan.Zero, HelloInterval = TimeSpan.Zero, PerSecond = 0, Burst = 1 };

        for (var i = 0; i < 100; i++)
        {
            Assert.IsTrue(limits.Allow(1, "fellowship", Start, off));
            Assert.IsTrue(limits.Allow(1, "hello", Start, off));
        }
    }

    [TestMethod]
    public void ForgetStartsThePlayerAfresh()
    {
        var limits = new PlayerUiLimits();

        for (var i = 0; i < 20; i++)
        {
            limits.Allow(1, "kind" + i, Start, Defaults);
        }

        limits.Forget(1);

        Assert.IsTrue(limits.Allow(1, "kind0", Start, Defaults));
    }

    [TestMethod]
    public void TheDefaultsAreServerProperties()
    {
        var properties = DefaultPropertyManager.DefaultLongProperties;

        Assert.AreEqual(400L, properties["playerui_request_interval_ms"].Item);
        Assert.AreEqual(2000L, properties["playerui_hello_interval_ms"].Item);
        Assert.AreEqual(10L, properties["playerui_requests_per_second"].Item);
        Assert.AreEqual(20L, properties["playerui_request_burst"].Item);
    }
}
