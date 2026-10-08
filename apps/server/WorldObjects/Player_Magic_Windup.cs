using System;
using System.Collections.Generic;
using System.Linq;
using ACE.Common;
using ACE.DatLoader;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Server.Entity;
using ACE.Server.Entity.Actions;
using ACE.Server.Managers;
using ACE.Server.Network.GameEvent.Events;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.WorldObjects;

partial class Player
{
    public void DoWindup(WindupParams windupParams, bool checkAngle)
    {
        //Console.WriteLine($"{Name}.DoWindup()");

        // ensure target still exists
        var targetCategory = GetTargetCategory(windupParams.TargetGuid, windupParams.SpellId, out var target);

        if (target == null)
        {
            SendUseDoneEvent(WeenieError.TargetNotAcquired);
            MagicState.OnCastDone();
            return;
        }

        if (!checkAngle || IsWithinAngle(target))
        {
            if (!CreatePlayerSpell(target, targetCategory, windupParams.SpellId, windupParams.CasterItem))
            {
                MagicState.OnCastDone();
            }
        }
        else
        {
            // restart turn if required
            if (PhysicsObj.MovementManager.MotionInterpreter.InterpretedState.TurnCommand == 0)
            {
                TurnTo_Magic(target);
            }
            else
            {
                MagicState.PendingTurnRelease = true;
            }
        }
    }

    public void DoSpellWords(Spell spell, bool isWeaponSpell)
    {
        spell.Formula.GetPlayerFormula(this);

        var spellWords = spell._spellBase.GetSpellWords(DatManager.PortalDat.SpellComponentsTable);
        if (!string.IsNullOrWhiteSpace(spellWords) && !isWeaponSpell)
        {
            EnqueueBroadcast(
                new GameMessageHearSpeech(spellWords, GetNameWithSuffix(), Guid.Full, ChatMessageType.Spellcasting),
                LocalBroadcastRange
            );
        }
    }

    public static float CastSpeed = 2.0f; // from retail pcaps, player animation speed for windup / first half of cast gesture

    public void DoWindupGestures(Spell spell, bool isWeaponSpell, ActionChain castChain)
    {
        if (spell.Flags.HasFlag(SpellFlags.FastCast) || isWeaponSpell)
        {
            return;
        }

        if (FastTick)
        {
            castChain.AddAction(
                this,
                () =>
                {
                    PhysicsObj.StopCompletely(false);

                    MagicState.TurnStarted = false;
                    MagicState.IsTurning = false;
                }
            );
        }

        var windupTime = 0.0f;

        foreach (var windupGesture in spell.Formula.WindupGestures)
        {
            if (RecordCast.Enabled)
            {
                castChain.AddAction(
                    this,
                    () =>
                    {
                        var animLength = Physics.Animation.MotionTable.GetAnimationLength(
                            MotionTableId,
                            CurrentMotionState.Stance,
                            windupGesture,
                            CastSpeed
                        );
                        RecordCast.Log($"Windup Gesture: {windupGesture}, Windup Time: {animLength}");
                    }
                );
            }

            // don't mess with CurrentMotionState here?
            if (!FastTick)
            {
                windupTime = EnqueueMotionMagic(castChain, windupGesture, CastSpeed);
            }

            /*Console.WriteLine($"{spell.Name}");
            Console.WriteLine($"Windup Gesture: " + windupGesture);
            Console.WriteLine($"Windup time: " + windupTime);
            Console.WriteLine("-------");*/
        }

        if (FastTick)
        {
            windupTime = EnqueueMotionAction(castChain, spell.Formula.WindupGestures, CastSpeed, MotionStance.Magic);
        }
    }

    public void DoCastGesture(Spell spell, WorldObject casterItem, ActionChain castChain)
    {
        MagicState.CastGesture = spell.Formula.CastGesture;

        if (casterItem != null)
        {
            //var caster = GetEquippedWand();
            if (casterItem.UseUserAnimation != 0)
            {
                MagicState.CastGesture = casterItem.UseUserAnimation;
            }
        }

        if (RecordCast.Enabled)
        {
            castChain.AddAction(
                this,
                () =>
                {
                    var animLength = Physics.Animation.MotionTable.GetAnimationLength(
                        MotionTableId,
                        CurrentMotionState.Stance,
                        MagicState.CastGesture,
                        CastSpeed
                    );
                    RecordCast.Log($"Cast Gesture: {MagicState.CastGesture}, Cast Time: {animLength}");
                }
            );
        }

        castChain.AddAction(
            this,
            () =>
            {
                if (!MagicState.IsCasting)
                {
                    return;
                }

                MagicState.CastGestureStartTime = DateTime.UtcNow;

                if (FastTick)
                {
                    PhysicsObj.StopCompletely(false);
                }
            }
        );

        if (MagicState.CastGesture == MotionCommand.Invalid)
        {
            MagicState.CastGesture = MotionCommand.Ready;
        }

        var castTime = 0.0f;
        if (FastTick)
        {
            castTime = EnqueueMotion(castChain, MagicState.CastGesture, CastSpeed, true, null, true);
        }
        else
        {
            castTime = EnqueueMotionMagic(castChain, MagicState.CastGesture, CastSpeed);
        }

        //Console.WriteLine($"Cast Gesture: " + MagicState.CastGesture);
        //Console.WriteLine($"Cast time: " + castTime);
    }

