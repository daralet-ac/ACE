using System;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Market;
using ACE.Server.Network;
using ACE.Server.WorldObjects;

namespace ACE.Server.Commands.PlayerCommands;

public static class Market
{
    [CommandHandler("market", AccessLevel.Player, CommandHandlerFlag.None, true, -1, "Market commands", "market <subcommand> [...]")]
    public static void HandleMarket(Session session, string[] parameters)
    {
        var player = session.Player;
        if (player == null)
        {
            return;
        }

        if (parameters.Length == 0)
        {
            player.SendMessage("Usage: /market listings | payouts | price <amount>");
            return;
        }

        var cmd = parameters[0].Trim();

        if (cmd.Equals("listings", StringComparison.OrdinalIgnoreCase))
        {
            var listings = MarketServiceLocator.PlayerMarketRepository.GetListingsForAccount(
                player.Character.AccountId,
                DateTime.UtcNow);
            var count = 0;
            foreach (var _ in listings)
            {
                count++;
            }

            player.SendMessage($"You have {count} active listing(s).");
            return;
        }

        if (cmd.Equals("payouts", StringComparison.OrdinalIgnoreCase))
        {
            var payouts = MarketServiceLocator.PlayerMarketRepository.GetPendingPayouts(player.Character.AccountId);
            var count = 0;
            foreach (var _ in payouts)
            {
                count++;
            }

            player.SendMessage($"You have {count} pending payout(s).");
            return;
        }

        if (cmd.Equals("price", StringComparison.OrdinalIgnoreCase))
        {
            if (parameters.Length < 2 || !int.TryParse(parameters[1], out var price) || price <= 0)
            {
                player.SendMessage("Usage: /market price <amount>");
                return;
            }

            if (!MarketBroker.TryGetPendingItemGuid(player, out var itemGuid))
            {
                player.SendMessage("No pending item. Give an item to a Market Broker first.");
                return;
            }

            var item = player.FindObject(itemGuid, Player.SearchLocations.MyInventory | Player.SearchLocations.MyEquippedItems, out _, out _, out _);
            if (item == null)
            {
                MarketBroker.ClearPendingItem(player);
                player.SendMessage("Pending item not found. Give it to a Market Broker again.");
                return;
            }

            if (!MarketBroker.IsSellable(item, out var reason))
            {
                MarketBroker.ClearPendingItem(player);
                player.SendMessage($"Cannot list: {reason}");
                return;
            }

            // Determine vendor tier bucket from player level
            var vendorTier = player.GetPlayerTier(player.Level);

            var listing = MarketServiceLocator.PlayerMarketRepository.CreateListingFromWorldObject(
                player,
                item,
                price,
                MarketCurrencyType.TradeNote,
                vendorTier,
                null,
                null,
                $"Listed by {player.Name}");

            // remove item from player inventory (server-side)
            // TODO: implement safe item removal/transfer once listing flow finalized.
            MarketBroker.ClearPendingItem(player);
            player.SendMessage($"Listed {item.Name} for {price}. (Item removal not implemented yet)");

            return;
        }

        player.SendMessage("Usage: /market listings | payouts | price <amount>");
    }
}
