using System;
using ACE.Entity.Enum;
using ACE.Server.WorldObjects;

namespace ACE.Server.Entity;

/// <summary>
/// The pure formulas behind DamageEvent's rolls and modifiers: numbers in, numbers out, no game objects or randomness.
/// </summary>
internal static class DamageFormulas
{
    /// <summary>
    /// Damage multiplier for a glancing blow (partial evasion)
    /// </summary>
    public const float GlancingBlowMod = 0.5f;

    /// <summary>
    /// Returns the chance (0-1) to evade an attack.
    /// COMBAT ABILITY - Smokescreen: 10% of the remaining chance to be hit becomes chance to evade.
    /// </summary>
    public static float GetEvadeChance(uint defenseSkill, uint attackSkill, bool smokescreen)
    {
        var evadeChance = SkillCheck.GetSkillChance(defenseSkill, attackSkill);

        if (smokescreen)
        {
            var remainingChance = 1.0f - evadeChance;
            var bonus = remainingChance * 0.1f;

            evadeChance += bonus;
        }

        if (evadeChance < 0)
        {
            evadeChance = 0;
        }

        return (float)Math.Min(evadeChance, 1.0f);
    }

    /// <summary>
    /// Once an attack is evaded, a second roll (0-1) picks how well: a full evade, a glancing blow or a full hit,
    /// with an equal chance of each
    /// </summary>
    public static PartialEvasion GetEvasionType(double evasionTypeRoll)
    {
        const float fullEvadeChance = 1.0f / 3.0f;
        const float glancingBlowChance = fullEvadeChance * 2;

        return evasionTypeRoll switch
        {
            < fullEvadeChance => PartialEvasion.All,
            < glancingBlowChance => PartialEvasion.Some,
            _ => PartialEvasion.None,
        };
    }

    /// <summary>
    /// Returns the chance (0-1) to block an attack with a shield.
    /// The base chance is 5%, up to 10% depending on shield level vs attack skill. The other bonuses add together and
    /// multiply the base chance: Spec Physical Defense (up to 50%), jewels and Riposte (100%). COMBAT ABILITY - Phalanx
    /// multiplies the result.
    /// </summary>
    public static double GetBlockChance(
        uint effectiveShieldLevel,
        uint attackSkill,
        float specPhysicalDefenseBonus,
        float jewelBonus,
        float riposteBonus,
        float phalanxMod
    )
    {
        const float minBlockChance = 0.05f;

        var shieldLevelBonus = 1.0 + SkillCheck.GetSkillChance(effectiveShieldLevel, attackSkill);
        var baseBlockChance = minBlockChance * shieldLevelBonus;

        var blockChance = baseBlockChance * (1.0f + specPhysicalDefenseBonus + jewelBonus + riposteBonus);

        return blockChance * phalanxMod;
    }

    /// <summary>
    /// Returns the chance (0-1) to parry an attack with a two-handed weapon or two weapons.
    /// The base chance is up to 10%, depending on Two-handed Combat or Dual Wield skill vs attack skill. The other bonuses
    /// add together and multiply the base chance: Spec Physical Defense (up to 50%) and Riposte (100%).
    /// COMBAT ABILITY - Phalanx multiplies the result.
    /// </summary>
    public static double GetParryChance(
        uint parrySkill,
        uint attackSkill,
        float specPhysicalDefenseBonus,
        float riposteBonus,
        float phalanxMod
    )
    {
        var parryMod = SkillCheck.GetSkillChance((uint)(parrySkill * 1.5), attackSkill);

        var maxBaseParryChance = 0.1f * parryMod;

        var parryChance = maxBaseParryChance * (1.0 + specPhysicalDefenseBonus + riposteBonus);

        return parryChance * phalanxMod;
    }

    /// <summary>
    /// Damage multiplier for Dual Wield and Two-handed Combat: up to +50%, based on that skill vs the defender's
    /// physical defense
    /// </summary>
    public static float GetCombatSkillDamageBonus(uint combatSkill, uint defenderPhysicalDefense)
    {
        var moddedCombatSkill = (uint)(combatSkill * 1.5f);

        var damageMod = 0.5f * SkillCheck.GetSkillChance(moddedCombatSkill, defenderPhysicalDefense);

        return 1.0f + (float)damageMod;
    }

