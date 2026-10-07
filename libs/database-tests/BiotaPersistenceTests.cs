using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using ACE.Database.Adapter;
using ACE.Database.Models.Shard;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Entity.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Biota = ACE.Entity.Models.Biota;

namespace ACE.Database.Tests;

/// <summary>
/// Every character and item is saved through BiotaConverter and BiotaUpdater. These save biotas to an in-memory
/// database and load them back.
/// </summary>
[TestClass]
public class BiotaPersistenceTests
{
    private const uint BiotaId = 0x80000001;

    // collections whose order isn't kept, or doesn't matter
    private static readonly HashSet<string> Unordered =
    [
        nameof(Biota.PropertiesCreateList),
        nameof(Biota.PropertiesEmote),
        nameof(Biota.PropertiesEnchantmentRegistry),
        nameof(Biota.PropertiesEventFilter),
        nameof(Biota.PropertiesGenerator),
    ];

    private readonly ShardDatabase database = new();
    private readonly ReaderWriterLockSlim rwLock = new();

    private DbContextOptions<ShardDbContext> options;
    private int nextValue;

    [TestInitialize]
    public void CreateDatabase()
    {
        options = new DbContextOptionsBuilder<ShardDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
    }

    [TestMethod]
    public void Converter_EverythingSurvivesTheRoundTrip()
    {
        var biota = CreateBiota();

        var converted = BiotaConverter.ConvertToEntityBiota(BiotaConverter.ConvertFromEntityBiota(biota));

        AssertSame(biota, converted);
    }

    [TestMethod]
    public void Save_ANewBiotaLoadsBackTheSame()
    {
        var biota = CreateBiota();

        Save(biota);

        AssertSame(biota, Load());
    }

    [TestMethod]
    public void Save_AnUpdateAddsChangesAndRemovesProperties()
    {
        var biota = CreateBiota();
        Save(biota);

        biota.PropertiesInt.Remove(PropertyInt.Value);
        biota.PropertiesInt[PropertyInt.EncumbranceVal] = 99;
        biota.PropertiesInt[PropertyInt.StackSize] = 3;

        biota.PropertiesString.Remove(PropertyString.Inscription);
        biota.PropertiesString[PropertyString.Name] = "Renamed";

        biota.PropertiesBool.Remove(PropertyBool.Stuck);
        biota.PropertiesFloat[PropertyFloat.Translucency] = 0.75;
        biota.PropertiesInt64.Remove(PropertyInt64.AvailableExperience);
        biota.PropertiesIID.Remove(PropertyInstanceId.Wielder);
        biota.PropertiesDID[PropertyDataId.Icon] = 0x06004321;

        biota.PropertiesPosition.Remove(PositionType.Sanctuary);
        biota.PropertiesSpellBook.Remove(2);
        biota.PropertiesSpellBook[3] = 0.25f;

        biota.PropertiesSkill.Remove(Skill.WarMagic);
        biota.PropertiesSkill[Skill.PhysicalDefense].InitLevel = 77;

        biota.PropertiesAnimPart.RemoveAt(0);
        biota.PropertiesPalette.Add(Fill(new PropertiesPalette()));
        biota.PropertiesEnchantmentRegistry.Remove(biota.PropertiesEnchantmentRegistry.First());
        biota.PropertiesBookPageData.RemoveAt(1);
        biota.PropertiesEmote.First().PropertiesEmoteAction.RemoveAt(0);
        biota.HousePermissions.Remove(0x50000004);

        Save(biota);

        AssertSame(biota, Load());
    }

