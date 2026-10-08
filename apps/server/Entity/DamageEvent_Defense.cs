using System;
using ACE.Common;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.WorldObjects;

namespace ACE.Server.Entity;

public partial class DamageEvent
{
    private void SetInvulnerable(Creature defender)
    {
        _invulnerable = false;

        if (_playerDefender is { UnderLifestoneProtection: true })
        {
            LifestoneProtection = true;
            _playerDefender.HandleLifestoneProtection();
            _invulnerable = true;
        }

        if (defender.Invincible)
        {
            _invulnerable = true;
        }
    }

    /// <summary>
    /// Checks for Overpower, Steady Strike, Fury, and Backstab auto-hits.
    /// If evade succeeded, determine if evade was full, partial, or none.
    /// Equal chance for each evasion type to occur.
    /// </summary>
    private void SetEvaded(Creature attacker, Creature defender)
    {
        var playerAttacker = attacker as Player;
        var playerDefender = defender as Player;

        Evaded = false;
        _evasionMod = 1.0f;
        PartialEvasion = PartialEvasion.None;

        if (defender.CombatMode is CombatMode.NonCombat)
        {
            return;
        }

        // Check for guaranteed hits
        var isOverpower = CheckForOverpower(attacker, defender);
        var isFuryNoEvade = CheckForCombatAbilityEnrageNoEvade(playerAttacker);
        var isBackstabNoEvade = CheckForCombatAbilityBackstabStealthNoEvade(playerAttacker, defender);

        if (isOverpower || isFuryNoEvade || isBackstabNoEvade || attacker == defender)
        {
            return;
        }

        // COMBAT ABILITY - Aegis: attacks can't be evaded, fully or partially.
        if (playerDefender is { AegisIsActive: true })
        {
            return;
        }

        // COMBAT ABILITY - Evasive Stance: flat 25% chance to fully evade any attack, independent of defense skill.
        if (playerDefender is { EvasiveStanceIsActive: true } && ThreadSafeRandom.Next(0.0f, 1.0f) < 0.25f)
        {
            Evaded = true;
            PartialEvasion = PartialEvasion.All;
            return;
        }

        // Roll combat hit chance
        var attackRoll = ThreadSafeRandom.Next(0.0f, 1.0f);
        if (attackRoll > GetEvadeChance())
        {
            return;
        }

        // Roll evade type (33% for each evade type)
        const float fullEvadeChance = 1.0f / 3.0f;
        const float partialEvadeChance = fullEvadeChance * 2;

        var partialEvadeRoll = ThreadSafeRandom.Next(0.0f, 1.0f);

        switch (partialEvadeRoll)
        {
            case < fullEvadeChance:
                PartialEvasion = PartialEvasion.All;
                Evaded = true;
                break;
            case < partialEvadeChance:
                _evasionMod = 0.5f;
                PartialEvasion = PartialEvasion.Some; // glancing blow
                Evaded = false;
                break;
            default:
                _evasionMod = 1.0f;
                PartialEvasion = PartialEvasion.None;
                Evaded = false;
                break;
        }

        if (playerDefender is not null && PartialEvasion == PartialEvasion.Some)
        {
            playerDefender.CheckForSigilTrinketOnAttackEffects(playerAttacker, this, Skill.PhysicalDefense, SigilTrinketPhysicalDefenseEffect.Evasion);
        }
    }

    private bool CheckForOverpower(Creature attacker, Creature defender)
    {
        if (attacker.Overpower == null)
        {
            return false;
        }

        _overpower = Creature.GetOverpower(attacker, defender);
        return _overpower;
    }

    private bool CheckForCombatAbilityEnrageNoEvade(Player playerAttacker)
    {
        if (playerAttacker is not {FuryEnrageIsActive: true})
        {
            return false;
        }

        Evaded = false;
        PartialEvasion = PartialEvasion.None;
        _evasionMod = 1f;

        return true;
    }

