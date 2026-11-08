using System.Collections.Generic;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Entity.Models;
using ACE.Server.Entity;
using ACE.Server.WorldObjects;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ACE.Server.Tests;

[TestClass]
public class WorkmanshipLooksTests
{
    private const uint Battered = 0x1000088A;
    private const uint Worn = 0x1000088B;
    private const uint Plain = 0x10000139;
    private const uint Fine = 0x1000088C;
    private const uint Pristine = 0x1000088D;
    private const uint Styled = 0x10000500;

    private const string List = "0x1000088A, 0x1000088B,0x10000139 ,0x1000088C,268437645";

    [TestMethod]
    public void Parse_ReadsFiveHexOrDecimalIds()
    {
        var entries = WorkmanshipLooks.Parse(List);
        CollectionAssert.AreEqual(new[] { Battered, Worn, Plain, Fine, Pristine }, System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(entries, e => e[0])));
        Assert.IsTrue(System.Array.TrueForAll(entries, e => e.Length == 1));
    }

    [TestMethod]
    public void Alternatives_EachItemKeepsOneOfTheEntrysLooks()
    {
        const uint BatteredB = 0x1000088E;
        var two = "0x1000088A|0x1000088E,0x1000088B,0x10000139,0x1000088C,0x1000088D";
        var seen = new HashSet<uint>();
        for (uint guid = 0x80000000; guid < 0x80000040; guid++)
        {
            var look = WorkmanshipLooks.Resolve(Plain, two, 1, guid);
            Assert.IsTrue(look == Battered || look == BatteredB, $"guid {guid:X8}");
            Assert.AreEqual(look, WorkmanshipLooks.Resolve(Plain, two, 2, guid), "the same item always draws the same look");
            seen.Add(look.Value);
        }

        Assert.AreEqual(2, seen.Count, "both looks are used");
        // The plain look is found among single entries, and either battered look counts as being in the list.
        Assert.AreEqual(Worn, WorkmanshipLooks.Resolve(BatteredB, two, 3, 0x80000001));
        Assert.IsNull(WorkmanshipLooks.Parse("0x1000088A|,0x1000088B,0x10000139,0x1000088C,0x1000088D"));
    }

    [TestMethod]
    public void TenEntries_GiveEachWorkmanshipItsOwnLook()
    {
        // A set that wears its own way: 1-3 battered, 4-6 worn, 7-8 plain (its ClothingBase need not be workmanship 6's),
        // 9-10 pristine.
        var ten = "0x1000088A,0x1000088A,0x1000088A,0x1000088B,0x1000088B,0x1000088B,0x10000139,0x10000139,0x1000088D,0x1000088D";
        uint[] expected = { Battered, Battered, Battered, Worn, Worn, Worn, Plain, Plain, Pristine, Pristine };
        for (var workmanship = 1; workmanship <= 10; workmanship++)
        {
            Assert.AreEqual(expected[workmanship - 1], WorkmanshipLooks.Resolve(Plain, ten, workmanship), $"workmanship {workmanship}");
        }

        Assert.AreEqual(Plain, WorkmanshipLooks.Resolve(Plain, ten, null));
        Assert.AreEqual(Styled, WorkmanshipLooks.Resolve(Styled, ten, 1));
    }

    [TestMethod]
    public void Parse_RefusesAnythingButFiveOrTenIds()
    {
        Assert.IsNull(WorkmanshipLooks.Parse(null));
        Assert.IsNull(WorkmanshipLooks.Parse(" "));
        Assert.IsNull(WorkmanshipLooks.Parse("0x1000088A,0x1000088B,0x10000139,0x1000088C"));
        Assert.IsNull(WorkmanshipLooks.Parse("0x1000088A,0x1000088B,0x10000139,0x1000088C,0x1000088D,0x1000088E"));
        Assert.IsNull(WorkmanshipLooks.Parse("0x1000088A,0x1000088B,bogus,0x1000088C,0x1000088D"));
        Assert.IsNull(WorkmanshipLooks.Parse("0x1000088A,0x1000088B,0,0x1000088C,0x1000088D"));
    }

    [TestMethod]
    public void Level_PairsWorkmanshipIntoFiveLevels()
    {
        int[] expected = { 0, 0, 1, 1, 2, 2, 3, 3, 4, 4 };
        for (var workmanship = 1; workmanship <= 10; workmanship++)
        {
            Assert.AreEqual(expected[workmanship - 1], WorkmanshipLooks.Level(workmanship), $"workmanship {workmanship}");
        }

        Assert.AreEqual(2, WorkmanshipLooks.Level(null));
        Assert.AreEqual(2, WorkmanshipLooks.Level(0));
        Assert.AreEqual(4, WorkmanshipLooks.Level(14));
    }

    [TestMethod]
    public void Resolve_SwapsOnlyThePlainClothingBase()
    {
        Assert.AreEqual(Battered, WorkmanshipLooks.Resolve(Plain, List, 1));
        Assert.AreEqual(Fine, WorkmanshipLooks.Resolve(Plain, List, 8));
        Assert.AreEqual(Plain, WorkmanshipLooks.Resolve(Plain, List, null));
        // A /style look (or anything else) with its own ClothingBase draws as it is.
        Assert.AreEqual(Styled, WorkmanshipLooks.Resolve(Styled, List, 1));
        Assert.AreEqual(Plain, WorkmanshipLooks.Resolve(Plain, "bogus", 1));
        Assert.IsNull(WorkmanshipLooks.Resolve(null, List, 1));
    }

    [TestMethod]
    public void DrawnClothingBase_ReadsTheListFromTheWeenie()
    {
        var item = CreateItem(weenieList: List, ownList: null, workmanship: 10);
        Assert.AreEqual(Pristine, WorkmanshipLooks.DrawnClothingBase(item));

        item.ClothingBase = Styled;
        Assert.AreEqual(Styled, WorkmanshipLooks.DrawnClothingBase(item));
    }

    [TestMethod]
    public void DrawnClothingBase_FallsBackToTheItemsOwnList()
    {
        var item = CreateItem(weenieList: null, ownList: List, workmanship: 3);
        Assert.AreEqual(Worn, WorkmanshipLooks.DrawnClothingBase(item));

        var none = CreateItem(weenieList: null, ownList: null, workmanship: 3);
        Assert.AreEqual(Plain, WorkmanshipLooks.DrawnClothingBase(none));
    }

    private static WorldObject CreateItem(string weenieList, string ownList, int workmanship)
    {
        var strings = new Dictionary<PropertyString, string> { [PropertyString.Name] = "Worn Item" };
        if (weenieList != null)
        {
            strings[PropertyString.WorkmanshipClothingBases] = weenieList;
        }

        var weenie = new Weenie
        {
            WeenieClassId = 999997,
            ClassName = "workmanshiplookstest",
            WeenieType = WeenieType.Generic,
            PropertiesInt = new Dictionary<PropertyInt, int> { [PropertyInt.ItemType] = (int)ItemType.MeleeWeapon },
            PropertiesDID = new Dictionary<PropertyDataId, uint> { [PropertyDataId.ClothingBase] = Plain },
            PropertiesString = strings,
        };

        var item = new GenericObject(weenie, new ObjectGuid(0x80000003)) { ItemWorkmanship = workmanship };
        if (ownList != null)
        {
            item.SetProperty(PropertyString.WorkmanshipClothingBases, ownList);
        }

        return item;
    }
}
