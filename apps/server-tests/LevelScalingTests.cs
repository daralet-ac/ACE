using System;
using ACE.Server.Entity;
using ACE.Server.WorldObjects;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ACE.Server.Tests;

[TestClass]
public class LevelScalingTests
{
    // the highest level a player can reach
    private const int MaxLevel = 275;

    [TestMethod]
    public void AtLevel_StatsNeverGetWorseAsTheLevelGoesUp()
    {
        (string Name, Func<int, double> Stat)[] stats =
        [
            ("player health", level => LevelScaling.GetPlayerHealthAtLevel(level)),
            ("player armor/ward", level => LevelScaling.GetPlayerArmorWardAtLevel(level)),
            ("player attribute", level => LevelScaling.GetPlayerAttributeAtLevel(level)),
            ("player attack", level => LevelScaling.GetPlayerAttackSkillAtLevel(level)),
            ("player defense", level => LevelScaling.GetPlayerDefenseSkillAtLevel(level)),
            ("player boost", level => LevelScaling.GetPlayerBoostAtLevel(level)),
            ("monster health", level => LevelScaling.GetMonsterHealthAtLevel(level)),
            ("monster armor/ward", level => LevelScaling.GetMonsterArmorWardAtLevel(level)),
            // a lower resistance mod resists more
            ("player resistance", level => -LevelScaling.GetPlayerResistanceAtLevel(level)),
        ];

        foreach (var (name, stat) in stats)
        {
            for (var level = LevelScaling.MinimumScaledLevel; level < MaxLevel; level++)
            {
                Assert.IsTrue(stat(level + 1) >= stat(level), $"{name} drops from level {level} to {level + 1}");
            }
        }
    }

    [TestMethod]
    public void ArmorWard_ReferenceGearIsWorthWhatReferenceGearIsAtTheMonstersLevel()
    {
        // #1251: monster damage is authored against the reference player's armor at the monster's level (TierPower),
        // so the monster's level cancels
        for (var playerLevel = LevelScaling.MinimumScaledLevel; playerLevel <= MaxLevel; playerLevel += 5)
        {
            for (var monsterLevel = LevelScaling.MinimumScaledLevel; monsterLevel <= playerLevel; monsterLevel += 5)
            {
                var ratio = LevelScaling.GetArmorWardModRatio(playerLevel, monsterLevel);

                var average = SkillFormula.CalcArmorMod(TierPower.GetReferenceArmor(playerLevel));
                var averageAtMonsterLevel = SkillFormula.CalcArmorMod(TierPower.GetReferenceArmor(monsterLevel));

                Assert.AreEqual(averageAtMonsterLevel, average * ratio, 1e-5, $"level {playerLevel} vs {monsterLevel}");

                // and ward the same way against the reference ward
                var wardRatio = LevelScaling.GetArmorWardModRatio(playerLevel, monsterLevel, ward: true);
                Assert.AreEqual(
                    SkillFormula.CalcWardMod(TierPower.GetReferenceWard(monsterLevel)),
                    SkillFormula.CalcWardMod(TierPower.GetReferenceWard(playerLevel)) * wardRatio,
                    1e-5,
                    $"ward, level {playerLevel} vs {monsterLevel}"
                );

                // and gear above the reference keeps the same edge over it
                var good = SkillFormula.CalcArmorMod(TierPower.GetReferenceArmor(playerLevel) * 1.5f);

                Assert.AreEqual(
                    good / average,
                    good * ratio / averageAtMonsterLevel,
                    1e-5,
                    $"level {playerLevel} vs {monsterLevel}"
                );
            }
        }
    }

    [TestMethod]
    public void ArmorWard_NothingChangesAtTheSameLevel()
    {
        for (var level = LevelScaling.MinimumScaledLevel; level <= MaxLevel; level++)
        {
            Assert.AreEqual(1.0f, LevelScaling.GetArmorWardModRatio(level, level), 1e-6, $"level {level}");
        }
    }

