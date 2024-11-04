using System;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.WorldObjects;

namespace ACE.Server.Commands.AdminCommands;

public class ModifyAttribute
{
    [CommandHandler(
        "modifyattr",
        AccessLevel.Admin,
        CommandHandlerFlag.None,
        2,
        "Adjusts an attribute for the last appraised mob/NPC/player",
        "<attribute> <delta>"
    )]
    public static void HandleModifyAttribute(Session session, params string[] parameters)
    {
        var lastAppraised = CommandHandlerHelper.GetLastAppraisedObject(session);
        if (lastAppraised == null || !(lastAppraised is Creature))
        {
            ChatPacket.SendServerMessage(
                session,
                "The last appraised object was not a mob/NPC/player.",
                ChatMessageType.Broadcast
            );
            return;
        }
        var creature = lastAppraised as Creature;

        if (parameters.Length < 2)
        {
            ChatPacket.SendServerMessage(
                session,
                "Usage: modifyattr <attribute> <delta>: missing attribute name and/or delta",
                ChatMessageType.Broadcast
            );
            return;
        }
        if (!Enum.TryParse(parameters[0], out PropertyAttribute attrType))
        {
            ChatPacket.SendServerMessage(
                session,
                "Invalid skillName, must be a valid skill name (without spaces, with capitalization), valid values are: Strength,Endurance,Coordination,Quickness,Focus,Self",
                ChatMessageType.Broadcast
            );
            return;
        }
        if (!Int32.TryParse(parameters[1], out var delta))
        {
            ChatPacket.SendServerMessage(session, "Invalid delta, must be a valid integer", ChatMessageType.Broadcast);
            return;
        }

        var attr = creature.Attributes[attrType];
        attr.StartingValue = (uint)Math.Clamp(attr.StartingValue + delta, 1, 9999);

        if (creature is Player || creature.IsDynamicThatShouldPersistToShard())
        {
            creature.SaveBiotaToDatabase();
        }
        if (creature is Player)
        {
            var player = creature as Player;
            player.Session.Network.EnqueueSend(new GameMessagePrivateUpdateAttribute(player, attr));
        }
    }
}
