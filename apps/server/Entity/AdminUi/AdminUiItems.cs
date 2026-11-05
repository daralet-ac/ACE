using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ACE.Database;
using ACE.Database.Models.Shard;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Entity.Actions;
using ACE.Server.Managers;
using ACE.Server.Market;
using ACE.Server.Network;
using ACE.Server.WorldObjects;
using Serilog;

namespace ACE.Server.Entity.AdminUi;

/// <summary>
/// An item the search found. Place is where it is: "equipped", "pack" (Container names the side pack, or null for the
/// main pack), "bank" (Container names the bank's side pack, if it's in one), "market" (listed, Container is the
/// listing) or "corpse" (on its owner's corpse). Owner is the character holding it, or null for a bank, which is the
/// account's.
/// </summary>
public record AdminUiItem(
    uint Guid,
    uint Wcid,
    string Name,
    int Amount,
    string Place,
    string Container,
    uint? OwnerGuid,
    string Owner,
    string Account,
    bool Online
);

/// <summary>
/// The reply to @adminui items. Truncated means there were more than it shows: narrow the search. Error says why there
/// are no results, when it isn't that nothing matched.
/// </summary>
public record AdminUiItemSearch(string Mode, string Query, List<AdminUiItem> Items, bool Truncated, string Error);

/// <summary>
/// One time an item changed hands (ItemTransferLog), or was sold on the market.
/// </summary>
public record AdminUiItemTransfer(
    double SecondsAgo,
    string Kind,
    uint ItemGuid,
    uint Wcid,
    string Item,
    int Amount,
    string From,
    string FromAccount,
    string To,
    string ToAccount,
    string Details
);

public record AdminUiItemHistory(string Mode, string Query, List<AdminUiItemTransfer> Entries, string Error);

/// <summary>
/// @adminui items and itemhistory: finds items on every character, online or not, in every bank and on the market, and
/// what's changed hands. Online players' items are read from the game (the database can be a few minutes behind them);
/// everything else from the shard database, off the world thread.
/// </summary>
public static class AdminUiItems
{
    private static readonly ILogger _log = Log.ForContext(typeof(AdminUiItems));

    public const string KindItems = "items";
    public const string KindHistory = "itemhistory";

    public static readonly string[] SearchModes = { "name", "wcid", "guid", "character", "account" };
    public static readonly string[] HistoryModes = { "guid", "name", "character", "account" };

    /// <summary>The most items one reply sends. Each is a line or so of chat, base64 and all.</summary>
    public const int MaxResults = 300;

    /// <summary>The most history entries one reply sends.</summary>
    public const int MaxHistory = 200;

    /// <summary>How many rows the database search reads before it stops. Some are dropped (online players, houses).</summary>
    private const int DatabaseLimit = 2000;

    private const int MinNameLength = 3;

    // A name search reads every name in the shard, so only one runs at a time, for all staff together.
    private static int _searching;

    public const string PlaceEquipped = "equipped";
    public const string PlacePack = "pack";
    public const string PlaceBank = "bank";
    public const string PlaceMarket = "market";
    public const string PlaceCorpse = "corpse";

    /// <summary>
    /// The mode and text of "@adminui items name iron jitte": the second word, and the rest. False if there's no text, or the
    /// mode isn't one of modes.
    /// </summary>
    public static bool TryParseRequest(string[] parameters, string[] modes, out string mode, out string text)
    {
        mode = parameters.Length > 1 ? parameters[1].ToLowerInvariant() : "";
        text = parameters.Length > 2 ? string.Join(" ", parameters.Skip(2)).Trim() : "";

        return modes.Contains(mode) && text.Length > 0;
    }

    /// <summary>
    /// A guid as staff type it: 0x80001234 or 80001234 (hex, as the game shows it), or 2147488308 (decimal, with no a-f).
    /// Hex wins when it could be either and is eight digits, the length of an item guid.
    /// </summary>
    public static bool TryParseGuid(string text, out uint guid)
    {
        text = text.Trim();

        if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return uint.TryParse(text[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out guid);
        }

        if (text.Length == 8 && uint.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out guid))
        {
            return true;
        }

