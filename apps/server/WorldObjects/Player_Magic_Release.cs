using System;
using System.Collections.Generic;
using System.Linq;
using ACE.Common;
using ACE.Entity.Enum;
using ACE.Server.Entity;
using ACE.Server.Entity.Actions;
using ACE.Server.Managers;
using ACE.Server.Network.GameEvent.Events;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.WorldObjects;

partial class Player
{
    private void DoCastSpell(bool checkAngle = true)
    {
        if (!MagicState.IsCasting)
        {
            return;
        }

        var cast = MagicState.CastSpellParams;

        if (cast == null)
        {
            _log.Warning($"{Name}.DoCastSpell(): null state detected");

            // send UseDone?
            SendUseDoneEvent(WeenieError.BadCast);

            return;
        }

        DoCastSpell(cast, checkAngle);
    }

    /// <summary>
    /// Releases the spell when the cast gesture is done, after checking the target still exists,
    /// turning to face it if needed, and checking it's still in range
    /// </summary>
    private void DoCastSpell(CastSpellParams cast, bool checkAngle)
    {
        var spell = cast.Spell;
        var target = cast.Target;

        if (target != null)
        {
            // verify target still exists
            var targetCategory = GetTargetCategory(target.Guid.Full, spell.Id, out target);

            if (target == null)
            {
                SendWeenieError(WeenieError.TargetNotAcquired);
                FinishCast();
                return;
            }

            // do second rotate, if applicable
            // TODO: investigate this more, difference for GetAngle() between ACE and ac physics engine
            if (checkAngle && !IsWithinAngle(target))
            {
                if (!FastTick)
                {
                    var rotateTime = Rotate(target);

                    var actionChain = new ActionChain();
                    actionChain.AddDelaySeconds(rotateTime);
                    actionChain.AddAction(this, () => DoCastSpell(cast, false));
                    actionChain.EnqueueChain();
                }
                else
                {
                    if (PhysicsObj.MovementManager.MotionInterpreter.InterpretedState.TurnCommand == 0)
                    {
                        TurnTo_Magic(target);
                    }
                    else
                    {
                        MagicState.PendingTurnRelease = true;
                    }
                }

                return;
            }

            // verify spell range
            if (!VerifySpellRange(target, targetCategory, spell, cast.CasterItem, cast.MagicSkill))
            {
                FinishCast();
                return;
            }
        }

        if (IsDead)
        {
            FinishCast();
            return;
        }

        DoCastSpell_Inner(cast, target, cast.Status);
    }

    /// <summary>
    /// Releases the spell: spends its mana and components, then casts it, or fizzles it
    /// </summary>
    /// <param name="target">The cast's target, looked up again when the spell is released</param>
    /// <param name="castingPreCheckStatus">The cast's status, or CastFailed for a fizzle</param>
    private void DoCastSpell_Inner(
        CastSpellParams cast,
        WorldObject target,
        CastingPreCheckStatus castingPreCheckStatus,
        bool finishCast = true
    )
    {
        var spell = cast.Spell;

        if (RecordCast.Enabled)
        {
            RecordCast.Log($"DoCastSpell_Inner()");
        }

        if (MagicState.CastMeter)
        {
            ShowCastEfficiency();
        }

        var caster = cast.CasterItem ?? GetEquippedWand(); // TODO: persist this from the beginning, since this is done with delay

        var isWeaponSpell = cast.CasterItem != null;

        var itemCaster = isWeaponSpell ? caster : null;

        SpendCastMana(cast, itemCaster);

        // consume spell components
        if (!isWeaponSpell)
        {
            TryBurnComponents(spell);
        }

        if (HasMovedTooFarToCast())
        {
            Session.Network.EnqueueSend(
                new GameMessageSystemChat("Your movement disrupted spell casting!", ChatMessageType.Magic)
            );

            EnqueueBroadcast(new GameMessageScript(Guid, PlayScript.Fizzle, 0.5f));

            if (finishCast)
            {
                FinishCast();
            }

            return;
        }

        var pk_error = CheckPKStatusVsTarget(target, spell);
        if (pk_error != null)
        {
            castingPreCheckStatus = CastingPreCheckStatus.InvalidPKStatus;
        }

        if (IsStealthed)
        {
            EndStealth(null, true);
        }

        var spellReleased = ReleaseSpell(spell, target, castingPreCheckStatus, caster, itemCaster, isWeaponSpell);

        if (pk_error != null && spell.NumProjectiles == 0)
        {
            SendPkCastErrors(target, pk_error);
        }

        if (finishCast)
        {
            FinishCast();
        }

        if (spellReleased)
        {
            CheckForCombatAbilityOverloadBacklash(spell);
        }
    }

