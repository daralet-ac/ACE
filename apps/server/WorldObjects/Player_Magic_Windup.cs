using System;
using ACE.DatLoader;
using ACE.Entity.Enum;
using ACE.Server.Entity;
using ACE.Server.Entity.Actions;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.WorldObjects;

partial class Player
{
    private void DoWindup(WindupParams windupParams, bool checkAngle)
    {
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

    private void DoSpellWords(Spell spell, bool isWeaponSpell)
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

    private const float CastSpeed = 2.0f; // from retail pcaps, player animation speed for windup / first half of cast gesture

    private void DoWindupGestures(Spell spell, bool isWeaponSpell, ActionChain castChain)
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
                EnqueueMotionMagic(castChain, windupGesture, CastSpeed);
            }
        }

        if (FastTick)
        {
            EnqueueMotionAction(castChain, spell.Formula.WindupGestures, CastSpeed, MotionStance.Magic);
        }
    }

    private void DoCastGesture(Spell spell, WorldObject casterItem, ActionChain castChain)
    {
        MagicState.CastGesture = spell.Formula.CastGesture;

        if (casterItem != null)
        {
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

        if (FastTick)
        {
            EnqueueMotion(castChain, MagicState.CastGesture, CastSpeed, true, null, true);
        }
        else
        {
            EnqueueMotionMagic(castChain, MagicState.CastGesture, CastSpeed);
        }
    }

    /// <summary>
    /// Method used for handling player targeted spell casts
    /// </summary>
    /// <param name="casterItem">The casting item, when casting one of its built-in spells</param>
    private bool CreatePlayerSpell(
        WorldObject target,
        TargetCategory targetCategory,
        uint spellId,
        WorldObject casterItem
    )
    {
        var spell = ValidateSpell(spellId, casterItem != null);
        if (spell == null)
        {
            return false;
        }

        if (!VerifySpellTarget(spell, target))
        {
            return false;
        }

        var isWeaponSpell = casterItem != null && IsWeaponSpell(spell.Id, casterItem);

        var magicSkill = GetModdedMagicSkill(spell.School);

        // a built-in spell adds its item's spellcraft bonus
        if (isWeaponSpell && casterItem.ItemSpellcraft != null)
        {
            magicSkill += GetSpellcraftSkillBonus(casterItem.ItemSpellcraft.Value, this);
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

        return StartCastSequence(spell, casterItem, isWeaponSpell, magicSkill, target, castingPreCheckStatus);
    }

    /// <summary>
    /// Method used for handling player untargeted spell casts
    /// </summary>
    private bool CreatePlayerSpell(uint spellId)
    {
        var spell = ValidateSpell(spellId);
        if (spell == null)
        {
            return false;
        }

        var magicSkill = GetModdedMagicSkill(spell.School);

        // SPEC BONUS - War/Life Magic: verify advanced spell
        if (!VerifyAdvancedSpell(spell))
        {
            return false;
        }

        var castingPreCheckStatus = GetCastingPreCheckStatus(spell, magicSkill, false);

        return StartCastSequence(spell, null, false, magicSkill, null, castingPreCheckStatus);
    }

    /// <summary>
    /// Works out the spell's mana cost, then starts the spell words, windup gestures and cast gesture.
    /// The spell is released when the cast gesture completes (DoCastSpell).
    /// Returns FALSE if the player doesn't have the mana for it.
    /// </summary>
    private bool StartCastSequence(
        Spell spell,
        WorldObject casterItem,
        bool isWeaponSpell,
        uint magicSkill,
        WorldObject target,
        CastingPreCheckStatus castingPreCheckStatus
    )
    {
        if (!CalculateManaUsage(castingPreCheckStatus, spell, target, casterItem, out var manaUsed, out var manaRefund))
        {
            return false;
        }

        DoSpellWords(spell, isWeaponSpell);

        var spellChain = new ActionChain();

        // do wind-up gestures: fastcast has no windup (creature enchantments)
        DoWindupGestures(spell, isWeaponSpell, spellChain);

        DoCastGesture(spell, casterItem, spellChain);

        MagicState.SetCastParams(spell, casterItem, magicSkill, manaUsed, manaRefund, target, castingPreCheckStatus);

        // with FastTick, the spell is released when the cast gesture's motion is done (HandleMotionDone_Magic)
        if (!FastTick)
        {
            spellChain.AddAction(this, () => DoCastSpell());
        }

        spellChain.EnqueueChain();

        return true;
    }
}
