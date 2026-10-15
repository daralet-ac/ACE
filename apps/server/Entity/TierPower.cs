using System;
using ACE.Entity.Enum;

namespace ACE.Server.Entity;

/// <summary>
/// The tier tables everything else reads: player gear (armor, ward, weapon damage, caster and launcher mods,
/// restoration, ammo), monster stats, and the damage and kill-time targets combat is built around.
///
/// Built up from player armor (docs/tier-difficulty-scaling.md):
///  - Armor and ward double every tier. The reference player wears the bottom roll of their tier's armor and ward.
///  - Monsters take 1% x tier of the reference player's health per second (T2 2% ... T8 8%), so reaching up a tier
///    means much more damage taken.
///  - Weapon damage doubles every tier. Like armor, a tier's weapons roll from its target DPS (the bottom roll, about
///    6% of a same-tier reference player's health per second in PvP) up to the next tier's.
///  - Monster health grows about x1.6 a tier in clean numbers, and armor is solved from it so a reference player
///    (bottom rolls) kills a same-level monster in 5 s at T1, then 10 + (tier - 1) x 10/7 s at each tier start (T8 20 s).
///  - Casters and launchers share one set of multiplier ranges. Like melee, each tier rolls x1-x2: the bottom roll with
///    the tier's spell level or ammo deals the target DPS, the top double it. The ranges climb x1.41 a tier and spell
///    and ammo base damage carry the rest, so a tier's range overlaps the next one's.
///
/// Tables by level are indexed by tier start (L1, 10, 20, 30, 40, 50, 75, 100, 125) and interpolated within a tier
/// the same way Creature.ApplyArchetypeSystem() weights a monster's level. Tables by loot tier are indexed tier - 1.
/// </summary>
public static class TierPower
{
    /// <summary>
    /// The level each tier starts at, index 0 = T1; index 8 (L125) is the end point levels 100+ interpolate toward
    /// </summary>
    public static readonly int[] TierStartLevels = [1, 10, 20, 30, 40, 50, 75, 100, 125];

    // --- by level (tier start) ---

    // the bottom Covenant armor roll of each tier
    private static readonly float[] ReferenceArmorTable = [50, 100, 200, 400, 800, 1600, 3200, 6400, 12800];

    // the ward of a full bottom-roll kit (9 Covenant pieces, shirt, pants, necklace, 2 rings, 2 bracelets)
    private static readonly float[] ReferenceWardTable = [48, 97, 194, 388, 776, 1552, 3104, 6208, 12416];

    // % of the reference player's health a same-level monster takes per second
    private static readonly float[] IncomingPercentTable = [1, 2, 3, 4, 5, 6, 7, 8, 9];

    // seconds for a reference player to kill a same-level monster
    private static readonly float[] KillTimeTable = [5.0f, 11.43f, 12.86f, 14.29f, 15.71f, 17.14f, 18.57f, 20.0f, 21.43f];

    // clean numbers growing about x1.6 a tier, so the damage a player's hits land grows steadily
    private static readonly float[] MonsterHealthTable = [20, 100, 175, 300, 500, 800, 1250, 2000, 3000];

    // solved from the health: armor = 100 x kill time x reference DPS against no armor / health - 100, for the
    // reference player at the tier's first level with the tier's bottom weapon roll
    private static readonly float[] MonsterArmorWardTable = [10, 20, 102, 192, 310, 493, 784, 1173, 1838];

    /// <summary>
    /// The kill time fellowship-required dungeons use for everyone: the T6 (level 50) target
    /// </summary>
    public static float FellowshipKillTime => KillTimeTable[5];

    // --- by loot tier (index tier - 1) ---

    // a tier's bottom weapon roll, which doubles every tier; weapons roll from it up to the next tier's (x1-x2)
    private static readonly float[] WeaponTargetDpsTable = [5, 10, 20, 40, 80, 160, 320, 640];

    // the weapon DPS each tier was built around before this rework
    private static readonly float[] PreviousWeaponDpsTable = [5, 10, 15, 22, 33, 50, 75, 110];

