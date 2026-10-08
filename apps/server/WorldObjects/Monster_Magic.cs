using System;
using ACE.Common;
using ACE.DatLoader;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Entity.Models;
using ACE.Server.Entity;
using ACE.Server.Entity.Actions;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.Physics.Animation;

namespace ACE.Server.WorldObjects;

/// <summary>
/// Monster casting for magic spells
/// </summary>
partial class Creature
{
    private bool AiUsesMana
    {
        get => GetProperty(PropertyBool.AiUsesMana) ?? true; // default true?
        set
        {
            if (!value)
            {
                RemoveProperty(PropertyBool.AiUsesMana);
            }
            else
            {
                SetProperty(PropertyBool.AiUsesMana, value);
            }
        }
    }

    /// <summary>
    /// Very specific monsters will be set to use the human casting animations,
    /// ie. the windup and casting gestures from the spell
    /// </summary>
    private bool AiUseHumanMagicAnimations
    {
        get => GetProperty(PropertyBool.AiUseHumanMagicAnimations) ?? false;
        set
        {
            if (!value)
            {
                RemoveProperty(PropertyBool.AiUseHumanMagicAnimations);
            }
            else
            {
                SetProperty(PropertyBool.AiUseHumanMagicAnimations, value);
            }
        }
    }

    /// <summary>
    /// The amount of time a monster waits to cast a magic spell
    /// defined as seconds from the start of the previous attack
    /// the most common value in the db is 3s
    /// some other common values include 2s and 1s, with some mobs having values up to 1m
    /// </summary>
    private double? AiUseMagicDelay
    {
        get => GetProperty(PropertyFloat.AiUseMagicDelay);
        set
        {
            if (!value.HasValue)
            {
                RemoveProperty(PropertyFloat.AiUseMagicDelay);
            }
            else
            {
                SetProperty(PropertyFloat.AiUseMagicDelay, value.Value);
            }
        }
    }

    /// <summary>
    /// Returns TRUE if monster has known spells
    /// </summary>
    private bool HasKnownSpells => Biota.HasKnownSpell(BiotaDatabaseLock);

    /// <summary>
    /// The next spell the monster will attempt to cast
    /// </summary>
    private Spell CurrentSpell { get; set; }

