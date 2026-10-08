using System;
using System.Collections.Generic;
using System.Linq;
using ACE.Common;
using ACE.DatLoader.Entity.AnimationHooks;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Entity.Models;
using ACE.Server.Factories.Tables;
using ACE.Server.Managers;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.WorldObjects;
using ACE.Server.WorldObjects.Entity;
using Serilog;
using Time = ACE.Common.Time;

namespace ACE.Server.Entity;

public partial class DamageEvent
{
    private void DpsLogging()
    {
        if (_attacker == null || _defender == null)
        {
            return;
        }

        // if (_attacker.Name is not "")
        // {
        //     return;
        // }

        var currentTime = Time.GetUnixTime();
        var timeSinceLastAttack = currentTime - _attacker.LastAttackTime;
        if (_attacker as Player == null)
        {
            timeSinceLastAttack = MonsterAverageAnimationLength.GetValueMod(_attacker.CreatureType);
        }

        var damageSource = Weapon == null ? _attacker : Weapon;

        _log.Information("---- DAMAGE LOG ({DamageSource}) ----", damageSource.Name);
        _log.Information(
            "CurrentTime: {CurrentTime}, LastAttackTime: {LastAttackTime} TimeBetweenAttacks: {TimeBetweenAttacks}",
            currentTime,
            _attacker.LastAttackTime,
            timeSinceLastAttack
        );
        _attacker.LastAttackTime = currentTime;

        var critRate = _criticalChance;
        var nonCritRate = 1 - critRate;
        var critDamageMod = 1.0f + WorldObject.GetWeaponCritDamageMod(damageSource, _attacker, _attackSkill, _defender);

        var avgNonCritHit = (_baseDamageMod.MaxDamage + _baseDamageMod.MinDamage) / 2;
        var critHit = _baseDamageMod.MaxDamage * critDamageMod;

        var averageDamage = avgNonCritHit * nonCritRate + critHit * critRate;
        var baseDps = averageDamage / timeSinceLastAttack;

        var averageDamageBeforeMitigation =
            averageDamage
            * _powerMod
            * _attributeMod
            * _slayerMod
            * _damageRatingMod
            * _dualWieldDamageBonus
            * _twohandedCombatDamageBonus
            * _combatAbilitySteadyStrikeDamageBonus;
        var averageDpsBeforeMitigation = averageDamageBeforeMitigation / timeSinceLastAttack;

        var averageDamageAfterMitigation =
            averageDamageBeforeMitigation
            * _armorMod
            * ShieldMod
            * _resistanceMod
            * _damageResistanceRatingMod
            * _levelScalingMod;
        var averageDpsAfterMitigation = averageDamageAfterMitigation / timeSinceLastAttack;

        _log.Information(
            "{DamageLog}",
            $"TimeSinceLastAttack: {timeSinceLastAttack}"
            + $"\n\n-- Base --\n"
            + $"BaseDamageMod.MaxDamage: {_baseDamageMod.MaxDamage}, BaseDamageMod.MinDamage: {_baseDamageMod.MinDamage}, LiveBaseDamage: {_baseDamage}\n"
            + $"AverageDamageNonCrit: {avgNonCritHit}, AverageDamageCrit: {critHit}, AverageDamageHit: {averageDamage}\n"
            + $"DPS Base: {baseDps}\n\n"
            + $"-- Before Mitigation --\n"
            + $"PowerMod: {_powerMod}, AttributeMod: {_attributeMod}, SlayerMod: {_slayerMod}, DamageRatingMod: {_damageRatingMod}, DualWieldMod: {_dualWieldDamageBonus}, TwoHandMod: {_twohandedCombatDamageBonus}, SteadyStrikeMod: {_combatAbilitySteadyStrikeDamageBonus}\n"
            + $"AverageDamage Before Mitigation: {averageDamageBeforeMitigation}\n"
            + $"DPS Before Mitigation: {averageDpsBeforeMitigation}\n\n"
            + $"-- After Mitigation --\n"
            + $"DamageScalar(health): {_levelScalingMod}, ArmorMod: {_armorMod}, ShieldMod: {ShieldMod}, ResistanceMod: {_resistanceMod}, DamageResistanceRatingMod: {_damageResistanceRatingMod}\n"
            + $"AverageDamage After Mitigation: {averageDamageAfterMitigation}\n"
            + $"DPS After Mitigation: {averageDpsAfterMitigation}\n"
            + $"---- END DAMAGE LOG ({damageSource.Name}) ----"
        );
    }

