using System;
using System.Collections.Generic;
using System.Linq;
using ACE.Server.Lfg;
using ACE.Server.Managers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ACE.Server.Tests;

[TestClass]
public class LfgTests
{
    private const long EvenShareLevel = 50;

    private static readonly (int Level, bool Shrouded)[] NoFellows = Array.Empty<(int, bool)>();

    [TestMethod]
    public void JoiningByName_IgnoresTheStaffPlus()
    {
        Assert.IsTrue(LfgManager.SameName("+Sorc", "Sorc"));
        Assert.IsTrue(LfgManager.SameName("+Sorc", "+sorc"));
        Assert.IsTrue(LfgManager.SameName("Arwen", "arwen "));
        Assert.IsFalse(LfgManager.SameName("Arwen", "Arwe"));
    }

    [TestMethod]
    public void ChannelLines_WaitTheIntervalBetweenPosts()
    {
        var now = new DateTime(2026, 10, 22, 12, 0, 0, DateTimeKind.Utc);
        var fiveMinutes = TimeSpan.FromMinutes(5);

        Assert.IsFalse(LfgManager.TooSoon(DateTime.MinValue, now, fiveMinutes, out _), "never posted");

        Assert.IsTrue(LfgManager.TooSoon(now.AddMinutes(-2), now, fiveMinutes, out var wait));
        Assert.AreEqual(TimeSpan.FromMinutes(3), wait);
        Assert.AreEqual("3 minutes", LfgManager.Minutes(wait));

        Assert.IsFalse(LfgManager.TooSoon(now.AddMinutes(-5), now, fiveMinutes, out _), "exactly the interval later");
        Assert.IsFalse(LfgManager.TooSoon(now.AddSeconds(-1), now, TimeSpan.Zero, out _), "0 for no limit");

        Assert.AreEqual("a minute", LfgManager.Minutes(TimeSpan.FromSeconds(50).Add(TimeSpan.FromSeconds(10))));
        Assert.AreEqual("30 seconds", LfgManager.Minutes(TimeSpan.FromSeconds(30)));
        Assert.AreEqual("5 minutes", LfgManager.Minutes(fiveMinutes));
    }

    private static LfgActivity Lfg(string text)
    {
        var activity = LfgParser.Parse(text.Split(' '), lfm: false, out _, out var error);
        Assert.IsNotNull(activity, error);
        return activity;
    }

    private static LfgActivity Lfm(string text)
    {
        return Lfm(text, out _);
    }

    private static LfgActivity Lfm(string text, out bool autoJoin)
    {
        var activity = LfgParser.Parse(text.Split(' '), lfm: true, out autoJoin, out var error);
        Assert.IsNotNull(activity, error);
        return activity;
    }

    private static bool Matches(string lfg, int level, string lfm)
    {
        return LfgMatching.Matches(Lfg(lfg), level, false, Lfm(lfm), NoFellows, EvenShareLevel);
    }

    [TestMethod]
    public void EveryCapstoneWithALeaderboard_HasARow()
    {
        CollectionAssert.AreEquivalent(
            CapstoneRunTracker.DungeonKeys.Keys.Select(LfgParser.Normalize).ToList(),
            LfgCapstones.All.Select(c => LfgParser.Normalize(c.Name)).ToList()
        );
    }

    [TestMethod]
    public void NoTwoCapstones_ShareAName()
    {
        var names = LfgCapstones.All.SelectMany(c => c.Aliases.Append(c.Name)).Select(LfgParser.Normalize).ToList();

        Assert.AreEqual(names.Count, names.Distinct().Count(), string.Join(", ", names.GroupBy(n => n).Where(g => g.Count() > 1).Select(g => g.Key)));
    }

