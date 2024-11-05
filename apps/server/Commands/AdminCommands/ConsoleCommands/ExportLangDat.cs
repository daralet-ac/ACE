using System;
using ACE.DatLoader;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.AdminCommands.ConsoleCommands;

public class ExportLangDat
{
    [CommandHandler(
        "language-export",
        AccessLevel.Admin,
        CommandHandlerFlag.ConsoleInvoke,
        1,
        "Export contents of client_local_English.dat file.",
        "<export-directory-without-spaces>"
    )]
    public static void ExportLanguageDatContents(Session session, params string[] parameters)
    {
        if (DatManager.LanguageDat == null)
        {
            Console.WriteLine("client_highres.dat file was not loaded.");
            return;
        }
        if (parameters?.Length != 1)
        {
            Console.WriteLine("language-export <export-directory-without-spaces>");
        }

        var exportDir = parameters[0];

        Console.WriteLine($"Exporting client_local_English.dat contents to {exportDir}.  This will take a while.");
        DatManager.LanguageDat.ExtractCategorizedPortalContents(exportDir);
        Console.WriteLine($"Export of client_local_English.dat to {exportDir} complete.");
    }
}
