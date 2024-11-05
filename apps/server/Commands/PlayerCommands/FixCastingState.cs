using System;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using ACE.Server.Network.GameEvent.Events;

namespace ACE.Server.Commands.PlayerCommands;

public class FixCastingState
{
    [CommandHandler(
        "fixcast",
        AccessLevel.Player,
        CommandHandlerFlag.RequiresWorld,
        "Fixes magic casting if locked up for an extended time"
    )]
    public static void HandleFixCast(Session session, params string[] parameters)
    {
        var magicState = session.Player.MagicState;

        if (magicState.IsCasting && DateTime.UtcNow - magicState.StartTime > TimeSpan.FromSeconds(5))
        {
            session.Network.EnqueueSend(new GameEventCommunicationTransientString(session, "Fixed casting state"));
            session.Player.SendUseDoneEvent();
            magicState.OnCastDone();
        }
    }
}
