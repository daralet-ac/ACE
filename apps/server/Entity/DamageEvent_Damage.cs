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
    private float GetDamageBeforeMitigation(Creature attacker, Creature defender, WorldObject damageSource)
    {
        SetBaseDamage(attacker, defender, damageSource);
        SetDamageModifiers(attacker, defender);

        _criticalChance = GetCriticalChance(attacker, defender);

        var roll = ThreadSafeRandom.Next(0.0f, 1.0f);
        var criticalRolled = roll <= _criticalChance;
        var criticalDefendedFromPerception = false;

        if (criticalRolled && !GetCriticalDefendedFromAug(attacker, defender))
        {
            criticalDefendedFromPerception = CheckForSpecPerceptionCriticalDefense(_playerDefender);
        }

        if (!criticalRolled || _criticalDefendedFromAug || criticalDefendedFromPerception)
        {
            _playerAttacker?.CheckForSigilTrinketOnAttackEffects(defender, this, Skill.TwoHandedCombat, SigilTrinketShieldTwohandedCombatEffect.Might);
            _playerAttacker?.CheckForSigilTrinketOnAttackEffects(defender, this, Skill.Shield, SigilTrinketShieldTwohandedCombatEffect.Might);

            if (!CriticalOverridedByTrinket)
            {
                if (criticalDefendedFromPerception)
                {
                    _playerDefender.Session.Network.EnqueueSend(
                        new GameMessageSystemChat(
                            "Your perception skill allowed you to prevent a critical strike!",
                            ChatMessageType.Broadcast
                        )
                    );
                }

                return GetNonCriticalDamageBeforeMitigation();
            }
        }

        IsCritical = true;
        return GetCriticalDamageBeforeMitigation(attacker, defender);
    }

    private void SetBaseDamage(Creature attacker, Creature defender, WorldObject damageSource)
    {
        if (attacker is Player playerAttacker)
        {
            GetBaseDamage(playerAttacker);
        }
        else
        {
            GetBaseDamage(attacker, _attackMotion ?? MotionCommand.Invalid, _attackHook);
        }

        if (DamageType == DamageType.Undef)
        {
            if ((attacker?.Guid.IsPlayer() ?? false) || (damageSource?.Guid.IsPlayer() ?? false))
            {
                _log.Error(
                    $"DamageEvent.DoCalculateDamage({attacker?.Name} ({attacker?.Guid}), {defender?.Name} ({defender?.Guid}), {damageSource?.Name} ({damageSource?.Guid})) - DamageType == DamageType.Undef"
                );
                _generalFailure = true;
            }
        }
    }

    private void SetDamageModifiers(Creature attacker, Creature defender, float? powerMod = null, bool consumeSneakAttackBonuses = true)
    {
        var playerAttacker = attacker as Player;
        var playerDefender = defender as Player;

        _powerMod = powerMod ?? attacker.GetPowerMod(Weapon);
        _attributeMod = attacker.GetAttributeMod(Weapon, false);
        _slayerMod = WorldObject.GetWeaponCreatureSlayerModifier(Weapon, attacker, defender);
        _damageRatingMod = Creature.GetPositiveRatingMod(attacker.GetDamageRating());
        _dualWieldDamageBonus = GetDualWieldDamageBonus(playerAttacker, defender);
        _twohandedCombatDamageBonus = GetTwohandedCombatDamageBonus(playerAttacker, defender);
        _combatAbilityMultishotDamagePenalty = GetCombatAbilityMultishotDamagePenalty(playerAttacker);
        _combatAbilityFuryDamageBonus = GetCombatAbilityFuryDamageBonus(playerAttacker, playerDefender);
        _combatAbilityRelentlessDamagePenalty = GetCombatAbilityRelentlessDamagePenalty(playerAttacker);
        _combatAbilitySteadyStrikeDamageBonus = GetCombatAbilitySteadyStrikeDamageBonus(playerAttacker);
        _recklessnessMod = Creature.GetRecklessnessMod(attacker, defender);

        // Sneak attack / Backstab bonuses (and their one-shot charges) should only be
        // consumed by an attacker's own normal attack - not by ancillary reactive damage
        // calculations like Thorns reflection or a Riposte counter-hit.
        if (consumeSneakAttackBonuses)
        {
            SneakAttackMod = attacker.GetSneakAttackMod(defender);
            _backstabDamageMultiplier = Creature.GetStealthBackstabDamageMultiplier(playerAttacker, defender);
        }
        else
        {
            SneakAttackMod = 1.0f;
            _backstabDamageMultiplier = 1.0f;
        }

        _attackHeightDamageBonus = GetHighAttackHeightBonus(playerAttacker);
        _ratingElementalDamageBonus = Jewel.HandleElementalBonuses(playerAttacker, DamageType);
        _ratingPierceResistanceBonus = GetRatingPierceResistanceBonus(defender, playerAttacker);
        _levelScalingMod = GetLevelScalingMod(attacker, defender, playerDefender);
        _ammoEffectMod = GetAmmoEffectMod(Weapon, playerAttacker);

        if (!_pkBattle)
        {
            return;
        }

        _pkDamageMod = Creature.GetPositiveRatingMod(attacker.GetPKDamageRating());
        _damageRatingMod = Creature.AdditiveCombine(_damageRatingMod, _pkDamageMod);
    }

    /// <summary>
    /// Dual Wield Damage Mod
    /// </summary>
    private static float GetDualWieldDamageBonus(Player playerAttacker, Creature defender)
    {
        if (playerAttacker is not {IsDualWieldAttack: true} || defender is null)
        {
            return 1.0f;
        }

        var moddedDualWieldCombatSkill = (uint)(playerAttacker.GetModdedDualWieldSkill() * 1.5f);
        var defenderPhysicalDefense = defender.GetModdedPhysicalDefSkill();

        var damageMod = 0.5f * SkillCheck.GetSkillChance(moddedDualWieldCombatSkill, defenderPhysicalDefense);

        var finalDamageMod = 1.0f + (float)damageMod;

        return finalDamageMod;
    }

    /// <summary>
    /// Two-handed Combat Damage Mod
    /// </summary>
    private static float GetTwohandedCombatDamageBonus(Player playerAttacker, Creature defender)
    {
        if (playerAttacker?.GetEquippedWeapon() is null
            || playerAttacker.GetEquippedWeapon().W_WeaponType is not WeaponType.TwoHanded
            || defender is null)
        {
            return 1.0f;
        }

        var moddedTwohandedCombatSkill = (uint)(playerAttacker.GetModdedTwohandedCombatSkill() * 1.5f);
        var defenderPhysicalDefense = defender.GetModdedPhysicalDefSkill();

        var damageMod = 0.5f * SkillCheck.GetSkillChance(moddedTwohandedCombatSkill, defenderPhysicalDefense);

        var finalDamageMod = 1.0f + (float)damageMod;

        return finalDamageMod;
    }

    /// <summary>
    /// COMBAT ABILITY - Multishot: Damage reduced by 25% if 2 targets, by 33% if 3 targets.
    /// </summary>
    private float GetCombatAbilityMultishotDamagePenalty(Player playerAttacker)
    {
        return 1.0f; // TODO: Decide if this damage penalty is needed

        if (playerAttacker is not { MultiShotIsActive: true})
        {
            return 1.0f;
        }

        return playerAttacker.MultishotNumTargets switch
        {
            3 => 0.67f,
            2 => 0.75f,
            _ => 1.0f,
        };
    }

    /// <summary>
    /// COMBAT ABILITY - Fury (Stance): Damage dealt and taken is increased by up to 25%. Attacking and
    /// taking damage builds up a Adrenaline meter. Adrenaline meter decreases over time.
    /// COMBAT ABILITY - Fury (Enrage): Damage increased by Adrenaline build up amount (%) for 10 seconds.
    /// </summary>
    private static float GetCombatAbilityFuryDamageBonus(Player playerAttacker, Player playerDefender)
    {
        var recklessMod = 1.0f;

        if (playerAttacker is {FuryStanceIsActive: true})
        {
            recklessMod += 0.25f * playerAttacker.AdrenalineMeter;
        }

        if (playerDefender is { FuryStanceIsActive: true } or { FuryEnrageIsActive: true })
        {
            recklessMod += 0.25f * playerDefender.AdrenalineMeter;
        }

        if (playerAttacker is {FuryEnrageIsActive: true})
        {
            recklessMod += playerAttacker.EnrageLevel;
        }

        return recklessMod;
    }

    /// <summary>
    /// COMBAT ABILITY - Relentless (Stance): Up to -10% damage, based on relentless adrenaline stacks.
    /// </summary>
    private static float GetCombatAbilityRelentlessDamagePenalty(Player playerAttacker)
    {
        if (playerAttacker is not { RelentlessStanceIsActive: true })
        {
            return 1.0f;
        }

        return 1.0f - 0.1f * playerAttacker.AdrenalineMeter;
    }

    /// <summary>
    /// COMBAT ABILITY - Steady Strike: +25% damage with melee/missile weapons.
    /// </summary>
    /// <param name="playerAttacker"></param>
    /// <returns></returns>
    private static float GetCombatAbilitySteadyStrikeDamageBonus(Player playerAttacker)
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
    /// ATTACK HEIGHT BONUS - High: (10% increased damage, 20% if weapon is specialized)
    /// </summary>
    private float GetHighAttackHeightBonus(Player playerAttacker)
    {
        if (playerAttacker is { AttackHeight: AttackHeight.High })
        {
            return WeaponIsSpecialized(playerAttacker) ? 1.2f : 1.10f;
        }

        return 1.0f;
    }

    private static float GetLevelScalingMod(Creature attacker, Creature defender, Player playerDefender)
    {
        var monsterHealthScalingMod = playerDefender != null
            ? LevelScaling.GetMonsterDamageDealtHealthScalar(playerDefender, attacker)
            : LevelScaling.GetMonsterDamageTakenHealthScalar(attacker, defender);

        return monsterHealthScalingMod;
    }

    private float GetCriticalChance(Creature attacker, Creature defender)
    {
        var playerAttacker = attacker as Player;
        var playerDefender = defender as Player;

        if (playerDefender != null && (playerDefender.IsLoggingOut || playerDefender.PKLogout || playerDefender.CombatMode is CombatMode.NonCombat))
        {
            return 1.0f;
        }

        if (CheckForRatingReprisal(playerAttacker))
        {
            return 1.0f;
        }

        var criticalChance = WorldObject.GetWeaponCriticalChance(Weapon, attacker, _attackSkill, defender);
        criticalChance += GetPlayerSpecSkillCriticalChanceBonus();

        return criticalChance;
    }

    private bool GetCriticalDefendedFromAug(Creature attacker, Creature defender)
    {
        var playerAttacker = attacker as Player;
        var playerDefender = defender as Player;

        _criticalDefendedFromAug = CheckForAugmentationCriticalDefense(playerDefender, playerAttacker);

        return _criticalDefendedFromAug;
    }

    private float GetCriticalDamageBeforeMitigation(Creature attacker, Creature defender)
    {
        var playerAttacker = attacker as Player;

        CriticalDamageBonusFromTrinket = 1.0f;
        playerAttacker?.CheckForSigilTrinketOnAttackEffects(defender, this, Skill.Thievery, SigilTrinketThieveryEffect.Treachery, true);

        _criticalDamageMod = 1.0f + WorldObject.GetWeaponCritDamageMod(Weapon, attacker, _attackSkill, defender);
        _criticalDamageMod += GetMaceSpecCriticalDamageBonus(playerAttacker);
        _criticalDamageMod += GetStaffSpecCriticalDamageBonus(playerAttacker);
        _criticalDamageMod *= 1.0f + Jewel.GetJewelEffectMod(playerAttacker, PropertyInt.GearBludgeon, "Bludgeon", rampQuestSource: defender);
        _criticalDamageMod *= CriticalDamageBonusFromTrinket;

        // RATING - Reprisal: the defender may evade the critical hit (see DoCalculateDamage)
        CheckForRatingReprisalCriticalDefense(attacker, _playerDefender);

        // _damageRatingMod already includes the PK damage rating (see SetDamageModifiers)
        _criticalDamageRating = Creature.GetPositiveRatingMod(attacker.GetCritDamageRating());
        _damageRatingMod = Creature.AdditiveCombine(_damageRatingMod, _criticalDamageRating);

        if (_baseDamageMod is null)
        {
            _log.Error("GetCriticalDamageBeforeMitigation({Attacker}, {Defender}) - _baseDamageMod is null", attacker.Name, defender.Name);
            return 0;
        }

        // Intentional: player crits always use the top of the weapon's damage range, while monster crits use
        // the median of their attack's range. Non-critical hits use a random roll for both (see _baseDamage).
        var baseDamage = playerAttacker != null ? _baseDamageMod.MaxDamage : _baseDamageMod.MedianDamage;

        return baseDamage
               * _attributeMod
               * _powerMod
               * _slayerMod
               * _damageRatingMod
               * _criticalDamageMod
               * _dualWieldDamageBonus
               * _twohandedCombatDamageBonus
               * _combatAbilityMultishotDamagePenalty
               * _combatAbilityFuryDamageBonus
               * _combatAbilityRelentlessDamagePenalty
               * _combatAbilitySteadyStrikeDamageBonus
               * _ratingElementalDamageBonus
               * _ratingPierceResistanceBonus
               * _recklessnessMod
               * SneakAttackMod
               * _backstabDamageMultiplier
               * _attackHeightDamageBonus
               * _ammoEffectMod
               * _levelScalingMod;
    }

    private float GetNonCriticalDamageBeforeMitigation()
    {
        return _baseDamage
               * _attributeMod
               * _powerMod
               * _slayerMod
               * _damageRatingMod
               * _recklessnessMod
               * SneakAttackMod
               * _backstabDamageMultiplier
               * _attackHeightDamageBonus
               * _ratingElementalDamageBonus
               * _ratingPierceResistanceBonus
               * _dualWieldDamageBonus
               * _twohandedCombatDamageBonus
               * _combatAbilityMultishotDamagePenalty
               * _combatAbilityFuryDamageBonus
               * _combatAbilityRelentlessDamagePenalty
               * _combatAbilitySteadyStrikeDamageBonus
               * _ammoEffectMod
               * _levelScalingMod;
    }

    /// <summary>
    /// RATING - Pierce: Ramping Piercing Resistance Penetration.
    /// Up to +20% + 1% per rating (at max quest stamps).
    /// (JEWEL - Black Garnet)
    /// </summary>
    private float GetRatingPierceResistanceBonus(Creature defender, Player playerAttacker)
    {
        if (playerAttacker == null)
        {
            return 1.0f;
        }

        var rating = playerAttacker.GetEquippedAndActivatedItemRatingSum(PropertyInt.GearPierce);

        if (rating <= 0 || DamageType != DamageType.Pierce)
        {
            return 1.0f;
        }

        var rampPercentage = (float)defender.QuestManager.GetCurrentSolves($"{playerAttacker.Name},Pierce") / 100;

        const float baseMod = 0.2f;
        const float bonusPerRating = 0.01f;

        return 1.0f + (rampPercentage * (baseMod + bonusPerRating * rating));
    }

    /// <summary>
    /// RATING - Reprisal: Evade an Incoming Crit, auto crit in return
    /// (JEWEL - Black Opal)
    /// </summary>
    private void CheckForRatingReprisalCriticalDefense(Creature attacker, Player playerDefender)
    {
        if (playerDefender == null)
        {
            return;
        }

        var rating = playerDefender.GetEquippedAndActivatedItemRatingSum(PropertyInt.GearReprisal);

        if (rating <= 0)
        {
            return;
        }

        var chance = Jewel.GetJewelEffectMod(playerDefender, PropertyInt.GearReprisal);

        if (ThreadSafeRandom.Next(0.0f, 1.0f) > chance)
        {
            return;
        }

        playerDefender.QuestManager.HandleReprisalQuest();
        playerDefender.QuestManager.Stamp($"{attacker.Guid}/Reprisal");
        Evaded = true;
        PartialEvasion = PartialEvasion.All;
        playerDefender.Reprisal = true;

        var msg = $"Reprisal! You evade the attack by {attacker.Name}";
        playerDefender.Session.Network.EnqueueSend(new GameMessageSystemChat(msg, ChatMessageType.CombatEnemy));
    }

    /// <summary>
    /// SPEC BONUS - Staff: +50% crit damage (additively)
    /// </summary>
    private static float GetStaffSpecCriticalDamageBonus(Player playerAttacker)
    {
        if (playerAttacker?.GetEquippedWeapon() == null)
        {
            return 0.0f;
        }

        return IsWeaponSkillSpecialized(playerAttacker, Skill.Staff, Skill.Staff) ? 0.5f : 0.0f;
    }

    /// <summary>
    /// SPEC BONUS - Martial Weapons (Mace): +50% crit damage (additively)
    /// </summary>
    private static float GetMaceSpecCriticalDamageBonus(Player playerAttacker)
    {
        if (playerAttacker?.GetEquippedWeapon() == null)
        {
            return 0.0f;
        }

        return IsWeaponSkillSpecialized(playerAttacker, Skill.Mace, Skill.MartialWeapons) ? 0.5f : 0.0f;
    }

    /// <summary>
    /// SPEC BONUS - Perception - Up to 50% chance to defend against a critical hit, based on Perception vs the attacker's effective attack skill
    /// </summary>
    private bool CheckForSpecPerceptionCriticalDefense(Player playerDefender)
    {
        if (playerDefender == null)
        {
            return false;
        }

        var perception = playerDefender.GetCreatureSkill(Skill.Perception);
        if (perception.AdvancementClass != SkillAdvancementClass.Specialized)
        {
            return false;
        }

        // an attack skill of 0 can't beat any Perception, so it gets the full 50%
        var skillCheck = EffectiveAttackSkill > 0 ? playerDefender.GetModdedPerceptionSkill() / (float)EffectiveAttackSkill : 1.0f;
        var criticalDefenseChance = skillCheck > 1f ? 0.5f : skillCheck * 0.5f;

        return criticalDefenseChance > ThreadSafeRandom.Next(0f, 1f);
    }

    private static bool CheckForAugmentationCriticalDefense(Player playerDefender, Player playerAttacker)
    {
        if (playerDefender == null || playerDefender.AugmentationCriticalDefense <= 0)
        {
            return false;
        }

        var criticalDefenseMod = playerAttacker != null ? 0.05f : 0.25f;
        var criticalDefenseChance = playerDefender.AugmentationCriticalDefense * criticalDefenseMod;

        return !(criticalDefenseChance < ThreadSafeRandom.Next(0.0f, 1.0f));
    }

    private bool CheckForRatingReprisal(Player playerAttacker)
    {
        if (playerAttacker == null)
        {
            return false;
        }

        if (playerAttacker.GetEquippedAndActivatedItemRatingSum(PropertyInt.GearReprisal) <= 0)
        {
            return false;
        }

        if (!playerAttacker.QuestManager.HasQuest($"{_defender.Guid}/Reprisal"))
        {
            return false;
        }

        playerAttacker.QuestManager.Erase($"{_defender.Guid}/Reprisal");
        return true;
    }

    /// <summary>
    /// SPEC BONUS - Axe/Dagger: +5% crit chance
    /// </summary>
    private float GetPlayerSpecSkillCriticalChanceBonus()
    {
        if (_playerAttacker?.GetEquippedWeapon() == null)
        {
            return 0.0f;
        }

        // SPEC BONUS - Martial Weapons (Axe): +5% crit chance (additively)
        if (IsWeaponSkillSpecialized(_playerAttacker, Skill.Axe, Skill.MartialWeapons))
        {
            return 0.05f;
        }

        // SPEC BONUS - Dagger: +5% crit chance (additively)
        if (IsWeaponSkillSpecialized(_playerAttacker, Skill.Dagger, Skill.Dagger))
        {
            return 0.05f;
        }

        return 0.0f;
    }

    /// <summary>
    /// Returns the base damage for a player attacker
    /// </summary>
    private void GetBaseDamage(Player attacker)
    {
        if (_damageSource.ItemType == ItemType.MissileWeapon)
        {
            DamageType = _damageSource.W_DamageType;

            // handle prismatic arrows
            if (DamageType == DamageType.Base)
            {
                if (Weapon != null && Weapon.W_DamageType != DamageType.Undef)
                {
                    DamageType = Weapon.W_DamageType;
                }
                else
                {
                    DamageType = DamageType.Pierce;
                }
            }
        }
        else
        {
            DamageType = attacker.GetDamageType(false, CombatType.Melee);
        }

        // TODO: combat maneuvers for player?
        _baseDamageMod = attacker.GetBaseDamageMod(_damageSource);

        // some quest bows can have built-in damage bonus
        if (Weapon?.WeenieType == WeenieType.MissileLauncher)
        {
            _baseDamageMod.DamageBonus += Weapon.Damage ?? 0;
        }

        if (_damageSource.ItemType == ItemType.MissileWeapon)
        {
            _baseDamageMod.ElementalBonus = WorldObject.GetMissileElementalDamageBonus(Weapon, attacker, DamageType);
            _baseDamageMod.DamageMod = WorldObject.GetMissileElementalDamageModifier(Weapon, DamageType);
        }

        _baseDamage = (float)ThreadSafeRandom.Next(_baseDamageMod.MinDamage, _baseDamageMod.MaxDamage);
    }

    /// <summary>
    /// Returns the base damage for a non-player attacker
    /// </summary>
    private void GetBaseDamage(Creature attacker, MotionCommand motionCommand, AttackHook attackHook)
    {
        _attackPart = attacker.GetAttackPart(motionCommand, attackHook);
        if (_attackPart.Value == null)
        {
            _generalFailure = true;
            return;
        }

        _baseDamageMod = attacker.GetBaseDamage(_attackPart.Value);
        _baseDamage = (float)ThreadSafeRandom.Next(_baseDamageMod.MinDamage, _baseDamageMod.MaxDamage);

        DamageType = attacker.GetDamageType(_attackPart.Value, CombatType);
    }

    public static float GetAmmoEffectMod(WorldObject weapon, Player player)
    {
        if (weapon is {IsAmmoLauncher: not true} || player is null)
        {
            return 1.0f;
        }

        var ammo = player.GetEquippedAmmo() as Ammunition;

        if (ammo?.AmmoEffectUsesRemaining is null)
        {
            return 1.0f;
        }

        switch ((AmmoEffect)(ammo.AmmoEffect ?? 0))
        {
            case AmmoEffect.Sharpened: return 1.1f;
            default: return 1.0f;
        }
    }
}
