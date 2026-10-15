using System;
using ACE.Server.WorldObjects;

namespace ACE.Server.Entity;

/// <summary>
/// Armor and ward penetration: Armor/Ward Cleaving, Armor/Ward Rending and the Spear and Orb spec bonuses.
///
/// Penetration adds up (a 15% cleave and a 20% rend are 35%), and adds damage instead of ignoring a fixed share of
/// armor: against a target with the average armor (or ward) for its level, the average loot cleave (13.3%) adds 10%
/// damage. The bonus scales with the target's own armor against that average, up to x1.3, so it's worth more against
/// heavily armored targets and less against lightly armored ones. It's applied by removing just enough of the target's
/// armor to add that much damage, so it can never do more than ignore all of it.
///
/// The ratio is measured from the target's base armor, before buffs and debuffs, so debuffing a target's armor doesn't
/// weaken its attacker's penetration. The armor removed comes off what's left after them.
/// </summary>
public static class ArmorPenetration
{
    /// <summary>
    /// The average loot cleave roll: 10%, plus 10% x the average diminishing roll (1/3)
    /// </summary>
    public const float ReferencePenetration = 0.1f + 0.1f / 3.0f;

    /// <summary>
    /// The damage the average loot cleave adds against a target with the average armor for its level
    /// </summary>
    public const float ReferenceDamageBonus = 0.1f;

    public const float DamageBonusPerPenetration = ReferenceDamageBonus / ReferencePenetration;

    /// <summary>
    /// The most a target's armor counts for against the average for its level
    /// </summary>
    public const float MaxArmorRatio = 1.3f;

    /// <summary>
    /// The damage penetration adds against a target whose base armor (or ward) is baseArmor, where the average for
    /// its level is referenceArmor
    /// </summary>
    public static float GetDamageBonus(float penetration, float baseArmor, float referenceArmor)
    {
        if (penetration <= 0 || baseArmor <= 0 || referenceArmor <= 0)
        {
            return 0.0f;
        }

        var ratio = Math.Min(baseArmor / referenceArmor, MaxArmorRatio);

        return penetration * DamageBonusPerPenetration * ratio;
    }

    /// <summary>
    /// The share of armor (or ward) left that raises the damage taken through it by damageBonus. armor is the level
    /// that goes into SkillFormula.CalcArmorMod() / CalcWardMod(), whose curve constant is armorMod.
    /// </summary>
    public static float GetArmorRemaining(float damageBonus, float armor, float armorMod = SkillFormula.ArmorMod)
    {
        if (damageBonus <= 0 || armor <= 0)
        {
            return 1.0f;
        }

        var reducedArmor = (armor + armorMod) / (1.0f + damageBonus) - armorMod;

        return Math.Clamp(reducedArmor / armor, 0.0f, 1.0f);
    }

    /// <summary>
    /// The share of armor (or ward) left after penetration: see GetDamageBonus() and GetArmorRemaining()
    /// </summary>
    public static float GetArmorRemaining(
        float penetration,
        float baseArmor,
        float referenceArmor,
        float armor,
        float armorMod = SkillFormula.ArmorMod
    )
    {
        return GetArmorRemaining(GetDamageBonus(penetration, baseArmor, referenceArmor), armor, armorMod);
    }

    /// <summary>
    /// A player's average armor and ward against the reference player's (TierPower), which wears the bottom of each
    /// tier's x1-x2 roll range: the middle of that range. Tinks and clothing push real kits higher, so raise this if
    /// players' equipped armor shows they average more.
    /// </summary>
    public const float PlayerReferenceMultiplier = 1.5f;

    /// <summary>
    /// The average armor on one body part at the defender's level: a monster's from the archetype system, a player's
    /// from the middle of the reference player's roll range
    /// </summary>
    public static float GetReferenceArmor(Creature defender)
    {
        var level = Math.Max(1, defender.Level ?? 1);

        return defender is Player
            ? TierPower.GetReferenceArmor(level) * PlayerReferenceMultiplier
            : TierPower.GetMonsterArmorWard(level);
    }

    /// <summary>
    /// The average ward at the defender's level, as GetReferenceArmor()
    /// </summary>
    public static float GetReferenceWard(Creature defender)
    {
        var level = Math.Max(1, defender.Level ?? 1);

        return defender is Player
            ? TierPower.GetReferenceWard(level) * PlayerReferenceMultiplier
            : TierPower.GetMonsterArmorWard(level);
    }
}

/// <summary>
/// What a caster's spell does to its target's ward: penetration (Ward Cleaving, Ward Rending, SPEC BONUS - War Magic
/// (Orb); see ArmorPenetration) and RATING - WardPen, which still lowers the ward by a flat share
/// </summary>
public readonly record struct WardPenetration(float Penetration, float RatingMod)
{
    public static readonly WardPenetration None = new(0.0f, 1.0f);
}
