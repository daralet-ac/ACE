using System.Reflection;
using ACE.Entity.Enum;
using ACE.Server.Entity;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ACE.Server.Tests;

[TestClass]
public class MagicFormulaTests
{
    private const double Tolerance = 1e-5;

    // ---- SpellDamageModifiers / SpellMitigationModifiers

    [TestMethod]
    public void SpellDamageModifiers_AllAtOne_LeaveBaseDamageUnchanged()
    {
        Assert.AreEqual(150.0f, SetAllFields(new SpellDamageModifiers(), 1.0f).Apply(150.0f));
    }

    [TestMethod]
    public void SpellDamageModifiers_EveryModifierIsApplied()
    {
        foreach (var field in typeof(SpellDamageModifiers).GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            object modifiers = SetAllFields(new SpellDamageModifiers(), 1.0f);
            field.SetValue(modifiers, 2.0f);

            Assert.AreEqual(20.0f, ((SpellDamageModifiers)modifiers).Apply(10.0f), $"{field.Name} isn't applied");
        }
    }

    [TestMethod]
    public void SpellDamageModifiers_NotFilledIn_GiveZeroDamage()
    {
        Assert.AreEqual(0.0f, new SpellDamageModifiers().Apply(150.0f));
    }

    [TestMethod]
    public void SpellMitigationModifiers_EveryModifierIsApplied()
    {
        foreach (var field in typeof(SpellMitigationModifiers).GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            object modifiers = SetAllFields(new SpellMitigationModifiers(), 1.0f);
            field.SetValue(modifiers, 0.5f);

            Assert.AreEqual(5.0f, ((SpellMitigationModifiers)modifiers).Apply(10.0f), $"{field.Name} isn't applied");
        }
    }

    // ---- Resist

    [TestMethod]
    public void ResistedMod_FullHitPartialAndFullResist()
    {
        Assert.AreEqual(1.0f, MagicFormulas.GetResistedMod(PartialEvasion.None));
        Assert.AreEqual(0.5f, MagicFormulas.GetResistedMod(PartialEvasion.Some));
        Assert.AreEqual(0.0f, MagicFormulas.GetResistedMod(PartialEvasion.All));
    }

    // ---- Range and spellcraft

    [TestMethod]
    public void MaxCastRange_GrowsWithSkill_UpToTheCap()
    {
        Assert.AreEqual(25.0f, MagicFormulas.GetMaxCastRange(5.0f, 0.2f, 100, 75.0f), Tolerance);
        Assert.AreEqual(75.0f, MagicFormulas.GetMaxCastRange(10.0f, 1.0f, 300, 75.0f), Tolerance);
    }

    [TestMethod]
    public void SpellcraftSkillBonus_IsTenPercent_RoundedDown()
    {
        Assert.AreEqual(30u, MagicFormulas.GetSpellcraftSkillBonus(300, 0));
        Assert.AreEqual(30u, MagicFormulas.GetSpellcraftSkillBonus(309, 0));
        Assert.AreEqual(45u, MagicFormulas.GetSpellcraftSkillBonus(300, 150));
    }

    [TestMethod]
    public void ProcSpellcraftDamageMod_IsOnePercentOfSpellcraft()
    {
        Assert.AreEqual(3.0f, MagicFormulas.GetProcSpellcraftDamageMod(300), Tolerance);
        Assert.AreEqual(1.5f, MagicFormulas.GetProcSpellcraftDamageMod(150), Tolerance);
    }

    [TestMethod]
    public void SelfTargetProcMod_IsSkillVsPower_FromHalfToDouble()
    {
        Assert.AreEqual(1.0f, MagicFormulas.GetSelfTargetProcMod(250, 100, 260), Tolerance);
        Assert.AreEqual(2.0f, MagicFormulas.GetSelfTargetProcMod(400, 0, 100), Tolerance);
        Assert.AreEqual(0.5f, MagicFormulas.GetSelfTargetProcMod(100, 0, 400), Tolerance);
        Assert.AreEqual(1.0f, MagicFormulas.GetSelfTargetProcMod(100, 0, 0), "a spell with no power");
    }

    // ---- Mitigation

