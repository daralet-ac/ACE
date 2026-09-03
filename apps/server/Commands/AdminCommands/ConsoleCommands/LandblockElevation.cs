using System;
using System.Globalization;
using System.IO;
using System.Linq;
using ACE.DatLoader;
using ACE.DatLoader.FileTypes;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.AdminCommands.ConsoleCommands;

public class LandblockElevation
{
    [CommandHandler(
        "landblock-elevation",
        AccessLevel.Admin,
        CommandHandlerFlag.ConsoleInvoke,
        2,
        "Computes average/min/max terrain elevation (Z) for a list of landblocks, reading terrain heights directly from the loaded cell/portal dat files.",
        "<input-file-of-4hex-landblock-ids> <output-csv-file>"
    )]
    public static void ComputeLandblockElevation(Session session, params string[] parameters)
    {
        if (parameters?.Length != 2)
        {
            Console.WriteLine("landblock-elevation <input-file-of-4hex-landblock-ids> <output-csv-file>");
            return;
        }

        var inputFile = parameters[0];
        var outputFile = parameters[1];

        if (!File.Exists(inputFile))
        {
            Console.WriteLine($"Input file not found: {inputFile}");
            return;
        }

        var regionDesc = DatManager.PortalDat.ReadFromDat<RegionDesc>(0x13000000);
        var landHeightTable = regionDesc.LandDefs.LandHeightTable;

        var results = new System.Collections.Generic.List<string> { "Landblock,AvgZ,MinZ,MaxZ" };

        var lines = File.ReadAllLines(inputFile);
        var processed = 0;
        var errors = 0;

        foreach (var rawLine in lines)
        {
            var hex = rawLine.Trim();
            if (string.IsNullOrEmpty(hex))
            {
                continue;
            }

            if (!uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var landblockId))
            {
                results.Add($"{hex},ERROR,ERROR,ERROR");
                errors++;
                continue;
            }

            try
            {
                var fileId = (landblockId << 16) | 0xFFFF;
                var cellLandblock = DatManager.CellDat.ReadFromDat<CellLandblock>(fileId);

                var heights = cellLandblock.Height.Select(h => landHeightTable[h]).ToList();

                var avg = heights.Average();
                var min = heights.Min();
                var max = heights.Max();

                results.Add(
                    $"{hex},{avg.ToString("F2", CultureInfo.InvariantCulture)},{min.ToString("F2", CultureInfo.InvariantCulture)},{max.ToString("F2", CultureInfo.InvariantCulture)}"
                );
                processed++;
            }
            catch (Exception ex)
            {
                results.Add($"{hex},ERROR,ERROR,{ex.Message}");
                errors++;
            }
        }

        File.WriteAllLines(outputFile, results);

        Console.WriteLine($"Processed {processed} landblocks ({errors} errors). Wrote results to {outputFile}.");
    }
}
