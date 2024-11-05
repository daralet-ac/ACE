using System;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using static ACE.Server.Commands.DeveloperCommands.ContentCommands.ContentCommandUtilities;

namespace ACE.Server.Commands.DeveloperCommands.ContentCommands;

public class ImportSql
{
    [CommandHandler(
        "import-sql",
        AccessLevel.Developer,
        CommandHandlerFlag.None,
        1,
        "Imports sql data from the Content folder",
        "<type> <wcid>\n<type> - landblock, quest, recipe, spell, weenie (default if not specified)\n<wcid> - filename prefix to search for. can be 'all' to import all files for this content type"
    )]
    public static void HandleImportSQL(Session session, params string[] parameters)
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
        try
        {
            switch (contentType)
            {
                case FileType.LandblockInstance:
                    ImportSQLLandblock(session, param);
                    break;

                case FileType.Quest:
                    ImportSQLQuest(session, param);
                    break;

                case FileType.Recipe:
                    ImportSQLRecipe(session, param);
                    break;

                case FileType.Spell:
                    ImportSQLSpell(session, param);
                    break;

                case FileType.Weenie:
                    ImportSQLWeenie(session, param);
                    break;
            }
        }
        catch (Exception e)
        {
            CommandHandlerHelper.WriteOutputError(session, $"There was an error importing the SQL:\n\n{e.Message}");
        }
    }
}
