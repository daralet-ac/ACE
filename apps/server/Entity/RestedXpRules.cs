using System;
using System.Collections.Generic;
using ACE.Server.Entity.PlayerUi;
using ACE.Server.Managers;

namespace ACE.Server.Entity;

/// <summary>
/// Rested experience: a pool of extra kill XP a player builds up while logged out, or in a town out of combat mode, and
/// uses up as they kill: each kill earns rested_xp_bonus more, taken from the pool, until it's gone. The sums and the
/// words, kept apart from Player so tests can reach them; Player_RestedXp.cs keeps the pool.
/// </summary>
public static class RestedXpRules
{
    /// <summary>The player is in a town out of combat mode, and not yet fully rested: the pool is growing.</summary>
    public const string Resting = "resting";

    /// <summary>In a town, but in combat mode, which keeps them from resting.</summary>
    public const string InCombat = "combat";

    /// <summary>Not in a town: they only rest once they're in one, or logged out.</summary>
    public const string Away = "away";

    /// <summary>The pool is as big as it gets.</summary>
    public const string Full = "full";

    /// <summary>At the highest level, earning no XP: there's nothing to rest for.</summary>
    public const string MaxLevel = "maxlevel";

    /// <summary>
    /// High enough a level that an hour's rest comes to nothing (level 100 and up, with the defaults): what's left can
    /// still be spent, but no more builds up.
    /// </summary>
    public const string NoRest = "norest";

    /// <summary>Below rested_xp_min_level (10): nothing builds up yet, in town or logged out.</summary>
    public const string LowLevel = "lowlevel";

    /// <summary>rested_xp is off.</summary>
    public const string Off = "off";

    /// <summary>The rested_xp server properties, read once for each use.</summary>
    /// <param name="Curve">An hour's rest at each level (rested_xp_rate_curve)</param>
    /// <param name="MinLevelsPerHour">The least it comes to, however high the level</param>
    /// <param name="SlowdownPerLevel">How much slower it builds for each full level's cost rested: 0.5 halves it</param>
    /// <param name="MinLevel">The level rest starts building at (rested_xp_min_level)</param>
    /// <param name="OfflineRate">The share of the rate logged-out time gets (rested_xp_offline_rate): 0.5 is half</param>
    public readonly record struct Settings(
        bool Enabled,
        RestedXpCurve Curve,
        double MinLevelsPerHour,
        double MaxLevels,
        double SlowdownPerLevel,
        double Bonus,
        int MinLevel = 0,
        double OfflineRate = 1
    )
    {
        public static Settings FromProperties()
        {
            return new Settings(
                PropertyManager.GetBool("rested_xp").Item,
                RestedXpCurve.FromText(PropertyManager.GetString("rested_xp_rate_curve").Item),
                PropertyManager.GetDouble("rested_xp_min_levels_per_hour").Item,
                PropertyManager.GetDouble("rested_xp_max_levels").Item,
                PropertyManager.GetDouble("rested_xp_slowdown_per_level").Item,
                PropertyManager.GetDouble("rested_xp_bonus").Item,
                (int)PropertyManager.GetLong("rested_xp_min_level").Item,
                PropertyManager.GetDouble("rested_xp_offline_rate").Item
            );
        }
    }

    /// <summary>
    /// The most rested XP a player of a level costing this can have: rested_xp_max_levels of the level's cost, or 0 for no
    /// most at all (rested_xp_max_levels 0, the default), the slowdown alone holding it back.
    /// </summary>
    public static long Max(ulong levelCost, Settings settings)
    {
        return settings.MaxLevels > 0 ? (long)Math.Round(levelCost * settings.MaxLevels) : 0;
    }

    /// <summary>The most as the sums use it: no most (0) is as high as a pool could go.</summary>
    public static long Cap(long max)
    {
        return max > 0 ? max : long.MaxValue;
    }

    /// <summary>
    /// An hour's rest at this level, as a fraction of the level's cost: rested_xp_rate_curve's (RestedXpCurve: 10% to
    /// level 10, 1% at 50, nothing from 100), but never under rested_xp_min_levels_per_hour (0 to start;
    /// raise it to keep higher levels resting). Nothing below rested_xp_min_level.
    /// </summary>
    public static double LevelsPerHour(int level, Settings settings)
    {
        // too new a character: rest starts at rested_xp_min_level, so fresh alts bank nothing
        if (level < settings.MinLevel)
        {
            return 0;
        }

        var rate = settings.Curve?.LevelsPerHour(level) ?? 0;
        return Math.Max(0, Math.Max(settings.MinLevelsPerHour, rate));
    }

