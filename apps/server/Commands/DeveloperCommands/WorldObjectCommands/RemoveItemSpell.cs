using System;
using ACE.Entity.Enum;
using ACE.Entity.Models;
using ACE.Server.Commands.Handlers;
using ACE.Server.Entity;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.Commands.DeveloperCommands.WorldObjectCommands;

public class RemoveItemSpell
{
    [CommandHandler(
        "removeitemspell",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        1,
        "Removes a spell to the last appraised item's spellbook.",
        "<spell id>"
    )]
    public static void HandleRemoveItemSpell(Session session, params string[] parameters)
    {
        var obj = CommandHandlerHelper.GetLastAppraisedObject(session);
        if (obj == null)
        {
            return;
        }

        if (!Enum.TryParse(parameters[0], true, out SpellId spellId))
        {
            session.Network.EnqueueSend(
                new GameMessageSystemChat($"{parameters[0]} is not a valid spell id", ChatMessageType.Broadcast)
            );
            return;
        }

        // ensure valid spell id
        var spell = new Spell(spellId);

        if (spell.NotFound)
        {
            session.Network.EnqueueSend(new GameMessageSystemChat("SpellID is not found", ChatMessageType.Broadcast));
            return;
        }

        var spellRemoved = obj.Biota.TryRemoveKnownSpell((int)spellId, obj.BiotaDatabaseLock);

        var msg = spellRemoved ? "removed from" : "not found on";

        session.Network.EnqueueSend(
            new GameMessageSystemChat($"{spell.Name} ({spell.Id}) {msg} {obj.Name}", ChatMessageType.Broadcast)
        );
    }
}
