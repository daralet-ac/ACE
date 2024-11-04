using System;
using ACE.DatLoader;
using ACE.DatLoader.FileTypes;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.AdminCommands.ConsoleCommands;

public class ExportImage
{
    /// <summary>
    /// Export all texture/image files to a specific directory.
    /// </summary>
    [CommandHandler(
        "image-export",
        AccessLevel.Admin,
        CommandHandlerFlag.ConsoleInvoke,
        0,
        "Export Texture/Image Files"
    )]
    public static void ExportImageFile(Session session, params string[] parameters)
    {
        var syntax = "image-export <export-directory-without-spaces> [id]";
        if (parameters?.Length < 1)
        {
            Console.WriteLine(syntax);
            return;
        }

        var exportDir = parameters[0];
        if (exportDir.Length == 0 || !System.IO.Directory.Exists(exportDir))
        {
            Console.WriteLine(syntax);
            return;
        }

        if (parameters.Length > 1)
        {
            uint imageId;
            if (parameters[1].StartsWith("0x"))
            {
                var hex = parameters[1].Substring(2);
                if (
                    !uint.TryParse(
                        hex,
                        System.Globalization.NumberStyles.HexNumber,
                        System.Globalization.CultureInfo.CurrentCulture,
                        out imageId
                    )
                )
                {
                    Console.WriteLine(syntax);
                    return;
                }
            }
            else if (!uint.TryParse(parameters[1], out imageId))
            {
                Console.WriteLine(syntax);
                return;
            }

            var image = DatManager.PortalDat.ReadFromDat<Texture>(imageId);
            image.ExportTexture(exportDir);

            Console.WriteLine($"Exported " + imageId.ToString("X8") + " to " + exportDir + ".");
        }
        else
        {
            var portalFiles = 0;
            var highresFiles = 0;
            Console.WriteLine(
                $"Exporting client_portal.dat textures and images to {exportDir}.  This may take a while."
            );
            foreach (var entry in DatManager.PortalDat.AllFiles)
            {
                if (entry.Value.GetFileType(DatDatabaseType.Portal) == DatFileType.Texture)
                {
                    var image = DatManager.PortalDat.ReadFromDat<Texture>(entry.Value.ObjectId);
                    image.ExportTexture(exportDir);
                    portalFiles++;
                }
            }
            Console.WriteLine($"Exported {portalFiles} total files from client_portal.dat to {exportDir}.");

            if (DatManager.HighResDat != null)
            {
                foreach (var entry in DatManager.HighResDat.AllFiles)
                {
                    if (entry.Value.GetFileType(DatDatabaseType.Portal) == DatFileType.Texture)
                    {
                        var image = DatManager.HighResDat.ReadFromDat<Texture>(entry.Value.ObjectId);
                        image.ExportTexture(exportDir);
                        highresFiles++;
                    }
                }
                Console.WriteLine($"Exported {highresFiles} total files from client_highres.dat to {exportDir}.");
            }
            var totalFiles = portalFiles + highresFiles;
            Console.WriteLine($"Exported {totalFiles} total files to {exportDir}.");
        }
    }
}
