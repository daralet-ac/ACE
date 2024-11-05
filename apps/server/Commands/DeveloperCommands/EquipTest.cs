using System;
using System.Globalization;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.DeveloperCommands;

public class EquipTest
{
    /// <summary>
    /// Debug command to test the ObjDescEvent message.
    /// </summary>
    [CommandHandler(
        "equiptest",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        "Simulates equipping a new item to your character, replacing all other items."
    )]
    public static void HandleEquipTest(Session session, params string[] parameters)
    {
        if (!(parameters?.Length > 0))
        {
            ChatPacket.SendServerMessage(
                session,
                "Usage: @equiptest (hex)clothingTableId [palette_index] [shade].\neg '@equiptest 0x100005fd'",
                ChatMessageType.Broadcast
            );
            return;
        }

        try
        {
            uint modelId;
            if (parameters[0].StartsWith("0x"))
            {
                var strippedmodelid = parameters[0].Substring(2);
                modelId = UInt32.Parse(strippedmodelid, NumberStyles.HexNumber);
            }
            else
            {
                modelId = UInt32.Parse(parameters[0], NumberStyles.HexNumber);
            }

            var palOption = -1;
            if (parameters.Length > 1)
            {
                palOption = Int32.Parse(parameters[1]);
            }

            float shade = 0;
            if (parameters.Length > 2)
            {
                shade = Single.Parse(parameters[2]);
            }

            if (shade < 0)
            {
                shade = 0;
            }

            if (shade > 1)
            {
                shade = 1;
            }

            //if ((modelId >= 0x10000001) && (modelId <= 0x1000086B))
            //    session.Player.TestWieldItem(session, modelId, palOption, shade);
            //else
            //    ChatPacket.SendServerMessage(session, "Please enter a value greater than 0x10000000 and less than 0x1000086C",
            //        ChatMessageType.Broadcast);
        }
        catch (Exception)
        {
            ChatPacket.SendServerMessage(
                session,
                "Please enter a value greater than 0x10000000 and less than 0x1000086C",
                ChatMessageType.Broadcast
            );
        }
    }
}
