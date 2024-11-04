using System;
using ACE.DatLoader;
using ACE.DatLoader.FileTypes;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.AdminCommands.ConsoleCommands;

public class ExportWave
{
    /// <summary>
    /// Export all wav files to a specific directory.
    /// </summary>
    [CommandHandler("wave-export", AccessLevel.Admin, CommandHandlerFlag.ConsoleInvoke, 0, "Export Wave Files")]
    public static void ExportWaveFiles(Session session, params string[] parameters)
    {
        if (parameters?.Length != 1)
        {
            Console.WriteLine("wave-export <export-directory-without-spaces>");
            return;
        }

        var exportDir = parameters[0];

        Console.WriteLine($"Exporting portal.dat WAV files to {exportDir}.  This may take a while.");
        foreach (var entry in DatManager.PortalDat.AllFiles)
        {
            if (entry.Value.GetFileType(DatDatabaseType.Portal) == DatFileType.Wave)
            {
                var wave = DatManager.PortalDat.ReadFromDat<Wave>(entry.Value.ObjectId);

                wave.ExportWave(exportDir);
            }
        }
        Console.WriteLine($"Export to {exportDir} complete.");
    }
}
