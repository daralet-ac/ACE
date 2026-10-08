using System;
using System.Collections.Generic;
using System.Linq;
using ACE.Common;
using ACE.DatLoader.Entity.AnimationHooks;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Entity.Models;
using ACE.Server.Factories.Tables;
using ACE.Server.Managers;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.WorldObjects;
using ACE.Server.WorldObjects.Entity;
using Serilog;
using Time = ACE.Common.Time;

namespace ACE.Server.Entity;

public partial class DamageEvent
{
    /// <summary>
    /// RATING - Thorns: Reflects a percentage of a blocked attack's damage back to a close-range attacker
    /// (JEWEL - White Quartz)
    /// </summary>
    private void CheckForRatingThorns(Creature attacker, Creature defender, WorldObject damageSource)
    {
        var playerDefender = defender as Player;

        if (Blocked != true || playerDefender == null || !(attacker.GetDistance(playerDefender) < 10))
        {
            return;
        }

        if (playerDefender.GetEquippedAndActivatedItemRatingSum(PropertyInt.GearThorns) <= 0)
        {
            return;
        }

        if (damageSource is null)
        {
            return;
        }

        // the damage the blocked attack would have dealt, before mitigation
        var blockedAttack = CreateReactiveDamageEvent(attacker, defender, damageSource);

        if (blockedAttack._generalFailure)
        {
            return;
        }

        var thornsAmount = blockedAttack.GetNonCriticalDamageBeforeMitigation() * Jewel.GetJewelEffectMod(playerDefender, PropertyInt.GearThorns);

        var damageDealt = ApplyReactiveDamage(playerDefender, attacker, blockedAttack.DamageType, thornsAmount);

        if (damageDealt is null)
        {
            return;
        }

        playerDefender.ShieldReprisal = damageDealt;

        var msg = $"You deflect {damageDealt} damage back to the attacker!";
        playerDefender.Session.Network.EnqueueSend(new GameMessageSystemChat(msg, ChatMessageType.CombatSelf));
    }

    public void CheckForRiposte(Creature attacker, Creature defender)
    {
        if ((!Parried && !Blocked) || defender is not Player playerDefender || !(attacker.GetDistance(playerDefender) < 10))
        {
            return;
        }

        if (!playerDefender.RiposteIsActive)
        {
            return;
        }

        if (!playerDefender.TwoHandedCombat && !playerDefender.IsDualWieldAttack && playerDefender.GetEquippedShield() is null)
        {
            return;
        }

        var riposteWeapon = defender.GetEquippedWeapon();

        if (riposteWeapon is null)
        {
            return;
        }

        var riposte = CreateReactiveDamageEvent(defender, attacker, riposteWeapon, powerMod: 1.0f);

        if (riposte._generalFailure)
        {
            return;
        }

        var baseDamage = riposte.GetNonCriticalDamageBeforeMitigation();
        var mitigation = riposte.GetMitigation(defender, attacker);

        // GetMitigation evades the riposte if the attacker has no body part to hit
        if (riposte.Evaded)
        {
            return;
        }

        var damageDealt = ApplyReactiveDamage(playerDefender, attacker, riposte.DamageType, baseDamage * mitigation);

        if (damageDealt is null)
        {
            return;
        }

        var parryType = Parried ? "parry" : "block";

        var msg = $"You follow up your {parryType} with a quick riposte, dealing {damageDealt} {riposte.DamageType} damage to {attacker.Name}!";
        playerDefender.Session.Network.EnqueueSend(new GameMessageSystemChat(msg, ChatMessageType.CombatSelf));
    }

    /// <summary>
    /// Calculates reactive damage (Thorns, Riposte) in a separate DamageEvent, so this event keeps describing the original attack
    /// </summary>
    private DamageEvent CreateReactiveDamageEvent(Creature source, Creature target, WorldObject damageSource, float? powerMod = null)
    {
        var reactiveDamageEvent = new DamageEvent
        {
            _attackMotion = _attackMotion,
            _attackHook = _attackHook,
            _evasionMod = 1.0f,
        };

        reactiveDamageEvent.SetCombatSources(source, target, damageSource);
        reactiveDamageEvent.SetBaseDamage(source, target, damageSource);
        reactiveDamageEvent.SetDamageModifiers(source, target, powerMod, consumeSneakAttackBonuses: false);

        return reactiveDamageEvent;
    }

    /// <summary>
    /// Deals reactive damage (Thorns, Riposte) to target through the normal damage path, which handles invulnerability, damage history and death
    /// </summary>
    /// <returns>The damage dealt, or null if no damage could be dealt</returns>
    private int? ApplyReactiveDamage(Player source, Creature target, DamageType damageType, float damage)
    {
        if (target.IsDead || target.Invincible || target is Player { UnderLifestoneProtection: true })
        {
            return null;
        }

        if (float.IsNaN(damage) || damage is < 0 or > int.MaxValue)
        {
            _log.Error(
                "ApplyReactiveDamage({Source}, {Target}) - damage ({Damage}) is out of range",
                source.Name,
                target.Name,
                damage
            );
            return null;
        }

        return (int)target.TakeDamage(source, damageType, damage);
    }
}
