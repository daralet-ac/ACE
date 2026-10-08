using System.Collections.Generic;
using ACE.DatLoader.Entity.AnimationHooks;
using ACE.Entity.Enum;
using ACE.Entity.Models;
using ACE.Server.Managers;
using ACE.Server.WorldObjects;
using ACE.Server.WorldObjects.Entity;
using Serilog;

namespace ACE.Server.Entity;

public partial class DamageEvent
{
    private readonly ILogger _log = Log.ForContext<DamageEvent>();
    private float _accuracyMod;
    private List<WorldObject> _armor;
    private Creature _attacker;
    private AttackHeight _attackHeight;
    private AttackHook _attackHook;
    private MotionCommand? _attackMotion;
    private KeyValuePair<CombatBodyPart, PropertiesBodyPart> _attackPart; // body part this monster is attacking with
    private CreatureSkill _attackSkill;
    private AttackType _attackType; // slash / thrust / punch / kick / offhand / multistrike
    private float _baseDamage;
    private BaseDamageMod _baseDamageMod;
    private Creature_BodyPart _creaturePart;
    private float _criticalChance;
    private float _criticalDamageMod;
    private float _criticalDamageRating;
    private float _criticalDamageResistanceRatingMod;
    private bool _criticalDefendedFromAug;
    private float _damageBeforeMitigation;
    private float _damageMitigated;
    private float _damageResistanceRatingBaseMod;
    private WorldObject _damageSource;
    private Creature _defender;
    private uint _effectiveDefenseSkill;
    private DamageModifiers _damageModifiers;
    private float _evasionMod;
    private bool _generalFailure;
    private float _ignoreArmorMod;
    private bool _invulnerable;
    private MitigationModifiers _mitigationModifiers;
    private bool _overpower;
    private bool _pkBattle;
    private float _pkDamageMod;
    private float _pkDamageResistanceMod;
    private Player _playerAttacker;
    private Player _playerDefender;
    private KeyValuePair<CombatBodyPart, PropertiesBodyPart> _propertiesBodyPart;
    private Quadrant _quadrant;
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
    public float SneakAttackMod => _damageModifiers.SneakAttack;
    public bool IsCritical { get; private set; }
    public BodyPart BodyPart { get; private set; }
    public float ShieldMod => _mitigationModifiers.Shield;
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

            if (_damageModifiers.Recklessness > 1.0f)
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

        SetInvulnerable();

        if (_invulnerable)
        {
            return 0.0f;
        }

        // Evade, block and parry all compare against these skills, so they must be set before any of them roll,
        // including when a guaranteed hit (Overpower, Enrage, Backstab) skips the evade roll.
        SetAttackAndDefenseSkills();

        SetEvaded();
        SetBlocked();
        SetParry();

        if (Evaded || Blocked || Parried)
        {
            if (Blocked)
            {
                CheckForRatingThorns();
            }

            return 0.0f;
        }

        _damageBeforeMitigation = GetDamageBeforeMitigation();

        if (_generalFailure)
        {
            return 0.0f;
        }

        var mitigation = GetMitigation();
        var cleaveMod = cleaveHits ? 0.5f : 1.0f;

        Damage = _damageBeforeMitigation * mitigation * cleaveMod;

        if (defender.Invulnerable)
        {
            Damage = 0.0f;
            defender.OnInvulnerableHit();
        }

        _damageMitigated = _damageBeforeMitigation - Damage;

        PostDamageMitigationEffects();

        // Reprisal (during the critical hit) and a missing body part (during the armor lookup) can evade the attack after
        // its damage is rolled. The on-hit effects above still trigger, but no damage is dealt and no threat is generated.
        if (Evaded)
        {
            Damage = 0.0f;
            IsCritical = false;
            return 0.0f;
        }

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

    /// <summary>
    /// Returns true if the player's equipped weapon uses weaponSkill and the skill that specializes it is specialized
    /// </summary>
    private static bool IsWeaponSkillSpecialized(Player player, Skill weaponSkill)
    {
        var specializationSkill = GetSpecializationSkill(weaponSkill);

        return specializationSkill != null
               && player.GetEquippedWeapon().WeaponSkill == weaponSkill
               && IsSkillSpecialized(player, specializationSkill.Value);
    }

    private static bool IsSkillSpecialized(Player player, Skill creatureSkill)
    {
        return player?.GetCreatureSkill(creatureSkill).AdvancementClass == SkillAdvancementClass.Specialized;
    }

    /// <summary>
    /// Returns true if the player attacker has specialized the skill for this attack's weapon (Unarmed Combat with no weapon)
    /// </summary>
    private bool WeaponIsSpecialized()
    {
        if (_playerAttacker == null)
        {
            return false;
        }

        var specializationSkill = GetSpecializationSkill(Weapon?.WeaponSkill ?? Skill.UnarmedCombat);

        return specializationSkill != null && IsSkillSpecialized(_playerAttacker, specializationSkill.Value);
    }

    /// <summary>
    /// Returns the skill that has to be specialized for a weapon skill's specialization bonuses, or null if it has none
    /// </summary>
    private static Skill? GetSpecializationSkill(Skill weaponSkill)
    {
        return weaponSkill switch
        {
            Skill.Axe or Skill.Mace or Skill.Sword or Skill.Spear => Skill.MartialWeapons,
            Skill.Crossbow => Skill.Bow,
            Skill.Dagger or Skill.Staff or Skill.UnarmedCombat or Skill.Bow or Skill.ThrownWeapon => weaponSkill,
            _ => null,
        };
    }
}
