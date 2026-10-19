using System;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using ACE.Database;
using ACE.Entity.Models;
using ACE.Server.Factories;
using ACE.Server.WorldObjects;

namespace ACE.Server.Market;

public static class MarketListingSnapshotSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = false
    };

    public static string? TryCreateSnapshotJson(WorldObject item)
    {
        if (item?.Biota == null)
        {
            return null;
        }

        try
        {
            // Force-load a complete biota from DB so navigation collections are populated.
            var biotaId = (uint)item.Biota.Id;
            var dbBiota = DatabaseManager.Shard.BaseDatabase.GetBiota(biotaId, true);
            if (dbBiota == null)
            {
                return null;
            }

            var entityBiota = Database.Adapter.BiotaConverter.ConvertToEntityBiota(dbBiota);

            var snap = new MarketListingSnapshot
            {
                WeenieClassId = (uint)entityBiota.WeenieClassId,
                BiotaId = (uint)entityBiota.Id,
                WeenieType = entityBiota.WeenieType,
                PropertiesInt = entityBiota.PropertiesInt != null ? new(entityBiota.PropertiesInt) : new(),
                PropertiesInt64 = entityBiota.PropertiesInt64 != null ? new(entityBiota.PropertiesInt64) : new(),
                PropertiesBool = entityBiota.PropertiesBool != null ? new(entityBiota.PropertiesBool) : new(),
                PropertiesFloat = entityBiota.PropertiesFloat != null ? new(entityBiota.PropertiesFloat) : new(),
                PropertiesString = entityBiota.PropertiesString != null ? new(entityBiota.PropertiesString) : new(),
                PropertiesDID = entityBiota.PropertiesDID != null ? new(entityBiota.PropertiesDID) : new(),
                PropertiesIID = entityBiota.PropertiesIID != null ? new(entityBiota.PropertiesIID) : new(),
                PropertiesSkill = entityBiota.PropertiesSkill != null ? new(entityBiota.PropertiesSkill) : new(),
                PropertiesPosition = entityBiota.PropertiesPosition != null ? new(entityBiota.PropertiesPosition) : new(),
                PropertiesSpellBook = entityBiota.PropertiesSpellBook != null ? new(entityBiota.PropertiesSpellBook) : new(),
                PropertiesAnimPart = entityBiota.PropertiesAnimPart?.ToList() ?? [],
                PropertiesPalette = entityBiota.PropertiesPalette?.ToList() ?? [],
                PropertiesTextureMap = entityBiota.PropertiesTextureMap?.ToList() ?? [],
                CreatedAtUtc = DateTime.UtcNow
            };

            return JsonSerializer.Serialize(snap, Options);
        }
        catch
        {
            return null;
        }

    }

    /// <summary>
    /// The same snapshot, made from the item as it is in memory rather than from the database: for an item that may not
    /// have been saved yet, like loot on a corpse (the Daralet plugin's loot rules, PlayerUiCorpseData). Only the
    /// properties the market reads are filled in; the looks (anim parts, palettes, textures, positions) are left out.
    /// </summary>
    public static string? TryCreateLiveSnapshotJson(WorldObject item)
    {
        var biota = item?.Biota;

        if (biota == null)
        {
            return null;
        }

        item.BiotaDatabaseLock.EnterReadLock();

        try
        {
            var snap = new MarketListingSnapshot
            {
                WeenieClassId = item.WeenieClassId,
                BiotaId = item.Guid.Full,
                WeenieType = biota.WeenieType,
                PropertiesInt = biota.PropertiesInt != null ? new(biota.PropertiesInt) : new(),
                PropertiesInt64 = biota.PropertiesInt64 != null ? new(biota.PropertiesInt64) : new(),
                PropertiesBool = biota.PropertiesBool != null ? new(biota.PropertiesBool) : new(),
                PropertiesFloat = biota.PropertiesFloat != null ? new(biota.PropertiesFloat) : new(),
                PropertiesString = biota.PropertiesString != null ? new(biota.PropertiesString) : new(),
                PropertiesDID = biota.PropertiesDID != null ? new(biota.PropertiesDID) : new(),
                PropertiesIID = biota.PropertiesIID != null ? new(biota.PropertiesIID) : new(),
                PropertiesSpellBook = biota.PropertiesSpellBook != null ? new(biota.PropertiesSpellBook) : new(),
                CreatedAtUtc = DateTime.UtcNow
            };

            return JsonSerializer.Serialize(snap, Options);
        }
        catch
        {
            return null;
        }
        finally
        {
            item.BiotaDatabaseLock.ExitReadLock();
        }
    }

    public static MarketListingSnapshot? TryDeserializeSnapshot(string snapshotJson)
    {
        if (string.IsNullOrWhiteSpace(snapshotJson))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<MarketListingSnapshot>(snapshotJson, Options);
        }
        catch
        {
            return null;
        }
    }

    public static WorldObject? TryRecreateWorldObjectFromSnapshot(string snapshotJson)
    {
        if (string.IsNullOrWhiteSpace(snapshotJson))
        {
            return null;
        }

        try
        {
            var snap = JsonSerializer.Deserialize<MarketListingSnapshot>(snapshotJson, Options);
            if (snap == null)
            {
                return null;
            }

            var entityBiota = new Biota
            {
                Id = snap.BiotaId,
                WeenieClassId = snap.WeenieClassId,
                WeenieType = snap.WeenieType,
                PropertiesInt = snap.PropertiesInt,
                PropertiesInt64 = snap.PropertiesInt64,
                PropertiesBool = snap.PropertiesBool,
                PropertiesFloat = snap.PropertiesFloat,
                PropertiesString = snap.PropertiesString,
                PropertiesDID = snap.PropertiesDID,
                PropertiesIID = snap.PropertiesIID,
                PropertiesSkill = snap.PropertiesSkill,
                PropertiesSpellBook = snap.PropertiesSpellBook,
                PropertiesPosition = snap.PropertiesPosition,
                PropertiesAnimPart = snap.PropertiesAnimPart,
                PropertiesPalette = snap.PropertiesPalette,
                PropertiesTextureMap = snap.PropertiesTextureMap
            };

            return WorldObjectFactory.CreateWorldObject(entityBiota);
        }
        catch
        {
            return null;
        }
    }
}
