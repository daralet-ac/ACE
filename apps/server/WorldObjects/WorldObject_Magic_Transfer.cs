using System;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Entity;
using ACE.Server.Entity.Actions;

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

        // the result of this caster's last resist roll (TryResistSpell)
        var partialEvasion = _partialEvasion;

        // source and destination can be the same creature, or different creatures
        var transferSource = spell.TransferFlags.HasFlag(TransferFlags.CasterSource) ? creature : targetCreature;
        var destination = spell.TransferFlags.HasFlag(TransferFlags.CasterDestination) ? creature : targetCreature;

        // Calculate vital changes
        uint srcVitalChange = 0,
            destVitalChange;

        // Drain Resistances - allows one to partially resist drain health/stamina/mana and harm attacks (not including other life transfer spells).
        var isDrain = spell.TransferFlags.HasFlag(TransferFlags.TargetSource | TransferFlags.CasterDestination);
        if (transferSource != null)
        {
            var drainMod = isDrain ? (float)transferSource.GetResistanceMod(GetDrainResistanceType(GetVitalDamageType(spell.Source))) : 1.0f;

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
            var boostMod = isDrain ? (float)destination.GetResistanceMod(GetBoostResistanceType(GetVitalDamageType(spell.Destination))) : 1.0f;

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
                if (targetPlayer is { PhalanxIsEffective: true } && partialEvasion == PartialEvasion.None)
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
                var overloadMod = CheckForCombatAbilityOverloadDamageMod(player);
                var batterMod = CheckForCombatAbilityBatteryDamageMod(player);

                destVitalChange = (uint)(destVitalChange * overloadMod * batterMod);
            }

            // Archetype Mod
            if (creature is not null)
            {
                var archetypeSpellDamageMod = (float)(creature.ArchetypeSpellDamageMultiplier ?? 1.0);
                destVitalChange = Convert.ToUInt32(destVitalChange * archetypeSpellDamageMod);
            }

            // LEVEL SCALING - Reduce Drain effectiveness vs. monsters, and increase vs. player
            if (isDrain)
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
                        destination != transferSource
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
                    break;
            }

            if (transferSource != player && srcVitalChange > 0)
            {
                HandlePostDamageRatingEffects(transferSource, srcVitalChange, player, targetPlayer, creature, spell, ProjectileSpellType.Undef);
            }
            else if (targetPlayer == player)
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

            var chargedMsg = player?.GetChargedMessage() ?? "";

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
                        targetMsg = $"{chargedMsg}You lose {srcVitalChange} points of {srcVital} due to {creature.Name} casting {spell.Name} on you";
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
                        targetMsg = $"{chargedMsg}You gain {destVitalChange} points of {destVital} due to {creature.Name} casting {spell.Name} on you";
                    }
                }
            }

            if (player != null && sourceMsg != null && showMsg)
            {
                player.SendChatMessage(player, sourceMsg, ChatMessageType.Magic);
            }

            if (targetPlayer != null && targetMsg != null && showMsg)
            {
                targetPlayer.SendChatMessage(creature, targetMsg, ChatMessageType.Magic);
            }

            if (isDrain && targetCreature.IsAlive && spell.Source == PropertyAttribute2nd.Health)
            {
                var damagePercent = (float)srcVitalChange / targetCreature.Health.MaxValue;
                ScheduleSpellDamageReactions(targetCreature, creature, equippedCloak, damagePercent);
            }
        }

        HandleBoostTransferDeath(creature, targetCreature);
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
