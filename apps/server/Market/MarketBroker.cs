using System;
using System.Linq;
using System.Text;
using ACE.Database;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Entity;
using ACE.Server.Factories;
using ACE.Server.Network.GameEvent.Events;
using ACE.Server.WorldObjects;

namespace ACE.Server.Market;

public static class MarketBroker
{
    public const string TemplateName = "Market Broker";

    private static string? TryGetListingItemName(ACE.Database.Models.Shard.PlayerMarketListing listing)
    {
        if (listing == null)
        {
            return null;
        }

        // Prefer the exact persisted item name, if the biota still exists.
        if (listing.ItemBiotaId > 0)
        {
            try
            {
                var biota = DatabaseManager.Shard.BaseDatabase.GetBiota(listing.ItemBiotaId, true);
                var name = biota?.BiotaPropertiesString?.FirstOrDefault(p => p.Type == (ushort)PropertyString.Name)?.Value;
                if (!string.IsNullOrWhiteSpace(name))
                {
                    return name;
                }
            }
            catch
            {
                // ignore; fall back to weenie
            }
        }

        // Fallback: base weenie name.
        var weenie = DatabaseManager.World.GetCachedWeenie(listing.ItemWeenieClassId);
        if (weenie?.PropertiesString != null
            && weenie.PropertiesString.TryGetValue(PropertyString.Name, out var weenieName)
            && !string.IsNullOrWhiteSpace(weenieName))
        {
            return weenieName;
        }

        return null;
    }

    private static string FormatRemaining(TimeSpan remaining)
    {
        if (remaining.TotalDays >= 1)
        {
            return $"{(int)Math.Floor(remaining.TotalDays)}d {remaining.Hours}h";
        }

        if (remaining.TotalHours >= 1)
        {
            return $"{(int)Math.Floor(remaining.TotalHours)}h {remaining.Minutes}m";
        }

        if (remaining.TotalMinutes >= 1)
        {
            return $"{(int)Math.Floor(remaining.TotalMinutes)}m {remaining.Seconds}s";
        }

        return $"{Math.Max(0, remaining.Seconds)}s";
    }

    private static string FormatListingDuration(TimeSpan duration)
    {
        if (duration.TotalDays >= 1)
        {
            return $"{duration.TotalDays:N0} day(s)";
        }

        if (duration.TotalHours >= 1)
        {
            return $"{duration.TotalHours:N0} hour(s)";
        }

        return $"{duration.TotalMinutes:N0} minute(s)";
    }

