using System;
using System.Collections.Generic;
using ACE.DatLoader.Entity.AnimationHooks;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Entity;
using ACE.Server.Entity.Actions;
using ACE.Server.Managers;
using ACE.Server.Network.GameEvent.Events;
using ACE.Server.Physics.Animation;

namespace ACE.Server.WorldObjects
{
    /// <summary>
    /// Player melee attack
    /// </summary>
    partial class Player
    {
        /// <summary>
        /// The target this player is currently performing a melee attack on
        /// </summary>
        public Creature MeleeTarget;

        private float _powerLevel;

        /// <summary>
        /// The power bar level, a value between 0-1
        /// </summary>
        public float PowerLevel
        {
            get => IsExhausted ? 0.0f : _powerLevel;
            set => _powerLevel = value;
        }

        public override PowerAccuracy GetPowerRange()
        {
            if (PowerLevel < 0.33f)
                return PowerAccuracy.Low;
            else if (PowerLevel < 0.66f)
                return PowerAccuracy.Medium;
            else
                return PowerAccuracy.High;
        }

        public AttackQueue AttackQueue;

        /// <summary>
        /// Called when a player first initiates a melee attack
        /// </summary>
        public void HandleActionTargetedMeleeAttack(uint targetGuid, uint attackHeight, float powerLevel)
        {
            //log.Info($"-");

            if (CombatMode != CombatMode.Melee)
            {
                _log.Error($"{Name}.HandleActionTargetedMeleeAttack({targetGuid:X8}, {attackHeight}, {powerLevel}) - CombatMode mismatch {CombatMode}, LastCombatMode {LastCombatMode}");

                if (LastCombatMode == CombatMode.Melee)
                    CombatMode = CombatMode.Melee;
                else
                {
                    OnAttackDone();
                    return;
                }
            }

            if (IsBusy || Teleporting || suicideInProgress)
            {
                SendWeenieError(WeenieError.YoureTooBusy);
                OnAttackDone();
                return;
            }

            if (IsJumping)
            {
                SendWeenieError(WeenieError.YouCantDoThatWhileInTheAir);
                OnAttackDone();
                return;
            }

            if (PKLogout)
            {
                SendWeenieError(WeenieError.YouHaveBeenInPKBattleTooRecently);
                OnAttackDone();
                return;
            }

            // verify input
            powerLevel = Math.Clamp(powerLevel, 0.0f, 1.0f);

            AttackHeight = (AttackHeight)attackHeight;
            AttackQueue.Add(powerLevel);

            if (MeleeTarget == null)
                PowerLevel = AttackQueue.Fetch();

            // already in melee loop?
            if (Attacking || MeleeTarget != null && MeleeTarget.IsAlive)
                return;

            // get world object for target creature
            var target = CurrentLandblock?.GetObject(targetGuid);

            if (target == null)
            {
                //log.Debug($"{Name}.HandleActionTargetedMeleeAttack({targetGuid:X8}, {AttackHeight}, {powerLevel}) - couldn't find target guid");
                OnAttackDone();
                return;
            }

            var creatureTarget = target as Creature;
            if (creatureTarget == null)
            {
                _log.Warning($"{Name}.HandleActionTargetedMeleeAttack({targetGuid:X8}, {AttackHeight}, {powerLevel}) - target guid not creature");
                OnAttackDone();
                return;
            }

            if (!CanDamage(creatureTarget))
            {
                SendTransientError($"You cannot attack {creatureTarget.Name}");
                OnAttackDone();
                return;
            }

            if (!creatureTarget.IsAlive)
            {
                OnAttackDone();
                return;
            }

            //log.Info($"{Name}.HandleActionTargetedMeleeAttack({targetGuid:X8}, {attackHeight}, {powerLevel})");

            MeleeTarget = creatureTarget;
            AttackTarget = MeleeTarget;

            // reset PrevMotionCommand / DualWieldAlternate each time button is clicked
            PrevMotionCommand = MotionCommand.Invalid;
            DualWieldAlternate = false;

            var attackSequence = ++AttackSequence;

            if (NextRefillTime > DateTime.UtcNow)
            {
                var delayTime = (float)(NextRefillTime - DateTime.UtcNow).TotalSeconds;

                var actionChain = new ActionChain();
                actionChain.AddDelaySeconds(delayTime);
                actionChain.AddAction(this, () =>
                {
                    if (!creatureTarget.IsAlive)
                    {
                        OnAttackDone();
                        return;
                    }

                    HandleActionTargetedMeleeAttack_Inner(target, attackSequence);
                });
                actionChain.EnqueueChain();
            }
            else
                HandleActionTargetedMeleeAttack_Inner(target, attackSequence);
        }

        public static readonly float MeleeDistance  = 0.6f;
        public static readonly float StickyDistance = 4.0f;
        public static readonly float RepeatDistance = 16.0f;

