using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using ACE.Database.Models.Shard;
using ACE.Entity;
using ACE.MarketApi;
using ACE.Server.Entity.Actions;
using ACE.Server.Managers;
using ACE.Server.Market;
using ACE.Server.WorldObjects;

namespace ACE.Server.Entity.PlayerUi;

/// <summary>
/// @playerui market and @playerui broker, for the plugin's Market tab. Looking is allowed anywhere; anything that changes a
/// listing goes through a Market Broker near the player, exactly as if they'd told it what to do (MarketBroker.HandleTalkDirect),
/// so the broker's own checks and Yes/No confirmations apply.
/// </summary>
public static class PlayerUiMarketData
{
    /// <summary>How near a Market Broker has to be for the broker actions: the stand in Hebian-To, not across town.</summary>
    public const float BrokerRange = 30;

    /// <summary>Sales are listed for this long after they happen.</summary>
    public static readonly TimeSpan SalesHistory = TimeSpan.FromDays(30);

    private const int MaxSales = 100;

    /// <summary>
    /// A listing's item name and icon never change, and working them out loads the item from the database and reads its
    /// snapshot, so each listing's is looked up once.
    /// </summary>
    private static readonly ConcurrentDictionary<int, (string Name, PlayerUiIcon Icon)> ItemLooks = new();

    private const int MaxCachedLooks = 5000;

    /// <summary>
    /// An account's market rows are read again only once they're this old, or sooner when one of them changes
    /// (MarketListingEvents.AccountChanged). The plugin asks every 5 seconds while its Market tab is open, so this mostly
    /// stops someone typing the command over and over from loading the database.
    /// </summary>
    public static readonly TimeSpan RowsFor = TimeSpan.FromSeconds(4);

    /// <summary>What the market's four queries found for an account, and when.</summary>
    private sealed class AccountRows
    {
        public DateTime ReadAt;
        public List<PlayerMarketListing> Listings;
        public List<PlayerMarketPayout> Pending;
        public List<PlayerMarketListing> Sales;
        public List<PlayerMarketListing> Expired;
    }

    private static readonly ConcurrentDictionary<uint, AccountRows> Rows = new();

    /// <summary>
    /// Goes up each time an account's listings or payouts change, so rows read while one changed aren't kept.
    /// </summary>
    private static readonly ConcurrentDictionary<uint, int> Changes = new();

    /// <summary>Accounts whose rows are being read now: a request meanwhile is answered by that read.</summary>
    private static readonly ConcurrentDictionary<uint, bool> Reading = new();

    static PlayerUiMarketData()
    {
        MarketListingEvents.AccountChanged += Changed;
    }

    private static void Changed(uint accountId)
    {
        Changes.AddOrUpdate(accountId, 1, (_, count) => count + 1);
        Rows.TryRemove(accountId, out _);
    }

    /// <summary>At logout, so the account's rows aren't kept for nothing.</summary>
    public static void Forget(uint accountId)
    {
        Rows.TryRemove(accountId, out _);
        Changes.TryRemove(accountId, out _);
    }

