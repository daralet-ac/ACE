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
public class ItemSetupToggleTests
{
    private const uint SetupA = 0x02000100;
    private const uint SetupB = 0x02000200;

    [TestMethod]
    public void Toggle_SwapsSetupsBackAndForth()
    {
        var item = CreateItem(SetupA, SetupB);

        Assert.IsTrue(ItemSetupToggle.Toggle(item));
        Assert.AreEqual(SetupB, item.SetupTableId);
        Assert.AreEqual(SetupA, item.AlternateSetup);

        Assert.IsTrue(ItemSetupToggle.Toggle(item));
        Assert.AreEqual(SetupA, item.SetupTableId);
        Assert.AreEqual(SetupB, item.AlternateSetup);
    }

    [TestMethod]
    public void Toggle_LeavesItemsWithOneLookAlone()
    {
        var unflagged = CreateItem(SetupA, null);
        Assert.IsFalse(ItemSetupToggle.HasAlternateSetup(unflagged));
        Assert.IsFalse(ItemSetupToggle.Toggle(unflagged));
        Assert.AreEqual(SetupA, unflagged.SetupTableId);

        var sameSetup = CreateItem(SetupA, SetupA);
        Assert.IsFalse(ItemSetupToggle.Toggle(sameSetup));

        var noSetup = CreateItem(null, SetupB);
        Assert.IsFalse(ItemSetupToggle.Toggle(noSetup));
        Assert.AreEqual(SetupB, noSetup.AlternateSetup);

        Assert.IsFalse(ItemSetupToggle.HasAlternateSetup(null));
    }

    private static WorldObject CreateItem(uint? setup, uint? alternateSetup)
    {
        var dids = new Dictionary<PropertyDataId, uint>();
        if (setup != null)
        {
            dids[PropertyDataId.Setup] = setup.Value;
        }

        if (alternateSetup != null)
        {
            dids[PropertyDataId.AlternateSetup] = alternateSetup.Value;
        }

        var weenie = new Weenie
        {
            WeenieClassId = 999998,
            ClassName = "itemsetuptoggletest",
            WeenieType = WeenieType.Generic,
            PropertiesInt = new Dictionary<PropertyInt, int> { [PropertyInt.ItemType] = (int)ItemType.Misc },
            PropertiesDID = dids,
            PropertiesString = new Dictionary<PropertyString, string> { [PropertyString.Name] = "Two-Look Item" },
        };

        return new GenericObject(weenie, new ObjectGuid(0x80000002));
    }
}
