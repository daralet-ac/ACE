using System;
using System.Reflection;
using ACE.Entity.Enum;
using ACE.Server.Entity;
using ACE.Server.WorldObjects;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ACE.Server.Tests;

[TestClass]
public class DamageFormulaTests
{
    private const double Tolerance = 1e-6;

    // ---- DamageModifiers / MitigationModifiers

    [TestMethod]
    public void DamageModifiers_AllAtOne_LeaveBaseDamageUnchanged()
    {
        var modifiers = SetAllFields(new DamageModifiers(), 1.0f);

        Assert.AreEqual(150.0f, modifiers.Apply(150.0f));
    }

    [TestMethod]
    public void DamageModifiers_EveryModifierIsApplied()
    {
        foreach (var field in typeof(DamageModifiers).GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            object modifiers = SetAllFields(new DamageModifiers(), 1.0f);
            field.SetValue(modifiers, 2.0f);

            Assert.AreEqual(20.0f, ((DamageModifiers)modifiers).Apply(10.0f), $"{field.Name} isn't applied");
        }
    }

    [TestMethod]
    public void DamageModifiers_NotFilledIn_GiveZeroDamage()
    {
        Assert.AreEqual(0.0f, new DamageModifiers().Apply(150.0f));
    }

    [TestMethod]
    public void MitigationModifiers_AllAtOne_HaveAProductOfOne()
    {
        Assert.AreEqual(1.0f, SetAllFields(new MitigationModifiers(), 1.0f).Product());
    }

    [TestMethod]
    public void MitigationModifiers_EveryModifierIsInTheProduct()
    {
        foreach (var field in typeof(MitigationModifiers).GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            object modifiers = SetAllFields(new MitigationModifiers(), 1.0f);
            field.SetValue(modifiers, 0.5f);

            Assert.AreEqual(0.5f, ((MitigationModifiers)modifiers).Product(), $"{field.Name} isn't in the product");
        }
    }

    // ---- Evade

    [TestMethod]
    public void EvadeChance_IsHalfAtEqualSkills()
    {
        Assert.AreEqual(0.5f, DamageFormulas.GetEvadeChance(300, 300, smokescreen: false), Tolerance);
    }

    [TestMethod]
    public void EvadeChance_Smokescreen_TurnsTenPercentOfTheChanceToBeHitIntoEvade()
    {
        Assert.AreEqual(0.55f, DamageFormulas.GetEvadeChance(300, 300, smokescreen: true), Tolerance);
    }

    [TestMethod]
    public void EvadeChance_StaysBetweenZeroAndOne()
    {
        Assert.AreEqual(1.0f, DamageFormulas.GetEvadeChance(5000, 0, smokescreen: true), Tolerance);
        Assert.AreEqual(0.0f, DamageFormulas.GetEvadeChance(0, 5000, smokescreen: false), Tolerance);
    }

    [TestMethod]
    public void EvasionType_SplitsTheRollIntoThirds()
    {
        Assert.AreEqual(PartialEvasion.All, DamageFormulas.GetEvasionType(0.0));
        Assert.AreEqual(PartialEvasion.All, DamageFormulas.GetEvasionType(0.333));
        Assert.AreEqual(PartialEvasion.Some, DamageFormulas.GetEvasionType(0.334));
        Assert.AreEqual(PartialEvasion.Some, DamageFormulas.GetEvasionType(0.666));
        Assert.AreEqual(PartialEvasion.None, DamageFormulas.GetEvasionType(0.667));
        Assert.AreEqual(PartialEvasion.None, DamageFormulas.GetEvasionType(0.999));
    }

    // ---- Block / parry

    [TestMethod]
    public void BlockChance_IsBetweenFiveAndTenPercentByShieldLevel()
    {
        // shield level far below the attack skill: 5%, equal: 7.5%, far above: 10%
        Assert.AreEqual(0.05, DamageFormulas.GetBlockChance(0, 1000, 0, 0, 0, 1.0f), 0.001);
        Assert.AreEqual(0.075, DamageFormulas.GetBlockChance(300, 300, 0, 0, 0, 1.0f), Tolerance);
        Assert.AreEqual(0.1, DamageFormulas.GetBlockChance(1000, 0, 0, 0, 0, 1.0f), 0.001);
    }

