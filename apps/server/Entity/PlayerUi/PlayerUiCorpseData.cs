using System;
using System.Collections.Generic;
using System.Linq;
using ACE.Entity;
using ACE.MarketApi;
using ACE.Server.Factories;
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

    /// <summary>
    /// A container standing in the world that a player loots like a chest: a Chest, or a plain Container (the static corpses
    /// and piles placed in dungeons are these, not Corpses). Not a corpse (checked on its own, by permission), the bank or
    /// house storage (Storage), a house hook or slumlord, a creature, or anything in an inventory.
    /// </summary>
    public static bool IsLootableContainer(Container container)
    {
        if (container is Corpse or Storage or Hook or SlumLord or Creature)
        {
            return false;
        }

        return (container.ContainerId ?? 0) == 0 && (container.OwnerId ?? 0) == 0;
    }

    public static PlayerUiCorpse GetCorpse(Player player, uint guid)
    {
        var landblock = player.CurrentLandblock;
        var found = landblock?.GetObject(new ObjectGuid(guid));

        if (found == null)
        {
            return new PlayerUiCorpse(guid, "gone", null, []);
        }

        // near enough to use, as opening it is; except one this player has open, which the game let them open from where
        // they stand (a corpse's use radius is small, and WithinUseRadius was stricter than the open), and which is closed
        // when they walk away from it instead
        var openByPlayer = found is Container { IsOpen: true } open && open.Viewer == player.Guid.Full;

        if (!openByPlayer && !landblock.WithinUseRadius(player, found.Guid, out _))
        {
            return new PlayerUiCorpse(guid, "gone", null, []);
        }

        Container container;

        if (found is Corpse corpse)
        {
            if (!corpse.HasPermission(player))
            {
                return new PlayerUiCorpse(guid, "denied", corpse.Name, []);
            }

            container = corpse;
        }
        else if (found is Container worldContainer && IsLootableContainer(worldContainer))
        {
            // only while this player has it open: what's in a chest is theirs to see once they've opened it (unlocked it,
            // passed its quest gate), and not before
            if (!worldContainer.IsOpen || worldContainer.Viewer != player.Guid.Full)
            {
                return new PlayerUiCorpse(guid, "denied", worldContainer.Name, []);
            }

            container = worldContainer;
        }
        else
        {
            return new PlayerUiCorpse(guid, "notcorpse", found.Name, []);
        }

        var items = container.Inventory.Values
            .OrderBy(i => i.PlacementPosition ?? int.MaxValue)
            .Select(Describe)
            .Where(i => i != null)
            .ToList();

        return new PlayerUiCorpse(guid, "ok", container.Name, items);
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
            listing,
            RollQuality(item)
        );
    }

    /// <summary>
    /// The qualities appraisal's Roll Quality section shows, by stat; null when there are none (most items, so the reply
    /// leaves it out).
    /// </summary>
    private static Dictionary<string, double> RollQuality(WorldObject item)
    {
        var qualities = new Dictionary<string, double>();

        foreach (var range in LootGenerationFactory.GetItemRollRanges(item))
        {
            if (range.Quality is { } quality)
            {
                qualities.TryAdd(range.Stat, quality);
            }
        }

        return qualities.Count > 0 ? qualities : null;
    }

    /// <summary>
    /// Items the player carries or wears, or has in the bank they have open, described as a corpse's are: for the plugin's
    /// item tooltips (the bank window), its "test against my packs", and its appraisal's compare with what's equipped. Guids
    /// that are none of these are skipped, so this only ever describes the player's own things.
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
            var carried = player.FindObject(guid, Player.SearchLocations.MyInventory | Player.SearchLocations.MyEquippedItems);

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
