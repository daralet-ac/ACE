using System.Linq;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Entity.Actions;
using ACE.Server.Network;

namespace ACE.Server.Commands.PlayerCommands;

public class Sort
{
    // sort
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
            if (item is null || item.Container is null)
            {
                continue;
            }

            actionChain.AddDelaySeconds(0.03);
            actionChain.AddAction(player,
                () => {
                    player.HandleActionPutItemInContainer(item.Guid.Full, item.Container.Guid.Full);
                }
            );
        }

        actionChain.EnqueueChain();

        player.IsBusy = false;
    }
}
