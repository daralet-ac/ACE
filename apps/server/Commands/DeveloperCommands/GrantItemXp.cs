using System.Linq;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.WorldObjects;

namespace ACE.Server.Commands.DeveloperCommands;

public class GrantItemXp
{
    [CommandHandler(
        "grantitemxp",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        1,
        "Give item XP to the last appraised item."
    )]
    public static void HandleGrantItemXp(Session session, params string[] parameters)
    {
        if (!long.TryParse(parameters[0], out var amount))
        {
            session.Network.EnqueueSend(
                new GameMessageSystemChat($"Invalid amount {parameters[0]}", ChatMessageType.Broadcast)
            );
            return;
        }

        var item = CommandHandlerHelper.GetLastAppraisedObject(session);
        if (item == null)
        {
            return;
        }

        if (item is Player player)
        {
            player.GrantItemXP(amount);

            foreach (var i in player.EquippedObjects.Values.Where(i => i.HasItemLevel))
            {
                session.Network.EnqueueSend(
                    new GameMessageSystemChat($"{amount:N0} experience granted to {i.Name}.", ChatMessageType.Broadcast)
                );
            }
        }
        else
        {
            if (item.HasItemLevel)
            {
                session.Player.GrantItemXP(item, amount);

                session.Network.EnqueueSend(
                    new GameMessageSystemChat(
                        $"{amount:N0} experience granted to {item.Name}.",
                        ChatMessageType.Broadcast
                    )
                );
            }
            else
            {
                session.Network.EnqueueSend(
                    new GameMessageSystemChat($"{item.Name} is not a levelable item.", ChatMessageType.Broadcast)
                );
            }
        }
    }
}
