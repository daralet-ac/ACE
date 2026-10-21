using System;
using System.Collections.Concurrent;
using ACE.Server.Managers;

namespace ACE.Server.Entity.PlayerUi;

/// <summary>
/// How often a player may ask @playerui for things. Anyone can type or script the command, so the limits are here, not in
/// the plugin. Two checks, both of which a request has to pass, and a request that fails one gets no answer:
/// <list type="bullet">
/// <item>each kind no sooner than its interval after the last of that kind (hello 2 seconds, the rest 400ms);</item>
/// <item>a budget for every kind together, a token bucket, so asking for each kind in turn can't add up.</item>
/// </list>
/// The plugin's own pace stays well inside both: at most about 6 requests a second with every window open, a kind no
/// sooner than 500ms apart, and hello every 3 seconds until it's answered. Each limit is a server property (/modifylong
/// playerui_...), read for every request, so a change applies at once.
/// </summary>
public sealed class PlayerUiLimits
{
    /// <summary>The limits in force: from the server properties, or made up by a test.</summary>
    public readonly struct Settings
    {
        /// <summary>Between two requests of the same kind; zero for no limit.</summary>
        public TimeSpan KindInterval { get; init; }

        /// <summary>Between two hellos; zero for no limit.</summary>
        public TimeSpan HelloInterval { get; init; }

        /// <summary>Requests a second, over time, of every kind together; zero for no budget.</summary>
        public double PerSecond { get; init; }

        /// <summary>How many may come at once: opening a window can ask for several things in the same moment.</summary>
        public double Burst { get; init; }

        public static Settings FromProperties()
        {
            return new Settings
            {
                KindInterval = TimeSpan.FromMilliseconds(Math.Max(0, PropertyManager.GetLong("playerui_request_interval_ms", 400).Item)),
                HelloInterval = TimeSpan.FromMilliseconds(Math.Max(0, PropertyManager.GetLong("playerui_hello_interval_ms", 2000).Item)),
                PerSecond = Math.Max(0, PropertyManager.GetLong("playerui_requests_per_second", 10).Item),
                // a burst below one would let nothing through
                Burst = Math.Max(1, PropertyManager.GetLong("playerui_request_burst", 20).Item),
            };
        }
    }

    private sealed class Bucket
    {
        public double Tokens;
        public DateTime At;
    }

    private readonly ConcurrentDictionary<(uint Player, string Kind), DateTime> lastRequest = new();

    private readonly ConcurrentDictionary<uint, Bucket> buckets = new();

    /// <summary>Whether to answer this request, counting it if so.</summary>
    public bool Allow(uint player, string kind, DateTime now, Settings settings)
    {
        var key = (player, kind);
        var interval = kind == "hello" ? settings.HelloInterval : settings.KindInterval;

        if (lastRequest.TryGetValue(key, out var last) && now - last < interval)
        {
            return false;
        }

        if (settings.PerSecond > 0 && !TakeToken(player, now, settings))
        {
            return false;
        }

        lastRequest[key] = now;
        return true;
    }

    private bool TakeToken(uint player, DateTime now, Settings settings)
    {
        var bucket = buckets.GetOrAdd(player, _ => new Bucket { Tokens = settings.Burst, At = now });

        lock (bucket)
        {
            var seconds = Math.Max(0, (now - bucket.At).TotalSeconds);
            bucket.Tokens = Math.Min(settings.Burst, bucket.Tokens + seconds * settings.PerSecond);
            bucket.At = now;

            if (bucket.Tokens < 1)
            {
                return false;
            }

            bucket.Tokens -= 1;
            return true;
        }
    }

    /// <summary>At logout: what a player asked for matters only while they're online.</summary>
    public void Forget(uint player)
    {
        buckets.TryRemove(player, out _);

        foreach (var key in lastRequest.Keys)
        {
            if (key.Player == player)
            {
                lastRequest.TryRemove(key, out _);
            }
        }
    }
}
