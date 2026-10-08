using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using ACE.Common;
using ACE.Database;
using ACE.DatLoader;
using ACE.DatLoader.FileTypes;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Entity.Models;
using ACE.Server.Entity;
using ACE.Server.Entity.Actions;
using ACE.Server.Factories;
using ACE.Server.Factories.Entity;
using ACE.Server.Managers;
using ACE.Server.Network.GameEvent.Events;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.Network.Structure;
using ACE.Server.Physics;
using ACE.Server.Physics.Extensions;
using ACE.Server.WorldObjects.Managers;

namespace ACE.Server.WorldObjects;

partial class WorldObject
{
    /// <summary>
    /// RATING - Nullification: Ramping boost spell defense.
    /// Spell damage taken reduced by up to 20% + 1% per rating. (after quest stamp build up of 25% per spell hit received)
    /// (JEWEL - Amethyst)
    /// </summary>
    private static float CheckForRatingNullificationBoostDefenseBonus(Creature targetCreature)
    {
        if (targetCreature is not Player targetPlayer)
        {
            return 1.0f;
        }

        if (targetPlayer.GetEquippedAndActivatedItemRatingSum(PropertyInt.GearNullification) <= 0)
        {
            return 1.0f;
        }

        const float baseMod = 0.2f;
        const float bonusPerRating = 0.01f;
        var rating = targetPlayer.GetEquippedAndActivatedItemRatingSum(PropertyInt.GearNullification);
        var finalRating = baseMod + bonusPerRating * rating;

        var rampMod = (float)targetPlayer.QuestManager.GetCurrentSolves($"{targetPlayer.Name},Nullification") / 100;

        return 1.0f - rampMod * finalRating;
    }

    /// <summary>
    /// Returns the boost resistance type for a vital
    /// </summary>
    private static ResistanceType GetBoostResistanceType(PropertyAttribute2nd vital)
    {
        switch (vital)
        {
            case PropertyAttribute2nd.Health:
                return ResistanceType.HealthBoost;
            case PropertyAttribute2nd.Stamina:
                return ResistanceType.StaminaBoost;
            case PropertyAttribute2nd.Mana:
                return ResistanceType.ManaBoost;
            default:
                return ResistanceType.Undef;
        }
    }

    /// <summary>
    /// Returns the drain resistance type for a vital
    /// </summary>
    private static ResistanceType GetDrainResistanceType(PropertyAttribute2nd vital)
    {
        switch (vital)
        {
            case PropertyAttribute2nd.Health:
                return ResistanceType.HealthDrain;
            case PropertyAttribute2nd.Stamina:
                return ResistanceType.StaminaDrain;
            case PropertyAttribute2nd.Mana:
                return ResistanceType.ManaDrain;
            default:
                return ResistanceType.Undef;
        }
    }

