using System;
using System.Collections.Generic;
using System.Diagnostics;
using ACE.Common.Performance;
using ACE.Server.Managers;
using ACE.Server.Network.Managers;
using ACE.Server.WorldObjects;

namespace ACE.Server.Entity.AdminUi;

/// <summary>
/// The game loop's timing from ServerPerformanceMonitor's 5 minute window, as running totals. The window is cleared every 5
/// minutes, so the plugin takes the difference between two snapshots for the time between them, and starts over when Events
/// goes down.
/// </summary>
/// <param name="Events">Ticks of WorldManager.UpdateGameWorld timed since the window was cleared</param>
/// <param name="TotalMs">Their time added up</param>
/// <param name="LongestMs">The longest tick since the window was cleared</param>
/// <param name="LastMs">The latest tick</param>
/// <param name="WindowSeconds">How long ago the window was cleared</param>
public record AdminUiTickTotals(long Events, double TotalMs, double LongestMs, double LastMs, double WindowSeconds);

/// <summary>
/// One snapshot for the Performance tab's graphs, asked for every few seconds, so it's kept small: about 7 chat lines.
/// </summary>
/// <param name="CpuSeconds">The server process's CPU time since it started: the plugin works out CPU use from two snapshots</param>
/// <param name="MemoryMb">The process's private memory</param>
/// <param name="ManagedMemoryMb">What the .NET garbage collector holds of that</param>
/// <param name="Connections">Authenticated sessions</param>
/// <param name="Creatures">Creatures (players not included) on loaded landblocks</param>
/// <param name="OtherObjects">Every other object on loaded landblocks</param>
/// <param name="MonitorRunning">/serverperformance start: without it Tick is null</param>
/// <param name="CumulativeRunning">/serverperformance start cumulative: the per-tick totals in the timers table</param>
public record AdminUiPerformanceSnapshot(
    double UptimeSeconds,
    double CpuSeconds,
    int ProcessorCount,
    long MemoryMb,
    long ManagedMemoryMb,
    int Threads,
    int Connections,
    int PlayersOnline,
    int LandblocksActive,
    int LandblocksDormant,
    int LandblockGroups,
    int Creatures,
    int OtherObjects,
    bool MonitorRunning,
    bool CumulativeRunning,
    AdminUiTickTotals Tick
);

/// <summary>
/// One of ServerPerformanceMonitor's timers over its three windows, in milliseconds
/// </summary>
/// <param name="Group">Where it's timed: a section of the loop, or "Per tick" for the cumulative ones</param>
public record AdminUiTimer(
    string Name,
    string Group,
    long Hits5m,
    double Avg5m,
    double Longest5m,
    double Avg1h,
    double Longest1h,
    double Avg24h,
    double Longest24h
);

/// <summary>
/// Every timer ServerPerformanceMonitor keeps, for the Performance tab's table. Larger than the snapshot (about 25 chat lines),
/// so the plugin asks for it less often.
/// </summary>
public record AdminUiTimers(
    bool Running,
    bool CumulativeRunning,
    double Window5mSeconds,
    double Window1hSeconds,
    double Window24hSeconds,
    List<AdminUiTimer> Timers
);

public static class AdminUiPerformance
{
    public const string GroupWorld = "World loop";
    public const string GroupGameWorld = "Game world";
    public const string GroupSessions = "Sessions";
    public const string GroupPerTick = "Per tick";

    public static AdminUiPerformanceSnapshot Get()
    {
        using var proc = Process.GetCurrentProcess();

        // the same counts /serverstatus makes
        var landblocks = LandblockManager.GetLoadedLandblocks();
        int dormant = 0,
            creatures = 0,
            other = 0;

        foreach (var landblock in landblocks)
        {
            if (landblock.IsDormant)
            {
                dormant++;
            }

            foreach (var worldObject in landblock.GetAllWorldObjectsForDiagnostics())
            {
                if (worldObject is Player)
                {
                    continue;
                }

                if (worldObject is Creature)
                {
                    creatures++;
                }
                else
                {
                    other++;
                }
            }
        }

        AdminUiTickTotals tick = null;

        if (ServerPerformanceMonitor.IsRunning)
        {
            tick = TickTotals(
                ServerPerformanceMonitor.GetEventHistory5m(ServerPerformanceMonitor.MonitorType.UpdateGameWorld_Entire),
                ServerPerformanceMonitor.Monitors5mRunTime.TotalSeconds
            );
        }

        return new AdminUiPerformanceSnapshot(
            Math.Round((DateTime.Now - proc.StartTime).TotalSeconds),
            Math.Round(proc.TotalProcessorTime.TotalSeconds, 2),
            Environment.ProcessorCount,
            proc.PrivateMemorySize64 >> 20,
            GC.GetTotalMemory(false) >> 20,
            proc.Threads.Count,
            NetworkManager.GetAuthenticatedSessionCount(),
            PlayerManager.GetOnlineCount(),
            landblocks.Count - dormant,
            dormant,
            LandblockManager.LandblockGroupsCount,
            creatures,
            other,
            ServerPerformanceMonitor.IsRunning,
            ServerPerformanceMonitor.IsRunningCumulative,
            tick
        );
    }