    /// <summary>
    /// Debug: how far through the cast gesture the spell was released
    /// </summary>
    private void ShowCastEfficiency()
    {
        var gestureTime = Physics.Animation.MotionTable.GetAnimationLength(
            MotionTableId,
            CurrentMotionState.Stance,
            MagicState.CastGesture,
            CastSpeed
        );
        var castTime = DateTime.UtcNow - MagicState.CastGestureStartTime;
        var efficiency = 1.0f - (float)castTime.TotalSeconds / gestureTime;
        var msg = $"Cast efficiency: {efficiency * 100}%";
        Session.Network.EnqueueSend(new GameMessageSystemChat(msg, ChatMessageType.Broadcast));
    }

    /// <summary>
    /// Spends the spell's mana, from the player or from the casting item for a built-in spell,
    /// and grants the health/stamina refund for it
    /// </summary>
    private void SpendCastMana(CastSpellParams cast, WorldObject itemCaster)
    {
        var manaUsed = ApplySigilScarabManaReduction(cast.Spell, cast.ManaUsed);

        if (itemCaster == null)
        {
            UpdateVitalDelta(Mana, -(int)manaUsed);
        }
        else
        {
            itemCaster.ItemCurMana -= (int)manaUsed;
        }

        ApplyManaCastRefund(cast.ManaRefund);
    }

    /// <summary>
    /// SIGIL SCARAB - Mana Cost Reduction
    /// </summary>
    private uint ApplySigilScarabManaReduction(Spell spell, uint manaUsed)
    {
        var manaModifier =
            spell.School == MagicSchool.LifeMagic
                ? GetSigilTrinketManaReductionMod(spell, Skill.LifeMagic, SigilTrinketLifeWarMagicEffect.Reduction)
                : GetSigilTrinketManaReductionMod(spell, Skill.WarMagic, SigilTrinketLifeWarMagicEffect.Reduction);

        var before = manaUsed;
        manaUsed = (uint)(manaUsed * manaModifier);

        if (DebugSpellcasting)
        {
            var scarabMsg = $"[ManaDbg]  scarab factor {manaModifier:F3}: manaUsed {before} -> {manaUsed}";
            _log.Information(scarabMsg);
            Session.Network.EnqueueSend(new GameMessageSystemChat(scarabMsg, ChatMessageType.Magic));
        }

        if (manaModifier < 1.0f)
        {
            Session.Network.EnqueueSend(
                new GameMessageSystemChat(
                    $"Sigil Scarab of Reduction reduced the spell's cost by {Math.Round((1.0f - manaModifier) * 100, 0)}%, from {before} to {manaUsed}!  ",
                    ChatMessageType.Magic
                )
            );
        }

        return manaUsed;
    }

    /// <summary>
    /// A PK moving too far during the windup disrupts the cast
    /// </summary>
    private bool HasMovedTooFarToCast()
    {
        // check windup move distance cap
        var dist = StartPos.Distance(PhysicsObj.Position);

        // only PKs affected by these caps?
        return dist > Windup_MaxMove && PlayerKillerStatus != PlayerKillerStatus.NPK;
    }

