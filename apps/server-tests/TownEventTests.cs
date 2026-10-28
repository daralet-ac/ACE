using System;
using System.Collections.Generic;
using System.Linq;
using ACE.Entity.Enum;
using ACE.Server.Entity;
using ACE.Server.Factories.Tables;
using ACE.Server.TownEvents;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ACE.Server.Tests;

[TestClass]
public class TownEventTests
{
    [TestMethod]
    public void MemberSharePercent_KeepsTheRetailTableUpToNine()
    {
        Assert.AreEqual(1.0, Fellowship.GetMemberSharePercent(1));
        Assert.AreEqual(0.75, Fellowship.GetMemberSharePercent(2));
        Assert.AreEqual(0.5, Fellowship.GetMemberSharePercent(5));
        Assert.AreEqual(0.3, Fellowship.GetMemberSharePercent(9));
    }

    [TestMethod]
    public void MemberSharePercent_SplitsThreeHundredPercentPastTen()
    {
        // 10 fellows get 30% each, 300% between them, and a larger Town Defense fellowship splits the same 300%
        Assert.AreEqual(0.3, Fellowship.GetMemberSharePercent(10));
        Assert.AreEqual(0.2, Fellowship.GetMemberSharePercent(15), 1e-9);
        Assert.AreEqual(0.1, Fellowship.GetMemberSharePercent(30), 1e-9);

        for (var count = 10; count <= 40; count++)
        {
            Assert.AreEqual(3.0, Fellowship.GetMemberSharePercent(count) * count, 1e-9, $"{count} fellows");
        }
    }

    [TestMethod]
    public void TriggerChance_ClimbsFromTenPercentAtTheThresholdToFiftyAtDouble()
    {
        Assert.AreEqual(0, TownEventManager.GetTriggerChance(199, 200));
        Assert.AreEqual(0.10, TownEventManager.GetTriggerChance(200, 200), 1e-9);
        Assert.AreEqual(0.20, TownEventManager.GetTriggerChance(250, 200), 1e-9);
        Assert.AreEqual(0.30, TownEventManager.GetTriggerChance(300, 200), 1e-9);
        Assert.AreEqual(0.50, TownEventManager.GetTriggerChance(400, 200), 1e-9);
        Assert.AreEqual(0.50, TownEventManager.GetTriggerChance(1000, 200), 1e-9);
    }

    [TestMethod]
    public void SupplyThreshold_StartsAt500ForTierZeroThen1000TimesTierSquaredAndGrowsATenthEachLevel()
    {
        Assert.AreEqual(0, TownSupply.GetThreshold(0, 0));
        Assert.AreEqual(500, TownSupply.GetThreshold(1, 0), 1e-6);
        Assert.AreEqual(1050, TownSupply.GetThreshold(2, 0), 1e-6);
        Assert.AreEqual(1650, TownSupply.GetThreshold(3, 0), 1e-6);

        Assert.AreEqual(1000, TownSupply.GetThreshold(1, 1), 1e-6);
        Assert.AreEqual(4000, TownSupply.GetThreshold(1, 2), 1e-6);
        Assert.AreEqual(25000, TownSupply.GetThreshold(1, 5), 1e-6);

        // each level costs a tenth more than the one before, rounded to the nearest 50
        for (var tier = 0; tier <= 5; tier++)
        {
            for (var level = 1; level <= TownSupply.MaxLevel; level++)
            {
                var exact = TownSupply.GetLevelOneCost(tier) * (Math.Pow(TownSupply.LevelGrowth, level) - 1) / (TownSupply.LevelGrowth - 1);
                var cost = TownSupply.GetThreshold(level, tier);

                Assert.AreEqual(0, cost % TownSupply.CostRounding, $"tier {tier} level {level}");
                Assert.IsTrue(Math.Abs(cost - exact) <= TownSupply.CostRounding / 2, $"tier {tier} level {level}: {cost} vs {exact}");
            }
        }
    }

