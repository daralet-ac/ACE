using System;
using ACE.DatLoader;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.AdminCommands.ConsoleCommands;

public class ExportCellDat
{
    [CommandHandler(
        "cell-export",
        AccessLevel.Admin,
        CommandHandlerFlag.ConsoleInvoke,
        1,
        "Export contents of CELL DAT file.",
        "<export-directory-without-spaces>"
    )]
    public static void ExportCellDatContents(Session session, params string[] parameters)
    {
        if (parameters?.Length != 1)
        {
            Console.WriteLine("cell-export <export-directory-without-spaces>");
        }

        var exportDir = parameters[0];

        Console.WriteLine($"Exporting cell.dat contents to {exportDir}.  This can take longer than an hour.");
        DatManager.CellDat.ExtractLandblockContents(exportDir);
        Console.WriteLine($"Export of cell.dat to {exportDir} complete.");
    }
}
