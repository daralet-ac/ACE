using System;
using ACE.Entity.Enum;
using ACE.Entity.Models;
using ACE.Server.Commands.Handlers;
using ACE.Server.Entity;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.Commands.DeveloperCommands.WorldObjectCommands;

public class AddItemSpell
{
    /// <summary>
    /// This is to add spells to items (whether loot or quest generated).  For making weapons to check damage from pcaps or other sources
    /// </summary>
    [CommandHandler(
        "additemspell",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        1,
        "Adds a spell to the last appraised item's spellbook.",
        "<spell id>"
    )]
    public static void HandleAddItemSpell(Session session, params string[] parameters)
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

        obj.Biota.GetOrAddKnownSpell((int)spellId, obj.BiotaDatabaseLock, out var spellAdded);

        var msg = spellAdded ? "added to" : "already on";

        session.Network.EnqueueSend(
            new GameMessageSystemChat($"{spell.Name} ({spell.Id}) {msg} {obj.Name}", ChatMessageType.Broadcast)
        );
    }
}
