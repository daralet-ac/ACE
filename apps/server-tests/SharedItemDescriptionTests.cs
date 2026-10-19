using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using ACE.MarketApi;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ACE.Server.Tests;

/// <summary>
/// The Market API's item description, which the server shares (linked source) to describe a corpse's items for the
/// Daralet plugin's loot rules.
/// </summary>
[TestClass]
public class SharedItemDescriptionTests
{
    private static string MarketRepositorySource([CallerFilePath] string here = "")
    {
        return File.ReadAllText(Path.Combine(Path.GetDirectoryName(here)!, "..", "market-api", "MarketRepository.cs"));
    }

    /// <summary>
    /// The snapshot reader finds each column where the listings query's SELECT does: the table and the SQL must agree.
    /// </summary>
    [TestMethod]
    public void EveryColumnIsWhereTheListingsQueryReadsIt()
    {
        var sql = MarketRepositorySource();

        foreach (var (column, path) in ItemColumns.Paths)
        {
            if (column == ItemColumns.SpellCount)
            {
                StringAssert.Contains(sql, $"JSON_LENGTH(JSON_EXTRACT(ItemSnapshotJson, '$.{path}')) AS {column}");
                continue;
            }

            Assert.IsTrue(
                sql.Contains($"JSON_EXTRACT(ItemSnapshotJson, '$.{path}') AS {column}") || sql.Contains($"JSON_EXTRACT(ItemSnapshotJson, '$.{path}')) AS {column}"),
                $"{column} isn't read from $.{path} in MarketRepository.cs"
            );
        }

        Assert.IsTrue(ItemColumns.Paths.Count > 100);
    }

    [TestMethod]
    public void SnapshotFactsReadLikeJsonExtract()
    {
        var json = "{\"WeenieType\":6,\"PropertiesInt\":{\"Damage\":100,\"ItemType\":1},\"PropertiesFloat\":{\"DamageVariance\":0.5},"
            + "\"PropertiesString\":{\"Name\":\"Longsword\"},\"PropertiesDID\":{\"Icon\":100667956},\"PropertiesSpellBook\":{\"2101\":1,\"2102\":1}}";

        var facts = new SnapshotItemFacts(json, new Dictionary<string, object> { ["ItemTier"] = 6, ["ListedPrice"] = 1000, ["ItemTier2"] = null });

        Assert.AreEqual(100, facts.Int("DamageRaw"));
        Assert.AreEqual(0.5, facts.Double("DamageVarianceRaw"));
        Assert.AreEqual(100667956L, facts.Long("IconDidRaw"));
        Assert.AreEqual(6, facts.Int("WeenieTypeRaw"));
        Assert.AreEqual("Longsword", facts.Text("ItemName"));
        Assert.IsNull(facts.Int("WeaponTimeRaw"), "a property the item doesn't have");
        Assert.IsNull(facts.Text("ItemDesc"));
        Assert.AreEqual(2, facts.Int(ItemColumns.SpellCount));
        StringAssert.Contains(facts.Text("SpellBookRaw"), "\"2101\":1");
        StringAssert.Contains(facts.Text("AllPropertiesIntRaw"), "\"Damage\":100");
        Assert.AreEqual(6, facts.Int("ItemTier"), "row values come from the row");
        Assert.AreEqual(1000, facts.Int("ListedPrice"));
    }

    [TestMethod]
    public void AWeaponIsDescribedFromItsSnapshotLikeAListing()
    {
        // ItemType 1 is MeleeWeapon, WeaponType 2 Sword
        var json = "{\"WeenieType\":6,\"PropertiesInt\":{\"ItemType\":1,\"WeaponType\":2,\"Damage\":100,\"WeaponTime\":50},"
            + "\"PropertiesFloat\":{\"DamageVariance\":0.5,\"WeaponOffense\":1.1},\"PropertiesString\":{\"Name\":\"Longsword\"},"
            + "\"PropertiesSpellBook\":{\"2101\":1}}";

        var (listing, spellIds) = MarketRepository.Describe(new SnapshotItemFacts(json, new Dictionary<string, object> { ["ItemTier"] = 6, ["ItemWeenieClassId"] = 351u }));

        Assert.AreEqual("Longsword", listing.ItemName);
        Assert.AreEqual("Weapon", listing.ItemCategory);
        Assert.AreEqual("Martial", listing.WeaponClass);
        Assert.AreEqual(100, listing.BaseDamage);
        Assert.AreEqual(75, listing.AvgDamage);
        Assert.AreEqual(1.1, listing.WeaponOffense);
        Assert.AreEqual(6, listing.ItemTier);
        Assert.AreEqual(351u, listing.ItemWeenieClassId);
        CollectionAssert.AreEqual(new[] { 2101 }, spellIds);
    }
}
