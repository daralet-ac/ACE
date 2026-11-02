using System;
using System.Collections.Generic;
using System.Linq;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Entity.Models;
using ACE.Server.Entity;
using ACE.Server.WorldObjects;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ACE.Server.Tests;

[TestClass]
public class ItemStylesTests
{
    private const uint LookA = 0x10000100;
    private const uint LookB = 0x10000200;
    private const uint LookC = 0x10000300;
    private const uint LookD = 0x10000400;

    private static readonly Func<string, bool> NoQuests = _ => false;

    private static Func<string, bool> Quests(params string[] flags)
    {
        return q => flags.Contains(q, StringComparer.OrdinalIgnoreCase);
    }

    [TestMethod]
    public void Parse_ReadsEntriesAndSkipsBadOnes()
    {
        var styles = ItemStyles.Parse(
            "0x10000200|Shadow\n 0x10000300 | Pumpkin | ACCOUNT_Halloween ;bogus|X;268436480"
        );

        Assert.AreEqual(3, styles.Count);
        Assert.AreEqual(new ItemStyle(LookB, "Shadow", null), styles[0]);
        Assert.AreEqual(new ItemStyle(LookC, "Pumpkin", "ACCOUNT_Halloween"), styles[1]);
        Assert.AreEqual(new ItemStyle(LookD, null, null), styles[2]);
        Assert.AreEqual(0, ItemStyles.Parse(null).Count);
    }

    [TestMethod]
    public void GetStyles_StartsWithTheDefaultThenAlternateThenItemStyles()
    {
        var item = CreateItem(LookA, LookB, "0x10000300|Pumpkin|Event;0x10000400");
        var styles = ItemStyles.GetStyles(item);

        CollectionAssert.AreEqual(new[] { LookA, LookB, LookC, LookD }, styles.Select(s => s.ClothingBase).ToArray());
        CollectionAssert.AreEqual(
            new[] { ItemStyles.DefaultName, ItemStyles.AlternateName, "Pumpkin", "Style 4" },
            styles.Select(s => s.Name).ToArray()
        );
    }

    [TestMethod]
    public void GetStyles_LetsItemStylesNameTheDefaultAndDropsRepeats()
    {
        var item = CreateItem(LookA, null, "0x10000100|Classic|Ignored;0x10000200|Shadow;0x10000200|Again");
        var styles = ItemStyles.GetStyles(item);

        Assert.AreEqual(2, styles.Count);
        Assert.AreEqual(new ItemStyle(LookA, "Classic", null), styles[0]);
        Assert.AreEqual("Shadow", styles[1].Name);
    }

    [TestMethod]
    public void LockedStyles_AreOnlyAvailableWithTheirQuest()
    {
        var item = CreateItem(LookA, null, "0x10000200|Pumpkin|Event");

        Assert.IsFalse(ItemStyles.CanSwitch(item, NoQuests));
        Assert.AreEqual(1, ItemStyles.GetAvailableStyles(item, NoQuests).Count);
        Assert.IsNull(ItemStyles.FindAvailableStyle(item, "Pumpkin", NoQuests));

        Assert.IsTrue(ItemStyles.CanSwitch(item, Quests("event")));
        Assert.AreEqual(LookB, ItemStyles.FindAvailableStyle(item, "pumpkin", Quests("Event")).ClothingBase);
        Assert.AreEqual(LookB, ItemStyles.FindAvailableStyle(item, "2", Quests("Event")).ClothingBase);
    }

    [TestMethod]
    public void NextStyle_CyclesThroughAvailableStylesSkippingLockedOnes()
    {
        var item = CreateItem(LookA, LookB, "0x10000300|Pumpkin|Event;0x10000400|Plain");

        var seen = new List<uint>();
        for (var i = 0; i < 4; i++)
        {
            Assert.IsTrue(ItemStyles.Apply(item, ItemStyles.GetNextStyle(item, NoQuests)));
            seen.Add(item.ClothingBase.Value);
        }

        CollectionAssert.AreEqual(new[] { LookB, LookD, LookA, LookB }, seen);
    }

