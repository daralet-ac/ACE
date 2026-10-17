using System;
using System.Linq;
using ACE.Server.Entity;
using ACE.Server.Managers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ACE.Server.Tests;

[TestClass]
public class CapstoneRunTrackerTests
{
    private static readonly DateTime Start = new(2026, 10, 16, 12, 0, 0, DateTimeKind.Utc);

    [TestMethod]
    public void EveryCompletionQuest_HasADungeonKey()
    {
        // the website's dungeon list is the completion quests without "Completed"
        CollectionAssert.AreEquivalent(
            QuestManager.CapstoneCompletionQuests.ToList(),
            CapstoneRunTracker.DungeonKeys.Values.Select(key => key + "Completed").ToList()
        );
    }

    [TestMethod]
    public void EveryDungeonKey_IsADungeonTheServerKnows()
    {
        foreach (var name in CapstoneRunTracker.DungeonKeys.Keys)
        {
            Assert.IsNotNull(Landblock.CapstoneDungeonLists(name), name);
        }
    }

    [TestMethod]
    public void SecondParts_AreDungeonsOfTheirOwnWhoseFirstPartIsTimed()
    {
        Assert.AreNotEqual(0, CapstoneRunTracker.SecondParts.Count);

        foreach (var (second, first) in CapstoneRunTracker.SecondParts)
        {
            Assert.IsNotNull(Landblock.CapstoneDungeonLists(second), second);
            Assert.IsTrue(CapstoneRunTracker.DungeonKeys.ContainsKey(first), first);
            Assert.IsFalse(CapstoneRunTracker.DungeonKeys.ContainsKey(second), second);
        }
    }

    [TestMethod]
    public void Begin_StartsAClockForAFirstPartOnly()
    {
        var state = CapstoneRunTracker.Begin("Colier Mine", Start);

        Assert.IsNotNull(state);
        Assert.AreEqual("Colier Mine", state.Dungeon);
        Assert.AreEqual(Start, state.StartedUtc);

        // the second part of a dungeon carries on the first's run, the raid has no board, and nothing is not a dungeon
        Assert.IsNull(CapstoneRunTracker.Begin("Lugian Mines2", Start));
        Assert.IsNull(CapstoneRunTracker.Begin("Beyond the Mines", Start));
        Assert.IsNull(CapstoneRunTracker.Begin("Olthoi Queen's Lair", Start));
        Assert.IsNull(CapstoneRunTracker.Begin("Nowhere", Start));
        Assert.IsNull(CapstoneRunTracker.Begin(null, Start));
    }

    [TestMethod]
    public void StartRun_SecondPartLeavesTheFirstPartsClock()
    {
        var fellowship = (Fellowship)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(Fellowship));

        CapstoneRunTracker.StartRun(fellowship, "Mines of Despair");
        var first = fellowship.CapstoneRun;
        Assert.IsNotNull(first);

        CapstoneRunTracker.StartRun(fellowship, "Beyond the Mines");
        Assert.AreSame(first, fellowship.CapstoneRun);

        // a different dungeon is a new run
        CapstoneRunTracker.StartRun(fellowship, "Mage Academy");
        Assert.AreNotSame(first, fellowship.CapstoneRun);
        Assert.AreEqual("Mage Academy", fellowship.CapstoneRun.Dungeon);
    }

    [TestMethod]
    public void EndsRun_OnlyTheBossKillOrCompletionOfTheDungeonBeingRun()
    {
        var state = new CapstoneRunState("Colier Mine", Start);

        // the boss's death stamps the first, the reward cache the second
        Assert.IsTrue(CapstoneRunTracker.EndsRun(state, "CapstoneMinesOfColierBossKilled"));
        Assert.IsTrue(CapstoneRunTracker.EndsRun(state, "MinesOfColierCompleted"));

        Assert.IsFalse(CapstoneRunTracker.EndsRun(state, "CapstoneMinesOfDespairBossKilled"));
        Assert.IsFalse(CapstoneRunTracker.EndsRun(state, "MinesOfDespairCompleted"));
        Assert.IsFalse(CapstoneRunTracker.EndsRun(state, "MinesOfColierCompleted_size:2_diff:5%"));
        Assert.IsFalse(CapstoneRunTracker.EndsRun(state, "CapstoneMinesOfColierBossKilled@2"));
        Assert.IsFalse(CapstoneRunTracker.EndsRun(state, null));
        Assert.IsFalse(CapstoneRunTracker.EndsRun(null, "CapstoneMinesOfColierBossKilled"));
    }

    [TestMethod]
    public void BossKilledQuest_IsNamedLikeTheWorldDatabasesOnes()
    {
        // e.g. the boss of Green Mire Grave stamps this on its killer's fellowship when it dies
        Assert.AreEqual("CapstoneGreenMireGraveBossKilled", CapstoneRunTracker.BossKilledQuest("GreenMireGrave"));

        // every dungeon with a leaderboard has one, and none is mistaken for another dungeon's
        var names = CapstoneRunTracker.DungeonKeys.Values.Select(CapstoneRunTracker.BossKilledQuest).ToList();
        Assert.AreEqual(names.Count, names.Distinct().Count());
    }

    [TestMethod]
    public void BuildRun_RecordsTheWholeRosterSmallestIdFirst()
    {
        var state = new CapstoneRunState("Colier Mine", Start);

        var run = CapstoneRunTracker.BuildRun(
            state,
            Start.AddMinutes(12).AddSeconds(34.4),
            7,
            [(0x50000010u, "Beta"), (0x50000002u, "Alpha"), (0x50000030u, "Gamma")]
        );

        Assert.AreEqual("MinesOfColier", run.Dungeon);
        Assert.AreEqual(3, run.FellowshipSize);
        Assert.AreEqual(7, run.DifficultyTier);
        Assert.AreEqual(754u, run.CompletionSeconds);
        Assert.AreEqual("1342177282,1342177296,1342177328", run.RosterKey);
        CollectionAssert.AreEqual(new[] { "Alpha", "Beta", "Gamma" }, run.Members.Select(m => m.CharacterName).ToArray());
    }

    [TestMethod]
    public void BuildRun_TheSameGroupGetsTheSameKeyInAnyOrder()
    {
        var state = new CapstoneRunState("Mage Academy", Start);
        var end = Start.AddMinutes(5);

        var a = CapstoneRunTracker.BuildRun(state, end, 0, [(5u, "A"), (9u, "B")]);
        var b = CapstoneRunTracker.BuildRun(state, end, 0, [(9u, "B"), (5u, "A")]);
        var other = CapstoneRunTracker.BuildRun(state, end, 0, [(5u, "A"), (10u, "C")]);

        Assert.AreEqual(a.RosterKey, b.RosterKey);
        Assert.AreNotEqual(a.RosterKey, other.RosterKey);
    }

    [TestMethod]
    public void BuildRun_ASecondOrLessIsOneSecond_AndTheTierStaysOnTheBoard()
    {
        var state = new CapstoneRunState("Mage Academy", Start);

        var instant = CapstoneRunTracker.BuildRun(state, Start, 0, [(1u, "Solo")]);
        var clockSkew = CapstoneRunTracker.BuildRun(state, Start.AddSeconds(-30), 99, [(1u, "Solo")]);

        Assert.AreEqual(1u, instant.CompletionSeconds);
        Assert.AreEqual(1, instant.FellowshipSize);
        Assert.AreEqual(1u, clockSkew.CompletionSeconds);
        Assert.AreEqual(20, clockSkew.DifficultyTier);
    }
}
