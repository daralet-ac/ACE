using System.Linq;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Entity.Actions;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.Commands.PlayerCommands;

public class Sort
{
    // pop
    [CommandHandler("sort", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, 0, "Show player main pack by WeenieType>ItemType>Name.", "")]
    public static void HandleSort(Session session, params string[] parameters)
    {
        SortBag(session);
    }

    private static void SortBag(Session session, ulong discordChannel = 0)
    {
        var player = session.Player;
        var mainPack = player.GetAllPossessions(true);

        var sortedPack =
            mainPack.OrderByDescending(x => x.WeenieType)
            .ThenByDescending(x => x.ItemType)
            .ThenByDescending(x => x.Name).ToList();

        var actionChain = new ActionChain();

        player.IsBusy = true;

        foreach (var item in sortedPack)
        {
            actionChain.AddAction(player,
                () => {
                    player.MoveItemToFirstContainerSlot(item);
                    player.EnqueueBroadcast(new GameMessageUpdateObject(item));
                }
            );

            actionChain.AddDelaySeconds(0.03);
        }

        actionChain.EnqueueChain();

        player.IsBusy = false;
    }
}