        public void HandleActionTargetedMeleeAttack_Inner(WorldObject target, int attackSequence)
        {
            var dist = GetCylinderDistance(target);

            if (dist <= MeleeDistance || dist <= StickyDistance && IsMeleeVisible(target))
            {
                // sticky melee
                var angle = GetAngle(target);
                if (angle > PropertyManager.GetDouble("melee_max_angle").Item)
                {
                    var rotateTime = Rotate(target);

                    var actionChain = new ActionChain();
                    actionChain.AddDelaySeconds(rotateTime);
                    actionChain.AddAction(this, () => Attack(target, attackSequence));
                    actionChain.EnqueueChain();
                }
                else
                    Attack(target, attackSequence);
            }
            else
            {
                // turn / move to required
                if (GetCharacterOption(CharacterOption.UseChargeAttack))
                {
                    //log.Info($"{Name}.MoveTo({target.Name})");

                    // charge attack
                    MoveTo(target);
                }
                else
                {
                    //log.Info($"{Name}.CreateMoveToChain({target.Name})");

                    CreateMoveToChain(target, (success) =>
                    {
                        if (success)
                            Attack(target, attackSequence);
                        else
                            OnAttackDone();
                    });
                }
            }
        }

        public void OnAttackDone(WeenieError error = WeenieError.None)
        {
            // this function is called at the very end of an attack sequence,
            // and not between the repeat attacks

            // it sends action cancelled so the power / accuracy meter
            // is reset, and doesn't start refilling again

            // the werror for this network message is not displayed to the client --
            // if you wish to display a message, a separate GameEventWeenieError should also be sent

            Session.Network.EnqueueSend(new GameEventAttackDone(Session, WeenieError.ActionCancelled));

            AttackTarget = null;
            MeleeTarget = null;
            MissileTarget = null;

            AttackQueue.Clear();

            AttackCancelled = false;
        }

        /// <summary>
        /// called when client sends the 'Cancel attack' network message
        /// </summary>
        public void HandleActionCancelAttack(WeenieError error = WeenieError.None)
        {
            //Console.WriteLine($"{Name}.HandleActionCancelAttack()");

            if (Attacking)
                AttackCancelled = true;
            else if (AttackTarget != null)
                OnAttackDone();

            PhysicsObj.cancel_moveto();
        }

        public bool JewelCleave = false;
        public double LastJewelCleave = 0;

