using System.Collections.Generic;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Entity.Models;
using ACE.Server.Entity;
using ACE.Server.WorldObjects;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ACE.Server.Tests;

/// <summary>
/// The checks on a buy request that the exploit fixes from upstream #4503 added
/// </summary>
[TestClass]
public class VendorBuyRequestTests
{
    private const uint Sword = 0x80000010;
    private const uint Portal = 0x80000011;

    [DataTestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    [DataRow(int.MinValue)]
    public void AnAmountThatIsntPositiveIsRefused(int amount)
    {
        Assert.AreEqual(
            Vendor.BuyRequestError.InvalidAmount,
            Vendor.CheckBuyRequestItem(new ItemProfile(amount, Sword), [], DefaultItem(isService: false))
        );
    }

    [TestMethod]
    public void TheSameItemTwiceInOneRequestIsRefused()
    {
        var requested = new HashSet<uint>();

        Assert.AreEqual(Vendor.BuyRequestError.None, Check(new ItemProfile(1, Sword), requested));
        Assert.AreEqual(Vendor.BuyRequestError.None, Check(new ItemProfile(1, Portal), requested));
        Assert.AreEqual(Vendor.BuyRequestError.DuplicateItem, Check(new ItemProfile(5, Sword), requested));
    }

    [TestMethod]
    public void AServiceIsBoughtOneAtATime()
    {
        // services aren't limited by pack space
        var service = DefaultItem(isService: true);

        Assert.AreEqual(
            Vendor.BuyRequestError.None,
            Vendor.CheckBuyRequestItem(new ItemProfile(1, Portal), [], service)
        );
        Assert.AreEqual(
            Vendor.BuyRequestError.InvalidAmount,
            Vendor.CheckBuyRequestItem(new ItemProfile(2, Portal), [], service)
        );
    }

    [TestMethod]
    public void OtherItemsCanBeBoughtInAnyAmount()
    {
        Assert.AreEqual(
            Vendor.BuyRequestError.None,
            Vendor.CheckBuyRequestItem(new ItemProfile(500, Sword), [], DefaultItem(isService: false))
        );

        // a unique item, which the vendor checks for itself afterwards
        Assert.AreEqual(Vendor.BuyRequestError.None, Vendor.CheckBuyRequestItem(new ItemProfile(3, Sword), [], null));
    }

    private static Vendor.BuyRequestError Check(ItemProfile itemProfile, HashSet<uint> requested)
    {
        return Vendor.CheckBuyRequestItem(itemProfile, requested, DefaultItem(isService: false));
    }

    private static WorldObject DefaultItem(bool isService)
    {
        var weenie = new Weenie
        {
            WeenieClassId = 999999,
            ClassName = "vendorbuyrequesttest",
            WeenieType = WeenieType.Generic,
            PropertiesBool = new Dictionary<PropertyBool, bool> { [PropertyBool.VendorService] = isService },
            PropertiesString = new Dictionary<PropertyString, string> { [PropertyString.Name] = "For Sale" },
        };

        return new GenericObject(weenie, new ObjectGuid(0x80000001));
    }
}
