using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using static ACE.Server.Commands.DeveloperCommands.ContentCommands.ContentCommandUtilities;

namespace ACE.Server.Commands.DeveloperCommands.ContentCommands;

public class ImportJson
{
    [CommandHandler(
        "import-json",
        AccessLevel.Developer,
        CommandHandlerFlag.None,
        1,
        "Imports json data from the Content folder",
        "<type> <wcid>\n<type> - landblock, quest, recipe, spell, weenie (default if not specified)\n<wcid> - filename prefix to search for. can be 'all' to import all files for this content type"
    )]
    public static void HandleImportJson(Session session, params string[] parameters)
    {
        var param = parameters[0];
        var contentType = FileType.Weenie;

        if (parameters.Length > 1)
        {
            contentType = GetContentType(parameters, ref param);

            if (contentType == FileType.Undefined)
            {
                CommandHandlerHelper.WriteOutputInfo(session, $"Unknown content type '{parameters[1]}'");
                return;
            }
        }
        switch (contentType)
        {
            case FileType.LandblockInstance:
                ImportJsonLandblock(session, param);
                break;

            case FileType.Quest:
                ImportJsonQuest(session, param);
                break;

            case FileType.Recipe:
                ImportJsonRecipe(session, param);
                break;

            case FileType.Weenie:
                ImportJsonWeenie(session, param);
                break;
        }
    }
}