    [TestMethod]
    public void WardDebuffDurationMod_IsHalfwayToTheWardMod()
    {
        Assert.AreEqual(0.75f, MagicFormulas.GetWardDebuffDurationMod(0.5f), Tolerance);
        Assert.AreEqual(1.0f, MagicFormulas.GetWardDebuffDurationMod(1.0f), Tolerance);
        Assert.AreEqual(0.5f, MagicFormulas.GetWardDebuffDurationMod(0.0f), Tolerance);
    }

    [TestMethod]
    public void PvpAbsorbMod_IsSeventyTwoPercentEffective()
    {
        Assert.AreEqual(1.0f, MagicFormulas.GetPvpAbsorbMod(1.0f), Tolerance);
        Assert.AreEqual(0.64f, MagicFormulas.GetPvpAbsorbMod(0.5f), Tolerance);
    }

    [TestMethod]
    public void StrikethroughPenalty_SplitsDamageByTargetsStruck()
    {
        Assert.AreEqual(1.0f, MagicFormulas.GetStrikethroughPenalty(0), Tolerance);
        Assert.AreEqual(0.5f, MagicFormulas.GetStrikethroughPenalty(1), Tolerance);
        Assert.AreEqual(1.0f / 3.0f, MagicFormulas.GetStrikethroughPenalty(2), Tolerance);
    }

    [TestMethod]
    public void ShieldMagicAbsorbMod_MatchesTheShieldTable()
    {
        // speced/trained, 100 skill = 0%
        Assert.AreEqual(1.0f, MagicFormulas.GetShieldMagicAbsorbMod(1.0f, 100, true), Tolerance);
        Assert.AreEqual(1.0f, MagicFormulas.GetShieldMagicAbsorbMod(1.0f, 100, false), Tolerance);

        // speced 200 = 30%, trained 200 = 24%
        Assert.AreEqual(0.7f, MagicFormulas.GetShieldMagicAbsorbMod(1.0f, 200, true), Tolerance);
        Assert.AreEqual(0.76f, MagicFormulas.GetShieldMagicAbsorbMod(1.0f, 200, false), Tolerance);

        // speced 300 = 60%, trained 300 = 48%
        Assert.AreEqual(0.4f, MagicFormulas.GetShieldMagicAbsorbMod(1.0f, 300, true), Tolerance);
        Assert.AreEqual(0.52f, MagicFormulas.GetShieldMagicAbsorbMod(1.0f, 300, false), Tolerance);

        // base skill above 433 counts as 433
        Assert.AreEqual(
            MagicFormulas.GetShieldMagicAbsorbMod(1.0f, 433, true),
            MagicFormulas.GetShieldMagicAbsorbMod(1.0f, 500, true)
        );

        // the cap scales the reduction
        Assert.AreEqual(0.7f, MagicFormulas.GetShieldMagicAbsorbMod(0.5f, 300, true), Tolerance);
    }

    [TestMethod]
    public void MagicAbsorbingMod_ReachesItsMaxAt319BaseMagicDefense()
    {
        // a 25% item: full effect from 319, none at 69 and below
        Assert.AreEqual(0.75f, MagicFormulas.GetMagicAbsorbingMod(0.25, 319), Tolerance);
        Assert.AreEqual(0.75f, MagicFormulas.GetMagicAbsorbingMod(0.25, 400), Tolerance);
        Assert.AreEqual(1.0f, MagicFormulas.GetMagicAbsorbingMod(0.25, 69), Tolerance);
        Assert.AreEqual(1.0f, MagicFormulas.GetMagicAbsorbingMod(0.25, 10), Tolerance);

        // a 10% item at 194 base: halfway
        Assert.AreEqual(0.95f, MagicFormulas.GetMagicAbsorbingMod(0.10, 194), Tolerance);
    }

    // ---- Projectile spread

