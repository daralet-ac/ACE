using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.AdvocateCommands.Stats;

public class AllStats
{
    // allstats
    [CommandHandler(
        "allstats",
        AccessLevel.Advocate,
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

        DeveloperDatabaseCommands.HandleDatabaseQueueInfo(session, parameters);
    }
}
