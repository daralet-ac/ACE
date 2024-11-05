using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.WorldObjects;

namespace ACE.Server.Commands.EnvoyCommands;

public class Heal
{
    // heal
    [CommandHandler(
        "heal",
        AccessLevel.Envoy,
        CommandHandlerFlag.RequiresWorld,
        0,
        "Heals yourself (or the selected creature)",
        "\n" + "This command fully restores your(or the selected creature's) health, mana, and stamina"
    )]
    public static void HandleHeal(Session session, params string[] parameters)
    {
        // usage: @heal
        // This command fully restores your(or the selected creature's) health, mana, and stamina.
        // @heal - Heals yourself(or the selected creature).

        var objectId = ObjectGuid.Invalid;

        if (session.Player.HealthQueryTarget.HasValue)
        {
            objectId = new ObjectGuid((uint)session.Player.HealthQueryTarget);
        }
        else if (session.Player.ManaQueryTarget.HasValue)
        {
            objectId = new ObjectGuid((uint)session.Player.ManaQueryTarget);
        }
        else if (session.Player.CurrentAppraisalTarget.HasValue)
        {
            objectId = new ObjectGuid((uint)session.Player.CurrentAppraisalTarget);
        }

        if (objectId == ObjectGuid.Invalid)
        {
            objectId = session.Player.Guid;
        }

        var wo = session.Player.CurrentLandblock?.GetObject(objectId);

        if (wo is null)
        {
            session.Network.EnqueueSend(
                new GameMessageSystemChat($"Unable to locate what you have selected.", ChatMessageType.Broadcast)
            );
        }
        else if (wo is Player player)
        {
            player.SetMaxVitals();
        }
        else
        {
            session.Network.EnqueueSend(
                new GameMessageSystemChat(
                    $"You cannot heal {wo.Name} because it is not a player.",
                    ChatMessageType.Broadcast
                )
            );
        }
    }
}
