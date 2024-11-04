using System.Collections.Generic;
using System.Linq;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.WorldObjects;
using static ACE.Server.Commands.DeveloperCommands.DeveloperCommandUtilities;

namespace ACE.Server.Commands.DeveloperCommands;

public class ResistInfo
{
    [CommandHandler(
        "resist-info",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        "Shows the resistance info for the last appraised creature."
    )]
    public static void HandleResistInfo(Session session, params string[] parameters)
    {
        var creature = CommandHandlerHelper.GetLastAppraisedObject(session) as Creature;
        if (creature == null)
        {
            session.Network.EnqueueSend(
                new GameMessageSystemChat(
                    $"You must appraise a creature to use this command.",
                    ChatMessageType.Broadcast
                )
            );
            return;
        }

        session.Network.EnqueueSend(
            new GameMessageSystemChat($"{creature.Name} ({creature.Guid}):", ChatMessageType.Broadcast)
        );

        var resistInfo = new Dictionary<PropertyFloat, double?>();
        foreach (var prop in ResistProperties)
        {
            resistInfo.Add(prop, creature.GetProperty(prop));
        }

        foreach (var kvp in resistInfo.OrderByDescending(i => i.Value))
        {
            session.Network.EnqueueSend(
                new GameMessageSystemChat($"{kvp.Key} - {kvp.Value}", ChatMessageType.Broadcast)
            );
        }
    }
}
