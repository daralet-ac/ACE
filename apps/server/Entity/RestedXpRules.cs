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

    /// <summary>rested_xp is off.</summary>
    public const string Off = "off";

    /// <summary>The rested_xp server properties, read once for each use.</summary>
    public readonly record struct Settings(bool Enabled, double LevelsPerHour, double MaxLevels, double Bonus)
    {
        public static Settings FromProperties()
        {
            return new Settings(
                PropertyManager.GetBool("rested_xp").Item,
                PropertyManager.GetDouble("rested_xp_levels_per_hour").Item,
                PropertyManager.GetDouble("rested_xp_max_levels").Item,
                PropertyManager.GetDouble("rested_xp_bonus").Item
            );
        }
    }

    /// <summary>The most rested XP a player of a level costing this can have.</summary>
    public static long Max(ulong levelCost, Settings settings)
    {
        return (long)Math.Round(levelCost * Math.Max(0, settings.MaxLevels));
    }

    /// <summary>How much rested XP a player of a level costing this builds up in an hour of rest.</summary>
    public static long PerHour(ulong levelCost, Settings settings)
    {
        return (long)Math.Round(levelCost * Math.Max(0, settings.LevelsPerHour));
    }

    /// <summary>
    /// The pool after resting this many seconds: it grows to the most and no further, but a pool already over the most (the
    /// properties lowered since) isn't cut back.
    /// </summary>
    public static double Accrue(double pool, double seconds, long perHour, long max)
    {
        if (seconds <= 0 || perHour <= 0 || pool >= max)
        {
            return pool;
        }

        return Math.Min(max, pool + perHour * seconds / 3600);
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

    /// <summary>Seconds of rest until the pool is full, or 0 when it is already or isn't growing.</summary>
    public static double SecondsToFull(PlayerUiRested rested)
    {
        if (rested.PerHour <= 0 || rested.Rested >= rested.Max)
        {
            return 0;
        }

        return (rested.Max - rested.Rested) * 3600.0 / rested.PerHour;
    }

    /// <summary>What /rested says: the same as the Daralet Toolkit shows, from the same reply.</summary>
    public static List<string> Lines(PlayerUiRested rested)
    {
        if (!rested.Enabled)
        {
            return ["Rested experience is turned off on this server."];
        }

        var lines = new List<string>();
        var percent = rested.Max > 0 ? Math.Min(100, rested.Rested * 100 / rested.Max) : 0;
        var bonus = $"{Math.Round(rested.Bonus * 100):0}%";
        var perHour = $"{rested.PerHour:N0}";

        lines.Add($"Rested experience: {rested.Rested:N0} of {rested.Max:N0} ({percent}%).");

        if (rested.Rested > 0)
        {
            lines.Add($"Your kills earn {bonus} more experience, taken from it, until it's used up.");
        }
        else
        {
            lines.Add($"With some, your kills would earn {bonus} more experience, taken from it.");
        }

        lines.Add(
            rested.State switch
            {
                Resting => $"You're resting: you gain {perHour} an hour here, full in {Duration(SecondsToFull(rested))}.",
                InCombat => $"You're in a town, but don't rest in combat mode. Out of it you'd gain {perHour} an hour.",
                Full => "You're fully rested.",
                MaxLevel => "You're at the highest level, earning no experience: there's nothing to rest for.",
                _ => $"You're not resting. In a town out of combat mode, or logged out, you gain {perHour} an hour.",
            }
        );

        if (rested.EarnedThisSession > 0)
        {
            lines.Add($"Your kills have earned {rested.EarnedThisSession:N0} more experience from it since you logged in.");
        }

        return lines;
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
