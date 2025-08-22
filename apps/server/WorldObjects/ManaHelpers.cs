using System;
using System.Collections.Generic;
using System.Linq;
using ACE.Server.WorldObjects;

namespace ACE.Server.WorldObjects;

public static class ManaHelpers
{
    public sealed class ChargeResult
    {
        public Dictionary<WorldObject, int> Items { get; } = new();
        public int TotalGranted { get; internal set; }
        public int RemainingPool { get; internal set; }
    }

    /// <summary>
    /// Distribute a POOL of mana across equipped items that can hold mana, using the same
    /// "ration in rounds" approach used by ManaStone when used on the player:
    /// - Evenly ration the current pool across all items that still need mana (at least 1 per item).
    /// - Clamp each item to its max; repeat rounds until pool exhausted or everything is full.
    /// - Optionally apply the luminance augment multiplier, returning any overflow back to the pool.
    /// </summary>
    public static ChargeResult DistributePoolLikeManaStone(Player player, int pool, bool applyLumAug = true)
    {
        var result = new ChargeResult();
        if (player == null || pool <= 0) { result.RemainingPool = Math.Max(pool, 0); return result; }

        // Original behavior targets EQUIPPED items that have both Cur and Max mana and are not full
        var equippedNeedingMana = player.EquippedObjects.Values
            .Where(i => i.ItemCurMana.HasValue && i.ItemMaxMana.HasValue && i.ItemCurMana < i.ItemMaxMana)
            .ToList();

        while (pool > 0)
        {
            var needers = equippedNeedingMana.Where(i => i.ItemCurMana < i.ItemMaxMana).ToList();
            if (needers.Count < 1) break;

            var ration = Math.Max(pool / needers.Count, 1); // at least 1 per item per round

            foreach (var item in needers)
            {
                var max = item.ItemMaxMana!.Value;
                var cur = item.ItemCurMana ?? 0;
                var space = (int)(max - cur);
                if (space <= 0) continue;

                var adjusted = Math.Min(ration, space);

                // Deduct from pool FIRST (matching ManaStone flow)
                pool -= adjusted;

                // Optional luminance augment multiplier (same shape as ManaStone)
                if (applyLumAug && player.LumAugItemManaGain != 0)
                {
                    var boosted = (int)Math.Round(adjusted * Creature.GetPositiveRatingMod(player.LumAugItemManaGain * 5));
                    if (boosted > space)
                    {
                        // Any overflow beyond the item’s space is returned to the pool
                        var diff = boosted - space;
                        adjusted = space;
                        pool += diff;
                    }
                    else
                    {
                        adjusted = boosted;
                    }
                }

                // Apply to item, track totals
                item.ItemCurMana = cur + adjusted;

                if (!result.Items.ContainsKey(item)) result.Items[item] = 0;
                result.Items[item] += adjusted;
                result.TotalGranted += adjusted;

                if (pool <= 0) break;
            }
        }

        result.RemainingPool = Math.Max(pool, 0);
        return result;
    }

    /// <summary>
    /// Helper to compute the pool as a percent of TOTAL equipped max mana.
    /// </summary>
    public static int ComputePoolFromEquippedMax(Player player, double percent /* e.g., 0.20 */)
    {
        if (player == null || percent <= 0) return 0;
        var sumMax = player.EquippedObjects.Values
            .Where(i => i.ItemMaxMana.HasValue && i.ItemMaxMana.Value > 0)
            .Sum(i => i.ItemMaxMana!.Value);
        return (int)Math.Round(sumMax * percent);
    }
}
