using ACE.Database;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.DeveloperCommands.DatabaseCommands;

public class DatabaseQueueInfo
{
    [CommandHandler(
        "databasequeueinfo",
        AccessLevel.Developer,
        CommandHandlerFlag.None,
        0,
        "Show database queue information."
    )]
    public static void HandleDatabaseQueueInfo(Session session, params string[] parameters)
    {
        CommandHandlerHelper.WriteOutputInfo(
            session,
            $"Current database queue count: {DatabaseManager.Shard.QueueCount}"
        );

        DatabaseManager.Shard.GetCurrentQueueWaitTime(result =>
        {
            CommandHandlerHelper.WriteOutputInfo(
                session,
                $"Current database queue wait time: {result.TotalMilliseconds:N0} ms"
            );
        });
    }
}
