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

    private const uint IconA = 0x06001000;
    private const uint IconB = 0x06002000;
    private const uint SetupA = 0x02000100;
    private const uint SetupB = 0x02000200;
    private const uint ScriptA = 0x33000100;
    private const uint ScriptB = 0x33000200;

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
        Assert.AreEqual(new ItemStyle("Shadow", null, new ItemLook(LookB)), styles[0]);
        Assert.AreEqual(new ItemStyle("Pumpkin", "ACCOUNT_Halloween", new ItemLook(LookC)), styles[1]);
        Assert.AreEqual(new ItemStyle(null, null, new ItemLook(LookD)), styles[2]);
        Assert.AreEqual(0, ItemStyles.Parse(null).Count);
    }

    [TestMethod]
    public void Parse_ReadsChanges()
    {
        var styles = ItemStyles.Parse(
            "|Crimson||palette=14, shade=0.25;"
                + "0x10000200|Frost|Event|SETUP=0x02000200,script=0x33000200,icon=0x06002000;"
                + "|Plain||script=0"
        );

        Assert.AreEqual(3, styles.Count);
        Assert.AreEqual(new ItemLook(PaletteTemplate: 14, Shade: 0.25), styles[0].Look);
        Assert.AreEqual(new ItemLook(LookB, Icon: IconB, Setup: SetupB, PhysicsScript: ScriptB), styles[1].Look);
        Assert.AreEqual("Event", styles[1].QuestFlag);
        Assert.AreEqual(new ItemLook(PhysicsScript: ItemLook.NoScript), styles[2].Look);
    }

    [TestMethod]
    public void Parse_SkipsChangesOfTheWrongKind()
    {
        var styles = ItemStyles.Parse(
            "0x06001000|IconAsClothing;|BadSetup||setup=0x06001000;|BadIcon||icon=0x02000100;"
                + "|BadShade||shade=2;|BadKey||color=3;|BadScript||script=0x02000100;"
                + "|NoValues|;|Good||palette=3"
        );

        // "|NoValues|" changes nothing and only names the default look, so it is kept.
        CollectionAssert.AreEqual(new[] { "NoValues", "Good" }, styles.Select(s => s.Name).ToArray());
    }

    [TestMethod]
    public void GetStyles_StartsWithTheDefaultThenAlternateThenItemStyles()
    {
        var item = CreateItem(LookA, LookB, "0x10000300|Pumpkin|Event;0x10000400");
        var styles = ItemStyles.GetStyles(item);

        CollectionAssert.AreEqual(
            new uint?[] { LookA, LookB, LookC, LookD },
            styles.Select(s => s.Look.ClothingBase).ToArray()
        );
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
        Assert.AreEqual("Classic", styles[0].Name);
        Assert.IsNull(styles[0].QuestFlag);
        Assert.AreEqual(LookA, styles[0].Look.ClothingBase);
        Assert.AreEqual("Shadow", styles[1].Name);
    }

    [TestMethod]
    public void GetStyles_LetsItemStylesNameTheAlternateButNotLockIt()
    {
        var item = CreateItem(LookA, LookB, "0x10000200|Shadow|Event;0x10000200|Again;0x10000300|Pumpkin");
        var styles = ItemStyles.GetStyles(item);

        Assert.AreEqual(3, styles.Count);
        Assert.AreEqual("Shadow", styles[1].Name);
        Assert.IsNull(styles[1].QuestFlag);
        Assert.AreEqual("Pumpkin", styles[2].Name);
    }

    [TestMethod]
    public void GetStyles_FillsWhatAStyleLeavesOutFromTheDefaultLook()
    {
        var item = CreateItem(LookA, null, "|Crimson||palette=14;0x10000200|Shadow||icon=0x06002000");
        item.PaletteTemplate = 3;
        item.Shade = 0.5;
        item.IconId = IconA;

        var styles = ItemStyles.GetStyles(item);

        Assert.AreEqual(new ItemLook(LookA, 14, 0.5, IconA), styles[1].Look);
        Assert.AreEqual(new ItemLook(LookB, 3, 0.5, IconB), styles[2].Look);
    }

    [TestMethod]
    public void LockedStyles_AreOnlyAvailableWithTheirQuest()
    {
        var item = CreateItem(LookA, null, "0x10000200|Pumpkin|Event");

        Assert.IsFalse(ItemStyles.CanSwitch(item, NoQuests));
        Assert.AreEqual(1, ItemStyles.GetAvailableStyles(item, NoQuests).Count);
        Assert.IsNull(ItemStyles.FindAvailableStyle(item, "Pumpkin", NoQuests));

        Assert.IsTrue(ItemStyles.CanSwitch(item, Quests("event")));
        Assert.AreEqual(LookB, ItemStyles.FindAvailableStyle(item, "pumpkin", Quests("Event")).Look.ClothingBase);
        Assert.AreEqual(LookB, ItemStyles.FindAvailableStyle(item, "2", Quests("Event")).Look.ClothingBase);
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
    public void NextStyle_TellsApartStylesThatShareAClothingBase()
    {
        var item = CreateItem(LookA, null, "|Crimson||palette=14;|Midnight||palette=20");
        item.PaletteTemplate = 3;

        var seen = new List<int?>();
        for (var i = 0; i < 3; i++)
        {
            Assert.IsTrue(ItemStyles.Apply(item, ItemStyles.GetNextStyle(item, NoQuests)));
            seen.Add(item.PaletteTemplate);
            Assert.AreEqual(LookA, item.ClothingBase);
        }

        CollectionAssert.AreEqual(new int?[] { 14, 20, 3 }, seen);
        Assert.IsNull(item.ItemStyleDefaultLook);
    }

    [TestMethod]
    public void Apply_RemembersTheDefaultAndClearsItOnReturn()
    {
        var item = CreateItem(LookA, null, "0x10000200|Shadow;0x10000300|Pumpkin");

        Assert.IsTrue(ItemStyles.Apply(item, ItemStyles.FindAvailableStyle(item, "Pumpkin", NoQuests)));
        Assert.AreEqual(LookC, item.ClothingBase);
        Assert.AreEqual(LookA, ItemStyles.GetDefaultLook(item).ClothingBase);

        Assert.IsTrue(ItemStyles.Apply(item, ItemStyles.FindAvailableStyle(item, "Shadow", NoQuests)));
        Assert.AreEqual(LookB, item.ClothingBase);
        Assert.AreEqual(LookA, ItemStyles.GetDefaultLook(item).ClothingBase);

        Assert.IsTrue(ItemStyles.Apply(item, ItemStyles.FindAvailableStyle(item, "default", NoQuests)));
        Assert.AreEqual(LookA, item.ClothingBase);
        Assert.IsNull(item.ItemStyleDefaultLook);

        Assert.IsFalse(ItemStyles.Apply(item, ItemStyles.FindAvailableStyle(item, "1", NoQuests)));
    }

    [TestMethod]
    public void Apply_ChangesEveryFieldAndPutsEveryOneBack()
    {
        var item = CreateItem(LookA, null, "0x10000200|Frost||palette=14,shade=0.75,icon=0x06002000,setup=0x02000200,script=0x33000200");
        item.PaletteTemplate = 3;
        item.IconId = IconA;
        item.SetupTableId = SetupA;

        // The default has no Shade and no physics script: going back must remove the style's, not keep them.
        var original = ItemLook.Of(item);

        Assert.IsTrue(ItemStyles.Apply(item, ItemStyles.FindAvailableStyle(item, "Frost", NoQuests)));
        Assert.AreEqual(new ItemLook(LookB, 14, 0.75, IconB, SetupB, ScriptB), ItemLook.Of(item));

        Assert.IsTrue(ItemStyles.Apply(item, ItemStyles.FindAvailableStyle(item, "default", NoQuests)));
        Assert.AreEqual(original, ItemLook.Of(item));
        Assert.IsNull(item.Shade);
        Assert.IsNull(item.GetProperty(PropertyDataId.PhysicsScript));
        Assert.IsNull(item.ItemStyleDefaultLook);
    }

    [TestMethod]
    public void Apply_ScriptZeroRemovesTheDefaultsScript()
    {
        var item = CreateItem(null, null, "|Plain||script=0");
        item.SetupTableId = SetupA;
        item.SetProperty(PropertyDataId.PhysicsScript, ScriptA);

        Assert.IsTrue(ItemStyles.Apply(item, ItemStyles.FindAvailableStyle(item, "Plain", NoQuests)));
        Assert.IsNull(item.GetProperty(PropertyDataId.PhysicsScript));

        Assert.IsTrue(ItemStyles.Apply(item, ItemStyles.FindAvailableStyle(item, "default", NoQuests)));
        Assert.AreEqual(ScriptA, item.GetProperty(PropertyDataId.PhysicsScript));
    }

    [TestMethod]
    public void ItemsWithoutAClothingBase_CanSwitchSetupAndEffect()
    {
        // A weapon with no clothing table.
        var item = CreateItem(null, null, "|Frost|Event|setup=0x02000200");
        item.SetupTableId = SetupA;

        Assert.IsFalse(ItemStyles.CanSwitch(item, NoQuests));
        Assert.IsTrue(ItemStyles.Apply(item, ItemStyles.GetNextStyle(item, Quests("Event"))));
        Assert.AreEqual(SetupB, item.SetupTableId);
        Assert.IsNull(item.ClothingBase);

        // Traded to someone without the flag.
        Assert.IsTrue(ItemStyles.Apply(item, ItemStyles.GetNextStyle(item, NoQuests)));
        Assert.AreEqual(SetupA, item.SetupTableId);
    }

    [TestMethod]
    public void DefaultLook_RoundTripsThroughItsProperty()
    {
        var look = new ItemLook(LookA, 7, 0.123456789012345, IconA, SetupA, ScriptA);

        Assert.IsTrue(ItemLook.TryRead(look.Write(), out var read, out _));
        Assert.AreEqual(look, read);

        Assert.IsTrue(ItemLook.TryRead(new ItemLook().Write(), out var empty, out _));
        Assert.IsTrue(empty.IsEmpty);
    }

    [TestMethod]
    public void ItemSwitchedBeforeTheWholeLookWasKept_StillGoesBack()
    {
        var item = CreateItem(LookB, null, "0x10000200|Shadow");
        item.ItemStyleDefaultClothingBase = LookA;

        Assert.AreEqual(LookA, ItemStyles.GetDefaultLook(item).ClothingBase);
        Assert.AreEqual("Shadow", ItemStyles.GetCurrentStyle(item).Name);

        Assert.IsTrue(ItemStyles.Apply(item, ItemStyles.GetNextStyle(item, NoQuests)));
        Assert.AreEqual(LookA, item.ClothingBase);
        Assert.IsNull(item.ItemStyleDefaultClothingBase);
        Assert.IsNull(item.ItemStyleDefaultLook);
    }

    [TestMethod]
    public void ItemShowingALockedStyle_CanAlwaysGoBackToTheDefault()
    {
        var item = CreateItem(LookA, null, "0x10000200|Pumpkin|Event");
        Assert.IsTrue(ItemStyles.Apply(item, ItemStyles.FindAvailableStyle(item, "Pumpkin", Quests("Event"))));

        // Traded to someone without the flag.
        Assert.IsTrue(ItemStyles.CanSwitch(item, NoQuests));
        var next = ItemStyles.GetNextStyle(item, NoQuests);
        Assert.AreEqual(LookA, next.Look.ClothingBase);
        Assert.IsTrue(ItemStyles.Apply(item, next));
        Assert.IsNull(item.ItemStyleDefaultLook);
    }

    [TestMethod]
    public void ItemsWithoutOtherLooks_CantSwitch()
    {
        Assert.IsFalse(ItemStyles.CanSwitch(CreateItem(LookA, null, null), NoQuests));
        Assert.IsFalse(ItemStyles.CanSwitch(CreateItem(LookA, LookA, null), NoQuests));
        Assert.IsFalse(ItemStyles.CanSwitch(CreateItem(LookA, null, "0x10000100|Classic"), NoQuests));
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
