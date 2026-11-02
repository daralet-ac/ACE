using System;
using System.Linq;
using ACE.Server.Entity.PlayerUi;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ACE.Server.Tests;

[TestClass]
public class SharedItemStoreTests
{
    private static readonly DateTime Start = new(2026, 10, 31, 12, 0, 0, DateTimeKind.Utc);

    private static PlayerUiCorpseItem Item(uint guid, string name)
    {
        return new PlayerUiCorpseItem(guid, name, 1, 100, 50, true, true, "Steel", 5.5, null, null);
    }

    private static SharedItemStore Store(int capacity = 10)
    {
        return new SharedItemStore(capacity, TimeSpan.FromDays(1));
    }

    private static string Key(string owner, string name) => SharedItemStore.Key(owner, name);

    [TestMethod]
    public void ALinkedItemIsFoundByWhoSaidItAndItsName()
    {
        var store = Store();

        var shared = store.Add("Bob", 0x200, Item(7, "Steel Sword"), Start);

        Assert.AreEqual("Steel Sword", shared.Name);
        Assert.AreEqual("Bob", shared.Owner);
        Assert.AreEqual(0x200, shared.Slots);

        var key = Key("Bob", "Steel Sword");
        var found = store.Find(key, Start.AddHours(1));
        Assert.AreEqual("Steel Sword", found.Item.Name);
        Assert.AreEqual(key, found.Key, "the plugin knows the answer by the word it asked with");
    }

    [TestMethod]
    public void CaseAndAStaffCharactersPlusDontMatter()
    {
        var store = Store();
        store.Add("+Archer town", 0, Item(7, "Steel Sword"), Start);

        Assert.IsNotNull(store.Find(Key("Archer Town", "steel sword"), Start));
        Assert.IsNotNull(store.Find(Key("+Archer town", "Steel Sword"), Start));
    }

    [TestMethod]
    public void NobodyElsesNameFindsIt()
    {
        var store = Store();
        store.Add("Bob", 0, Item(7, "Steel Sword"), Start);

        Assert.IsNull(store.Find(Key("Ann", "Steel Sword"), Start));
        Assert.IsNull(store.Find(Key("Bob", "Gold Sword"), Start));
    }

    [TestMethod]
    public void TheLatestItemOfANameIsTheLink()
    {
        var store = Store();
        store.Add("Bob", 0, Item(7, "Sword"), Start);
        store.Add("Bob", 0, Item(8, "Sword"), Start.AddMinutes(10));

        Assert.AreEqual(8u, store.Find(Key("Bob", "Sword"), Start.AddMinutes(11)).Item.Guid);
        Assert.AreEqual(1, store.Count);
    }

    [TestMethod]
    public void LinksAreForgottenAfterADay()
    {
        var store = Store();
        store.Add("Bob", 0, Item(7, "Sword"), Start);

        Assert.IsNotNull(store.Find(Key("Bob", "Sword"), Start.AddHours(23)));
        Assert.IsNull(store.Find(Key("Bob", "Sword"), Start.AddDays(1)));
        Assert.AreEqual(0, store.Count);
    }

    [TestMethod]
    public void LinkingItAgainStartsItsDayAgain()
    {
        var store = Store();
        store.Add("Bob", 0, Item(7, "Sword"), Start);
        store.Add("Bob", 0, Item(7, "Sword"), Start.AddHours(23));

        // the first entry's day ending mustn't take the second with it
        store.Add("Bob", 0, Item(9, "Other"), Start.AddHours(25));

        Assert.IsNotNull(store.Find(Key("Bob", "Sword"), Start.AddHours(25)));
    }

    [TestMethod]
    public void OnlyTheNewestAreKept()
    {
        var store = Store(capacity: 2);

        store.Add("Bob", 0, Item(1, "One"), Start);
        store.Add("Bob", 0, Item(2, "Two"), Start);
        store.Add("Bob", 0, Item(3, "Three"), Start);

        Assert.IsNull(store.Find(Key("Bob", "One"), Start));
        Assert.IsNotNull(store.Find(Key("Bob", "Two"), Start));
        Assert.IsNotNull(store.Find(Key("Bob", "Three"), Start));
        Assert.AreEqual(2, store.Count);
    }

    [TestMethod]
    public void LinkingOneItemOverAndOverHoldsOnlyTheLatest()
    {
        var store = Store();

        for (var i = 0; i < 1000; i++)
        {
            store.Add("Bob", 0, Item(7, "Sword"), Start.AddSeconds(i));
        }

        Assert.AreEqual(1, store.Count);
        Assert.AreEqual(1, store.Queued, "replaced links mustn't stay in memory for their day");
    }

    [TestMethod]
    public void ForgottenLinksLeaveTheQueueToo()
    {
        var store = Store();
        store.Add("Bob", 0, Item(7, "Sword"), Start);

        Assert.IsNull(store.Find(Key("Bob", "Sword"), Start.AddDays(1)));
        Assert.AreEqual(0, store.Queued);
    }

