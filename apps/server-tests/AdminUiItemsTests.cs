using System.Collections.Generic;
using ACE.Database.Models.Shard;
using ACE.Entity.Enum;
using ACE.Server.Entity.AdminUi;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ACE.Server.Tests;

[TestClass]
public class AdminUiItemsTests
{
    private const uint Player = 0x50000001;
    private const uint Pack = 0x80000010;
    private const uint Bank = 0x80000020;
    private const uint Corpse = 0x80000030;

    [TestMethod]
    public void ReadsTheModeAndTheRestAsText()
    {
        Assert.IsTrue(AdminUiItems.TryParseRequest(new[] { "items", "Name", "Iron", "Jitte" }, AdminUiItems.SearchModes, out var mode, out var text));
        Assert.AreEqual("name", mode);
        Assert.AreEqual("Iron Jitte", text);

        Assert.IsFalse(AdminUiItems.TryParseRequest(new[] { "items", "name" }, AdminUiItems.SearchModes, out _, out _));
        Assert.IsFalse(AdminUiItems.TryParseRequest(new[] { "items", "colour", "red" }, AdminUiItems.SearchModes, out _, out _));
        Assert.IsFalse(AdminUiItems.TryParseRequest(new[] { "items" }, AdminUiItems.SearchModes, out _, out _));
    }

    [TestMethod]
    public void ReadsGuidsAsTheGameShowsThem()
    {
        Assert.IsTrue(AdminUiItems.TryParseGuid("0x80001234", out var guid));
        Assert.AreEqual(0x80001234u, guid);

        // eight digits is a guid in hex, as the game prints it, without the 0x
        Assert.IsTrue(AdminUiItems.TryParseGuid("80001234", out guid));
        Assert.AreEqual(0x80001234u, guid);

        Assert.IsTrue(AdminUiItems.TryParseGuid("2147488308", out guid));
        Assert.AreEqual(2147488308u, guid);

        Assert.IsTrue(AdminUiItems.TryParseGuid("8000ABCD", out guid));
        Assert.AreEqual(0x8000ABCDu, guid);

        Assert.IsFalse(AdminUiItems.TryParseGuid("Iron Jitte", out _));
    }

    [TestMethod]
    public void LocatesItemsOnACharacter()
    {
        var parents = new Dictionary<uint, ItemSearchRow>
        {
            [Pack] = new ItemSearchRow { Id = Pack, Name = "Backpack", ContainerId = Player },
        };

        Assert.AreEqual((AdminUiItems.PlaceEquipped, (string)null, (uint?)Player, (uint?)null),
            AdminUiItems.Locate(new ItemSearchRow { WielderId = Player }, parents));
        Assert.AreEqual((AdminUiItems.PlacePack, (string)null, (uint?)Player, (uint?)null),
            AdminUiItems.Locate(new ItemSearchRow { ContainerId = Player }, parents));
        Assert.AreEqual((AdminUiItems.PlacePack, "Backpack", (uint?)Player, (uint?)null),
            AdminUiItems.Locate(new ItemSearchRow { ContainerId = Pack }, parents));
    }

    /// <summary>
    /// Items in the bank carry the account's BankAccountId; items in the bank's side packs don't, the pack does.
    /// </summary>
    [TestMethod]
    public void LocatesItemsInABank()
    {
        var parents = new Dictionary<uint, ItemSearchRow>
        {
            [Pack] = new ItemSearchRow { Id = Pack, Name = "Weapons Pack", ContainerId = Bank, BankAccountId = 7 },
        };

        Assert.AreEqual((AdminUiItems.PlaceBank, (string)null, (uint?)null, (uint?)7),
            AdminUiItems.Locate(new ItemSearchRow { ContainerId = Bank, BankAccountId = 7 }, parents));
        Assert.AreEqual((AdminUiItems.PlaceBank, "Weapons Pack", (uint?)null, (uint?)7),
            AdminUiItems.Locate(new ItemSearchRow { ContainerId = Pack }, parents));
    }

