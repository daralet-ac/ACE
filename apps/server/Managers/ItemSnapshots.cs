using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using ACE.Database;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Entity.Models;
using ACE.Server.Factories;
using ACE.Server.Market;
using ACE.Server.WorldObjects;

namespace ACE.Server.Managers;

/// <summary>
/// Whole copies of items, for the item history (a sold or salvaged item can be restored from the one taken when it went)
/// and for @itemcopy. The same JSON as the market's listing snapshots (MarketListingSnapshot), with the looks too.
/// </summary>
public static class ItemSnapshots
{
    private static readonly JsonSerializerOptions Options = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
    };

    /// <summary>
    /// Where an item was: whoever held it, the bank it was in, the spot it lay on. A copy is somewhere else.
    /// </summary>
    private static readonly PropertyInstanceId[] WhereItWas =
    {
        PropertyInstanceId.Container,
        PropertyInstanceId.Wielder,
        PropertyInstanceId.BankAccountId,
        PropertyInstanceId.Generator,
    };

    /// <summary>
    /// The item as it is in memory, everything that makes it what it is. Null if it has none (it isn't a real object).
    /// </summary>
    public static string FromLive(WorldObject item)
    {
        var biota = item?.Biota;

        if (biota == null)
        {
            return null;
        }

        item.BiotaDatabaseLock.EnterReadLock();

        try
        {
            return Serialize(biota, item.WeenieClassId, item.Guid.Full);
        }
        finally
        {
            item.BiotaDatabaseLock.ExitReadLock();
        }
    }

    /// <summary>
    /// The item as the database has it (an offline player's, a bank's, a market listing's). Reads the database: call it
    /// off the world thread. Null if there's no such object.
    /// </summary>
    public static string FromDatabase(uint id)
    {
        var dbBiota = DatabaseManager.Shard.BaseDatabase.GetBiota(id, true);

        if (dbBiota == null)
        {
            return null;
        }

        var biota = Database.Adapter.BiotaConverter.ConvertToEntityBiota(dbBiota);
        return Serialize(biota, biota.WeenieClassId, biota.Id);
    }

    private static string Serialize(Biota biota, uint wcid, uint id)
    {
        var snapshot = new MarketListingSnapshot
        {
            WeenieClassId = wcid,
            BiotaId = id,
            WeenieType = biota.WeenieType,
            PropertiesInt = biota.PropertiesInt != null ? new(biota.PropertiesInt) : new(),
            PropertiesInt64 = biota.PropertiesInt64 != null ? new(biota.PropertiesInt64) : new(),
            PropertiesBool = biota.PropertiesBool != null ? new(biota.PropertiesBool) : new(),
            PropertiesFloat = biota.PropertiesFloat != null ? new(biota.PropertiesFloat) : new(),
            PropertiesString = biota.PropertiesString != null ? new(biota.PropertiesString) : new(),
            PropertiesDID = biota.PropertiesDID != null ? new(biota.PropertiesDID) : new(),
            PropertiesIID = biota.PropertiesIID != null ? new(biota.PropertiesIID) : new(),
            PropertiesSkill = biota.PropertiesSkill != null ? new(biota.PropertiesSkill) : new(),
            PropertiesSpellBook = biota.PropertiesSpellBook != null ? new(biota.PropertiesSpellBook) : new(),
            PropertiesAnimPart = biota.PropertiesAnimPart?.ToList() ?? new(),
            PropertiesPalette = biota.PropertiesPalette?.ToList() ?? new(),
            PropertiesTextureMap = biota.PropertiesTextureMap?.ToList() ?? new(),
            CreatedAtUtc = DateTime.UtcNow,
        };

        return JsonSerializer.Serialize(snapshot, Options);
    }

    /// <summary>
    /// A new item made from a snapshot, with a guid of its own (the old one may belong to something else by now, or still to
    /// the original) and nothing saying where the original was. Null if the snapshot can't be read. Call on the world thread.
    /// </summary>
    public static WorldObject Recreate(string json)
    {
        MarketListingSnapshot snapshot;

        try
        {
            snapshot = JsonSerializer.Deserialize<MarketListingSnapshot>(json, Options);
        }
        catch (JsonException)
        {
            return null;
        }

        if (snapshot == null)
        {
            return null;
        }

        Strip(snapshot);

        var biota = new Biota
        {
            Id = GuidManager.NewDynamicGuid().Full,
            WeenieClassId = snapshot.WeenieClassId,
            WeenieType = snapshot.WeenieType,
            PropertiesInt = snapshot.PropertiesInt,
            PropertiesInt64 = snapshot.PropertiesInt64,
            PropertiesBool = snapshot.PropertiesBool,
            PropertiesFloat = snapshot.PropertiesFloat,
            PropertiesString = snapshot.PropertiesString,
            PropertiesDID = snapshot.PropertiesDID,
            PropertiesIID = snapshot.PropertiesIID,
            PropertiesSkill = snapshot.PropertiesSkill,
            PropertiesSpellBook = snapshot.PropertiesSpellBook,
            PropertiesAnimPart = snapshot.PropertiesAnimPart,
            PropertiesPalette = snapshot.PropertiesPalette,
            PropertiesTextureMap = snapshot.PropertiesTextureMap,
        };

        return WorldObjectFactory.CreateWorldObject(biota);
    }

    /// <summary>
    /// Takes out where the original was: who held it, its slot, the bank it was in, the market listing, where it lay.
    /// </summary>
    public static void Strip(MarketListingSnapshot snapshot)
    {
        foreach (var property in WhereItWas)
        {
            snapshot.PropertiesIID?.Remove(property);
        }

        snapshot.PropertiesInt?.Remove(PropertyInt.CurrentWieldedLocation);
        snapshot.PropertiesInt?.Remove(PropertyInt.PlacementPosition);
        snapshot.PropertiesInt?.Remove(PropertyInt.MarketListingId);
        snapshot.PropertiesPosition = new Dictionary<PositionType, PropertiesPosition>();
    }

    /// <summary>
    /// Whether an item can be copied: a pack can't, as its contents wouldn't come with it, and a player or monster isn't an
    /// item. Says why not.
    /// </summary>
    public static bool CanCopy(WeenieType type, out string reason)
    {
        reason = type switch
        {
            WeenieType.Container => "Packs can't be copied: what's in them wouldn't come along. Copy the items in it instead.",
            WeenieType.Creature or WeenieType.Admin or WeenieType.Sentinel or WeenieType.Cow or WeenieType.Vendor
                => "That isn't an item.",
            WeenieType.Corpse or WeenieType.Storage => "That's a container, not an item.",
            _ => null,
        };

        return reason == null;
    }
}
