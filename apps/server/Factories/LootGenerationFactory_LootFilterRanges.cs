// The ranges loot rolls each stat in at each tier, for the Daralet plugin's loot filters (@playerui rollranges): the
// same formulas as LootGenerationFactory_LootRollRanges, but for any item of a kind rather than one item. Where the
// range depends on the item (a weapon's speed, how many mods rolled, headwear), it's the lowest to the highest any
// item of that kind can roll.
using System;
using System.Collections.Generic;
using System.Linq;
using ACE.Database;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Entity.Models;
using ACE.Server.Entity;
using ACE.Server.Entity.PlayerUi;

namespace ACE.Server.Factories;

public static partial class LootGenerationFactory
{
    private static List<PlayerUiRollRange> lootFilterRanges;

    /// <summary>
    /// Every filter stat's range at every tier, by the Market API's names: its category, and the filter parameter without
    /// "min" or "max". Each is in the units the filter is typed in: percents as percents (+15% is 15), others whole.
    /// Worked out once: the tables and weenies don't change while the server runs.
    /// </summary>
    public static List<PlayerUiRollRange> GetLootFilterRanges()
    {
        return lootFilterRanges ??= BuildLootFilterRanges(wcid => DatabaseManager.World.GetCachedWeenie(wcid));
    }

    /// <summary>
    /// The ranges, with the loot weapons' weenies from getWeenie (null for one it hasn't got: that weapon is left out)
    /// </summary>
    internal static List<PlayerUiRollRange> BuildLootFilterRanges(Func<uint, Weenie> getWeenie)
    {
        var ranges = new List<PlayerUiRollRange>();

        for (var tier = 1; tier <= 8; tier++)
        {
            void Add(string category, string stat, double minimum, double maximum, Func<double, double> display)
            {
                // nothing to say about a stat that can't roll above nothing at this tier (a tier 1 ring's ratings)
                if (maximum < minimum || display(maximum) <= 0)
                {
                    return;
                }

                ranges.Add(new(category, stat, tier, Math.Round(display(minimum), 2), Math.Round(display(maximum), 2)));
            }

            AddWeaponFilterRanges(tier, getWeenie, Add);
            AddArmorFilterRanges(tier, Add);
            AddJewelryFilterRanges(tier, Add);
        }

        return ranges;
    }

    private delegate void AddFilterRange(string category, string stat, double minimum, double maximum, Func<double, double> display);

    private static void AddWeaponFilterRanges(int tier, Func<uint, Weenie> getWeenie, AddFilterRange add)
    {
        var index = Math.Clamp(tier - 1, 0, 7);

        // melee and thrown Damage: every loot weapon's own range at its speed and variance (GetMeleeDamageRollRange)
        var damage = MeleeLootWeenies(getWeenie)
            .Select(w => GetMeleeDamageRollRange(w.WeaponTime, w.Variance, w.Thrown, tier))
            .Where(r => r.Maximum > 0)
            .ToList();

        if (damage.Count > 0)
        {
            add("Weapon", "BaseDamage", Math.Round(damage.Min(r => r.Minimum)), Math.Round(damage.Max(r => r.Maximum)), LootWhole);
        }

        if (tier >= 2)
        {
            var (minimum, maximum) = TierPower.GetMultiplierRange(tier);
            add("Weapon", "DamageMod", minimum, maximum, LootBonusPercent);
            add("Weapon", "ElementalDamageMod", minimum, maximum, LootBonusPercent);

            var war = TierPower.GetWarRestorationRange(tier);
            var nether = TierPower.GetNetherRestorationRange(tier);
            add("Weapon", "RestorationMod", Math.Min(war.Minimum, nether.Minimum), Math.Max(war.Maximum, nether.Maximum), LootBonusPercent);
        }
        else
        {
            // tier 1 launchers roll in the range from before the tier rework, sized by each launcher's speed
            var launchers = LauncherLootWeenies(getWeenie)
                .Select(w => GetPreviousLauncherDamageModRange(w.Skill, tier, GetWeaponHitsPerSecond(w.WeaponTime, true, tier)))
                .Where(r => r.Maximum > r.Minimum)
                .ToList();

            if (launchers.Count > 0)
            {
                add("Weapon", "DamageMod", launchers.Min(r => r.Minimum), launchers.Max(r => r.Maximum), LootBonusPercent);
            }
        }

        // attack, or a caster's war or life magic, and the defenses: 10-20% plus the tier's bonus, scaled down to 75% when
        // both rolled (AddLootWeaponModRanges); a caster staff's defenses carry its own bonus on top
        var modMinimum = (LootTables.WeaponBaseOffenseMod + LootTables.WeaponOffenseModBonusPerTier[index]) * GetRolledTypesMultiplier(2);
        var modMaximum = LootTables.WeaponBaseOffenseMod * 2 + LootTables.WeaponOffenseModBonusPerTier[index];
        var staffMaximum = LootTables.WeaponDefenseModBonusPerTier[index] + 0.1;

        add("Weapon", "AttackMod", modMinimum, modMaximum, LootPercent);
        add("Weapon", "WarMagicMod", modMinimum, modMaximum, LootPercent);
        add("Weapon", "LifeMagicMod", modMinimum, modMaximum, LootPercent);
        add("Weapon", "PhysDefMod", modMinimum, modMaximum + staffMaximum, LootPercent);
        add("Weapon", "MagicDefMod", modMinimum, modMaximum + staffMaximum, LootPercent);
    }