    /// <summary>
    /// Sends the player their market (@playerui market). The database is read on a thread of its own, never the world's:
    /// the reply follows from the world thread once the rows are in, or at once when the rows read last are recent enough.
    /// </summary>
    public static void RequestMarket(Player player)
    {
        var accountId = player.Character.AccountId;

        if (Rows.TryGetValue(accountId, out var recent) && DateTime.UtcNow - recent.ReadAt < RowsFor)
        {
            SendMarket(player, recent);
            return;
        }

        if (!Reading.TryAdd(accountId, true))
        {
            return;
        }

        var repository = MarketServiceLocator.PlayerMarketRepository;
        var changesBefore = Changes.TryGetValue(accountId, out var count) ? count : 0;

        _ = Task.Run(() =>
        {
            AccountRows rows = null;

            try
            {
                rows = ReadRows(repository, accountId);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "[PLAYERUI] Reading the market for account {AccountId} failed", accountId);
            }
            finally
            {
                Reading.TryRemove(accountId, out _);
            }

            if (rows == null)
            {
                return;
            }

            // finding the broker, looking items up and sending all belong to the world thread; logging out happens there too,
            // so a player still online here hasn't had their account forgotten
            WorldManager.EnqueueAction(new ActionEventDelegate(() =>
            {
                if (PlayerManager.GetOnlinePlayer(player.Guid.Full) != player)
                {
                    return;
                }

                if ((Changes.TryGetValue(accountId, out var changesNow) ? changesNow : 0) == changesBefore)
                {
                    Rows[accountId] = rows;
                }

                SendMarket(player, rows);
            }));
        });
    }

    private static AccountRows ReadRows(IPlayerMarketRepository repository, uint accountId)
    {
        var now = DateTime.UtcNow;

        return new AccountRows
        {
            ReadAt = now,
            Listings = repository.GetListingsForAccount(accountId, now).ToList(),
            Pending = repository.GetPendingPayouts(accountId).ToList(),
            Sales = repository.GetSoldListingsForAccount(accountId, now - SalesHistory).Take(MaxSales).ToList(),
            Expired = repository.GetExpiredListingsForAccount(accountId, now).Where(l => !l.IsSold).ToList(),
        };
    }

    private static void SendMarket(Player player, AccountRows rows)
    {
        PlayerUiProtocol.Send(player.Session, "market", GetMarket(player, rows));
    }

    private static PlayerUiMarket GetMarket(Player player, AccountRows rows)
    {
        var now = DateTime.UtcNow;
        var broker = FindBroker(player);

        var listings = rows
            .Listings
            .OrderBy(l => l.ExpiresAtUtc)
            .ThenBy(l => l.Id)
            .Select(l => new PlayerUiListing(l.Id, Look(l).Name, l.ItemWeenieClassId, l.ItemTier, l.ListedPrice, Seconds(l.ExpiresAtUtc - now), Look(l).Icon))
            .ToList();

        var pending = rows.Pending;
        var pendingByListing = pending.GroupBy(p => p.ListingId).ToDictionary(g => g.Key, g => g.Sum(p => p.Amount));

        var sales = rows
            .Sales
            .Select(l =>
            {
                var claimed = !pendingByListing.TryGetValue(l.Id, out var amount);
                var payout = claimed ? MarketServiceLocator.CalculateNetAfterFee(l.ListedPrice) : amount;

                return new PlayerUiSale(l.Id, Look(l).Name, l.ItemWeenieClassId, l.ListedPrice, payout, claimed, Seconds(now - (l.SoldAtUtc ?? now)), Look(l).Icon);
            })
            .ToList();

        var expired = rows
            .Expired
            .OrderBy(l => l.ExpiresAtUtc)
            .Select(l => new PlayerUiExpired(l.Id, Look(l).Name, l.ItemWeenieClassId, l.ListedPrice, Seconds(now - l.ExpiresAtUtc), Look(l).Icon))
            .ToList();

        return new PlayerUiMarket(
            broker != null,
            broker?.Name,
            broker == null ? null : player.GetDistance(broker),
            PropertyManager.GetDouble("market_listing_payout_fee").Item,
            PropertyManager.GetDouble("market_listing_cancellation_fee").Item,
            pending.Sum(p => (long)p.Amount),
            listings,
            sales,
            expired
        );
    }

    /// <summary>
    /// What the plugin's broker buttons send, turned into what the player would tell the broker:
    /// <list type="bullet">
    /// <item>list &lt;item guid&gt; &lt;price&gt;: as if they gave it the item, then told it the price.</item>
    /// <item>cancel &lt;listing id&gt;, claim (payouts), expired (claim expired listings).</item>
    /// </list>
    /// Returns what went wrong, for the player, or null once it's handed to the broker (which answers in its own tells).
    /// </summary>
    public static string BrokerAction(Player player, string[] words)
    {
        var broker = FindBroker(player);

        if (broker == null)
        {
            return $"You need to be within {BrokerRange:0} meters of a Market Broker (the market stand in Hebian-To) to do that.";
        }

        var action = words.Length > 0 ? words[0].ToLowerInvariant() : "";

        switch (action)
        {
            case "list":
            {
                if (
                    words.Length < 3
                    || !TryParseGuid(words[1], out var itemGuid)
                    || !int.TryParse(words[2], NumberStyles.None, CultureInfo.InvariantCulture, out var price)
                    || price <= 0
                )
                {
                    return "Usage: @playerui broker list <item guid> <price>";
                }

                var item = player.FindObject(itemGuid, Player.SearchLocations.MyInventory);

                if (item == null)
                {
                    return "That item isn't in your packs.";
                }

                MarketBroker.StartListingFromRefuse(player, item);

                // StartListingFromRefuse refuses what can't be sold, in a tell, and then there's nothing pending to price
                if (MarketBroker.TryGetPendingItemGuid(player, out var pending) && pending == itemGuid)
                {
                    MarketBroker.HandleTalkDirect(player, broker, price.ToString(CultureInfo.InvariantCulture));
                }

                return null;
            }
            case "cancel":
                if (words.Length < 2 || !int.TryParse(words[1], NumberStyles.None, CultureInfo.InvariantCulture, out var listingId))
                {
                    return "Usage: @playerui broker cancel <listing id>";
                }

                MarketBroker.HandleTalkDirect(player, broker, $"cancel {listingId}");
                return null;
            case "claim":
                MarketBroker.HandleTalkDirect(player, broker, "claim payouts");
                return null;
            case "expired":
                MarketBroker.HandleTalkDirect(player, broker, "claim expired");
                return null;
            default:
                return "Usage: @playerui broker list <item guid> <price> | cancel <listing id> | claim | expired";
        }
    }

    /// <summary>
    /// The nearest Market Broker the player can see, within BrokerRange, or null.
    /// </summary>
    public static Creature FindBroker(Player player)
    {
        var known = player.PhysicsObj?.ObjMaint.GetKnownObjectsValuesAsCreature();

        if (known == null)
        {
            return null;
        }

        return known
            .Where(c => MarketBroker.IsMarketBroker(c) && c.InstanceId == player.InstanceId)
            .Select(c => (Broker: c, Distance: player.GetDistance(c)))
            .Where(b => b.Distance <= BrokerRange)
            .OrderBy(b => b.Distance)
            .Select(b => b.Broker)
            .FirstOrDefault();
    }

    private static (string Name, PlayerUiIcon Icon) Look(PlayerMarketListing listing)
    {
        if (ItemLooks.TryGetValue(listing.Id, out var cached))
        {
            return cached;
        }

        if (ItemLooks.Count > MaxCachedLooks)
        {
            ItemLooks.Clear();
        }

        var look = (MarketBroker.TryGetListingItemName(listing) ?? $"Item {listing.ItemWeenieClassId}", Icon(listing));
        ItemLooks[listing.Id] = look;
        return look;
    }

    /// <summary>
    /// The listing's icon, read from its snapshot by the Market API's own code, so it's the icon market search shows.
    /// </summary>
    private static PlayerUiIcon Icon(PlayerMarketListing listing)
    {
        if (string.IsNullOrWhiteSpace(listing.ItemSnapshotJson))
        {
            return null;
        }

        try
        {
            var row = new Dictionary<string, object> { ["ItemWeenieClassId"] = listing.ItemWeenieClassId };
            var (described, _) = MarketRepository.Describe(new SnapshotItemFacts(listing.ItemSnapshotJson, row));

            return new PlayerUiIcon(described.IconHex, described.IconOverlayHex, described.IconUnderlay, described.IconEffect);
        }
        catch (Exception)
        {
            // a snapshot that can't be read only loses its icon
            return null;
        }
    }

    /// <summary>Whole seconds, never below zero.</summary>
    public static double Seconds(TimeSpan span)
    {
        return Math.Max(0, Math.Floor(span.TotalSeconds));
    }

    private static bool TryParseGuid(string text, out uint guid)
    {
        var parsed = PlayerUiData.ParseGuids([text]);
        guid = parsed.Count > 0 ? parsed[0] : 0;
        return guid != 0;
    }
}