    [TestMethod]
    public void Lfg_ReadsTheWaysItIsAskedFor()
    {
        var upTo30 = Lfg("dungeon 30");
        Assert.AreEqual(LfgActivityKind.Capstone, upTo30.Kind);
        Assert.IsNull(upTo30.Dungeon);
        Assert.AreEqual(30, upTo30.MaxLevel);

        var gmg = Lfg("green mire grave d10");
        Assert.AreEqual("Green Mire Grave", gmg.Dungeon.Name);
        Assert.AreEqual(10, gmg.DifficultyMin);
        Assert.AreEqual(10, gmg.DifficultyMax);

        var any = Lfg("dungeon");
        Assert.IsNull(any.Dungeon);
        Assert.IsNull(any.MaxLevel);
        Assert.IsNull(any.DifficultyMin);

        var atLeast5 = Lfg("dungeon d5");
        Assert.AreEqual(5, atLeast5.DifficultyMin);
        Assert.IsNull(atLeast5.DifficultyMax);

        var quest = Lfg("quest Cragstone");
        Assert.AreEqual(LfgActivityKind.Quest, quest.Kind);
        Assert.AreEqual("Cragstone", quest.Text);

        Assert.AreEqual(LfgActivityKind.Leveling, Lfg("leveling").Kind);

        Assert.AreEqual("Smugglers Hideaway", Lfg("Smuggler's Hideaway").Dungeon.Name);
        Assert.AreEqual("Green Mire Grave", Lfg("GMG").Dungeon.Name);
    }

    [TestMethod]
    public void Lfm_ReadsTheWaysItIsAskedFor()
    {
        var gmg = Lfm("green mire grave d20", out var autoJoin);
        Assert.AreEqual("Green Mire Grave", gmg.Dungeon.Name);
        Assert.AreEqual(20, gmg.Difficulty);
        Assert.IsTrue(autoJoin);

        var anyD10 = Lfm("dungeon d10");
        Assert.IsNull(anyD10.Dungeon);
        Assert.AreEqual(10, anyD10.Difficulty);

        Assert.AreEqual(LfgActivityKind.Leveling, Lfm("leveling").Kind);
        Assert.AreEqual("arwic", Lfm("quest arwic").Text);
        Assert.AreEqual(LfgActivityKind.Other, Lfm("esper").Kind);
        Assert.AreEqual("Olthoi north", Lfm("Olthoi north").Text);

        Lfm("gmg d20 manual", out var manual);
        Assert.IsFalse(manual);

        Assert.AreEqual(0, Lfm("gmg").Difficulty);
    }

    [TestMethod]
    public void WhatCantBeRead_SaysWhy()
    {
        Assert.IsNull(LfgParser.Parse(new[] { "gmg", "d25" }, lfm: false, out _, out var tooHard));
        Assert.IsNotNull(tooHard);

        Assert.IsNull(LfgParser.Parse(new[] { "gmg", "d5+" }, lfm: true, out _, out var lfmRange));
        Assert.IsNotNull(lfmRange);

        Assert.IsNull(LfgParser.Parse(new[] { "dungeon", "soon" }, lfm: false, out _, out var unknown));
        Assert.IsNotNull(unknown);

        Assert.IsNull(LfgParser.Parse(new[] { "manual" }, lfm: true, out _, out var nothing));
        Assert.IsNotNull(nothing);
    }

    [TestMethod]
    public void AnyCapstone_UpToALevel()
    {
        Assert.IsTrue(Matches("dungeon 30", 40, "grievous vault"));
        Assert.IsFalse(Matches("dungeon 30", 40, "folthid cellar"));

        // up to your own level, without one
        Assert.IsTrue(Matches("dungeon", 25, "hoth"));
        Assert.IsFalse(Matches("dungeon", 25, "folthid cellar"));

        Assert.IsTrue(Matches("dungeon", 25, "dungeon d10"));
        Assert.IsFalse(Matches("dungeon", 9, "dungeon d10"));
    }