        if (uint.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out guid))
        {
            return true;
        }

        return uint.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out guid);
    }

    // ---------------------------------------------------------------- search

    /// <summary>
    /// Who a search is limited to (the character and account modes): their characters and bank accounts.
    /// </summary>
    private sealed class Scope
    {
        public HashSet<uint> Characters = new();
        public HashSet<uint> Accounts = new();
    }

    public static void HandleSearch(Session session, string[] parameters)
    {
        if (!TryParseRequest(parameters, SearchModes, out var mode, out var text))
        {
            SendSearch(session, new AdminUiItemSearch(mode, text, new(), false,
                "Search by name, wcid, guid, character or account, e.g. @adminui items name Iron Jitte."));
            return;
        }

        uint number = 0;
        Scope scope = null;
        string error = null;

        switch (mode)
        {
            case "name":
                if (text.Length < MinNameLength)
                {
                    error = $"Type at least {MinNameLength} letters of the name.";
                }
                break;
            case "wcid":
                if (!uint.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out number))
                {
                    error = $"{text} isn't a WCID (a number).";
                }
                break;
            case "guid":
                if (!TryParseGuid(text, out number))
                {
                    error = $"{text} isn't a guid (0x8... as the game shows it).";
                }
                break;
            case "character":
            case "account":
                scope = FindScope(mode, text, out error);
                break;
        }

        if (error != null)
        {
            SendSearch(session, new AdminUiItemSearch(mode, text, new(), false, error));
            return;
        }

        if (Interlocked.CompareExchange(ref _searching, 1, 0) != 0)
        {
            SendSearch(session, new AdminUiItemSearch(mode, text, new(), false,
                "Another item search is running. Try again in a moment."));
            return;
        }

        _ = Task.Run(() =>
        {
            List<ItemSearchRow> rows = null;
            Dictionary<uint, ItemSearchRow> parents = null;
            List<(PlayerMarketListing Listing, ItemSearchRow Row)> listed = null;
            var truncated = false;

            try
            {
                var db = DatabaseManager.Shard.BaseDatabase;

                rows = mode switch
                {
                    "name" => db.SearchItemsByName(text, DatabaseLimit),
                    "wcid" => db.SearchItemsByWcid(number, DatabaseLimit),
                    "guid" => db.GetItemRows(new[] { number }),
                    _ => db.SearchItemsByOwners(scope.Characters, scope.Accounts, DatabaseLimit),
                };

                truncated = rows.Count >= DatabaseLimit;
                parents = ReadParents(rows);
                listed = ReadListed(mode, text, number, scope);
            }
            catch (Exception ex)
            {
                _log.Error(ex, "[ADMINUI] Item search ({Mode} {Text}) failed.", mode, text);
                rows = null;
            }
            finally
            {
                Interlocked.Exchange(ref _searching, 0);
            }

            WorldManager.EnqueueAction(new ActionEventDelegate(() =>
            {
                try
                {
                    if (rows == null)
                    {
                        SendSearch(session, new AdminUiItemSearch(mode, text, new(), false,
                            "The search couldn't read the database. The server log has the details."));
                        return;
                    }

                    var items = Merge(mode, text, number, scope, rows, parents, listed);
                    var tooMany = items.Count > MaxResults;

                    SendSearch(session, new AdminUiItemSearch(mode, text, items.Take(MaxResults).ToList(), truncated || tooMany, null));
                }
                catch (Exception ex)
                {
                    _log.Error(ex, "[ADMINUI] Item search ({Mode} {Text}) failed.", mode, text);
                }
            }));
        });
    }

    /// <summary>
    /// The characters and banks a character or account search covers. Account takes an account name or the name of any
    /// character on it; character takes a character's name, and includes their account's bank.
    /// </summary>
    private static Scope FindScope(string mode, string text, out string error)
    {
        error = null;
        var scope = new Scope();
        var all = PlayerManager.GetAllPlayers();

        uint? accountId = null;

        if (mode == "account")
        {
            accountId = all.FirstOrDefault(p => string.Equals(p.Account?.AccountName, text, StringComparison.OrdinalIgnoreCase))?.Account?.AccountId;
        }

        var character = PlayerManager.FindByName(text);

        if (accountId == null && character == null)
        {
            error = mode == "account" ? $"No account or character is called {text}." : $"No character is called {text}.";
            return null;
        }

        accountId ??= character.Account?.AccountId;

        if (mode == "character")
        {
            scope.Characters.Add(character.Guid.Full);
        }
        else
        {
            foreach (var player in all.Where(p => p.Account?.AccountId == accountId))
            {
                scope.Characters.Add(player.Guid.Full);
            }
        }

        if (accountId != null)
        {
            scope.Accounts.Add(accountId.Value);
        }

        return scope;
    }

    /// <summary>
    /// The containers above the items found, two levels up: an item in a pack on a corpse is as deep as it goes.
    /// </summary>
    private static Dictionary<uint, ItemSearchRow> ReadParents(List<ItemSearchRow> rows)
    {
        var parents = new Dictionary<uint, ItemSearchRow>();
        var wanted = rows.Select(r => r.ContainerId).ToList();

        for (var level = 0; level < 2; level++)
        {
            var ids = wanted
                .Where(id => id != null && !new ObjectGuid(id.Value).IsPlayer() && !parents.ContainsKey(id.Value))
                .Select(id => id.Value)
                .Distinct()
                .ToList();

            if (ids.Count == 0)
            {
                break;
            }

            var found = DatabaseManager.Shard.BaseDatabase.GetItemRows(ids);

            foreach (var row in found)
            {
                parents[row.Id] = row;
            }

            wanted = found.Select(r => r.ContainerId).ToList();
        }

        return parents;
    }

    /// <summary>
    /// The market's listings that hold an item the search is for: by name, wcid or guid, or by the characters or account
    /// searched for. The market holds them in no container, so the other searches can't find them.
    /// </summary>
    private static List<(PlayerMarketListing, ItemSearchRow)> ReadListed(string mode, string text, uint number, Scope scope)
    {
        var listings = DatabaseManager.Shard.BaseDatabase.GetListingsHoldingItems();

        if (mode == "character" || mode == "account")
        {
            listings = listings
                .Where(l => (l.SellerCharacterId != null && scope.Characters.Contains(l.SellerCharacterId.Value))
                    || (mode == "account" && scope.Accounts.Contains(l.SellerAccountId)))
                .ToList();
        }
        else if (mode == "wcid")
        {
            listings = listings.Where(l => l.ItemWeenieClassId == number).ToList();
        }
        else if (mode == "guid")
        {
            listings = listings.Where(l => l.ItemBiotaId == number || l.ItemGuid == number).ToList();
        }

        if (listings.Count == 0)
        {
            return new();
        }

        var rows = DatabaseManager.Shard.BaseDatabase
            .GetItemRows(listings.Select(l => l.ItemBiotaId))
            .ToDictionary(r => r.Id);

        return listings
            .Where(l => rows.ContainsKey(l.ItemBiotaId))
            .Select(l => (l, rows[l.ItemBiotaId]))
            // still in escrow: one with a container has been handed back
            .Where(x => x.Item2.ContainerId == null && x.Item2.WielderId == null)
            .Where(x => mode != "name" || Contains(x.Item2.Name, text))
            .ToList();
    }

    /// <summary>
    /// The database's rows and the market's, less anything an online player holds, which is read from the game instead.
    /// Runs on the world thread.
    /// </summary>
    private static List<AdminUiItem> Merge(
        string mode,
        string text,
        uint number,
        Scope scope,
        List<ItemSearchRow> rows,
        Dictionary<uint, ItemSearchRow> parents,
        List<(PlayerMarketListing Listing, ItemSearchRow Row)> listed
    )
    {
        var accountNames = PlayerManager.GetAllPlayers()
            .Where(p => p.Account != null)
            .GroupBy(p => p.Account.AccountId)
            .ToDictionary(g => g.Key, g => g.First().Account.AccountName);

        string AccountName(uint? id) => id == null ? null : accountNames.TryGetValue(id.Value, out var name) ? name : $"#{id}";

        var items = new List<AdminUiItem>();
        var seen = new HashSet<uint>();

        // online players first: the game has what they hold now
        foreach (var player in PlayerManager.GetAllOnline())
        {
            if (scope != null && !scope.Characters.Contains(player.Guid.Full))
            {
                continue;
            }

            var possessions = player.GetAllPossessions();
            var packs = possessions.OfType<Container>().ToDictionary(c => c.Guid.Full);

            foreach (var item in possessions)
            {
                if (!Matches(mode, text, number, scope, item.Name, item.WeenieClassId, item.Guid.Full) || !seen.Add(item.Guid.Full))
                {
                    continue;
                }

                var equipped = item.WielderId == player.Guid.Full;
                var pack = !equipped && item.ContainerId != null && packs.TryGetValue(item.ContainerId.Value, out var p) ? p.Name : null;

                items.Add(new AdminUiItem(
                    item.Guid.Full,
                    item.WeenieClassId,
                    item.Name,
                    item.StackSize ?? 1,
                    equipped ? PlaceEquipped : PlacePack,
                    pack,
                    player.Guid.Full,
                    player.Name,
                    player.Account?.AccountName,
                    true
                ));
            }
        }

        foreach (var row in rows)
        {
            if (seen.Contains(row.Id) || !Matches(mode, text, number, scope, row.Name, row.Wcid, row.Id))
            {
                continue;
            }

            var where = Locate(row, parents);

            if (where == null)
            {
                continue;
            }

            var (place, container, ownerGuid, accountId) = where.Value;
            IPlayer owner = null;

            if (ownerGuid != null)
            {
                owner = PlayerManager.FindByGuid(ownerGuid.Value, out var online);

                // read from the game above, or the database still has it on someone who's moved it since
                if (online && place != PlaceCorpse)
                {
                    continue;
                }

                accountId ??= owner?.Account?.AccountId;
            }

            seen.Add(row.Id);
            items.Add(new AdminUiItem(
                row.Id,
                row.Wcid,
                row.Name,
                row.StackSize ?? 1,
                place,
                container,
                ownerGuid,
                OwnerName(owner, ownerGuid),
                AccountName(accountId),
                owner != null && PlayerManager.GetOnlinePlayer(owner.Guid.Full) != null
            ));
        }

        foreach (var (listing, row) in listed ?? new())
        {
            if (!seen.Add(row.Id))
            {
                continue;
            }

            var expired = listing.ExpiresAtUtc <= DateTime.UtcNow;
            var currency = ((MarketCurrencyType)listing.CurrencyType) == MarketCurrencyType.TradeNote ? "in trade notes" : "pyreals";

            items.Add(new AdminUiItem(
                row.Id,
                row.Wcid,
                row.Name,
                row.StackSize ?? 1,
                PlaceMarket,
                $"listing #{listing.Id}, {listing.ListedPrice:N0} {currency}{(expired ? ", expired" : "")}",
                listing.SellerCharacterId,
                listing.SellerName,
                AccountName(listing.SellerAccountId),
                listing.SellerCharacterId != null && PlayerManager.GetOnlinePlayer(listing.SellerCharacterId.Value) != null
            ));
        }

        return items
            .OrderBy(i => i.Owner ?? "~", StringComparer.OrdinalIgnoreCase)
            .ThenBy(i => i.Account, StringComparer.OrdinalIgnoreCase)
            .ThenBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string OwnerName(IPlayer owner, uint? guid)
    {
        if (owner == null)
        {
            return guid == null ? null : $"0x{guid:X8}";
        }

        return owner.IsDeleted ? $"{owner.Name} (deleted)" : owner.Name;
    }

    /// <summary>
    /// Whether an item is what the search is for. The database already picked its rows this way; this is for online
    /// players' items, and so both agree.
    /// </summary>
    private static bool Matches(string mode, string text, uint number, Scope scope, string name, uint wcid, uint guid)
    {
        return mode switch
        {
            "name" => Contains(name, text),
            "wcid" => wcid == number,
            "guid" => guid == number,
            _ => scope != null,
        };
    }

    private static bool Contains(string value, string text)
    {
        return value != null && value.Contains(text, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Where a database row is: its place, the pack it's in (if any), the character holding it and, for a bank, the
    /// account. Null for anything not a player's (a house's chests and hooks, the ground).
    /// </summary>
    public static (string Place, string Container, uint? Owner, uint? Account)? Locate(ItemSearchRow row, IReadOnlyDictionary<uint, ItemSearchRow> parents)
    {
        if (row.WielderId != null && new ObjectGuid(row.WielderId.Value).IsPlayer())
        {
            return (PlaceEquipped, null, row.WielderId, null);
        }

        if (row.BankAccountId != null)
        {
            return (PlaceBank, null, null, row.BankAccountId);
        }

        if (row.ContainerId == null)
        {
            return null;
        }

        if (new ObjectGuid(row.ContainerId.Value).IsPlayer())
        {
            return (PlacePack, null, row.ContainerId, null);
        }

        if (!parents.TryGetValue(row.ContainerId.Value, out var parent))
        {
            return null;
        }

        if (parent.BankAccountId != null)
        {
            return (PlaceBank, parent.Name, null, parent.BankAccountId);
        }

        if (IsPlayersCorpse(parent))
        {
            return (PlaceCorpse, null, parent.VictimId, null);
        }

        if (parent.ContainerId == null)
        {
            return null;
        }

        if (new ObjectGuid(parent.ContainerId.Value).IsPlayer())
        {
            return (PlacePack, parent.Name, parent.ContainerId, null);
        }

        if (parents.TryGetValue(parent.ContainerId.Value, out var grandparent) && IsPlayersCorpse(grandparent))
        {
            return (PlaceCorpse, parent.Name, grandparent.VictimId, null);
        }

        return null;
    }

    private static bool IsPlayersCorpse(ItemSearchRow row)
    {
        return row.WeenieType == (int)WeenieType.Corpse && row.VictimId != null && new ObjectGuid(row.VictimId.Value).IsPlayer();
    }

    private static void SendSearch(Session session, AdminUiItemSearch reply)
    {
        Send(session, KindItems, reply);
    }

    // ---------------------------------------------------------------- history

    public static void HandleHistory(Session session, string[] parameters)
    {
        if (!TryParseRequest(parameters, HistoryModes, out var mode, out var text))
        {
            SendHistory(session, new AdminUiItemHistory(mode, text, new(),
                "Look up history by guid, name, character or account, e.g. @adminui itemhistory guid 0x80001234."));
            return;
        }

        uint guid = 0;
        Scope scope = null;
        string error = null;

        switch (mode)
        {
            case "guid":
                if (!TryParseGuid(text, out guid))
                {
                    error = $"{text} isn't a guid (0x8... as the game shows it).";
                }
                break;
            case "name":
                if (text.Length < MinNameLength)
                {
                    error = $"Type at least {MinNameLength} letters of the name.";
                }
                break;
            default:
                scope = FindScope(mode, text, out error);
                break;
        }

        if (error != null)
        {
            SendHistory(session, new AdminUiItemHistory(mode, text, new(), error));
            return;
        }

        _ = Task.Run(() =>
        {
            List<ItemTransfer> transfers = null;
            List<PlayerMarketTransaction> sales = null;

            try
            {
                var db = DatabaseManager.Shard.BaseDatabase;

                switch (mode)
                {
                    case "guid":
                        transfers = db.GetItemTransfersForItem(guid, MaxHistory);
                        sales = db.GetMarketTransactions(guid, null, null, MaxHistory);
                        break;
                    case "name":
                        transfers = db.GetItemTransfersByName(text, MaxHistory);
                        sales = db.GetMarketTransactions(null, null, text, MaxHistory);
                        break;
                    default:
                        transfers = db.GetItemTransfersForCharacters(scope.Characters, MaxHistory);
                        sales = db.GetMarketTransactions(null, scope.Characters, null, MaxHistory);
                        break;
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, "[ADMINUI] Item history ({Mode} {Text}) failed. Has the 2026-11-05-00-Item-Transfer-Log.sql update run?", mode, text);
                transfers = null;
            }

            WorldManager.EnqueueAction(new ActionEventDelegate(() =>
            {
                try
                {
                    if (transfers == null)
                    {
                        SendHistory(session, new AdminUiItemHistory(mode, text, new(),
                            "The history couldn't be read from the database. The server log has the details."));
                        return;
                    }

                    SendHistory(session, new AdminUiItemHistory(mode, text, BuildHistory(transfers, sales ?? new(), DateTime.UtcNow), null));
                }
                catch (Exception ex)
                {
                    _log.Error(ex, "[ADMINUI] Item history ({Mode} {Text}) failed.", mode, text);
                }
            }));
        });
    }

    /// <summary>
    /// The transfers and market sales together, newest first, with account names. Runs on the world thread.
    /// </summary>
    private static List<AdminUiItemTransfer> BuildHistory(List<ItemTransfer> transfers, List<PlayerMarketTransaction> sales, DateTime now)
    {
        var accountNames = PlayerManager.GetAllPlayers()
            .Where(p => p.Account != null)
            .GroupBy(p => p.Account.AccountId)
            .ToDictionary(g => g.Key, g => g.First().Account.AccountName);

        string AccountName(uint? id) => id == null ? null : accountNames.TryGetValue(id.Value, out var name) ? name : $"#{id}";

        var entries = transfers.Select(t => (t.CreatedAtUtc, new AdminUiItemTransfer(
            (now - t.CreatedAtUtc).TotalSeconds,
            t.Kind,
            t.ItemId,
            t.ItemWcid,
            t.ItemName,
            t.Amount,
            t.FromName,
            AccountName(t.FromAccountId),
            t.ToName,
            AccountName(t.ToAccountId),
            t.Details
        )));

        var sold = sales.Select(s => (s.CreatedAtUtc, new AdminUiItemTransfer(
            (now - s.CreatedAtUtc).TotalSeconds,
            "sold",
            s.ItemBiotaId,
            s.ItemWeenieClassId,
            s.ItemName,
            s.Quantity,
            s.SellerName,
            AccountName(s.SellerAccountId),
            s.BuyerName,
            AccountName(s.BuyerAccountId),
            $"on the market for {s.Price:N0} {((MarketCurrencyType)s.CurrencyType == MarketCurrencyType.TradeNote ? "in trade notes" : "pyreals")}"
        )));

        return entries.Concat(sold)
            .OrderByDescending(e => e.Item1)
            .Take(MaxHistory)
            .Select(e => e.Item2)
            .ToList();
    }

    private static void SendHistory(Session session, AdminUiItemHistory reply)
    {
        Send(session, KindHistory, reply);
    }

    private static void Send(Session session, string kind, object payload)
    {
        // the player may have logged off while the database was read
        if (session?.Player == null)
        {
            return;
        }

        foreach (var message in AdminUiProtocol.BuildMessages(kind, payload))
        {
            CommandHandlerHelper.WriteOutputInfo(session, message, ChatMessageType.Broadcast);
        }
    }
}
