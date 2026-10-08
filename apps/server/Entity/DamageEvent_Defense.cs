using System;
using ACE.Common;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.WorldObjects;

namespace ACE.Server.Entity;

public partial class DamageEvent
{
    private void SetInvulnerable()
    {
        _invulnerable = false;

        if (_playerDefender is { UnderLifestoneProtection: true })
        {
            LifestoneProtection = true;
            _playerDefender.HandleLifestoneProtection();
            _invulnerable = true;
        }

        if (_defender.Invincible)
        {
            _invulnerable = true;
        }
    }

    /// <summary>
    /// Checks for Overpower, Steady Strike, Fury, and Backstab auto-hits.
    /// If evade succeeded, determine if evade was full, partial, or none.
    /// Equal chance for each evasion type to occur.
    /// </summary>
    private void SetEvaded()
    {
        Evaded = false;
        _evasionMod = 1.0f;
        PartialEvasion = PartialEvasion.None;

        if (_defender.CombatMode is CombatMode.NonCombat)
        {
            return;
        }

        // Check for guaranteed hits
        var isOverpower = CheckForOverpower();
        var isFuryNoEvade = CheckForCombatAbilityEnrageNoEvade();
        var isBackstabNoEvade = CheckForCombatAbilityBackstabStealthNoEvade();

        if (isOverpower || isFuryNoEvade || isBackstabNoEvade || _attacker == _defender)
        {
            return;
        }

        // COMBAT ABILITY - Aegis: attacks can't be evaded, fully or partially.
        if (_playerDefender is { AegisIsActive: true })
        {
            return;
        }

        // COMBAT ABILITY - Evasive Stance: flat 25% chance to fully evade any attack, independent of defense skill.
        if (_playerDefender is { EvasiveStanceIsActive: true } && ThreadSafeRandom.Next(0.0f, 1.0f) < 0.25f)
        {
            Evaded = true;
            PartialEvasion = PartialEvasion.All;
            return;
        }

        // Roll combat hit chance
        var smokescreen = _playerDefender is { SmokescreenIsActive: true };
        var evadeChance = DamageFormulas.GetEvadeChance(_effectiveDefenseSkill, EffectiveAttackSkill, smokescreen);

        var attackRoll = ThreadSafeRandom.Next(0.0f, 1.0f);
        if (attackRoll > evadeChance)
        {
            return;
        }

        // Roll evade type (33% for each evade type)
        var partialEvadeRoll = ThreadSafeRandom.Next(0.0f, 1.0f);

        PartialEvasion = DamageFormulas.GetEvasionType(partialEvadeRoll);
        Evaded = PartialEvasion == PartialEvasion.All;
        _evasionMod = PartialEvasion == PartialEvasion.Some ? DamageFormulas.GlancingBlowMod : 1.0f;

        if (_playerDefender is not null && PartialEvasion == PartialEvasion.Some)
        {
            _playerDefender.CheckForSigilTrinketOnAttackEffects(
                _playerAttacker,
                this,
                Skill.PhysicalDefense,
                SigilTrinketPhysicalDefenseEffect.Evasion
            );
        }
    }

    private bool CheckForOverpower()
    {
        if (_attacker.Overpower == null)
        {
            return false;
        }

        _overpower = Creature.GetOverpower(_attacker, _defender);
        return _overpower;
    }