    /// <summary>
    /// Handles casting SpellType.Transfer spells
    /// usually for Life Magic, ie. Stamina to Mana, Drain
    /// </summary>
    private void HandleCastSpell_Transfer(Spell spell, Creature targetCreature, bool showMsg = true, WorldObject weapon = null, bool fromProc = false)
    {
        var player = this as Player;
        var creature = this as Creature;

        var targetPlayer = targetCreature as Player;

        // prevent double deaths from indirect casts
        // caster is already checked in player/monster, and re-checking caster here would break death emotes such as bunny smite
        if (targetCreature != null && targetCreature.IsDead)
        {
            return;
        }

        // source and destination can be the same creature, or different creatures
        var caster = this as Creature;
        var transferSource = spell.TransferFlags.HasFlag(TransferFlags.CasterSource) ? caster : targetCreature;
        var destination = spell.TransferFlags.HasFlag(TransferFlags.CasterDestination) ? caster : targetCreature;

        // Calculate vital changes
        uint srcVitalChange = 0,
            destVitalChange;

        // Drain Resistances - allows one to partially resist drain health/stamina/mana and harm attacks (not including other life transfer spells).
        var isDrain = spell.TransferFlags.HasFlag(TransferFlags.TargetSource | TransferFlags.CasterDestination);
        if (transferSource != null)
        {
            var drainMod = isDrain ? (float)transferSource.GetResistanceMod(GetDrainResistanceType(spell.Source)) : 1.0f;

            srcVitalChange = (uint)
                Math.Round(transferSource.GetCreatureVital(spell.Source).Current * spell.Proportion * drainMod);
        }

        // TransferCap caps both srcVitalChange and destVitalChange
        // https://asheron.fandom.com/wiki/Announcements_-_2003/01_-_The_Slumbering_Giant#Letter_to_the_Players

        if (spell.TransferCap != 0 && srcVitalChange > spell.TransferCap)
        {
            srcVitalChange = (uint)spell.TransferCap;
        }

        // should healing resistances be applied here?
        if (destination != null)
        {
            var boostMod = isDrain ? (float)destination.GetResistanceMod(GetBoostResistanceType(spell.Destination)) : 1.0f;

            destVitalChange = (uint)Math.Round(srcVitalChange * (1.0f - spell.LossPercent) * boostMod);

            // scale srcVitalChange to destVitalChange?
            var missingDest = destination.GetCreatureVital(spell.Destination).Missing;

            var maxDestVitalChange = missingDest;
            if (spell.TransferCap != 0 && maxDestVitalChange > spell.TransferCap)
            {
                maxDestVitalChange = (uint)spell.TransferCap;
            }

            if (destVitalChange > maxDestVitalChange)
            {
                var scalar = (float)maxDestVitalChange / destVitalChange;

                srcVitalChange = (uint)Math.Round(srcVitalChange * scalar);
                destVitalChange = maxDestVitalChange;
            }

            var vitalTransferRatingBonus = CheckForRatingVitalsTransferBonus(player);
            srcVitalChange = Convert.ToUInt32(srcVitalChange * vitalTransferRatingBonus);
            destVitalChange = Convert.ToUInt32(destVitalChange * vitalTransferRatingBonus);

            ResetRatingElementalistQuestStamps(player);

            // RATING - Nullification only reduces spell damage taken, so it doesn't apply to beneficial transfers
            var nullificationRatingBonus = isDrain ? CheckForRatingNullificationBoostDefenseBonus(targetPlayer) : 1.0f;
            srcVitalChange = Convert.ToUInt32(srcVitalChange * nullificationRatingBonus);
            destVitalChange = Convert.ToUInt32(destVitalChange * nullificationRatingBonus);

            // handle cloak damage procs for drain health other
            var equippedCloak = targetCreature?.EquippedCloak;

            if (isDrain && spell.Source == PropertyAttribute2nd.Health)
            {
                if (targetCreature != null)
                {
                    var percent = (float)srcVitalChange / targetCreature.Health.MaxValue;

                    if (equippedCloak != null && Cloak.HasDamageProc(equippedCloak) && Cloak.RollProc(equippedCloak, percent))
                    {
                        var reduced = Cloak.GetReducedAmount(this, srcVitalChange);

                        Cloak.ShowMessage(targetCreature, this, srcVitalChange, reduced);

                        srcVitalChange = reduced;
                        destVitalChange = (uint)Math.Round(srcVitalChange * (1.0f - spell.LossPercent) * boostMod);
                    }
                }

                // COMBAT ABILITY - Phalanx: health drained by a full hit reduced by 30%. Partial resists are unaffected.
                if (targetPlayer is { PhalanxIsEffective: true } && _partialEvasion == PartialEvasion.None)
                {
                    var phalanxMod = targetPlayer.GetPhalanxFullHitDamageMod();

                    srcVitalChange = (uint)Math.Round(srcVitalChange * phalanxMod);
                    destVitalChange = (uint)Math.Round(destVitalChange * phalanxMod);
                }
            }

            string srcVital = null, destVital;

            // Restoration Spell Mod
            if (weapon is { WeaponRestorationSpellsMod: > 1 })
            {
                var weaponRestorationMod = weapon.WeaponRestorationSpellsMod;

                destVitalChange = Convert.ToUInt32(destVitalChange * weaponRestorationMod);
            }

            // COMBAT ABILITIES: Spell Effectiveness Mods
            if (player != null)
            {
                var overloadMod = CheckForCombatAbilityOverloadDamageBonus(player);
                var batterMod = CheckForCombatAbilityBatteryDamagePenalty(player);

                destVitalChange = (uint)(destVitalChange * overloadMod * batterMod);
            }

            // Archetype Mod
            if (creature is not null)
            {
                var archetypeSpellDamageMod = (float)(creature.ArchetypeSpellDamageMultiplier ?? 1.0);
                destVitalChange = Convert.ToUInt32(destVitalChange * archetypeSpellDamageMod);
            }

            // LEVEL SCALING - Reduce Drain effectiveness vs. monsters, and increase vs. player
            if (spell.TransferFlags.HasFlag(TransferFlags.TargetSource | TransferFlags.CasterDestination))
            {
                var levelScalingMod = LevelScaling.GetPlayerBoostSpellScalar(player, targetCreature);

                srcVitalChange = (uint)(srcVitalChange * levelScalingMod);
                destVitalChange = (uint)(destVitalChange * levelScalingMod);
            }

            // for traps and creatures the archetype system doesn't scale,
            // make sure they receive multipliers from landblock mods
            destVitalChange = Convert.ToUInt32(destVitalChange * GetLandblockLethalitySpellMod());

            // Apply the change in vitals to the source
            if (transferSource != null)
            {
                switch (spell.Source)
                {
                    case PropertyAttribute2nd.Mana:
                        srcVital = "mana";
                        srcVitalChange = (uint)-transferSource.UpdateVitalDelta(transferSource.Mana, -(int)srcVitalChange);
                        break;
                    case PropertyAttribute2nd.Stamina:
                        srcVital = "stamina";
                        srcVitalChange = (uint)-transferSource.UpdateVitalDelta(transferSource.Stamina, -(int)srcVitalChange);
                        break;
                    default: // Health
                        srcVital = "health";

                        // COMBAT ABILITY - Mana Barrier: part of the drained health is taken as mana instead
                        if (isDrain && transferSource is Player { ManaBarrierIsActive: true } barrierPlayer)
                        {
                            srcVitalChange = Player.CombatAbilityManaBarrier(barrierPlayer, srcVitalChange, this, DamageType.Health);
                        }
                        else
                        {
                            srcVitalChange = (uint)-transferSource.UpdateVitalDelta(transferSource.Health, -(int)srcVitalChange);

                            transferSource.DamageHistory.Add(this, DamageType.Health, srcVitalChange);
                        }
                        break;
                }

                // Determine if this drain/infuse should increase the charge meter. Self-transfer does not increase charge.
                var shouldIncreaseCharge =
                    destVitalChange > 0 &&
                    (
                        transferSource is not Player ||
                        (transferSource is Player && destination != transferSource)
                    );

                if (shouldIncreaseCharge &&
                    player is { OverloadStanceIsActive: true } or { BatteryStanceIsActive: true })
                {
                    player.IncreaseChargedMeter(spell, fromProc);
                }
            }

            // Apply the scaled change in vitals to the caster
            switch (spell.Destination)
            {
                case PropertyAttribute2nd.Mana:
                    destVital = "mana";
                    destVitalChange = (uint)destination.UpdateVitalDelta(destination.Mana, destVitalChange);
                    break;
                case PropertyAttribute2nd.Stamina:
                    destVital = "stamina";
                    destVitalChange = (uint)destination.UpdateVitalDelta(destination.Stamina, destVitalChange);
                    break;
                default: // Health
                    destVital = "health";
                    destVitalChange = (uint)destination.UpdateVitalDelta(destination.Health, destVitalChange);

                    destination.DamageHistory.OnHeal(destVitalChange);

                    //var destPlayer = destination as Player;
                    //if (destPlayer != null && destPlayer.Fellowship != null)
                    //destPlayer.Fellowship.OnVitalUpdate(destPlayer);

                    break;
            }

            if (transferSource != player && srcVitalChange > 0)
            {
                HandlePostDamageRatingEffects(transferSource, srcVitalChange, player, targetPlayer, creature, spell, ProjectileSpellType.Undef);
            }
            else if (targetPlayer == player && destVitalChange >= 0)
            {
                HandlePostHealRatingEffects(player, targetPlayer);
            }

            // You gain 52 points of health due to casting Drain Health Other I on Olthoi Warrior
            // You lose 22 points of mana due to casting Incantation of Infuse Mana Other on High-Voltage VI
            // You lose 12 points of mana due to Zofrit Zefir casting Drain Mana Other II on you

            // You cast Stamina to Mana Self I on yourself and lose 50 points of stamina and also gain 45 points of mana
            // You cast Stamina to Health Self VI on yourself and fail to affect your  stamina and also gain 1 point of health

            // unverified:
            // You gain X points of vital due to caster casting spell on you
            // You lose X points of vital due to caster casting spell on you

            var playerSource = transferSource as Player;
            var playerDestination = destination as Player;

            string sourceMsg = null, targetMsg = null;

            var chargedMsg = "";

            if (player is { OverloadStanceIsActive: true } or { BatteryStanceIsActive: true })
            {
                var chargedPercent = Math.Round(player.ManaChargeMeter * 100);
                chargedMsg = $"{chargedPercent}% Charged! ";
            }

            chargedMsg = player switch
            {
                { OverloadDischargeIsActive: true } => "Overload Discharge! ",
                { BatteryDischargeIsActive: true } => "Battery Discharge! ",
                _ => chargedMsg
            };

            if (playerSource != null && playerDestination != null && transferSource.Guid == destination.Guid)
            {
                sourceMsg = $"{chargedMsg}You cast {spell.Name} on yourself and lose {srcVitalChange} points of {srcVital} and also gain {destVitalChange} points of {destVital}";
            }
            else
            {
                if (playerSource != null)
                {
                    if (transferSource == this)
                    {
                        sourceMsg = $"{chargedMsg}You lose {srcVitalChange} points of {srcVital} due to casting {spell.Name} on {targetCreature.Name}";
                    }
                    else
                    {
                        targetMsg = $"{chargedMsg}You lose {srcVitalChange} points of {srcVital} due to {caster.Name} casting {spell.Name} on you";
                    }

                    if (destination != null)
                    {
                        playerSource.SetCurrentAttacker(destination);
                    }
                }

                if (playerDestination != null)
                {
                    if (destination == this)
                    {
                        sourceMsg = $"{chargedMsg}You gain {destVitalChange} points of {destVital} due to casting {spell.Name} on {targetCreature.Name}";
                    }
                    else
                    {
                        targetMsg = $"{chargedMsg}You gain {destVitalChange} points of {destVital} due to {caster.Name} casting {spell.Name} on you";
                    }
                }
            }

            if (player != null && sourceMsg != null && showMsg)
            {
                player.SendChatMessage(player, sourceMsg, ChatMessageType.Magic);
            }

            if (targetPlayer != null && targetMsg != null && showMsg)
            {
                targetPlayer.SendChatMessage(caster, targetMsg, ChatMessageType.Magic);
            }

            if (isDrain && targetCreature.IsAlive && spell.Source == PropertyAttribute2nd.Health)
            {
                // handle cloak spell proc
                if (equippedCloak != null && Cloak.HasProcSpell(equippedCloak))
                {
                    var pct = (float)srcVitalChange / targetCreature.Health.MaxValue;

                    // ensure message is sent after enchantment.Message
                    var actionChain = new ActionChain();
                    actionChain.AddDelayForOneTick();
                    actionChain.AddAction(this, () => Cloak.TryProcSpell(targetCreature, this, equippedCloak, pct));
                    actionChain.EnqueueChain();
                }

                // ensure emote process occurs after damage msg
                var emoteChain = new ActionChain();
                emoteChain.AddDelayForOneTick();
                emoteChain.AddAction(targetCreature, () => targetCreature.EmoteManager.OnDamage(creature));
                //if (critical)
                //    emoteChain.AddAction(targetCreature, () => targetCreature.EmoteManager.OnReceiveCritical(creature));
                emoteChain.EnqueueChain();
            }
        }

        HandleBoostTransferDeath(creature, targetCreature);
    }

