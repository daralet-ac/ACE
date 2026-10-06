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
public class ItemStyleToggleTests
{
    private const uint LookA = 0x10000100;
    private const uint LookB = 0x10000200;

    [TestMethod]
    public void Toggle_SwapsClothingBasesBackAndForth()
    {
        var item = CreateItem(LookA, LookB);

        Assert.IsTrue(ItemStyleToggle.Toggle(item));
        Assert.AreEqual(LookB, item.ClothingBase);
        Assert.AreEqual(LookA, item.AlternateClothingBase);

        Assert.IsTrue(ItemStyleToggle.Toggle(item));
        Assert.AreEqual(LookA, item.ClothingBase);
        Assert.AreEqual(LookB, item.AlternateClothingBase);
    }

    [TestMethod]
    public void Toggle_LeavesItemsWithOneLookAlone()
    {
        var unflagged = CreateItem(LookA, null);
        Assert.IsFalse(ItemStyleToggle.HasAlternateStyle(unflagged));
        Assert.IsFalse(ItemStyleToggle.Toggle(unflagged));
        Assert.AreEqual(LookA, unflagged.ClothingBase);

        var sameLook = CreateItem(LookA, LookA);
        Assert.IsFalse(ItemStyleToggle.Toggle(sameLook));

        var noLook = CreateItem(null, LookB);
        Assert.IsFalse(ItemStyleToggle.Toggle(noLook));
        Assert.AreEqual(LookB, noLook.AlternateClothingBase);

        Assert.IsFalse(ItemStyleToggle.HasAlternateStyle(null));
    }

    private static WorldObject CreateItem(uint? clothingBase, uint? alternateClothingBase)
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

        var weenie = new Weenie
        {
            WeenieClassId = 999998,
            ClassName = "itemstyletoggletest",
            WeenieType = WeenieType.Generic,
            PropertiesInt = new Dictionary<PropertyInt, int> { [PropertyInt.ItemType] = (int)ItemType.Misc },
            PropertiesDID = dids,
            PropertiesString = new Dictionary<PropertyString, string> { [PropertyString.Name] = "Two-Look Item" },
        };

        return new GenericObject(weenie, new ObjectGuid(0x80000002));
    }
}
