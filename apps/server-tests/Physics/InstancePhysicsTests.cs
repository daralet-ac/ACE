using ACE.Entity;
using ACE.Server.Managers;
using ACE.Server.Physics;
using ACE.Server.Physics.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PhysicsPosition = ACE.Server.Physics.Common.Position;

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

    [TestMethod]
    public void ObjCell_TheSameIdInAnotherInstanceIsADifferentCell()
    {
        // Physics decides whether an object has changed cells by comparing them. A cell of another instance with the same id
        // has to count as a different cell, or an object teleported into an instance would stay registered in the cell it left.
        var a = new ObjCell(0x01E30100) { Instance = 1 };
        var b = new ObjCell(0x01E30100) { Instance = 1 };
        var elsewhere = new ObjCell(0x01E30100) { Instance = 2 };
        var otherId = new ObjCell(0x01E30101) { Instance = 1 };

        Assert.IsTrue(a.Equals(b));
        Assert.IsFalse(a.Equals(elsewhere));
        Assert.IsFalse(a.Equals(otherId));
        Assert.IsFalse(a.Equals((ObjCell)null));
    }

    [TestMethod]
    public void EnvCell_TheSameIdInAnotherInstanceIsADifferentCell()
    {
        var a = new EnvCell { ID = 0x01E30100, Instance = 1 };
        var b = new EnvCell { ID = 0x01E30100, Instance = 1 };
        var elsewhere = new EnvCell { ID = 0x01E30100, Instance = 2 };

        Assert.IsTrue(a.Equals(b));
        Assert.IsFalse(a.Equals(elsewhere));
        Assert.IsFalse(((ObjCell)a).Equals(elsewhere));
    }

    [TestMethod]
    public void PhysicsObj_LeavesACellThatBelongsToAnotherInstanceEvenIfItHasTheSameId()
    {
        // The cell the object is in is instance 0's. The object has been moved to instance 1, and its position (and so its cell id) hasn't changed.
        // It must not go on thinking it is in that cell. No landblock is loaded for instance 1 here, so it ends up in no cell at all.
        var obj = new PhysicsObj();
        var cell = new ObjCell(0x01E30100) { Instance = 0 };
        obj.CurCell = cell;
        obj.Instance = 1;

        obj.set_current_pos(new PhysicsPosition(0x01E30100));

        Assert.IsNull(obj.CurCell);
    }

    [TestMethod]
    public void PhysicsObj_StaysInItsCellIfNothingAboutItHasChanged()
    {
        // and the other way round: same id, same instance, so nothing to do
        var obj = new PhysicsObj();
        var cell = new ObjCell(0x01E30100) { Instance = 0 };
        obj.CurCell = cell;

        obj.set_current_pos(new PhysicsPosition(0x01E30100));

        Assert.AreSame(cell, obj.CurCell);
    }
}
