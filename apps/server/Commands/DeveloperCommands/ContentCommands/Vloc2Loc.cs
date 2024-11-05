using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Entity;
using ACE.Server.Network;
using static ACE.Server.Commands.DeveloperCommands.ContentCommands.ContentCommandUtilities;

namespace ACE.Server.Commands.DeveloperCommands.ContentCommands;

public class Vloc2Loc
{
    [CommandHandler(
        "vloc2loc",
        AccessLevel.Developer,
        CommandHandlerFlag.None,
        1,
        "Output a set of LOCs for a given landblock found in the VLOCS dataset",
        "<LandblockID>\nExample: @vloc2loc 0x0007\n         @vloc2loc 0xCE95"
    )]
    public static void HandleVLOCtoLOC(Session session, params string[] parameters)
    {
        var hex = parameters[0];

        if (
            hex.StartsWith("0x", StringComparison.CurrentCultureIgnoreCase)
            || hex.StartsWith("&H", StringComparison.CurrentCultureIgnoreCase)
        )
        {
            hex = hex[2..];
        }

        if (uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out var lbid))
        {
            var di = VerifyContentFolder(session);
            if (!di.Exists)
            {
                return;
            }

            var sep = Path.DirectorySeparatorChar;

            var vloc_folder = $"{di.FullName}{sep}vlocs{sep}";

            di = new DirectoryInfo(vloc_folder);

            var vlocDB = vloc_folder + "vlocDB.txt";

            var vlocs = di.Exists
                ? new FileInfo(vlocDB).Exists
                    ? File.ReadLines(vlocDB).ToArray()
                    : null
                : null;

            if (vlocs == null)
            {
                CommandHandlerHelper.WriteOutputInfo(
                    session,
                    $"Unable to read VLOC database file located here: {vlocDB}"
                );
                return;
            }

            // Name,ObjectClass,LandCell,X,Y
            // Master MacTavish,37,-114359889,97.14075000286103,-63.93749958674113

            if (vlocs.Length == 0 || !vlocs[0].Equals("Name,ObjectClass,LandCell,X,Y"))
            {
                CommandHandlerHelper.WriteOutputInfo(
                    session,
                    $"{vlocDB} does not appear to be a valid VLOC database file."
                );
                return;
            }

            var vlocFile = vloc_folder + $"{lbid:X4}.txt";

            var vi = new FileInfo(vlocFile);
            if (vi.Exists)
            {
                vi.Delete();
            }

            for (var i = 1; i < vlocs.Length; i++)
            {
                var split = vlocs[i].Split(",");

                var name = split[0].Trim();
                var objectClass = split[1].Trim();
                var strLandCell = split[2].Trim();
                var strX = split[3].Trim();
                var strY = split[4].Trim();

                if (!int.TryParse(strLandCell, out var landCell))
                {
                    CommandHandlerHelper.WriteOutputInfo(
                        session,
                        $"Unable to parse LandCell ({strLandCell}) value from line {i} in vlocDB: {vlocs[i]}"
                    );
                    continue;
                }
                var objCellId = (uint)landCell;
                if (!float.TryParse(strX, out var x))
                {
                    CommandHandlerHelper.WriteOutputInfo(
                        session,
                        $"Unable to parse X ({strX}) value from line {i} in vlocDB: {vlocs[i]}"
                    );
                    continue;
                }
                if (!float.TryParse(strY, out var y))
                {
                    CommandHandlerHelper.WriteOutputInfo(
                        session,
                        $"Unable to parse Y ({strY}) value from line {i} in vlocDB: {vlocs[i]}"
                    );
                    continue;
                }

                if ((objCellId >> 16) != lbid)
                {
                    continue;
                }

                try
                {
                    var pos = new Position(new Vector2(x, y));
                    pos.AdjustMapCoords();
                    pos.Translate(objCellId);
                    pos.FindZ();

                    using (var sw = File.AppendText(vlocFile))
                    {
                        sw.WriteLine($"{name} - @teleloc {pos.ToLOCString()}");
                    }
                }
                catch (Exception)
                {
                    using (var sw = File.AppendText(vlocFile))
                    {
                        sw.WriteLine($"Unable to parse {name} - 0x{objCellId:X8} {strX}, {strY}");
                    }
                }
            }

            vi = new FileInfo(vlocFile);
            if (vi.Exists)
            {
                CommandHandlerHelper.WriteOutputInfo(
                    session,
                    $"Successfully wrote VLOCs for 0x{lbid:X4} to {vlocFile}"
                );
            }
            else
            {
                CommandHandlerHelper.WriteOutputInfo(session, $"No VLOCs able to be written for 0x{lbid:X4}");
            }
        }
        else
        {
            CommandHandlerHelper.WriteOutputInfo(
                session,
                $"Invalid Landblock ID: {parameters[0]}\nLandblock ID should be in the hex format such as this: @vloc2loc 0xAB94"
            );
        }
    }
}