    // caster ElementalDamageMod / launcher DamageMod by tier (index tier - 1; T1 keeps 0.75-1.0). The middle of the
    // ranges these replaced (1.0-1.4, 1.4-2.0 ... 8.0-11.2) dealt the target with the tier's spell or ammo; the
    // bottom roll does now, and the top doubles it
    private static readonly float[] MultiplierMinimum = [0.75f, 1.2f, 1.7f, 2.4f, 3.4f, 4.8f, 6.7f, 9.6f];
    private static readonly float[] MultiplierMaximum = [1.0f, 2.4f, 3.4f, 4.8f, 6.8f, 9.6f, 13.4f, 19.2f];

    // restoration rolls at the same position as the damage roll: tier t from index t - 1 to index t
    private static readonly float[] WarRestorationLadder = [1.0f, 1.0f, 1.12f, 1.25f, 1.40f, 1.57f, 1.76f, 1.97f, 2.21f];
    private static readonly float[] NetherRestorationLadder = [1.0f, 1.0f, 1.20f, 1.44f, 1.73f, 2.07f, 2.49f, 2.99f, 3.58f];

    // max damage of each tier's ammo
    private static readonly int[] ArrowMaxDamage = [6, 10, 14, 20, 28, 40, 56, 80];
    private static readonly int[] QuarrelMaxDamage = [9, 14, 20, 28, 40, 56, 80, 112];
    private static readonly int[] DartMaxDamage = [12, 18, 25, 35, 50, 70, 100, 140];

    // average damage of the spell level a monster of each tier casts (T2-T8 = spell levels I-VII)
    private static readonly float[] AvgSpellDamageTable = [9, 9, 18, 36, 60, 90, 135, 195];

    /// <summary>
    /// Each active Void damage over time on a target raises the direct damage it takes by this much
    /// </summary>
    public const float VoidDotVulnerabilityPerDot = 0.05f;

    // --- level ---

    /// <summary>
    /// The tier a monster of this level has (Creature.SetTierByCreatureLevel), and the loot tier its level wears
    /// </summary>
    public static int GetTierForLevel(int level)
    {
        switch (level)
        {
            case < 10:
                return 1;
            case < 20:
                return 2;
            case < 30:
                return 3;
            case < 40:
                return 4;
            case < 50:
                return 5;
            case < 75:
                return 6;
            case < 100:
                return 7;
            default:
                return 8;
        }
    }

    /// <summary>
    /// The tier index (0 = T1) and how far through the tier a level is, as Creature.ApplyArchetypeSystem() weights it
    /// </summary>
    public static (int TierIndex, float Weight) GetTierIndexAndWeight(float level)
    {
        var index = GetTierForLevel((int)level) - 1;
        var start = TierStartLevels[index];
        var span = index == 0 ? 9.0f : TierStartLevels[index + 1] - start;

        return (index, (level - (index == 0 ? 0 : start)) / span);
    }

    /// <summary>
    /// A level table's value at a tier index and weight, as the archetype system interpolates its own tables
    /// </summary>
    public static float Interpolate(float[] table, int tierIndex, float weight)
    {
        return table[tierIndex] + (table[tierIndex + 1] - table[tierIndex]) * weight;
    }

    private static float AtLevel(float[] table, float level)
    {
        var (index, weight) = GetTierIndexAndWeight(level);

        return Interpolate(table, index, weight);
    }

    public static float GetReferenceArmor(float level) => AtLevel(ReferenceArmorTable, level);

    public static float GetReferenceArmor(int tierIndex, float weight) => Interpolate(ReferenceArmorTable, tierIndex, weight);

    public static float GetReferenceWard(float level) => AtLevel(ReferenceWardTable, level);

    public static float GetReferenceWard(int tierIndex, float weight) => Interpolate(ReferenceWardTable, tierIndex, weight);

    public static float GetIncomingPercent(int tierIndex, float weight) => Interpolate(IncomingPercentTable, tierIndex, weight);

    /// <summary>
    /// The incoming % fellowship-required dungeons use for everyone: the T6 (level 50) target
    /// </summary>
    public static float FellowshipIncomingPercent => IncomingPercentTable[5];

    public static float GetKillTime(float level) => AtLevel(KillTimeTable, level);

    public static float GetMonsterHealth(float level) => AtLevel(MonsterHealthTable, level);

    public static float GetMonsterHealth(int tierIndex, float weight) => Interpolate(MonsterHealthTable, tierIndex, weight);

    public static float GetMonsterArmorWard(float level) => AtLevel(MonsterArmorWardTable, level);