    [TestMethod]
    public void OnePlayersLinksCantPushOutAnothers()
    {
        var store = new SharedItemStore(capacity: 4, TimeSpan.FromDays(1), maxPerOwner: 2);
        store.Add("Ann", 0, Item(1, "Ann's Sword"), Start);

        for (var i = 0; i < 100; i++)
        {
            store.Add("Bob", 0, Item((uint)i, $"Item {i}"), Start.AddSeconds(i));
        }

        Assert.IsNotNull(store.Find(Key("Ann", "Ann's Sword"), Start.AddMinutes(5)));
        Assert.IsNotNull(store.Find(Key("Bob", "Item 99"), Start.AddMinutes(5)));
        Assert.IsNotNull(store.Find(Key("Bob", "Item 98"), Start.AddMinutes(5)));
        Assert.IsNull(store.Find(Key("Bob", "Item 97"), Start.AddMinutes(5)));
        Assert.AreEqual(3, store.Count);
    }

    [TestMethod]
    public void AStaffCharactersPlusIsTheSameOwner()
    {
        var store = new SharedItemStore(capacity: 10, TimeSpan.FromDays(1), maxPerOwner: 1);

        store.Add("+Bob", 0, Item(1, "One"), Start);
        store.Add("Bob", 0, Item(2, "Two"), Start);

        Assert.IsNull(store.Find(Key("Bob", "One"), Start));
        Assert.IsNotNull(store.Find(Key("Bob", "Two"), Start));
    }

    [TestMethod]
    public void ALinkCanBePassedOnAndStillSaysWhoLinkedIt()
    {
        var store = Store();
        store.Add("Bob", 0, Item(7, "Sword"), Start);

        var passed = store.PassOn(Key("Bob", "Sword"), "Ann", Start.AddHours(1));
        Assert.AreEqual("Sword", passed.Name);

        var found = store.Find(Key("Ann", "Sword"), Start.AddHours(1));
        Assert.AreEqual("Bob", found.Owner);
        Assert.AreEqual(7u, found.Item.Guid);
        Assert.IsNull(store.PassOn(Key("Bob", "Gone"), "Ann", Start));
    }

    [TestMethod]
    public void NamesAreCleanedToStayALink()
    {
        Assert.AreEqual("Gold Sword", SharedItemStore.LinkName("Gold [Sword]"));
        Assert.AreEqual("Pyreal #1 Mug", SharedItemStore.LinkName(" Pyreal #1 Mug "));
        Assert.AreEqual("Item", SharedItemStore.LinkName(null));
        Assert.AreEqual(SharedItemStore.MaxName, SharedItemStore.LinkName(new string('a', 200)).Length);
    }

    [TestMethod]
    public void KeysAreOneWordOfSafeLetters()
    {
        // what base64 would write as + and /
        var key = Key("Bob", "ÿþý>>>???");

        Assert.IsTrue(key.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_'), key);
    }

    [TestMethod]
    public void AWordThatIsntAKeyFindsNothing()
    {
        var store = Store();

        Assert.IsNull(store.Find("not base64!", Start));
        Assert.IsNull(store.Find(null, Start));
        Assert.IsNull(store.Find(new string('a', SharedItemStore.MaxKeyLength + 1), Start));
    }

    [TestMethod]
    public void Lookup_SaysWhichLinksAreMissing()
    {
        var missing = Key("Nobody Here", "Nothing");
        var lookup = PlayerUiItemShares.Lookup([missing, missing, " "]);

        Assert.AreEqual(0, lookup.Items.Count);
        CollectionAssert.AreEqual(new[] { missing }, lookup.Missing);
    }

    /// <summary>
    /// The names the plugin's ShareReplyInfo, SharedItemInfo and SharedLookupInfo read (Feed/SharedItemInfo.cs in
    /// daralet-toolkit).
    /// </summary>
    [TestMethod]
    public void SharedJsonHasTheNamesThePluginReads()
    {
        var shared = new PlayerUiSharedItem("Ym9i", "Steel Sword", "Bob", 512, Item(7, "Steel Sword"));
        var reply = new PlayerUiShareReply(7, null, shared);

        var json = PlayerUiProtocol.BuildMessages("shareitem", reply, omitNulls: true).JoinedJson();

        Assert.AreEqual(
            "{\"guid\":7,\"shared\":{\"key\":\"Ym9i\",\"name\":\"Steel Sword\",\"owner\":\"Bob\",\"slots\":512,\"item\":{\"guid\":7,"
                + "\"name\":\"Steel Sword\",\"stackSize\":1,\"value\":100,\"burden\":50,\"salvageable\":true,"
                + "\"sellable\":true,\"material\":\"Steel\",\"workmanship\":5.5}}}",
            json
        );

        var lookup = PlayerUiProtocol.BuildMessages("shared", new PlayerUiSharedLookup([], ["Ym9i"]), omitNulls: true)
            .JoinedJson();

        Assert.AreEqual("{\"items\":[],\"missing\":[\"Ym9i\"]}", lookup);
    }
}