    [TestMethod]
    public void DamageTaken_AShroudedPlayerKillsInTheMonsterLevelsKillTime()
    {
        for (var playerLevel = LevelScaling.MinimumScaledLevel; playerLevel <= MaxLevel; playerLevel += 5)
        {
            for (var monsterLevel = LevelScaling.MinimumScaledLevel; monsterLevel <= playerLevel; monsterLevel += 5)
            {
                // a reference player at their own level kills a same-level monster in that level's kill time; scaled
                // to a lower monster, damage over its health gives the monster level's kill time
                var ratio = LevelScaling.GetDamageTakenRatio(playerLevel, monsterLevel, false);
                var killTime = TierPower.GetKillTime(playerLevel)
                    * TierPower.GetMonsterHealth(monsterLevel) / TierPower.GetMonsterHealth(playerLevel) / ratio;

                Assert.AreEqual(TierPower.GetKillTime(monsterLevel), killTime, 1e-3, $"level {playerLevel} vs {monsterLevel}");

                // fellowship dungeons use their own target, whatever the levels
                var fellowshipRatio = LevelScaling.GetDamageTakenRatio(playerLevel, monsterLevel, true);
                var fellowshipKillTime = TierPower.GetKillTime(playerLevel)
                    * TierPower.GetMonsterHealth(monsterLevel) / TierPower.GetMonsterHealth(playerLevel) / fellowshipRatio;

                Assert.AreEqual(TierPower.FellowshipKillTime, fellowshipKillTime, 1e-3, $"fellowship, level {playerLevel} vs {monsterLevel}");
            }
        }

        // the design's examples: Shrouded L100 vs L10 kills in the T2 time, and nothing changes at the same level
        Assert.AreEqual(1.0f, LevelScaling.GetDamageTakenRatio(50, 50, false), 1e-5);
        Assert.AreEqual(11.43f, TierPower.GetKillTime(10), 1e-3);
        Assert.AreEqual(17.14f, TierPower.FellowshipKillTime, 1e-3);
    }

    [TestMethod]
    public void Duel_TheHigherFighterAlsoLosesTheirWeaponTiersEdge()
    {
        // L100 (T8) hits L50 (T6): the health ratio, and weapons that are 4x as strong
        Assert.AreEqual(
            LevelScaling.GetDuelHealthRatio(100, 50) * 160f / 640f,
            LevelScaling.GetDuelDamageRatio(100, 50),
            1e-5
        );

        // the lower fighter only follows the health ratio, and nothing changes at the same level
        Assert.AreEqual(LevelScaling.GetDuelHealthRatio(50, 100), LevelScaling.GetDuelDamageRatio(50, 100), 1e-6);
        Assert.AreEqual(1.0f, LevelScaling.GetDuelDamageRatio(75, 75), 1e-6);
    }

    [TestMethod]
    public void Defense_KeepsThePlayersWholeAdvantageOverTheAverage()
    {
        // #1251: defense is lowered by the difference in averages, not scaled by their ratio
        const uint advantage = 75;

        for (var playerLevel = LevelScaling.MinimumScaledLevel; playerLevel <= MaxLevel; playerLevel += 5)
        {
            for (var monsterLevel = LevelScaling.MinimumScaledLevel; monsterLevel <= playerLevel; monsterLevel += 5)
            {
                var skill = (uint)LevelScaling.GetPlayerDefenseSkillAtLevel(playerLevel) + advantage;
                var scaled = LevelScaling.GetScaledDefenseSkill(skill, playerLevel, monsterLevel);

                Assert.AreEqual(
                    advantage,
                    scaled - (uint)LevelScaling.GetPlayerDefenseSkillAtLevel(monsterLevel),
                    $"level {playerLevel} vs {monsterLevel}"
                );
            }
        }
    }

    [TestMethod]
    public void Defense_NeverGoesBelowZero()
    {
        Assert.AreEqual(0u, LevelScaling.GetScaledDefenseSkill(5, MaxLevel, LevelScaling.MinimumScaledLevel));
        Assert.AreEqual(123u, LevelScaling.GetScaledDefenseSkill(123, 50, 50));
    }

    [TestMethod]
    public void Tables_PlayerAttackAndDefenseMatchTheArchetypeSystem()
    {
        // monsters' attack and defense are a flat gap above these, so a Shrouded player scaled to a monster's level
        // gets the same evade odds a native does only while the two files agree
        CollectionAssert.AreEqual(Creature.avgPlayerPhysicalMagicDefense, LevelScaling.AvgPlayerAttackSkillPerTier);
        CollectionAssert.AreEqual(Creature.avgPlayerPhysicalMagicDefense, LevelScaling.AvgPlayerDefenseSkillPerTier);
    }
}
