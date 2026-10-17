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
    public void CapstoneTrackingExcluded_IsAPropertyOfItsOwn()
    {
        // the leaderboard API reads this id straight from biota_properties_bool (LeaderboardRepository)
        Assert.AreEqual(9018, (int)PropertyBool.CapstoneTrackingExcluded);

        var ids = Enum.GetValues<PropertyBool>().Select(p => (int)p).ToList();
        Assert.AreEqual(ids.Count, ids.Distinct().Count());
    }
}
