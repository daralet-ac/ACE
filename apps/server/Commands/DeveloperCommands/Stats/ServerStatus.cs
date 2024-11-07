using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using ACE.Common;
using ACE.Database;
using ACE.DatLoader;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Managers;
using ACE.Server.Network;
using ACE.Server.Network.Managers;
using ACE.Server.Physics.Entity;
using ACE.Server.Physics.Managers;
using ACE.Server.WorldObjects;

namespace ACE.Server.Commands.DeveloperCommands.Stats;

public class ServerStatus
{
    // serverstatus
    [CommandHandler(
        "serverstatus",
        AccessLevel.Developer,
        CommandHandlerFlag.None,
        0,
        "Displays a summary of server statistics and usage"
    )]
    public static void HandleServerStatus(Session session, params string[] parameters)
    {
        // This is formatted very similarly to GDL.

        var sb = new StringBuilder();

        var proc = Process.GetCurrentProcess();

        sb.Append($"Server Status:{'\n'}");

        sb.Append($"Host Info: {Environment.OSVersion}, vCPU: {Environment.ProcessorCount}{'\n'}");

        ThreadPool.GetMinThreads(out var minWorkerThreads, out var minCompletionPortThreads);
        ThreadPool.GetMaxThreads(out var maxWorkerThreads, out var maxCompletionPortThreads);
        ThreadPool.GetAvailableThreads(out var availWorkerThreads, out var availCompletionPortThreads);

        sb.Append(
            $"ThreadPool Min: {minWorkerThreads} {minCompletionPortThreads}, Max: {maxWorkerThreads} {maxCompletionPortThreads}, Avail: {availWorkerThreads} {availCompletionPortThreads}, Current: {ThreadPool.ThreadCount}{'\n'}"
        );

        var runTime = DateTime.Now - proc.StartTime;
        sb.Append($"Server Runtime: {(int)runTime.TotalHours}h {runTime.Minutes}m {runTime.Seconds}s{'\n'}");

        sb.Append(
            $"Total CPU Time: {(int)proc.TotalProcessorTime.TotalHours}h {proc.TotalProcessorTime.Minutes}m {proc.TotalProcessorTime.Seconds}s, Threads: {proc.Threads.Count}{'\n'}"
        );

        // todo, add actual system memory used/avail
        sb.Append($"{(proc.PrivateMemorySize64 >> 20):N0} MB used{'\n'}"); // sb.Append($"{(proc.PrivateMemorySize64 >> 20)} MB used, xxxx / yyyy MB physical mem free.{'\n'}");

        sb.Append(
            $"{NetworkManager.GetSessionCount():N0} connections, {NetworkManager.GetAuthenticatedSessionCount():N0} authenticated connections, {NetworkManager.GetUniqueSessionEndpointCount():N0} unique connections, {PlayerManager.GetOnlineCount():N0} players online{'\n'}"
        );
        sb.Append(
            $"Total Accounts Created: {DatabaseManager.Authentication.GetAccountCount():N0}, Total Characters Created: {(PlayerManager.GetOfflineCount() + PlayerManager.GetOnlineCount()):N0}{'\n'}"
        );

        // 330 active objects, 1931 total objects(16777216 buckets.)

        // todo, expand this
        var loadedLandblocks = LandblockManager.GetLoadedLandblocks();
        int dormantLandblocks = 0,
            activeDungeonLandblocks = 0,
            dormantDungeonLandblocks = 0;
        int players = 0,
            creatures = 0,
            missiles = 0,
            other = 0,
            total = 0;
        foreach (var landblock in loadedLandblocks)
        {
            if (landblock.IsDormant)
            {
                dormantLandblocks++;
            }

            if (landblock.IsDungeon)
            {
                if (landblock.IsDormant)
                {
                    dormantDungeonLandblocks++;
                }
                else
                {
                    activeDungeonLandblocks++;
                }
            }

            foreach (var worldObject in landblock.GetAllWorldObjectsForDiagnostics())
            {
                if (worldObject is Player)
                {
                    players++;
                }
                else if (worldObject is Creature)
                {
                    creatures++;
                }
                else if (worldObject.Missile ?? false)
                {
                    missiles++;
                }
                else
                {
                    other++;
                }

                total++;
            }
        }
        sb.Append(
            $"Landblocks: {(loadedLandblocks.Count - dormantLandblocks):N0} active ({activeDungeonLandblocks:N0} dungeons), {dormantLandblocks:N0} dormant ({dormantDungeonLandblocks:N0} dungeons), Landblock Groups: {LandblockManager.LandblockGroupsCount:N0} - Players: {players:N0}, Creatures: {creatures:N0}, Missiles: {missiles:N0}, Other: {other:N0}, Total: {total:N0}.{'\n'}"
        ); // 11 total blocks loaded. 11 active. 0 pending dormancy. 0 dormant. 314 unloaded.
        // 11 total blocks loaded. 11 active. 0 pending dormancy. 0 dormant. 314 unloaded.

        if (ServerPerformanceMonitor.IsRunning)
        {
            sb.Append(
                $"Server Performance Monitor - UpdateGameWorld ~5m {ServerPerformanceMonitor.GetEventHistory5m(ServerPerformanceMonitor.MonitorType.UpdateGameWorld_Entire).AverageEventDuration:N3}, ~1h {ServerPerformanceMonitor.GetEventHistory1h(ServerPerformanceMonitor.MonitorType.UpdateGameWorld_Entire).AverageEventDuration:N3} s{'\n'}"
            );
        }
        else
        {
            sb.Append($"Server Performance Monitor - Not running. To start use /serverperformance start{'\n'}");
        }

        sb.Append(
            $"Threading - WorldThreadCount: {ConfigManager.Config.Server.Threading.LandblockManagerParallelOptions.MaxDegreeOfParallelism}, Multithread Physics: {ConfigManager.Config.Server.Threading.MultiThreadedLandblockGroupPhysicsTicking}, Multithread Non-Physics: {ConfigManager.Config.Server.Threading.MultiThreadedLandblockGroupTicking}, DatabaseThreadCount: {ConfigManager.Config.Server.Threading.DatabaseParallelOptions.MaxDegreeOfParallelism}{'\n'}"
        );

        sb.Append(
            $"Physics Cache Counts - BSPCache: {BSPCache.Count:N0}, GfxObjCache: {GfxObjCache.Count:N0}, PolygonCache: {PolygonCache.Count:N0}, VertexCache: {VertexCache.Count:N0}{'\n'}"
        );

        sb.Append($"Total Server Objects: {ServerObjectManager.ServerObjects.Count:N0}{'\n'}");

        sb.Append(
            $"World DB Cache Counts - Weenies: {DatabaseManager.World.GetWeenieCacheCount():N0}, LandblockInstances: {DatabaseManager.World.GetLandblockInstancesCacheCount():N0}, PointsOfInterest: {DatabaseManager.World.GetPointsOfInterestCacheCount():N0}, Cookbooks: {DatabaseManager.World.GetCookbookCacheCount():N0}, Spells: {DatabaseManager.World.GetSpellCacheCount():N0}, Encounters: {DatabaseManager.World.GetEncounterCacheCount():N0}, Events: {DatabaseManager.World.GetEventsCacheCount():N0}{'\n'}"
        );
        //sb.Append($"Shard DB Counts - Biotas: {DatabaseManager.Shard.BaseDatabase.GetBiotaCount():N0}{'\n'}");
        sb.Append(
            $"Shard DB Counts - Biotas: ~{DatabaseManager.Shard.BaseDatabase.GetEstimatedBiotaCount(ConfigManager.Config.MySql.Shard.Database):N0}{'\n'}"
        );
        if (DatabaseManager.Shard.BaseDatabase is ShardDatabaseWithCaching shardDatabaseWithCaching)
        {
            var biotaIds = shardDatabaseWithCaching.GetBiotaCacheKeys();
            var playerBiotaIds = biotaIds.Count(id => ObjectGuid.IsPlayer(id));
            var nonPlayerBiotaIds = biotaIds.Count - playerBiotaIds;
            sb.Append(
                $"Shard DB Cache Counts - Player Biotas: {playerBiotaIds} ~ {shardDatabaseWithCaching.PlayerBiotaRetentionTime.TotalMinutes:N0} m, Non Players {nonPlayerBiotaIds} ~ {shardDatabaseWithCaching.NonPlayerBiotaRetentionTime.TotalMinutes:N0} m{'\n'}"
            );
        }

        sb.Append(GuidManager.GetDynamicGuidDebugInfo() + '\n');

        sb.Append(
            $"Portal.dat has {DatManager.PortalDat.FileCache.Count:N0} files cached of {DatManager.PortalDat.AllFiles.Count:N0} total{'\n'}"
        );
        sb.Append(
            $"Cell.dat has {DatManager.CellDat.FileCache.Count:N0} files cached of {DatManager.CellDat.AllFiles.Count:N0} total{'\n'}"
        );

        CommandHandlerHelper.WriteOutputInfo(session, $"{sb}");
    }
}
