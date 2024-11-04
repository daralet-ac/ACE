using System;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.WorldObjects;
using ACE.Server.WorldObjects.Entity;

namespace ACE.Server.Commands.AdminCommands;

public class ModifyVital
{
    [CommandHandler(
        "modifyvital",
        AccessLevel.Admin,
        CommandHandlerFlag.None,
        2,
        "Adjusts the maximum vital attribute for the last appraised mob/player and restores full vitals",
        "<Health|Stamina|Mana> <delta>"
    )]
    public static void HandleModifyVital(Session session, params string[] parameters)
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
                "Usage: modifyvital <Invalid vital type, valid values are: Health,Stamina,Mana",
                ChatMessageType.Broadcast
            );
            return;
        }

        // determine the vital type
        if (!Enum.TryParse(parameters[0], out PropertyAttribute2nd vitalAttr))
        {
            ChatPacket.SendServerMessage(
                session,
                "Invalid vital type, valid values are: Health,Stamina,Mana",
                ChatMessageType.Broadcast
            );
            return;
        }

        if (!Int32.TryParse(parameters[1], out var delta))
        {
            ChatPacket.SendServerMessage(
                session,
                "Invalid vital value, values must be valid integers",
                ChatMessageType.Broadcast
            );
            return;
        }

        PropertyAttribute2nd maxAttr;
        switch (vitalAttr)
        {
            case PropertyAttribute2nd.Health:
                maxAttr = PropertyAttribute2nd.MaxHealth;
                break;
            case PropertyAttribute2nd.Stamina:
                maxAttr = PropertyAttribute2nd.MaxStamina;
                break;
            case PropertyAttribute2nd.Mana:
                maxAttr = PropertyAttribute2nd.MaxMana;
                break;
            default:
                ChatPacket.SendServerMessage(
                    session,
                    "Unexpected vital type, valid values are: Health,Stamina,Mana",
                    ChatMessageType.Broadcast
                );
                return;
        }

        var maxVital = new CreatureVital(creature, maxAttr);
        maxVital.Ranks = (uint)Math.Clamp(maxVital.Ranks + delta, 1, uint.MaxValue);
        creature.UpdateVital(maxVital, maxVital.MaxValue);

        var vital = new CreatureVital(creature, vitalAttr);
        vital.Ranks = (uint)Math.Clamp(vital.Ranks + delta, 1, uint.MaxValue);
        creature.UpdateVital(vital, maxVital.MaxValue);

        if (creature is Player)
        {
            var player = creature as Player;
            player.Session.Network.EnqueueSend(new GameMessagePrivateUpdateVital(player, maxVital));
        }

        creature.SetMaxVitals();

        // save changes
        if (creature is Player || creature.IsDynamicThatShouldPersistToShard())
        {
            creature.SaveBiotaToDatabase();
        }
    }
}