    [TestMethod]
    public void OneCapstone_AtItsDifficulty()
    {
        Assert.IsTrue(Matches("gmg d10", 15, "green mire grave d10"));
        Assert.IsFalse(Matches("gmg d10", 15, "gmg d20"));
        Assert.IsTrue(Matches("gmg", 15, "gmg d20"));

        // a fellowship that hasn't said which capstone isn't what someone after one asked for
        Assert.IsFalse(Matches("gmg d10", 15, "dungeon d10"));
        Assert.IsFalse(Matches("gmg", 15, "sand shallow"));

        Assert.IsTrue(Matches("dungeon d5", 15, "gmg d10"));
        Assert.IsFalse(Matches("dungeon d5", 15, "gmg"));
        Assert.IsTrue(Matches("dungeon d5-10", 15, "gmg d10"));
        Assert.IsFalse(Matches("dungeon d5-10", 15, "gmg d11"));
    }

    [TestMethod]
    public void QuestsAndFreeText_ByTheirWords()
    {
        Assert.IsTrue(Matches("quest cragstone", 20, "quest Cragstone"));
        Assert.IsFalse(Matches("quest cragstone", 20, "quest arwic"));
        Assert.IsTrue(Matches("quest", 20, "quest arwic"));
        Assert.IsTrue(Matches("quest cragstone", 20, "quest"));

        Assert.IsTrue(Matches("olthoi", 20, "Olthoi north"));
        Assert.IsTrue(Matches("the olthoi north", 20, "olthoi"));
        Assert.IsFalse(Matches("esper", 20, "olthoi north"));
        Assert.IsFalse(Matches("esper", 20, "quest esper"));
    }

    [TestMethod]
    public void Leveling_OnlyWhereXpIsSharedEvenly()
    {
        var leveling = Lfg("leveling");
        var offered = Lfm("leveling");

        Assert.IsTrue(LfgMatching.Matches(leveling, 35, false, offered, new[] { (30, false), (33, false) }, EvenShareLevel));
        Assert.IsFalse(LfgMatching.Matches(leveling, 36, false, offered, new[] { (30, false) }, EvenShareLevel));
        Assert.IsTrue(LfgMatching.Matches(leveling, 70, false, offered, new[] { (55, false) }, EvenShareLevel));

        // Shrouded, up to 25 levels above the lowest counts as the lowest
        Assert.IsTrue(LfgMatching.Matches(leveling, 50, true, offered, new[] { (30, false) }, EvenShareLevel));
        Assert.IsFalse(LfgMatching.Matches(leveling, 56, true, offered, new[] { (30, false) }, EvenShareLevel));
    }

    [TestMethod]
    public void Lines_SayWhatItIsAndHowToJoin()
    {
        var gmg = Lfm("green mire grave d20");
        Assert.AreEqual("Green Mire Grave d20", gmg.Describe(lfm: true));
        Assert.AreEqual("gmg d20", gmg.JoinWords());

        Assert.AreEqual("any capstone d10", Lfm("dungeon d10").Describe(lfm: true));
        Assert.AreEqual("dungeon d10", Lfm("dungeon d10").JoinWords());
        Assert.AreEqual("arwic quests", Lfm("quest arwic").Describe(lfm: true));
        Assert.AreEqual("quest arwic", Lfm("quest arwic").JoinWords());

        Assert.AreEqual("any capstone up to level 30, d5+", Lfg("dungeon 30 d5").Describe(lfm: false));
        Assert.AreEqual("any capstone d5+", Lfg("dungeon d5").Describe(lfm: false));
        Assert.AreEqual("Green Mire Grave d10", Lfg("gmg d10").Describe(lfm: false));

        // what the line says to type is read back as the same thing
        foreach (var text in new[] { "green mire grave d20", "dungeon d10", "quest arwic", "leveling", "Olthoi north" })
        {
            var offered = Lfm(text);
            var seeking = Lfg(offered.JoinWords());
            Assert.IsTrue(
                LfgMatching.Matches(seeking, 60, false, offered, new List<(int, bool)> { (60, false) }, EvenShareLevel),
                text
            );
        }
    }
}