    public static float GetMonsterArmorWard(int tierIndex, float weight) => Interpolate(MonsterArmorWardTable, tierIndex, weight);

    /// <summary>
    /// The average damage of the spells monsters of this tier cast (tier 1-8)
    /// </summary>
    public static float GetAvgSpellDamage(int tier) => AvgSpellDamageTable[GetIndex(tier)];

    // --- loot tier ---

    public static float GetWeaponTargetDps(int tier) => WeaponTargetDpsTable[GetIndex(tier)];

    /// <summary>
    /// How much more damage a tier's weapons deal than before this rework: scales the per-subtype damage tables
    /// </summary>
    public static float GetWeaponScale(int tier) => WeaponTargetDpsTable[GetIndex(tier)] / PreviousWeaponDpsTable[GetIndex(tier)];

    /// <summary>
    /// Scales a table of weapon damage by tier (index 0 = tier 1) by each tier's weapon scale
    /// </summary>
    public static int[] ScaleWeaponDamage(int[] damageByTier)
    {
        var scaled = new int[damageByTier.Length];

        for (var i = 0; i < damageByTier.Length; i++)
        {
            scaled[i] = (int)Math.Round(damageByTier[i] * GetWeaponScale(i + 1), MidpointRounding.AwayFromZero);
        }

        return scaled;
    }

    /// <summary>
    /// The bottoms (or tops) of the multiplier ranges by tier (index 0 = tier 1), for a damage mod table. Tier 1 keeps
    /// the table's own value: tier 1 casters don't roll a mod, and tier 1 launchers keep theirs.
    /// </summary>
    public static float[] GetMultiplierTable(float[] tier1Table, bool maximum)
    {
        var table = new float[8];

        for (var i = 0; i < table.Length; i++)
        {
            table[i] = i == 0 ? tier1Table[0] : maximum ? MultiplierMaximum[i] : MultiplierMinimum[i];
        }

        return table;
    }

    /// <summary>
    /// The range a caster's ElementalDamageMod or a launcher's DamageMod rolls in
    /// </summary>
    public static (float Minimum, float Maximum) GetMultiplierRange(int tier)
    {
        var index = GetIndex(tier);

        return (MultiplierMinimum[index], MultiplierMaximum[index]);
    }

    public static (float Minimum, float Maximum) GetWarRestorationRange(int tier)
    {
        var index = GetIndex(tier);

        return (WarRestorationLadder[index], WarRestorationLadder[index + 1]);
    }

    public static (float Minimum, float Maximum) GetNetherRestorationRange(int tier)
    {
        var index = GetIndex(tier);

        return (NetherRestorationLadder[index], NetherRestorationLadder[index + 1]);
    }

    /// <summary>
    /// A caster's restoration mod for a damage roll at the given position (0-1) in its tier's range
    /// </summary>
    public static float GetRestorationForRoll(int tier, float rollPosition, bool nether)
    {
        var (minimum, maximum) = nether ? GetNetherRestorationRange(tier) : GetWarRestorationRange(tier);

        return minimum + (maximum - minimum) * Math.Clamp(rollPosition, 0.0f, 1.0f);
    }

    /// <summary>
    /// Where a value sits in a range, 0 at the bottom and 1 at the top
    /// </summary>
    public static float GetRollPosition(double value, (double Minimum, double Maximum) range)
    {
        var width = range.Maximum - range.Minimum;

        return width <= 0 ? 0.0f : (float)((value - range.Minimum) / width);
    }

    /// <summary>
    /// The armor level range a tier's loot armor rolls in, for an armor style's base armor level (T1 doesn't roll)
    /// </summary>
    public static (float Minimum, float Maximum) GetArmorRollRange(int baseArmorLevel, int tier)
    {
        return GetDoublingRange(baseArmorLevel, tier);
    }

    /// <summary>
    /// The ward level range a tier's loot armor rolls in per armor slot, for its weight class's base ward
    /// </summary>
    public static (float Minimum, float Maximum) GetWardRollRange(int baseWardLevel, int tier)
    {
        return GetDoublingRange(baseWardLevel, tier);
    }

    /// <summary>
    /// The armor level range shields keep from before this rework: a shield's armor feeds the block check, not
    /// damage mitigation, so it doesn't double
    /// </summary>
    public static (float Minimum, float Maximum) GetShieldArmorRollRange(int baseArmorLevel, int tier)
    {
        var t = Math.Clamp(tier, 1, 8);

        return (baseArmorLevel * (t - 1), baseArmorLevel * t);
    }

