using System.Collections.Generic;
using ACE.Common.Performance;
using ACE.Entity.Enum;
using ACE.Server.Entity.AdminUi;
using ACE.Server.Managers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ACE.Server.Tests;

[TestClass]
public class AdminUiPerformanceTests
{
    /// <summary>
    /// The plugin reads these names (daralet-admin-plugin src/DaraletAdmin/Feed/ReportModels.cs), so they can't change
    /// without it.
    /// </summary>
    [TestMethod]
    public void SnapshotJsonHasTheNamesThePluginReads()
    {
        var snapshot = new AdminUiPerformanceSnapshot(
            3600,
            125.5,
            8,
            2048,
            900,
            64,
            12,
            10,
            40,
            15,
            9,
            1500,
            30000,
            true,
            false,
            new AdminUiTickTotals(6000, 30000.5, 250, 4.2, 120)
        );

        var json = AdminUiProtocol.BuildMessages("perf", snapshot).JoinedJson();

        StringAssert.Contains(
            json,
            "{\"uptimeSeconds\":3600,\"cpuSeconds\":125.5,\"processorCount\":8,\"memoryMb\":2048,\"managedMemoryMb\":900,\"threads\":64,"
                + "\"connections\":12,\"playersOnline\":10,\"landblocksActive\":40,\"landblocksDormant\":15,\"landblockGroups\":9,"
                + "\"creatures\":1500,\"otherObjects\":30000,\"monitorRunning\":true,\"cumulativeRunning\":false,"
                + "\"tick\":{\"events\":6000,\"totalMs\":30000.5,\"longestMs\":250,\"lastMs\":4.2,\"windowSeconds\":120}}"
        );
    }

    [TestMethod]
    public void TimersJsonHasTheNamesThePluginReads()
    {
        var timers = new AdminUiTimers(
            true,
            true,
            100,
            200,
            300,
            new List<AdminUiTimer> { new AdminUiTimer("UpdateGameWorld_Entire", "Game world", 50, 1.5, 9, 1.4, 12, 1.3, 40) }
        );

        var json = AdminUiProtocol.BuildMessages("perftimers", timers).JoinedJson();

        Assert.AreEqual(
            "{\"running\":true,\"cumulativeRunning\":true,\"window5mSeconds\":100,\"window1hSeconds\":200,\"window24hSeconds\":300,"
                + "\"timers\":[{\"name\":\"UpdateGameWorld_Entire\",\"group\":\"Game world\",\"hits5m\":50,\"avg5m\":1.5,\"longest5m\":9,"
                + "\"avg1h\":1.4,\"longest1h\":12,\"avg24h\":1.3,\"longest24h\":40}]}",
            json
        );
    }

    [TestMethod]
    public void TimersAreInMilliseconds()
    {
        var last5m = new TimedEventHistory();
        last5m.RegisterEvent(0.002);
        last5m.RegisterEvent(0.004);

        var last1h = new TimedEventHistory();
        last1h.RegisterEvent(0.0105);

        var timer = AdminUiPerformance.Timer("X", "Per tick", last5m, last1h, new TimedEventHistory());

        Assert.AreEqual(2, timer.Hits5m);
        Assert.AreEqual(3, timer.Avg5m, 1e-9);
        Assert.AreEqual(4, timer.Longest5m, 1e-9);
        Assert.AreEqual(10.5, timer.Avg1h, 1e-9);
        Assert.AreEqual(0, timer.Avg24h);

        var tick = AdminUiPerformance.TickTotals(last5m, 59.6);

        Assert.AreEqual(2, tick.Events);
        Assert.AreEqual(6, tick.TotalMs, 1e-9);
        Assert.AreEqual(4, tick.LastMs, 1e-9);
        Assert.AreEqual(60, tick.WindowSeconds);
    }

    [TestMethod]
    public void MonitorsAreGroupedWhereTheyreTimed()
    {
        Assert.AreEqual(AdminUiPerformance.GroupWorld, AdminUiPerformance.Group(ServerPerformanceMonitor.MonitorType.PlayerManager_Tick));
        Assert.AreEqual(
            AdminUiPerformance.GroupWorld,
            AdminUiPerformance.Group(ServerPerformanceMonitor.MonitorType.NetworkManager_DoSessionWork)
        );
        Assert.AreEqual(
            AdminUiPerformance.GroupGameWorld,
            AdminUiPerformance.Group(ServerPerformanceMonitor.MonitorType.UpdateGameWorld_Entire)
        );
        Assert.AreEqual(
            AdminUiPerformance.GroupGameWorld,
            AdminUiPerformance.Group(ServerPerformanceMonitor.MonitorType.LandblockManager_TickSingleThreadedWork)
        );
        Assert.AreEqual(
            AdminUiPerformance.GroupSessions,
            AdminUiPerformance.Group(ServerPerformanceMonitor.MonitorType.DoSessionWork_RemoveSessions)
        );
    }

    [TestMethod]
    public void PerformanceIsDeveloperOnly()
    {
        Assert.AreEqual(AccessLevel.Developer, AdminUiAccess.Tabs["performance"]);
        Assert.AreEqual(AccessLevel.Developer, AdminUiAccess.Requests["perf"]);
        Assert.AreEqual(AccessLevel.Developer, AdminUiAccess.Requests["perftimers"]);
    }
}