    [TestMethod]
    public void LocatesItemsOnAPlayersCorpseOnly()
    {
        var parents = new Dictionary<uint, ItemSearchRow>
        {
            [Corpse] = new ItemSearchRow { Id = Corpse, WeenieType = (int)WeenieType.Corpse, VictimId = Player },
            [Pack] = new ItemSearchRow { Id = Pack, Name = "Backpack", ContainerId = Corpse },
            [0x80000040] = new ItemSearchRow { Id = 0x80000040, WeenieType = (int)WeenieType.Corpse, VictimId = 0x80000099 },
            [0x80000050] = new ItemSearchRow { Id = 0x80000050, WeenieType = (int)WeenieType.Storage },
        };

        Assert.AreEqual((AdminUiItems.PlaceCorpse, (string)null, (uint?)Player, (uint?)null),
            AdminUiItems.Locate(new ItemSearchRow { ContainerId = Corpse }, parents));
        Assert.AreEqual((AdminUiItems.PlaceCorpse, "Backpack", (uint?)Player, (uint?)null),
            AdminUiItems.Locate(new ItemSearchRow { ContainerId = Pack }, parents));

        // a monster's corpse, a house's chest, the ground, and a container the search didn't read
        Assert.IsNull(AdminUiItems.Locate(new ItemSearchRow { ContainerId = 0x80000040 }, parents));
        Assert.IsNull(AdminUiItems.Locate(new ItemSearchRow { ContainerId = 0x80000050 }, parents));
        Assert.IsNull(AdminUiItems.Locate(new ItemSearchRow(), parents));
        Assert.IsNull(AdminUiItems.Locate(new ItemSearchRow { ContainerId = 0x80000060 }, parents));
    }

    [TestMethod]
    public void ItemsAreGatedAtAdmin()
    {
        Assert.AreEqual(AccessLevel.Admin, AdminUiAccess.Tabs["items"]);
        Assert.AreEqual(AdminUiAccess.Tabs["items"], AdminUiAccess.Requests["items"]);
        Assert.AreEqual(AdminUiAccess.Tabs["items"], AdminUiAccess.Requests["itemhistory"]);
    }

    /// <summary>
    /// The plugin reads these names (daralet-admin-plugin src/DaraletAdmin/Feed/ItemModels.cs), so they can't change
    /// without it.
    /// </summary>
    [TestMethod]
    public void SearchJsonHasTheNamesThePluginReads()
    {
        var reply = new AdminUiItemSearch(
            "name",
            "jitte",
            new List<AdminUiItem>
            {
                new(0x80000001, 3001, "Iron Jitte", 1, "pack", "Backpack", Player, "Ann", "ann1", true),
            },
            true,
            null
        );

        var json = AdminUiProtocol.BuildMessages("items", reply).JoinedJson();

        foreach (
            var name in new[]
            {
                "\"mode\":\"name\"",
                "\"query\":\"jitte\"",
                "\"truncated\":true",
                "\"error\":null",
                "\"guid\":2147483649",
                "\"wcid\":3001",
                "\"name\":\"Iron Jitte\"",
                "\"amount\":1",
                "\"place\":\"pack\"",
                "\"container\":\"Backpack\"",
                "\"ownerGuid\":1342177281",
                "\"owner\":\"Ann\"",
                "\"account\":\"ann1\"",
                "\"online\":true",
            }
        )
        {
            StringAssert.Contains(json, name);
        }
    }

    [TestMethod]
    public void HistoryJsonHasTheNamesThePluginReads()
    {
        var reply = new AdminUiItemHistory(
            "guid",
            "0x80000001",
            new List<AdminUiItemTransfer>
            {
                new(90, "given", 0x80000001, 3001, "Iron Jitte", 1, "Ann", "ann1", "Bob", "bob1", null),
            },
            null
        );

        var json = AdminUiProtocol.BuildMessages("itemhistory", reply).JoinedJson();

        foreach (
            var name in new[]
            {
                "\"entries\":",
                "\"secondsAgo\":90",
                "\"kind\":\"given\"",
                "\"itemGuid\":2147483649",
                "\"item\":\"Iron Jitte\"",
                "\"from\":\"Ann\"",
                "\"fromAccount\":\"ann1\"",
                "\"to\":\"Bob\"",
                "\"toAccount\":\"bob1\"",
                "\"details\":null",
            }
        )
        {
            StringAssert.Contains(json, name);
        }
    }
}
