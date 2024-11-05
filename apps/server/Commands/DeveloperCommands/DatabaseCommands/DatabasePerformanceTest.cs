using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Commands.Processors;
using ACE.Server.Network;

namespace ACE.Server.Commands.DeveloperCommands.DatabaseCommands;

public class DatabasePerformanceTest
{
    [CommandHandler(
        "databaseperftest",
        AccessLevel.Developer,
        CommandHandlerFlag.None,
        0,
        "Test server/database performance.",
        "biotasPerTest\n" + "optional parameter biotasPerTest if omitted 1000"
    )]
    public static void HandleDatabasePerfTest(Session session, params string[] parameters)
    {
        var biotasPerTest = DatabasePerfTest.DefaultBiotasTestCount;

        if (parameters?.Length > 0)
        {
            int.TryParse(parameters[0], out biotasPerTest);
        }

        var processor = new DatabasePerfTest();
        processor.RunAsync(session, biotasPerTest);
    }
}