    [TestMethod]
    public void Save_RemovingEverythingOfAKindLeavesNothingBehind()
    {
        var biota = CreateBiota();
        Save(biota);

        biota.PropertiesBool.Clear();
        biota.PropertiesSpellBook.Clear();
        biota.PropertiesEmote.Clear();
        biota.PropertiesEnchantmentRegistry.Clear();
        biota.PropertiesAllegiance.Clear();
        biota.PropertiesBook = null;
        biota.PropertiesBookPageData.Clear();

        Save(biota);

        var loaded = Load();

        Assert.IsTrue(loaded.PropertiesBool == null || loaded.PropertiesBool.Count == 0);
        Assert.IsTrue(loaded.PropertiesSpellBook == null || loaded.PropertiesSpellBook.Count == 0);
        Assert.IsTrue(loaded.PropertiesEmote == null || loaded.PropertiesEmote.Count == 0);
        Assert.IsTrue(loaded.PropertiesEnchantmentRegistry == null || loaded.PropertiesEnchantmentRegistry.Count == 0);
        Assert.IsTrue(loaded.PropertiesAllegiance == null || loaded.PropertiesAllegiance.Count == 0);
        Assert.IsNull(loaded.PropertiesBook);
        Assert.IsTrue(loaded.PropertiesBookPageData == null || loaded.PropertiesBookPageData.Count == 0);

        // and what was left alone is still there
        Assert.AreEqual("Round Trip", loaded.PropertiesString[PropertyString.Name]);
    }

    private void Save(Biota biota)
    {
        using var context = new ShardDbContext(options);

        Assert.IsTrue(database.SaveBiota(context, biota, rwLock));
    }

    private Biota Load()
    {
        using var context = new ShardDbContext(options);

        var biota = database.GetBiota(context, BiotaId);
        Assert.IsNotNull(biota);

        return BiotaConverter.ConvertToEntityBiota(biota);
    }

    /// <summary>
    /// A biota with something in every collection, and every field of every record set
    /// </summary>
    private Biota CreateBiota()
    {
        var emote = Fill(new PropertiesEmote());
        emote.PropertiesEmoteAction = [Fill(new PropertiesEmoteAction()), Fill(new PropertiesEmoteAction())];

        return new Biota
        {
            Id = BiotaId,
            WeenieClassId = 12345,
            WeenieType = WeenieType.Creature,
            PropertiesBool = new Dictionary<PropertyBool, bool>
            {
                [PropertyBool.Stuck] = true,
                [PropertyBool.Attackable] = false,
            },
            PropertiesDID = new Dictionary<PropertyDataId, uint>
            {
                [PropertyDataId.Setup] = 0x02000001,
                [PropertyDataId.Icon] = 0x06001234,
            },
            PropertiesFloat = new Dictionary<PropertyFloat, double>
            {
                [PropertyFloat.HeartbeatInterval] = 5.5,
                [PropertyFloat.Translucency] = 0.25,
            },
            PropertiesIID = new Dictionary<PropertyInstanceId, uint>
            {
                [PropertyInstanceId.Owner] = 0x50000001,
                [PropertyInstanceId.Wielder] = 0x50000002,
            },
            PropertiesInt = new Dictionary<PropertyInt, int>
            {
                [PropertyInt.Value] = 1234,
                [PropertyInt.EncumbranceVal] = -5,
            },
            PropertiesInt64 = new Dictionary<PropertyInt64, long>
            {
                [PropertyInt64.TotalExperience] = 10_000_000_000L,
                [PropertyInt64.AvailableExperience] = 7,
            },
            PropertiesString = new Dictionary<PropertyString, string>
            {
                [PropertyString.Name] = "Round Trip",
                [PropertyString.Inscription] = "with ünïcode ✓",
            },
            PropertiesPosition = new Dictionary<PositionType, PropertiesPosition>
            {
                [PositionType.Location] = Fill(new PropertiesPosition()),
                [PositionType.Sanctuary] = Fill(new PropertiesPosition()),
            },
            PropertiesSpellBook = new Dictionary<int, float> { [1] = 1.0f, [2] = 0.5f },
            PropertiesAnimPart = [Fill(new PropertiesAnimPart()), Fill(new PropertiesAnimPart())],
            PropertiesPalette = [Fill(new PropertiesPalette()), Fill(new PropertiesPalette())],
            PropertiesTextureMap = [Fill(new PropertiesTextureMap()), Fill(new PropertiesTextureMap())],
            PropertiesCreateList = [Fill(new PropertiesCreateList()), Fill(new PropertiesCreateList())],
            PropertiesEmote = [emote],
            PropertiesEventFilter = [1, 2, 3],
            PropertiesGenerator = [Fill(new PropertiesGenerator()), Fill(new PropertiesGenerator())],
            PropertiesAttribute = new Dictionary<PropertyAttribute, PropertiesAttribute>
            {
                [PropertyAttribute.Strength] = Fill(new PropertiesAttribute()),
                [PropertyAttribute.Coordination] = Fill(new PropertiesAttribute()),
            },
            PropertiesAttribute2nd = new Dictionary<PropertyAttribute2nd, PropertiesAttribute2nd>
            {
                [PropertyAttribute2nd.MaxHealth] = Fill(new PropertiesAttribute2nd()),
                [PropertyAttribute2nd.MaxMana] = Fill(new PropertiesAttribute2nd()),
            },
            PropertiesBodyPart = new Dictionary<CombatBodyPart, PropertiesBodyPart>
            {
                [CombatBodyPart.Head] = Fill(new PropertiesBodyPart()),
                [CombatBodyPart.Chest] = Fill(new PropertiesBodyPart()),
            },
            PropertiesSkill = new Dictionary<Skill, PropertiesSkill>
            {
                [Skill.PhysicalDefense] = Fill(new PropertiesSkill()),
                [Skill.WarMagic] = Fill(new PropertiesSkill()),
            },
            PropertiesBook = Fill(new PropertiesBook()),
            PropertiesBookPageData = [Fill(new PropertiesBookPageData()), Fill(new PropertiesBookPageData())],
            PropertiesAllegiance = new Dictionary<uint, PropertiesAllegiance>
            {
                [0x50000003] = Fill(new PropertiesAllegiance()),
            },
            PropertiesEnchantmentRegistry =
            [
                Fill(new PropertiesEnchantmentRegistry()),
                Fill(new PropertiesEnchantmentRegistry()),
            ],
            HousePermissions = new Dictionary<uint, bool> { [0x50000004] = true, [0x50000005] = false },
        };
    }