    /// <summary>
    /// Attack cannot be evaded if Backstab ability activated when attacking from behind and stealthed.
    /// Targets with Phalanx active cannot be sneak attacked, so they are never considered "behind".
    /// </summary>
    private bool CheckForCombatAbilityBackstabStealthNoEvade(Player playerAttacker, Creature creatureTarget)
    {
        if (playerAttacker is {BackstabIsActive: true, IsAttackFromStealth: true}
            && playerAttacker.IsBehindTargetCreature(creatureTarget)
            && creatureTarget is not Player { PhalanxIsEffective: true })
        {
            Evaded = false;
            PartialEvasion = PartialEvasion.None;

            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks attack angle and if defender has Spec Shield and/or Phalanx is active. If a block is possible,
    /// check for and combine bonuses from Spec Physical Defense, Phalanx Ability, and Block Rating.
    /// Roll and set Blocked accordingly.
    /// <list type="bullet">
    /// <item>Spec Bonus - Shield: Effective block angle is 225 degrees instead of 180.</item>
    /// <item>Spec Bonus - Phys Def: Up to +50% increased block chance.</item>
    /// <item>Gear Block Rating: +X% increased block chance, equal to 10% + 0.5% per rating.</item>
    /// <item>Phalanx Ability: Effective block angle is 360 degrees and block chance is increased by 25-50%, based on shield size.</item>
    /// </list>
    /// </summary>
    private void SetBlocked(Creature attacker, Creature defender)
    {
        Blocked = false;

        var playerDefender = defender as Player;

        if (defender.CombatMode is CombatMode.NonCombat)
        {
            return;
        }

        var equippedShield = defender.GetEquippedShield();
        if (equippedShield is null)
        {
            return;
        }

        var effectiveAngle = 180.0f;
        effectiveAngle += GetSpecShieldEffectiveAngleBonus(playerDefender);

        var blockableAngle = Math.Abs(defender.GetAngle(attacker)) < effectiveAngle / 2.0f || playerDefender is { PhalanxIsEffective: true };

        if (!blockableAngle)
        {
            return;
        }

        // base block/parry chance is 5%
        // Blocks (shields) can have up to +5% additional base chance, depending on Shield Level vs Attacker Skill
        // Parry base chance is always 5%
        const float minBlockChance = 0.05f;

        var blockChanceShieldBonus = GetBlockChanceShieldLevelBonus(defender, equippedShield.ArmorLevel ?? 1);
        var baseBlockChance = minBlockChance * blockChanceShieldBonus;

        // other bonuses are additive then multiplied against base block chance
        // Spec Phys Def = up to 50%, Jewels = 10% + ratings, Riposte = 100%
        var specPhysicalDefenseBlockChanceBonus = GetSpecPhysicalDefenseBlockChanceBonus();
        var jewelBlockChanceBonus = Jewel.GetJewelEffectMod(playerDefender, PropertyInt.GearBlock);
        var riposteBlockChanceBonus = 0.0f;
        if (playerDefender is { RiposteIsActive: true })
        {
            riposteBlockChanceBonus = 1.0f;
        }

        var blockChance = baseBlockChance * (1.0f + specPhysicalDefenseBlockChanceBonus + jewelBlockChanceBonus + riposteBlockChanceBonus);

        // COMBAT ABILITY - Phalanx: block chance increased by 25-50%, based on shield size
        blockChance *= playerDefender?.GetPhalanxBlockParryMod() ?? 1.0f;

        if ((ThreadSafeRandom.Next(0f, 1f) > blockChance))
        {
            return;
        }

        Blocked = true;
    }

    /// <summary>
    /// </summary>
    private void SetParry(Creature attacker, Creature defender)
    {
        Parried = false;

        var playerDefender = defender as Player;

        if (defender.CombatMode is CombatMode.NonCombat)
        {
            return;
        }

        var equippedMainHand = defender.GetEquippedWeapon();
        var equippedOffHand = defender.GetEquippedOffHand();

        // parrying requires a two-handed weapon, or a weapon in each hand
        if (equippedMainHand is null || (!equippedMainHand.IsTwoHanded && equippedOffHand is not { ItemType: ItemType.MeleeWeapon }))
        {
            return;
        }

        const float effectiveAngle = 180.0f;
        var parryAngle = Math.Abs(defender.GetAngle(attacker)) < effectiveAngle / 2.0f || playerDefender is { PhalanxIsEffective: true };

        if (!parryAngle)
        {
            return;
        }

        var parrySkillUsed = equippedMainHand is { IsTwoHanded: true }
            ? defender.GetModdedTwohandedCombatSkill()
            : defender.GetModdedDualWieldSkill();

        var parryMod = SkillCheck.GetSkillChance((uint)(parrySkillUsed * 1.5), EffectiveAttackSkill);

        // parry chance is up to 10%, based on Two-hand or Dual-wield skill levels vs Attack Skill
        var maxBaseParryChance = 0.1f * parryMod;

        // other bonuses are additive then multiplied against base parry chance
        // Spec Phys Def = up to 50%, Riposte = 100%
        var specPhysicalDefenseParryChanceBonus = GetSpecPhysicalDefenseBlockChanceBonus();
        var riposteActivatedBonus = playerDefender is { RiposteIsActive: true } ? 1.0f : 0.0f;
        var parryChance = maxBaseParryChance * (1.0 + specPhysicalDefenseParryChanceBonus + riposteActivatedBonus);

        // COMBAT ABILITY - Phalanx: parry chance increased by 25% with two-handed weapons
        parryChance *= playerDefender?.GetPhalanxBlockParryMod() ?? 1.0f;

        if ((ThreadSafeRandom.Next(0f, 1f) > parryChance))
        {
            return;
        }

        Parried = true;
    }

    private double GetBlockChanceShieldLevelBonus(Creature defender, int shieldLevel)
    {
        var effectiveShieldLevel = (uint)defender.GetSkillModifiedShieldLevel(shieldLevel);

        return 1.0 + SkillCheck.GetSkillChance(effectiveShieldLevel, EffectiveAttackSkill);
    }

    /// <summary>
    /// Sets EffectiveAttackSkill and the effective defense skill, which the evade, block and parry chances all use
    /// </summary>
    private void SetAttackAndDefenseSkills(Creature attacker, Creature defender)
    {
        var playerAttacker = attacker as Player;
        var playerDefender = defender as Player;

        _accuracyMod = attacker.GetAccuracySkillMod(Weapon);

        EffectiveAttackSkill = (uint)(attacker.GetEffectiveAttackSkill() * LevelScaling.GetPlayerAttackSkillScalar(playerAttacker, defender));

        EffectiveAttackSkill = Convert.ToUInt32(EffectiveAttackSkill * CheckForAttackHeightMediumAttackSkillBonus(playerAttacker));
        EffectiveAttackSkill = Convert.ToUInt32(EffectiveAttackSkill * CheckForCombatAbilitySteadyStrikeAttackSkillBonus(playerAttacker));
        EffectiveAttackSkill = Convert.ToUInt32(EffectiveAttackSkill * (1.0f + Jewel.GetJewelEffectMod(playerAttacker, PropertyInt.GearBravado, "Bravado", rampQuestSource: defender)));

        _effectiveDefenseSkill = defender.GetEffectiveDefenseSkill(CombatType);

        _effectiveDefenseSkill = Convert.ToUInt32(_effectiveDefenseSkill * CheckForAttackHeightLowDefenseSkillBonus(playerDefender, playerAttacker));
        _effectiveDefenseSkill = Convert.ToUInt32(_effectiveDefenseSkill * (1.0f + Jewel.GetJewelEffectMod(playerDefender, PropertyInt.GearFamiliarity, "Familiarity", rampQuestSource: attacker)));

        // level scaling goes last, so the bonuses above are worth the same at every level (see GetScaledPlayerDefenseSkill)
        _effectiveDefenseSkill = LevelScaling.GetScaledPlayerDefenseSkill(_effectiveDefenseSkill, playerDefender, attacker);
    }

    /// <summary>
    /// Returns the chance for the defender to evade the attack
    /// </summary>
    private float GetEvadeChance()
    {
        var evadeChance = SkillCheck.GetSkillChance(_effectiveDefenseSkill, EffectiveAttackSkill);
        evadeChance = CheckForCombatAbilitySmokescreenEvadeChanceBonus(evadeChance, _playerDefender);

        if (evadeChance < 0)
        {
            evadeChance = 0;
        }

        return (float)Math.Min(evadeChance, 1.0f);
    }

    /// <summary>
    /// COMBAT Ability - Smokescreen: 10% increased chance to evade attacks
    /// </summary>
    private static double CheckForCombatAbilitySmokescreenEvadeChanceBonus(double evadeChance, Player playerDefender)
    {
        if (playerDefender is not { SmokescreenIsActive: true })
        {
            return evadeChance;
        }

        var remainingChance = 1.0f - evadeChance;
        var bonus = remainingChance * 0.1f;

        return evadeChance + bonus;

    }

    /// <summary>
    /// COMBAT ABILITY - Steady Strike: Increased attack skill with melee/missile attacks by 25%.
    /// </summary>
    private static float CheckForCombatAbilitySteadyStrikeAttackSkillBonus(Player playerAttacker)
    {
        if (playerAttacker?.GetEquippedWeapon() is null)
        {
            return 1.0f;
        }

        if (playerAttacker.GetPowerAccuracyBar() < 0.5f)
        {
            return 1.0f;
        }

        return playerAttacker is {SteadyStrikeIsActive: true} ? 1.25f : 1.0f;
    }

    /// <summary>
    /// ATTACK HEIGHT BONUS: Low (+10% physical defense skill, +20% if weapon specialized)
    /// </summary>
    /// <returns></returns>
    private float CheckForAttackHeightLowDefenseSkillBonus(Player playerDefender, Player playerAttacker)
    {
        if (playerDefender is { AttackHeight: AttackHeight.Low })
        {
            return WeaponIsSpecialized(playerAttacker) ? 1.2f : 1.1f;
        }

        return 1.0f;
    }

    /// <summary>
    /// ATTACK HEIGHT BONUS: Medium (+10% attack skill, +20% if weapon specialized)
    /// </summary>
    private float CheckForAttackHeightMediumAttackSkillBonus(Player playerAttacker)
    {
        if (playerAttacker is { AttackHeight: AttackHeight.Medium })
        {
            return WeaponIsSpecialized(playerAttacker) ? 1.2f : 1.1f;
        }

        return 1.0f;
    }

    /// <summary>
    /// SPEC BONUS - Shield: Increase shield effective angle by 45 degrees (to 225)
    /// </summary>
    private static float GetSpecShieldEffectiveAngleBonus(Player playerDefender)
    {
        if (playerDefender == null)
        {
            return 0.0f;
        }

        return IsSkillSpecialized(playerDefender, Skill.Shield) ? 45.0f : 0.0f;
    }

    /// <summary>
    /// SPEC BONUS - Physical Defense: Increase block/parry chance up to 50% (multiplicatively).
    /// Based on defender 'defense skill' and attacker 'attack skill'.
    /// Players must have Physical Defense specialized. Monsters always receive this bonus.
    /// </summary>
    private float GetSpecPhysicalDefenseBlockChanceBonus()
    {
        if (_playerDefender is not null && !IsSkillSpecialized(_playerDefender, Skill.PhysicalDefense))
        {
            return 0.0f;
        }

        var blockChanceMod = SkillCheck.GetSkillChance(_effectiveDefenseSkill, EffectiveAttackSkill);

        return 0.5f * (float)blockChanceMod;
    }
}
