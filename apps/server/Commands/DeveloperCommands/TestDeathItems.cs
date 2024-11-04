using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Entity;
using ACE.Server.Managers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.Commands.DeveloperCommands;

public class TestDeathItems
{
    [CommandHandler(
        "testdeathitems",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        0,
        "Test death item selection",
        ""
    )]
    public static void HandleTestDeathItems(Session session, params string[] parameters)
    {
        var target = session.Player;
        if (parameters.Length > 0)
        {
            target = PlayerManager.GetOnlinePlayer(parameters[0]);
            if (target == null)
            {
                session.Network.EnqueueSend(
                    new GameMessageSystemChat($"Couldn't find {parameters[0]}", ChatMessageType.Broadcast)
                );
                return;
            }
        }

        var inventory = target.GetAllPossessions();
        var sorted = new DeathItems(inventory);

        var i = 0;
        foreach (var item in sorted.Inventory)
        {
            var bonded = item.WorldObject.Bonded ?? BondedStatus.Normal;

            if (bonded != BondedStatus.Normal)
            {
                continue;
            }

            session.Network.EnqueueSend(
                new GameMessageSystemChat(
                    $"{++i}. {item.Name} ({item.Category}, AdjustedValue: {item.AdjustedValue})",
                    ChatMessageType.Broadcast
                )
            );
        }
    }
}
