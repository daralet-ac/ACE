using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using ACE.Entity.Enum;
using ACE.Server.Factories.Entity;
using ACE.Server.Factories.Tables;
using ACE.Server.Managers;
using ACE.Server.WorldObjects;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ACE.Server.Tests;

/// <summary>
/// ChanceTable only checks itself the first time it is rolled, and then only logs.
/// These find every table in the server and check them all up front.
/// </summary>
[TestClass]
public class ChanceTableTests
{
    // the same tolerance ChanceTable.VerifyTable logs at
    private const decimal Threshold = 0.0000001M;

    // built from the world database when first used, so there is nothing here to check
    private static readonly HashSet<Type> DatabaseTables = [typeof(GemCountChance)];

    private static List<(string Name, object Table)> tables;

    [ClassInitialize]
    public static void FindTables(TestContext context)
    {
        // CantripChance scales its tables by these when it loads: use the defaults instead of asking the database
        string[] dropRates =
        [
            "cantrip_drop_rate",
            "minor_cantrip_drop_rate",
            "major_cantrip_drop_rate",
            "epic_cantrip_drop_rate",
            "legendary_cantrip_drop_rate",
        ];

        foreach (var dropRate in dropRates)
        {
            PropertyManager.ModifyDouble(dropRate, DefaultPropertyManager.DefaultDoubleProperties[dropRate].Item, true);
        }

        tables = [];

        foreach (var type in typeof(WorldObject).Assembly.GetTypes())
        {
            if (type.ContainsGenericParameters || DatabaseTables.Contains(type))
            {
                continue;
            }

            var fields = type.GetFields(
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly
            );

            foreach (var field in fields.Where(f => MayHoldChanceTable(f.FieldType)))
            {
                Collect($"{type.Name}.{field.Name}", field.GetValue(null));
            }
        }
    }

    [TestMethod]
    public void FindsTheTables()
    {
        // so a change to how they are declared can't quietly leave nothing to check
        Assert.IsTrue(tables.Count > 400, $"only found {tables.Count} chance tables");
    }

    [TestMethod]
    public void ChanceTablesAddUpToOne()
    {
        // empty tables are NoTableIsEmpty's business
        var wrong = tables
            .Where(t => GetTableType(t.Table) == ChanceTableType.Chance && GetEntries(t.Table).Any())
            .Select(t => (t.Name, Total: GetEntries(t.Table).Sum(e => (decimal)e.Chance)))
            .Where(t => Math.Abs(1.0M - t.Total) > Threshold)
            .Select(t => $"{t.Name} adds up to {t.Total}")
            .ToList();

        Assert.AreEqual(0, wrong.Count, "\n" + string.Join("\n", wrong));
    }

    [TestMethod]
    public void WeightTablesHaveSomethingToRoll()
    {
        var wrong = tables
            .Where(t => GetTableType(t.Table) == ChanceTableType.Weight)
            .Where(t => GetEntries(t.Table).Sum(e => e.Chance) <= 0)
            .Select(t => t.Name)
            .ToList();

        Assert.AreEqual(0, wrong.Count, "\n" + string.Join("\n", wrong));
    }

    [TestMethod]
    public void NoTableHasANegativeChance()
    {
        var wrong = tables
            .SelectMany(t =>
                GetEntries(t.Table).Where(e => e.Chance < 0).Select(e => $"{t.Name}: {e.Result} {e.Chance}")
            )
            .ToList();

        Assert.AreEqual(0, wrong.Count, "\n" + string.Join("\n", wrong));
    }

    [TestMethod]
    public void NoTableIsEmpty()
    {
        // rolling an empty table throws. SpellSelectionTable.Roll checks first: an empty group is an item type with no spells.
        var wrong = tables
            .Where(t => !t.Name.StartsWith($"{nameof(SpellSelectionTable)}."))
            .Where(t => !GetEntries(t.Table).Any(e => e.Chance > 0))
            .Select(t => t.Name)
            .ToList();

        Assert.AreEqual(0, wrong.Count, "\n" + string.Join("\n", wrong));
    }

    [TestMethod]
    public void SpellSelection_EveryItemTypeCanRollItsSpells()
    {
        // LootGenerationFactory.GetSpellSelectionCode_Dynamic picks codes 1 - 21
        for (var spellCode = 1; spellCode <= 21; spellCode++)
        {
            try
            {
                for (var i = 0; i < 100; i++)
                {
                    SpellSelectionTable.Roll(spellCode);
                }
            }
            catch (Exception ex)
            {
                Assert.Fail($"spell code {spellCode}: {ex.GetType().Name}: {ex.Message}");
            }
        }
    }

    [TestMethod]
    public void SpellSelection_ClothingAndDinnerwareHaveNoSpells()
    {
        // their groups are empty: RollEnchantments ignores Undef, so they get no spells
        Assert.AreEqual(SpellId.Undef, SpellSelectionTable.Roll(12));
        Assert.AreEqual(SpellId.Undef, SpellSelectionTable.Roll(16));
    }

    private static bool IsChanceTable(Type type)
    {
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ChanceTable<>);
    }

    private static bool MayHoldChanceTable(Type type)
    {
        if (IsChanceTable(type))
        {
            return true;
        }

        if (type.IsArray)
        {
            return MayHoldChanceTable(type.GetElementType());
        }

        return type.IsGenericType && type.GetGenericArguments().Any(MayHoldChanceTable);
    }

    private static void Collect(string name, object value)
    {
        switch (value)
        {
            case null:
                return;

            case var table when IsChanceTable(table.GetType()):
                tables.Add((name, table));
                return;

            case IDictionary dictionary:
                foreach (DictionaryEntry entry in dictionary)
                {
                    Collect($"{name}[{entry.Key}]", entry.Value);
                }
                return;

            case IEnumerable enumerable:
                var i = 0;
                foreach (var item in enumerable)
                {
                    Collect($"{name}[{i++}]", item);
                }
                return;
        }
    }

    private static ChanceTableType GetTableType(object table)
    {
        return (ChanceTableType)
            table.GetType().GetField("TableType", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(table);
    }

    private static IEnumerable<(object Result, float Chance)> GetEntries(object table)
    {
        foreach (var entry in (IEnumerable)table)
        {
            var tuple = (ITuple)entry;
            yield return (tuple[0], (float)tuple[1]);
        }
    }
}