    private bool TryRollSpell()
    {
        CurrentSpell = null;

        // monster spellbooks have probabilities with base 2.0
        // ie. a 5% chance would be 2.05 instead of 0.05

        // much less common, some monsters will have spells with just base 2.0 probability
        // there were probably other criteria used to select these spells (emote responses, monster ai responses)
        // for now, 2.0 base just becomes a 2% chance

        if (Biota.PropertiesSpellBook == null)
        {
            return false;
        }

        // We don't use thread safety here. Monster spell books aren't mutated cross-threads.
        // This reduces memory consumption by not cloning the spell book every single TryRollSpell()
        foreach (var spell in Biota.PropertiesSpellBook) // Not thread-safe
        {
            var probability = MagicFormulas.GetSpellbookCastChance(spell.Value);

            var rng = ThreadSafeRandom.Next(0.0f, 1.0f);

            // Simplified Ai-Acquire Health - place heals/drains at top of Spellbook with probability of 2, as health decreases prob of casting increases.

            if (spell.Value == 2.0f)
            {
                probability = MagicFormulas.GetSpellbookHealthCastChance(Health.MaxValue, Health.Current);
            }

            if (rng < probability)
            {
                CurrentSpell = new Spell(spell.Key);
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Returns the maximum range for the current spell
    /// </summary>
    private float GetSpellMaxRange()
    {
        var skill = GetMagicSkillForRangeCheck();

        var maxRange = CurrentSpell.GetMaxCastRange(skill);

        if (maxRange == 0.0f)
        {
            maxRange = float.PositiveInfinity;
        }

        return maxRange;
    }

    private bool IsSelfCast()
    {
        if (CurrentAttack != CombatType.Magic)
        {
            return false;
        }

        return GetSpellMaxRange() == float.PositiveInfinity;
    }

    /// <summary>
    /// Performs the monster windup spell animation,
    /// casts the spell, and returns to attack stance
    /// </summary>
    private void MagicAttack()
    {
        if (AttackTarget is not Creature { IsAlive: true } target)
        {
            FindNextTarget(false);
            return;
        }

        if (target is Player { VanishIsActive: true } player)
        {
            FindNextTarget(false, player);
            return;
        }

        var spell = CurrentSpell;

        // turn to?
        if (AiUsesMana && !UseMana())
        {
            return;
        }

        // spell words
        if (AiUseHumanMagicAnimations)
        {
            var spellWords = spell._spellBase.GetSpellWords(DatManager.PortalDat.SpellComponentsTable);
            if (!string.IsNullOrWhiteSpace(spellWords))
            {
                EnqueueBroadcast(
                    new GameMessageHearSpeech(spellWords, Name, Guid.Full, ChatMessageType.Spellcasting),
                    LocalBroadcastRange
                );
            }
        }

        var preCastTime = PreCastMotion();

        var actionChain = new ActionChain();
        actionChain.AddDelaySeconds(preCastTime);
        actionChain.AddAction(
            this,
            () =>
            {
                // the target may have left the instance during the windup
                if (IsDead || AttackTarget == null || target.IsDead || target.InstanceId != InstanceId)
                {
                    return;
                }

                CastSpell(spell);

                PostCastMotion();
            }
        );
        actionChain.EnqueueChain();

        var postCastTime = GetPostCastTime(spell);

        // slight variation here
        PrevAttackTime = Timers.RunningTime + preCastTime;
        var powerupTime = (float)(PowerupTime ?? 1.0f);

        var postDelay = ThreadSafeRandom.Next(0.0f, powerupTime);

        NextMoveTime = NextAttackTime = PrevAttackTime + postCastTime + postDelay;
    }

    private bool UseMana()
    {
        // do any monsters have mana conversion?
        var target = GetSpellMaxRange() < float.PositiveInfinity ? AttackTarget : this;

        var manaUsed = CalculateManaUsage(CurrentSpell, target, out var manaRefund);

        if (manaUsed > Mana.Current)
        {
            return false;
        }

        Mana.Current -= manaUsed;
        ApplyManaCastRefund(manaRefund);
        return true;
    }

    private const float PreCastSpeed = 2.0f;
    private const float PostCastSpeed = 1.0f;
    private const float PostCastSpeed_Ranged = 1.66f; // ??

    /// <summary>
    /// Perform the first part of monster spell casting animation - spreading arms out
    /// </summary>
    public float PreCastMotion(bool fallback = false)
    {
        if (AiUseHumanMagicAnimations && !fallback)
        {
            return PreCastMotion_Human();
        }

        var motion = new ACE.Server.Entity.Motion(this, MotionCommand.CastSpell, PreCastSpeed);
        motion.MotionState.TurnSpeed = 2.25f;
        CurrentMotionState = motion;

        EnqueueBroadcastMotion(motion);

        return MotionTable.GetAnimationLength(
            MotionTableId,
            CurrentMotionState.Stance,
            MotionCommand.CastSpell,
            PreCastSpeed
        );
    }

    /// <summary>
    /// For monsters with AiUseHumanMagicAnimations = true,
    /// performs the windup gestures from the spell scarabs
    ///
    /// <returns>The amount of time for the windup gestures to complete</returns>
    private float PreCastMotion_Human()
    {
        // todo: play each motion at the proper time,
        // ensuring the monster is still alive at each step
        CurrentSpell.Formula.GetMonsterFormula();

        // FIXME: data
        var castAnimTime = MotionTable.GetAnimationLength(
            MotionTableId,
            CurrentMotionState.Stance,
            CurrentSpell.Formula.CastGesture,
            PreCastSpeed
        );

        if (castAnimTime == 0)
        {
            return PreCastMotion(true);
        }

        var animTime = 0.0f;

        foreach (var windupGesture in CurrentSpell.Formula.WindupGestures)
        {
            var motion = new ACE.Server.Entity.Motion(this, windupGesture, PreCastSpeed);
            motion.MotionState.TurnSpeed = 2.25f;
            CurrentMotionState = motion;

            EnqueueBroadcastMotion(motion);

            animTime += MotionTable.GetAnimationLength(
                MotionTableId,
                CurrentMotionState.Stance,
                windupGesture,
                PreCastSpeed
            );
        }

        var castMotion = new ACE.Server.Entity.Motion(this, CurrentSpell.Formula.CastGesture, PreCastSpeed);
        castMotion.MotionState.TurnSpeed = 2.25f;
        CurrentMotionState = castMotion;

        EnqueueBroadcastMotion(castMotion);

        animTime += castAnimTime;

        return animTime;
    }

    /// <summary>
    /// Casts the current monster spell on target
    /// </summary>
    public void CastSpell(Spell spell)
    {
        if (AttackTarget == null)
        {
            return;
        }

        var target = GetMonsterSpellTarget(spell);

        var caster = GetEquippedWand();

        // handle self procs
        if (spell.IsHarmful && target != this)
        {
            TryProcEquippedItems(this, this, true, caster);
        }

        // If the target is too far away, don't cast. This checks to see of this monster and the target are on separate landblock groups, and potentially separate threads.
        // This also fixes cross-threading issues
        if (target != null && !IsInSameLandblockGroup(target))
        {
            return;
        }

        // try to resist spell, if applicable
        if (TryResistSpell(target, spell, out _))
        {
            TryHandleFactionMob(target);
            return;
        }

        // TODO: see if this can be coalesced
        switch (spell.School)
        {
            case MagicSchool.CreatureEnchantment:

                HandleCastSpell(spell, target);

                TryProcOnSpellTarget(spell, target, caster);
                break;

            case MagicSchool.PortalMagic:

                TryCastItemEnchantment_WithRedirects(spell, target);
                break;

            case MagicSchool.LifeMagic:

                HandleCastSpell(spell, target, weapon: caster);

                if (spell.MetaSpellType != SpellType.LifeProjectile)
                {
                    TryHandleFactionMob(target);

                    TryProcOnSpellTarget(spell, target, caster);
                }
                break;

            case MagicSchool.WarMagic:
            case MagicSchool.VoidMagic:

                HandleCastSpell(spell, target, weapon: caster);
                break;
        }
    }

    /// <summary>
    /// Untargeted spells have no target, self-targeted spells target the monster, the rest target its attack target
    /// </summary>
    private WorldObject GetMonsterSpellTarget(Spell spell)
    {
        if (spell.NonComponentTargetType == ItemType.None)
        {
            return null;
        }

        if (spell.Flags.HasFlag(SpellFlags.SelfTargeted))
        {
            return this;
        }

        return AttackTarget;
    }

    private bool IsInSameLandblockGroup(WorldObject target)
    {
        return CurrentLandblock != null
            && target.CurrentLandblock != null
            && CurrentLandblock.CurrentLandblockGroup == target.CurrentLandblock.CurrentLandblockGroup;
    }

    /// <summary>
    /// A harmful spell procs the monster's equipped items on its target
    /// </summary>
    private void TryProcOnSpellTarget(Spell spell, WorldObject target, WorldObject caster)
    {
        // handle target procs
        if (spell.IsHarmful && target is Creature targetCreature && targetCreature != this)
        {
            TryProcEquippedItems(this, targetCreature, false, caster);
        }
    }

    /// <summary>
    /// Perform the animations after casting a spell,
    /// ie. moving arms back in, returning to previous stance
    /// </summary>
    public void PostCastMotion()
    {
        var animSpeed = IsRanged ? PostCastSpeed_Ranged : PostCastSpeed;

        var motion = new ACE.Server.Entity.Motion(this, MotionCommand.Ready, animSpeed);
        motion.MotionState.TurnSpeed = 2.25f;
        CurrentMotionState = motion;

        EnqueueBroadcastMotion(motion);
    }

    public float GetPostCastTime(Spell spell, bool fallback = false)
    {
        if (AiUseHumanMagicAnimations && !fallback)
        {
            return GetPostCastTime_Human(spell);
        }

        var animSpeed = IsRanged ? PostCastSpeed_Ranged : PostCastSpeed;

        return MotionTable.GetAnimationLength(
            MotionTableId,
            CurrentMotionState.Stance,
            MotionCommand.CastSpell,
            MotionCommand.Ready,
            animSpeed
        );
    }

    private float GetPostCastTime_Human(Spell spell)
    {
        var animSpeed = IsRanged ? PostCastSpeed_Ranged : PostCastSpeed;

        var animTime = MotionTable.GetAnimationLength(
            MotionTableId,
            CurrentMotionState.Stance,
            spell.Formula.CastGesture,
            MotionCommand.Ready,
            animSpeed
        );

        // FIXME: data
        if (animTime == 0.0f)
        {
            return GetPostCastTime(spell, true);
        }

        return animTime;
    }

    /// <summary>
    /// Returns the magic skill level used for spell range checks.
    /// (initial points + points due to directly raising the skill)
    /// </summary>
    /// <returns></returns>
    private uint GetMagicSkillForRangeCheck()
    {
        var skill = GetCreatureSkill(CurrentSpell.School);

        // verify this - should it be using base?
        // seems like it could be off, player formula uses current + cap?

        return skill.InitLevel + skill.Ranks;
    }

    public void TryHandleFactionMob(WorldObject target)
    {
        if (target == this || target is Player)
        {
            return;
        }

        var creatureTarget = target as Creature;

        if (creatureTarget == null || !AllowFactionCombat(creatureTarget) && !PotentialFoe(creatureTarget))
        {
            return;
        }

        MonsterOnAttackMonster(creatureTarget);
    }

    /// <summary>
    /// Checks for AiUseHumanMagicAnimations and if true, sets CurrentSpell and sets combat mode to Magic
    /// </summary>
    public void CheckForHumanPreCast(Spell spell)
    {
        if (AiUseHumanMagicAnimations)
        {
            CurrentSpell = new Spell(spell.Id);
            SetCombatMode(CombatMode.Magic);
        }
    }
}
