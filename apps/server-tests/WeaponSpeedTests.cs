using ACE.Server.Entity;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ACE.Server.Tests;

[TestClass]
public class WeaponSpeedTests
{
    [TestMethod]
    public void WeaponTimeIsSixtiethsOfASecondPerHitAtReferenceQuickness()
    {
        Assert.AreEqual(1.0f, WeaponSpeed.GetSecondsPerHit(60), 0.0001f);
        Assert.AreEqual(0.5f, WeaponSpeed.GetSecondsPerHit(30), 0.0001f);
    }

    [TestMethod]
    public void QuicknessShortensEachHit()
    {
        var reference = WeaponSpeed.GetSecondsPerHit(60);
        var low = WeaponSpeed.GetSecondsPerHit(60, 50);
        var high = WeaponSpeed.GetSecondsPerHit(60, 400);

        Assert.IsTrue(low > reference);
        Assert.IsTrue(high < reference);
        // 1 + 100/600 over 1 + 400/600
        Assert.AreEqual(0.7f, high, 0.0001f);
    }

    [TestMethod]
    public void QuicknessHelpsMissileWeaponsLess()
    {
        var melee = WeaponSpeed.GetSecondsPerHit(60, 400);
        var missile = WeaponSpeed.GetSecondsPerHit(60, 400, true);

        Assert.AreEqual(1.0f, WeaponSpeed.GetSecondsPerHit(60, WeaponSpeed.ReferenceQuickness, true), 0.0001f);
        Assert.IsTrue(missile < 1.0f);
        Assert.IsTrue(missile > melee);
    }

    [TestMethod]
    public void SpeedPercentScalesWeaponTime()
    {
        Assert.AreEqual(45, WeaponSpeed.ApplySpeedPercent(50, -10));
        Assert.AreEqual(56, WeaponSpeed.ApplySpeedPercent(50, 12));
        Assert.AreEqual(50, WeaponSpeed.ApplySpeedPercent(50, 0));
    }

    [TestMethod]
    public void SpeedPercentIsCapped()
    {
        // Prodigal Swift Killer and stacked buffs can't make a weapon more than 75% faster
        Assert.AreEqual(13, WeaponSpeed.ApplySpeedPercent(50, -1000));
        Assert.AreEqual(1, WeaponSpeed.ApplySpeedPercent(1, -75));
    }

    [TestMethod]
    public void AnimSpeedPlaysAnimationInTargetTime()
    {
        Assert.AreEqual(2.0f, WeaponSpeed.GetAnimSpeed(1.33f, 0.665f), 0.0001f);
        Assert.AreEqual(WeaponSpeed.MaxAnimSpeed, WeaponSpeed.GetAnimSpeed(1.33f, 0.1f));
        Assert.AreEqual(WeaponSpeed.MinAnimSpeed, WeaponSpeed.GetAnimSpeed(1.33f, 10.0f));
        Assert.AreEqual(WeaponSpeed.MaxAnimSpeed, WeaponSpeed.GetAnimSpeed(1.33f, 0.0f));
        Assert.AreEqual(1.0f, WeaponSpeed.GetAnimSpeed(0.0f, 1.0f));
    }
}
