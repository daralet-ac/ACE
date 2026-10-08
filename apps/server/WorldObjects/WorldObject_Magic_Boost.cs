using System;
using ACE.Common;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Entity;
using ACE.Server.Entity.Actions;

namespace ACE.Server.WorldObjects;

partial class WorldObject
{
    /// <summary>
    /// Handles casting SpellType.Boost / FellowBoost spells
    /// typically for Life Magic, ie. Heal, Harm
    /// </summary>
    /// <param name="reflectedCaster">
    /// COMBAT ABILITY - Reflect: when set, this spell was reflected back at its original caster.
    /// It is still cast by this player (kill credit, threat, messages), but its damage is based on the original caster's stats.
    /// </param>
    private void HandleCastSpell_Boost(
        Spell spell,
        Creature targetCreature,
        bool fromProc,
        bool showMsg = true,
        WorldObject weapon = null,
        double damageMultiplier = 1.0,
        Creature reflectedCaster = null
    )
    {
        var player = this as Player;
        var creature = this as Creature;
        var targetPlayer = targetCreature as Player;

        // the creature whose stats are used for damage
        var damageSource = reflectedCaster ?? creature;
        var damageSourcePlayer = damageSource as Player;

        // prevent double deaths from indirect casts
        // caster is already checked in player/monster, and re-checking caster here would break death emotes such as bunny smite
        if (targetCreature != null && targetCreature.IsDead)
        {
            return;
        }

        // handle negatives?
        var minBoostValue = Math.Min(spell.Boost, spell.MaxBoost);
        var maxBoostValue = Math.Max(spell.Boost, spell.MaxBoost);

        var resistanceType =
            minBoostValue > 0
                ? GetBoostResistanceType(spell.VitalDamageType)
                : GetDrainResistanceType(spell.VitalDamageType);

        double? weaponRestorationMod = 1.0;
        if (weapon is { WeaponRestorationSpellsMod: > 1 })
        {
            weaponRestorationMod = weapon.WeaponRestorationSpellsMod;
        }

        // COMBAT ABILITY - Reflect: a reflected spell never damages the reflecting player.
        // A spell that was already reflected can't be reflected again.
        if (
            reflectedCaster == null
            && CheckForCombatAbilityReflectSpell(_partialEvasion is PartialEvasion.All or PartialEvasion.Some, targetPlayer, creature, spell)
        )
        {
            targetPlayer.CastReflectedSpell(spell, creature, null, damageMultiplier);
            return;
        }

        // Resist
        var resistedMod = GetResistedMod(_partialEvasion);

        var selfTargetProcSpellMod = SelfTargetSpellProcMod(fromProc, spell, weapon, player);

        var tryBoost = (int)(
            ThreadSafeRandom.Next(minBoostValue, maxBoostValue) * weaponRestorationMod * selfTargetProcSpellMod
        );

        // Boost Crits
        var critMessage = "";
        var critChance = 0.1;
        if (weapon != null && weapon.CriticalFrequency != null)
        {
            critChance += (double)weapon.CriticalFrequency;
        }

        var critMultiplier = 1.5f;
        if (weapon?.GetProperty(PropertyFloat.CriticalMultiplier) != null)
        {
            var weaponCritMulti = (float)weapon.GetProperty(PropertyFloat.CriticalMultiplier);
            critMultiplier += (weaponCritMulti / 1.5f);
        }

        var roll = ThreadSafeRandom.Next(0.0f, 1.0f);

        if (critChance > roll)
        {
            tryBoost = (int)(tryBoost * critMultiplier);
            critMessage = "Critical! ";
        }

        if (targetCreature != this && tryBoost < 0)
        {
            var damageRating = damageSource?.GetDamageRating() ?? 0;
            var damageRatingMod = Creature.AdditiveCombine(Creature.GetPositiveRatingMod(damageRating));

            tryBoost = (int)(tryBoost * damageRatingMod);
        }

        if (targetCreature == null)
        {
            return;
        }

        tryBoost = (int)Math.Round(tryBoost * targetCreature.GetResistanceMod(resistanceType));

        int boost;

        // handle cloak damage proc for harm other
        var equippedCloak = targetCreature.EquippedCloak;

        if (targetCreature != this && spell.VitalDamageType == DamageType.Health && tryBoost < 0)
        {
            var percent = (float)-tryBoost / targetCreature.Health.MaxValue;

            if (equippedCloak != null && Cloak.HasDamageProc(equippedCloak) &&
                Cloak.RollProc(equippedCloak, percent))
            {
                var reduced = -Cloak.GetReducedAmount(this, -tryBoost);

                Cloak.ShowMessage(targetCreature, this, -tryBoost, -reduced);

                tryBoost = reduced;
            }
        }

        var overloadMod = CheckForCombatAbilityOverloadDamageMod(damageSourcePlayer);
        var batterMod = CheckForCombatAbilityBatteryDamageMod(damageSourcePlayer);

        // proc spells receive 1% of spellcraft as a damage multiplier (300 spellcraft = x3), same as spell projectiles
        var spellcraftMod = 1.0f;
        if (fromProc && weapon?.ItemSpellcraft != null)
        {
            var spellcraft = weapon.ItemSpellcraft.Value + CheckForArcaneLoreSpecSpellcraftBonus(damageSource);
            spellcraftMod = spellcraft * 0.01f;
        }

        // for traps and creatures the archetype system doesn't scale,
        // make sure they receive multipliers from landblock mods
        var landblockScalingMod = (reflectedCaster ?? this).GetLandblockLethalitySpellMod();

        tryBoost = (int)(tryBoost * overloadMod * batterMod * damageMultiplier * spellcraftMod * landblockScalingMod * resistedMod);

        string srcVital;

        if (tryBoost > 0) // heal
        {
            // increases
            // Selfless Spirit (Lavender Jade): full bonus when restoring others, an equivalent
            // penalty when restoring yourself, and no effect when restoring a pet/monster.
            var selflessnessMod = Jewel.GetJewelEffectMod(player, PropertyInt.GearSelflessness);
            if (selflessnessMod > 0.0f && !targetCreature.IsMonster)
            {
                var selflessnessFactor = targetCreature == this ? 1.0f - selflessnessMod : 1.0f + selflessnessMod;
                tryBoost = Convert.ToInt32(tryBoost * selflessnessFactor);
            }

            tryBoost = Convert.ToInt32(tryBoost * (1.0f + Jewel.GetJewelEffectMod(player, PropertyInt.GearHealBubble)));

            // reductions
            tryBoost = Convert.ToInt32(tryBoost * (1.0f - Jewel.GetJewelEffectMod(player, PropertyInt.GearVitalsTransfer)));
        }
        else // harm
        {
            // increases
            tryBoost = Convert.ToInt32(tryBoost * (1.0f + Jewel.GetJewelRedFury(damageSourcePlayer)));
            tryBoost = Convert.ToInt32(tryBoost * (1.0f + Jewel.GetJewelBlueFury(damageSourcePlayer)));
            tryBoost = Convert.ToInt32(tryBoost * (1.0f + Jewel.GetJewelEffectMod(damageSourcePlayer, PropertyInt.GearSelfHarm)));

            var attributeMod = damageSource?.GetAttributeMod(weapon, true) ?? 1.0f;
            tryBoost = Convert.ToInt32(tryBoost * attributeMod);

            // reductions
            tryBoost = Convert.ToInt32(tryBoost * (1.0f - Jewel.GetJewelEffectMod(targetPlayer, PropertyInt.GearNullification,"Nullification")));

            // ward
            var ignoreWardMod = 1.0f - Jewel.GetJewelEffectMod(damageSourcePlayer, PropertyInt.GearWardPen, "WardPen");
            var wardMod = GetWardMod(damageSource, targetCreature, ignoreWardMod);

            tryBoost = Convert.ToInt32(tryBoost * wardMod);

            // COMBAT ABILITY - Phalanx: health damage taken from full hits reduced by 30%. Partial resists are unaffected.
            if (spell.VitalDamageType == DamageType.Health && _partialEvasion == PartialEvasion.None)
            {
                tryBoost = Convert.ToInt32(tryBoost * (targetPlayer?.GetPhalanxFullHitDamageMod() ?? 1.0f));
            }
        }

        ResetRatingElementalistQuestStamps(player);

        if (damageSource is not null)
        {
            var archetypeSpellDamageMod = (float)(damageSource.ArchetypeSpellDamageMultiplier ?? 1.0);
            tryBoost = Convert.ToInt32(tryBoost * archetypeSpellDamageMod);
        }

        // LEVEL SCALING - Reduces harms against enemies, and restoration for players.
        // Also scales up healing on higher level player targets when both players are Shrouded (excluding self heals).
        var scalar = LevelScaling.GetPlayerBoostSpellScalar(damageSourcePlayer, targetCreature);
        if (tryBoost > 0 && player != null && targetPlayer != null && targetPlayer != player)
        {
            scalar *= LevelScaling.GetPlayerBoostHealScalarShroudedUpward(player, targetPlayer);
        }

        tryBoost = (int)(tryBoost * scalar);

        SigilTrinketSpellDamageReduction = 1.0f;
        targetPlayer?.CheckForSigilTrinketOnSpellHitReceivedEffects(this, spell, tryBoost, Skill.MagicDefense, SigilTrinketMagicDefenseEffect.Absorption);
        tryBoost = Convert.ToInt32(tryBoost * SigilTrinketSpellDamageReduction);

        switch (spell.VitalDamageType)
        {
            case DamageType.Mana:
                boost = targetCreature.UpdateVitalDelta(targetCreature.Mana, tryBoost);
                srcVital = "mana";
                break;
            case DamageType.Stamina:
                boost = targetCreature.UpdateVitalDelta(targetCreature.Stamina, tryBoost);
                srcVital = "stamina";
                break;
            default: // Health
                // COMBAT ABILITY - Mana Barrier: part of the health damage is taken as mana instead.
                // It records the damage in the target's damage history itself.
                var manaBarrier =
                    tryBoost < 0 && targetCreature != this && targetPlayer is { ManaBarrierIsActive: true };

                if (manaBarrier)
                {
                    boost = -(int)Player.CombatAbilityManaBarrier(targetPlayer, (uint)-tryBoost, this, DamageType.Health);
                }
                else
                {
                    boost = targetCreature.UpdateVitalDelta(targetCreature.Health, tryBoost);
                }

                srcVital = "health";

                if (tryBoost >= 0)
                {
                    if (!targetCreature.IsMonster)
                    {
                        targetCreature.DamageHistory.OnHeal((uint)boost);
                        GenerateSupportSpellThreat(spell, targetCreature, boost);

                        if (player is { OverloadStanceIsActive: true } or { BatteryStanceIsActive: true } && boost > 0)
                        {
                            player.IncreaseChargedMeter(spell, fromProc);
                        }
                    }
                }
                else
                {
                    // credits the caster with the damage, for kill credit and death messages
                    if (!manaBarrier)
                    {
                        targetCreature.DamageHistory.Add(this, DamageType.Health, (uint)-boost);
                    }

                    if (creature is { IsMonster: false } && targetCreature.IsMonster)
                    {
                        var percentOfTargetMaxHealth = -boost / (float)targetCreature.Health.MaxValue;
                        targetCreature.IncreaseTargetThreatLevel(player, (int)(percentOfTargetMaxHealth * 1000));

                        if (player is { OverloadStanceIsActive: true } or { BatteryStanceIsActive: true } && boost < 0)
                        {
                            player.IncreaseChargedMeter(spell, fromProc);
                        }
                    }
                }
                break;
        }

        if (boost < 0)
        {
            HandlePostDamageRatingEffects(
                targetCreature,
                -boost,
                player,
                targetPlayer,
                creature,
                spell,
                ProjectileSpellType.Undef
            );
        }
        else if (boost > 0)
        {
            HandlePostHealRatingEffects(player, targetPlayer);
        }

        var partialResist = _partialEvasion == PartialEvasion.Some ? "Partial Resist! " : "";

        if (player != null)
        {
            string casterMessage;

            var chargedMsg = player.GetChargedMessage();

            if (player != targetCreature)
            {
                if (spell.IsBeneficial)
                {
                    casterMessage = $"{partialResist}{chargedMsg}{critMessage}With {spell.Name} you restore {boost} points of {srcVital} to {targetCreature.Name}.";
                }
                else
                {
                    casterMessage = $"{partialResist}{chargedMsg}{critMessage}With {spell.Name} you drain {Math.Abs(boost)} points of {srcVital} from {targetCreature.Name}.";
                }
            }
            else
            {
                var verb = spell.IsBeneficial ? "restore" : "drain";

                casterMessage = $"{partialResist}{chargedMsg}{critMessage}You cast {spell.Name} and {verb} {Math.Abs(boost)} points of your {srcVital}.";
            }

            if (showMsg)
            {
                player.SendChatMessage(player, casterMessage, ChatMessageType.Magic);
            }
        }

        if (targetPlayer != null && player != targetPlayer)
        {
            string targetMessage;

            if (spell.IsBeneficial)
            {
                targetMessage = $"{partialResist}{critMessage}{Name} casts {spell.Name} and restores {boost} points of your {srcVital}.";
            }
            else
            {
                targetMessage = $"{partialResist}{critMessage}{Name} casts {spell.Name} and drains {Math.Abs(boost)} points of your {srcVital}.";

                if (creature != null)
                {
                    targetPlayer.SetCurrentAttacker(creature);
                }
            }

            if (showMsg)
            {
                targetPlayer.SendChatMessage(player, targetMessage, ChatMessageType.Magic);
            }
        }

        if (targetCreature.IsAlive && spell.VitalDamageType == DamageType.Health &&
            boost < 0)
        {
            var damagePercent = (float)-boost / targetCreature.Health.MaxValue;
            ScheduleSpellDamageReactions(targetCreature, creature, equippedCloak, damagePercent);
        }

        HandleBoostTransferDeath(creature, targetCreature);
    }