    [TestMethod]
    public void SpreadAnglePerStep_SplitsTheSpreadBetweenProjectiles()
    {
        Assert.AreEqual(0.0f, MagicFormulas.GetSpreadAnglePerStep(0.0f, 5));
        Assert.AreEqual(0.0f, MagicFormulas.GetSpreadAnglePerStep(90.0f, 1));
        Assert.AreEqual(22.5f, MagicFormulas.GetSpreadAnglePerStep(90.0f, 4), Tolerance);

        // an odd count keeps one projectile in the middle
        Assert.AreEqual(22.5f, MagicFormulas.GetSpreadAnglePerStep(90.0f, 5), Tolerance);
    }

    // ---- Overload backlash

    [TestMethod]
    public void OverloadBacklash_ChanceAndDamageScaleWithCharge()
    {
        Assert.AreEqual(0.5f, MagicFormulas.GetOverloadBacklashChance(1.0f), Tolerance);
        Assert.AreEqual(0.0f, MagicFormulas.GetOverloadBacklashChance(0.0f), Tolerance);

        Assert.AreEqual(10, MagicFormulas.GetOverloadBacklashDamage(1.0f, 100));
        Assert.AreEqual(0, MagicFormulas.GetOverloadBacklashDamage(0.0f, 100));
    }

    [TestMethod]
    public void OverloadBacklashDamage_RoundsHalfToEven()
    {
        Assert.AreEqual(2, MagicFormulas.GetOverloadBacklashDamage(0.5f, 50), "2.5 rounds to 2");
        Assert.AreEqual(4, MagicFormulas.GetOverloadBacklashDamage(0.5f, 70), "3.5 rounds to 4");
    }

    // ---- Mana Conversion

    [TestMethod]
    public void ManaConversionSavings_UpToHalfTheCost()
    {
        Assert.AreEqual(50u, MagicFormulas.GetManaConversionSavings(100, 1.0, out var fullFraction));
        Assert.AreEqual(0.5, fullFraction, Tolerance);

        Assert.AreEqual(25u, MagicFormulas.GetManaConversionSavings(100, 0.5, out _));
        Assert.AreEqual(0u, MagicFormulas.GetManaConversionSavings(100, 0.0, out _));
    }

    [TestMethod]
    public void SpecManaConversionRefund_IsHalfTheSavings_RoundingHalfToEven()
    {
        Assert.AreEqual(5, MagicFormulas.GetSpecManaConversionRefund(10));
        Assert.AreEqual(2, MagicFormulas.GetSpecManaConversionRefund(5), "2.5 rounds to 2");
        Assert.AreEqual(4, MagicFormulas.GetSpecManaConversionRefund(7), "3.5 rounds to 4");
    }

    // ---- Monster spellbooks

    [TestMethod]
    public void SpellbookCastChance_Base2IsAChance_OtherwiseAPercent()
    {
        Assert.AreEqual(0.05f, MagicFormulas.GetSpellbookCastChance(2.05f), Tolerance);
        Assert.AreEqual(0.015f, MagicFormulas.GetSpellbookCastChance(1.5f), Tolerance);
    }

    [TestMethod]
    public void SpellbookHealthCastChance_GrowsAsHealthIsLost()
    {
        Assert.AreEqual(0.0f, MagicFormulas.GetSpellbookHealthCastChance(1000, 1000), Tolerance);
        Assert.AreEqual(0.165f, MagicFormulas.GetSpellbookHealthCastChance(1000, 500), Tolerance);
        Assert.AreEqual(0.33f, MagicFormulas.GetSpellbookHealthCastChance(1000, 0), Tolerance);
    }

    // ---- Advanced spells

    [TestMethod]
    public void AdvancedSpells_ByCategoryOrSpell()
    {
        Assert.IsTrue(AdvancedSpells.IsAdvanced(SpellCategory.FireBlast, (uint)SpellId.StrengthOther1));
        Assert.IsTrue(AdvancedSpells.IsAdvanced(SpellCategory.StrengthRaising, (uint)SpellId.HealthBolt1));
        Assert.IsFalse(AdvancedSpells.IsAdvanced(SpellCategory.StrengthRaising, (uint)SpellId.StrengthOther1));
    }

    private static T SetAllFields<T>(T value, float fieldValue)
        where T : struct
    {
        object boxed = value;

        foreach (var field in typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            field.SetValue(boxed, fieldValue);
        }

        return (T)boxed;
    }
}
