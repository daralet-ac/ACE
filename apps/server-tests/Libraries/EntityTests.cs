using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using ACE.Entity;
using ACE.Entity.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ACE.Server.Tests.Libraries;

[TestClass]
public class EntityTests
{
    // a dungeon cell: an outdoor position's cell follows from where it is
    private const uint Cell = 0x01D90108;

    #region Book pages

    [TestMethod]
    public void AddPage_GivesTheIndexOfThePageItAdded()
    {
        // #1284: it gave the index after it
        var rwLock = new ReaderWriterLockSlim();
        var pages = new List<PropertiesBookPageData>
        {
            new() { PageText = "one" },
            new() { PageText = "two" },
        };
        var page = new PropertiesBookPageData { PageText = "three" };

        pages.AddPage(page, out var index, rwLock);

        Assert.AreEqual(2, index);
        Assert.AreSame(page, pages.GetPage(index, rwLock));
    }

    #endregion

    #region Position

    [TestMethod]
    public void Position_DistancesToNothingAreAsFarAsCanBe()
    {
        // #1284: these threw
        var position = new Position(Cell, new Vector3(10, 10, 0), Quaternion.Identity);

        Assert.AreEqual(float.MaxValue, position.DistanceTo(null));
        Assert.AreEqual(float.MaxValue, position.SquaredDistanceTo(null));
        Assert.AreEqual(float.MaxValue, position.Distance2D(null));
        Assert.AreEqual(float.MaxValue, position.Distance2DSquared(null));
        Assert.AreEqual(new Vector3(float.MaxValue), position.GetOffset(null));
        Assert.IsFalse(position.Equals(null));
    }

    [TestMethod]
    public void Position_DistancesInOneCell()
    {
        var a = new Position(Cell, new Vector3(10, 10, 0), Quaternion.Identity);
        var b = new Position(Cell, new Vector3(13, 14, 12), Quaternion.Identity);

        Assert.AreEqual(13, a.DistanceTo(b), 1e-4);
        Assert.AreEqual(169, a.SquaredDistanceTo(b), 1e-3);

        // 2D leaves out the height
        Assert.AreEqual(5, a.Distance2D(b), 1e-4);
        Assert.AreEqual(25, a.Distance2DSquared(b), 1e-3);
    }

    [TestMethod]
    public void Position_EqualMeansTheSameCellPlaceAndFacing()
    {
        var a = new Position(Cell, new Vector3(10, 10, 0), Quaternion.Identity);

        Assert.IsTrue(a.Equals(new Position(a)));
        Assert.IsFalse(a.Equals(new Position(Cell + 1, new Vector3(10, 10, 0), Quaternion.Identity)));
        Assert.IsFalse(a.Equals(new Position(Cell, new Vector3(10, 11, 0), Quaternion.Identity)));
        Assert.IsFalse(
            a.Equals(new Position(Cell, new Vector3(10, 10, 0), Quaternion.CreateFromYawPitchRoll(1, 0, 0)))
        );
    }

    #endregion
}