    [TestMethod]
    public void SupplyLevel_IsTheHighestThresholdReachedAndStopsAtTen()
    {
        Assert.AreEqual(0, TownSupply.GetLevel(0, 1));
        Assert.AreEqual(0, TownSupply.GetLevel(999, 1));
        Assert.AreEqual(1, TownSupply.GetLevel(1000, 1));
        Assert.AreEqual(2, TownSupply.GetLevel(3299, 1));
        Assert.AreEqual(3, TownSupply.GetLevel(3300, 1));
        Assert.AreEqual(TownSupply.MaxLevel, TownSupply.GetLevel(1e12, 1));
    }

    [TestMethod]
    public void SupplyLevel_LowersTheUnrestThresholdUntilLevelTenHalvesIt()
    {
        Assert.AreEqual(1.0, TownSupply.GetThresholdMultiplier(0), 1e-9);
        Assert.AreEqual(0.95, TownSupply.GetThresholdMultiplier(1), 1e-9);
        Assert.AreEqual(0.5, TownSupply.GetThresholdMultiplier(10), 1e-9);
    }

    [TestMethod]
    public void SupplyHourlyLoss_IsAShareOfTheLevelsThreshold()
    {
        Assert.AreEqual(0.02 * 1000, TownSupply.GetHourlyLoss(1, 1), 1e-6);
        Assert.AreEqual(0.03 * 2100, TownSupply.GetHourlyLoss(2, 1), 1e-6);
        Assert.AreEqual(0.40 * TownSupply.GetThreshold(10, 1), TownSupply.GetHourlyLoss(10, 1), 1e-6);

        // below level 1 drains at level 1's rate
        Assert.AreEqual(TownSupply.GetHourlyLoss(1, 1), TownSupply.GetHourlyLoss(0, 1), 1e-6);
    }

    [TestMethod]
    public void GuardArmor_IsChainmailAtZeroThenBronzeSilverGold()
    {
        Assert.IsNull(TownGuardUpgrades.GetArmorColor(0));
        Assert.AreEqual(PaletteTemplate.Copper, TownGuardUpgrades.GetArmorColor(1)?.Palette);
        Assert.AreEqual(PaletteTemplate.Copper, TownGuardUpgrades.GetArmorColor(2)?.Palette);
        Assert.AreEqual(PaletteTemplate.Silver, TownGuardUpgrades.GetArmorColor(3)?.Palette);
        Assert.AreEqual(0.943, TownGuardUpgrades.GetArmorColor(4)?.Shade);
        Assert.AreEqual(PaletteTemplate.Gold, TownGuardUpgrades.GetArmorColor(5)?.Palette);
        Assert.AreEqual(PaletteTemplate.Gold, TownGuardUpgrades.GetArmorColor(10)?.Palette);
    }

    [TestMethod]
    public void GuardWeapon_IsBlackThenBronzeSilverGoldALevelBehindTheArmor()
    {
        Assert.AreEqual(PaletteTemplate.Black, TownGuardUpgrades.GetWeaponPalette(0));
        Assert.AreEqual(PaletteTemplate.Black, TownGuardUpgrades.GetWeaponPalette(1));
        Assert.AreEqual(PaletteTemplate.Brown, TownGuardUpgrades.GetWeaponPalette(2));
        Assert.AreEqual(PaletteTemplate.Silver, TownGuardUpgrades.GetWeaponPalette(4));
        Assert.AreEqual(PaletteTemplate.Gold, TownGuardUpgrades.GetWeaponPalette(6));
        Assert.AreEqual(PaletteTemplate.Gold, TownGuardUpgrades.GetWeaponPalette(10));
    }

    [TestMethod]
    public void GuardWeaponSpells_StackFromSevenAndFollowTheTownsTier()
    {
        Assert.AreEqual(0, TownGuardUpgrades.GetWeaponSpells(6, 0).Count);

        CollectionAssert.AreEqual(new[] { SpellId.DefenderSelf1 }, TownGuardUpgrades.GetWeaponSpells(7, 0));
        CollectionAssert.AreEqual(new[] { SpellId.DefenderSelf1, SpellId.HeartSeekerSelf1 }, TownGuardUpgrades.GetWeaponSpells(8, 0));

        // level 10's Biting Strike is the weapon's critical chance, not a spell
        CollectionAssert.AreEqual(
            new[] { SpellId.DefenderSelf7, SpellId.HeartSeekerSelf7, SpellId.BloodDrinkerSelf7 },
            TownGuardUpgrades.GetWeaponSpells(10, 6)
        );

        Assert.AreEqual(1, TownGuardUpgrades.GetSpellLevel(0));
        Assert.AreEqual(4, TownGuardUpgrades.GetSpellLevel(3));
        Assert.AreEqual(8, TownGuardUpgrades.GetSpellLevel(9));
    }

