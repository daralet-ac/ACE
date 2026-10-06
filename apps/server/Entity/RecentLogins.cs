using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ACE.Common;
using ACE.Database.Models.Shard;

namespace ACE.Server.Entity;

/// <summary>
/// One account that logged in during a recent-logins window.
/// Ips and Characters are newest first; Characters only holds those whose latest login falls in the window.
/// </summary>
public record RecentLogin(
    uint AccountId,
    string AccountName,
    int Logins,
    DateTime LastLogin,
    IReadOnlyList<string> Ips,
    IReadOnlyList<string> Characters,
    bool Online
);

/// <summary>
/// Builds the report shared by the in-game /recentlogins command and the Discord recent-logins command,
/// from account_session_log (every login) and character.last_Login_Timestamp (each character's latest login).
/// </summary>
public static class RecentLogins
{
    public const double DefaultHours = 24;

    /// <summary>
    /// The account_session_log cutoff (server local time, as the rows are written) and the character cutoff (unix time) for a window ending now.
    /// </summary>
    public static (DateTime Since, double SinceUnixTime) GetCutoffs(TimeSpan window)
    {
        return (DateTime.Now - window, Time.GetUnixTime(DateTime.UtcNow - window));
    }

    /// <summary>
    /// Groups the sessions by account, newest login first. Characters are matched to accounts that have a session in the window.
    /// </summary>
    public static List<RecentLogin> Build(
        IEnumerable<AccountSessionLog> sessions,
        IEnumerable<Character> characters,
        ICollection<uint> onlineAccountIds
    )
    {
        var charactersByAccount = characters
            .GroupBy(c => c.AccountId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<string>)g.OrderByDescending(c => c.LastLoginTimestamp).Select(c => c.Name).ToList()
            );

        return sessions
            .GroupBy(s => s.AccountId)
            .Select(g =>
            {
                var newestFirst = g.OrderByDescending(s => s.LoginDateTime).ToList();

                return new RecentLogin(
                    g.Key,
                    newestFirst[0].AccountName,
                    newestFirst.Count,
                    newestFirst[0].LoginDateTime,
                    newestFirst.Select(s => s.SessionIp).Distinct().ToList(),
                    charactersByAccount.TryGetValue(g.Key, out var names) ? names : Array.Empty<string>(),
                    onlineAccountIds.Contains(g.Key)
                );
            })
            .OrderByDescending(r => r.LastLogin)
            .ThenBy(r => r.AccountName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// "the last 24 hours", "the last 1.5 hours", "the last hour"
    /// </summary>
    public static string DescribeWindow(double hours)
    {
        return hours == 1 ? "the last hour" : $"the last {hours:0.##} hours";
    }

    public static string GetTotalsLine(IList<RecentLogin> logins, double hours)
    {
        var online = logins.Count(l => l.Online);
        var ips = logins.SelectMany(l => l.Ips).Distinct().Count();

        return $"Accounts logged in during {DescribeWindow(hours)}: {logins.Count} ({ips} unique IPs, {online} still online)";
    }

    /// <summary>
    /// "just now", "42m ago", "3h 05m ago", "2d 4h ago"
    /// </summary>
    public static string FormatAgo(DateTime then, DateTime now)
    {
        var ago = now - then;

        if (ago < TimeSpan.FromMinutes(1))
        {
            return "just now";
        }

        if (ago < TimeSpan.FromHours(1))
        {
            return $"{ago.Minutes}m ago";
        }

        if (ago < TimeSpan.FromDays(1))
        {
            return $"{ago.Hours}h {ago.Minutes:00}m ago";
        }

        return $"{(int)ago.TotalDays}d {ago.Hours}h ago";
    }

    /// <summary>
    /// One line per account, for in-game chat and the console.
    /// </summary>
    public static string FormatForChat(IList<RecentLogin> logins, double hours, DateTime now)
    {
        var sb = new StringBuilder(" \n");
        sb.Append(GetTotalsLine(logins, hours)).Append('\n');

        foreach (var login in logins)
        {
            sb.Append($"{login.AccountName} ({login.AccountId})");
            sb.Append($"  -  Last: {FormatAgo(login.LastLogin, now)}");
            sb.Append($"  -  Logins: {login.Logins}");
            sb.Append($"  -  IPs: {string.Join(", ", login.Ips)}");

            if (login.Characters.Count > 0)
            {
                sb.Append($"  -  Chars: {string.Join(", ", login.Characters)}");
            }

            if (login.Online)
            {
                sb.Append("  -  [Online]");
            }

            sb.Append('\n');
        }

        return sb.ToString();
    }

    /// <summary>
    /// A fixed-width table for a Discord code block or a .txt attachment.
    /// </summary>
    public static string FormatTable(IList<RecentLogin> logins, DateTime now, bool showIps)
    {
        var headers = new List<string> { "Account", "Last login", "Logins", "Online", "Characters" };
        if (showIps)
        {
            headers.Add("IPs");
        }

        var rows = logins
            .Select(l =>
            {
                var row = new List<string>
                {
                    l.AccountName,
                    FormatAgo(l.LastLogin, now),
                    l.Logins.ToString(),
                    l.Online ? "Yes" : "",
                    string.Join(", ", l.Characters)
                };

                if (showIps)
                {
                    row.Add(string.Join(", ", l.Ips));
                }

                return row;
            })
            .ToList();

        var widths = headers.Select((h, i) => rows.Select(r => r[i].Length).Prepend(h.Length).Max()).ToList();

        var sb = new StringBuilder();
        sb.Append(FormatRow(headers, widths)).Append('\n');
        sb.Append('|').Append(string.Join("|", widths.Select(w => new string('-', w + 2)))).Append("|\n");

        foreach (var row in rows)
        {
            sb.Append(FormatRow(row, widths)).Append('\n');
        }

        return sb.ToString();
    }

    private static string FormatRow(IList<string> cells, IList<int> widths)
    {
        return "| " + string.Join(" | ", cells.Select((c, i) => c.PadRight(widths[i]))) + " |";
    }
}
