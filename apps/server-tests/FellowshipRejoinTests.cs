using ACE.Server.Entity;
using ACE.Server.Managers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ACE.Server.Tests;

[TestClass]
public class FellowshipRejoinTests
{
    [TestMethod]
    public void Settings_LockedRejoinWindowDefaultsToTenMinutes()
    {
        // /modifylong only changes properties that have a default
        Assert.IsTrue(
            DefaultPropertyManager.DefaultLongProperties.TryGetValue("fellow_locked_rejoin_seconds", out var seconds)
        );
        Assert.AreEqual(600L, seconds.Item, "the same 10 minutes a locked fellowship always gave to be recruited back");
    }

    [TestMethod]
    public void TryRejoinOnLogin_NoPlayer_DoesNothing()
    {
        Assert.IsFalse(Fellowship.TryRejoinOnLogin(null));
    }
}
