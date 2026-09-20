using ACE.Entity;
using ACE.Server.Managers;
using ACE.Server.Physics;
using ACE.Server.Physics.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ACE.Server.Tests.Physics;

[TestClass]
public class InstancePhysicsTests
{
    private PhysicsEngine previousEngine;

    [TestInitialize]
    public void UseAServerPhysicsEngine()
    {
        previousEngine = PhysicsEngine.Instance;

        // in server mode LScape resolves landblocks through LandblockManager
        _ = new PhysicsEngine(new ObjectMaint(), new SmartBox()) { Server = true };
    }

    [TestCleanup]
    public void RestoreThePhysicsEngine()
    {
        PhysicsEngine.Instance = previousEngine;
    }

    [TestMethod]
    public void LScape_NeverLoadsALandblockInAnInstanceOnDemand()
    {
        // The persistent world loads landblocks on demand. An instance only ever contains what was set up for it,
        // so asking for a landblock nobody created gives nothing, and must not create it.
        var cellId = 0x01E30001u;

        Assert.IsNull(LScape.get_landblock(cellId, 7));
        Assert.IsNull(LScape.get_landcell(cellId, 7));
        Assert.IsNull(LandblockManager.TryGetLandblock(new LandblockId(cellId), 7));
        Assert.IsFalse(LandblockManager.IsLoaded(new LandblockId(cellId), 7));
        Assert.AreEqual(0, LandblockManager.InstancedLandblockCount);
    }

    [TestMethod]
    public void CellArray_DefaultsToThePersistentWorld()
    {
        Assert.AreEqual(LScape.PersistentInstance, new CellArray().Instance);
    }

    [TestMethod]
    public void PhysicsObj_SharesItsInstanceWithItsCellArray()
    {
        // find_cell_list and the static helpers below it only receive the CellArray,
        // so it has to carry the instance of the object it belongs to
        var obj = new PhysicsObj();

        Assert.AreEqual(LScape.PersistentInstance, obj.Instance);
        Assert.AreEqual(LScape.PersistentInstance, obj.CellArray.Instance);

        obj.Instance = 5;

        Assert.AreEqual(5u, obj.Instance);
        Assert.AreEqual(5u, obj.CellArray.Instance);
    }

    [TestMethod]
    public void Transition_LooksCellsUpInTheInstanceOfTheObjectItIsReusedFor()
    {
        // transitions are pooled, so binding one to an object has to overwrite whatever instance it had for the last object
        var transition = new Transition();
        transition.Init();

        var first = new PhysicsObj { Instance = 3 };
        transition.InitObject(first, ObjectInfoState.Default);
        Assert.AreEqual(3u, transition.CellArray.Instance);

        var second = new PhysicsObj { Instance = 0 };
        transition.InitObject(second, ObjectInfoState.Default);
        Assert.AreEqual(0u, transition.CellArray.Instance);
    }

    [TestMethod]
    public void ObjCell_StartsInThePersistentWorld()
    {
        Assert.AreEqual(LScape.PersistentInstance, new ObjCell().Instance);
    }
}
