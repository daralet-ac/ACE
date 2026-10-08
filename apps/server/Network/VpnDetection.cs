using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text.Json;
using ACE.Server.Managers;
using Serilog;

namespace ACE.Server.Network;

/// <summary>
/// Looks up client IPs against proxycheck.io to identify VPN / proxy connections.
/// Results are cached in memory for the lifetime of the server (or until cleared by an admin).
/// </summary>
public static class VpnDetection
{
    private static readonly ILogger _log = Log.ForContext(typeof(VpnDetection));

    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(3) };

    /// <summary>
    /// IP -> true if the IP was identified as a VPN / proxy, false if it was checked and is clean.
    /// Failed lookups are not cached so they are retried on the next login.
    /// </summary>
    private static readonly ConcurrentDictionary<string, bool> CheckedIPs = new();

    /// <summary>
    /// Returns true if logins for this account from this address should be refused.
    /// Fails open: if the lookup service is unavailable the connection is allowed.
    /// </summary>
    public static bool ShouldBlock(string accountName, IPAddress address)
    {
        if (!PropertyManager.GetBool("block_vpn_connections").Item)
        {
            return false;
        }

        if (IsAccountWhitelisted(accountName) || address == null || IsLocalAddress(address))
        {
            return false;
        }

        var ip = address.ToString();

        if (CheckedIPs.TryGetValue(ip, out var isVpn))
        {
            return isVpn;
        }

        var result = LookupIsVpn(ip);

        if (result.HasValue)
        {
            CheckedIPs[ip] = result.Value;
            return result.Value;
        }

        return false;
    }

    /// <summary>
    /// Removes every IP flagged as a VPN from the cache so they are re-checked on their next login.
    /// </summary>
    public static int ClearBlockedIPs()
    {
        var count = 0;

        foreach (var entry in CheckedIPs.Where(e => e.Value))
        {
            if (CheckedIPs.TryRemove(entry.Key, out _))
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Removes a single IP from the cache so it is re-checked on its next login.
    /// </summary>
    public static bool RemoveBlockedIP(string ip)
    {
        return CheckedIPs.TryGetValue(ip, out var isVpn) && isVpn && CheckedIPs.TryRemove(ip, out _);
    }

    private static bool IsAccountWhitelisted(string accountName)
    {
        var whitelist = PropertyManager.GetString("vpn_account_whitelist").Item;

        if (string.IsNullOrWhiteSpace(whitelist) || string.IsNullOrEmpty(accountName))
        {
            return false;
        }

        return whitelist
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Contains(accountName, StringComparer.OrdinalIgnoreCase);
    }

    internal static bool IsLocalAddress(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (IPAddress.IsLoopback(address))
        {
            return true;
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            return address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6UniqueLocal;
        }

        var bytes = address.GetAddressBytes();

        return bytes[0] == 10
            || (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
            || (bytes[0] == 192 && bytes[1] == 168)
            || (bytes[0] == 169 && bytes[1] == 254);
    }

    /// <summary>
    /// Queries proxycheck.io. Returns null if the lookup failed or timed out.
    /// </summary>
    private static bool? LookupIsVpn(string ip)
    {
        var url = $"https://proxycheck.io/v2/{Uri.EscapeDataString(ip)}?vpn=1&asn=1";

        var apiKey = PropertyManager.GetString("proxycheck_api_key").Item;
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            url += "&key=" + Uri.EscapeDataString(apiKey);
        }

        try
        {
            // Login handling is synchronous, so block here; the HttpClient timeout bounds the wait.
            var data = HttpClient.GetStringAsync(url).GetAwaiter().GetResult();

            return ParseLookup(ip, data);
        }
        catch (Exception ex)
        {
            _log.Warning(ex, "VPN lookup for {Ip} failed", ip);
            return null;
        }
    }

    /// <summary>
    /// Reads proxycheck.io's answer for an IP. Returns null if it has no answer for that IP.
    /// </summary>
    internal static bool? ParseLookup(string ip, string data)
    {
        using var doc = JsonDocument.Parse(data);
        var root = doc.RootElement;

        if (!root.TryGetProperty(ip, out var info) || info.ValueKind != JsonValueKind.Object)
        {
            var status = root.TryGetProperty("status", out var s) ? s.ToString() : "unknown";
            var message = root.TryGetProperty("message", out var m) ? m.ToString() : "";
            _log.Warning("VPN lookup for {Ip} returned no result. Status: {Status} {Message}", ip, status, message);
            return null;
        }

        var proxy = GetString(info, "proxy");
        var isVpn = string.Equals(proxy, "yes", StringComparison.OrdinalIgnoreCase);

        if (isVpn)
        {
            _log.Warning(
                "VPN detected for {Ip}. Type: {Type}, Provider: {Provider}, ASN: {Asn}, Country: {Country}",
                ip,
                GetString(info, "type"),
                GetString(info, "provider"),
                GetString(info, "asn"),
                GetString(info, "country")
            );
        }

        return isVpn;
    }

    private static string GetString(JsonElement element, string name)
    {
        return element.TryGetProperty(name, out var value) ? value.ToString() : null;
    }
}
