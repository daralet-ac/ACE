using System;
using System.Collections.Generic;
using System.Linq;

namespace ACE.Server.TownEvents;

/// <summary>
/// One side's attacks on another in a town event (raiders on the guards, the Captain on raiders, ...): how the swings
/// went, and the damage, for balancing the guards against the attackers
/// </summary>
public sealed class TownCombatLine
{
    /// <summary>
    /// Melee and missile attacks, and how many were evaded, blocked or parried. The rest landed.
    /// </summary>
    public int Swings { get; set; }

    public int Evaded { get; set; }
    public int Blocked { get; set; }
    public int Parried { get; set; }

    public int Landed => Swings - Evaded - Blocked - Parried;

    /// <summary>
    /// The landed swings' damage before and after armor, resistances and shields
    /// </summary>
    public double Raw { get; set; }

    public double Mitigated { get; set; }

    /// <summary>
    /// All the damage done, spells included, up to the target's remaining health
    /// </summary>
    public double Dealt { get; set; }
}

/// <summary>
/// The swings and damage between a town event's raiders and the players, guards and Captain, by "attacker>target" side
/// (raider, player, guard, captain). Saved as JSON with the event log (town_event.combat).
/// </summary>
public sealed class TownCombatStats
{
    public static readonly string[] Keys =
    {
        "raider>player",
        "raider>guard",
        "raider>captain",
        "player>raider",
        "guard>raider",
        "captain>raider"
    };

    public Dictionary<string, TownCombatLine> Lines { get; set; } = new();

    private TownCombatLine Line(string key)
    {
        if (!Lines.TryGetValue(key, out var line))
        {
            line = new TownCombatLine();
            Lines[key] = line;
        }

        return line;
    }

    public static bool Tracked(string key) => Array.IndexOf(Keys, key) >= 0;

    public void AddSwing(string key, bool evaded, bool blocked, bool parried, double raw, double mitigated)
    {
        var line = Line(key);
        line.Swings++;

        if (evaded)
        {
            line.Evaded++;
        }
        else if (blocked)
        {
            line.Blocked++;
        }
        else if (parried)
        {
            line.Parried++;
        }
        else
        {
            line.Raw += raw;
            line.Mitigated += mitigated;
        }
    }

    public void AddDealt(string key, double amount) => Line(key).Dealt += amount;

    /// <summary>
    /// A share as a whole percent, the same under every culture: the P0 format gives "25 %" under the invariant culture a
    /// Linux server can run in, and "25%" under en-US
    /// </summary>
    public static string FormatPercent(double share) => $"{share * 100:0}%";

    /// <summary>
    /// One line per side that fought: swings, how many missed and how, the average landed hit before and after
    /// mitigation, and the damage done
    /// </summary>
    public IEnumerable<string> Describe()
    {
        foreach (var key in Keys.Where(Lines.ContainsKey))
        {
            var l = Lines[key];
            string Share(int n) => l.Swings > 0 ? FormatPercent((double)n / l.Swings) : "0%";

            yield return $"{key.Replace(">", " vs ")}: {l.Swings} swings, evaded {Share(l.Evaded)}, blocked {Share(l.Blocked)}, "
                + $"parried {Share(l.Parried)}, landed {l.Landed} "
                + (l.Landed > 0 ? $"(avg {l.Raw / l.Landed:N1} before armor, {l.Mitigated / l.Landed:N1} after)" : "")
                + $"; dealt {l.Dealt:N0}";
        }
    }
}