    [TestMethod]
    public void LeaderArchetype_ScalesWithSupplyLevel()
    {
        Assert.AreEqual(TownArchetypeMods.None, TownArchetypeMods.ForLeader(0));

        var top = TownArchetypeMods.ForLeader(10);
        Assert.AreEqual(3.0, top.Toughness, 1e-9);
        Assert.AreEqual(1.2, top.Physicality, 1e-9);
        Assert.AreEqual(1.2, top.Dexterity, 1e-9);
        Assert.AreEqual(1.2, top.Magic, 1e-9);
        Assert.AreEqual(1.2, top.Intelligence, 1e-9);
        Assert.AreEqual(2.0, top.Lethality, 1e-9);

        Assert.AreEqual(2.0, TownArchetypeMods.ForLeader(5).Toughness, 1e-9);

        // past the top level it stays at level 10's
        Assert.AreEqual(top, TownArchetypeMods.ForLeader(15));
    }

    [TestMethod]
    public void EliteAndRaiderArchetype_GetAShareOfTheLeadersScaling()
    {
        // Elites get 75% of the Warlord's per-level gains, Raiders 50%
        Assert.AreEqual(1 + 0.75 * 2.0, TownArchetypeMods.ForElite(10).Toughness, 1e-9);
        Assert.AreEqual(1 + 0.75 * 1.0, TownArchetypeMods.ForElite(10).Lethality, 1e-9);
        Assert.AreEqual(2.0, TownArchetypeMods.ForRaider(10).Toughness, 1e-9);
        Assert.AreEqual(1.5, TownArchetypeMods.ForRaider(10).Lethality, 1e-9);
        Assert.AreEqual(1.1, TownArchetypeMods.ForRaider(10).Physicality, 1e-9);

        Assert.AreEqual(TownArchetypeMods.None, TownArchetypeMods.ForElite(0));
        Assert.AreEqual(TownArchetypeMods.None, TownArchetypeMods.ForRaider(0));
    }

    [TestMethod]
    public void GuardArmorLevel_IsCovenantBaseInChainmailUpToDoubleInGold()
    {
        // tier 0: 50 in chainmail to 100 in gold
        Assert.AreEqual(50, TownGuardUpgrades.GetArmorLevel(0, 0));
        Assert.AreEqual(67, TownGuardUpgrades.GetArmorLevel(1, 0));
        Assert.AreEqual(83, TownGuardUpgrades.GetArmorLevel(3, 0));
        Assert.AreEqual(100, TownGuardUpgrades.GetArmorLevel(5, 0));

        // then the tier's Covenant base, doubling each tier
        Assert.AreEqual(100, TownGuardUpgrades.GetChainmailArmorLevel(1));
        Assert.AreEqual(200, TownGuardUpgrades.GetChainmailArmorLevel(2));
        Assert.AreEqual(1600, TownGuardUpgrades.GetChainmailArmorLevel(5));
        Assert.AreEqual(3200, TownGuardUpgrades.GetArmorLevel(10, 5));
    }

    [TestMethod]
    public void GuardWeaponDamage_RisesWithItsColour()
    {
        Assert.AreEqual(1.0, TownGuardUpgrades.GetWeaponDamageMod(1));
        Assert.AreEqual(1.15, TownGuardUpgrades.GetWeaponDamageMod(2));
        Assert.AreEqual(1.3, TownGuardUpgrades.GetWeaponDamageMod(5));
        Assert.AreEqual(1.5, TownGuardUpgrades.GetWeaponDamageMod(6));
        Assert.AreEqual(1.5, TownGuardUpgrades.GetWeaponDamageMod(10));
    }