    private bool CheckForCombatAbilityEnrageNoEvade()
    {
        if (_playerAttacker is not { FuryEnrageIsActive: true })
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
    private bool CheckForCombatAbilityBackstabStealthNoEvade()
    {
        if (
            _playerAttacker is { BackstabIsActive: true, IsAttackFromStealth: true }
            && _playerAttacker.IsBehindTargetCreature(_defender)
            && _defender is not Player { PhalanxIsEffective: true }
        )
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
    private void SetBlocked()
    {
        Blocked = false;

        if (_defender.CombatMode is CombatMode.NonCombat)
        {
            return;
        }

        var equippedShield = _defender.GetEquippedShield();
        if (equippedShield is null)
        {
            return;
        }

        var effectiveAngle = 180.0f;
        effectiveAngle += GetSpecShieldEffectiveAngleBonus(_playerDefender);

        var blockableAngle =
            Math.Abs(_defender.GetAngle(_attacker)) < effectiveAngle / 2.0f
            || _playerDefender is { PhalanxIsEffective: true };

        if (!blockableAngle)
        {
            return;
        }

        var blockChance = DamageFormulas.GetBlockChance(
            (uint)_defender.GetSkillModifiedShieldLevel(equippedShield.ArmorLevel ?? 1),
            EffectiveAttackSkill,
            GetSpecPhysicalDefenseBlockChanceBonus(),
            Jewel.GetJewelEffectMod(_playerDefender, PropertyInt.GearBlock), // 10% + ratings
            _playerDefender is { RiposteIsActive: true } ? 1.0f : 0.0f,
            _playerDefender?.GetPhalanxBlockParryMod() ?? 1.0f // COMBAT ABILITY - Phalanx: 25-50%, based on shield size
        );

        if ((ThreadSafeRandom.Next(0f, 1f) > blockChance))
        {
            return;
        }

        Blocked = true;
    }

    /// <summary>
    /// </summary>
    private void SetParry()
    {
        Parried = false;

        if (_defender.CombatMode is CombatMode.NonCombat)
        {
            return;
        }

        var equippedMainHand = _defender.GetEquippedWeapon();
        var equippedOffHand = _defender.GetEquippedOffHand();

        // parrying requires a two-handed weapon, or a weapon in each hand
        if (
            equippedMainHand is null
            || (!equippedMainHand.IsTwoHanded && equippedOffHand is not { ItemType: ItemType.MeleeWeapon })
        )
        {
            return;
        }

        const float effectiveAngle = 180.0f;
        var parryAngle =
            Math.Abs(_defender.GetAngle(_attacker)) < effectiveAngle / 2.0f
            || _playerDefender is { PhalanxIsEffective: true };

        if (!parryAngle)
        {
            return;
        }

        var parrySkillUsed = equippedMainHand is { IsTwoHanded: true }
            ? _defender.GetModdedTwohandedCombatSkill()
            : _defender.GetModdedDualWieldSkill();

        var parryChance = DamageFormulas.GetParryChance(
            parrySkillUsed,
            EffectiveAttackSkill,
            GetSpecPhysicalDefenseBlockChanceBonus(),
            _playerDefender is { RiposteIsActive: true } ? 1.0f : 0.0f,
            _playerDefender?.GetPhalanxBlockParryMod() ?? 1.0f // COMBAT ABILITY - Phalanx
        );

        if ((ThreadSafeRandom.Next(0f, 1f) > parryChance))
        {
            return;
        }

        Parried = true;
    }

    /// <summary>
    /// Sets EffectiveAttackSkill and the effective defense skill, which the evade, block and parry chances all use
    /// </summary>
    private void SetAttackAndDefenseSkills()
    {
        _accuracyMod = _attacker.GetAccuracySkillMod(Weapon);

        EffectiveAttackSkill = (uint)(
            _attacker.GetEffectiveAttackSkill() * LevelScaling.GetPlayerAttackSkillScalar(_playerAttacker, _defender)
        );

        EffectiveAttackSkill = Convert.ToUInt32(EffectiveAttackSkill * CheckForAttackHeightMediumAttackSkillBonus());
        EffectiveAttackSkill = Convert.ToUInt32(
            EffectiveAttackSkill * CheckForCombatAbilitySteadyStrikeAttackSkillBonus(_playerAttacker)
        );
        EffectiveAttackSkill = Convert.ToUInt32(
            EffectiveAttackSkill
                * (
                    1.0f
                    + Jewel.GetJewelEffectMod(
                        _playerAttacker,
                        PropertyInt.GearBravado,
                        "Bravado",
                        rampQuestSource: _defender
                    )
                )
        );

        _effectiveDefenseSkill = _defender.GetEffectiveDefenseSkill(CombatType);

        _effectiveDefenseSkill = Convert.ToUInt32(_effectiveDefenseSkill * CheckForAttackHeightLowDefenseSkillBonus());
        _effectiveDefenseSkill = Convert.ToUInt32(
            _effectiveDefenseSkill
                * (
                    1.0f
                    + Jewel.GetJewelEffectMod(
                        _playerDefender,
                        PropertyInt.GearFamiliarity,
                        "Familiarity",
                        rampQuestSource: _attacker
                    )
                )
        );

        // level scaling goes last, so the bonuses above are worth the same at every level (see GetScaledPlayerDefenseSkill)
        _effectiveDefenseSkill = LevelScaling.GetScaledPlayerDefenseSkill(
            _effectiveDefenseSkill,
            _playerDefender,
            _attacker
        );
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

        return playerAttacker is { SteadyStrikeIsActive: true } ? 1.25f : 1.0f;
    }

    /// <summary>
    /// ATTACK HEIGHT BONUS: Low (+10% physical defense skill, +20% if the defender's weapon is specialized)
    /// </summary>
    /// <returns></returns>
    private float CheckForAttackHeightLowDefenseSkillBonus()
    {
        if (_playerDefender is { AttackHeight: AttackHeight.Low })
        {
            return IsWeaponSpecialized(_playerDefender, DefenderWeapon) ? 1.2f : 1.1f;
        }

        return 1.0f;
    }

    /// <summary>
    /// ATTACK HEIGHT BONUS: Medium (+10% attack skill, +20% if weapon specialized)
    /// </summary>
    private float CheckForAttackHeightMediumAttackSkillBonus()
    {
        if (_playerAttacker is { AttackHeight: AttackHeight.Medium })
        {
            return IsWeaponSpecialized(_playerAttacker, Weapon) ? 1.2f : 1.1f;
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