    /// <summary>
    /// The armor's skill mods, a slot's share (the loot filters compare them per slot, as the market does): 10-20% (20-40%
    /// on headwear) plus the tier's bonus, over 10 slots, scaled down by how many rolled, at most 5 (AddLootArmorModRanges).
    /// Misc clothing's vital mods are halved, for the whole piece.
    /// </summary>
    private static void AddArmorFilterRanges(int tier, AddFilterRange add)
    {
        var tierBonus = LootTables.ArmorSkillModBonusPerTier[Math.Clamp(tier - 1, 0, 7)];
        var baseMod = LootTables.ArmorBaseSkillMod;

        var minimum = (baseMod + tierBonus) * GetRolledTypesMultiplier(5) / 10.0;
        var maximum = (baseMod * 2 * 2 + tierBonus) / 10.0;

        foreach (var stat in new[]
        {
            "ArmorWarMagicMod", "ArmorLifeMagicMod", "ArmorManaRegenMod", "ArmorManaConversionMod", "ArmorAttackMod",
            "ArmorShieldMod", "ArmorDualWieldMod", "ArmorThieveryMod", "ArmorRunMod", "ArmorStaminaRegenMod",
            "ArmorPhysicalDefMod", "ArmorMagicDefMod", "ArmorTwohandedCombatMod", "ArmorHealthRegenMod",
            "ArmorPerceptionMod", "ArmorDeceptionMod",
        })
        {
            add("Armor", stat, minimum, maximum, LootPercent);
        }

        var clothingMinimum = (baseMod + tierBonus) * GetRolledTypesMultiplier(3) * 0.5;
        var clothingMaximum = (baseMod * 2 + tierBonus) * 0.5;

        foreach (var stat in new[] { "ArmorHealthMod", "ArmorManaMod", "ArmorStaminaMod" })
        {
            add("Clothing", stat, clothingMinimum, clothingMaximum, LootPercent);
        }
    }

    /// <summary>
    /// A ring's or bracelet's ward up to the next tier's bottom, a necklace's doubled, and the ratings (AddLootJewelryRanges)
    /// </summary>
    private static void AddJewelryFilterRanges(int tier, AddFilterRange add)
    {
        add("Jewelry", "WardLevel", GetBaseWardOfTier(tier), GetBaseWardOfTier(Math.Clamp(tier + 1, 1, 9)) * 2, LootWhole);

        foreach (var stat in new[] { "GearCritDamage", "GearCritDamageResist", "GearDamage", "GearDamageResist" })
        {
            add("Jewelry", stat, (tier - 1) / 2.0, tier - 1.0, Math.Floor);
        }

        foreach (var stat in new[] { "GearHealingBoost", "GearMaxHealth" })
        {
            add("Jewelry", stat, 2 * (tier - 1), 4 * (tier - 1), Math.Floor);
        }
    }

    /// <summary>The speed and variance of every melee and thrown weapon loot makes, from its weenie.</summary>
    private static IEnumerable<(int WeaponTime, double Variance, bool Thrown)> MeleeLootWeenies(Func<uint, Weenie> getWeenie)
    {
        var melee = new[]
        {
            LootTables.HeavyWeaponsMatrix, LootTables.LightWeaponsMatrix, LootTables.FinesseWeaponsMatrix, LootTables.TwoHandedWeaponsMatrix,
        }.SelectMany(m => m).SelectMany(row => row).Select(wcid => ((uint)wcid, false));

        var thrown = LootTables.ThrownWeaponMatrix.SelectMany(row => row).Select(wcid => (wcid, true));

        foreach (var (wcid, isThrown) in melee.Concat(thrown).Distinct())
        {
            var weenie = getWeenie(wcid);
            var weaponTime = weenie?.GetProperty(PropertyInt.WeaponTime);

            if (weaponTime is > 0)
            {
                yield return (weaponTime.Value, weenie.GetProperty(PropertyFloat.DamageVariance) ?? 0.0, isThrown);
            }
        }
    }

    /// <summary>The skill and speed of every launcher loot makes, from its weenie.</summary>
    private static IEnumerable<(Skill Skill, int WeaponTime)> LauncherLootWeenies(Func<uint, Weenie> getWeenie)
    {
        foreach (var wcid in LootTables.MissileWeaponsMatrices.SelectMany(m => m).SelectMany(row => row).Distinct())
        {
            var weenie = getWeenie((uint)wcid);
            var weaponTime = weenie?.GetProperty(PropertyInt.WeaponTime);
            var skill = weenie?.GetProperty(PropertyInt.WeaponSkill);

            if (weaponTime is > 0 && skill.HasValue)
            {
                yield return ((Skill)skill.Value, weaponTime.Value);
            }
        }
    }
}