    /// <summary>
    /// Sets every number, enum, string and bool on a record, each to a different value
    /// </summary>
    private T Fill<T>(T record)
    {
        foreach (var property in typeof(T).GetProperties().Where(p => p.CanWrite && p.GetIndexParameters().Length == 0))
        {
            // the database assigns these
            if (property.Name == "DatabaseRecordId")
            {
                continue;
            }

            var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

            // small enough for every column
            var n = nextValue++ % 100 + 1;

            object value;

            if (type == typeof(bool))
            {
                value = true;
            }
            else if (type == typeof(string))
            {
                value = $"text {n}";
            }
            else if (type.IsEnum)
            {
                value = Enum.ToObject(type, n);
            }
            else if (type == typeof(float))
            {
                value = n + 0.25f;
            }
            else if (type == typeof(double))
            {
                value = n + 0.125;
            }
            else if (type.IsPrimitive)
            {
                value = Convert.ChangeType(n, type);
            }
            else
            {
                continue;
            }

            property.SetValue(record, value);
        }

        return record;
    }

    private static void AssertSame(Biota expected, Biota actual)
    {
        Assert.AreEqual(Canonical(expected), Canonical(actual));
    }

    /// <summary>
    /// The biota as json with its keys sorted, so two can be compared
    /// </summary>
    private static string Canonical(Biota biota)
    {
        var json = Normalize(JsonSerializer.SerializeToNode(biota), null);

        return json.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }

    private static JsonNode Normalize(JsonNode node, string name)
    {
        switch (node)
        {
            case JsonObject jsonObject:
                var sorted = new JsonObject();

                foreach (var (key, value) in jsonObject.OrderBy(p => p.Key, StringComparer.Ordinal))
                {
                    // assigned by the database, and a weenie the emote isn't saved with
                    if (key is "DatabaseRecordId" or nameof(PropertiesEmote.Object))
                    {
                        continue;
                    }

                    sorted[key] = Normalize(value, key);
                }

                return sorted;

            case JsonArray jsonArray:
                var items = jsonArray.Select(item => Normalize(item, null));

                if (name != null && Unordered.Contains(name))
                {
                    items = items.OrderBy(item => item?.ToJsonString(), StringComparer.Ordinal);
                }

                return new JsonArray(items.ToArray());

            default:
                return node?.DeepClone();
        }
    }
}
