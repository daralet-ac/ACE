using System;
using ACE.Common.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ACE.Server.Tests.Libraries;

[TestClass]
public class TimeSpanExtensionsTests
{
    [DataTestMethod]
    [DataRow(1, 2, 3, 4, "1d 2h 3m 4s")]
    [DataRow(0, 0, 5, 0, "5m")]
    [DataRow(0, 1, 0, 30, "1h 30s")]
    [DataRow(0, 0, 0, 0, "")]
    public void FriendlyString_ListsWhatIsntZero(int days, int hours, int minutes, int seconds, string expected)
    {
        Assert.AreEqual(expected, new TimeSpan(days, hours, minutes, seconds).GetFriendlyString());
    }

    // NPCs say this to players: %mxqt in an emote
    [DataTestMethod]
    [DataRow(1, 2, 3, 4, "1 day, 2 hours, 3 minutes and 4 seconds")]
    [DataRow(2, 1, 0, 0, "2 days, 1 hour")]
    [DataRow(1, 0, 0, 1, "1 day and 1 second")]
    [DataRow(0, 0, 10, 0, "10 minutes")]
    [DataRow(0, 0, 0, 45, "45 seconds")]
    [DataRow(0, 3, 0, 15, "3 hours and 15 seconds")]
    [DataRow(0, 0, 0, 0, "")]
    public void FriendlyLongString_ReadsLikeASentence(int days, int hours, int minutes, int seconds, string expected)
    {
        Assert.AreEqual(expected, new TimeSpan(days, hours, minutes, seconds).GetFriendlyLongString());
    }
}