        /// <summary>
        /// Performs a player melee attack against a target
        /// </summary>
        public void Attack(WorldObject target, int attackSequence, bool subsequent = false)
        {
            //log.Info($"{Name}.Attack({target.Name}, {attackSequence})");

            if (AttackSequence != attackSequence)
                return;

            if (CombatMode != CombatMode.Melee || MeleeTarget == null || IsBusy || !IsAlive || suicideInProgress)
            {
                OnAttackDone();
                return;
            }

            var creature = target as Creature;
            if (creature == null || !creature.IsAlive)
            {
                OnAttackDone();
                return;
            }

            var animLength = DoSwingMotion(target, out var attackFrames);
            if (animLength == 0)
            {
                OnAttackDone();
                return;
            }

            // point of no return beyond this point -- cannot be cancelled
            Attacking = true;

            if (IsStealthed)
            {
                var angle = Math.Abs(creature.GetAngle(this));
                if (angle < 90 || creature.CombatMode != CombatMode.NonCombat)
                    EndStealth();
                else
                    EndStealth(null, true);
            }

            if (subsequent)
            {
                // client shows hourglass, until attack done is received
                // retail only did this for subsequent attacks w/ repeat attacks on
                Session.Network.EnqueueSend(new GameEventCombatCommenceAttack(Session));
            }

            var weapon = GetEquippedMeleeWeapon();
            var attackType = GetWeaponAttackType(weapon);
            var numStrikes = GetNumStrikes(attackType);
            var swingTime = animLength / numStrikes / 1.5f;

            var dualWieldSpec = GetCreatureSkill(Skill.DualWield).AdvancementClass == SkillAdvancementClass.Specialized;
            var dualWieldStaminaBonus = dualWieldSpec && IsDualWieldAttack;

            var actionChain = new ActionChain();

            // stamina usage
            // TODO: ensure enough stamina for attack
            var staminaCost = GetAttackStamina(GetPowerRange(), (float)LastAttackAnimationLength, weapon, dualWieldStaminaBonus);
            
            if (EquippedCombatAbility == CombatAbility.Fury && QuestManager.HasQuest($"{this.Name},Reckless"))
            {
                var recklessStacks = this.QuestManager.GetCurrentSolves($"{this.Name},Reckless");
                float recklessMod = 1 + (recklessStacks / 500);
                staminaCost = (int)(staminaCost * recklessMod);
            }

            // JEWEL - Citrine: Stamina cost reduction
            if (this is Player)
            {
                if (this.GetEquippedItemsRatingSum(PropertyInt.GearStamReduction) > 0)
                {
                    if (this.QuestManager.HasQuest($"{this.Name},StamReduction"))
                    {
                        var jewelRampMod = (float)this.QuestManager.GetCurrentSolves($"{this.Name},StamReduction") / 500;
                        var stamReductionMod = 1f - (jewelRampMod * (float)this.GetEquippedItemsRatingSum(PropertyInt.GearStamReduction) / 100);
                        var modifiedStamCost = (float)staminaCost * stamReductionMod;
                        staminaCost = (int)modifiedStamCost;
                    }
                }   
            }

            UpdateVitalDelta(Stamina, -staminaCost);

            var combatAbility = CombatAbility.None;
            var combatFocus = GetEquippedCombatFocus();
            if (combatFocus != null)
                combatAbility = combatFocus.GetCombatAbility();

            // COMBAT ABILITY - Enchant: All weapon attacks also consume mana
            if(combatAbility == CombatAbility.EnchantedWeapon)
                UpdateVitalDelta(Mana, -staminaCost);

            if (numStrikes != attackFrames.Count)
            {
                //log.Warn($"{Name}.GetAttackFrames(): MotionTableId: {MotionTableId:X8}, MotionStance: {CurrentMotionState.Stance}, Motion: {GetSwingAnimation()}, AttackFrames.Count({attackFrames.Count}) != NumStrikes({numStrikes})");
                numStrikes = attackFrames.Count;
            }

            // handle self-procs
            TryProcEquippedItems(this, this, true, weapon);

            var prevTime = 0.0f;
            bool targetProc = false;

            for (var i = 0; i < numStrikes; i++)
            {
                // are there animation hooks for damage frames?
                //if (numStrikes > 1 && !TwoHandedCombat)
                //actionChain.AddDelaySeconds(swingTime);
                actionChain.AddDelaySeconds(attackFrames[i].time * animLength - prevTime);
                prevTime = attackFrames[i].time * animLength;

                actionChain.AddAction(this, () =>
                {
                    if (IsDead)
                    {
                        Attacking = false;
                        OnAttackDone();
                        return;
                    }

                    var damageEvent = DamageTarget(creature, weapon);

                    // handle target procs
                    if (damageEvent != null && damageEvent.HasDamage && !targetProc)
                    {
                        TryProcEquippedItems(this, creature, false, weapon);
                        targetProc = true;
                    }

                    if (weapon != null && weapon.IsCleaving || (GetEquippedItemsRatingSum(PropertyInt.GearSlash) > 0) && damageEvent.DamageType == DamageType.Slash)
                    {
                        var cleave = GetCleaveTarget(creature, weapon);

                        if (cleave != null)
                        {
                            foreach (var cleaveHit in cleave)
                            {
                                // target procs don't happen for cleaving
                                DamageTarget(cleaveHit, weapon);
                            }
                        }
                    }

                });

                //if (numStrikes == 1 || TwoHandedCombat)
                    //actionChain.AddDelaySeconds(swingTime);
            }

            //actionChain.AddDelaySeconds(animLength - swingTime * numStrikes);
            actionChain.AddDelaySeconds(animLength - prevTime);

            actionChain.AddAction(this, () =>
            {
                Attacking = false;

                // powerbar refill timing
                var isDualWieldSpec = GetCreatureSkill(Skill.DualWield).AdvancementClass == SkillAdvancementClass.Specialized;
                var dualWieldSpecBonus = (IsDualWieldAttack && isDualWieldSpec) ? 1.25f : 1.0f; // Dual Wield Spec Bonus: +25% faster refill time

                var refillMod = 1.0f / dualWieldSpecBonus;    

                PowerLevel = AttackQueue.Fetch();

                var nextRefillTime = PowerLevel * refillMod;
                NextRefillTime = DateTime.UtcNow.AddSeconds(nextRefillTime);

                var dist = GetCylinderDistance(target);

                if (creature.IsAlive && GetCharacterOption(CharacterOption.AutoRepeatAttacks) && (dist <= MeleeDistance || dist <= StickyDistance && IsMeleeVisible(target)) && !IsBusy && !AttackCancelled)
                {
                    // client starts refilling power meter
                    Session.Network.EnqueueSend(new GameEventAttackDone(Session));

                    var nextAttack = new ActionChain();
                    nextAttack.AddDelaySeconds(nextRefillTime);
                    nextAttack.AddAction(this, () => Attack(target, attackSequence, true));
                    nextAttack.EnqueueChain();
                }
                else
                    OnAttackDone();
            });

            actionChain.EnqueueChain();

            if (UnderLifestoneProtection)
                LifestoneProtectionDispel();
        }

