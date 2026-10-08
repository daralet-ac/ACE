using System.Linq;
using ACE.Entity.Enum;
using ACE.Server.Managers;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.WorldObjects;

namespace ACE.Server.Entity;

public partial class DamageEvent
{
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

        if (_playerAttacker == null)
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
        info += $"AttributeMod: {_damageModifiers.Attribute}\n";

        if (_damageModifiers.Power != 0.0f && _damageModifiers.Power != 1.0f)
        {
            info += $"PowerMod: {_damageModifiers.Power}\n";
        }

        if (_damageModifiers.Slayer != 0.0f && _damageModifiers.Slayer != 1.0f)
        {
            info += $"SlayerMod: {_damageModifiers.Slayer}\n";
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
        if (_damageModifiers.Recklessness != 0.0f && _damageModifiers.Recklessness != 1.0f)
        {
            info += $"RecklessnessMod: {_damageModifiers.Recklessness}\n";
        }

        if (SneakAttackMod != 0.0f && SneakAttackMod != 1.0f)
        {
            info += $"SneakAttackMod: {SneakAttackMod}\n";
        }

        if (_pkDamageMod != 0.0f && _pkDamageMod != 1.0f)
        {
            info += $"PkDamageMod: {_pkDamageMod}\n";
        }

        if (_damageModifiers.DamageRating != 0.0f && _damageModifiers.DamageRating != 1.0f)
        {
            info += $"DamageRatingMod: {_damageModifiers.DamageRating}\n";
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
        if (_mitigationModifiers.Armor != 0.0f && _mitigationModifiers.Armor != 1.0f)
        {
            info += $"ArmorMod: {_mitigationModifiers.Armor}\n";
        }

        if (_mitigationModifiers.Resistance != 0.0f && _mitigationModifiers.Resistance != 1.0f)
        {
            info += $"ResistanceMod: {_mitigationModifiers.Resistance}\n";
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

        if (_mitigationModifiers.DamageResistanceRating != 0.0f && _mitigationModifiers.DamageResistanceRating != 1.0f)
        {
            info += $"DamageResistanceRatingMod: {_mitigationModifiers.DamageResistanceRating}\n";
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
