using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using static ACE.Server.Commands.DeveloperCommands.ContentCommands.ContentCommandUtilities;

namespace ACE.Server.Commands.DeveloperCommands.ContentCommands;

public class ExportSql
{
    [CommandHandler(
        "export-sql",
        AccessLevel.Developer,
        CommandHandlerFlag.None,
        1,
        "Exports content from database to SQL file",
        "<optional type> <id>\n<optional type> - landblock, quest, recipe, spell, weenie (default if not specified)\n<id> - wcid or content id to export"
    )]
    public static void HandleExportSql(Session session, params string[] parameters)
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
                ExportSQLLandblock(session, param);
                break;

            case FileType.Quest:
                ExportSQLQuest(session, param);
                break;

            case FileType.Recipe:
                ExportSQLRecipe(session, param);
                break;

            case FileType.Spell:
                ExportSQLSpell(session, param);
                break;

            case FileType.Weenie:
                ExportSQLWeenie(session, param);
                break;
        }
    }
}