    /// <summary>
    /// COMBAT ABILITY - Battery: Decrease vital transfer effectiveness.
    /// </summary>
    private static uint CheckForCombatAbilityBatteryVitalTransferPenalty(CombatAbility combatAbility, Player player, uint srcVitalChange, ref uint destVitalChange)
    {
        if (!player.BatteryDischargeIsActive)
        {
            return srcVitalChange;
        }

        var maxMana = (float)player.Mana.MaxValue;
        var currentMana = (float)player.Mana.Current == 0 ? 1 : (float)player.Mana.Current;

        if ((currentMana / maxMana) < 0.75)
        {
            var newMax = maxMana * 0.75;
            var batteryMod = 1f - 0.25f * ((newMax - currentMana) / newMax);

            srcVitalChange = (uint)(srcVitalChange * batteryMod);
            destVitalChange = (uint)(destVitalChange * batteryMod);
        }

        return srcVitalChange;
    }

    /// <summary>
    /// RATING - Vitals Trasfer: +2% boost effecitveness per rating to transfer spells
    /// (JEWEL - Rose Quartz)
    /// </summary>
    private static float CheckForRatingVitalsTransferBonus(Player player)
    {
        if (player == null)
        {
            return 1.0f;
        }

        if (player.GetEquippedAndActivatedItemRatingSum(PropertyInt.GearVitalsTransfer) <= 0)
        {
            return 1.0f;
        }

        var ratingMod = player.GetEquippedAndActivatedItemRatingSum(PropertyInt.GearVitalsTransfer) * 0.02f;

        return 1.0f + ratingMod;
    }
}
