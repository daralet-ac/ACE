using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using ACE.Server.Commands.DeveloperCommands.DatabaseCommands;

namespace ACE.Server.Commands.DeveloperCommands.Stats;

public class AllStats
{
    // allstats
    [CommandHandler(
        "allstats",
        AccessLevel.Developer,
        CommandHandlerFlag.None,
        0,
        "Displays a summary of all server statistics and usage"
    )]
    public static void HandleAllStats(Session session, params string[] parameters)
    {
        ServerStatus.HandleServerStatus(session, parameters);

        ServerPerformance.HandleServerPerformance(session, parameters);

        LandblockStats.HandleLandblockStats(session, parameters);

        LandblockGroupStats.HandleLBGroupStats(session, parameters);

        GcStatus.HandleGCStatus(session, parameters);

        DatabaseQueueInfo.HandleDatabaseQueueInfo(session, parameters);
    }
}
