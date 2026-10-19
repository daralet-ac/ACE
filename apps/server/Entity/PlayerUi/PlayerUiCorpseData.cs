using System;
using System.Collections.Generic;
using System.Linq;
using ACE.Entity;
using ACE.MarketApi;
using ACE.Server.Market;
using ACE.Server.WorldObjects;

namespace ACE.Server.Entity.PlayerUi;

/// <summary>
/// @playerui corpse and @playerui salvage, for the plugin's loot rules. The plugin decides what to loot; looting is the game's
/// own move, and salvaging the game's own salvage (Player.HandleSalvaging) with the Ust in the player's packs, so every
/// check they make still applies. Nothing here moves an item.
/// </summary>
public static class PlayerUiCorpseData
{
    /// <summary>The most items one @playerui salvage may name.</summary>
    public const int MaxSalvageItems = 20;

    /// <summary>The most items one @playerui describe may name.</summary>
    public const int MaxDescribeItems = 20;

    public static PlayerUiCorpse GetCorpse(Player player, uint guid)
    {
        var landblock = player.CurrentLandblock;
        var found = landblock?.GetObject(new ObjectGuid(guid));

        // near enough to use, as opening it is
        if (found == null || !landblock.WithinUseRadius(player, found.Guid, out _))
        {
            return new PlayerUiCorpse(guid, "gone", null, []);
        }

        if (found is not Corpse corpse)
        {
            return new PlayerUiCorpse(guid, "notcorpse", found.Name, []);
        }

        if (!corpse.HasPermission(player))
        {
            return new PlayerUiCorpse(guid, "denied", corpse.Name, []);
        }

        var items = corpse.Inventory.Values
            .OrderBy(i => i.PlacementPosition ?? int.MaxValue)
            .Select(Describe)
            .Where(i => i != null)
            .ToList();

        return new PlayerUiCorpse(guid, "ok", corpse.Name, items);
    }

    /// <summary>
    /// An item, with the market's description of it: the same snapshot a listing keeps, made from the item in memory (loot
    /// isn't in the database yet), read by the Market API's own code.
    /// </summary>
    public static PlayerUiCorpseItem Describe(WorldObject item)
    {
        MarketListing listing = null;
        var json = MarketListingSnapshotSerializer.TryCreateLiveSnapshotJson(item);

        if (json != null)
        {
            var tier = MarketBroker.IsNonTier(item) ? null : MarketBroker.GetItemTier(item, out _);

            // the columns of the listings query that come from the listing rather than the item
            var row = new Dictionary<string, object>
            {
                ["Id"] = 0,
                ["ItemWeenieClassId"] = item.WeenieClassId,
                ["ListedPrice"] = item.Value ?? 0,
                ["ItemTier"] = tier,
                ["MarketVendorTier"] = tier ?? 0,
                ["CreatedAtUtc"] = DateTime.UtcNow,
                ["ExpiresAtUtc"] = DateTime.UtcNow,
            };

            var (described, spellIds) = MarketRepository.Describe(new SnapshotItemFacts(json, row));
            described.Spells = spellIds.Select(id => new Spell(id).Name ?? $"Spell {id}").ToList();
            listing = described;
        }

        return new PlayerUiCorpseItem(
            item.Guid.Full,
            item.NameWithMaterial ?? item.Name,
            item.StackSize ?? 1,
            item.Value ?? 0,
            item.EncumbranceVal ?? 0,
            Player.IsSalvageable(item),
            item.MaterialType?.ToString(),
            item.Workmanship,
            listing
        );
    }

    /// <summary>
    /// Items the player carries, or has in the bank they have open, described as a corpse's are: for the plugin's item
    /// tooltips (the bank window) and its "test against my packs". Guids that are neither are skipped, so this only ever
    /// describes the player's own things.
    /// </summary>
    public static List<PlayerUiCorpseItem> DescribeCarried(Player player, IEnumerable<string> words)
    {
        var guids = words
            .SelectMany(w => PlayerUiData.ParseGuids([w]))
            .Distinct()
            .Take(MaxDescribeItems)
            .ToList();

        // the bank's own items only while it's open and near, and only this account's (GetBankContents)
        Dictionary<uint, WorldObject> banked = null;

        WorldObject Find(uint guid)
        {
            var carried = player.FindObject(guid, Player.SearchLocations.MyInventory);

            if (carried != null)
            {
                return carried;
            }

            if (banked == null)
            {
                var bank = player.GetOpenBank();

                banked = bank == null || !bank.BankInventoryLoaded
                    ? new Dictionary<uint, WorldObject>()
                    : player.GetBankContents(bank).ToDictionary(c => c.Item.Guid.Full, c => c.Item);
            }

            return banked.TryGetValue(guid, out var item) ? item : null;
        }

        return guids
            .Select(Find)
            .Where(item => item != null)
            .Select(Describe)
            .ToList();
    }

    /// <summary>
    /// Salvages items in the player's packs with the Ust they carry, as the salvage panel would. Returns what went wrong, for
    /// the player, or null once it's handed to the salvaging (which says what it made).
    /// </summary>
    public static string Salvage(Player player, IEnumerable<string> words)
    {
        var guids = new List<uint>();

        foreach (var word in words)
        {
            if (guids.Count >= MaxSalvageItems)
            {
                break;
            }

            guids.AddRange(PlayerUiData.ParseGuids([word]));
        }

        if (guids.Count == 0)
        {
            return "Usage: @playerui salvage <item guid> [item guid]...";
        }

        var ust = player.FindUst();

        if (ust == null)
        {
            return "You need an Ust in your packs to salvage.";
        }

        player.HandleSalvaging(ust.Guid.Full, guids.Distinct().ToList());
        return null;
    }

    /// <summary>The guid as the plugin sends it, or 0.</summary>
    public static uint ParseGuid(string word)
    {
        var parsed = PlayerUiData.ParseGuids([word ?? ""]);
        return parsed.Count > 0 ? parsed[0] : 0;
    }
}
