using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Managers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.WorldObjects;
using Serilog;

namespace ACE.Server.Commands.EnvoyCommands;

public class Delete
{
    private static readonly ILogger _log = Log.ForContext(typeof(Delete));

    // delete
    [CommandHandler(
        "delete",
        AccessLevel.Envoy,
        CommandHandlerFlag.RequiresWorld,
        0,
        "Deletes the selected object.",
        "Players may not be deleted this way."
    )]
    public static void HandleDeleteSelected(Session session, params string[] parameters)
    {
        // @delete - Deletes the selected object. Players may not be deleted this way.

        var objectId = ObjectGuid.Invalid;

        if (session.Player.HealthQueryTarget.HasValue)
        {
            objectId = new ObjectGuid(session.Player.HealthQueryTarget.Value);
        }
        else if (session.Player.ManaQueryTarget.HasValue)
        {
            objectId = new ObjectGuid(session.Player.ManaQueryTarget.Value);
        }
        else if (session.Player.CurrentAppraisalTarget.HasValue)
        {
            objectId = new ObjectGuid(session.Player.CurrentAppraisalTarget.Value);
        }

        if (objectId == ObjectGuid.Invalid)
        {
            ChatPacket.SendServerMessage(
                session,
                "Delete failed. Please identify the object you wish to delete first.",
                ChatMessageType.Broadcast
            );
        }

        if (objectId.IsPlayer())
        {
            ChatPacket.SendServerMessage(
                session,
                "Delete failed. Players cannot be deleted.",
                ChatMessageType.Broadcast
            );
            return;
        }

        var wo = session.Player.FindObject(
            objectId.Full,
            Player.SearchLocations.Everywhere,
            out _,
            out var rootOwner,
            out var wasEquipped
        );

        if (wo == null)
        {
            ChatPacket.SendServerMessage(session, "Delete failed. Object not found.", ChatMessageType.Broadcast);
            return;
        }

        if (parameters.Length == 1)
        {
            var objectType = parameters[0].ToLower();

            if (objectType != wo.GetType().Name.ToLower() && objectType != wo.WeenieType.ToString().ToLower())
            {
                ChatPacket.SendServerMessage(
                    session,
                    $"Delete failed. Object type specified ({parameters[0]}) does not match object type ({wo.GetType().Name}) or weenie type ({wo.WeenieType.ToString()}) for 0x{wo.Guid}:{wo.Name}.",
                    ChatMessageType.Broadcast
                );
                return;
            }
        }

        wo.DeleteObject(rootOwner);
        session.Network.EnqueueSend(new GameMessageDeleteObject(wo));

        PlayerManager.BroadcastToAuditChannel(
            session.Player,
            $"{session.Player.Name} has deleted 0x{wo.Guid}:{wo.Name}"
        );
    }
}
