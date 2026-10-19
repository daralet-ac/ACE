using System.Collections.Generic;
using System.Linq;
using ACE.Entity.Enum;
using ACE.Server.Entity.AdminUi;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ACE.Server.Tests;

[TestClass]
public class AdminUiReportTests
{
    [TestMethod]
    public void StabilityBandsMatchTheControllers()
    {
        Assert.AreEqual("Zero", AdminUiRegions.StabilityBand(-999));
        Assert.AreEqual("Zero", AdminUiRegions.StabilityBand(0));
        Assert.AreEqual("Low", AdminUiRegions.StabilityBand(1));
        Assert.AreEqual("Low", AdminUiRegions.StabilityBand(4999));
        Assert.AreEqual("Mid", AdminUiRegions.StabilityBand(5000));
        Assert.AreEqual("High", AdminUiRegions.StabilityBand(10000));
        Assert.AreEqual("High", AdminUiRegions.StabilityBand(14999));
        Assert.AreEqual("100%", AdminUiRegions.StabilityBand(15000));
    }

    [TestMethod]
    public void DifficultyTierComesFromTheChosenMods()
    {
        Assert.AreEqual(3, AdminUiCapstones.DifficultyTierFromMods(new[] { "Swarming", "Difficulty III" }));
        Assert.AreEqual(14, AdminUiCapstones.DifficultyTierFromMods(new[] { "Difficulty XIV" }));
        Assert.AreEqual(20, AdminUiCapstones.DifficultyTierFromMods(new[] { "Difficulty IX", "Difficulty XX" }));
        Assert.AreEqual(4, AdminUiCapstones.DifficultyTierFromMods(new[] { "Difficulty IV" }));
        Assert.AreEqual(0, AdminUiCapstones.DifficultyTierFromMods(new[] { "Swarming", "Inspired" }));
        Assert.AreEqual(0, AdminUiCapstones.DifficultyTierFromMods(null));
    }

    [TestMethod]
    public void CampNamesMatchWorldDb()
    {
        Assert.AreEqual("olthoi_north_camp_north_supply_level", AdminUiRegions.SupplyProperty("North"));
        Assert.AreEqual("OlthoiNorthCampWestWellSupplied", AdminUiRegions.WellSuppliedEvent("West"));
        Assert.AreEqual("OlthoiNorthCampSouthGreatlySupplied", AdminUiRegions.GreatlySuppliedEvent("South"));
    }

    /// <summary>
    /// The plugin reads these names (daralet-admin-plugin src/DaraletAdmin/Feed/ReportModels.cs), so they can't change
    /// without it.
    /// </summary>
    [TestMethod]
    public void ArenaReportJsonHasTheNamesThePluginReads()
    {
        var report = new AdminUiArenaReport(
            new List<AdminUiArenaMatch>(),
            new List<string> { "Bob" },
            new List<AdminUiArenaResult>
            {
                new AdminUiArenaResult(
                    4,
                    "Queue",
                    "Pit",
                    true,
                    false,
                    1,
                    30,
                    95,
                    1,
                    false,
                    new List<List<string>>
                    {
                        new() { "A" },
                        new() { "B" }
                    }
                ),
            }
        );

        var json = AdminUiProtocol.BuildMessages("arenas", report).Single().Split('|', 6)[5];

        foreach (
            var name in new[]
            {
                "\"matches\":",
                "\"queue\":",
                "\"recent\":",
                "\"secondsAgo\":30",
                "\"fightSeconds\":95",
                "\"winningSide\":1",
                "\"calledOff\":false",
                "\"sides\":[[\"A\"],[\"B\"]]",
            }
        )
        {
            StringAssert.Contains(json, name);
        }
    }

    [TestMethod]
    public void EveryRequestIsGatedAtTheLevelOfItsTab()
    {
        Assert.AreEqual(AccessLevel.Sentinel, AdminUiAccess.Tabs["players"]);
        Assert.AreEqual(AccessLevel.Envoy, AdminUiAccess.Tabs["teleport"]);
        Assert.AreEqual(AccessLevel.Admin, AdminUiAccess.Tabs["tidemage"]);
        Assert.AreEqual(AccessLevel.Admin, AdminUiAccess.Tabs["olthoinorth"]);
        Assert.AreEqual(AdminUiAccess.Tabs["olthoinorth"], AdminUiAccess.Requests["olthoinorth"]);

        Assert.AreEqual(AdminUiAccess.Tabs["teleport"], AdminUiAccess.Requests["pois"]);
        Assert.AreEqual(AdminUiAccess.Tabs["arena"], AdminUiAccess.Requests["arenas"]);
        Assert.AreEqual(AdminUiAccess.Tabs["instances"], AdminUiAccess.Requests["instances"]);

        // nothing is open to players: @adminui itself needs Sentinel
        Assert.IsTrue(AdminUiAccess.Tabs.Values.All(level => level >= AccessLevel.Sentinel));
        Assert.IsTrue(AdminUiAccess.Requests.Values.All(level => level >= AccessLevel.Sentinel));
    }

    /// <summary>
    /// The plugin reads these as JSON objects (DataContractJsonSerializer with UseSimpleDictionaryFormat)
    /// </summary>
    [TestMethod]
    public void HelloSendsLevelsAsJsonObjects()
    {
        var hello = new AdminUiHello(
            1,
            "+Admin",
            "Admin",
            5,
            new Dictionary<string, int> { ["players"] = 2 },
            new Dictionary<string, int> { ["boot"] = 5 }
        );

        var json = AdminUiProtocol.BuildMessages("hello", hello).Single().Split('|', 6)[5];

        StringAssert.Contains(json, "\"tabs\":{\"players\":2}");
        StringAssert.Contains(json, "\"commands\":{\"boot\":5}");
    }

    [TestMethod]
    public void TideMageJsonHasTheNamesThePluginReads()
    {
        var report = new AdminUiTideMage(
            7500,
            "Mid",
            true,
            new List<AdminUiEvent> { new AdminUiEvent("TideMageZone3", "On") }
        );

        var json = AdminUiProtocol.BuildMessages("tidemage", report).Single().Split('|', 6)[5];

        StringAssert.Contains(json, "\"stability\":7500");
        StringAssert.Contains(json, "\"band\":\"Mid\"");
        StringAssert.Contains(json, "\"mageHouseLoaded\":true");
        StringAssert.Contains(json, "\"events\":[{\"name\":\"TideMageZone3\",\"state\":\"On\"}]");
    }
}
