using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using static ACE.Server.Commands.DeveloperCommands.ContentCommands.ContentCommandUtilities;

namespace ACE.Server.Commands.DeveloperCommands.ContentCommands;

public class ImportSqlFolders
{
    [CommandHandler(
        "import-sql-folders",
        AccessLevel.Developer,
        CommandHandlerFlag.None,
        1,
        "Imports all weenie sql data from the Content folder and all sub-folders",
        "<wcid>\n<wcid> - wcid prefix to search for. can be 'all' to import everything"
    )]
    public static void HandleImportSQLFolders(Session session, params string[] parameters)
    {
        var param = parameters[0];
        ImportSQLWeenie(session, param, true);
    }
}
