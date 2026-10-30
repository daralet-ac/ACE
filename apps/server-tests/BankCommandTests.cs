using ACE.Server.Commands.PlayerCommands;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ACE.Server.Tests;

[TestClass]
public class BankCommandTests
{
    [TestMethod]
    public void TryParsePackId_ReadsHashAndHex()
    {
        Assert.IsTrue(BankCommand.TryParsePackId("#8000ABCD", out var id));
        Assert.AreEqual(0x8000ABCDu, id);

        Assert.IsTrue(BankCommand.TryParsePackId("#0x8000abcd", out id));
        Assert.AreEqual(0x8000ABCDu, id);
    }

    [TestMethod]
    public void TryParsePackId_LeavesNumbersAndTagsAlone()
    {
        // a bank pack's number and every tag word must still read as before
        Assert.IsFalse(BankCommand.TryParsePackId("2", out _));
        Assert.IsFalse(BankCommand.TryParsePackId("weapons", out _));
        Assert.IsFalse(BankCommand.TryParsePackId("#", out _));
        Assert.IsFalse(BankCommand.TryParsePackId("#0", out _));
        Assert.IsFalse(BankCommand.TryParsePackId("#weapons", out _));
        Assert.IsFalse(BankCommand.TryParsePackId(null, out _));
    }
}