    [TestMethod]
    public void LogRanks_PutTheTopFirstShareTiesAndSkipZeros()
    {
        var ann = new TownDefenseScore { PlayerGuid = 1, Damage = 500 };
        var bob = new TownDefenseScore { PlayerGuid = 2, Damage = 300 };
        var cat = new TownDefenseScore { PlayerGuid = 3, Damage = 300 };
        var dan = new TownDefenseScore { PlayerGuid = 4, Damage = 100 };
        var eve = new TownDefenseScore { PlayerGuid = 5, Healing = 50 };

        var ranks = TownEventRecorder.Ranks(new List<TownDefenseScore> { dan, cat, ann, eve, bob }, s => s.Damage);

        Assert.AreEqual((ushort)1, ranks[ann]);
        Assert.AreEqual((ushort)2, ranks[bob]);
        Assert.AreEqual((ushort)2, ranks[cat]);
        Assert.AreEqual((ushort)4, ranks[dan]);
        Assert.IsFalse(ranks.ContainsKey(eve));
    }

    [TestMethod]
    public void InboundOrder_RunsFromTheFarEndToTheCentre()
    {
        // a road walked out and back: 0 is in town, 3 is at the edge, then back
        var order = TownGeography.InboundOrder(new[] { 10f, 50f, 90f, 130f, 90f, 50f });

        Assert.AreEqual(3, order[0]);
        Assert.AreEqual(0, order[^1]);
        Assert.AreEqual(4, order.Count);
    }

    [TestMethod]
    public void InboundOrder_GoesTheShorterWayRoundALoop()
    {
        // far end at 1, centre at 5: forward is 4 legs, backward round the loop is 2
        var order = TownGeography.InboundOrder(new[] { 60f, 140f, 110f, 90f, 70f, 5f });

        CollectionAssert.AreEqual(new[] { 1, 0, 5 }, order);
    }

    [TestMethod]
    public void InboundOrder_WrapsForward()
    {
        // far end at 4, centre at 1: forward round the end of the list is 3 legs, backward is 3, so forward
        var order = TownGeography.InboundOrder(new[] { 40f, 5f, 60f, 100f, 150f, 80f });

        CollectionAssert.AreEqual(new[] { 4, 5, 0, 1 }, order);
    }

    [TestMethod]
    public void SupportKit_HealsAndBuffsFirstThenHarmsAndAllThreeMartyrs()
    {
        var kit = TownCasterKits.GetKit(TownCasterRole.Support, 8).Select(k => k.Spell).ToList();

        CollectionAssert.AreEqual(
            new[]
            {
                (int)SpellId.HealOther8,
                (int)SpellId.BloodDrinkerOther8,
                (int)SpellId.HeartSeekerOther8,
                (int)SpellId.HarmOther8,
                2766, // Martyr's Hecatomb VII: Martyr's spells stop at VII
                2773, // Martyr's Tenacity VII
                2780 // Martyr's Blight VII
            },
            kit
        );
    }

    [TestMethod]
    public void SupportKit_AtLevelOne_UsesLevelOneSpells()
    {
        var kit = TownCasterKits.GetKit(TownCasterRole.Support, 1).Select(k => k.Spell).ToList();

        CollectionAssert.AreEqual(
            new[] { (int)SpellId.HealOther1, (int)SpellId.BloodDrinkerOther1, (int)SpellId.HeartSeekerOther1, (int)SpellId.HarmOther1, 2760, 2767, 2774 },
            kit
        );
    }

