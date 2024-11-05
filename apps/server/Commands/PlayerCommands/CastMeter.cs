using System;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.Commands.PlayerCommands;

public class CastMeter
{
    [CommandHandler(
        "castmeter",
        AccessLevel.Player,
        CommandHandlerFlag.RequiresWorld,
        "Shows the fast casting efficiency meter"
    )]
    public static void HandleCastMeter(Session session, params string[] parameters)
    {
        if (parameters.Length == 0)
        {
            session.Player.MagicState.CastMeter = !session.Player.MagicState.CastMeter;
        }
        else
        {
            if (parameters[0].Equals("on", StringComparison.OrdinalIgnoreCase))
            {
                session.Player.MagicState.CastMeter = true;
            }
            else
            {
                session.Player.MagicState.CastMeter = false;
            }
        }
        session.Network.EnqueueSend(
            new GameMessageSystemChat(
                $"Cast efficiency meter {(session.Player.MagicState.CastMeter ? "enabled" : "disabled")}",
                ChatMessageType.Broadcast
            )
        );
    }
}
