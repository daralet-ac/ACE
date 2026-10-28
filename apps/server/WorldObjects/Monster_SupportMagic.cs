using System.Collections.Concurrent;
using System.Linq;
using ACE.Entity.Enum;
using ACE.Server.Entity;

namespace ACE.Server.WorldObjects;

/// <summary>
/// Monster support casting: a monster with a heal or a buff for others in its spellbook casts it on an ally that needs it,
/// and otherwise fights with the rest of its spellbook
/// </summary>
partial class Creature
{
    /// <summary>
    /// The ally the current spell was rolled for, when it's a support spell, or null
    /// </summary>
    private Creature CurrentSpellAlly { get; set; }

    // an ally is healed (or has its stamina or mana restored) once it's below this share of that vital
    public const float SupportVitalThreshold = 0.5f;

    private static readonly ConcurrentDictionary<int, bool> supportSpells = new();
    private static readonly ConcurrentDictionary<int, bool> allyOnlySpells = new();

    /// <summary>
    /// Returns TRUE for a spell a monster casts on its allies rather than its foes: a heal or a vital boost, a creature or
    /// life buff, a weapon aura (Aura of Blood Drinker Other...), or a buff for a held weapon
    /// </summary>
    public static bool IsSupportSpell(int spellId) => supportSpells.GetOrAdd(spellId, id => IsSupportSpell(new Spell(id)));

    /// <summary>
    /// Returns TRUE for a beneficial spell cast on someone else: one a monster must never cast on its attack target, whether
    /// or not it's a support spell it knows how to use
    /// </summary>
    public static bool IsAllyOnlySpell(int spellId) =>
        allyOnlySpells.GetOrAdd(
            spellId,
            id => new Spell(id) is { NotFound: false, IsBeneficial: true, IsSelfTargeted: false } spell
                && spell.NonComponentTargetType != ItemType.None
        );

    public static bool IsSupportSpell(Spell spell)
    {
        if (spell.NotFound || !spell.IsBeneficial || spell.IsSelfTargeted || spell.IsFellowshipSpell)
        {
            return false;
        }

        if (spell.NonComponentTargetType == ItemType.None)
        {
            return false;
        }

        if (IsWeaponBuff(spell) || IsWeaponAura(spell))
        {
            return true;
        }

        return spell.School switch
        {
            MagicSchool.LifeMagic => spell.MetaSpellType is SpellType.Boost or SpellType.Enchantment,
            MagicSchool.CreatureEnchantment => spell.MetaSpellType == SpellType.Enchantment,
            _ => false
        };
    }

    /// <summary>
    /// Returns TRUE for an item buff that lands on a creature's held weapon or wand
    /// </summary>
    public static bool IsWeaponBuff(Spell spell) =>
        spell.School == MagicSchool.PortalMagic
        && spell.MetaSpellType == SpellType.Enchantment
        && spell.NonComponentTargetType
            is ItemType.Weapon
                or ItemType.Caster
                or ItemType.WeaponOrCaster
                or ItemType.MeleeWeapon
                or ItemType.MissileWeapon;

    /// <summary>
    /// Returns TRUE for an item enchantment cast on a creature rather than its weapon: the Aura spells (Aura of Blood Drinker
    /// Other, Aura of Heart Seeker Other...), which raise the damage or attack of whatever weapon it wields
    /// </summary>
    public static bool IsWeaponAura(Spell spell) =>
        spell.School == MagicSchool.PortalMagic
        && spell.MetaSpellType == SpellType.Enchantment
        && (spell.NonComponentTargetType & ItemType.Creature) != 0;

