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
    public void ArmorWard_AverageGearIsWorthWhatAverageGearIsAtTheMonstersLevel()
    {
        // #1251: monster damage is authored against the average armor at the monster's level, so the monster's level cancels
        for (var playerLevel = LevelScaling.MinimumScaledLevel; playerLevel <= MaxLevel; playerLevel += 5)
        {
            for (var monsterLevel = LevelScaling.MinimumScaledLevel; monsterLevel <= playerLevel; monsterLevel += 5)
            {
                var ratio = LevelScaling.GetArmorWardModRatio(playerLevel, monsterLevel);

                var average = SkillFormula.CalcArmorMod(LevelScaling.GetPlayerArmorWardAtLevel(playerLevel));
                var averageAtMonsterLevel = SkillFormula.CalcArmorMod(
                    LevelScaling.GetPlayerArmorWardAtLevel(monsterLevel)
                );

                Assert.AreEqual(averageAtMonsterLevel, average * ratio, 1e-5, $"level {playerLevel} vs {monsterLevel}");

                // and gear above the average keeps the same edge over the average
                var good = SkillFormula.CalcArmorMod(LevelScaling.GetPlayerArmorWardAtLevel(playerLevel) * 1.5f);

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
