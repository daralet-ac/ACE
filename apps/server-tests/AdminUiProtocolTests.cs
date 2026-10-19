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
        var messages = AdminUiProtocol.BuildMessages("hello", "{\"protocol\":1}", 7);

        Assert.AreEqual(1, messages.Count);
        Assert.AreEqual("DAA1|hello|7|1|1|{\"protocol\":1}", messages[0]);
    }

    [TestMethod]
    public void BuildMessages_LongReplyIsSplitAndJoinsBackTogether()
    {
        var json = new string('x', AdminUiProtocol.ChunkSize * 2 + 5);

        var messages = AdminUiProtocol.BuildMessages("players", json, 3);

        Assert.AreEqual(3, messages.Count);

        var parts = messages.Select(m => m.Split('|', 6)).ToList();

        for (var i = 0; i < parts.Count; i++)
        {
            Assert.AreEqual("DAA1", parts[i][0]);
            Assert.AreEqual("players", parts[i][1]);
            Assert.AreEqual("3", parts[i][2]);
            Assert.AreEqual((i + 1).ToString(), parts[i][3]);
            Assert.AreEqual("3", parts[i][4]);
        }

        Assert.AreEqual(json, string.Concat(parts.Select(p => p[5])));
    }

    [TestMethod]
    public void EveryChunkIsAShortLine()
    {
        // a long chat line could reach the chat window instead of being hidden (AdminUiProtocol.ChunkSize)
        var json = new string('x', 5000);

        var messages = AdminUiProtocol.BuildMessages("players", json, 9);

        Assert.IsTrue(messages.All(m => m.Length < 260));
        Assert.IsTrue(messages.All(m => m.StartsWith("DAA1|players|9|")));
        Assert.AreEqual(json, messages.JoinedJson());
    }

    [TestMethod]
    public void BuildMessages_ChunksMayContainTheSeparator()
    {
        var json = "{\"name\":\"a|b\"}";

        var messages = AdminUiProtocol.BuildMessages("players", json, 1);

        Assert.AreEqual(json, messages[0].Split('|', 6)[5]);
    }

    [TestMethod]
    public void BuildMessages_RepliesGetNewIds()
    {
        var first = AdminUiProtocol.BuildMessages("hello", new { a = 1 })[0].Split('|')[2];
        var second = AdminUiProtocol.BuildMessages("hello", new { a = 1 })[0].Split('|')[2];

        Assert.AreNotEqual(first, second);
    }
}