    /// <summary>
    /// How much rested XP a player of this level, costing this, builds up in an hour of rest with less than a level's cost
    /// rested. Past each full level's cost it builds slower (RateAt).
    /// </summary>
    public static long PerHour(ulong levelCost, int level, Settings settings)
    {
        return (long)Math.Round(levelCost * LevelsPerHour(level, settings));
    }

    /// <summary>
    /// An hour's rest with a pool this big: perHour, slowed by slowdown (0.5 halves it) for each full level's cost already
    /// in the pool. With the defaults, full speed to a level's cost, then half speed to the most, a level and a half.
    /// </summary>
    public static double RateAt(double pool, long perHour, long levelCost, double slowdown)
    {
        return perHour * Math.Pow(Keep(slowdown), FullLevels(pool, levelCost));
    }

    /// <summary>
    /// The pool after resting this many seconds, slowing as it passes each level's cost: it grows to the most and no
    /// further, but a pool already over the most (the properties lowered since) isn't cut back.
    /// </summary>
    public static double Accrue(double pool, double seconds, long perHour, long max, long levelCost, double slowdown)
    {
        max = Cap(max);

        if (seconds <= 0 || perHour <= 0 || pool >= max)
        {
            return pool;
        }

        return Walk(pool, seconds, perHour, max, levelCost, slowdown).Pool;
    }

    /// <summary>
    /// Seconds of rest until a pool this big is full, slowing as it passes each level's cost, or null when it never will
    /// be (nothing builds up, there's no most, or a slowdown of 1 stops it at a level's cost). 0 when it's full already.
    /// </summary>
    public static double? SecondsToFull(double pool, long perHour, long max, long levelCost, double slowdown)
    {
        // with no most it's never full
        if (max <= 0)
        {
            return null;
        }

        if (pool >= max)
        {
            return 0;
        }

        if (perHour <= 0)
        {
            return null;
        }

        var (end, seconds) = Walk(pool, double.PositiveInfinity, perHour, max, levelCost, slowdown);
        return end >= max ? seconds : null;
    }

    /// <summary>
    /// Rests up to this many seconds (infinity for as long as it takes), a stretch at each speed: to the next level's cost
    /// at one, past it at the next. The pool it comes to, and the seconds that took.
    /// </summary>
    private static (double Pool, double Seconds) Walk(
        double pool,
        double seconds,
        long perHour,
        long max,
        long levelCost,
        double slowdown
    )
    {
        var used = 0.0;
        var keep = Keep(slowdown);
        var levels = FullLevels(pool, levelCost);

        while (pool < max && used < seconds)
        {
            var rate = perHour * Math.Pow(keep, levels);

            if (rate <= 0)
            {
                break;
            }

            // counted on from where it started, not worked out from the pool again, which floating point could leave a
            // hair under the level it just reached
            var next = levelCost > 0 ? Math.Min(max, (levels + 1.0) * levelCost) : max;
            var need = Math.Max(0, next - pool) / rate * 3600;
            var left = seconds - used;

            if (need >= left)
            {
                pool += rate * left / 3600;
                used = seconds;
                break;
            }

            pool = Math.Max(pool, next);
            used += need;
            levels++;
        }

        return (Math.Min(pool, max), used);
    }

    /// <summary>What's kept of the speed for each full level rested: 0.5 slowdown keeps half.</summary>
    private static double Keep(double slowdown)
    {
        return 1 - Math.Clamp(slowdown, 0, 1);
    }

    /// <summary>How many full levels' cost the pool holds.</summary>
    private static int FullLevels(double pool, long levelCost)
    {
        return levelCost > 0 ? (int)Math.Max(0, Math.Floor(pool / levelCost)) : 0;
    }

    /// <summary>An hour logged out, as rested_xp_offline_rate has it: half an hour in town's to start.</summary>
    public static long OfflinePerHour(long perHour, Settings settings)
    {
        return (long)Math.Round(perHour * Math.Max(0, settings.OfflineRate));
    }

    /// <summary>The extra XP a kill worth this much earns from a pool this big: never more than is left in it.</summary>
    public static long Bonus(double pool, long amount, double bonus)
    {
        if (pool < 1 || amount <= 0 || bonus <= 0)
        {
            return 0;
        }

        return (long)Math.Min(Math.Floor(pool), Math.Round(amount * bonus));
    }

