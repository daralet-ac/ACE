using System;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.Commands.DeveloperCommands;

public class RecordCast
{
    [CommandHandler(
        "recordcast",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        "Records spell casting keypresses to server for debugging"
    )]
    public static void HandleRecordCast(Session session, params string[] parameters)
    {
        if (parameters.Length == 0)
        {
            session.Player.RecordCast.Enabled = !session.Player.RecordCast.Enabled;
        }
        else
        {
            if (parameters[0].Equals("on", StringComparison.OrdinalIgnoreCase))
            {
                session.Player.RecordCast.Enabled = true;
            }
            else
            {
                session.Player.RecordCast.Enabled = false;
            }
        }
        session.Network.EnqueueSend(
            new GameMessageSystemChat(
                $"Record cast {(session.Player.RecordCast.Enabled ? "enabled" : "disabled")}",
                ChatMessageType.Broadcast
            )
        );
    }
}
