using System;
using ACE.Entity;
using ACE.Server.Entity;
using ACE.Server.Managers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ACE.Server.Tests;

[TestClass]
public class InstanceManagerTests
{
    private Func<DateTime> previousClock;
    private Func<TimeSpan> previousTimeout;
    private DateTime now;

    private static readonly LandblockId Dungeon = new LandblockId(0x01E3FFFF);
    private static readonly LandblockId Neighbour = new LandblockId(0x01E4FFFF);

    [TestInitialize]
    public void UseAFakeClock()
    {
        previousClock = InstanceManager.UtcNow;
        previousTimeout = InstanceManager.EmptyTimeout;

        now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        InstanceManager.UtcNow = () => now;
        InstanceManager.EmptyTimeout = () => TimeSpan.FromMinutes(15);
    }

    [TestCleanup]
    public void RestoreTheClock()
    {
        InstanceManager.UtcNow = previousClock;
        InstanceManager.EmptyTimeout = previousTimeout;
    }

    private static Position At(uint cell)
    {
        return new Position(cell, 10f, 10f, 0f, 0f, 0f, 0f, 1f);
    }

    // The instance manager is static and shared by every test, so every test makes its own templates and only looks at its own instances
    private static InstanceTemplate NewTemplate(bool instanceOnly = false, params LandblockId[] footprint)
    {
        return new InstanceTemplate(
            "test-" + Guid.NewGuid(),
            footprint.Length > 0 ? footprint : new[] { Dungeon },
            At(0x01E30100),
            At(0xA9B40019),
            instanceOnly
        );
    }

    [TestMethod]
    public void InstanceTemplate_NeedsANameLandblocksAndAnEntryPosition()
    {
        Assert.ThrowsException<ArgumentException>(() => new InstanceTemplate(" ", new[] { Dungeon }, At(0x01E30100)));
        Assert.ThrowsException<ArgumentException>(() => new InstanceTemplate("x", new LandblockId[0], At(0x01E30100)));
        Assert.ThrowsException<ArgumentNullException>(() => new InstanceTemplate("x", new[] { Dungeon }, null));
        Assert.ThrowsException<ArgumentNullException>(() => new InstanceTemplate("x", null, At(0x01E30100)));
    }

    [TestMethod]
    public void InstanceTemplate_ListsEachLandblockOnceWhateverItsCellBits()
    {
        var template = new InstanceTemplate(
            "x",
            new[] { new LandblockId(0x01E30001), new LandblockId(0x01E3FFFF), new LandblockId(0x01E303B9) },
            At(0x01E30100)
        );

        Assert.AreEqual(1, template.Footprint.Count);
        Assert.IsTrue(template.Contains(new LandblockId(0x01E30100)));
        Assert.IsFalse(template.Contains(Neighbour));
    }

    [TestMethod]
    public void InstanceManager_GivesEveryInstanceItsOwnIdAndNeverTheIdOfThePersistentWorld()
    {
        var template = NewTemplate();

        var first = InstanceManager.Register(template);
        var second = InstanceManager.Register(template);

        Assert.AreNotEqual(Landblock.PersistentInstance, first.Id);
        Assert.AreNotEqual(first.Id, second.Id);
        Assert.IsTrue(second.Id > first.Id);
        Assert.AreSame(first, InstanceManager.Get(first.Id));
    }

    [TestMethod]
    public void InstanceManager_AnInstanceContainsOnlyTheLandblocksOfItsTemplate()
    {
        var instance = InstanceManager.Register(NewTemplate());

        Assert.IsTrue(InstanceManager.IsInFootprint(instance.Id, Dungeon));
        Assert.IsTrue(
            InstanceManager.IsInFootprint(instance.Id, new LandblockId(0x01E303B9)),
            "cell bits don't matter"
        );
        Assert.IsFalse(
            InstanceManager.IsInFootprint(instance.Id, Neighbour),
            "what is next to it in the world is not part of it"
        );
        Assert.IsFalse(
            InstanceManager.IsInFootprint(instance.Id + 1000, Dungeon),
            "an instance that doesn't exist contains nothing"
        );

        // the persistent world contains everything
        Assert.IsTrue(InstanceManager.IsInFootprint(Landblock.PersistentInstance, Neighbour));
    }