    private void ShowInfo(Creature creature)
    {
        var targetInfo = PlayerManager.GetOnlinePlayer(creature.DebugDamageTarget);
        if (targetInfo == null)
        {
            creature.DebugDamage = Creature.DebugDamageType.None;
            return;
        }

        // setup
        var info = $"Attacker: {_attacker.Name} ({_attacker.Guid})\n";
        info += $"Defender: {_defender.Name} ({_defender.Guid})\n";

        info += $"CombatType: {CombatType}\n";

        info += $"DamageSource: {_damageSource.Name} ({_damageSource.Guid})\n";
        info += $"DamageType: {DamageType}\n";

        var weaponName = Weapon != null ? $"{Weapon.Name} ({Weapon.Guid})" : "None\n";
        info += $"Weapon: {weaponName}\n";

        info += $"AttackType: {_attackType}\n";
        info += $"AttackHeight: {_attackHeight}\n";

        // lifestone protection
        if (LifestoneProtection)
        {
            info += $"LifestoneProtection: {LifestoneProtection}\n";
        }

        // evade
        if (_accuracyMod != 0.0f && _accuracyMod != 1.0f)
        {
            info += $"AccuracyMod: {_accuracyMod}\n";
        }

        info += $"EffectiveAttackSkill: {EffectiveAttackSkill}\n";
        info += $"EffectiveDefenseSkill: {_effectiveDefenseSkill}\n";

        if (_attacker.Overpower != null)
        {
            info += $"Overpower: {_overpower} ({Creature.GetOverpowerChance(_attacker, _defender)})\n";
        }

        info += $"Evaded: {Evaded}\n";
        info += $"Blocked: {Blocked}\n";
        info += $"PartialEvaded: {PartialEvasion}\n";

        if (!(_attacker is Player))
        {
            if (_attackMotion != null)
            {
                info += $"AttackMotion: {_attackMotion}\n";
            }

            if (_attackPart.Value != null)
            {
                info += $"AttackPart: {_attackPart.Key}\n";
            }
        }

        // base damage
        if (_baseDamageMod != null)
        {
            info += $"BaseDamageRange: {_baseDamageMod.Range}\n";
        }

        info += $"BaseDamage: {_baseDamage}\n";

        // damage modifiers
        info += $"AttributeMod: {_attributeMod}\n";

        if (_powerMod != 0.0f && _powerMod != 1.0f)
        {
            info += $"PowerMod: {_powerMod}\n";
        }

        if (_slayerMod != 0.0f && _slayerMod != 1.0f)
        {
            info += $"SlayerMod: {_slayerMod}\n";
        }

        if (_baseDamageMod != null)
        {
            if (_baseDamageMod.DamageBonus != 0)
            {
                info += $"DamageBonus: {_baseDamageMod.DamageBonus}\n";
            }

            if (_baseDamageMod.DamageMod != 0.0f && _baseDamageMod.DamageMod != 1.0f)
            {
                info += $"DamageMod: {_baseDamageMod.DamageMod}\n";
            }

            if (_baseDamageMod.ElementalBonus != 0)
            {
                info += $"ElementalDamageBonus: {_baseDamageMod.ElementalBonus}\n";
            }
        }

        // critical hit
        info += $"CriticalChance: {_criticalChance}\n";
        info += $"CriticalHit: {IsCritical}\n";

        if (_criticalDefendedFromAug)
        {
            info += $"CriticalDefended: {_criticalDefendedFromAug}\n";
        }

        if (_criticalDamageMod != 0.0f && _criticalDamageMod != 1.0f)
        {
            info += $"CriticalDamageMod: {_criticalDamageMod}\n";
        }

        if (_criticalDamageRating != 0.0f && _criticalDamageRating != 1.0f)
        {
            info += $"CriticalDamageRatingMod: {_criticalDamageRating}\n";
        }

        // damage ratings
        if (_recklessnessMod != 0.0f && _recklessnessMod != 1.0f)
        {
            info += $"RecklessnessMod: {_recklessnessMod}\n";
        }

        if (SneakAttackMod != 0.0f && SneakAttackMod != 1.0f)
        {
            info += $"SneakAttackMod: {SneakAttackMod}\n";
        }

        if (_pkDamageMod != 0.0f && _pkDamageMod != 1.0f)
        {
            info += $"PkDamageMod: {_pkDamageMod}\n";
        }

        if (_damageRatingMod != 0.0f && _damageRatingMod != 1.0f)
        {
            info += $"DamageRatingMod: {_damageRatingMod}\n";
        }

        if (BodyPart != 0)
        {
            // player body part
            info += $"BodyPart: {BodyPart}\n";
        }

        if (_armor != null && _armor.Count > 0)
        {
            info += $"Armors: {string.Join(", ", _armor.Select(i => i.Name))}\n";
        }

        if (_creaturePart != null)
        {
            // creature body part
            info += $"BodyPart: {_propertiesBodyPart.Key}\n";
            info += $"BaseArmor: {_creaturePart.Biota.Value.BaseArmor}\n";
        }

        // damage mitigation
        if (_armorMod != 0.0f && _armorMod != 1.0f)
        {
            info += $"ArmorMod: {_armorMod}\n";
        }

        if (_resistanceMod != 0.0f && _resistanceMod != 1.0f)
        {
            info += $"ResistanceMod: {_resistanceMod}\n";
        }

        if (ShieldMod != 0.0f && ShieldMod != 1.0f)
        {
            info += $"ShieldMod: {ShieldMod}\n";
        }

        if (_weaponResistanceMod != 0.0f && _weaponResistanceMod != 1.0f)
        {
            info += $"WeaponResistanceMod: {_weaponResistanceMod}\n";
        }

        if (_damageResistanceRatingBaseMod != 0.0f && _damageResistanceRatingBaseMod != 1.0f)
        {
            info += $"DamageResistanceRatingBaseMod: {_damageResistanceRatingBaseMod}\n";
        }

        if (_criticalDamageResistanceRatingMod != 0.0f && _criticalDamageResistanceRatingMod != 1.0f)
        {
            info += $"CriticalDamageResistanceRatingMod: {_criticalDamageResistanceRatingMod}\n";
        }

        if (_pkDamageResistanceMod != 0.0f && _pkDamageResistanceMod != 1.0f)
        {
            info += $"PkDamageResistanceMod: {_pkDamageResistanceMod}\n";
        }

        if (_damageResistanceRatingMod != 0.0f && _damageResistanceRatingMod != 1.0f)
        {
            info += $"DamageResistanceRatingMod: {_damageResistanceRatingMod}\n";
        }

        if (IgnoreMagicArmor)
        {
            info += $"IgnoreMagicArmor: {IgnoreMagicArmor}\n";
        }

        if (IgnoreMagicResist)
        {
            info += $"IgnoreMagicResist: {IgnoreMagicResist}\n";
        }

        // final damage
        info += $"DamageBeforeMitigation: {_damageBeforeMitigation}\n";
        info += $"DamageMitigated: {_damageMitigated}\n";
        info += $"Damage: {Damage}\n";

        info += "----";

        targetInfo.Session.Network.EnqueueSend(new GameMessageSystemChat(info, ChatMessageType.Broadcast));
    }

    private void HandleLogging(Creature attacker, Creature defender)
    {
        if (attacker != null && (attacker.DebugDamage & Creature.DebugDamageType.Attacker) != 0)
        {
            ShowInfo(attacker);
        }

        if (defender != null && (defender.DebugDamage & Creature.DebugDamageType.Defender) != 0)
        {
            ShowInfo(defender);
        }
    }
}
