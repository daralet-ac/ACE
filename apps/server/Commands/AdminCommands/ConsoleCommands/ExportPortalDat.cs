using System;
using ACE.DatLoader;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.AdminCommands.ConsoleCommands;

public class ExportPortalDat
{
    [CommandHandler(
        "portal-export",
        AccessLevel.Admin,
        CommandHandlerFlag.ConsoleInvoke,
        1,
        "Export contents of PORTAL DAT file.",
        "<export-directory-without-spaces>"
    )]
    public static void ExportPortalDatContents(Session session, params string[] parameters)
    {
        if (parameters?.Length != 1)
        {
            Console.WriteLine("portal-export <export-directory-without-spaces>");
        }

        var exportDir = parameters[0];

        Console.WriteLine($"Exporting portal.dat contents to {exportDir}.  This will take a while.");
        DatManager.PortalDat.ExtractCategorizedPortalContents(exportDir);
        Console.WriteLine($"Export of portal.dat to {exportDir} complete.");
    }
}