    [TestMethod]
    public void LandblockManager_NeverLoadsALandblockForAnInstanceThatDoesNotExistOrForAPlaceOutsideIt()
    {
        var instance = InstanceManager.Register(NewTemplate());

        Assert.IsNull(LandblockManager.GetLandblock(Dungeon, instance.Id + 1000, false), "no such instance");
        Assert.IsNull(LandblockManager.GetLandblock(Neighbour, instance.Id, false), "not part of the instance");
        Assert.IsFalse(LandblockManager.IsLoaded(Neighbour, instance.Id));
    }

    [TestMethod]
    public void InstanceManager_NobodyCanBeSentIntoAnInstanceThatIsShuttingDown()
    {
        var instance = InstanceManager.Register(NewTemplate());
        Assert.IsTrue(InstanceManager.CanEnter(instance.Id, Dungeon));
        Assert.IsFalse(InstanceManager.CanEnter(instance.Id, Neighbour));
        Assert.IsFalse(InstanceManager.CanEnter(instance.Id + 1000, Dungeon));
        Assert.IsTrue(InstanceManager.CanEnter(Landblock.PersistentInstance, Neighbour));

        Assert.IsTrue(InstanceManager.Close(instance.Id));

        Assert.IsTrue(instance.IsClosing);
        Assert.IsFalse(InstanceManager.CanEnter(instance.Id, Dungeon));
    }

    [TestMethod]
    public void InstanceManager_FindsTheInstanceOfATemplateThatBelongsToAnOwner()
    {
        var template = NewTemplate();
        var ownerA = new object();
        var ownerB = new object();

        var instance = InstanceManager.Register(template, ownerA);

        Assert.AreSame(instance, InstanceManager.Find(template, ownerA));
        Assert.IsNull(InstanceManager.Find(template, ownerB));

        InstanceManager.Close(instance.Id);
        Assert.IsNull(InstanceManager.Find(template, ownerA), "one that is shutting down can't be handed out again");
    }

    [TestMethod]
    public void InstanceManager_ATeleportStaysInTheInstanceOnlyIfItIsToSomewhereInsideIt()
    {
        var instance = InstanceManager.Register(NewTemplate());

        // a portal inside a dungeon
        Assert.AreEqual(instance.Id, InstanceManager.ResolveDestinationInstance(instance.Id, At(0x01E30200)));

        // a recall, a lifestone
        Assert.AreEqual(
            Landblock.PersistentInstance,
            InstanceManager.ResolveDestinationInstance(instance.Id, At(0xA9B40019))
        );

        // nobody gets into an instance from the persistent world by accident, even to the same coordinates
        Assert.AreEqual(
            Landblock.PersistentInstance,
            InstanceManager.ResolveDestinationInstance(Landblock.PersistentInstance, At(0x01E30200))
        );
    }

    [TestMethod]
    public void InstanceManager_AnInstanceNobodyEntersIsDeletedAfterTheTimeout()
    {
        var instance = InstanceManager.Register(NewTemplate());

        InstanceManager.Tick(now.AddMinutes(14), TimeSpan.FromMinutes(15));
        Assert.IsNotNull(InstanceManager.Get(instance.Id), "not yet");

        InstanceManager.Tick(now.AddMinutes(16), TimeSpan.FromMinutes(15));
        Assert.IsNull(InstanceManager.Get(instance.Id));
    }

    [TestMethod]
    public void InstanceManager_TheTimeoutOnlyStartsOnceTheLastPlayerHasLeft()
    {
        var instance = InstanceManager.Register(NewTemplate());

        InstanceManager.OnMemberEntered(instance.Id, 0x50000001);
        InstanceManager.OnMemberEntered(instance.Id, 0x50000002);
        Assert.AreEqual(2, instance.MemberCount);

        now = now.AddHours(3);
        InstanceManager.Tick(now, TimeSpan.FromMinutes(15));
        Assert.IsNotNull(InstanceManager.Get(instance.Id), "there are players in it, however long it has been open");

        InstanceManager.OnMemberLeft(instance.Id, 0x50000001);
        now = now.AddMinutes(20);
        InstanceManager.Tick(now, TimeSpan.FromMinutes(15));
        Assert.IsNotNull(InstanceManager.Get(instance.Id), "one is still in it");

        InstanceManager.OnMemberLeft(instance.Id, 0x50000002);
        now = now.AddMinutes(14);
        InstanceManager.Tick(now, TimeSpan.FromMinutes(15));
        Assert.IsNotNull(InstanceManager.Get(instance.Id), "the last one left 14 minutes ago");

        now = now.AddMinutes(2);
        InstanceManager.Tick(now, TimeSpan.FromMinutes(15));
        Assert.IsNull(InstanceManager.Get(instance.Id));
    }

