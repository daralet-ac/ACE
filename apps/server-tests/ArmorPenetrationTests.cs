using ACE.Server.Entity;
using ACE.Server.WorldObjects;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ACE.Server.Tests;

[TestClass]
public class ArmorPenetrationTests
{
    private static readonly int[] TierStarts = [10, 20, 30, 40, 50, 75, 100];

    /// <summary>
    /// The damage a hit through this much armor deals with penetration, over the damage without it
    /// </summary>
    private static float GetDamageGain(float penetration, float armor, float referenceArmor)
    {
        var remaining = ArmorPenetration.GetArmorRemaining(penetration, armor, referenceArmor, armor);

        return SkillFormula.CalcArmorMod(armor * remaining) / SkillFormula.CalcArmorMod(armor) - 1;
    }

    [TestMethod]
    public void AverageCleave_AddsTenPercentAgainstAverageArmorAtEveryTier()
    {
        foreach (var level in TierStarts)
        {
            var reference = TierPower.GetMonsterArmorWard(level);

            Assert.AreEqual(0.10f, GetDamageGain(ArmorPenetration.ReferencePenetration, reference, reference), 1e-4, $"level {level}");
        }
    }

    [TestMethod]
    public void Bonus_ScalesLinearlyWithArmorUpToTheCap()
    {
        const float reference = 500;
        var penetration = ArmorPenetration.ReferencePenetration;

        Assert.AreEqual(0.07f, ArmorPenetration.GetDamageBonus(penetration, reference * 0.7f, reference), 1e-4);
        Assert.AreEqual(0.13f, ArmorPenetration.GetDamageBonus(penetration, reference * 1.3f, reference), 1e-4);

        // capped at x1.3 the average
        Assert.AreEqual(0.13f, ArmorPenetration.GetDamageBonus(penetration, reference * 3.0f, reference), 1e-4);
    }

    [TestMethod]
    public void Penetration_AddsUp()
    {
        // average cleave + Armor Rending at 500 skill (20%): 25% against average armor
        Assert.AreEqual(0.25f, ArmorPenetration.GetDamageBonus(ArmorPenetration.ReferencePenetration + 0.2f, 500, 500), 1e-4);
    }

    [TestMethod]
    public void Penetration_DoesNothingWithoutArmorOrPenetration()
    {
        Assert.AreEqual(1.0f, ArmorPenetration.GetArmorRemaining(0.2f, 0, 500, 0));
        Assert.AreEqual(1.0f, ArmorPenetration.GetArmorRemaining(0.0f, 500, 500, 500));
        Assert.AreEqual(1.0f, ArmorPenetration.GetArmorRemaining(0.2f, -50, 500, -50));
    }

    [TestMethod]
    public void Penetration_NeverIgnoresMoreThanAllArmor()
    {
        // 20 armor can only ever add 20% damage
        Assert.AreEqual(0.0f, ArmorPenetration.GetArmorRemaining(0.5f, 20, 20, 20));
        Assert.AreEqual(0.20f, GetDamageGain(0.5f, 20, 20), 1e-4);
    }

    [TestMethod]
    public void Penetration_ScalesWithBaseArmorButRemovesFromWhatsLeft()
    {
        // imperil takes a target from 500 to 300: the bonus still counts its base 500, and comes off the 300 left
        var remaining = ArmorPenetration.GetArmorRemaining(ArmorPenetration.ReferencePenetration, 500, 500, 300);
        var gain = SkillFormula.CalcArmorMod(300 * remaining) / SkillFormula.CalcArmorMod(300) - 1;

        Assert.AreEqual(0.10f, gain, 1e-4);
    }
}
