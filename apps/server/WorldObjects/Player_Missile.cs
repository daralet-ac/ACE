using System;
using System.Collections.Generic;
using System.Numerics;
using ACE.Common;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Entity.Actions;
using ACE.Server.Network.GameEvent.Events;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.Physics.Animation;
using MotionCommand = ACE.Entity.Enum.MotionCommand;

namespace ACE.Server.WorldObjects;

partial class Player
{
    private float _accuracyLevel;

    public float AccuracyLevel
    {
        get => IsExhausted ? 0.0f : _accuracyLevel;
        set => _accuracyLevel = value;
    }

    private Creature _missileTarget;

    private PowerAccuracy GetAccuracyRange()
    {
        switch (AccuracyLevel)
        {
            case < 0.33f:
                return PowerAccuracy.Low;
            case < 0.66f:
                return PowerAccuracy.Medium;
            default:
                return PowerAccuracy.High;
        }
    }

    /// <summary>
    /// Called by network packet handler 0xA - GameActionTargetedMissileAttack
    /// </summary>
    /// <param name="targetGuid">The target guid</param>
    /// <param name="attackHeight">The attack height 1-3</param>
    /// <param name="accuracyLevel">The 0-1 accuracy bar level</param>
    public void HandleActionTargetedMissileAttack(uint targetGuid, uint attackHeight, float accuracyLevel)
    {
        //log.Info($"-");

        if (CombatMode != CombatMode.Missile)
        {
            _log.Error(
                $"{Name}.HandleActionTargetedMissileAttack({targetGuid:X8}, {attackHeight}, {accuracyLevel}) - CombatMode mismatch {CombatMode}, LastCombatMode: {LastCombatMode}"
            );

            if (LastCombatMode == CombatMode.Missile)
            {
                CombatMode = CombatMode.Missile;
            }
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

        var weapon = GetEquippedMissileWeapon();
        var ammo = GetEquippedAmmo();

        // sanity check
        accuracyLevel = Math.Clamp(accuracyLevel, 0.0f, 1.0f);

        if (weapon == null || weapon.IsAmmoLauncher && ammo == null)
        {
            OnAttackDone();
            return;
        }

        AttackHeight = (AttackHeight)attackHeight;
        AttackQueue.Add(accuracyLevel);

        if (_missileTarget == null)
        {
            AccuracyLevel = accuracyLevel; // verify
        }

        // get world object of target guid
        var target = CurrentLandblock?.GetObject(targetGuid) as Creature;
        if (target == null || target.Teleporting)
        {
            //log.Warn($"{Name}.HandleActionTargetedMissileAttack({targetGuid:X8}, {AttackHeight}, {accuracyLevel}) - couldn't find creature target guid");
            OnAttackDone();
            return;
        }

        if (Attacking || _missileTarget != null && _missileTarget.IsAlive)
        {
            return;
        }

        if (!CanDamage(target))
        {
            SendTransientError($"You cannot attack {target.Name}");
            OnAttackDone();
            return;
        }

        //log.Info($"{Name}.HandleActionTargetedMissileAttack({targetGuid:X8}, {attackHeight}, {accuracyLevel})");

        AttackTarget = target;
        _missileTarget = target;

        var attackSequence = ++AttackSequence;

        // record stance here and pass it along
        // accounts for odd client behavior with swapping bows during repeat attacks
        var motionStance = CurrentMotionState.Stance;

        // turn if required
        var rotateTime = Rotate(target);
        var actionChain = new ActionChain();

        var delayTime = rotateTime;
        if (NextRefillTime > DateTime.UtcNow.AddSeconds(delayTime))
        {
            delayTime = (float)(NextRefillTime - DateTime.UtcNow).TotalSeconds;
        }

        actionChain.AddDelaySeconds(delayTime);

        // do missile attack
        actionChain.AddAction(this, () => LaunchMissile(target, attackSequence, motionStance));
        actionChain.EnqueueChain();
    }

    /// <summary>
    /// Launches a missile attack from player to target
    /// </summary>
    private void LaunchMissile(WorldObject target, int attackSequence, MotionStance motionStance, bool subsequent = false)
    {
        if (AttackSequence != attackSequence)
        {
            return;
        }

        var weapon = GetEquippedMissileWeapon();
        if (weapon == null || CombatMode == CombatMode.NonCombat)
        {
            OnAttackDone();
            return;
        }

        var ammo = weapon.IsAmmoLauncher ? GetEquippedAmmo() : weapon;
        if (ammo == null)
        {
            OnAttackDone();
            return;
        }

        var launcher = GetEquippedMissileLauncher();

        var creature = target as Creature;
        if (!IsAlive || IsBusy || _missileTarget == null || creature == null || !creature.IsAlive || suicideInProgress)
        {
            OnAttackDone();
            return;
        }

        if (!TargetInRange(target))
        {
            // this must also be sent to actually display the transient message
            SendWeenieError(WeenieError.MissileOutOfRange);

            // this prevents the accuracy bar from refilling when 'repeat attacks' is enabled
            OnAttackDone();

            return;
        }

        var actionChain = new ActionChain();

        if (subsequent && !IsFacing(target))
        {
            var rotateTime = Rotate(target);
            actionChain.AddDelaySeconds(rotateTime);
        }

        // launch animation
        // point of no return beyond this point -- cannot be cancelled
        actionChain.AddAction(this, () => Attacking = true);

        // if (subsequent)
        // {
        //     // client shows hourglass, until attack done is received
        //     // retail only did this for subsequent attacks w/ repeat attacks on
        //     Session.Network.EnqueueSend(new GameEventCombatCommenceAttack(Session));
        // }

        var projectileSpeed = GetProjectileSpeed();

        // get z-angle for aim motion
        var aimVelocity = GetAimVelocity(target, projectileSpeed);

        var aimLevel = GetAimLevel(aimVelocity);

        // calculate projectile spawn pos and velocity
        var localOrigin = GetProjectileSpawnOrigin(ammo.WeenieClassId, aimLevel);

        var velocity = CalculateProjectileVelocity(
            localOrigin,
            target,
            projectileSpeed,
            out var origin,
            out var orientation
        );

        //Console.WriteLine($"Velocity: {velocity}");

        if (velocity == Vector3.Zero)
        {
            // pre-check succeeded, but actual velocity calculation failed
            SendWeenieError(WeenieError.MissileOutOfRange);

            // this prevents the accuracy bar from refilling when 'repeat attacks' is enabled
            Attacking = false;
            OnAttackDone();
            return;
        }

        // launch and reload animations are sped up or slowed down
        // so the full attack takes the weapon's time per hit
        var animSpeed = GetMissileAnimSpeed(motionStance, aimLevel, target as Creature);

        var launchTime = EnqueueMotionPersist(actionChain, aimLevel, animSpeed);

        // launch projectile
        actionChain.AddAction(
            this,
            () =>
            {
                // handle self-procs
                TryProcEquippedItems(this, this, true, weapon);

                var sound = GetLaunchMissileSound(weapon);
                EnqueueBroadcast(new GameMessageSound(Guid, sound, 1.0f));

                // stamina usage
                // TODO: ensure enough stamina for attack
                // TODO: verify formulas - double/triple cost for bow/xbow?


                var projectile = LaunchProjectile(launcher, ammo, target, origin, orientation, velocity);
                UpdateAmmoAfterLaunch(ammo);

                if (IsStealthed)
                {
                    EndStealth(null, true);
                }

                LaunchBonusProjectiles(weapon, launcher, ammo, creature, projectileSpeed);
            }
        );

        // ammo remaining?
        if (!ammo.UnlimitedUse && (ammo.StackSize == null || ammo.StackSize <= 1))
        {
            actionChain.AddAction(
                this,
                () =>
                {
                    Session.Network.EnqueueSend(
                        new GameEventCommunicationTransientString(Session, "You are out of ammunition!")
                    );
                    SetCombatMode(CombatMode.NonCombat);
                    Attacking = false;
                    OnAttackDone();
                }
            );

            actionChain.EnqueueChain();
            return;
        }

        // reload animation
        var reloadTime = EnqueueMotionPersist(actionChain, motionStance, MotionCommand.Reload, animSpeed);

        // reset for next projectile
        EnqueueMotionPersist(actionChain, motionStance, MotionCommand.Ready);
        var linkTime = MotionTable.GetAnimationLength(MotionTableId, motionStance, MotionCommand.Reload, MotionCommand.Ready);
        //var cycleTime = MotionTable.GetCycleLength(MotionTableId, CurrentMotionState.Stance, MotionCommand.Ready);

        // stamina cost and accuracy damage bonus for missile attacks were tuned against about half of the attack time
        const float missileAttackLengthScale = 0.55f;
        LastAttackAnimationLength = (linkTime + launchTime + reloadTime) * missileAttackLengthScale;
        //Console.WriteLine($"LaunchTime: {launchTime}, Reload: {reloadTime} (BaseReload: {reloadTime*animSpeed}), Link: {linkTime}, TOTAL: {LastAttackAnimationLength}");

        var staminaCost = GetAttackStamina((float)LastAttackAnimationLength, weapon);
        UpdateVitalDelta(Stamina, -staminaCost);

        if (Stamina.Current < 1 && EvasiveStanceIsActive)
        {
            EvasiveStanceIsActive = false;

            Session.Network.EnqueueSend(
                new GameMessageSystemChat($"Your fall out of your evasive stance!!", ChatMessageType.Broadcast)
            );

            var evasiveStanceItem = GetInventoryItemsOfWCID(1051114);
            if (evasiveStanceItem.Count > 0)
            {
                EnchantmentManager.StartCooldown(evasiveStanceItem[0]);
            }
        }

        CheckForFurySelfDamage(staminaCost);

        actionChain.AddAction(
            this,
            () =>
            {
                if (CombatMode == CombatMode.Missile)
                {
                    EnqueueBroadcast(
                        new GameMessageParentEvent(
                            this,
                            ammo,
                            ACE.Entity.Enum.ParentLocation.RightHand,
                            ACE.Entity.Enum.Placement.RightHandCombat
                        )
                    );
                }
            }
        );

        actionChain.AddDelaySeconds(linkTime);

        actionChain.AddAction(
            this,
            () =>
            {
                Attacking = false;

                if (
                    creature.IsAlive
                    && GetCharacterOption(CharacterOption.AutoRepeatAttacks)
                    && !IsBusy
                    && !AttackCancelled
                )
                {
                    // client starts refilling accuracy bar
                    Session.Network.EnqueueSend(new GameEventAttackDone(Session));

                    AccuracyLevel = AttackQueue.Fetch();

                    // can be cancelled, but cannot be pre-empted with another attack
                    var nextAttack = new ActionChain();
                    var nextRefillTime = AccuracyLevel;

                    NextRefillTime = DateTime.UtcNow.AddSeconds(nextRefillTime);
                    nextAttack.AddDelaySeconds(nextRefillTime);

                    // perform next attack
                    nextAttack.AddAction(
                        this,
                        () =>
                        {
                            LaunchMissile(target, attackSequence, motionStance, true);
                        }
                    );
                    nextAttack.EnqueueChain();
                }
                else
                {
                    OnAttackDone();
                }
            }
        );

        actionChain.EnqueueChain();

        if (UnderLifestoneProtection)
        {
            LifestoneProtectionDispel();
        }
    }

    // bonus projectiles can only hit creatures this close to the main target
    private const float MissileCleaveRange = 10.0f;

    private const int MultishotBonusProjectiles = 2;

    // sideways spawn offset for bonus projectiles fired at the main target, so they don't fly as a single arrow
    private const float MultishotSpread = 0.25f;

    /// <summary>
    /// Fires the bonus projectiles for an attack.
    /// COMBAT ABILITY - Multishot: 2 bonus projectiles at nearby enemies. Any that can't find one fire at the main target
    /// instead, where they deal half damage like melee cleaves.
    /// RATING - Slash: a bonus projectile at a nearby enemy, if there's one Multishot didn't already take.
    /// </summary>
    private void LaunchBonusProjectiles(
        WorldObject weapon,
        WorldObject launcher,
        WorldObject ammo,
        Creature target,
        float projectileSpeed
    )
    {
        var multishotProjectiles = MultiShotIsActive && GetPowerAccuracyBar() >= 0.5f ? MultishotBonusProjectiles : 0;
        var slashProjectiles = CheckForRatingSlashCleaveBonus(ammo);

        if (multishotProjectiles + slashProjectiles == 0)
        {
            return;
        }

        var bonusTargets = GetMissileCleaveTargets(target, multishotProjectiles + slashProjectiles, multishotProjectiles > 0);

        while (bonusTargets.Count < multishotProjectiles)
        {
            bonusTargets.Add(target);
        }

        var spreadDirection = 1.0f;

        foreach (var bonusTarget in bonusTargets)
        {
            var ammoCheck = weapon.IsAmmoLauncher ? GetEquippedAmmo() : weapon;
            if (ammoCheck == null)
            {
                OnAttackDone();
                return;
            }

            var aimVelocity = GetAimVelocity(bonusTarget, projectileSpeed);
            var aimLevel = GetAimLevel(aimVelocity);
            var localOrigin = GetProjectileSpawnOrigin(ammo.WeenieClassId, aimLevel);

            var isMainTarget = bonusTarget == target;

            if (isMainTarget)
            {
                localOrigin.X += MultishotSpread * spreadDirection;
                spreadDirection = -spreadDirection;
            }

            var velocity = CalculateProjectileVelocity(
                localOrigin,
                bonusTarget,
                projectileSpeed,
                out var origin,
                out var orientation
            );

            if (velocity == Vector3.Zero)
            {
                continue;
            }

            LaunchProjectile(launcher, ammo, bonusTarget, origin, orientation, velocity, isMainTarget);
            UpdateAmmoAfterLaunch(ammo);
        }
    }

    /// <summary>
    /// Returns up to maxTargets enemies near the main target that can be hit with a bonus projectile, nearest first
    /// </summary>
    private List<Creature> GetMissileCleaveTargets(Creature target, int maxTargets, bool multishot)
    {
        var cleaveTargets = new List<Creature>();

        // excludes the main target itself
        foreach (var creature in target.GetNearbyMonsters(MissileCleaveRange))
        {
            if (cleaveTargets.Count == maxTargets)
            {
                break;
            }

            if (!creature.IsAlive || !CanDamage(creature) || creature.Translucency == 1 || !TargetInRange(creature))
            {
                continue;
            }

            // Multishot can hit targets behind you
            if (!multishot && Math.Abs(GetAngle(creature)) > CleaveAngle / 2.0f)
            {
                continue;
            }

            cleaveTargets.Add(creature);
        }

        return cleaveTargets;
    }

    /// <summary>
    /// RATING - Slash: 1% chance per rating to gain +1 cleave target.
    /// (JEWEL - Imperial Topaz)
    /// </summary>
    private int CheckForRatingSlashCleaveBonus(WorldObject ammo)
    {
        if (ammo is not { W_DamageType: DamageType.Slash })
        {
            return 0;
        }

        var chance = Jewel.GetJewelEffectMod(this, PropertyInt.GearSlash);

        return ThreadSafeRandom.Next(0.0f, 1.0f) < chance ? 1 : 0;
    }

    // TODO: the damage pipeline currently uses the creature ammo instead of the projectile
    // for calculating damage. when the last arrow is launched, the player ammo will be null
    // give projectiles an owner, and have the damage pipeline take the actual damage source object
    // (ie. the arrow-in-flight, or a melee weapon)

    public override float GetAimHeight(WorldObject target)
    {
        if (AttackHeight == null)
        {
            return 2.0f;
        }

        switch (AttackHeight.Value)
        {
            case ACE.Entity.Enum.AttackHeight.High:
                return 1.0f;
            case ACE.Entity.Enum.AttackHeight.Medium:
                return 2.0f;
            //case AttackHeight.Low: return target.Height;
            case ACE.Entity.Enum.AttackHeight.Low:
                return 3.0f;
        }

        return 2.0f;
    }

    public override void UpdateAmmoAfterLaunch(WorldObject ammo)
    {
        //if (ammo.UnlimitedUse)
        //    return;

        // hide previously held ammo
        EnqueueBroadcast(new GameMessagePickupEvent(ammo));

        if (ammo is Ammunition {AmmoEffectUsesRemaining: not null} ammunition)
        {
            ammunition.AmmoEffectUsesRemaining -= 1;

            if (ammunition.AmmoEffectUsesRemaining < 1)
            {
                ammunition.AmmoEffectUsesRemaining = null;
            }
        }

        if (ammo.UnlimitedUse)
        {
            return;
        }

        if (ammo.StackSize == null || ammo.StackSize <= 1)
        {
            TryDequipObjectWithNetworking(ammo.Guid, out _, DequipObjectAction.ConsumeItem);
        }
        else
        {
            TryConsumeFromInventoryWithNetworking(ammo, 1);
        }
    }

    private bool TargetInRange(WorldObject target)
    {
        // 2d or 3d distance?
        var dist = Location.DistanceTo(target.Location);

        var maxRange = GetMaxMissileRange();

        return dist <= maxRange;
    }
}
