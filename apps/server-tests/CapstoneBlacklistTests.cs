using System;
using System.Linq;
using ACE.Entity.Enum.Properties;
using ACE.Server.Commands.AdminCommands;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ACE.Server.Tests;

[TestClass]
public class CapstoneBlacklistTests
{
    [TestMethod]
    public void ParseBlacklist_AddAndRemoveTakeTheWholeName()
    {
        Assert.AreEqual((Capstone.BlacklistAction.Add, "Raider Finder"), Capstone.ParseBlacklist(["add", "Raider", "Finder"]));
        Assert.AreEqual((Capstone.BlacklistAction.Remove, "Avaricious Golem"), Capstone.ParseBlacklist(["remove", "Avaricious", "Golem"]));
        Assert.AreEqual((Capstone.BlacklistAction.Add, "Solo"), Capstone.ParseBlacklist(["ADD", "Solo"]));
    }

    [TestMethod]
    public void ParseBlacklist_ListNeedsNoName()
    {
        Assert.AreEqual((Capstone.BlacklistAction.List, ""), Capstone.ParseBlacklist(["list"]));
        Assert.AreEqual((Capstone.BlacklistAction.List, ""), Capstone.ParseBlacklist(["List", "whatever"]));
    }

    [TestMethod]
    public void ParseBlacklist_AnythingElseIsTheUsage()
    {
        Assert.AreEqual(Capstone.BlacklistAction.Usage, Capstone.ParseBlacklist([]).Action);
        Assert.AreEqual(Capstone.BlacklistAction.Usage, Capstone.ParseBlacklist(["add"]).Action);
        Assert.AreEqual(Capstone.BlacklistAction.Usage, Capstone.ParseBlacklist(["remove"]).Action);
        Assert.AreEqual(Capstone.BlacklistAction.Usage, Capstone.ParseBlacklist(["delete", "Someone"]).Action);
    }

    [TestMethod]
    public void RunBlacklist_WithNothingToDoSaysTheUsageOnce()
    {
        // the one way in game, console and Discord all say what they were not given
        var said = new System.Collections.Generic.List<string>();

        Capstone.RunBlacklist([], said.Add);
        Capstone.RunBlacklist(["add"], said.Add);

        Assert.AreEqual(2, said.Count);
        Assert.IsTrue(said.All(m => m.Contains("blacklist") && m.Contains("capstoneblacklist")), string.Join("|", said));
    }

    [TestMethod]
    public void ConsoleCommand_IsConsoleOnlyAndHasANameOfItsOwn()
    {
        var handlers = typeof(Capstone)
            .Assembly.GetTypes()
            .SelectMany(t => t.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
            .SelectMany(m => m.GetCustomAttributes(typeof(ACE.Server.Commands.Handlers.CommandHandlerAttribute), false))
            .Cast<ACE.Server.Commands.Handlers.CommandHandlerAttribute>()
            .Where(a => a.Command == "capstoneblacklist")
            .ToList();

        // the command manager keeps a console command and an in-game one apart, so it has to be a command of its own
        Assert.AreEqual(1, handlers.Count);
        Assert.AreEqual(ACE.Server.Commands.Handlers.CommandHandlerFlag.ConsoleInvoke, handlers[0].Flags);
    }

    [TestMethod]
    public void CapstoneTrackingExcluded_IsAPropertyOfItsOwn()
    {
        // the leaderboard API reads this id straight from biota_properties_bool (LeaderboardRepository)
        Assert.AreEqual(9018, (int)PropertyBool.CapstoneTrackingExcluded);

        var ids = Enum.GetValues<PropertyBool>().Select(p => (int)p).ToList();
        Assert.AreEqual(ids.Count, ids.Distinct().Count());
    }
}