        /// <summary>
        /// Performs the player melee swing animation
        /// </summary>
        public float DoSwingMotion(WorldObject target, out List<(float time, AttackHook attackHook)> attackFrames)
        {
            // get the proper animation speed for this attack,
            // based on weapon speed and player quickness
            var baseSpeed = GetAnimSpeed();

            var isDualWieldSpec = GetCreatureSkill(Skill.DualWield).AdvancementClass == SkillAdvancementClass.Specialized;
            var animSpeedMod = (IsDualWieldAttack && isDualWieldSpec) ? 1.25f : 1.0f; // Dual Wield Spec Bonus: +25% faster dual-wield swing animation

            var animSpeed = baseSpeed * animSpeedMod;

            var swingAnimation = GetSwingAnimation();
            var animLength = MotionTable.GetAnimationLength(MotionTableId, CurrentMotionState.Stance, swingAnimation, animSpeed);
            //Console.WriteLine($"AnimSpeed: {animSpeed}, AnimLength: {animLength}");

            LastAttackAnimationLength = animLength;
            
            attackFrames = MotionTable.GetAttackFrames(MotionTableId, CurrentMotionState.Stance, swingAnimation);
            //Console.WriteLine($"Attack frames: {string.Join(",", attackFrames)}");

            // broadcast player swing animation to clients
            var motion = new Motion(this, swingAnimation, animSpeed);
            if (PropertyManager.GetBool("persist_movement").Item)
            {
                motion.Persist(CurrentMotionState);
            }
            motion.MotionState.TurnSpeed = 2.25f;
            motion.MotionFlags |= MotionFlags.StickToObject;
            motion.TargetGuid = target.Guid;
            CurrentMotionState = motion;

            EnqueueBroadcastMotion(motion);

            if (FastTick)
                PhysicsObj.stick_to_object(target.Guid.Full);

            return animLength;
        }

        public static readonly float KickThreshold = 0.75f;

        public MotionCommand PrevMotionCommand;

        /// <summary>
        /// Returns the melee swing animation - based on weapon,
        /// current stance, power bar, and attack height
        /// </summary>
        public MotionCommand GetSwingAnimation()
        {
            if (IsDualWieldAttack)
                DualWieldAlternate = !DualWieldAlternate;

            var offhand = IsDualWieldAttack && !DualWieldAlternate;

            var weapon = GetEquippedMeleeWeapon();

            // for reference: https://www.youtube.com/watch?v=MUaD53D9c74
            // a player with 1/2 power bar, or slightly below half
            // doing the backswing, well above 33%
            var subdivision = 0.33f;

            if (weapon != null)
            {
                AttackType = weapon.GetAttackType(CurrentMotionState.Stance, SlashThrustToggle, offhand);
                if (weapon.IsThrustSlash)
                    subdivision = 0.66f;
            }
            else
            {
                AttackType = PowerLevel > KickThreshold && !IsDualWieldAttack ? AttackType.Kick : AttackType.Punch;
            }

            var motions = CombatTable.GetMotion(CurrentMotionState.Stance, AttackHeight.Value, AttackType, PrevMotionCommand);

            MotionCommand motion;
            // higher-powered animation always in first slot ?
            if (AttackType == AttackType.Punch)
                motion = motions.Count > 1 && SlashThrustToggle ? motions[1] : motions[0];
            else
                motion = motions.Count > 1 && PowerLevel < subdivision ? motions[1] : motions[0];

            PrevMotionCommand = motion;

            //Console.WriteLine($"{motion}");

            return motion;
        }

        /// <summary>
        /// Returns the melee swing motion - based on weapon,
        /// current stance, power bar, and attack height
        /// </summary>
        public MotionCommand GetSwingMotion()
        {
            var offhand = IsDualWieldAttack && !DualWieldAlternate;

            var weapon = GetEquippedMeleeWeapon();

            var subdivision = 0.33f;

            if (weapon != null)
            {
                AttackType = weapon.GetAttackType(CurrentMotionState.Stance, SlashThrustToggle, offhand);
                if (weapon.IsThrustSlash)
                    subdivision = 0.66f;
            }
            else
            {
                AttackType = PowerLevel > KickThreshold && !IsDualWieldAttack ? AttackType.Kick : AttackType.Punch;
            }

            var motions = CombatTable.GetMotion(CurrentMotionState.Stance, AttackHeight.Value, AttackType, PrevMotionCommand);

            MotionCommand motion;
            // higher-powered animation always in first slot ?
            if (AttackType == AttackType.Punch)
                motion = motions.Count > 1 && SlashThrustToggle ? motions[1] : motions[0];
            else
                motion = motions.Count > 1 && PowerLevel < subdivision ? motions[1] : motions[0];

            PrevMotionCommand = motion;

            return motion;
        }
    }
}
