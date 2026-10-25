using System;
using System.Linq;
using ACE.Entity.Enum.Properties;
using ACE.Server.Commands.AdminCommands;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ACE.Server.Tests;

[TestClass]
public class LeaderboardExclusionTests
{
    [TestMethod]
    public void ParseExclusion_ExcludeAndIncludeTakeTheWholeName()
    {
        Assert.AreEqual((Leaderboard.ExclusionAction.Exclude, "Raider Finder"), Leaderboard.ParseExclusion(["exclude", "Raider", "Finder"]));
        Assert.AreEqual((Leaderboard.ExclusionAction.Include, "Avaricious Golem"), Leaderboard.ParseExclusion(["include", "Avaricious", "Golem"]));
        Assert.AreEqual((Leaderboard.ExclusionAction.Exclude, "Solo"), Leaderboard.ParseExclusion(["EXCLUDE", "Solo"]));
    }

    [TestMethod]
    public void ParseExclusion_ListNeedsNoName()
    {
        Assert.AreEqual((Leaderboard.ExclusionAction.List, ""), Leaderboard.ParseExclusion(["list"]));
    }

    [TestMethod]
    public void ParseExclusion_AnythingElseIsTheUsage()
    {
        Assert.AreEqual(Leaderboard.ExclusionAction.Usage, Leaderboard.ParseExclusion([]).Action);
        Assert.AreEqual(Leaderboard.ExclusionAction.Usage, Leaderboard.ParseExclusion(["exclude"]).Action);
        Assert.AreEqual(Leaderboard.ExclusionAction.Usage, Leaderboard.ParseExclusion(["include", "  "]).Action);
        Assert.AreEqual(Leaderboard.ExclusionAction.Usage, Leaderboard.ParseExclusion(["remove", "Someone"]).Action);
    }

    [TestMethod]
    public void RunExclusion_WithNothingToDoSaysTheUsageOnce()
    {
        var said = new System.Collections.Generic.List<string>();

        Leaderboard.RunExclusion([], said.Add);
        Leaderboard.RunExclusion(["exclude"], said.Add);

        Assert.AreEqual(2, said.Count);
        Assert.IsTrue(said.All(m => m.Contains("@leaderboard") && m.Contains("leaderboardexclude")), string.Join("|", said));
    }

    [TestMethod]
    public void ConsoleCommand_IsConsoleOnlyAndHasANameOfItsOwn()
    {
        var handlers = typeof(Leaderboard)
            .Assembly.GetTypes()
            .SelectMany(t => t.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
            .SelectMany(m => m.GetCustomAttributes(typeof(ACE.Server.Commands.Handlers.CommandHandlerAttribute), false))
            .Cast<ACE.Server.Commands.Handlers.CommandHandlerAttribute>()
            .Where(a => a.Command is "leaderboardexclude" or "leaderboard")
            .ToList();

        Assert.AreEqual(2, handlers.Count);
        Assert.AreEqual(
            ACE.Server.Commands.Handlers.CommandHandlerFlag.ConsoleInvoke,
            handlers.Single(h => h.Command == "leaderboardexclude").Flags
        );
    }

    [TestMethod]
    public void LeaderboardExcluded_IsAPropertyOfItsOwn()
    {
        // the leaderboard API reads this id straight from biota_properties_bool (LeaderboardRepository)
        Assert.AreEqual(9019, (int)PropertyBool.LeaderboardExcluded);

        var ids = Enum.GetValues<PropertyBool>().Select(p => (int)p).ToList();
        Assert.AreEqual(ids.Count, ids.Distinct().Count());
    }
}
