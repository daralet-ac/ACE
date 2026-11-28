using System;
using ACE.Entity.Enum;
using ACE.Entity.Models;
using ACE.Server.Entity;

namespace ACE.Server.WorldObjects;

partial class Player
{
    /// <summary>
    /// The player's strongest Rested buff from a campfire (CampfireRest1-3), for the chat line when it goes on and /rested:
    /// the XP it adds, the part of that from the fire's Allegiance Rank (CampfireRules), and the seconds it has left (null if
    /// it doesn't run out). Null without one.
    /// </summary>
    public (float BuffXp, float RankXp, double? SecondsLeft)? GetCampfireRest()
    {
        PropertiesEnchantmentRegistry best = null;
        var bestXp = 0.0f;

        foreach (var enchantment in EnchantmentManager.GetEnchantments(SpellCategory.CampfireRested))
        {
            var xp = CampfireRules.BuffXp(
                enchantment.StatModValue,
                enchantment.StatModType.HasFlag(EnchantmentTypeFlags.Multiplicative)
            );

            if (best == null || xp > bestXp)
            {
                best = enchantment;
                bestXp = xp;
            }
        }

        if (best == null)
        {
            return null;
        }

        var rankXp = CampfireRules.RankXp((uint)best.SpellId);
        double? secondsLeft = best.Duration < 0 ? null : Math.Max(0, best.Duration + best.StartTime);

        return (bestXp, rankXp, secondsLeft);
    }
}