    private static void SendTell(Player player, string message)
    {
        if (player?.Session == null || string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        player.Session.Network.EnqueueSend(new GameEventTell(player, message, player, ChatMessageType.Tell));
    }

    public static bool IsMarketBroker(WorldObject npc)
    {
        if (npc == null)
        {
            return false;
        }

        return string.Equals(npc.GetProperty(PropertyString.Template), TemplateName, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsSellable(WorldObject item, out string reason)
    {
        if (item == null)
        {
            reason = "Item not found.";
            return false;
        }

        if (item.Attuned == AttunedStatus.Attuned)
        {
            reason = "Attuned items cannot be listed.";
            return false;
        }

        var t = item.ItemType;
        var ok = t == ItemType.Weapon
                 || t == ItemType.MeleeWeapon
                 || t == ItemType.MissileWeapon
                 || t == ItemType.Caster
                 || t == ItemType.Armor
                 || t == ItemType.Clothing
                 || t == ItemType.Jewelry
                 || t == ItemType.Food
                 || t == ItemType.Gem
                 || t == ItemType.TinkeringMaterial
                 || t == ItemType.Useless;

        if (!ok)
        {
            reason = "Only weapons, armor, clothin, jewelry, casters, gems, salvage, trophies, and consumables can be listed.";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    public static void SendHelp(Player player)
    {
        if (player?.Session == null)
        {
            return;
        }

        var pendingPayouts = MarketServiceLocator.PlayerMarketRepository
            .GetPendingPayouts(player.Character.AccountId)
            .Count();

        SendTell(
            player,
            $"Give me an item to list it. Say 'listings' to review your active listings, or 'payouts' to see pending payouts ({pendingPayouts}).");
    }

    public static void HandleTalkDirect(Player player, WorldObject broker, string message)
    {
        if (player == null || broker == null)
        {
            return;
        }

        if (!IsMarketBroker(broker) || string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        var input = message.Trim();

        if (input.Equals("help", StringComparison.OrdinalIgnoreCase)
            || input.Equals("market", StringComparison.OrdinalIgnoreCase))
        {
            SendHelp(player);
            return;
        }

        if (input.Equals("payouts", StringComparison.OrdinalIgnoreCase))
        {
            var payouts = MarketServiceLocator.PlayerMarketRepository
                .GetPendingPayouts(player.Character.AccountId)
                .ToList();

            if (payouts.Count == 0)
            {
                SendTell(player, "You have no pending payouts.");
                return;
            }

            SendTell(player, $"You have {payouts.Count} pending payout(s). (Claiming not implemented yet)");
            return;
        }

        if (input.Equals("listings", StringComparison.OrdinalIgnoreCase))
        {
            var listings = MarketServiceLocator.PlayerMarketRepository
                .GetListingsForAccount(player.Character.AccountId, DateTime.UtcNow)
                .ToList();

            if (listings.Count == 0)
            {
                SendTell(player, "You have no active listings.");
                return;
            }

            var now = DateTime.UtcNow;

            // Build output in chunks so we don't exceed tell size limits.
            // Also, order by soonest expiration so the most urgent listings show first.
            var ordered = listings
                .OrderBy(l => l.ExpiresAtUtc)
                .ThenBy(l => l.Id)
                .ToList();

            var header = $"You have {ordered.Count} active listing(s):";

            var sb = new StringBuilder();
            sb.AppendLine(header);

            // Rough limit to avoid overly long messages.
            const int maxTellLength = 850;

            foreach (var l in ordered)
            {
                var remaining = l.ExpiresAtUtc - now;
                if (remaining < TimeSpan.Zero)
                {
                    remaining = TimeSpan.Zero;
                }

                var itemName = TryGetListingItemName(l) ?? $"WCID {l.ItemWeenieClassId}";
                var line = $"#{l.Id} | {itemName} | {l.ListedPrice:N0} py | {FormatRemaining(remaining)} left";

                // If adding another line would exceed the limit, flush and start a new tell.
                if (sb.Length + line.Length + Environment.NewLine.Length > maxTellLength)
                {
                    SendTell(player, sb.ToString().TrimEnd());
                    sb.Clear();
                }

                sb.AppendLine(line);
            }

            if (sb.Length > 0)
            {
                SendTell(player, sb.ToString().TrimEnd());
            }
            return;
        }

        if (TryParsePriceInput(input, out var price))
        {
            if (!TryGetPendingItemGuid(player, out var itemGuid))
            {
                SendTell(player, "No pending item. Give me an item first.");
                return;
            }

            var item = player.FindObject(
                itemGuid,
                Player.SearchLocations.MyInventory | Player.SearchLocations.MyEquippedItems,
                out _,
                out _,
                out _);
            if (item == null)
            {
                ClearPendingItem(player);
                SendTell(player, "I can't find that item anymore. Give it to me again.");
                return;
            }

            if (!IsSellable(item, out var reason))
            {
                ClearPendingItem(player);
                SendTell(player, reason);
                return;
            }

            var duration = MarketServiceLocator.Config.ListingLifetime;
            var expiresAtUtc = DateTime.UtcNow + duration;

            // Store pending listing details and require a Yes/No confirmation.
            _stateByPlayerGuid.AddOrUpdate(
                player.Guid.Full,
                _ => new MarketBrokerSessionState
                {
                    PendingItemGuid = item.Guid.Full,
                    PendingItemWeenieClassId = item.WeenieClassId,
                    PendingPrice = price,
                    PendingExpiresAtUtc = expiresAtUtc
                },
                (_, state) =>
                {
                    state.PendingItemGuid = item.Guid.Full;
                    state.PendingItemWeenieClassId = item.WeenieClassId;
                    state.PendingPrice = price;
                    state.PendingExpiresAtUtc = expiresAtUtc;
                    return state;
                });

            var confirmText =
                $"List '{item.Name}' for {price:N0} pyreals?\n\n" +
                $"Duration: {FormatListingDuration(duration)}\n" +
                $"Expires: {expiresAtUtc:yyyy-MM-dd HH:mm} UTC\n\n" +
                $"To cancel: tell the broker 'listings' (canceling not implemented yet).";

            player.ConfirmationManager.EnqueueSend(
                new Confirmation_Custom(
                    player.Guid,
                    () => FinalizeConfirmedListing(player)
                ),
                confirmText);

            SendTell(player, "Please confirm the listing.");
            return;
        }

        SendHelp(player);
    }

    private static void FinalizeConfirmedListing(Player player)
    {
        if (player?.Session == null)
        {
            return;
        }

        if (!_stateByPlayerGuid.TryGetValue(player.Guid.Full, out var state)
            || !state.PendingItemGuid.HasValue
            || !state.PendingPrice.HasValue)
        {
            SendTell(player, "No pending listing to confirm.");
            return;
        }

        var itemGuid = state.PendingItemGuid.Value;
        var price = state.PendingPrice.Value;

        var item = player.FindObject(
            itemGuid,
            Player.SearchLocations.MyInventory | Player.SearchLocations.MyEquippedItems,
            out _,
            out _,
            out var wasEquipped);
        if (item == null)
        {
            ClearPendingItem(player);
            SendTell(player, "I can't find that item anymore. Give it to me again.");
            return;
        }

        if (!IsSellable(item, out var reason))
        {
            ClearPendingItem(player);
            SendTell(player, reason);
            return;
        }

        // Escrow: remove the whole object from the player now.
        if (wasEquipped)
        {
            if (!player.TryDequipObjectWithNetworking(item.Guid, out _, Player.DequipObjectAction.GiveItem))
            {
                SendTell(player, "Unable to escrow item (failed to dequip).");
                return;
            }
        }
        else
        {
            if (!player.TryRemoveFromInventoryWithNetworking(item.Guid, out _, Player.RemoveFromInventoryAction.GiveItem))
            {
                SendTell(player, "Unable to escrow item (failed to remove from inventory).");
                return;
            }
        }

        // Persist item off-player. For now, keep the same biota record but clear ownership.
        item.ContainerId = (uint?)null;
        item.WielderId = (uint?)null;
        item.Location = null;
        item.SaveBiotaToDatabase();

        int? wieldReq = null;
        int? itemTier = null;

        // Determine tier so the listing can show up on the correct market vendor tier.
        // Weapons/Armor: derive tier from WieldDifficulty.
        // Clothing/Jewelry: derive tier from required level (wield difficulty if using level requirement).
        switch (item.ItemType)
        {
            case ItemType.Weapon:
            case ItemType.MeleeWeapon:
            case ItemType.MissileWeapon:
            case ItemType.Caster:
            case ItemType.Armor:
                if (item.WieldDifficulty.HasValue)
                {
                    wieldReq = item.WieldDifficulty.Value;
                    itemTier = LootGenerationFactory.GetTierFromWieldDifficulty(item.WieldDifficulty.Value);
                }

                break;

            case ItemType.Clothing:
            case ItemType.Jewelry:
                // prefer explicit required level if present
                if (item.WieldRequirements == WieldRequirement.Level && item.WieldDifficulty.HasValue)
                {
                    wieldReq = item.WieldDifficulty.Value;
                    itemTier = LootGenerationFactory.GetTierFromRequiredLevel(item.WieldDifficulty.Value);
                }
                else if (item.WieldDifficulty.HasValue)
                {
                    // fallback: treat wield difficulty as the level requirement if that's all we have
                    wieldReq = item.WieldDifficulty.Value;
                    itemTier = LootGenerationFactory.GetTierFromRequiredLevel(item.WieldDifficulty.Value);
                }

                break;
        }

        // Route the listing to the vendor tier that matches the item.
        // If we can't determine the item's tier, fall back to the seller's tier.
        var vendorTier = itemTier ?? player.GetPlayerTier(player.Level);

        MarketServiceLocator.PlayerMarketRepository.CreateListingFromWorldObject(
            player,
            item,
            price,
            MarketCurrencyType.Pyreal,
            vendorTier,
            wieldReq,
            itemTier,
            $"Listed by {player.Name}");

        ClearPendingItem(player);

        SendTell(player, $"Listed {item.Name} for {price:N0} pyreals.");
    }

    private static bool TryParsePriceInput(string input, out int price)
    {
        price = 0;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var trimmed = input.Trim();

        if (int.TryParse(trimmed, out price) && price > 0)
        {
            return true;
        }

        if (trimmed.StartsWith("price ", StringComparison.OrdinalIgnoreCase))
        {
            var rest = trimmed.Substring("price ".Length).Trim();
            return int.TryParse(rest, out price) && price > 0;
        }

        return false;
    }

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<uint, MarketBrokerSessionState> _stateByPlayerGuid =
        new();

    public static bool TryGetPendingItemGuid(Player player, out uint itemGuid)
    {
        itemGuid = 0;
        if (player == null)
        {
            return false;
        }

        if (_stateByPlayerGuid.TryGetValue(player.Guid.Full, out var state)
            && state.PendingItemGuid.HasValue)
        {
            itemGuid = state.PendingItemGuid.Value;
            return true;
        }

        return false;
    }

    public static void ClearPendingItem(Player player)
    {
        if (player == null)
        {
            return;
        }

        _stateByPlayerGuid.TryRemove(player.Guid.Full, out _);
    }

    public static void StartListingFromRefuse(Player player, WorldObject item)
    {
        if (player == null || item == null)
        {
            return;
        }

        if (!IsSellable(item, out var reason))
        {
            SendTell(player, reason);
            return;
        }

        _stateByPlayerGuid[player.Guid.Full] = new MarketBrokerSessionState
        {
            PendingItemGuid = item.Guid.Full,
            PendingItemWeenieClassId = item.WeenieClassId
        };

        SendTell(player, $"Ready to list '{item.Name}'. Tell me the price (number of pyreals).");
    }
}