    [TestMethod]
    public void BlockChance_BonusesAddTogetherThenMultiply()
    {
        // 7.5% * (1 + 0.5 spec + 0.1 jewel + 1.0 riposte) * 1.25 phalanx
        var blockChance = DamageFormulas.GetBlockChance(300, 300, 0.5f, 0.1f, 1.0f, 1.25f);

        Assert.AreEqual(0.075 * 2.6 * 1.25, blockChance, Tolerance);
    }

    [TestMethod]
    public void ParryChance_IsUpToTenPercentBySkill()
    {
        // the parry skill counts 1.5x: 200 parry skill is even with 300 attack skill
        Assert.AreEqual(0.05, DamageFormulas.GetParryChance(200, 300, 0, 0, 1.0f), Tolerance);
        Assert.AreEqual(0.1, DamageFormulas.GetParryChance(1000, 0, 0, 0, 1.0f), 0.001);
    }

    [TestMethod]
    public void ParryChance_BonusesAddTogetherThenMultiply()
    {
        // 5% * (1 + 0.5 spec + 1.0 riposte) * 1.25 phalanx
        var parryChance = DamageFormulas.GetParryChance(200, 300, 0.5f, 1.0f, 1.25f);

        Assert.AreEqual(0.05 * 2.5 * 1.25, parryChance, Tolerance);
    }

    // ---- Damage

    [TestMethod]
    public void CombatSkillDamageBonus_IsUpToFiftyPercent()
    {
        // the combat skill counts 1.5x: 200 skill is even with 300 physical defense
        Assert.AreEqual(1.25f, DamageFormulas.GetCombatSkillDamageBonus(200, 300), Tolerance);
        Assert.AreEqual(1.5f, DamageFormulas.GetCombatSkillDamageBonus(1000, 0), 0.001);
        Assert.AreEqual(1.0f, DamageFormulas.GetCombatSkillDamageBonus(0, 1000), 0.001);
    }

    // ---- Mitigation

    [TestMethod]
    public void SpecDefenseMod_GoesFromTenToTwentyPercentReductionAtFiveHundredSkill()
    {
        Assert.AreEqual(0.9f, DamageFormulas.GetSpecDefenseMod(0), Tolerance);
        Assert.AreEqual(0.85f, DamageFormulas.GetSpecDefenseMod(250), Tolerance);
        Assert.AreEqual(0.8f, DamageFormulas.GetSpecDefenseMod(500), Tolerance);
        Assert.AreEqual(0.8f, DamageFormulas.GetSpecDefenseMod(1000), Tolerance);
    }

    [TestMethod]
    public void SwarmedMod_ReducesDamageTenPercentPerEnemyBeyondTheFirst()
    {
        Assert.AreEqual(1.0f, DamageFormulas.GetSwarmedMod(0));
        Assert.AreEqual(1.0f, DamageFormulas.GetSwarmedMod(1));
        Assert.AreEqual(0.9f, DamageFormulas.GetSwarmedMod(2), Tolerance);
        Assert.AreEqual(0.81f, DamageFormulas.GetSwarmedMod(3), Tolerance);
    }

    [TestMethod]
    public void ImbuedArmorMod_ReducesDamageOnePercentPerPieceDownToHalf()
    {
        Assert.AreEqual(1.0f, DamageFormulas.GetImbuedArmorMod(0));
        Assert.AreEqual(1.0f, DamageFormulas.GetImbuedArmorMod(-1));
        Assert.AreEqual(0.95f, DamageFormulas.GetImbuedArmorMod(5), Tolerance);
        Assert.AreEqual(0.5f, DamageFormulas.GetImbuedArmorMod(100), Tolerance);
    }