    /// <summary>
    /// SPEC BONUS - Physical Defense: damage taken multiplier, from 0.9 down to 0.8 at 500 physical defense
    /// </summary>
    public static float GetSpecDefenseMod(float physicalDefense)
    {
        var bonusAmount = Math.Min(physicalDefense, 500) / 50;

        return 0.9f - bonusAmount * 0.01f;
    }

    /// <summary>
    /// Damage taken multiplier for a player fighting several nearby monsters: 10% less for each one beyond the first
    /// </summary>
    public static float GetSwarmedMod(int nearbyEnemies)
    {
        var swarmedMod = 1.0f;

        // start at 1 to only count enemies beyond the first
        for (var i = 1; i < nearbyEnemies; i++)
        {
            swarmedMod *= 0.9f;
        }

        return swarmedMod;
    }

    /// <summary>
    /// Damage taken multiplier from armor imbues: 1% less per imbued piece, down to 50%
    /// </summary>
    public static float GetImbuedArmorMod(int imbuedPieces)
    {
        const float reductionPerPiece = 0.01f;

        if (imbuedPieces > 0)
        {
            return Math.Max(0.5f, 1.0f - imbuedPieces * reductionPerPiece);
        }

        return 1.0f;
    }

    /// <summary>
    /// Combines the defender's damage resistance rating with the critical damage resistance rating (critical hits) and
    /// the PK damage resistance rating (PK battles). A null rating doesn't apply.
    /// </summary>
    public static float CombineDamageResistRatings(float baseMod, float? criticalMod, float? pkMod)
    {
        var damageResistRatingMod = baseMod;

        if (criticalMod is not null)
        {
            damageResistRatingMod = Creature.AdditiveCombine(damageResistRatingMod, criticalMod.Value);
        }

        if (pkMod is not null)
        {
            damageResistRatingMod = Creature.AdditiveCombine(damageResistRatingMod, pkMod.Value);
        }

        return damageResistRatingMod;
    }

    /// <summary>
    /// The weapon tier (1-7) that scales Weapon Master debuffs, from the weapon's loot tier
    /// </summary>
    public static int GetWeaponMasterTier(int? weaponLootTier)
    {
        return Math.Clamp((weaponLootTier ?? 1) - 1, 1, 7);
    }

    /// <summary>
    /// Weapon Master debuff strength multiplier for a weapon tier (1-7, see GetWeaponMasterTier)
    /// </summary>
    public static float GetWeaponMasterTierMod(int weaponTier)
    {
        return weaponTier switch
        {
            1 => 1.0f,
            2 => 3.0f,
            3 => 4.0f,
            4 => 5.0f,
            5 => 6.0f,
            6 => 8.0f,
            7 => 10.0f,
            _ => throw new ArgumentOutOfRangeException(nameof(weaponTier), weaponTier, null),
        };
    }

    /// <summary>
    /// The lootgen damage range of weapons that can cause a Weapon Master bleed, or null for other weapon skills
    /// </summary>
    public static (int Min, int Max)? GetBleedWeaponDamageRange(Skill weaponSkill)
    {
        // todo: use each weapon's subtype for its damage range once all lootgen weapons have one
        return weaponSkill switch
        {
            Skill.Axe => (9, 132),
            Skill.Dagger => (4, 95),
            Skill.ThrownWeapon => (10, 296),
            Skill.TwoHandedCombat => (5, 107),
            _ => null,
        };
    }

    /// <summary>
    /// Where a weapon's damage falls within its type's damage range (0-1), which scales a Weapon Master bleed.
    /// Weapons outside the range clamp to 0 or 1.
    /// </summary>
    public static float GetBleedDamagePercentile(int weaponDamage, (int Min, int Max) damageRange)
    {
        var range = damageRange.Max - damageRange.Min;
        var weaponDamageRoll = weaponDamage - damageRange.Min;

        return Math.Clamp((float)weaponDamageRoll / range, 0.0f, 1.0f);
    }
}