    /// <summary>Seconds of rest until the reply's pool is full, or null when it never will be.</summary>
    public static double? SecondsToFull(PlayerUiRested rested)
    {
        return SecondsToFull(rested.Rested, rested.PerHour, rested.Max, rested.LevelCost, rested.SlowdownPerLevel);
    }

    /// <summary>An hour's rest with the reply's pool, slowed for each full level's cost in it.</summary>
    public static long RateNow(PlayerUiRested rested)
    {
        return (long)Math.Round(RateAt(rested.Rested, rested.PerHour, rested.LevelCost, rested.SlowdownPerLevel));
    }

    /// <summary>What /rested says: the same as the Daralet Toolkit's XP bar shows, from the same reply.</summary>
    public static List<string> Lines(PlayerUiRested rested)
    {
        if (!rested.Enabled)
        {
            return ["Rested experience is turned off on this server."];
        }

        var lines = new List<string>();
        var percent = rested.Max > 0 ? Math.Min(100, rested.Rested * 100 / rested.Max) : 0;
        var levels = rested.LevelCost > 0 ? $", {(double)rested.Rested / rested.LevelCost:0.0#} levels' worth" : "";
        var bonus = $"{Math.Round(rested.Bonus * 100):0}%";
        var perHour = $"{RateNow(rested):N0}";
        var toFull = SecondsToFull(rested);
        var fullIn = toFull is > 0 ? $", full in {Duration(toFull.Value)}" : "";

        lines.Add(
            rested.Max > 0
                ? $"Rested experience: {rested.Rested:N0} of {rested.Max:N0} ({percent}%)."
                : $"Rested experience: {rested.Rested:N0}{levels}."
        );

        if (rested.Rested > 0)
        {
            lines.Add($"Your kills earn {bonus} more experience, taken from it, until it's used up.");
        }
        else if (rested.State is not (NoRest or LowLevel))
        {
            lines.Add($"With some, your kills would earn {bonus} more experience, taken from it.");
        }

        lines.Add(
            rested.State switch
            {
                Resting => $"You're resting: you gain {perHour} an hour here{fullIn}.",
                InCombat => $"You're in a town, but don't rest in combat mode. Out of it you'd gain {perHour} an hour.",
                Full => "You're fully rested.",
                MaxLevel => "You're at the highest level, earning no experience: there's nothing to rest for.",
                NoRest => "You no longer build up rested experience at your level.",
                LowLevel => $"You start building up rested experience at level {rested.MinLevel}.",
                _ => $"You're not resting. {WhereYouRest(rested, perHour)}",
            }
        );

        // only once it's slowed, so nobody below a level's worth reads about it
        if (
            rested.SlowdownPerLevel > 0
            && rested.LevelCost > 0
            && rested.Rested >= rested.LevelCost
            && rested.State is Resting or InCombat or Away
        )
        {
            lines.Add(
                $"It builds up {Math.Round(Math.Min(1, rested.SlowdownPerLevel) * 100):0}% slower for each full level's "
                    + "worth you've rested."
            );
        }

        if (rested.EarnedThisSession > 0)
        {
            lines.Add($"Your kills have earned {rested.EarnedThisSession:N0} more experience from it since you logged in.");
        }

        return lines;
    }

    /// <summary>How fast in town and logged out, with the reply's offline rate.</summary>
    private static string WhereYouRest(PlayerUiRested rested, string perHour)
    {
        var offline = Math.Max(0, rested.OfflineRate);

        if (Math.Abs(offline - 1) < 1e-9)
        {
            return $"In a town out of combat mode, or logged out, you gain {perHour} an hour.";
        }

        if (offline <= 0)
        {
            return $"In a town out of combat mode you gain {perHour} an hour.";
        }

        var loggedOut = Math.Round(RateNow(rested) * offline);
        return $"In a town out of combat mode you gain {perHour} an hour, and {loggedOut:N0} an hour logged out.";
    }

    /// <summary>45s, 12m, 3h 07m, 2d 04h.</summary>
    public static string Duration(double seconds)
    {
        var time = TimeSpan.FromSeconds(Math.Max(0, Math.Ceiling(seconds)));

        if (time.TotalMinutes < 1)
        {
            return $"{time.Seconds}s";
        }

        if (time.TotalHours < 1)
        {
            return $"{time.Minutes}m";
        }

        if (time.TotalDays < 1)
        {
            return $"{(int)time.TotalHours}h {time.Minutes:00}m";
        }

        return $"{(int)time.TotalDays}d {time.Hours:00}h";
    }
}
