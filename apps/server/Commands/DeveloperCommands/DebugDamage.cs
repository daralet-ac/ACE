using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.WorldObjects;

namespace ACE.Server.Commands.DeveloperCommands;

public class DebugDamage
{
    /// <summary>
    /// Toggles the display for player damage info
    /// </summary>
    [CommandHandler(
        "debugdamage",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        0,
        "Toggles the display for player damage info",
        "<attack|defense|all|on|off>"
    )]
    public static void HandleDebugDamage(Session session, params string[] parameters)
    {
        // get last appraisal creature target
        var targetCreature = CommandHandlerHelper.GetLastAppraisedObject(session) as Creature;
        if (targetCreature == null)
        {
            return;
        }

        if (parameters.Length == 0)
        {
            // toggle
            if (targetCreature.DebugDamage == Creature.DebugDamageType.None)
            {
                targetCreature.DebugDamage = Creature.DebugDamageType.All;
            }
            else
            {
                targetCreature.DebugDamage = Creature.DebugDamageType.None;
            }
        }
        else
        {
            var param = parameters[0].ToLower();
            if (param.Equals("on") || param.Equals("all"))
            {
                targetCreature.DebugDamage = Creature.DebugDamageType.All;
            }
            else if (param.Equals("off"))
            {
                targetCreature.DebugDamage = Creature.DebugDamageType.None;
            }
            else if (param.StartsWith("attack"))
            {
                targetCreature.DebugDamage = Creature.DebugDamageType.Attacker;
            }
            else if (param.StartsWith("defen"))
            {
                targetCreature.DebugDamage = Creature.DebugDamageType.Defender;
            }
            else
            {
                session.Network.EnqueueSend(
                    new GameMessageSystemChat(
                        $"DebugDamage: - unknown {param} ({targetCreature.Name})",
                        ChatMessageType.Broadcast
                    )
                );
                return;
            }
        }
        session.Network.EnqueueSend(
            new GameMessageSystemChat(
                $"DebugDamage: - {targetCreature.DebugDamage} ({targetCreature.Name})",
                ChatMessageType.Broadcast
            )
        );
        targetCreature.DebugDamageTarget = session.Player.Guid;
    }
}
