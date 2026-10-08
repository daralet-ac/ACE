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
    private readonly ILogger _log = Log.ForContext<DamageEvent>();
    private float _accuracyMod;
    private float _ammoEffectMod;
    private List<WorldObject> _armor;
    private float _armorMod;
    private Creature _attacker;
    private AttackHeight _attackHeight;
    private float _attackHeightDamageBonus;
    private AttackHook _attackHook;
    private MotionCommand? _attackMotion;
    private KeyValuePair<CombatBodyPart, PropertiesBodyPart> _attackPart; // body part this monster is attacking with
    private CreatureSkill _attackSkill;
    private AttackType _attackType; // slash / thrust / punch / kick / offhand / multistrike
    private float _attributeMod;
    private float _backstabDamageMultiplier;
    private float _baseDamage;
    private BaseDamageMod _baseDamageMod;
    private float _combatAbilityAegisDamageReduction;
    private float _combatAbilityFuryDamageBonus;
    private float _combatAbilityRelentlessDamagePenalty;
    private float _combatAbilityMultishotDamagePenalty;
    private float _combatAbilityPhalanxDamageReduction;
    private float _combatAbilityProvokeDamageReduction;
    private Creature_BodyPart _creaturePart;
    private float _criticalChance;
    private float _criticalDamageMod;
    private float _criticalDamageRating;
    private float _criticalDamageResistanceRatingMod;
    private float _imbuedArmorPhysicalDamageMod;
    private float _imbuedArmorCritDamageMod;
    private bool _criticalDefendedFromAug;
    private float _damageBeforeMitigation;
    private float _damageMitigated;
    private float _damageRatingMod;
    private float _damageResistanceRatingBaseMod;
    private float _damageResistanceRatingMod;
    private WorldObject _damageSource;
    private Creature _defender;
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
    private float _ratingPierceResistanceBonus;
    private float _recklessnessMod;
    private float _resistanceMod;
    private float _slayerMod;
    private float _specDefenseMod;
    private float _swarmedDamageReductionMod;
    private float _combatAbilitySteadyStrikeDamageBonus;
    private float _twohandedCombatDamageBonus;
    private float _weaponResistanceMod;

    private bool IgnoreMagicArmor =>
        (Weapon?.IgnoreMagicArmor ?? false) || (_attacker?.IgnoreMagicArmor ?? false); // ignores impen / banes

    private bool IgnoreMagicResist =>
        (Weapon?.IgnoreMagicResist ?? false) || (_attacker?.IgnoreMagicResist ?? false); // ignores life armor / prots

    public bool Blocked { get; private set; }
    public bool Parried { get; private set; }
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
    public bool HasDamage => !Evaded && !Blocked && !Parried && !LifestoneProtection;

    public AttackConditions AttackConditions
    {
        get
        {
            var attackConditions = new AttackConditions();

            if (_criticalDefendedFromAug)
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
            _log.Information("---- LEVEL SCALING - {Attacker} vs {Defender} ----", attacker.Name, defender.Name);
        }

        if (defender.Name is "Placeholder")
        {
            return 0;
        }

        SetCombatSources(attacker, defender, damageSource);
        CheckForOnAttackEffects(cleaveHits);

        SetInvulnerable(defender);

        if (_invulnerable)
        {
            return 0.0f;
        }

        // Evade, block and parry all compare against these skills, so they must be set before any of them roll,
        // including when a guaranteed hit (Overpower, Enrage, Backstab) skips the evade roll.
        SetAttackAndDefenseSkills(attacker, defender);

        SetEvaded(attacker, defender);
        SetBlocked(attacker, defender);
        SetParry(attacker, defender);

        if (Evaded || Blocked || Parried)
        {
            if (Blocked)
            {
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

        if (defender.Invulnerable)
        {
            Damage = 0.0f;
            defender.OnInvulnerableHit();
        }

        _damageMitigated = _damageBeforeMitigation - Damage;

        PostDamageMitigationEffects(attacker, defender, damageSource);

        // Reprisal (during the critical hit) and a missing body part (during the armor lookup) can evade the attack after
        // its damage is rolled. The on-hit effects above still trigger, but no damage is dealt and no threat is generated.
        if (Evaded)
        {
            Damage = 0.0f;
            IsCritical = false;
            return 0.0f;
        }

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

    private static bool IsWeaponSkillSpecialized(Player player, Skill weaponSkill, Skill creatureSkill)
    {
        return player.GetEquippedWeapon().WeaponSkill == weaponSkill
               && player.GetCreatureSkill(creatureSkill).AdvancementClass == SkillAdvancementClass.Specialized;
    }

    private static bool IsSkillSpecialized(Player player, Skill creatureSkill)
    {
        return player?.GetCreatureSkill(creatureSkill).AdvancementClass == SkillAdvancementClass.Specialized;
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
}