    /// <summary>
    /// Casts the spell on its target (or fellows), or fizzles it. Returns TRUE if the spell was cast.
    /// </summary>
    private bool ReleaseSpell(
        Spell spell,
        WorldObject target,
        CastingPreCheckStatus castingPreCheckStatus,
        WorldObject caster,
        WorldObject itemCaster,
        bool isWeaponSpell
    )
    {
        switch (castingPreCheckStatus)
        {
            case CastingPreCheckStatus.Success:

                if (!spell.IsFellowshipSpell)
                {
                    CastPlayerSpellOn(target, spell, isWeaponSpell);
                }
                else
                {
                    CastOnFellows(spell, isWeaponSpell);
                }

                // handle self procs
                if (spell.IsHarmful && target != this)
                {
                    TryProcEquippedItems(this, this, true, caster);
                }

                return true;

            case CastingPreCheckStatus.InvalidPKStatus:

                // projectiles still launch at a target the caster can't attack (they can't damage it)
                if (spell.NumProjectiles <= 0)
                {
                    return false;
                }

                HandleCastSpell(spell, target, itemCaster, weapon: caster, isWeaponSpell);
                return true;

            default:
                EnqueueBroadcast(new GameMessageScript(Guid, PlayScript.Fizzle, 0.5f));
                SendWeenieError(WeenieError.YourSpellFizzled);

                SendRestrictedCasterFizzleMessages(caster, spell);

                return false;
        }
    }

    /// <summary>
    /// Casts a fellowship spell on the caster's fellows in range
    /// </summary>
    private void CastOnFellows(Spell spell, bool isWeaponSpell)
    {
        var fellows = GetFellowshipTargets();
        foreach (var fellow in fellows)
        {
            // Fellowship spells do not affect the caster
            if (fellow == this)
            {
                continue;
            }

            // Fellowship spells only affect targets in range
            var magicSkill = GetCreatureSkill(spell.School).Current;
            var maxRange = spell.GetMaxCastRange(magicSkill);

            if (GetDistance(fellow) > maxRange)
            {
                continue;
            }

            CastPlayerSpellOn(fellow, spell, isWeaponSpell);
        }
    }

    /// <summary>
    /// Tells the caster, and a target player, that the caster can't cast on the target with their PK statuses
    /// </summary>
    private void SendPkCastErrors(WorldObject target, List<WeenieErrorWithString> pk_error)
    {
        Session.Network.EnqueueSend(new GameEventWeenieErrorWithString(Session, pk_error[0], target.Name));

        if (target is Player targetPlayer)
        {
            targetPlayer.Session.Network.EnqueueSend(
                new GameEventWeenieErrorWithString(targetPlayer.Session, pk_error[1], Name)
            );
        }
    }

    /// <summary>
    /// After a fizzle, explains it when the caster is an item restricted to one school (or out of mana)
    /// </summary>
    private void SendRestrictedCasterFizzleMessages(WorldObject caster, Spell spell)
    {
        if (GetRestrictedSchool(caster) is not { } restrictedSchool)
        {
            return;
        }

        if (
            spell.School != restrictedSchool
            && spell.School is MagicSchool.WarMagic or MagicSchool.LifeMagic or MagicSchool.PortalMagic
        )
        {
            var schoolName = restrictedSchool switch
            {
                MagicSchool.WarMagic => "War Magic",
                MagicSchool.LifeMagic => "Life Magic",
                _ => "Portal Magic",
            };

            SendMessage($"{caster.Name} can only cast {schoolName} spells.");
        }

        if (caster.ItemCurMana == 0 && spell.School == restrictedSchool)
        {
            SendMessage($"{caster.Name} cannot cast spells while it is out of mana.");
        }
    }

    /// <summary>
    /// COMBAT ABILITY - Overload: a released spell has a chance to burn the caster, scaling with the charge level.
    /// </summary>
    private void CheckForCombatAbilityOverloadBacklash(Spell spell)
    {
        if (!OverloadStanceIsActive && !OverloadDischargeIsActive)
        {
            return;
        }

        var meter = OverloadStanceIsActive ? ManaChargeMeter : DischargeLevel;
        var chance = MagicFormulas.GetOverloadBacklashChance(meter);

        if (ThreadSafeRandom.Next(0.0f, 1.0f) >= chance)
        {
            return;
        }

        var selfDamage = MagicFormulas.GetOverloadBacklashDamage(meter, spell.BaseMana);

        if (selfDamage <= 0)
        {
            return;
        }

        UpdateVitalDelta(Health, -selfDamage);
        DamageHistory.Add(this, DamageType.Health, (uint)selfDamage);

        Session.Network.EnqueueSend(
            new GameMessageSystemChat(
                $"Overload! The unstable mana in your spell burns you for {selfDamage} damage!",
                ChatMessageType.CombatEnemy
            )
        );

        if (!IsDead)
        {
            return;
        }

        OnDeath(DamageHistory.LastDamager, DamageType.Health);
        Die();
    }

