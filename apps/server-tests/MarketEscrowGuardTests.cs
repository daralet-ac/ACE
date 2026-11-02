using ACE.Database.Models.Shard;
using ACE.Entity.Enum.Properties;
using ACE.Server.Market;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ACE.Server.Tests;

[TestClass]
public class MarketEscrowGuardTests
{
    private static Biota WithInstanceIds(params PropertyInstanceId[] types)
    {
        var biota = new Biota { Id = 0x80000001 };

        foreach (var type in types)
        {
            biota.BiotaPropertiesIID.Add(new BiotaPropertiesIID { ObjectId = biota.Id, Type = (ushort)type, Value = 0x50000001 });
        }

        return biota;
    }

    [TestMethod]
    public void AnItemWithNoHolderIsInEscrow()
    {
        Assert.IsTrue(MarketEscrowGuard.IsInEscrow(WithInstanceIds()));
        Assert.IsTrue(MarketEscrowGuard.IsInEscrow(WithInstanceIds(PropertyInstanceId.Owner)));
    }

    [TestMethod]
    public void AnItemInAPackOrWornIsntInEscrow()
    {
        Assert.IsFalse(MarketEscrowGuard.IsInEscrow(WithInstanceIds(PropertyInstanceId.Container)));
        Assert.IsFalse(MarketEscrowGuard.IsInEscrow(WithInstanceIds(PropertyInstanceId.Wielder)));
    }
}