    /// <summary>
    /// Returns the boost resistance for a damage type
    /// </summary>
    private static ResistanceType GetBoostResistanceType(DamageType damageType)
    {
        return damageType switch
        {
            DamageType.Health => ResistanceType.HealthBoost,
            DamageType.Stamina => ResistanceType.StaminaBoost,
            DamageType.Mana => ResistanceType.ManaBoost,
            _ => ResistanceType.Undef,
        };
    }

    /// <summary>
    /// Returns the drain resistance for a damage type
    /// </summary>
    private static ResistanceType GetDrainResistanceType(DamageType damageType)
    {
        return damageType switch
        {
            DamageType.Health => ResistanceType.HealthDrain,
            DamageType.Stamina => ResistanceType.StaminaDrain,
            DamageType.Mana => ResistanceType.ManaDrain,
            _ => ResistanceType.Undef,
        };
    }

    /// <summary>
    /// The damage type for a vital, so transfer spells can share the boost spell lookups
    /// </summary>
    private static DamageType GetVitalDamageType(PropertyAttribute2nd vital)
    {
        return vital switch
        {
            PropertyAttribute2nd.Health => DamageType.Health,
            PropertyAttribute2nd.Stamina => DamageType.Stamina,
            PropertyAttribute2nd.Mana => DamageType.Mana,
            _ => DamageType.Undef,
        };
    }

    private float SelfTargetSpellProcMod(bool fromProc, Spell spell, WorldObject weapon, Player player)
    {
        if (!fromProc || player == null || weapon == null)
        {
            return 1.0f;
        }

        var spellcraft = (uint)(weapon.ItemSpellcraft ?? 1) + CheckForArcaneLoreSpecSpellcraftBonus(player);

        var playerSpellSkill =
            spell.School == MagicSchool.WarMagic
                ? player.GetModdedWarMagicSkill()
                : player.GetModdedLifeMagicSkill();

        var procSpellSkill = (int)(playerSpellSkill + spellcraft * 0.1);

        if (spell.Power == 0)
        {
            return 1.0f;
        }

        var mod = (float)procSpellSkill / spell.Power;

        return Math.Clamp(mod, 0.5f, 2.0f);
    }
}