    private void FinishCast()
    {
        var hasWindupGestures = MagicState.CastSpellParams?.HasWindupGestures ?? true;
        var castGesture = MagicState.CastGesture;

        if (FastTick)
        {
            castGesture = hasWindupGestures ? CurrentMotionState.MotionState.ForwardCommand : MagicState.CastGesture;
        }

        var selfTarget = !hasWindupGestures && MagicState.CastSpellParams.Target == this;

        MagicState.OnCastDone();

        IsBusy = true;

        var queue = PropertyManager.GetBool("spellcast_recoil_queue").Item;

        if (queue)
        {
            MagicState.CanQueue = true;
        }

        if (FastTick)
        {
            var fastbuff = selfTarget && PropertyManager.GetBool("fastbuff").Item;

            // return to magic ready stance
            var actionChain = new ActionChain();
            EnqueueMotion(actionChain, MotionCommand.Ready, 1.0f, true, castGesture, false, fastbuff);
            actionChain.AddAction(
                this,
                () =>
                {
                    IsBusy = false;
                    SendUseDoneEvent();

                    if (queue)
                    {
                        HandleCastQueue();
                    }
                }
            );
            actionChain.EnqueueChain();
        }
        else
        {
            // temporarily old version:

            // return to magic combat stance
            var returnStance = new Motion(MotionStance.Magic, MotionCommand.Ready, 1.0f);
            EnqueueBroadcastMotion(returnStance);

            var actionChain = new ActionChain();
            actionChain.AddDelaySeconds(1.0f); // TODO: get actual recoil timing
            actionChain.AddAction(
                this,
                () =>
                {
                    IsBusy = false;
                    SendUseDoneEvent();

                    if (queue)
                    {
                        HandleCastQueue();
                    }
                }
            );
            actionChain.EnqueueChain();
        }
    }

    private List<Player> GetFellowshipTargets()
    {
        if (Fellowship != null)
        {
            return Fellowship.GetFellowshipMembers().Values.ToList();
        }
        else
        {
            return new List<Player>() { this };
        }
    }

    /// <summary>
    /// Casts a released spell on one target
    /// </summary>
    private void CastPlayerSpellOn(WorldObject target, Spell spell, bool isWeaponSpell, bool sigilTrinketSpell = false)
    {
        LastSuccessCast_Time = Time.GetUnixTime();

        ApplyCastAftereffectSpells(spell);

        var caster = GetEquippedWand();

        var itemCaster = isWeaponSpell ? caster : null;

        // verify after windup, still consumes mana
        if (spell.MetaSpellType == SpellType.Dispel && !VerifyDispelPkStatus(this, target))
        {
            return;
        }

        if (spell.School == MagicSchool.PortalMagic)
        {
            CastPortalItemSpell(spell, target, itemCaster);
            return;
        }

        var partialEvasion = PartialEvasion.None;

        if (!spell.IsProjectile && !TryLandNonProjectileSpell(spell, target, itemCaster, out partialEvasion))
        {
            return;
        }

        HandleCastSpell(
            spell,
            target,
            itemCaster,
            weapon: caster,
            isWeaponSpell,
            fromProc: false,
            equip: false,
            showMsg: true,
            sigilTrinketSpell,
            partialEvasion: partialEvasion
        );

        if (!spell.IsProjectile)
        {
            OnNonProjectileSpellLanded(spell, target, caster);
        }
    }

