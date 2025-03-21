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

public class DamageEvent
{
    private readonly ILogger _log = Log.ForContext<DamageEvent>();

    private float _accuracyMod;
    private float _ammoEffectMod;
    private List<WorldObject> _armor;
    private float _armorMod;
    private Creature _attacker;
    private CombatAbility _attackerCombatAbility;
    private AttackHeight _attackHeight;
    private float _attackHeightDamageBonus;
    private AttackHook _attackHook;
    private MotionCommand? _attackMotion;
    private KeyValuePair<CombatBodyPart, PropertiesBodyPart> _attackPart; // body part this monster is attacking with
    private CreatureSkill _attackSkill;
    private AttackType _attackType; // slash / thrust / punch / kick / offhand / multistrike
    private float _attributeMod;
    private float _baseDamage;
    private BaseDamageMod _baseDamageMod;
    private float _combatAbilityFuryDamageBonus;
    private float _combatAbilityMultishotDamagePenalty;
    private float _combatAbilityProvokeDamageBonus;
    private Creature_BodyPart _creaturePart;
    private float _criticalChance;
    private float _criticalDamageMod;
    private float _criticalDamageRating;
    private float _criticalDamageResistanceRatingMod;
    private bool _criticalDefended;
    private float _damageBeforeMitigation;
    private float _damageMitigated;
    private float _damageRatingMod;
    private float _damageResistanceRatingBaseMod;
    private float _damageResistanceRatingMod;
    private WorldObject _damageSource;
    private Creature _defender;
    private CombatAbility _defenderCombatAbility;
    private float _dualWieldDamageBonus;
    private uint _effectiveDefenseSkill;
    private float _evasionMod;
    private bool _generalFailure;
    private float _ignoreArmorMod;
    private bool _invulnerable;
    private float _levelScalingMod;
    private bool _overpower;
    private bool _pkBattle;
    private float _pkDamageMod;
    private float _pkDamageResistanceMod;
    private Player _playerAttacker;
    private Player _playerDefender;
    private float _powerMod;
    private KeyValuePair<CombatBodyPart, PropertiesBodyPart> _propertiesBodyPart;
    private Quadrant _quadrant;
    private float _ratingElementalDamageBonus;
    private float _ratingDamageTypeWard;
    private float _ratingRedFury;
    private float _ratingYellowFury;
    private float _ratingSelfHarm;
    private float _recklessnessMod;
    private float _resistanceMod;
    private float _slayerMod;
    private float _specDefenseMod;
    private float _combatAbilitySteadyShotDamageBonus;
    private float _twohandedCombatDamageBonus;
    private float _weaponResistanceMod;

    private bool IgnoreMagicArmor =>
        (Weapon?.IgnoreMagicArmor ?? false) || (_attacker?.IgnoreMagicArmor ?? false); // ignores impen / banes

    private bool IgnoreMagicResist =>
        (Weapon?.IgnoreMagicResist ?? false) || (_attacker?.IgnoreMagicResist ?? false); // ignores life armor / prots

    public bool Blocked { get; private set; }
    public float CriticalDamageBonusFromTrinket { get; set; }
    public bool CriticalOverridedByTrinket { get; set; }
    public bool Evaded { get; set; }
    public bool LifestoneProtection { get; private set; }
    public PartialEvasion PartialEvasion { get; set; }
    public uint EffectiveAttackSkill { get; private set; }
    public float SneakAttackMod { get; private set; }
    public bool IsCritical { get; private set; }
    public BodyPart BodyPart { get; private set; }
    public float ShieldMod { get; private set; }
    public float Damage { get; private set; }
    public CombatType CombatType { get; private set; }
    public DamageType DamageType { get; private set; }
    public WorldObject Weapon { get; private set; }
    public WorldObject DefenderWeapon { get; private set; }
    public WorldObject Offhand { get; private set; }

    public bool HasDamage => !Evaded && !Blocked && !LifestoneProtection;

    public AttackConditions AttackConditions
    {
        get
        {
            var attackConditions = new AttackConditions();

            if (_criticalDefended)
            {
                attackConditions |= AttackConditions.CriticalProtectionAugmentation;
            }

            if (_recklessnessMod > 1.0f)
            {
                attackConditions |= AttackConditions.Recklessness;
            }

            if (SneakAttackMod > 1.0f)
            {
                attackConditions |= AttackConditions.SneakAttack;
            }

            if (_overpower)
            {
                attackConditions |= AttackConditions.Overpower;
            }

            return attackConditions;
        }
    }

    public static DamageEvent CalculateDamage(
        Creature attacker,
        Creature defender,
        WorldObject damageSource,
        MotionCommand? attackMotion = null,
        AttackHook attackHook = null,
        bool cleaveHits = false
    )
    {
        var damageEvent = new DamageEvent { _attackMotion = attackMotion, _attackHook = attackHook };

        damageSource ??= attacker;

        damageEvent.DoCalculateDamage(attacker, defender, damageSource, cleaveHits);

        damageEvent.HandleLogging(attacker, defender);

        return damageEvent;
    }

    private float DoCalculateDamage(Creature attacker, Creature defender, WorldObject damageSource, bool cleaveHits = false)
    {
        if (PropertyManager.GetBool("debug_level_scaling_system").Item && (attacker is Player || defender is Player))
        {
            Console.WriteLine($"\n\n---- LEVEL SCALING - {attacker.Name} vs {defender.Name} ----");
        }

        if (defender.Name is "Placeholder")
        {
            return 0;
        }

        SetCombatSources(attacker, defender, damageSource);
        SetCombatAbilities(attacker, defender);

        SetInvulnerable(defender);
        SetEvaded(attacker, defender);
        SetBlocked(attacker, defender);

        if (_invulnerable || Evaded || Blocked)
        {
            if (Blocked)
            {
                CheckForParryRiposte(attacker, defender, damageSource);
                CheckForRatingThorns(attacker, defender, damageSource);
            }

            return 0.0f;
        }

        _damageBeforeMitigation = GetDamageBeforeMitigation(attacker, defender, damageSource);

        if (_generalFailure)
        {
            return 0.0f;
        }

        var mitigation = GetMitigation(attacker, defender);
        var cleaveMod = cleaveHits ? 0.5f : 1.0f;

        Damage = _damageBeforeMitigation * mitigation * cleaveMod;
        _damageMitigated = _damageBeforeMitigation - Damage;

        PostDamageMitigationEffects(attacker, defender, damageSource);

        //DpsLogging();

        return Damage;
    }

    /// <summary>
    /// Sets PlayerAttacker, PlayerDefender, Attacker, Defender, PkBattle, AttackSkill, CombatType, DamageSource, Weapon, AttackType, AttackHeight
    /// </summary>
    private void SetCombatSources(Creature attacker, Creature defender, WorldObject damageSource)
    {
        _playerAttacker = attacker as Player;
        _playerDefender = defender as Player;

        _pkBattle = _playerAttacker != null && _playerDefender != null;

        _attacker = attacker;
        _defender = defender;

        _attackSkill = attacker.GetCreatureSkill(attacker.GetCurrentWeaponSkill());

        CombatType = damageSource.ProjectileSource == null ? CombatType.Melee : CombatType.Missile;

        _damageSource = damageSource;

        Weapon =
            damageSource.ProjectileSource == null
                ? attacker.GetEquippedMeleeWeapon()
                : (damageSource.ProjectileLauncher ?? damageSource.ProjectileAmmo);

        DefenderWeapon =
            defender.GetEquippedWeapon();

        Offhand = attacker.GetEquippedOffHand();

        _attackType = attacker.AttackType;
        _attackHeight = attacker.AttackHeight ?? AttackHeight.Medium;
    }