    [TestMethod]
    public void DamageResistRatings_CriticalAndPkRatingsBothApplyOnAPkCriticalHit()
    {
        var baseMod = Creature.GetNegativeRatingMod(10);
        var ratingOf20 = Creature.GetNegativeRatingMod(20);

        Assert.AreEqual(baseMod, DamageFormulas.CombineDamageResistRatings(baseMod, null, null), Tolerance);
        Assert.AreEqual(
            Creature.GetNegativeRatingMod(30),
            DamageFormulas.CombineDamageResistRatings(baseMod, ratingOf20, null),
            Tolerance
        );
        Assert.AreEqual(
            Creature.GetNegativeRatingMod(50),
            DamageFormulas.CombineDamageResistRatings(baseMod, ratingOf20, ratingOf20),
            Tolerance
        );
    }

    // ---- Weapon Master

    [TestMethod]
    public void WeaponMasterTier_IsTheLootTierMinusOneBetweenOneAndSeven()
    {
        Assert.AreEqual(1, DamageFormulas.GetWeaponMasterTier(null));
        Assert.AreEqual(1, DamageFormulas.GetWeaponMasterTier(1));
        Assert.AreEqual(1, DamageFormulas.GetWeaponMasterTier(2));
        Assert.AreEqual(4, DamageFormulas.GetWeaponMasterTier(5));
        Assert.AreEqual(7, DamageFormulas.GetWeaponMasterTier(8));
        Assert.AreEqual(7, DamageFormulas.GetWeaponMasterTier(12));
    }

    [TestMethod]
    public void WeaponMasterTierMod_CoversEveryTier()
    {
        float[] expected = [1.0f, 3.0f, 4.0f, 5.0f, 6.0f, 8.0f, 10.0f];

        for (var tier = 1; tier <= 7; tier++)
        {
            Assert.AreEqual(expected[tier - 1], DamageFormulas.GetWeaponMasterTierMod(tier));
        }

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => DamageFormulas.GetWeaponMasterTierMod(0));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => DamageFormulas.GetWeaponMasterTierMod(8));
    }

    [TestMethod]
    public void BleedWeaponDamageRange_OnlyForBleedingWeapons()
    {
        Assert.AreEqual((9, 132), DamageFormulas.GetBleedWeaponDamageRange(Skill.Axe));
        Assert.AreEqual((4, 95), DamageFormulas.GetBleedWeaponDamageRange(Skill.Dagger));
        Assert.AreEqual((10, 296), DamageFormulas.GetBleedWeaponDamageRange(Skill.ThrownWeapon));
        Assert.AreEqual((5, 107), DamageFormulas.GetBleedWeaponDamageRange(Skill.TwoHandedCombat));
        Assert.IsNull(DamageFormulas.GetBleedWeaponDamageRange(Skill.Mace));
    }

    [TestMethod]
    public void BleedDamagePercentile_ScalesAcrossTheRangeAndClamps()
    {
        Assert.AreEqual(0.0f, DamageFormulas.GetBleedDamagePercentile(4, (4, 95)));
        Assert.AreEqual(1.0f, DamageFormulas.GetBleedDamagePercentile(95, (4, 95)));
        Assert.AreEqual(0.5f, DamageFormulas.GetBleedDamagePercentile(50, (0, 100)), Tolerance);
        Assert.AreEqual(0.0f, DamageFormulas.GetBleedDamagePercentile(1, (4, 95)));
        Assert.AreEqual(1.0f, DamageFormulas.GetBleedDamagePercentile(200, (4, 95)));
    }

    // ---- Perception / Deception

    [TestMethod]
    public void SkillRatioChance_GrowsWithTheRatioUpToTheMax()
    {
        Assert.AreEqual(0.0f, SkillCheck.GetSkillRatioChance(0, 300));
        Assert.AreEqual(0.25f, SkillCheck.GetSkillRatioChance(150, 300), Tolerance);
        Assert.AreEqual(0.5f, SkillCheck.GetSkillRatioChance(300, 300), Tolerance);
        Assert.AreEqual(0.5f, SkillCheck.GetSkillRatioChance(600, 300), Tolerance);
        Assert.AreEqual(0.5f, SkillCheck.GetSkillRatioChance(100, 0), Tolerance);
        Assert.AreEqual(0.2f, SkillCheck.GetSkillRatioChance(600, 300, maxChance: 0.2f), Tolerance);
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
