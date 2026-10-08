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
    /// Who and what is involved in casting a vital transfer spell (ie. Stamina to Mana, Drain)
    /// </summary>
    private readonly struct TransferCast
    {
        public readonly Spell Spell;
        public readonly Creature Target;
        public readonly Player TargetPlayer;

        /// <summary>
        /// The creature the vital is taken from: the caster or the target
        /// </summary>
        public readonly Creature Source;

        /// <summary>
        /// The creature the vital is given to: the caster or the target
        /// </summary>
        public readonly Creature Destination;

        /// <summary>
        /// Takes from the target and gives to the caster (ie. Drain Health Other)
        /// </summary>
        public readonly bool IsDrain;

        public readonly WorldObject Weapon;
        public readonly bool FromProc;

        /// <summary>
        /// The result of the caster's last resist roll (TryResistSpell)
        /// </summary>
        public readonly PartialEvasion PartialEvasion;

        public TransferCast(
            WorldObject caster,
            Spell spell,
            Creature target,
            WorldObject weapon,
            bool fromProc,
            PartialEvasion partialEvasion
        )
        {
            var casterCreature = caster as Creature;

            Spell = spell;
            Target = target;
            TargetPlayer = target as Player;

            // source and destination can be the same creature, or different creatures
            Source = spell.TransferFlags.HasFlag(TransferFlags.CasterSource) ? casterCreature : target;
            Destination = spell.TransferFlags.HasFlag(TransferFlags.CasterDestination) ? casterCreature : target;

            IsDrain = spell.TransferFlags.HasFlag(TransferFlags.TargetSource | TransferFlags.CasterDestination);
            Weapon = weapon;
            FromProc = fromProc;
            PartialEvasion = partialEvasion;
        }
    }

    /// <summary>
    /// Handles casting SpellType.Transfer spells
    /// usually for Life Magic, ie. Stamina to Mana, Drain
    /// </summary>
    private void HandleCastSpell_Transfer(Spell spell, Creature targetCreature, bool showMsg = true, WorldObject weapon = null, bool fromProc = false)
    {
        var creature = this as Creature;

        // prevent double deaths from indirect casts
        // caster is already checked in player/monster, and re-checking caster here would break death emotes such as bunny smite
        if (targetCreature != null && targetCreature.IsDead)
        {
            return;
        }

        var cast = new TransferCast(this, spell, targetCreature, weapon, fromProc, _partialEvasion);

        var srcVitalChange = GetTransferSourceAmount(cast);

        if (cast.Destination == null)
        {
            HandleBoostTransferDeath(creature, targetCreature);
            return;
        }

        var destVitalChange = GetTransferDestinationAmount(cast, ref srcVitalChange, out var boostMod);

        ApplyTransferRatingMods(cast, ref srcVitalChange, ref destVitalChange);

        // handle cloak damage procs for drain health other
        var equippedCloak = targetCreature?.EquippedCloak;

        if (cast.IsDrain && spell.Source == PropertyAttribute2nd.Health)
        {
            ApplyHealthDrainDefenses(cast, equippedCloak, boostMod, ref srcVitalChange, ref destVitalChange);
        }

        ApplyTransferMods(cast, ref srcVitalChange, ref destVitalChange);

        string srcVital = null;

        if (cast.Source != null)
        {
            srcVitalChange = TakeFromTransferSource(cast, srcVitalChange, destVitalChange, out srcVital);
        }

        destVitalChange = GiveToTransferDestination(cast, destVitalChange, out var destVital);

        var player = this as Player;

        if (cast.Source != player && srcVitalChange > 0)
        {
            HandlePostDamageRatingEffects(cast.Source, srcVitalChange, player, cast.TargetPlayer, creature, spell, ProjectileSpellType.Undef);
        }
        else if (cast.TargetPlayer == player)
        {
            HandlePostHealRatingEffects(player, cast.TargetPlayer);
        }

        SendTransferMessages(cast, srcVitalChange, destVitalChange, srcVital, destVital, showMsg);

        if (cast.IsDrain && targetCreature.IsAlive && spell.Source == PropertyAttribute2nd.Health)
        {
            var damagePercent = (float)srcVitalChange / targetCreature.Health.MaxValue;
            ScheduleSpellDamageReactions(targetCreature, creature, equippedCloak, damagePercent);
        }

        HandleBoostTransferDeath(creature, targetCreature);
    }

    /// <summary>
    /// The amount to take from the source: a proportion of its current vital, up to the spell's transfer cap.
    /// A drain can be partially resisted.
    /// </summary>
    private static uint GetTransferSourceAmount(in TransferCast cast)
    {
        var spell = cast.Spell;

        uint srcVitalChange = 0;

        // Drain Resistances - allows one to partially resist drain health/stamina/mana and harm attacks (not including other life transfer spells).
        if (cast.Source != null)
        {
            var drainMod = cast.IsDrain ? (float)cast.Source.GetResistanceMod(GetDrainResistanceType(GetVitalDamageType(spell.Source))) : 1.0f;

            srcVitalChange = (uint)
                Math.Round(cast.Source.GetCreatureVital(spell.Source).Current * spell.Proportion * drainMod);
        }

        // TransferCap caps both srcVitalChange and destVitalChange
        // https://asheron.fandom.com/wiki/Announcements_-_2003/01_-_The_Slumbering_Giant#Letter_to_the_Players

        if (spell.TransferCap != 0 && srcVitalChange > spell.TransferCap)
        {
            srcVitalChange = (uint)spell.TransferCap;
        }

        return srcVitalChange;
    }

    /// <summary>
    /// The amount to give the destination: the source amount less the spell's loss (a drain's can be partially resisted),
    /// up to the destination's missing vital and the transfer cap. When capped, the source amount scales down to match.
    /// </summary>
    /// <param name="boostMod">The destination's boost resistance, for a drain</param>
    private static uint GetTransferDestinationAmount(in TransferCast cast, ref uint srcVitalChange, out float boostMod)
    {
        var spell = cast.Spell;
        var destination = cast.Destination;

        // should healing resistances be applied here?
        boostMod = cast.IsDrain ? (float)destination.GetResistanceMod(GetBoostResistanceType(GetVitalDamageType(spell.Destination))) : 1.0f;

        var destVitalChange = (uint)Math.Round(srcVitalChange * (1.0f - spell.LossPercent) * boostMod);

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

        return destVitalChange;
    }

    /// <summary>
    /// RATING - Vitals Transfer for the caster, and RATING - Nullification for the target of a drain
    /// </summary>
    private void ApplyTransferRatingMods(in TransferCast cast, ref uint srcVitalChange, ref uint destVitalChange)
    {
        var player = this as Player;

        var vitalTransferRatingBonus = CheckForRatingVitalsTransferBonus(player);
        srcVitalChange = Convert.ToUInt32(srcVitalChange * vitalTransferRatingBonus);
        destVitalChange = Convert.ToUInt32(destVitalChange * vitalTransferRatingBonus);

        ResetRatingElementalistQuestStamps(player);

        // RATING - Nullification only reduces spell damage taken, so it doesn't apply to beneficial transfers
        var nullificationRatingBonus = cast.IsDrain ? CheckForRatingNullificationBoostDefenseBonus(cast.TargetPlayer) : 1.0f;
        srcVitalChange = Convert.ToUInt32(srcVitalChange * nullificationRatingBonus);
        destVitalChange = Convert.ToUInt32(destVitalChange * nullificationRatingBonus);
    }

    /// <summary>
    /// Draining another's health: the target's cloak may proc to reduce it,
    /// and COMBAT ABILITY - Phalanx reduces a full hit
    /// </summary>
    private void ApplyHealthDrainDefenses(
        in TransferCast cast,
        WorldObject equippedCloak,
        float boostMod,
        ref uint srcVitalChange,
        ref uint destVitalChange
    )
    {
        var targetCreature = cast.Target;

        if (targetCreature != null)
        {
            var percent = (float)srcVitalChange / targetCreature.Health.MaxValue;

            if (equippedCloak != null && Cloak.HasDamageProc(equippedCloak) && Cloak.RollProc(equippedCloak, percent))
            {
                var reduced = Cloak.GetReducedAmount(this, srcVitalChange);

                Cloak.ShowMessage(targetCreature, this, srcVitalChange, reduced);

                srcVitalChange = reduced;
                destVitalChange = (uint)Math.Round(srcVitalChange * (1.0f - cast.Spell.LossPercent) * boostMod);
            }
        }

        // COMBAT ABILITY - Phalanx: health drained by a full hit reduced by 30%. Partial resists are unaffected.
        if (cast.TargetPlayer is { PhalanxIsEffective: true } && cast.PartialEvasion == PartialEvasion.None)
        {
            var phalanxMod = cast.TargetPlayer.GetPhalanxFullHitDamageMod();

            srcVitalChange = (uint)Math.Round(srcVitalChange * phalanxMod);
            destVitalChange = (uint)Math.Round(destVitalChange * phalanxMod);
        }
    }

    /// <summary>
    /// The multipliers for the amount given: the weapon's restoration mod, COMBAT ABILITY - Overload/Battery,
    /// the caster's archetype and the dungeon mod. A drain's level scaling applies to both amounts.
    /// </summary>
    private void ApplyTransferMods(in TransferCast cast, ref uint srcVitalChange, ref uint destVitalChange)
    {
        var player = this as Player;
        var creature = this as Creature;

        // Restoration Spell Mod
        if (cast.Weapon is { WeaponRestorationSpellsMod: > 1 })
        {
            var weaponRestorationMod = cast.Weapon.WeaponRestorationSpellsMod;

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
        if (cast.IsDrain)
        {
            var levelScalingMod = LevelScaling.GetPlayerBoostSpellScalar(player, cast.Target);

            srcVitalChange = (uint)(srcVitalChange * levelScalingMod);
            destVitalChange = (uint)(destVitalChange * levelScalingMod);
        }

        // for traps and creatures the archetype system doesn't scale,
        // make sure they receive multipliers from landblock mods
        destVitalChange = Convert.ToUInt32(destVitalChange * GetLandblockLethalitySpellMod());
    }

    /// <summary>
    /// Takes the amount from the source, returning what was actually taken.
    /// Health taken by a drain is damage: COMBAT ABILITY - Mana Barrier may take part of it as mana.
    /// </summary>
    private uint TakeFromTransferSource(in TransferCast cast, uint srcVitalChange, uint destVitalChange, out string srcVital)
    {
        var player = this as Player;
        var transferSource = cast.Source;

        switch (cast.Spell.Source)
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
                if (cast.IsDrain && transferSource is Player { ManaBarrierIsActive: true } barrierPlayer)
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
                cast.Destination != transferSource
            );

        if (shouldIncreaseCharge &&
            player is { OverloadStanceIsActive: true } or { BatteryStanceIsActive: true })
        {
            player.IncreaseChargedMeter(cast.Spell, cast.FromProc);
        }

        return srcVitalChange;
    }

    /// <summary>
    /// Gives the amount to the destination, returning what it actually gained
    /// </summary>
    private static uint GiveToTransferDestination(in TransferCast cast, uint destVitalChange, out string destVital)
    {
        var destination = cast.Destination;

        switch (cast.Spell.Destination)
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

        return destVitalChange;
    }

    /// <summary>
    /// Tells the caster and the target what the spell took and gave
    /// </summary>
    private void SendTransferMessages(
        in TransferCast cast,
        uint srcVitalChange,
        uint destVitalChange,
        string srcVital,
        string destVital,
        bool showMsg
    )
    {
        var player = this as Player;
        var creature = this as Creature;
        var spell = cast.Spell;
        var transferSource = cast.Source;
        var destination = cast.Destination;

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
                    sourceMsg = $"{chargedMsg}You lose {srcVitalChange} points of {srcVital} due to casting {spell.Name} on {cast.Target.Name}";
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
                    sourceMsg = $"{chargedMsg}You gain {destVitalChange} points of {destVital} due to casting {spell.Name} on {cast.Target.Name}";
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

        if (cast.TargetPlayer != null && targetMsg != null && showMsg)
        {
            cast.TargetPlayer.SendChatMessage(creature, targetMsg, ChatMessageType.Magic);
        }
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
