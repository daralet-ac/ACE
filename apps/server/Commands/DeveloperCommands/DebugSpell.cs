using System;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.Commands.DeveloperCommands;

public class DebugSpell
{
    [CommandHandler(
        "debugspell",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        "Toggles spell projectile debugging info"
    )]
    public static void HandleDebugSpell(Session session, params string[] parameters)
    {
        if (parameters.Length == 0)
        {
            session.Player.DebugSpell = !session.Player.DebugSpell;
        }
        else
        {
            if (parameters[0].Equals("on", StringComparison.OrdinalIgnoreCase))
            {
                session.Player.DebugSpell = true;
            }
            else
            {
                session.Player.DebugSpell = false;
            }
        }
        session.Network.EnqueueSend(
            new GameMessageSystemChat(
                $"Spell projectile debugging is {(session.Player.DebugSpell ? "enabled" : "disabled")}",
                ChatMessageType.Broadcast
            )
        );
    }
}
