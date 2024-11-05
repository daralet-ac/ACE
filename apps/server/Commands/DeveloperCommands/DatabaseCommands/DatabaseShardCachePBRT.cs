using System;
using ACE.Database;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.DeveloperCommands.DatabaseCommands;

public class DatabaseShardCachePBRT
{
    [CommandHandler(
        "database-shard-cache-pbrt",
        AccessLevel.Developer,
        CommandHandlerFlag.None,
        0,
        "Shard Database, Player Biota Cache - Retention Time (in minutes)"
    )]
    public static void HandleDatabaseShardCachePBRT(Session session, params string[] parameters)
    {
        if (!(DatabaseManager.Shard.BaseDatabase is ShardDatabaseWithCaching shardDatabaseWithCaching))
        {
            CommandHandlerHelper.WriteOutputInfo(session, "DatabaseManager is not using ShardDatabaseWithCaching");

            return;
        }

        if (parameters == null || parameters.Length == 0)
        {
            CommandHandlerHelper.WriteOutputInfo(
                session,
                $"Shard Database, Player Biota Cache - Retention Time {shardDatabaseWithCaching.PlayerBiotaRetentionTime.TotalMinutes:N0} m"
            );

            return;
        }

        if (!int.TryParse(parameters[0], out var value) || value < 0)
        {
            CommandHandlerHelper.WriteOutputInfo(
                session,
                "Unable to parse argument. Specify retention time in integer minutes."
            );

            return;
        }

        shardDatabaseWithCaching.PlayerBiotaRetentionTime = TimeSpan.FromMinutes(value);

        CommandHandlerHelper.WriteOutputInfo(
            session,
            $"Shard Database, Player Biota Cache - Retention Time {shardDatabaseWithCaching.PlayerBiotaRetentionTime.TotalMinutes:N0} m"
        );
    }
}