    /// <summary>
    /// The ally in range that needs a support spell most: the one lowest on the vital a boost restores, or the nearest
    /// without the buff (or with a weaker one). Null when no ally needs it.
    /// </summary>
    private Creature FindSupportTarget(Spell spell)
    {
        var range = GetSpellRange(spell);

        Creature best = null;
        var bestScore = float.MaxValue;

        foreach (var obj in PhysicsObj.ObjMaint.GetVisibleObjects(PhysicsObj.CurCell).Append(PhysicsObj))
        {
            if (obj.WeenieObj.WorldObject is not Creature ally || !IsSupportAlly(ally))
            {
                continue;
            }

            var dist = ally == this ? 0 : (float)GetDistance(ally);
            if (dist > range || !NeedsSupport(spell, ally))
            {
                continue;
            }

            var score = spell.MetaSpellType == SpellType.Boost ? GetSupportVitalShare(spell, ally) : dist;
            if (score < bestScore)
            {
                best = ally;
                bestScore = score;
            }
        }

        if (best != null && best != this && !IsDirectVisible(best))
        {
            return null;
        }

        return best;
    }

    /// <summary>
    /// Returns TRUE if a creature is on this monster's side: itself, or a living monster that isn't its foe by faction or
    /// creature type, and isn't fighting it
    /// </summary>
    public bool IsSupportAlly(Creature creature)
    {
        if (creature == this)
        {
            return IsAlive;
        }

        if (creature is Player or CombatPet || creature.IsDead || !creature.IsMonster || creature.Teleporting || creature.Visibility)
        {
            return false;
        }

        if (creature.InstanceId != InstanceId)
        {
            return false;
        }

        if (AllowFactionCombat(creature) || PotentialFoe(creature))
        {
            return false;
        }

        return creature.AttackTarget != this && AttackTarget != creature;
    }

    /// <summary>
    /// Returns TRUE if a support spell would do an ally any good: a boost when it's below SupportVitalThreshold of that
    /// vital, and a buff when it (or its weapon) has nothing as strong in that spell's category
    /// </summary>
    public static bool NeedsSupport(Spell spell, Creature ally)
    {
        if (spell.MetaSpellType == SpellType.Boost)
        {
            return GetSupportVitalShare(spell, ally) < SupportVitalThreshold;
        }

        WorldObject target = ally;

        // an aura only helps a creature that fights with a weapon
        if (IsWeaponAura(spell) && ally.GetEquippedWeapon() == null && ally.GetEquippedWand() == null)
        {
            return false;
        }

        if (IsWeaponBuff(spell))
        {
            target = GetWeaponSpellTarget(spell, ally);
            if (target == null || !target.IsEnchantable)
            {
                return false;
            }
        }

        return !target.EnchantmentManager.GetEnchantments(spell.Category).Any(e => e.PowerLevel >= spell.Power);
    }

    /// <summary>
    /// The share of the vital a boost restores that an ally has left, from 0 to 1
    /// </summary>
    private static float GetSupportVitalShare(Spell spell, Creature ally)
    {
        var vital = spell.VitalDamageType switch
        {
            DamageType.Stamina => ally.Stamina,
            DamageType.Mana => ally.Mana,
            _ => ally.Health
        };

        return vital.MaxValue == 0 ? 1.0f : (float)vital.Current / vital.MaxValue;
    }

    /// <summary>
    /// For a support spell in the spellbook with the base probability of 2.0: more likely the lower the ally's vital, the
    /// way a monster's own heals are. A buff with it is cast a third of the time.
    /// </summary>
    private static float GetSupportHealthCastChance(Spell spell, Creature ally)
    {
        if (spell.MetaSpellType != SpellType.Boost)
        {
            return 0.33f;
        }

        return MagicFormulas.GetSpellbookHealthCastChance(1.0f, GetSupportVitalShare(spell, ally));
    }

    /// <summary>
    /// Casts a support spell on the ally it was rolled for, if it's still alive and nearby
    /// </summary>
    private void CastSupportSpell(Spell spell, Creature ally)
    {
        if (ally.IsDead || ally.InstanceId != InstanceId || !IsInSameLandblockGroup(ally))
        {
            return;
        }

        if (IsWeaponBuff(spell))
        {
            CastWeaponSpellOnCreature(spell, ally);
            return;
        }

        if (spell.School == MagicSchool.LifeMagic)
        {
            HandleCastSpell(spell, ally, weapon: GetEquippedWand());
        }
        else
        {
            HandleCastSpell(spell, ally);
        }
    }
}