    [TestMethod]
    public void Apply_RemembersTheDefaultAndClearsItOnReturn()
    {
        var item = CreateItem(LookA, null, "0x10000200|Shadow;0x10000300|Pumpkin");

        Assert.IsTrue(ItemStyles.Apply(item, ItemStyles.FindAvailableStyle(item, "Pumpkin", NoQuests)));
        Assert.AreEqual(LookC, item.ClothingBase);
        Assert.AreEqual(LookA, item.ItemStyleDefaultClothingBase);

        Assert.IsTrue(ItemStyles.Apply(item, ItemStyles.FindAvailableStyle(item, "Shadow", NoQuests)));
        Assert.AreEqual(LookB, item.ClothingBase);
        Assert.AreEqual(LookA, item.ItemStyleDefaultClothingBase);

        Assert.IsTrue(ItemStyles.Apply(item, ItemStyles.FindAvailableStyle(item, "default", NoQuests)));
        Assert.AreEqual(LookA, item.ClothingBase);
        Assert.IsNull(item.ItemStyleDefaultClothingBase);

        Assert.IsFalse(ItemStyles.Apply(item, ItemStyles.FindAvailableStyle(item, "1", NoQuests)));
    }

    [TestMethod]
    public void ItemShowingALockedStyle_CanAlwaysGoBackToTheDefault()
    {
        var item = CreateItem(LookA, null, "0x10000200|Pumpkin|Event");
        Assert.IsTrue(ItemStyles.Apply(item, ItemStyles.FindAvailableStyle(item, "Pumpkin", Quests("Event"))));

        // Traded to someone without the flag.
        Assert.IsTrue(ItemStyles.CanSwitch(item, NoQuests));
        var next = ItemStyles.GetNextStyle(item, NoQuests);
        Assert.AreEqual(LookA, next.ClothingBase);
        Assert.IsTrue(ItemStyles.Apply(item, next));
        Assert.IsNull(item.ItemStyleDefaultClothingBase);
    }

    [TestMethod]
    public void ItemsWithoutStylesOrAClothingBase_CantSwitch()
    {
        Assert.IsFalse(ItemStyles.CanSwitch(CreateItem(LookA, null, null), NoQuests));
        Assert.IsFalse(ItemStyles.CanSwitch(CreateItem(LookA, LookA, null), NoQuests));
        Assert.IsFalse(ItemStyles.CanSwitch(CreateItem(null, LookB, "0x10000300"), NoQuests));
        Assert.IsNull(ItemStyles.GetNextStyle(CreateItem(LookA, null, null), NoQuests));
        Assert.IsFalse(ItemStyles.CanSwitch(null, NoQuests));
    }

    [TestMethod]
    public void AlternateClothingBase_StillWorksAsATwoLookToggle()
    {
        var item = CreateItem(LookA, LookB, null);

        Assert.IsTrue(ItemStyles.Apply(item, ItemStyles.GetNextStyle(item, NoQuests)));
        Assert.AreEqual(LookB, item.ClothingBase);
        Assert.IsTrue(ItemStyles.Apply(item, ItemStyles.GetNextStyle(item, NoQuests)));
        Assert.AreEqual(LookA, item.ClothingBase);
        Assert.AreEqual(LookB, item.AlternateClothingBase);
    }

    private static WorldObject CreateItem(uint? clothingBase, uint? alternateClothingBase, string itemStyles)
    {
        var dids = new Dictionary<PropertyDataId, uint>();
        if (clothingBase != null)
        {
            dids[PropertyDataId.ClothingBase] = clothingBase.Value;
        }

        if (alternateClothingBase != null)
        {
            dids[PropertyDataId.AlternateClothingBase] = alternateClothingBase.Value;
        }

        var strings = new Dictionary<PropertyString, string> { [PropertyString.Name] = "Styled Item" };
        if (itemStyles != null)
        {
            strings[PropertyString.ItemStyles] = itemStyles;
        }

        var weenie = new Weenie
        {
            WeenieClassId = 999998,
            ClassName = "itemstylestest",
            WeenieType = WeenieType.Generic,
            PropertiesInt = new Dictionary<PropertyInt, int> { [PropertyInt.ItemType] = (int)ItemType.Misc },
            PropertiesDID = dids,
            PropertiesString = strings,
        };

        return new GenericObject(weenie, new ObjectGuid(0x80000002));
    }
}
