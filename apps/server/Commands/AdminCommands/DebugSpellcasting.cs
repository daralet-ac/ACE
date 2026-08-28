using System;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.Commands.AdminCommands;

public class DebugSpellcasting
{
    [CommandHandler(
        "debug-spellcasting",
        AccessLevel.Admin,
        CommandHandlerFlag.RequiresWorld,
        "Toggles spellcasting mana cost debug info for your own casts",
        "<on/off>"
    )]
    public static void HandleDebugSpellcasting(Session session, params string[] parameters)
    {
        if (parameters.Length == 0)
        {
            session.Player.DebugSpellcasting = !session.Player.DebugSpellcasting;
        }
        else if (parameters[0].Equals("on", StringComparison.OrdinalIgnoreCase))
        {
            session.Player.DebugSpellcasting = true;
        }
        else
        {
            session.Player.DebugSpellcasting = false;
        }

        session.Network.EnqueueSend(
            new GameMessageSystemChat(
                $"Spellcasting debugging is {(session.Player.DebugSpellcasting ? "enabled" : "disabled")}",
                ChatMessageType.Broadcast
            )
        );
    }
}
