using System;
using System.Globalization;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.DeveloperCommands;

public class TeleLoc
{
    // teleloc cell x y z [qx qy qz qw]
    [CommandHandler(
        "teleloc",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        4,
        "Teleport yourself to the specified location.",
        "cell [x y z] (qw qx qy qz)\n"
            + "@teleloc follows the same number order as displayed from @loc output\n"
            + "Example: @teleloc 0x7F0401AD [12.319900 -28.482000 0.005000] -0.338946 0.000000 0.000000 -0.940806\n"
            + "Example: @teleloc 0x7F0401AD 12.319900 -28.482000 0.005000 -0.338946 0.000000 0.000000 -0.940806\n"
            + "Example: @teleloc 7F0401AD 12.319900 -28.482000 0.005000"
    )]
    public static void HandleTeleportLOC(Session session, params string[] parameters)
    {
        try
        {
            uint cell;

            if (parameters[0].StartsWith("0x"))
            {
                var strippedcell = parameters[0].Substring(2);
                cell = (uint)int.Parse(strippedcell, NumberStyles.HexNumber);
            }
            else
            {
                cell = (uint)int.Parse(parameters[0], NumberStyles.HexNumber);
            }

            var positionData = new float[7];
            for (var i = 0u; i < 7u; i++)
            {
                if (i > 2 && parameters.Length < 8)
                {
                    positionData[3] = 1;
                    positionData[4] = 0;
                    positionData[5] = 0;
                    positionData[6] = 0;
                    break;
                }

                if (!float.TryParse(parameters[i + 1].Trim(new Char[] { ' ', '[', ']' }), out var position))
                {
                    return;
                }

                positionData[i] = position;
            }

            session.Player.Teleport(
                new Position(
                    cell,
                    positionData[0],
                    positionData[1],
                    positionData[2],
                    positionData[4],
                    positionData[5],
                    positionData[6],
                    positionData[3]
                )
            );
        }
        catch (Exception)
        {
            ChatPacket.SendServerMessage(session, "Invalid arguments for @teleloc", ChatMessageType.Broadcast);
            ChatPacket.SendServerMessage(
                session,
                "Hint: @teleloc follows the same number order as displayed from @loc output",
                ChatMessageType.Broadcast
            );
            ChatPacket.SendServerMessage(
                session,
                "Usage: @teleloc cell [x y z] (qw qx qy qz)",
                ChatMessageType.Broadcast
            );
            ChatPacket.SendServerMessage(
                session,
                "Example: @teleloc 0x7F0401AD [12.319900 -28.482000 0.005000] -0.338946 0.000000 0.000000 -0.940806",
                ChatMessageType.Broadcast
            );
            ChatPacket.SendServerMessage(
                session,
                "Example: @teleloc 0x7F0401AD 12.319900 -28.482000 0.005000 -0.338946 0.000000 0.000000 -0.940806",
                ChatMessageType.Broadcast
            );
            ChatPacket.SendServerMessage(
                session,
                "Example: @teleloc 7F0401AD 12.319900 -28.482000 0.005000",
                ChatMessageType.Broadcast
            );
        }
    }
}
