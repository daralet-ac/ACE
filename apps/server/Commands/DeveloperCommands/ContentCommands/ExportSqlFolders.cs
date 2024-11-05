using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using static ACE.Server.Commands.DeveloperCommands.ContentCommands.ContentCommandUtilities;

namespace ACE.Server.Commands.DeveloperCommands.ContentCommands;

public class ExportSqlFolders
{
    [CommandHandler(
        "export-sql-folders",
        AccessLevel.Developer,
        CommandHandlerFlag.None,
        1,
        "Exports weenie content from database to an SQL file in a WeenieType/ItemType folder structure",
        "<wcid>"
    )]
    public static void HandleExportSqlFolder(Session session, params string[] parameters)
    {
        var param = parameters[0];
        ExportSQLWeenie(session, param, true);
    }
}