    public static AdminUiTickTotals TickTotals(TimedEventHistory history, double windowSeconds)
    {
        return new AdminUiTickTotals(
            history.TotalEvents,
            Ms(history.TotalSeconds),
            Ms(history.LongestEvent),
            Ms(history.LastEvent),
            Math.Round(windowSeconds)
        );
    }

    public static AdminUiTimers GetTimers()
    {
        var timers = new List<AdminUiTimer>();

        if (ServerPerformanceMonitor.IsRunning)
        {
            for (var i = 0; i < (int)ServerPerformanceMonitor.MonitorType.MaxItems; i++)
            {
                var type = (ServerPerformanceMonitor.MonitorType)i;

                timers.Add(
                    Timer(
                        type.ToString(),
                        Group(type),
                        ServerPerformanceMonitor.GetEventHistory5m(type),
                        ServerPerformanceMonitor.GetEventHistory1h(type),
                        ServerPerformanceMonitor.GetEventHistory24h(type)
                    )
                );
            }

            if (ServerPerformanceMonitor.IsRunningCumulative)
            {
                for (var i = 0; i < (int)ServerPerformanceMonitor.CumulativeEventHistoryType.MaxItems; i++)
                {
                    var type = (ServerPerformanceMonitor.CumulativeEventHistoryType)i;

                    timers.Add(
                        Timer(
                            type.ToString(),
                            GroupPerTick,
                            ServerPerformanceMonitor.GetCumulativeHistory5m(type),
                            ServerPerformanceMonitor.GetCumulativeHistory1h(type),
                            ServerPerformanceMonitor.GetCumulativeHistory24h(type)
                        )
                    );
                }
            }
        }

        return new AdminUiTimers(
            ServerPerformanceMonitor.IsRunning,
            ServerPerformanceMonitor.IsRunningCumulative,
            Math.Round(ServerPerformanceMonitor.Monitors5mRunTime.TotalSeconds),
            Math.Round(ServerPerformanceMonitor.Monitors1hRunTime.TotalSeconds),
            Math.Round(ServerPerformanceMonitor.Monitors24hRunTime.TotalSeconds),
            timers
        );
    }

    public static AdminUiTimer Timer(
        string name,
        string group,
        TimedEventHistory last5m,
        TimedEventHistory last1h,
        TimedEventHistory last24h
    )
    {
        return new AdminUiTimer(
            name,
            group,
            last5m.TotalEvents,
            Ms(last5m.AverageEventDuration),
            Ms(last5m.LongestEvent),
            Ms(last1h.AverageEventDuration),
            Ms(last1h.LongestEvent),
            Ms(last24h.AverageEventDuration),
            Ms(last24h.LongestEvent)
        );
    }

    /// <summary>
    /// Where ServerPerformanceMonitor times each of its monitors (see its MonitorType comments)
    /// </summary>
    public static string Group(ServerPerformanceMonitor.MonitorType type)
    {
        if (type <= ServerPerformanceMonitor.MonitorType.NetworkManager_DoSessionWork)
        {
            return GroupWorld;
        }

        if (type <= ServerPerformanceMonitor.MonitorType.LandblockManager_TickSingleThreadedWork)
        {
            return GroupGameWorld;
        }

        return GroupSessions;
    }

    /// <summary>
    /// Seconds to milliseconds, to a microsecond: shorter JSON than seconds to 6 places, and what the tab shows
    /// </summary>
    public static double Ms(double seconds)
    {
        return Math.Round(seconds * 1000, 3);
    }
}
