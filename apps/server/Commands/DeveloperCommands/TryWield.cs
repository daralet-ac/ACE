using System;
using System.Globalization;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using ACE.Server.WorldObjects;

namespace ACE.Server.Commands.DeveloperCommands;

public class TryWield
{
    [CommandHandler("trywield", AccessLevel.Developer, CommandHandlerFlag.RequiresWorld, 2)]
    public static void HandleTryWield(Session session, params string[] parameters)
    {
        if (!uint.TryParse(parameters[0], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var itemGuid))
        {
            CommandHandlerHelper.WriteOutputInfo(
                session,
                $"Invalid item guid {parameters[0]}",
                ChatMessageType.Broadcast
            );
            return;
        }

        var item = session.Player.FindObject(itemGuid, Player.SearchLocations.MyInventory);

        if (item == null)
        {
            CommandHandlerHelper.WriteOutputInfo(
                session,
                $"Couldn't find item guid {parameters[0]}",
                ChatMessageType.Broadcast
            );
            return;
        }

        if (!Enum.TryParse(parameters[1], out EquipMask equipMask))
        {
            CommandHandlerHelper.WriteOutputInfo(
                session,
                $"Invalid EquipMask {parameters[1]}",
                ChatMessageType.Broadcast
            );
            return;
        }

        session.Player.HandleActionGetAndWieldItem(itemGuid, equipMask);
    }
}