    [TestMethod]
    public void WarKit_IsJustAWarBoltOfTheLevel_AndEveryChanceIsAboveTheBaseOfTwo()
    {
        var kit = TownCasterKits.GetKit(TownCasterRole.War, 5);

        var bolts = new[]
        {
            SpellLevelProgression.FlameBolt, SpellLevelProgression.FrostBolt, SpellLevelProgression.AcidStream,
            SpellLevelProgression.LightningBolt, SpellLevelProgression.ForceBolt, SpellLevelProgression.ShockWave,
            SpellLevelProgression.WhirlingBlade
        };

        Assert.AreEqual(1, kit.Count);
        Assert.IsTrue(bolts.Any(b => (int)b[4] == kit[0].Spell), $"spell {kit[0].Spell} isn't a level 5 bolt");

        foreach (var (_, probability) in kit.Concat(TownCasterKits.GetKit(TownCasterRole.Support, 5)))
        {
            Assert.IsTrue(probability > 2.0f && probability < 3.0f, $"probability {probability}");
        }

        Assert.AreEqual(0, TownCasterKits.GetKit(TownCasterRole.None, 5).Count);
    }

    private static TownPoolCreature Creature(uint wcid, int level) => new(wcid, $"c{wcid}", CreatureType.Lugian, level, 1, 1, false);

    [TestMethod]
    public void RaidThemes_Nearest_TakesTheInBandOnesAndThoseWithinTheSlack()
    {
        var ladder = new[] { Creature(1, 5), Creature(2, 15), Creature(3, 18), Creature(4, 27), Creature(5, 47), Creature(6, 21) };

        // tier 1 (10-19): 15 and 18 inside, and 21 is 2 out, within 3; 5 and 27 are 5 and 8 out
        CollectionAssert.AreEquivalent(new uint[] { 2, 3, 6 }, TownRaidThemes.Nearest(ladder, 10, 19).Select(c => c.Wcid).ToList());

        // tier 4 (40-49): 47 inside; 27 is 13 levels out
        CollectionAssert.AreEquivalent(new uint[] { 5 }, TownRaidThemes.Nearest(ladder, 40, 49).Select(c => c.Wcid).ToList());

        // tier 3 (30-39): nothing inside; 27 is nearest (3 out), and 47 (8 out) and 21 (9 out) are within 3 + 6
        CollectionAssert.AreEquivalent(new uint[] { 4, 5, 6 }, TownRaidThemes.Nearest(ladder, 30, 39).Select(c => c.Wcid).ToList());

        Assert.AreEqual(0, TownRaidThemes.Nearest(Array.Empty<TownPoolCreature>(), 1, 9).Count);
    }

    [TestMethod]
    public void RaidThemes_ScaleLevel_KeepsInBandLevelsAndMovesTheRestIn()
    {
        Assert.AreEqual(15, TownRaidThemes.ScaleLevel(15, 10, 19, false, (a, b) => throw new InvalidOperationException()));
        Assert.AreEqual(42, TownRaidThemes.ScaleLevel(18, 40, 49, false, (a, b) => 42));
        Assert.AreEqual(49, TownRaidThemes.ScaleLevel(18, 40, 49, true, (a, b) => 42));
        Assert.AreEqual(9, TownRaidThemes.ScaleLevel(47, 1, 9, true, (a, b) => 3));
    }

    [TestMethod]
    public void RaidThemes_EveryTownIsATownAndEveryThemeHasRaiders()
    {
        var towns = Town.TownNames.ToHashSet();

        foreach (var (town, names) in TownRaidThemes.Towns)
        {
            Assert.IsTrue(towns.Contains(town), $"{town} isn't in Town.cs");
            Assert.IsTrue(names.Length > 0, $"{town} has no themes");
            Assert.AreEqual(names.Length, TownRaidThemes.ForTown(town).Count, town);
        }

        foreach (var (name, theme) in TownRaidThemes.Themes)
        {
            Assert.AreEqual(name, theme.Name);
            Assert.IsTrue(theme.Raiders.Length > 0, $"{name} has no raiders");
            Assert.AreEqual(theme.Raiders.Length, theme.Raiders.Distinct().Count(), $"{name} lists a raider twice");
        }

        // beasts never raid alone, and towns without a theme fall back to raider families
        Assert.IsFalse(TownRaidThemes.RaiderTypes.Contains(CreatureType.Mattekar));
        Assert.IsTrue(TownRaidThemes.RaiderTypes.Contains(CreatureType.Virindi));
        Assert.AreEqual(0, TownRaidThemes.ForTown("Nowhere").Count);
    }
}
