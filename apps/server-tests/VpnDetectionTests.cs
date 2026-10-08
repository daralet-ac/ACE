using System.Net;
using ACE.Server.Managers;
using ACE.Server.Network;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ACE.Server.Tests;

[TestClass]
public class VpnDetectionTests
{
    private const string PublicIp = "203.0.113.7";

    [TestCleanup]
    public void PutTheSettingsBack()
    {
        PropertyManager.ModifyBool("block_vpn_connections", false);
        PropertyManager.ModifyString("vpn_account_whitelist", "");
    }

    #region Lookup answers

    [TestMethod]
    public void Lookup_AProxyIsAVpn()
    {
        var answer = $$"""{ "status": "ok", "{{PublicIp}}": { "proxy": "yes", "type": "VPN" } }""";

        Assert.AreEqual(true, VpnDetection.ParseLookup(PublicIp, answer));
        Assert.AreEqual(true, VpnDetection.ParseLookup(PublicIp, answer.Replace("yes", "YES")));
    }

    [TestMethod]
    public void Lookup_AnythingElseIsClean()
    {
        Assert.AreEqual(
            false,
            VpnDetection.ParseLookup(PublicIp, $$"""{ "status": "ok", "{{PublicIp}}": { "proxy": "no" } }""")
        );
        Assert.AreEqual(
            false,
            VpnDetection.ParseLookup(PublicIp, $$"""{ "status": "ok", "{{PublicIp}}": { "type": "Business" } }""")
        );
    }

    [DataTestMethod]
    [DataRow("""{ "status": "denied", "message": "1000 free queries exhausted" }""", DisplayName = "refused")]
    [DataRow("""{ "status": "ok", "198.51.100.1": { "proxy": "yes" } }""", DisplayName = "about another IP")]
    [DataRow("""{ "status": "ok", "203.0.113.7": "yes" }""", DisplayName = "not an object")]
    [DataRow("{}", DisplayName = "empty")]
    public void Lookup_NoAnswerForThisIpIsUnknown(string answer)
    {
        // unknown isn't cached, so the IP is checked again next login
        Assert.IsNull(VpnDetection.ParseLookup(PublicIp, answer));
    }

    #endregion

    #region Addresses

    [DataTestMethod]
    [DataRow("127.0.0.1")]
    [DataRow("::1")]
    [DataRow("10.1.2.3")]
    [DataRow("172.16.0.1")]
    [DataRow("172.31.255.255")]
    [DataRow("192.168.1.10")]
    [DataRow("169.254.0.5")]
    [DataRow("fe80::1")]
    [DataRow("fec0::1")]
    [DataRow("fd00::1")]
    [DataRow("::ffff:192.168.1.10")]
    public void LocalAddresses_AreNeverLookedUp(string address)
    {
        Assert.IsTrue(VpnDetection.IsLocalAddress(IPAddress.Parse(address)));
    }

    [DataTestMethod]
    [DataRow("8.8.8.8")]
    [DataRow("172.15.255.255")]
    [DataRow("172.32.0.1")]
    [DataRow("192.169.0.1")]
    [DataRow(PublicIp)]
    [DataRow("2001:4860:4860::8888")]
    [DataRow("::ffff:8.8.8.8")]
    public void PublicAddresses_AreNotLocal(string address)
    {
        Assert.IsFalse(VpnDetection.IsLocalAddress(IPAddress.Parse(address)));
    }

    #endregion

    #region ShouldBlock

    [TestMethod]
    public void ShouldBlock_NothingWhileTheSettingIsOff()
    {
        PropertyManager.ModifyBool("block_vpn_connections", false);

        Assert.IsFalse(VpnDetection.ShouldBlock("someone", IPAddress.Parse(PublicIp)));
    }

    [TestMethod]
    public void ShouldBlock_NeverAWhitelistedAccount()
    {
        PropertyManager.ModifyBool("block_vpn_connections", true);
        PropertyManager.ModifyString("vpn_account_whitelist", "alice, Bob ,carol");

        Assert.IsFalse(VpnDetection.ShouldBlock("bob", IPAddress.Parse(PublicIp)));
        Assert.IsFalse(VpnDetection.ShouldBlock("ALICE", IPAddress.Parse(PublicIp)));
    }

    [TestMethod]
    public void ShouldBlock_NeverALocalOrMissingAddress()
    {
        PropertyManager.ModifyBool("block_vpn_connections", true);

        Assert.IsFalse(VpnDetection.ShouldBlock("someone", IPAddress.Parse("192.168.1.10")));
        Assert.IsFalse(VpnDetection.ShouldBlock("someone", IPAddress.Loopback));
        Assert.IsFalse(VpnDetection.ShouldBlock("someone", null));
    }

    #endregion
}
