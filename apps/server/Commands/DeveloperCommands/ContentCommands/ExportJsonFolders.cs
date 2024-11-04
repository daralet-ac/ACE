using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using static ACE.Server.Commands.DeveloperCommands.ContentCommands.ContentCommandUtilities;

namespace ACE.Server.Commands.DeveloperCommands.ContentCommands;

public class ExportJsonFolders
{
    [CommandHandler(
        "export-json-folders",
        AccessLevel.Developer,
        CommandHandlerFlag.None,
        1,
        "Exports content from database to JSON file in a WeenieType/ItemType folder structure",
        "<wcid>"
    )]
    public static void HandleExportJsonFolder(Session session, params string[] parameters)
    {
        var param = parameters[0];
        ExportJsonWeenie(session, param, true);
    }
}