    /// <summary>
    /// The bottom of a ring or bracelet's ward roll at a tier (a necklace's is double); a tier rolls up to the next
    /// tier's bottom. Tier 9 is the top of tier 8.
    /// </summary>
    public static int GetJewelryBaseWard(int tier)
    {
        var t = Math.Clamp(tier, 1, 9);

        return t <= 2 ? 4 : 4 << (t - 2);
    }

    /// <summary>
    /// The max damage of a tier's arrows, crossbow bolts or atlatl darts
    /// </summary>
    public static int GetAmmoMaxDamage(Skill weaponSkill, int tier)
    {
        var index = GetIndex(tier);

        return weaponSkill switch
        {
            Skill.Crossbow => QuarrelMaxDamage[index],
            Skill.ThrownWeapon or Skill.MissileWeapons => DartMaxDamage[index],
            _ => ArrowMaxDamage[index],
        };
    }

    // --- the scale before this rework, which workmanship is fitted to (WorkmanshipChance.FromPowerScore) ---

    private static readonly float[] PreviousCasterMinMod = [0.75f, 1.0f, 1.2f, 1.4f, 1.9f, 2.5f, 3.5f, 4.5f];
    private static readonly float[] PreviousCasterMaxMod = [1.0f, 1.3f, 1.6f, 2.1f, 2.75f, 3.75f, 4.75f, 6.0f];
    private static readonly int[] PreviousJewelryBaseWard = [4, 4, 7, 11, 14, 18, 25, 38, 50];
    private static readonly int[] PreviousArrowMaxDamage = [6, 8, 10, 12, 14, 16, 18, 20];
    private static readonly int[] PreviousQuarrelMaxDamage = [9, 12, 15, 18, 21, 24, 27, 30];
    private static readonly int[] PreviousDartMaxDamage = [12, 16, 20, 24, 28, 32, 36, 40];

    /// <summary>
    /// The weapon DPS a tier was built around before this rework (tier 8 is workmanship's reference)
    /// </summary>
    public static float GetPreviousWeaponDps(int tier) => PreviousWeaponDpsTable[GetIndex(tier)];

    public static (float Minimum, float Maximum) GetPreviousCasterModRange(int tier)
    {
        var index = GetIndex(tier);

        return (PreviousCasterMinMod[index], PreviousCasterMaxMod[index]);
    }

    public static int GetPreviousJewelryBaseWard(int tier) => PreviousJewelryBaseWard[Math.Clamp(tier, 1, 9) - 1];

    /// <summary>
    /// The ammo max damage launchers were sized against before the tier rework. Atlatls (MissileWeapons) used the arrow
    /// values then, so this keeps that.
    /// </summary>
    public static int GetPreviousAmmoMaxDamage(Skill weaponSkill, int tier)
    {
        var index = GetIndex(tier);

        return weaponSkill switch
        {
            Skill.Crossbow => PreviousQuarrelMaxDamage[index],
            Skill.ThrownWeapon => PreviousDartMaxDamage[index],
            _ => PreviousArrowMaxDamage[index],
        };
    }

    /// <summary>
    /// The armor level range a tier rolled in before this rework (and shields still do)
    /// </summary>
    public static (float Minimum, float Maximum) GetPreviousArmorRollRange(int baseLevel, int tier)
    {
        var t = Math.Clamp(tier, 1, 8);

        return (baseLevel * (t - 1), baseLevel * t);
    }

    /// <summary>
    /// A value moved from one range to the same position in another. Not clamped, so tinks above the top of the
    /// range carry over.
    /// </summary>
    public static double MapToRange(double value, (double Minimum, double Maximum) from, (double Minimum, double Maximum) to)
    {
        var position = GetRollPosition(value, from);

        return to.Minimum + (to.Maximum - to.Minimum) * position;
    }

    private static (float Minimum, float Maximum) GetDoublingRange(int baseLevel, int tier)
    {
        var t = Math.Clamp(tier, 2, 8);
        var bottom = 1 << (t - 2);

        return (baseLevel * bottom, baseLevel * bottom * 2);
    }

    private static int GetIndex(int tier)
    {
        return Math.Clamp(tier, 1, 8) - 1;
    }
}
