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
    public void DoCastSpell(bool checkAngle = true)
    {
        if (!MagicState.IsCasting)
        {
            return;
        }

        var state = MagicState.CastSpellParams;

        if (state == null)
        {
            _log.Warning($"{Name}.DoCastSpell(): null state detected");

            // send UseDone?
            SendUseDoneEvent(WeenieError.BadCast);

            return;
        }

        DoCastSpell(
            state.Spell,
            state.CasterItem,
            state.MagicSkill,
            state.ManaUsed,
            state.ManaRefund,
            state.Target,
            state.Status,
            checkAngle
        );
    }

    public void DoCastSpell(
        Spell spell,
        WorldObject casterItem,
        uint magicSkill,
        uint manaUsed,
        ManaCastRefund manaRefund,
        WorldObject target,
        CastingPreCheckStatus castingPreCheckStatus,
        bool checkAngle = true
    )
    {
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
                    actionChain.AddAction(
                        this,
                        () =>
                            DoCastSpell(
                                spell,
                                casterItem,
                                magicSkill,
                                manaUsed,
                                manaRefund,
                                target,
                                castingPreCheckStatus,
                                false
                            )
                    );
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
            if (!VerifySpellRange(target, targetCategory, spell, casterItem, magicSkill))
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

        DoCastSpell_Inner(
            spell,
            casterItem,
            manaUsed,
            manaRefund,
            target,
            castingPreCheckStatus
        );
    }

    public void DoCastSpell_Inner(
        Spell spell,
        WorldObject casterItem,
        uint manaUsed,
        ManaCastRefund manaRefund,
        WorldObject target,
        CastingPreCheckStatus castingPreCheckStatus,
        bool finishCast = true
    )
    {
        if (RecordCast.Enabled)
        {
            RecordCast.Log($"DoCastSpell_Inner()");
        }

        if (MagicState.CastMeter)
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

        // consume mana
        var caster = casterItem ?? GetEquippedWand(); // TODO: persist this from the beginning, since this is done with delay

        var isWeaponSpell = casterItem != null;

        var itemCaster = isWeaponSpell ? caster : null;

        // SIGIL SCARAB - Mana Cost Reduction
        var manaModifier = spell.School == MagicSchool.LifeMagic
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

        if (!isWeaponSpell)
        {
            UpdateVitalDelta(Mana, -(int)manaUsed);
        }
        else
        {
            itemCaster.ItemCurMana -= (int)manaUsed;
        }

        ApplyManaCastRefund(manaRefund);

        // consume spell components
        if (!isWeaponSpell)
        {
            TryBurnComponents(spell);
        }

        // check windup move distance cap
        var dist = StartPos.Distance(PhysicsObj.Position);

        // only PKs affected by these caps?
        if (dist > Windup_MaxMove && PlayerKillerStatus != PlayerKillerStatus.NPK)
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

        var spellReleased = false;

        switch (castingPreCheckStatus)
        {
            case CastingPreCheckStatus.Success:

                spellReleased = true;

                if (!spell.IsFellowshipSpell)
                {
                    CreatePlayerSpell(target, spell, isWeaponSpell);
                }
                else
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

                        CreatePlayerSpell(fellow, spell, isWeaponSpell);
                    }
                }

                // handle self procs
                if (spell.IsHarmful && target != this)
                {
                    TryProcEquippedItems(this, this, true, caster);
                }

                break;

            case CastingPreCheckStatus.InvalidPKStatus:

                if (spell.NumProjectiles > 0)
                {
                    spellReleased = true;
                    HandleCastSpell(spell, target, itemCaster, caster, isWeaponSpell);
                }

                break;

            default:
                EnqueueBroadcast(new GameMessageScript(Guid, PlayScript.Fizzle, 0.5f));
                SendWeenieError(WeenieError.YourSpellFizzled);

                SendRestrictedCasterFizzleMessages(caster, spell);

                break;
        }

        if (pk_error != null && spell.NumProjectiles == 0)
        {
            Session.Network.EnqueueSend(new GameEventWeenieErrorWithString(Session, pk_error[0], target.Name));

            if (target is Player targetPlayer)
            {
                targetPlayer.Session.Network.EnqueueSend(
                    new GameEventWeenieErrorWithString(targetPlayer.Session, pk_error[1], Name)
                );
            }
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
    /// After a fizzle, explains it when the caster is an item restricted to one school (or out of mana)
    /// </summary>
    private void SendRestrictedCasterFizzleMessages(WorldObject caster, Spell spell)
    {
        if (GetRestrictedSchool(caster) is not { } restrictedSchool)
        {
            return;
        }

        if (spell.School != restrictedSchool && spell.School is MagicSchool.WarMagic or MagicSchool.LifeMagic or MagicSchool.PortalMagic)
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
        var chance = meter * 0.5f;

        if (ThreadSafeRandom.Next(0.0f, 1.0f) >= chance)
        {
            return;
        }

        var selfDamage = Convert.ToInt32(0.1f * meter * spell.BaseMana);

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

    public void FinishCast()
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

    public List<Player> GetFellowshipTargets()
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

    private void CreatePlayerSpell(WorldObject target, Spell spell, bool isWeaponSpell, bool sigilTrinketSpell = false)
    {
        var targetCreature = target as Creature;
        var targetPlayer = target as Player;

        LastSuccessCast_Time = Time.GetUnixTime();

        if (spell.School == MagicSchool.VoidMagic && spell.Id != (uint)SpellId.VoidRestorationPenalty)
        {
            CreatePlayerSpell(this, new Spell((int)SpellId.VoidRestorationPenalty), false);
        }

        // Apply Restoration Resonance for VitalityMend, VigorMend, or ClarityMend spells cast
        if (IsRestorationResonanceSpell(spell.Category))
        {
            CreatePlayerSpell(this, new Spell((int)SpellId.RestorationResonance), false);
        }

        var caster = GetEquippedWand();

        var itemCaster = isWeaponSpell ? caster : null;

        // verify after windup, still consumes mana
        if (spell.MetaSpellType == SpellType.Dispel && !VerifyDispelPkStatus(this, target))
        {
            return;
        }

        switch (spell.School)
        {
            case MagicSchool.PortalMagic:

                TryCastItemEnchantment_WithRedirects(spell, target, itemCaster);

                // use target resistance?
                // Proficiency.OnSuccessUse(this, GetCreatureSkill(Skill.PortalMagic), spell.PowerMod);

                if (spell.IsHarmful)
                {
                    var playerRedirect = targetPlayer;
                    if (playerRedirect == null && target?.WielderId != null)
                    {
                        playerRedirect = CurrentLandblock?.GetObject(target.WielderId.Value) as Player;
                    }

                    if (playerRedirect != null)
                    {
                        UpdatePKTimers(this, playerRedirect);
                    }
                }
                break;

            default:

                if (!spell.IsProjectile)
                {
                    if (targetPlayer == null)
                    {
                        OnAttackMonster(targetCreature);
                    }

                    if (TryResistSpell(target, spell, out _, itemCaster))
                    {
                        if (spell.IsHarmful && targetCreature != null && targetCreature != this)
                        {
                            targetCreature.OnAttackReceived(this, CombatType.Magic, false, true);
                        }

                        break;
                    }
                    else if (spell.IsHarmful && targetCreature != null && targetCreature != this)
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
                        break;
                    }
                }

                HandleCastSpell(spell, target, itemCaster, caster, isWeaponSpell, false, false, true, sigilTrinketSpell);

                if (!spell.IsProjectile)
                {
                    if (spell.IsHarmful)
                    {
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

                        if (targetPlayer != null)
                        {
                            UpdatePKTimers(this, targetPlayer);
                        }
                    }
                    else
                    {
                        Proficiency.OnSuccessUse(this, GetCreatureSkill(spell.School), spell.PowerMod);
                    }
                }

                break;
        }
    }

    public void FailCast(bool tryFizzle = true)
    {
        var parms = MagicState.CastSpellParams;

        var werror = WeenieError.None;

        if (parms != null && tryFizzle)
        {
            DoCastSpell_Inner(
                parms.Spell,
                parms.CasterItem,
                parms.ManaUsed,
                parms.ManaRefund,
                parms.Target,
                CastingPreCheckStatus.CastFailed,
                false
            );

            werror = WeenieError.YourSpellFizzled;
        }
        SendUseDoneEvent(werror);

        MagicState.OnCastDone();
    }
}