    private void SetCombatAbilities(Creature attacker, Creature defender)
    {
        _attackerCombatAbility = CombatAbility.None;
        _defenderCombatAbility = CombatAbility.None;

        var attackerCombatFocus = attacker?.GetEquippedCombatFocus();
        if (attackerCombatFocus != null)
        {
            _attackerCombatAbility = attackerCombatFocus.GetCombatAbility();
        }

        var defenderCombatFocus = defender?.GetEquippedCombatFocus();
        if (defenderCombatFocus != null)
        {
            _defenderCombatAbility = defenderCombatFocus.GetCombatAbility();
        }
    }

    private void SetInvulnerable(Creature defender)
    {
        var playerDefender = defender as Player;

        if (playerDefender is { UnderLifestoneProtection: true })
        {
            LifestoneProtection = true;
            playerDefender.HandleLifestoneProtection();
            {
                _invulnerable = true;
            }
        }

        if (defender.Invincible)
        {
            {
                _invulnerable = true;
            }
        }

        _invulnerable = false;
    }

    /// <summary>
    /// Checks for Overpower, Steady Shot, Fury, and Backstab auto-hits.
    /// If evade succeeded, determine if evade was full, partial, or none.
    /// Equal chance for each evasion type to occur.
    /// </summary>
    private void SetEvaded(Creature attacker, Creature defender)
    {
        var playerAttacker = attacker as Player;

        Evaded = false;
        _evasionMod = 1.0f;
        PartialEvasion = PartialEvasion.None;

        if (defender.CombatMode is CombatMode.NonCombat)
        {
            return;
        }

        // Check for guaranteed hits
        var isOverpower = CheckForOverpower(attacker, defender);
        var isSteadyShotNoEvade = CheckForCombatAbilitySteadyShotNoEvade(playerAttacker, _attackerCombatAbility);
        var isFuryNoEvade = CheckForCombatAbilityFuryNoEvade(playerAttacker, _attackerCombatAbility);
        var isBackstabNoEvade = CheckForCombatAbilityBackstabNoEvade(playerAttacker);

        if (isOverpower || isSteadyShotNoEvade || isFuryNoEvade || isBackstabNoEvade || attacker == defender)
        {
            return;
        }

        // Roll combat hit chance
        var attackRoll = ThreadSafeRandom.Next(0.0f, 1.0f);
        if (attackRoll > GetEvadeChance(attacker, defender))
        {
            return;
        }

        // Roll evade type (33% for each evade type)
        const float fullEvadeChance = 1.0f / 3.0f;
        const float partialEvadeChance = fullEvadeChance * 2;

        var roll = ThreadSafeRandom.Next(0.0f, 1.0f);

        switch (roll)
        {
            case < fullEvadeChance:
                PartialEvasion = PartialEvasion.All;
                Evaded = true;
                break;
            case < partialEvadeChance when _defenderCombatAbility == CombatAbility.Provoke:
                _evasionMod = 0.25f;
                PartialEvasion = PartialEvasion.Some; // glancing blow w/ Provoke bonus
                Evaded = false;
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

        if (defender is Player playerDefender && PartialEvasion == PartialEvasion.Some)
        {
            playerDefender.CheckForSigilTrinketOnAttackEffects(playerAttacker, this, Skill.PhysicalDefense, (int)SigilTrinketPhysicalDefenseEffect.Evasion);
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

    private bool CheckForCombatAbilityFuryNoEvade(Player playerAttacker, CombatAbility attackerCombatAbility)
    {
        if (playerAttacker == null)
        {
            return false;
        }

        if (
            attackerCombatAbility != CombatAbility.Fury
            || !playerAttacker.RecklessActivated
            || !(playerAttacker.LastRecklessActivated > Time.GetUnixTime() - playerAttacker.RecklessActivatedDuration)
        )
        {
            return false;
        }

        Evaded = false;
        PartialEvasion = PartialEvasion.None;
        _evasionMod = 1f;

        return true;
    }

    private bool CheckForCombatAbilitySteadyShotNoEvade(Player playerAttacker, CombatAbility attackerCombatAbility)
    {
        if (playerAttacker == null)
        {
            return false;
        }

        if (
            attackerCombatAbility != CombatAbility.SteadyShot
            || playerAttacker.GetEquippedMissileLauncher() == null
            || !(playerAttacker.LastSteadyShotActivated > Time.GetUnixTime() - playerAttacker.SteadyShotActivatedDuration)
        )
        {
            return false;
        }

        Evaded = false;
        PartialEvasion = PartialEvasion.None;
        _evasionMod = 1f;

        return true;
    }

    /// <summary>
    /// Attack cannot be evaded if Backstab ability activated when attacking from behind
    /// </summary>
    private bool CheckForCombatAbilityBackstabNoEvade(Player playerAttacker)
    {
        if (playerAttacker == null)
        {
            return false;
        }

        if (
            playerAttacker.EquippedCombatAbility != CombatAbility.Backstab
            || !(playerAttacker.LastBackstabActivated > Time.GetUnixTime() - playerAttacker.BackstabActivatedDuration)
        )
        {
            return false;
        }

        Evaded = false;
        PartialEvasion = PartialEvasion.None;

        return true;
    }

    /// <summary>
    /// Checks attack angle and if defender has Spec Shield and/or Phalanx is active. If a block is possible,
    /// check for and combine bonuses from Spec Physical Defense, Parry Ability, Phalanx Ability, and Block Rating.
    /// Roll and set Blocked accordingly.
    /// <list type="bullet">
    /// <item>Shield Spec Bonus: +45 degrees to effective block angle.</item>
    /// <item>Physical Defense Spec Bonus: Up to +10% block chance.</item>
    /// <item>Parry Ability: Passive 20% to block chance. Additional +15% when activated.</item>
    /// <item>Phalanx Ability: +50% to block chance.</item>
    /// <item>Gear Block Rating: +X% block chance, equal to rating amount.</item>
    /// </list>
    /// </summary>
    private void SetBlocked(Creature attacker, Creature defender)
    {
        var playerDefender = defender as Player;

        if (defender.CombatMode is CombatMode.NonCombat)
        {
            Blocked = false;
            return;
        }

        var blockChance = 0.0f;
        var effectiveAngle = 180.0f;

        effectiveAngle += GetSpecShieldEffectiveAngleBonus(playerDefender);

        // check for frontal radius prior to allowing a block unless PhalanxActivated
        var phalanxIsNotActive = playerDefender is not { EquippedCombatAbility: CombatAbility.Phalanx }
                            || playerDefender.LastPhalanxActivated <
                            Time.GetUnixTime() - playerDefender.PhalanxActivatedDuration;

        var blockableAngle = Math.Abs(defender.GetAngle(attacker)) < effectiveAngle / 2.0f;

        if (phalanxIsNotActive && !blockableAngle)
        {
            Blocked = false;
            return;
        }

        blockChance += GetSpecPhysicalDefenseBlockChanceBonus(playerDefender);
        blockChance += GetCombatAbilityParryBlockChanceBonus(playerDefender);
        blockChance += GetCombatAbilityPhalanxBlockChanceBonus(playerDefender);
        blockChance *= 1.0f + Jewel.GetJewelEffectMod(playerDefender, PropertyInt.GearBlock);

        if ((ThreadSafeRandom.Next(0f, 1f) > blockChance))
        {
            Blocked = false;
            return;
        }

        Blocked = true;
    }

    private float GetDamageBeforeMitigation(Creature attacker, Creature defender, WorldObject damageSource)
    {
        SetBaseDamage(attacker, defender, damageSource);
        SetDamageModifiers(attacker, defender);

        _criticalChance = GetCriticalChance(attacker, defender);
        _criticalDefended = GetCriticalDefended(attacker, defender);

        var roll = ThreadSafeRandom.Next(0.0f, 1.0f);
        if (roll > _criticalChance || _criticalDefended)
        {
            _playerAttacker?.CheckForSigilTrinketOnAttackEffects(defender, this, Skill.TwoHandedCombat, (int)SigilTrinketTwohandedCombatEffect.Might);
            _playerAttacker?.CheckForSigilTrinketOnAttackEffects(defender, this, Skill.Shield, (int)SigilTrinketShieldEffect.Might);

            if (!CriticalOverridedByTrinket)
            {
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

    private void SetDamageModifiers(Creature attacker, Creature defender)
    {
        var playerAttacker = attacker as Player;
        var playerDefender = defender as Player;

        _powerMod = attacker.GetPowerMod(Weapon);
        _attributeMod = attacker.GetAttributeMod(Weapon, false, defender);
        _slayerMod = WorldObject.GetWeaponCreatureSlayerModifier(Weapon, attacker, defender);
        _damageRatingMod = Creature.GetPositiveRatingMod(attacker.GetDamageRating());
        _dualWieldDamageBonus = GetDualWieldDamageBonus(playerAttacker);
        _twohandedCombatDamageBonus = GetTwohandedCombatDamageBonus(playerAttacker);
        _combatAbilityMultishotDamagePenalty = GetCombatAbilityMultishotDamagePenalty(playerAttacker);
        _combatAbilityProvokeDamageBonus = GetCombatAbilityProvokeDamageBonus(playerAttacker);
        _combatAbilityFuryDamageBonus = GetCombatAbilityRecklessDamageBonus(attacker, defender, playerAttacker);
        _combatAbilitySteadyShotDamageBonus = GetCombatAbilitySteadyShotDamageBonus(attacker, defender, playerAttacker);
        _recklessnessMod = Creature.GetRecklessnessMod(attacker, defender);
        SneakAttackMod = attacker.GetSneakAttackMod(defender);
        _attackHeightDamageBonus += GetHighAttackHeightBonus(playerAttacker);
        _ratingElementalDamageBonus = Jewel.HandleElementalBonuses(playerAttacker, DamageType);
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
    private float GetDualWieldDamageBonus(Player playerAttacker)
    {
        return playerAttacker is { IsDualWieldAttack: true, DualWieldAlternate: false }
            ? playerAttacker.GetDualWieldDamageMod()
            : 1.0f;
    }

    /// <summary>
    /// Two-handed Combat Damage Mod
    /// </summary>
    private static float GetTwohandedCombatDamageBonus(Player playerAttacker)
    {
        if (playerAttacker?.GetEquippedWeapon() == null)
        {
            return 1.0f;
        }

        return playerAttacker.GetEquippedWeapon().W_WeaponType == WeaponType.TwoHanded
            ? playerAttacker.GetTwoHandedCombatDamageMod()
            : 1.0f;
    }

    /// <summary>
    /// COMBAT ABILITY - Multishot: Damage reduced by 25%.
    /// </summary>
    private float GetCombatAbilityMultishotDamagePenalty(Player playerAttacker)
    {
        return playerAttacker is { EquippedCombatAbility: CombatAbility.Multishot } ? 0.75f : 1.0f;
    }

    private void PostDamageMitigationEffects()
    {
        if (_attacker.IsMonster)
        {
            Damage *= 1.0f;
        }

        if (!_attacker.IsMonster)
        {
            Damage *= 1.0f;
        }
    }

    /// <summary>
    /// COMBAT ABILITY - Provoke: Damage increased by 20%.
    /// </summary>
    private static float GetCombatAbilityProvokeDamageBonus(Player playerAttacker)
    {
        if (playerAttacker == null)
        {
            return 1.0f;
        }

        if (playerAttacker.EquippedCombatAbility != CombatAbility.Provoke)
        {
            return 1.0f;
        }

        return playerAttacker.LastProvokeActivated > Time.GetUnixTime() - playerAttacker.ProvokeActivatedDuration
            ? 1.2f
            : 1.0f;
    }

    /// <summary>
    /// COMBAT ABILITY - Fury: Damage increased by up to 25%.
    /// </summary>
    private float GetCombatAbilityRecklessDamageBonus(Creature attacker, Creature defender, Player playerAttacker)
    {
        var recklessMod = 1.0f;

        if (playerAttacker == null)
        {
            return recklessMod;
        }

        if (playerAttacker.EquippedCombatAbility != CombatAbility.Fury || defender == playerAttacker)
        {
            return recklessMod;
        }

        if (CombatType != CombatType.Melee && !(attacker.GetDistance(defender) < 3))
        {
            return recklessMod;
        }

        // 500 stacks is max, out of 2000 for a max of 25%
        var recklessStacks = Player.HandleRecklessStamps(playerAttacker);

        // If Reckless is not activated and last Reckless duration is over, recklessMod += stacks / 2000
        if (
            !playerAttacker.RecklessActivated
            && playerAttacker.LastRecklessActivated < Time.GetUnixTime() - playerAttacker.RecklessActivatedDuration
        )
        {
            recklessMod += recklessStacks / 2000f;
        }

        // If Reckless is activated and Reckless duration is over, set Activated to false and erase quest stamps
        if (
            playerAttacker.RecklessActivated
            && playerAttacker.LastRecklessActivated < Time.GetUnixTime() - playerAttacker.RecklessActivatedDuration
        )
        {
            playerAttacker.RecklessActivated = false;
            playerAttacker.RecklessDumped = false;
            playerAttacker.QuestManager.Erase($"{playerAttacker.Name},Reckless");
        }

        // If Reckless is activated and duration is not over, set Activated to false, erase quest stamps, and recklessMod += stacks / 1000
        if (
            playerAttacker.RecklessActivated
            && playerAttacker.LastRecklessActivated > Time.GetUnixTime() - playerAttacker.RecklessActivatedDuration
        )
        {
            playerAttacker.RecklessActivated = false;
            playerAttacker.RecklessDumped = true;
            playerAttacker.QuestManager.Erase($"{playerAttacker.Name},Reckless");

            recklessMod += recklessStacks / 1000f;
        }

        return recklessMod;
    }

    private float GetCombatAbilitySteadyShotDamageBonus(Creature attacker, Creature defender, Player playerAttacker)
    {
        if (playerAttacker == null)
        {
            return 1.0f;
        }

        if (
            _attackerCombatAbility is not CombatAbility.SteadyShot
            || playerAttacker.GetEquippedMissileLauncher() == null
            || !(playerAttacker.LastSteadyShotActivated > Time.GetUnixTime() - playerAttacker.SteadyShotActivatedDuration)
        )
        {
            return 1.0f;
        }

        return 1.25f;
    }

    /// <summary>
    /// ATTACK HEIGHT BONUS - High: (10% increased damage, 15% if weapon is specialized)
    /// </summary>
    private float GetHighAttackHeightBonus(Player playerAttacker)
    {
        if (playerAttacker is { AttackHeight: AttackHeight.High })
        {
            return WeaponIsSpecialized(playerAttacker) ? 1.15f : 1.10f;
        }

        return 1.0f;
    }

    private static float GetLevelScalingMod(Creature attacker, Creature defender, Player playerDefender)
    {
        var monsterHealthScalingMod = playerDefender != null
            ? LevelScaling.GetMonsterDamageDealtHealthScalar(playerDefender, attacker)
            : LevelScaling.GetMonsterDamageTakenHealthScalar(attacker, defender);

        var timeToKillMonsterScalingMod = playerDefender != null
            ? 1.0f
            : LevelScaling.GetMonsterDamageTakenTtkScalar(attacker, defender);

        var monsterDpsPerTierScalingMod = playerDefender != null
            ? 1.0f
            : LevelScaling.GetMonsterDamageDealtDpsPerTierScalar(attacker, defender);

        return monsterHealthScalingMod * timeToKillMonsterScalingMod * monsterDpsPerTierScalingMod;
    }

    private float GetCriticalChance(Creature attacker, Creature defender)
    {
        var playerAttacker = attacker as Player;
        var playerDefender = defender as Player;

        if (playerDefender != null && (playerDefender.IsLoggingOut || playerDefender.PKLogout || playerDefender.CombatMode is CombatMode.NonCombat))
        {
            return 1.0f;
        }

        if (CheckForPlayerStealthGuaranteedCritical(playerAttacker, playerDefender) || CheckForRatingReprisal(playerAttacker))
        {
            return 1.0f;
        }

        var criticalChance = WorldObject.GetWeaponCriticalChance(Weapon, attacker, _attackSkill, defender);
        criticalChance += GetPlayerBackstabCriticalChanceBonus();
        criticalChance += GetPlayerSpecSkillCriticalChanceBonus();

        return criticalChance;
    }

    private bool GetCriticalDefended(Creature attacker, Creature defender)
    {
        var playerAttacker = attacker as Player;
        var playerDefender = defender as Player;

        return CheckForAugmentationCriticalDefense(playerDefender, playerAttacker)
               || CheckForSpecPerceptionCriticalDefense(playerDefender);
    }

    private float GetCriticalDamageBeforeMitigation(Creature attacker, Creature defender)
    {
        var playerAttacker = attacker as Player;
        var playerDefender = defender as Player;

        CriticalDamageBonusFromTrinket = 1.0f;
        playerAttacker?.CheckForSigilTrinketOnAttackEffects(defender, this, Skill.Thievery, (int)SigilTrinketThieveryEffect.Treachery, true);

        _criticalDamageMod = 1.0f + WorldObject.GetWeaponCritDamageMod(Weapon, attacker, _attackSkill, defender);
        _criticalDamageMod += GetMaceSpecCriticalDamageBonus(playerAttacker);
        _criticalDamageMod += GetStaffSpecCriticalDamageBonus(playerAttacker);
        _criticalDamageMod *= 1.0f + Jewel.GetJewelEffectMod(playerAttacker, PropertyInt.GearBludgeon, "Bludgeon");
        _criticalDamageMod *= CriticalDamageBonusFromTrinket;

        CheckForRatingReprisalCriticalDefense(attacker, playerDefender);

        _criticalDamageRating = Creature.GetPositiveRatingMod(attacker.GetCritDamageRating());
        _damageRatingMod = Creature.AdditiveCombine(_damageRatingMod, _criticalDamageRating);

        if (_pkBattle)
        {
            _damageRatingMod = Creature.AdditiveCombine(_damageRatingMod, _pkDamageMod);
        }

        if (_baseDamageMod is null)
        {
            _log.Error("GetCriticalDamageBeforeMitigation({Attacker}, {Defender}) - _baseDamageMod is null", attacker.Name, defender.Name);
            return 0;
        }

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
               * _combatAbilityProvokeDamageBonus
               * _combatAbilityFuryDamageBonus
               * _combatAbilitySteadyShotDamageBonus
               * SneakAttackMod
               * _attackHeightDamageBonus
               * _levelScalingMod;
    }

    private float GetNonCriticalDamageBeforeMitigation()
    {
        // if (_playerAttacker is not null)
        // {
        //     Console.WriteLine($"Before Mitigaion\n" +
        //                       $" -Base Damage{_baseDamage}\n" +
        //                       $" -Power: {_powerMod}\n" +
        //                       $" -Slayer: {_slayerMod}\n" +
        //                       $" -DamageRating: {_damageRatingMod}\n" +
        //                       $" -Reckless: {_recklessnessMod}\n" +
        //                       $" -SneakAttack: {SneakAttackMod}\n" +
        //                       $" -AttackHeight: {_attackHeightDamageBonus}\n" +
        //                       $" -ElementalDamageRating: {_ratingElementalDamageBonus}\n" +
        //                       $" -DualWield: {_dualWieldDamageBonus}\n" +
        //                       $" -TwoHand: {_twohandedCombatDamageBonus}\n" +
        //                       $" -MultiShot: {_combatAbilityMultishotDamagePenalty}\n" +
        //                       $" -Provoke: {_combatAbilityProvokeDamageBonus}\n" +
        //                       $" -Fury: {_combatAbilityFuryDamageBonus}\n" +
        //                       $" -SteadyShot: {_combatAbilitySteadyShotDamageBonus}\n" +
        //                       $" -LevelScaling: {_levelScalingMod}");
        // }

        return _baseDamage
               * _attributeMod
               * _powerMod
               * _slayerMod
               * _damageRatingMod
               * _recklessnessMod
               * SneakAttackMod
               * _attackHeightDamageBonus
               * _ratingElementalDamageBonus
               * _dualWieldDamageBonus
               * _twohandedCombatDamageBonus
               * _combatAbilityMultishotDamagePenalty
               * _combatAbilityProvokeDamageBonus
               * _combatAbilityFuryDamageBonus
               * _combatAbilitySteadyShotDamageBonus
               * _ammoEffectMod
               * _levelScalingMod;
    }

    private bool CheckForPlayerStealthGuaranteedCritical(Creature playerAttacker, Creature playerDefender)
    {
        if (playerAttacker == null)
        {
            return false;
        }

        if (!IsAttackFromStealth())
        {
            return false;
        }

        if (playerDefender == null)
        {
            SneakAttackMod = 3.0f;
        }

        return true;
    }

    /// <summary>
    /// COMBAT ABILITY - Backstab: +20% crit chance from behind
    /// </summary>
    private float GetPlayerBackstabCriticalChanceBonus()
    {
        return SneakAttackMod > 1.0f ? 0.2f : 0.0f;
    }

    private float GetMitigation(Creature attacker, Creature defender)
    {
        var playerAttacker = attacker as Player;
        var playerDefender = defender as Player;

        _ignoreArmorMod = GetIgnoreArmorMod(attacker, defender);
        _ignoreArmorMod -= GetSpearSpecIgnoreArmorBonus(attacker);

        _armorMod = GetArmorMod(attacker, defender);

        _weaponResistanceMod = WorldObject.GetWeaponResistanceModifier(
            Weapon,
            attacker,
            _attackSkill,
            DamageType,
            defender
        );

        _resistanceMod = GetResistanceMod(defender, playerDefender);

        if (DamageType is DamageType.Pierce)
        {
            _resistanceMod += Jewel.GetJewelEffectMod(playerAttacker, PropertyInt.GearPierce, "Pierce");
        }

        _damageResistanceRatingMod = GetDamageResistRatingMod(defender, _pkBattle);
        _damageResistanceRatingMod *= 1.0f + Jewel.GetJewelEffectMod(playerAttacker, PropertyInt.GearHardenedDefense, "Hardened Defense");

        _specDefenseMod = GetSpecDefenseMod(attacker, playerDefender);

        ShieldMod = _defender.GetShieldMod(attacker, DamageType, Weapon);

        _ratingSelfHarm = 1.0f + Jewel.GetJewelEffectMod(playerAttacker, PropertyInt.GearSelfHarm);
        _ratingRedFury = 1.0f + Jewel.GetJewelRedFury(playerAttacker);
        _ratingYellowFury = 1.0f + Jewel.GetJewelYellowFury(playerAttacker);
        _ratingDamageTypeWard = DamageType switch
        {
            DamageType.Physical => Jewel.GetJewelEffectMod(playerDefender, PropertyInt.GearPhysicalWard),
            DamageType.Elemental => Jewel.GetJewelEffectMod(playerDefender, PropertyInt.GearElementalWard),
            _ => 1.0f
        };

        // if (playerAttacker is not null)
        // {
        //     Console.WriteLine(
        //         $"{_armorMod} {ShieldMod} {_resistanceMod} {_damageResistanceRatingMod} {_evasionMod} {_specDefenseMod} {_ratingDamageTypeWard} {_ratingSelfHarm} {_ratingRedFury} {_ratingYellowFury}");
        // }

        return _armorMod
               * ShieldMod
               * _resistanceMod
               * _damageResistanceRatingMod
               * _evasionMod
               * _specDefenseMod
               * _ratingDamageTypeWard
               * _ratingSelfHarm
               * _ratingRedFury
               * _ratingYellowFury;
    }

    private float GetIgnoreArmorMod(Creature attacker, Creature defender)
    {
        if (Weapon is null or { SpecialPropertiesRequireMana: true, ItemCurMana: 0 })
        {
            return 1.0f;
        }

        var playerAttacker = attacker as Player;

        var armorRendingMod = GetArmorRendingMod(defender, playerAttacker);
        var armorCleavingMod = attacker.GetArmorCleavingMod(Weapon);

        return Math.Min(armorRendingMod, armorCleavingMod);
    }

    private float GetArmorRendingMod(Creature defender, Player playerAttacker)
    {
        if (Weapon is null or { SpecialPropertiesRequireMana: true, ItemCurMana: 0 })
        {
            return 1.0f;
        }

        if (Weapon.HasImbuedEffect(ImbuedEffectType.ArmorRending))
        {
            return 1.0f - WorldObject.GetArmorRendingMod(_attackSkill, playerAttacker, defender);
        }

        return 1.0f;
    }

    /// <summary>
    /// SPEC BONUS - Martial Weapons (Spear): +10% armor penetration (additively)
    /// </summary>
    private float GetSpearSpecIgnoreArmorBonus(Creature attacker)
    {
        var playerAttacker = attacker as Player;

        if (playerAttacker?.GetEquippedWeapon() == null)
        {
            return 0.0f;
        }

        return IsWeaponSkillSpecialized(playerAttacker, Skill.Spear, Skill.MartialWeapons) ? 0.1f : 0.0f;
    }

    private float GetArmorMod(Creature attacker, Creature defender)
    {
        var playerDefender = defender as Player;

        if (Weapon != null && Weapon.HasImbuedEffect(ImbuedEffectType.IgnoreAllArmor))
        {
            return 1.0f;
        }

        if (playerDefender != null)
        {
            // select random body part @ current attack height
            GetBodyPart(_attackHeight);

            // get player armor pieces
            _armor = attacker.GetArmorLayers(playerDefender, BodyPart);

            // get armor modifiers
            return attacker.GetArmorMod(playerDefender, DamageType, _armor, Weapon, _ignoreArmorMod);
        }

        // determine height quadrant
        _quadrant = GetQuadrant(defender, attacker, _attackHeight, _damageSource);

        // select random body part @ current attack height
        GetBodyPart(defender, _quadrant);

        _armor = _creaturePart.GetArmorLayers(_propertiesBodyPart.Key);

        // get target armor
        return _creaturePart.GetArmorMod(DamageType, _armor, attacker, Weapon, _ignoreArmorMod);
    }

    private float GetResistanceMod(Creature defender, Player playerDefender)
    {
        if (playerDefender != null)
        {
            return playerDefender.GetResistanceMod(DamageType, _attacker, Weapon, _weaponResistanceMod);
        }

        var resistanceType = Creature.GetResistanceType(DamageType);

        return (float)
            Math.Max(0.0f, defender.GetResistanceMod(resistanceType, _attacker, Weapon, _weaponResistanceMod));
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
            return 0.0f;
        }

        var rating = playerAttacker.GetEquippedAndActivatedItemRatingSum(PropertyInt.GearPierce);

        if (rating <= 0 || DamageType != DamageType.Pierce)
        {
            return 0.0f;
        }

        var rampPercentage = (float)defender.QuestManager.GetCurrentSolves($"{playerAttacker.Name},Pierce") / 100;

        const float baseMod = 0.2f;
        const float bonusPerRating = 0.01f;

        return rampPercentage * (baseMod + bonusPerRating * rating);
    }

    private void PostDamageMitigationEffects(Creature attacker, Creature defender, WorldObject damageSource)
    {
        var playerAttack = attacker as Player;
        var playerDefender = defender as Player;

        CheckForRatingPostDamageEffects(attacker, defender, damageSource, playerAttack, playerDefender);
        CheckForCombatAbilityFuryRecklessSelfDamage(playerAttack);

        if (_attacker.IsMonster)
        {
            Damage *= 1.0f;
        }

        if (!_attacker.IsMonster)
        {
            Damage *= 1.0f;
        }
    }

    /// <summary>
    /// RATING (jewels) POST-DAMAGE STAMPS / PROCS / BONUSES
    /// </summary>
    private void CheckForRatingPostDamageEffects(
        Creature attacker,
        Creature defender,
        WorldObject damageSource,
        Player playerAttacker,
        Player playerDefender
    )
    {
        if (playerAttacker != null)
        {
            Jewel.HandlePlayerAttackerBonuses(playerAttacker, defender, Damage, DamageType);
            Jewel.HandleMeleeMissileAttackerRampingQuestStamps(playerAttacker, defender, DamageType);
        }

        if (playerDefender != null)
        {
            Jewel.HandlePlayerDefenderBonuses(playerDefender, attacker, Damage);
            Jewel.HandleMeleeMissileDefenderRampingQuestStamps(playerDefender);
        }
    }

    /// <summary>
    /// COMBAT ABILITY - Fury Self-damage chance with reckless attacks.
    /// Max Fury stacks = 500. Become "Reckless" at 250 stacks. Self-damage chance grows as player gets closer
    /// to 500 stacks. Max chance = 10%. When Self-damage occurs, attacker takes damage equal to
    /// 50% of the damage done to the enemy on this attack, or 10% of player max health, whichever is smaller.
    /// </summary>
    private void CheckForCombatAbilityFuryRecklessSelfDamage(Player playerAttacker)
    {
        if (playerAttacker == null || playerAttacker.EquippedCombatAbility != CombatAbility.Fury)
        {
            return;
        }

        const int recklessThreshold = 250;
        var stacks = playerAttacker.QuestManager.GetCurrentSolves($"{playerAttacker.Name},Reckless");

        if (stacks <= recklessThreshold)
        {
            return;
        }

        var recklessChance = 0.1f * (stacks - recklessThreshold) / recklessThreshold;

        if (!(recklessChance > ThreadSafeRandom.Next(0f, 1f)))
        {
            return;
        }

        var damageDealt = (uint)(Damage / 2);
        var percentHealth = playerAttacker.Health.MaxValue / 10;
        var damage = Math.Min(damageDealt, percentHealth);

        playerAttacker.UpdateVitalDelta(playerAttacker.Health, -(int)damage);
        playerAttacker.DamageHistory.Add(playerAttacker, DamageType.Health, damage);

        playerAttacker.Session.Network.EnqueueSend(
            new GameMessageSystemChat(
                $"In your rage, you injure yourself, suffering {damage} points of damage!",
                ChatMessageType.CombatSelf
            )
        );

        playerAttacker.PlayParticleEffect(PlayScript.SplatterMidLeftFront, playerAttacker.Guid);

        if (!playerAttacker.IsDead)
        {
            return;
        }

        var lastDamager = new DamageHistoryInfo(playerAttacker);

        playerAttacker.OnDeath(lastDamager, DamageType.Health);
        playerAttacker.Die();
    }

    /// <summary>
    /// SPEC BONUS: Physical Defense
    /// </summary>
    private static float GetSpecDefenseMod(Creature attacker, Player playerDefender)
    {
        if (
            playerDefender == null
            || playerDefender.GetCreatureSkill(Skill.PhysicalDefense).AdvancementClass != SkillAdvancementClass.Specialized
        )
        {
            return 1.0f;
        }

        var playerDefenderPhysicalDefense =
            playerDefender.GetModdedPhysicalDefSkill()
            * LevelScaling.GetPlayerDefenseSkillScalar(playerDefender, attacker);
        var bonusAmount = Math.Min(playerDefenderPhysicalDefense, 500) / 50;

        return 0.9f - bonusAmount * 0.01f;
    }

    /// <summary>
    /// RATING - Hardened Defense: Ramping Physical Damage Reduction.
    /// Up to +20% + 1% per rating (at max quest stamps).
    /// (JEWEL - Diamond)
    /// </summary>
    private static float GetRatingHardenedDefenseDamageResistanceBonus(Player playerDefender)
    {
        if (playerDefender == null)
        {
            return 0.0f;
        }

        var rating = playerDefender.GetEquippedAndActivatedItemRatingSum(PropertyInt.GearHardenedDefense);

        if (rating <= 0)
        {
            return 0.0f;
        }

        var rampPercentage = (float)playerDefender.QuestManager.GetCurrentSolves($"{playerDefender.Name},Hardened Defense") / 100;

        const float baseMod = 0.2f;
        const float bonusPerRating = 0.01f;

        return rampPercentage * (baseMod + bonusPerRating * rating);
    }

    private float GetDamageResistRatingMod(Creature defender, bool pkBattle)
    {
        _damageResistanceRatingBaseMod = defender.GetDamageResistRatingMod(CombatType);

        if (IsCritical)
        {
            _criticalDamageResistanceRatingMod = Creature.GetNegativeRatingMod(defender.GetCritDamageResistRating());
            return Creature.AdditiveCombine(_damageResistanceRatingBaseMod, _criticalDamageResistanceRatingMod);
        }

        if (pkBattle)
        {
            _pkDamageResistanceMod = Creature.GetNegativeRatingMod(defender.GetPKDamageResistRating());
            return Creature.AdditiveCombine(_damageResistanceRatingBaseMod, _pkDamageResistanceMod);
        }

        return _damageResistanceRatingBaseMod;
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
    /// RATING - Bludgeon: Ramping Bludgeon Crit Damage Bonus.
    /// Up to +20% + 1% per rating (at max quest stamps).
    /// (JEWEL - White Sapphire)
    /// </summary>
    private static float GetRatingBludgeonCriticalDamageBonus(Creature defender, Player playerAttacker)
    {
        if (playerAttacker == null)
        {
            return 0.0f;
        }

        var rating = playerAttacker.GetEquippedAndActivatedItemRatingSum(PropertyInt.GearBludgeon);
        if (rating <= 0)
        {
            return 0.0f;
        }

        var rampPercentage = (float)defender.QuestManager.GetCurrentSolves($"{playerAttacker.Name},Bludgeon") / 100;

        const float baseMod = 0.2f;
        const float bonusPerRating = 0.01f;

        return rampPercentage * (baseMod + bonusPerRating * rating);
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
    /// SPEC BONUS - Perception - 50% chance to defend against a critical hit
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

        var skillCheck = perception.Current / (float)_attackSkill.Current;
        var criticalDefenseChance = skillCheck > 1f ? 0.5f : skillCheck * 0.5f;

        if (!(criticalDefenseChance > ThreadSafeRandom.Next(0f, 1f)))
        {
            return false;
        }

        playerDefender.Session.Network.EnqueueSend(
            new GameMessageSystemChat(
                $"Your perception skill allowed you to prevent a critical strike!",
                ChatMessageType.CombatEnemy
            )
        );

        return true;
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

    private bool CheckForRatingReprisal(Creature playerAttacker)
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

    private static bool IsWeaponSkillSpecialized(Player player, Skill weaponSkill, Skill creatureSkill)
    {
        return player.GetEquippedWeapon().WeaponSkill == weaponSkill
               && player.GetCreatureSkill(creatureSkill).AdvancementClass == SkillAdvancementClass.Specialized;
    }

    private static bool IsSkillSpecialized(Player player, Skill creatureSkill)
    {
        return player?.GetCreatureSkill(creatureSkill).AdvancementClass == SkillAdvancementClass.Specialized;
    }

    /// <summary>
    /// RATING - Thorns: Reflects damage on block
    /// (JEWEL - White Quartz)
    /// </summary>
    private void CheckForRatingThorns(Creature attacker, Creature defender, WorldObject damageSource)
    {
        var playerDefender = defender as Player;

        if (Blocked != true || playerDefender == null || !(attacker.GetDistance(playerDefender) < 10))
        {
            return;
        }

        if (playerDefender.GetEquippedAndActivatedItemRatingSum(PropertyInt.GearThorns) <= 0)
        {
            return;
        }

        SetBaseDamage(attacker, defender, damageSource);
        SetDamageModifiers(attacker, defender);

        var damage = GetNonCriticalDamageBeforeMitigation();

        var thornsAmount = damage * Jewel.GetJewelEffectMod(playerDefender, PropertyInt.GearThorns);

        attacker.UpdateVitalDelta(attacker.Health, -(int)thornsAmount);
        attacker.DamageHistory.Add(playerDefender, DamageType.Health, (uint)thornsAmount);
        playerDefender.ShieldReprisal = (int)thornsAmount;

        var msg = $"You deflect {thornsAmount} damage back to the attacker!";
        playerDefender.Session.Network.EnqueueSend(new GameMessageSystemChat(msg, ChatMessageType.CombatSelf));

        if (!attacker.IsDead)
        {
            return;
        }

        attacker.OnDeath(attacker.DamageHistory.LastDamager, DamageType.Health);
        attacker.Die();
    }

    private void CheckForParryRiposte(Creature attacker, Creature defender, WorldObject damageSource)
    {
        var playerDefender = defender as Player;

        if (!Blocked || playerDefender == null)
        {
            return;
        }

        if (
            _defenderCombatAbility != CombatAbility.Parry
            || !(playerDefender.LastParryActivated > Time.GetUnixTime() - playerDefender.ParryActivatedDuration)
            || !(attacker.GetDistance(playerDefender) < 3)
        )
        {
            return;
        }

        if (playerDefender.TwoHandedCombat || playerDefender.IsDualWieldAttack)
        {
            playerDefender.DamageTarget(attacker, damageSource);
        }
    }

    private bool IsAttackFromStealth()
    {
        if (_playerAttacker == null)
        {
            return false;
        }

        var isAttackFromStealth = _playerAttacker.IsAttackFromStealth;
        _playerAttacker.IsAttackFromStealth = false;

        return isAttackFromStealth;
    }

    private Quadrant GetQuadrant(
        Creature defender,
        Creature attacker,
        AttackHeight attackHeight,
        WorldObject damageSource
    )
    {
        var quadrant = attackHeight.ToQuadrant();

        var wo = damageSource.CurrentLandblock != null ? damageSource : attacker;

        quadrant |= wo.GetRelativeDir(defender);

        return quadrant;
    }

    /// <summary>
    /// Returns the chance for creature to avoid monster attack
    /// </summary>
    private float GetEvadeChance(Creature attacker, Creature defender)
    {
        var playerAttacker = attacker as Player;
        var playerDefender = defender as Player;

        _accuracyMod = attacker.GetAccuracySkillMod(Weapon);

        EffectiveAttackSkill = (uint)(attacker.GetEffectiveAttackSkill() * LevelScaling.GetPlayerAttackSkillScalar(playerAttacker, defender));

        EffectiveAttackSkill = Convert.ToUInt32(EffectiveAttackSkill * CheckForAttackHeightMediumAttackSkillBonus(playerAttacker));
        EffectiveAttackSkill = Convert.ToUInt32(EffectiveAttackSkill * CheckForCombatAbilitySteadyShotAttackSkillBonus(playerAttacker));
        EffectiveAttackSkill = Convert.ToUInt32(EffectiveAttackSkill * (1.0f + Jewel.GetJewelEffectMod(playerAttacker, PropertyInt.GearBravado, "Bravado")));

        _effectiveDefenseSkill = (uint)(defender.GetEffectiveDefenseSkill(CombatType) * LevelScaling.GetPlayerDefenseSkillScalar(playerDefender, attacker)
        );

        _effectiveDefenseSkill = Convert.ToUInt32(_effectiveDefenseSkill * CheckForAttackHeightLowDefenseSkillBonus(playerDefender, playerAttacker));
        _effectiveDefenseSkill = Convert.ToUInt32(_effectiveDefenseSkill * (1.0f + Jewel.GetJewelEffectMod(playerDefender, PropertyInt.GearFamiliarity, "Familiarity")));

        var evadeChance = 1.0f - SkillCheck.GetSkillChance(EffectiveAttackSkill, _effectiveDefenseSkill);
        evadeChance = CheckForCombatAbilitySmokescreenEvadeChanceBonus(evadeChance, playerDefender);

        if (evadeChance < 0)
        {
            evadeChance = 0;
        }

        return (float)Math.Min(evadeChance, 1.0f);
    }

    private double CheckForCombatAbilitySmokescreenEvadeChanceBonus(double evadeChance, Player playerDefender)
    {
        // COMBAT FOCUS - Smokescreen (+10% chance to evade, +40% on Activated)
        if (_defenderCombatAbility == CombatAbility.Smokescreen)
        {
            evadeChance += 0.1f;

            if (
                playerDefender != null
                && playerDefender.LastSmokescreenActivated
                > Time.GetUnixTime() - playerDefender.SmokescreenActivatedDuration
            )
            {
                evadeChance += 0.3f;
            }
        }

        return evadeChance;
    }

    /// <summary>
    /// COMBAT ABILITY - Steady Shot: Increased attack skill by 20%.
    /// </summary>
    private float CheckForCombatAbilitySteadyShotAttackSkillBonus(Player playerAttacker)
    {
        if (playerAttacker == null)
        {
            return 1.0f;
        }

        return _attackerCombatAbility == CombatAbility.SteadyShot ? 1.2f : 1.0f;
    }

    /// <summary>
    /// ATTACK HEIGHT BONUS: Low (+10% physical defense skill, +15% if weapon specialized)
    /// </summary>
    /// <returns></returns>
    private float CheckForAttackHeightLowDefenseSkillBonus(Player playerDefender, Player playerAttacker)
    {
        if (playerDefender is { AttackHeight: AttackHeight.Low })
        {
            return WeaponIsSpecialized(playerAttacker) ? 1.15f : 1.1f;
        }

        return 1.0f;
    }

    /// <summary>
    /// ATTACK HEIGHT BONUS: Medium (+10% attack skill, +15% if weapon specialized)
    /// </summary>
    private float CheckForAttackHeightMediumAttackSkillBonus(Player playerAttacker)
    {
        if (playerAttacker is { AttackHeight: AttackHeight.Medium })
        {
            return WeaponIsSpecialized(playerAttacker) ? 1.15f : 1.1f;
        }

        return 1.0f;
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

    /// <summary>
    /// Returns a body part for a player defender
    /// </summary>
    private void GetBodyPart(AttackHeight attackHeight)
    {
        // select random body part @ current attack height
        BodyPart = BodyParts.GetBodyPart(attackHeight);
    }

    /// <summary>
    /// Returns a body part for a creature defender
    /// </summary>
    private void GetBodyPart(Creature defender, Quadrant quadrant)
    {
        // get cached body parts table
        var bodyParts = Creature.GetBodyParts(defender.WeenieClassId);

        // rng roll for body part
        var bodyPart = bodyParts.RollBodyPart(quadrant);

        if (bodyPart == CombatBodyPart.Undefined)
        {
            _log.Debug(
                "DamageEvent.GetBodyPart({Defender} ({DefenderGuid}) ) - couldn't find body part for wcid {DefenderWeenieClassId}, Quadrant {BodyPartQuadrant}",
                defender.Name,
                defender.Guid,
                defender.WeenieClassId,
                quadrant
            );
            Evaded = true;
            return;
        }

        //Console.WriteLine($"AttackHeight: {AttackHeight}, Quadrant: {quadrant & FrontBack}{quadrant & LeftRight}, AttackPart: {bodyPart}");

        defender.Biota.PropertiesBodyPart.TryGetValue(bodyPart, out var value);
        _propertiesBodyPart = new KeyValuePair<CombatBodyPart, PropertiesBodyPart>(bodyPart, value);

        // select random body part @ current attack height
        /*BiotaPropertiesBodyPart = BodyParts.GetBodyPart(defender, attackHeight);

        if (BiotaPropertiesBodyPart == null)
        {
            Evaded = true;
            return;
        }*/

        _creaturePart = new Creature_BodyPart(defender, _propertiesBodyPart);
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
    /// SPEC BONUS - Physical Defense: Increase shield block chance up to 10% (additively).
    /// Based on defender 'shield level' and attacker 'attack skill'.
    /// </summary>
    private float GetSpecPhysicalDefenseBlockChanceBonus(Player playerDefender)
    {
        if (playerDefender?.GetEquippedShield() == null || !IsSkillSpecialized(playerDefender, Skill.PhysicalDefense))
        {
            return 0.0f;
        }

        var shieldArmorLevel = playerDefender.GetEquippedShield().ArmorLevel ?? 0;

        var blockChanceMod = SkillCheck.GetSkillChance((uint)shieldArmorLevel, EffectiveAttackSkill);

        return 0.1f * (float)blockChanceMod;
    }

    /// <summary>
    /// COMBAT ABILITY - Parry: Passively increases Block Chance by 20% (additively) while using a two-handed weapon or dual-wielding.
    /// Increased to 35% when Parry skill is activated.
    /// </summary>
    private float GetCombatAbilityParryBlockChanceBonus(Player playerDefender)
    {
        if (playerDefender == null)
        {
            return 0.0f;
        }

        var blockChance = 0.0f;

        if (playerDefender is not { EquippedCombatAbility: CombatAbility.Parry })
        {
            return blockChance;
        }

        if (!playerDefender.TwoHandedCombat && !playerDefender.IsDualWieldAttack)
        {
            return blockChance;
        }

        blockChance = 0.2f;

        if (playerDefender.LastParryActivated > Time.GetUnixTime() - playerDefender.ParryActivatedDuration)
        {
            blockChance += 0.15f;
        }

        return blockChance;
    }

    /// <summary>
    /// COMBAT ABILITY - Phalanx: When activated, increases block chance by 50% (additively).
    /// </summary>
    private float GetCombatAbilityPhalanxBlockChanceBonus(Player playerDefender)
    {
        return playerDefender?.LastPhalanxActivated > Time.GetUnixTime() - playerDefender?.PhalanxActivatedDuration ? 0.5f : 0.0f;
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

    private bool WeaponIsSpecialized(Player playerAttacker)
    {
        if (playerAttacker == null)
        {
            return false;
        }

        if (Weapon != null)
        {
            switch (Weapon.WeaponSkill)
            {
                case Skill.Axe:
                    return playerAttacker.GetCreatureSkill(Skill.MartialWeapons).AdvancementClass
                           == SkillAdvancementClass.Specialized;
                case Skill.Mace:
                    return playerAttacker.GetCreatureSkill(Skill.MartialWeapons).AdvancementClass
                           == SkillAdvancementClass.Specialized;
                case Skill.Sword:
                    return playerAttacker.GetCreatureSkill(Skill.MartialWeapons).AdvancementClass
                           == SkillAdvancementClass.Specialized;
                case Skill.Spear:
                    return playerAttacker.GetCreatureSkill(Skill.MartialWeapons).AdvancementClass
                           == SkillAdvancementClass.Specialized;
                case Skill.Dagger:
                    return playerAttacker.GetCreatureSkill(Skill.Dagger).AdvancementClass
                           == SkillAdvancementClass.Specialized;
                case Skill.Staff:
                    return playerAttacker.GetCreatureSkill(Skill.Staff).AdvancementClass
                           == SkillAdvancementClass.Specialized;
                case Skill.UnarmedCombat:
                    return playerAttacker.GetCreatureSkill(Skill.UnarmedCombat).AdvancementClass
                           == SkillAdvancementClass.Specialized;
                case Skill.Bow:
                    return playerAttacker.GetCreatureSkill(Skill.Bow).AdvancementClass
                           == SkillAdvancementClass.Specialized;
                case Skill.Crossbow:
                    return playerAttacker.GetCreatureSkill(Skill.Bow).AdvancementClass
                           == SkillAdvancementClass.Specialized;
                case Skill.ThrownWeapon:
                    return playerAttacker.GetCreatureSkill(Skill.ThrownWeapon).AdvancementClass
                           == SkillAdvancementClass.Specialized;
                default:
                    return false;
            }
        }

        return playerAttacker.GetCreatureSkill(Skill.UnarmedCombat).AdvancementClass == SkillAdvancementClass.Specialized;
    }

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

        Console.WriteLine($"\n---- DAMAGE LOG ({damageSource.Name}) ----");
        Console.WriteLine(
            $"CurrentTime: {currentTime}, LastAttackTime: {_attacker.LastAttackTime} TimeBetweenAttacks: {timeSinceLastAttack}"
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
            * _combatAbilitySteadyShotDamageBonus;
        var averageDpsBeforeMitigation = averageDamageBeforeMitigation / timeSinceLastAttack;

        var averageDamageAfterMitigation =
            averageDamageBeforeMitigation
            * _armorMod
            * ShieldMod
            * _resistanceMod
            * _damageResistanceRatingMod
            * _levelScalingMod;
        var averageDpsAfterMitigation = averageDamageAfterMitigation / timeSinceLastAttack;

        Console.WriteLine(
            $"TimeSinceLastAttack: {timeSinceLastAttack}"
            + $"\n\n-- Base --\n"
            + $"BaseDamageMod.MaxDamage: {_baseDamageMod.MaxDamage}, BaseDamageMod.MinDamage: {_baseDamageMod.MinDamage}, LiveBaseDamage: {_baseDamage}\n"
            + $"AverageDamageNonCrit: {avgNonCritHit}, AverageDamageCrit: {critHit}, AverageDamageHit: {averageDamage}\n"
            + $"DPS Base: {baseDps}\n\n"
            + $"-- Before Mitigation --\n"
            + $"PowerMod: {_powerMod}, AttributeMod: {_attributeMod}, SlayerMod: {_slayerMod}, DamageRatingMod: {_damageRatingMod}, DualWieldMod: {_dualWieldDamageBonus}, TwoHandMod: {_twohandedCombatDamageBonus}, SteadyShotMod: {_combatAbilitySteadyShotDamageBonus}\n"
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

        if (_criticalDefended)
        {
            info += $"CriticalDefended: {_criticalDefended}\n";
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
