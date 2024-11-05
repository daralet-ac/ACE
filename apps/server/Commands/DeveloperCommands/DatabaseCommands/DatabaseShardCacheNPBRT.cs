using System;
using ACE.Database;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.DeveloperCommands.DatabaseCommands;

public class DatabaseShardCacheNPBRT
{
    [CommandHandler(
        "database-shard-cache-npbrt",
        AccessLevel.Developer,
        CommandHandlerFlag.None,
        0,
        "Shard Database, Non-Player Biota Cache - Retention Time (in minutes)"
    )]
    public static void HandleDatabaseShardCacheNPBRT(Session session, params string[] parameters)
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
                $"Shard Database, Non-Player Biota Cache - Retention Time {shardDatabaseWithCaching.NonPlayerBiotaRetentionTime.TotalMinutes:N0} m"
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

        shardDatabaseWithCaching.NonPlayerBiotaRetentionTime = TimeSpan.FromMinutes(value);

        CommandHandlerHelper.WriteOutputInfo(
            session,
            $"Shard Database, Non-Player Biota Cache - Retention Time {shardDatabaseWithCaching.NonPlayerBiotaRetentionTime.TotalMinutes:N0} m"
        );
    }
}
