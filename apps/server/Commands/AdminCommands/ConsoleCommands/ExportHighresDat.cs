using System;
using ACE.DatLoader;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.AdminCommands.ConsoleCommands;

public class ExportHighresDat
{
    [CommandHandler(
        "highres-export",
        AccessLevel.Admin,
        CommandHandlerFlag.ConsoleInvoke,
        1,
        "Export contents of client_highres.dat file.",
        "<export-directory-without-spaces>"
    )]
    public static void ExportHighresDatContents(Session session, params string[] parameters)
    {
        if (DatManager.HighResDat == null)
        {
            Console.WriteLine("client_highres.dat file was not loaded.");
            return;
        }
        if (parameters?.Length != 1)
        {
            Console.WriteLine("highres-export <export-directory-without-spaces>");
        }

        var exportDir = parameters[0];

        Console.WriteLine($"Exporting client_highres.dat contents to {exportDir}.  This will take a while.");
        DatManager.HighResDat.ExtractCategorizedPortalContents(exportDir);
        Console.WriteLine($"Export of client_highres.dat to {exportDir} complete.");
    }
}