    /// <summary>
    /// Spells a cast applies to its caster: void magic applies the void restoration penalty,
    /// and Vitality / Vigor / Clarity Mend apply Restoration Resonance
    /// </summary>
    private void ApplyCastAftereffectSpells(Spell spell)
    {
        if (spell.School == MagicSchool.VoidMagic && spell.Id != (uint)SpellId.VoidRestorationPenalty)
        {
            CastPlayerSpellOn(this, new Spell((int)SpellId.VoidRestorationPenalty), false);
        }

        // Apply Restoration Resonance for VitalityMend, VigorMend, or ClarityMend spells cast
        if (IsRestorationResonanceSpell(spell.Category))
        {
            CastPlayerSpellOn(this, new Spell((int)SpellId.RestorationResonance), false);
        }
    }

    /// <summary>
    /// Portal magic item spells (impen, bane, etc).
    /// A harmful one starts the PK timers with the target player, or the player wielding the target item.
    /// </summary>
    private void CastPortalItemSpell(Spell spell, WorldObject target, WorldObject itemCaster)
    {
        TryCastItemEnchantment_WithRedirects(spell, target, itemCaster);

        if (spell.IsHarmful)
        {
            var playerRedirect = target as Player;
            if (playerRedirect == null && target?.WielderId != null)
            {
                playerRedirect = CurrentLandblock?.GetObject(target.WielderId.Value) as Player;
            }

            if (playerRedirect != null)
            {
                UpdatePKTimers(this, playerRedirect);
            }
        }
    }

    /// <summary>
    /// A non-projectile spell lands unless the target resists it or is immune to non-projectile magic
    /// </summary>
    /// <param name="partialEvasion">The target's resist roll, for a spell that lands</param>
    private bool TryLandNonProjectileSpell(
        Spell spell,
        WorldObject target,
        WorldObject itemCaster,
        out PartialEvasion partialEvasion
    )
    {
        var targetCreature = target as Creature;
        var targetPlayer = target as Player;

        if (targetPlayer == null)
        {
            OnAttackMonster(targetCreature);
        }

        var harmsOther = spell.IsHarmful && targetCreature != null && targetCreature != this;

        if (TryResistSpell(target, spell, out partialEvasion, itemCaster))
        {
            if (harmsOther)
            {
                targetCreature.OnAttackReceived(this, CombatType.Magic, false, true);
            }

            return false;
        }

        if (harmsOther)
        {
            targetCreature.OnAttackReceived(this, CombatType.Magic, false, false);
        }

        if (targetCreature != null && targetCreature.NonProjectileMagicImmune)
        {
            Session.Network.EnqueueSend(
                new GameMessageSystemChat(
                    $"You fail to affect {targetCreature.Name} with {spell.Name}",
                    ChatMessageType.Magic
                )
            );
            return false;
        }

        return true;
    }

    /// <summary>
    /// After a non-projectile spell lands: proficiency, and for a harmful spell, the caster's procs and PK timers
    /// </summary>
    private void OnNonProjectileSpellLanded(Spell spell, WorldObject target, WorldObject caster)
    {
        var targetCreature = target as Creature;

        if (!spell.IsHarmful)
        {
            Proficiency.OnSuccessUse(this, GetCreatureSkill(spell.School), spell.PowerMod);
            return;
        }

        if (targetCreature != null)
        {
            Proficiency.OnSuccessUse(
                this,
                GetCreatureSkill(spell.School),
                targetCreature.GetCreatureSkill(Skill.MagicDefense).Current
            );
        }

        // handle target procs
        if (targetCreature != null && targetCreature != this)
        {
            TryProcEquippedItems(this, targetCreature, false, caster);
        }

        if (target is Player targetPlayer)
        {
            UpdatePKTimers(this, targetPlayer);
        }
    }

    public void FailCast(bool tryFizzle = true)
    {
        var parms = MagicState.CastSpellParams;

        var werror = WeenieError.None;

        if (parms != null && tryFizzle)
        {
            DoCastSpell_Inner(parms, parms.Target, CastingPreCheckStatus.CastFailed, false);

            werror = WeenieError.YourSpellFizzled;
        }
        SendUseDoneEvent(werror);

        MagicState.OnCastDone();
    }
}
