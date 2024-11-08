using System;
using ACE.Common;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Entity.Models;
using ACE.Server.Entity;
using ACE.Server.Entity.Actions;
using ACE.Server.Network.GameEvent.Events;
using ACE.Server.Network.GameMessages.Messages;
using Time = ACE.Common.Time;

namespace ACE.Server.WorldObjects;

/// <summary>
/// Activates an object based on collision
/// </summary>
public class PressurePlate : WorldObject
{
    /// <summary>
    /// The last time this pressure plate was activated
    /// </summary>
    public DateTime LastUseTime;

    /// <summary>
    /// A new biota be created taking all of its values from weenie.
    /// </summary>
    public PressurePlate(Weenie weenie, ObjectGuid guid)
        : base(weenie, guid)
    {
        SetEphemeralValues();
    }

    /// <summary>
    /// Restore a WorldObject from the database.
    /// </summary>
    public PressurePlate(Biota biota)
        : base(biota)
    {
        SetEphemeralValues();
    }

    private void SetEphemeralValues()
    {
        if (UseSound == 0)
        {
            UseSound = Sound.TriggerActivated;
        }

        DefaultActive = Active;
        NextRearm = 0;
    }

    public override void SetLinkProperties(WorldObject wo)
    {
        wo.ActivationTarget = Guid.Full;
    }

    /// <summary>
    /// Called when a player runs over the pressure plate
    /// </summary>
    public override void OnCollideObject(WorldObject wo)
    {
        OnActivate(wo);
    }

    public bool NextActivationIsFromUse = false;

    /// <summary>
    /// Activates the object linked to a pressure plate
    /// </summary>
    public override void OnActivate(WorldObject activator)
    {
        if (!Active)
        {
            return;
        }

        // handle monsters walking on pressure plates
        if (!(activator is Player player))
        {
            return;
        }

        if (
            !NextActivationIsFromUse
            && ResistPerception.HasValue
            && player.TestStealth((uint)ResistPerception, "You fail to avoid the trigger! You lose stealth.")
        )
        {
            return;
        }

        NextActivationIsFromUse = false;

        // prevent continuous event stream
        // TODO: should this go in base.OnActivate()?

        var currentTime = DateTime.UtcNow;
        if (currentTime < LastUseTime + TimeSpan.FromSeconds(2))
        {
            return;
        }

        LastUseTime = currentTime;

        player.EnqueueBroadcast(new GameMessageSound(player.Guid, UseSound));

        base.OnActivate(activator);
    }

    public override void ActOnUse(WorldObject wo) { }

    public override void Heartbeat(double currentUnixTime)
    {
        base.Heartbeat(currentUnixTime);

        if (NextRearm != 0 && NextRearm <= currentUnixTime)
        {
            Active = true;
        }
    }

    private bool DefaultActive;
    private double NextRearm;
    private static int DisarmLength = 300;

    public void Disarm()
    {
        if (!DefaultActive)
        {
            return;
        }

        if (Active)
        {
            Active = false;
            NextRearm = Time.GetFutureUnixTime(DisarmLength);

            EnqueueBroadcast(new GameMessagePublicUpdatePropertyInt(this, PropertyInt.Active, Active ? 1 : 0));
        }
        else
        {
            NextRearm = Time.GetFutureUnixTime(DisarmLength);
        }
    }

    public void Rearm()
    {
        if (!DefaultActive || Active)
        {
            return;
        }

        Active = true;
        NextRearm = 0;

        EnqueueBroadcast(new GameMessagePublicUpdatePropertyInt(this, PropertyInt.Active, Active ? 1 : 0));
    }

    public void AttemptDisarm(Player player, WorldObject unlocker)
    {
        if (!DefaultActive || !Tier.HasValue)
        {
            player.Session.Network.EnqueueSend(
                new GameEventUseDone(player.Session, WeenieError.YouCannotLockOrUnlockThat)
            );
            return;
        }

        var chain = new ActionChain();

        chain.AddAction(
            player,
            () =>
            {
                if (player.Skills[Skill.Lockpick].AdvancementClass < SkillAdvancementClass.Trained)
                {
                    player.Session.Network.EnqueueSend(
                        new GameEventUseDone(player.Session, WeenieError.YouArentTrainedInLockpicking)
                    );
                    return;
                }

                var difficulty = (uint)(ResistLockpick ?? 0);
                if (unlocker.WeenieType == WeenieType.Lockpick)
                {
                    var lockpickSkill = player.GetCreatureSkill(Skill.Lockpick);
                    var effectiveLockpickSkill = UnlockerHelper.GetEffectiveLockpickSkill(player, unlocker);

                    var pickChance = SkillCheck.GetSkillChance(effectiveLockpickSkill, difficulty);

                    var success = false;
                    var chance = ThreadSafeRandom.Next(0.0f, 1.0f);
                    if (chance < pickChance)
                    {
                        success = true;
                        Proficiency.OnSuccessUse(player, lockpickSkill, difficulty);
                        Disarm();
                        EnqueueBroadcast(new GameMessageSound(Guid, Sound.LockSuccess, 1.0f));
                    }
                    else
                    {
                        EnqueueBroadcast(new GameMessageSound(Guid, Sound.PicklockFail, 1.0f));
                    }

                    UnlockerHelper.SendDisarmResultMessage(
                        player,
                        UnlockerHelper.ConsumeUnlocker(player, unlocker, this),
                        this,
                        success
                    );
                }
                else
                {
                    player.Session.Network.EnqueueSend(
                        new GameEventUseDone(player.Session, WeenieError.YouCannotLockOrUnlockThat)
                    );
                }
            }
        );

        chain.EnqueueChain();
    }
}
