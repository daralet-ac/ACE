using System;
using System.Collections.Generic;
using ACE.Database.Models.Shard;
using ACE.Server.Entity;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ACE.Server.Tests;

[TestClass]
public class RecentLoginsTests
{
    private static readonly DateTime Now = new DateTime(2026, 10, 6, 12, 0, 0);

    private static AccountSessionLog Session(uint accountId, string name, string ip, double hoursAgo) =>
        new() { AccountId = accountId, AccountName = name, SessionIp = ip, LoginDateTime = Now.AddHours(-hoursAgo) };

    private static Character Char(uint accountId, string name, double lastLogin) =>
        new() { AccountId = accountId, Name = name, LastLoginTimestamp = lastLogin };

    [TestMethod]
    public void Build_GroupsSessionsByAccountNewestFirst()
    {
        var sessions = new List<AccountSessionLog>
        {
            Session(1, "alpha", "1.1.1.1", 5),
            Session(2, "bravo", "2.2.2.2", 1),
            Session(1, "alpha", "3.3.3.3", 2),
            Session(1, "alpha", "1.1.1.1", 3),
        };

        var logins = RecentLogins.Build(sessions, new List<Character>(), new HashSet<uint>());

        Assert.AreEqual(2, logins.Count);
        Assert.AreEqual("bravo", logins[0].AccountName);

        var alpha = logins[1];
        Assert.AreEqual(3, alpha.Logins);
        Assert.AreEqual(Now.AddHours(-2), alpha.LastLogin);
        CollectionAssert.AreEqual(new[] { "3.3.3.3", "1.1.1.1" }, (System.Collections.ICollection)alpha.Ips);
    }

    [TestMethod]
    public void Build_AttachesCharactersNewestFirstAndMarksOnlineAccounts()
    {
        var sessions = new List<AccountSessionLog> { Session(1, "alpha", "1.1.1.1", 1), Session(2, "bravo", "2.2.2.2", 2) };
        var characters = new List<Character> { Char(1, "Older Toon", 100), Char(1, "Newer Toon", 200), Char(3, "No Session", 300) };

        var logins = RecentLogins.Build(sessions, characters, new HashSet<uint> { 2 });

        CollectionAssert.AreEqual(new[] { "Newer Toon", "Older Toon" }, (System.Collections.ICollection)logins[0].Characters);
        Assert.IsFalse(logins[0].Online);

        Assert.AreEqual(0, logins[1].Characters.Count);
        Assert.IsTrue(logins[1].Online);
    }

    [TestMethod]
    public void GetTotalsLine_CountsUniqueIpsAcrossAccounts()
    {
        var sessions = new List<AccountSessionLog>
        {
            Session(1, "alpha", "1.1.1.1", 1),
            Session(2, "bravo", "1.1.1.1", 1),
            Session(3, "charlie", "9.9.9.9", 1),
        };

        var logins = RecentLogins.Build(sessions, new List<Character>(), new HashSet<uint> { 3 });

        Assert.AreEqual(
            "Accounts logged in during the last 6 hours: 3 (2 unique IPs, 1 still online)",
            RecentLogins.GetTotalsLine(logins, 6)
        );
    }

    [TestMethod]
    public void DescribeWindow_HandlesOneAndFractionalHours()
    {
        Assert.AreEqual("the last hour", RecentLogins.DescribeWindow(1));
        Assert.AreEqual("the last 1.5 hours", RecentLogins.DescribeWindow(1.5));
        Assert.AreEqual("the last 24 hours", RecentLogins.DescribeWindow(24));
    }

    [TestMethod]
    public void FormatAgo_PicksUnitsByAge()
    {
        Assert.AreEqual("just now", RecentLogins.FormatAgo(Now.AddSeconds(-30), Now));
        Assert.AreEqual("42m ago", RecentLogins.FormatAgo(Now.AddMinutes(-42), Now));
        Assert.AreEqual("3h 05m ago", RecentLogins.FormatAgo(Now.AddMinutes(-185), Now));
        Assert.AreEqual("2d 4h ago", RecentLogins.FormatAgo(Now.AddHours(-52), Now));
    }

    [TestMethod]
    public void FormatTable_PadsColumnsAndOnlyShowsIpsWhenAsked()
    {
        var logins = RecentLogins.Build(
            new List<AccountSessionLog> { Session(1, "alpha", "1.1.1.1", 1) },
            new List<Character> { Char(1, "Toon", 1) },
            new HashSet<uint> { 1 }
        );

        var withoutIps = RecentLogins.FormatTable(logins, Now, showIps: false);
        var lines = withoutIps.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        Assert.AreEqual(3, lines.Length);
        Assert.AreEqual("| Account | Last login | Logins | Online | Characters |", lines[0]);
        Assert.AreEqual("| alpha   | 1h 00m ago | 1      | Yes    | Toon       |", lines[2]);
        Assert.IsFalse(withoutIps.Contains("1.1.1.1"));

        Assert.IsTrue(RecentLogins.FormatTable(logins, Now, showIps: true).Contains("| 1.1.1.1 |"));
    }
}
