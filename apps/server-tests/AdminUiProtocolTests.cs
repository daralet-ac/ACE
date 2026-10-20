using System.Linq;
using ACE.Server.Entity.AdminUi;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ACE.Server.Tests;

[TestClass]
public class AdminUiProtocolTests
{
    [TestMethod]
    public void BuildMessages_SmallReplyIsOneLine()
    {
        var messages = AdminUiProtocol.BuildMessages("hello", "{\"protocol\":2}", 7);

        Assert.AreEqual(1, messages.Count);
        Assert.AreEqual("DAA2|hello|7|1|1|eyJwcm90b2NvbCI6Mn0=", messages[0]);
        Assert.AreEqual("{\"protocol\":2}", messages.JoinedJson());
    }

    [TestMethod]
    public void BuildMessages_LongReplyIsSplitAndJoinsBackTogether()
    {
        // 3 bytes of JSON are 4 characters of base64
        var json = new string('x', AdminUiProtocol.ChunkSize * 3 / 4 * 2 + 5);

        var messages = AdminUiProtocol.BuildMessages("players", json, 3);

        Assert.AreEqual(3, messages.Count);

        var parts = messages.Select(m => m.Split('|', 6)).ToList();

        for (var i = 0; i < parts.Count; i++)
        {
            Assert.AreEqual("DAA2", parts[i][0]);
            Assert.AreEqual("players", parts[i][1]);
            Assert.AreEqual("3", parts[i][2]);
            Assert.AreEqual((i + 1).ToString(), parts[i][3]);
            Assert.AreEqual("3", parts[i][4]);
        }

        Assert.AreEqual(json, messages.JoinedJson());
    }

    [TestMethod]
    public void EveryChunkIsAShortLine()
    {
        var json = new string('x', 5000);

        var messages = AdminUiProtocol.BuildMessages("players", json, 9);

        Assert.IsTrue(messages.All(m => m.Length < 260));
        Assert.IsTrue(messages.All(m => m.StartsWith("DAA2|players|9|")));
        Assert.AreEqual(json, messages.JoinedJson());
    }

    [TestMethod]
    public void BuildMessages_NoLineHasReadableText()
    {
        // Virindi Chat System posts its own copy of a chat line with coordinates in it, which the plugin can't hide
        var json = "{\"name\":\"+Admin a|b\",\"location\":\"Hebian-to (39.0S, 83.0E)\"}";

        var messages = AdminUiProtocol.BuildMessages("players", json, 1);

        Assert.IsFalse(messages.Any(m => m.Contains("39.0S") || m.Contains("Hebian") || m.Contains("location")));
        Assert.AreEqual(6, messages[0].Split('|').Length);
        Assert.AreEqual(json, messages.JoinedJson());
    }

    [TestMethod]
    public void BuildMessages_RepliesGetNewIds()
    {
        var first = AdminUiProtocol.BuildMessages("hello", new { a = 1 })[0].Split('|')[2];
        var second = AdminUiProtocol.BuildMessages("hello", new { a = 1 })[0].Split('|')[2];

        Assert.AreNotEqual(first, second);
    }
}