    [TestMethod]
    public void InstanceManager_SomeoneComingBackRestartsTheTimeout()
    {
        var instance = InstanceManager.Register(NewTemplate());

        InstanceManager.OnMemberEntered(instance.Id, 0x50000001);
        InstanceManager.OnMemberLeft(instance.Id, 0x50000001);

        now = now.AddMinutes(10);
        InstanceManager.OnMemberEntered(instance.Id, 0x50000001);
        InstanceManager.OnMemberLeft(instance.Id, 0x50000001);

        now = now.AddMinutes(10);
        InstanceManager.Tick(now, TimeSpan.FromMinutes(15));
        Assert.IsNotNull(
            InstanceManager.Get(instance.Id),
            "20 minutes since it opened, but only 10 since the last player left"
        );

        now = now.AddMinutes(6);
        InstanceManager.Tick(now, TimeSpan.FromMinutes(15));
        Assert.IsNull(InstanceManager.Get(instance.Id));
    }

    [TestMethod]
    public void InstanceManager_AShutDownInstanceIsDeletedAsSoonAsItIsEmpty()
    {
        var instance = InstanceManager.Register(NewTemplate());

        InstanceManager.Close(instance.Id);

        // nobody is in it, so there is nothing to wait for and no reason to wait for the timeout
        InstanceManager.Tick(now.AddSeconds(1), TimeSpan.FromMinutes(15));
        Assert.IsNull(InstanceManager.Get(instance.Id));
    }

    [TestMethod]
    public void InstanceManager_TheGuidsOfPlayersWhoAreNotOnlineDoNotKeepAShutDownInstanceOpen()
    {
        var instance = InstanceManager.Register(NewTemplate());
        InstanceManager.OnMemberEntered(instance.Id, 0x5FFFFFF0); // nobody is online with this guid

        InstanceManager.Close(instance.Id);

        Assert.AreEqual(0, instance.MemberCount);

        InstanceManager.Tick(now.AddSeconds(1), TimeSpan.FromMinutes(15));
        Assert.IsNull(InstanceManager.Get(instance.Id));
    }

    [TestMethod]
    public void InstanceManager_MembersOfThePersistentWorldAndUnknownInstancesAreIgnored()
    {
        var instance = InstanceManager.Register(NewTemplate());

        InstanceManager.OnMemberEntered(Landblock.PersistentInstance, 0x50000001);
        InstanceManager.OnMemberEntered(instance.Id + 1000, 0x50000001);
        InstanceManager.OnMemberLeft(Landblock.PersistentInstance, 0x50000001);
        InstanceManager.OnMemberLeft(instance.Id + 1000, 0x50000001);

        Assert.AreEqual(0, instance.MemberCount);
    }

    [TestMethod]
    public void InstanceManager_OnlyTemplatesThatAreInstanceOnlyReserveTheirLandblocks()
    {
        var reservedBlock = new LandblockId(0x7A7AFFFF);
        var ordinaryBlock = new LandblockId(0x7B7BFFFF);

        InstanceManager.RegisterTemplate(NewTemplate(true, reservedBlock));
        InstanceManager.RegisterTemplate(NewTemplate(false, ordinaryBlock));

        Assert.IsNotNull(InstanceManager.GetInstanceOnlyTemplate(reservedBlock));
        Assert.IsNull(InstanceManager.GetInstanceOnlyTemplate(ordinaryBlock));
        Assert.IsNull(InstanceManager.GetInstanceOnlyTemplate(new LandblockId(0x7C7CFFFF)));
    }

    [TestMethod]
    public void InstanceManager_ATemplateCanBeFoundByNameWhateverTheCase()
    {
        var template = NewTemplate();
        InstanceManager.RegisterTemplate(template);

        Assert.AreSame(template, InstanceManager.GetTemplate(template.Name.ToUpperInvariant()));
        Assert.IsNull(InstanceManager.GetTemplate("there is no such template"));
    }

    [TestMethod]
    public void InstanceManager_TheTimeoutIsFifteenMinutesUnlessTheServerPropertyIsChanged()
    {
        // the server property is read through the database, so what is checked here is the default it falls back to
        Assert.AreEqual(15L, DefaultPropertyManager.DefaultLongProperties["instance_empty_timeout_minutes"].Item);
    }
}