    /// <summary>
    /// Method used for handling player targeted spell casts
    /// </summary>
    /// <param name="builtInSpell">If TRUE, casting a built-in spell from a weapon</param>
    public bool CreatePlayerSpell(
        WorldObject target,
        TargetCategory targetCategory,
        uint spellId,
        WorldObject casterItem,
        bool sigilTrinketSpell = false
    )
    {
        var creatureTarget = target as Creature;

        var spell = ValidateSpell(spellId, casterItem != null);
        if (spell == null)
        {
            return false;
        }

        if (!VerifySpellTarget(spell, target))
        {
            return false;
        }

        // if casting implement has spell built in,
        // use spellcraft from the item, instead of player's magic skill?
        var caster = casterItem ?? GetEquippedWand();
        var isWeaponSpell = casterItem != null && IsWeaponSpell(spell.Id, casterItem);

        // Grab player's skill level in the spell's Magic School
        uint magicSkill;
        if (spell.School == MagicSchool.WarMagic)
        {
            magicSkill = GetModdedWarMagicSkill();
        }
        else if (spell.School is MagicSchool.LifeMagic or MagicSchool.VoidMagic)
        {
            magicSkill = GetModdedLifeMagicSkill();
        }
        else
        {
            magicSkill = GetCreatureSkill(spell.School).Current;
        }

        if (isWeaponSpell && caster.ItemSpellcraft != null)
        {
            var spellcraft = caster.ItemSpellcraft.Value + CheckForArcaneLoreSpecSpellcraftBonus(this);
            magicSkill += (uint)(spellcraft * 0.1);
        }

        // SPEC BONUS - War/Life Magic: verify advanced spell
        if (!VerifyAdvancedSpell(spell))
        {
            return false;
        }

        // verify spell range
        if (!VerifySpellRange(target, targetCategory, spell, casterItem, magicSkill))
        {
            return false;
        }

        // get casting pre-check status
        var castingPreCheckStatus = GetCastingPreCheckStatus(spell, magicSkill, isWeaponSpell);

        // calculate mana usage
        if (!CalculateManaUsage(castingPreCheckStatus, spell, target, casterItem, out var manaUsed, out var manaRefund))
        {
            return false;
        }

        // spell words
        DoSpellWords(spell, isWeaponSpell);

        var spellChain = new ActionChain();
        //StartPos = new Physics.Common.Position(PhysicsObj.Position);

        // do wind-up gestures: fastcast has no windup (creature enchantments)
        DoWindupGestures(spell, isWeaponSpell, spellChain);

        // cast spell
        DoCastGesture(spell, casterItem, spellChain);

        MagicState.SetCastParams(spell, casterItem, magicSkill, manaUsed, manaRefund, target, castingPreCheckStatus);

        if (!FastTick)
        {
            spellChain.AddAction(this, () => DoCastSpell(MagicState, true, sigilTrinketSpell));
        }

        spellChain.EnqueueChain();

        return true;
    }

    /// <summary>
    /// Method used for handling player untargeted spell casts
    /// </summary>
    public bool CreatePlayerSpell(uint spellId, bool sigilTrinketSpell = false)
    {
        var spell = ValidateSpell(spellId);
        if (spell == null)
        {
            return false;
        }

        // get player's current magic skill
        var magicSkill = GetCreatureSkill(spell.School).Current;

        var castingPreCheckStatus = GetCastingPreCheckStatus(spell, magicSkill, false);

        // calculate mana usage
        if (!CalculateManaUsage(castingPreCheckStatus, spell, null, null, out var manaUsed, out var manaRefund))
        {
            return false;
        }

        // begin spellcasting
        DoSpellWords(spell, false);

        var spellChain = new ActionChain();

        //StartPos = new Physics.Common.Position(PhysicsObj.Position);

        // do wind-up gestures: fastcast has no windup (creature enchantments)
        DoWindupGestures(spell, false, spellChain);

        // do cast gesture
        DoCastGesture(spell, null, spellChain);

        // cast untargeted spell
        MagicState.SetCastParams(spell, null, magicSkill, manaUsed, manaRefund, null, castingPreCheckStatus);

        if (!FastTick)
        {
            spellChain.AddAction(this, () => DoCastSpell(MagicState, true, sigilTrinketSpell));
        }

        spellChain.EnqueueChain();

        return true;
    }
}
