using System;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Entity;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.Commands.DeveloperCommands;

public class RemoveSpell
{
    [CommandHandler(
        "removespell",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        1,
        "Removes the specified spell to your own spellbook.",
        "<spellid>"
    )]
    public static void HandleRemoveSpell(Session session, params string[] parameters)
    {
        if (!Enum.TryParse(parameters[0], true, out SpellId spellId))
        {
            session.Network.EnqueueSend(
                new GameMessageSystemChat($"Unknown spell {parameters[0]}", ChatMessageType.Broadcast)
            );
            return;
        }
        if (session.Player.RemoveKnownSpell((uint)spellId))
        {
            var spell = new Spell(spellId, false);
            session.Network.EnqueueSend(
                new GameMessageSystemChat($"{spell.Name} removed from spellbook.", ChatMessageType.Broadcast)
            );
        }
        else
        {
            session.Network.EnqueueSend(
                new GameMessageSystemChat($"You don't know that spell!", ChatMessageType.Broadcast)
            );
        }
    }
}
