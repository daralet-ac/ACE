using System.Globalization;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.Physics.Managers;
using ACE.Server.WorldObjects;

namespace ACE.Server.Commands.DeveloperCommands;

public class TargetLocation
{
    [CommandHandler(
        "targetloc",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        "Shows the location of the last appraised object"
    )]
    public static void HandleTargetLoc(Session session, params string[] parameters)
    {
        WorldObject wo = null;
        if (parameters.Length == 0)
        {
            wo = CommandHandlerHelper.GetLastAppraisedObject(session);

            if (wo == null)
            {
                return;
            }
        }
        else
        {
            if (parameters[0].StartsWith("0x"))
            {
                parameters[0] = parameters[0].Substring(2);
            }

            if (!uint.TryParse(parameters[0], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var guid))
            {
                session.Network.EnqueueSend(
                    new GameMessageSystemChat($"{parameters[0]} is not a valid guid", ChatMessageType.Broadcast)
                );
                return;
            }

            wo = session.Player.CurrentLandblock?.GetObject(guid);

            if (wo == null)
            {
                wo = ServerObjectManager.GetObjectA(guid)?.WeenieObj?.WorldObject;
            }

            if (wo == null)
            {
                session.Network.EnqueueSend(
                    new GameMessageSystemChat($"Couldn't find {parameters[0]}", ChatMessageType.Broadcast)
                );
                return;
            }
        }

        session.Network.EnqueueSend(
            new GameMessageSystemChat(
                $"CurrentLandblock: 0x{wo.CurrentLandblock?.Id.Landblock:X4}",
                ChatMessageType.Broadcast
            )
        );
        session.Network.EnqueueSend(
            new GameMessageSystemChat($"Location: {wo.Location?.ToLOCString()}", ChatMessageType.Broadcast)
        );
        session.Network.EnqueueSend(
            new GameMessageSystemChat($"Physics : {wo.PhysicsObj?.Position}", ChatMessageType.Broadcast)
        );
        session.Network.EnqueueSend(
            new GameMessageSystemChat($"CurCell: 0x{wo.PhysicsObj?.CurCell?.ID:X8}", ChatMessageType.Broadcast)
        );
    }
}
