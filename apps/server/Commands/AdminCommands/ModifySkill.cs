using System;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.WorldObjects;

namespace ACE.Server.Commands.AdminCommands;

public class ModifySkill
{
    [CommandHandler(
        "modifyskill",
        AccessLevel.Admin,
        CommandHandlerFlag.None,
        2,
        "Adjusts the skill for the last appraised mob/player",
        "<skillName> <delta>"
    )]
    public static void HandleModifySkill(Session session, params string[] parameters)
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
                "Usage: modifyskill <skillName> <delta>: missing skillId and/or delta",
                ChatMessageType.Broadcast
            );
            return;
        }
        if (!Enum.TryParse(parameters[0], out Skill skill))
        {
            var names = Enum.GetNames(typeof(Skill));
            ChatPacket.SendServerMessage(
                session,
                "Invalid skillName, must be a valid skill name (without spaces, with capitalization), valid values are: "
                    + String.Join(", ", names),
                ChatMessageType.Broadcast
            );
            return;
        }
        if (!Int32.TryParse(parameters[1], out var delta))
        {
            ChatPacket.SendServerMessage(session, "Invalid delta, must be a valid integer", ChatMessageType.Broadcast);
            return;
        }

        var creatureSkill = creature is Player ? creature.Skills[skill] : creature.GetCreatureSkill(skill);
        creatureSkill.InitLevel = (ushort)Math.Clamp(creatureSkill.InitLevel + delta, 0, (Int32)ushort.MaxValue);

        // save changes
        if (creature is Player || creature.IsDynamicThatShouldPersistToShard())
        {
            creature.SaveBiotaToDatabase();
        }
        if (creature is Player)
        {
            var player = creature as Player;
            player.Session.Network.EnqueueSend(new GameMessagePrivateUpdateSkill(player, creatureSkill));
        }
    }
}
