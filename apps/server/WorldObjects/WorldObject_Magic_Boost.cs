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
    /// Who and what is involved in casting a boost spell (heal / harm) on a target
    /// </summary>
    private readonly struct BoostCast
    {
        public readonly Spell Spell;
        public readonly Creature Target;
        public readonly Player TargetPlayer;
        public readonly WorldObject Weapon;
        public readonly bool FromProc;
        public readonly double DamageMultiplier;

        /// <summary>
        /// COMBAT ABILITY - Reflect: the original caster of a reflected spell
        /// </summary>
        public readonly Creature ReflectedCaster;

        /// <summary>
        /// The creature whose stats are used for damage: the original caster of a reflected spell, otherwise the caster
        /// </summary>
        public readonly Creature DamageSource;

        public readonly Player DamageSourcePlayer;

        /// <summary>
        /// The result of the caster's last resist roll (TryResistSpell)
        /// </summary>
        public readonly PartialEvasion PartialEvasion;

        public BoostCast(
            WorldObject caster,
            Spell spell,
            Creature target,
            WorldObject weapon,
            bool fromProc,
            double damageMultiplier,
            Creature reflectedCaster,
            PartialEvasion partialEvasion
        )
        {
            Spell = spell;
            Target = target;
            TargetPlayer = target as Player;
            Weapon = weapon;
            FromProc = fromProc;
            DamageMultiplier = damageMultiplier;
            ReflectedCaster = reflectedCaster;
            DamageSource = reflectedCaster ?? caster as Creature;
            DamageSourcePlayer = DamageSource as Player;
            PartialEvasion = partialEvasion;
        }
    }

    /// <summary>
    /// Handles casting SpellType.Boost / FellowBoost spells
    /// typically for Life Magic, ie. Heal, Harm
    /// </summary>
    /// <param name="reflectedCaster">
    /// COMBAT ABILITY - Reflect: when set, this spell was reflected back at its original caster.
    /// It is still cast by this player (kill credit, threat, messages), but its damage is based on the original caster's stats.
    /// </param>
    /// <param name="partialEvasion">The target's resist roll for this spell, from TryResistSpell</param>
    private void HandleCastSpell_Boost(
        Spell spell,
        Creature targetCreature,
        bool fromProc,
        bool showMsg = true,
        WorldObject weapon = null,
        double damageMultiplier = 1.0,
        Creature reflectedCaster = null,
        PartialEvasion partialEvasion = PartialEvasion.None
    )
    {
        var player = this as Player;
        var creature = this as Creature;

        // prevent double deaths from indirect casts
        // caster is already checked in player/monster, and re-checking caster here would break death emotes such as bunny smite
        if (targetCreature != null && targetCreature.IsDead)
        {
            return;
        }

        var cast = new BoostCast(
            this,
            spell,
            targetCreature,
            weapon,
            fromProc,
            damageMultiplier,
            reflectedCaster,
            partialEvasion
        );

        // COMBAT ABILITY - Reflect: a reflected spell never damages the reflecting player.
        // A spell that was already reflected can't be reflected again.
        if (
            reflectedCaster == null
            && CheckForCombatAbilityReflectSpell(
                cast.PartialEvasion is PartialEvasion.All or PartialEvasion.Some,
                cast.TargetPlayer,
                creature,
                spell
            )
        )
        {
            cast.TargetPlayer.CastReflectedSpell(spell, creature, null, damageMultiplier);
            return;
        }

        var tryBoost = RollBoostAmount(cast);

        var critical = TryBoostCritical(weapon, ref tryBoost);

        // damage rating, for harming others
        if (targetCreature != this && tryBoost < 0)
        {
            var damageRating = cast.DamageSource?.GetDamageRating() ?? 0;
            var damageRatingMod = Creature.AdditiveCombine(Creature.GetPositiveRatingMod(damageRating));

            tryBoost = (int)(tryBoost * damageRatingMod);
        }

        if (targetCreature == null)
        {
            return;
        }

        tryBoost = (int)Math.Round(tryBoost * targetCreature.GetResistanceMod(GetBoostSpellResistanceType(spell)));

        var equippedCloak = targetCreature.EquippedCloak;

        tryBoost = ApplyCloakDamageProc(cast, equippedCloak, tryBoost);
        tryBoost = ApplyBoostSpellMods(cast, tryBoost);
        tryBoost = tryBoost > 0 ? ApplyHealRatingMods(cast, tryBoost) : ApplyHarmMods(cast, tryBoost);

        ResetRatingElementalistQuestStamps(player);

        tryBoost = ApplyArchetypeAndLevelScaling(cast, tryBoost);

        // SIGIL TRINKET - Top of Absorption: the target player may convert part of the damage into mana
        var sigilDamageReductionMod =
            cast.TargetPlayer?.CheckForSigilTrinketOnSpellHitReceivedEffects(
                this,
                spell,
                tryBoost,
                Skill.MagicDefense,
                SigilTrinketMagicDefenseEffect.Absorption
            ) ?? 1.0f;
        tryBoost = Convert.ToInt32(tryBoost * sigilDamageReductionMod);

        var boost = ApplyBoostToVital(cast, tryBoost, out var srcVital);

        if (boost < 0)
        {
            HandlePostDamageRatingEffects(
                targetCreature,
                -boost,
                player,
                cast.TargetPlayer,
                creature,
                spell,
                ProjectileSpellType.Undef
            );
        }
        else if (boost > 0)
        {
            HandlePostHealRatingEffects(player, cast.TargetPlayer);
        }

        SendBoostMessages(cast, boost, srcVital, critical, showMsg);

        if (targetCreature.IsAlive && spell.VitalDamageType == DamageType.Health && boost < 0)
        {
            var damagePercent = (float)-boost / targetCreature.Health.MaxValue;
            ScheduleSpellDamageReactions(targetCreature, creature, equippedCloak, damagePercent);
        }

        HandleBoostTransferDeath(creature, targetCreature);
    }

    /// <summary>
    /// Rolls the spell's boost range (negative for harm spells),
    /// with the weapon's restoration mod and the self-target proc mod
    /// </summary>
    private int RollBoostAmount(in BoostCast cast)
    {
        // handle negatives?
        var minBoostValue = Math.Min(cast.Spell.Boost, cast.Spell.MaxBoost);
        var maxBoostValue = Math.Max(cast.Spell.Boost, cast.Spell.MaxBoost);

        double? weaponRestorationMod = 1.0;
        if (cast.Weapon is { WeaponRestorationSpellsMod: > 1 })
        {
            weaponRestorationMod = cast.Weapon.WeaponRestorationSpellsMod;
        }

        var selfTargetProcSpellMod = SelfTargetSpellProcMod(cast.FromProc, cast.Spell, cast.Weapon, this as Player);

        return (int)(
            ThreadSafeRandom.Next(minBoostValue, maxBoostValue) * weaponRestorationMod * selfTargetProcSpellMod
        );
    }

    /// <summary>
    /// Boost spells crit 10% of the time (plus the weapon's crit frequency) for x1.5 (plus the weapon's crit multiplier / 1.5)
    /// </summary>
    private static bool TryBoostCritical(WorldObject weapon, ref int tryBoost)
    {
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
            return true;
        }

        return false;
    }

    /// <summary>
    /// A boost spell is resisted with the vital's boost resistance, a harm spell with its drain resistance
    /// </summary>
    private static ResistanceType GetBoostSpellResistanceType(Spell spell)
    {
        // handle negatives?
        var minBoostValue = Math.Min(spell.Boost, spell.MaxBoost);

        return minBoostValue > 0
            ? GetBoostResistanceType(spell.VitalDamageType)
            : GetDrainResistanceType(spell.VitalDamageType);
    }

    /// <summary>
    /// The target's cloak may proc to reduce the health damage of another's harm spell
    /// </summary>
    private int ApplyCloakDamageProc(in BoostCast cast, WorldObject equippedCloak, int tryBoost)
    {
        if (cast.Target == this || cast.Spell.VitalDamageType != DamageType.Health || tryBoost >= 0)
        {
            return tryBoost;
        }

        var percent = (float)-tryBoost / cast.Target.Health.MaxValue;

        if (equippedCloak == null || !Cloak.HasDamageProc(equippedCloak) || !Cloak.RollProc(equippedCloak, percent))
        {
            return tryBoost;
        }

        var reduced = -Cloak.GetReducedAmount(this, -tryBoost);

        Cloak.ShowMessage(cast.Target, this, -tryBoost, -reduced);

        return reduced;
    }

    /// <summary>
    /// The multipliers for both heals and harms: COMBAT ABILITY - Overload/Battery, the damage multiplier,
    /// the proc spellcraft mod, the dungeon mod and a partial resist
    /// </summary>
    private int ApplyBoostSpellMods(in BoostCast cast, int tryBoost)
    {
        var overloadMod = CheckForCombatAbilityOverloadDamageMod(cast.DamageSourcePlayer);
        var batterMod = CheckForCombatAbilityBatteryDamageMod(cast.DamageSourcePlayer);

        // proc spells receive 1% of spellcraft as a damage multiplier (300 spellcraft = x3), same as spell projectiles
        var spellcraftMod = 1.0f;
        if (cast.FromProc && cast.Weapon?.ItemSpellcraft != null)
        {
            var spellcraft =
                cast.Weapon.ItemSpellcraft.Value + CheckForArcaneLoreSpecSpellcraftBonus(cast.DamageSource);
            spellcraftMod = MagicFormulas.GetProcSpellcraftDamageMod(spellcraft);
        }

        // for traps and creatures the archetype system doesn't scale,
        // make sure they receive multipliers from landblock mods
        var landblockScalingMod = (cast.ReflectedCaster ?? this).GetLandblockLethalitySpellMod();

        var resistedMod = MagicFormulas.GetResistedMod(cast.PartialEvasion);

        return (int)(
            tryBoost
            * overloadMod
            * batterMod
            * cast.DamageMultiplier
            * spellcraftMod
            * landblockScalingMod
            * resistedMod
        );
    }

    /// <summary>
    /// RATING - Selflessness, Heal Bubble and Vitals Transfer (jewels), for a heal
    /// </summary>
    private int ApplyHealRatingMods(in BoostCast cast, int tryBoost)
    {
        var player = this as Player;

        // increases
        // Selfless Spirit (Lavender Jade): full bonus when restoring others, an equivalent
        // penalty when restoring yourself, and no effect when restoring a pet/monster.
        var selflessnessMod = Jewel.GetJewelEffectMod(player, PropertyInt.GearSelflessness);
        if (selflessnessMod > 0.0f && !cast.Target.IsMonster)
        {
            var selflessnessFactor = cast.Target == this ? 1.0f - selflessnessMod : 1.0f + selflessnessMod;
            tryBoost = Convert.ToInt32(tryBoost * selflessnessFactor);
        }

        tryBoost = Convert.ToInt32(tryBoost * (1.0f + Jewel.GetJewelEffectMod(player, PropertyInt.GearHealBubble)));

        // reductions
        return Convert.ToInt32(tryBoost * (1.0f - Jewel.GetJewelEffectMod(player, PropertyInt.GearVitalsTransfer)));
    }

    /// <summary>
    /// For a harm: the caster's jewels and attribute mod, then the target's Nullification, ward and COMBAT ABILITY - Phalanx
    /// </summary>
    private int ApplyHarmMods(in BoostCast cast, int tryBoost)
    {
        // increases
        tryBoost = Convert.ToInt32(tryBoost * (1.0f + Jewel.GetJewelRedFury(cast.DamageSourcePlayer)));
        tryBoost = Convert.ToInt32(tryBoost * (1.0f + Jewel.GetJewelBlueFury(cast.DamageSourcePlayer)));
        tryBoost = Convert.ToInt32(
            tryBoost * (1.0f + Jewel.GetJewelEffectMod(cast.DamageSourcePlayer, PropertyInt.GearSelfHarm))
        );

        var attributeMod = cast.DamageSource?.GetAttributeMod(cast.Weapon, true) ?? 1.0f;
        tryBoost = Convert.ToInt32(tryBoost * attributeMod);

        // reductions
        tryBoost = Convert.ToInt32(
            tryBoost
                * (1.0f - Jewel.GetJewelEffectMod(cast.TargetPlayer, PropertyInt.GearNullification, "Nullification"))
        );

        // ward
        var ignoreWardMod = 1.0f - Jewel.GetJewelEffectMod(cast.DamageSourcePlayer, PropertyInt.GearWardPen, "WardPen");
        var wardMod = GetWardMod(cast.DamageSource, cast.Target, ignoreWardMod);

        tryBoost = Convert.ToInt32(tryBoost * wardMod);

        // COMBAT ABILITY - Phalanx: health damage taken from full hits reduced by 30%. Partial resists are unaffected.
        if (cast.Spell.VitalDamageType == DamageType.Health && cast.PartialEvasion == PartialEvasion.None)
        {
            tryBoost = Convert.ToInt32(tryBoost * (cast.TargetPlayer?.GetPhalanxFullHitDamageMod() ?? 1.0f));
        }

        return tryBoost;
    }

    private int ApplyArchetypeAndLevelScaling(in BoostCast cast, int tryBoost)
    {
        var player = this as Player;

        if (cast.DamageSource is not null)
        {
            var archetypeSpellDamageMod = (float)(cast.DamageSource.ArchetypeSpellDamageMultiplier ?? 1.0);
            tryBoost = Convert.ToInt32(tryBoost * archetypeSpellDamageMod);
        }

        // LEVEL SCALING - Reduces harms against enemies, and restoration for players.
        // Also scales up healing on higher level player targets when both players are Shrouded (excluding self heals).
        var scalar = LevelScaling.GetPlayerBoostSpellScalar(cast.DamageSourcePlayer, cast.Target);
        if (tryBoost > 0 && player != null && cast.TargetPlayer != null && cast.TargetPlayer != player)
        {
            scalar *= LevelScaling.GetPlayerBoostHealScalarShroudedUpward(player, cast.TargetPlayer);
        }

        return (int)(tryBoost * scalar);
    }

    /// <summary>
    /// Applies the boost to the target's vital, returning the amount it actually changed.
    /// For health: records the heal or damage, and adds threat and charge.
    /// </summary>
    private int ApplyBoostToVital(in BoostCast cast, int tryBoost, out string srcVital)
    {
        var player = this as Player;
        var creature = this as Creature;
        var spell = cast.Spell;
        var targetCreature = cast.Target;
        var targetPlayer = cast.TargetPlayer;
        var fromProc = cast.FromProc;

        int boost;

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
                    boost = -(int)
                        Player.CombatAbilityManaBarrier(targetPlayer, (uint)-tryBoost, this, DamageType.Health);
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

        return boost;
    }

    /// <summary>
    /// Tells the caster and the target how much the spell restored or drained
    /// </summary>
    private void SendBoostMessages(in BoostCast cast, int boost, string srcVital, bool critical, bool showMsg)
    {
        var player = this as Player;
        var creature = this as Creature;
        var spell = cast.Spell;
        var targetCreature = cast.Target;
        var targetPlayer = cast.TargetPlayer;

        var critMessage = critical ? "Critical! " : "";
        var partialResist = cast.PartialEvasion == PartialEvasion.Some ? "Partial Resist! " : "";

        if (player != null)
        {
            string casterMessage;

            var chargedMsg = player.GetChargedMessage();

            if (player != targetCreature)
            {
                if (spell.IsBeneficial)
                {
                    casterMessage =
                        $"{partialResist}{chargedMsg}{critMessage}With {spell.Name} you restore {boost} points of {srcVital} to {targetCreature.Name}.";
                }
                else
                {
                    casterMessage =
                        $"{partialResist}{chargedMsg}{critMessage}With {spell.Name} you drain {Math.Abs(boost)} points of {srcVital} from {targetCreature.Name}.";
                }
            }
            else
            {
                var verb = spell.IsBeneficial ? "restore" : "drain";

                casterMessage =
                    $"{partialResist}{chargedMsg}{critMessage}You cast {spell.Name} and {verb} {Math.Abs(boost)} points of your {srcVital}.";
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
                targetMessage =
                    $"{partialResist}{critMessage}{Name} casts {spell.Name} and restores {boost} points of your {srcVital}.";
            }
            else
            {
                targetMessage =
                    $"{partialResist}{critMessage}{Name} casts {spell.Name} and drains {Math.Abs(boost)} points of your {srcVital}.";

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

    /// <summary>
    /// A proc spell cast on yourself scales with your magic skill vs the spell's power
    /// </summary>
    private static float SelfTargetSpellProcMod(bool fromProc, Spell spell, WorldObject weapon, Player player)
    {
        if (!fromProc || player == null || weapon == null)
        {
            return 1.0f;
        }

        var spellcraft = (uint)(weapon.ItemSpellcraft ?? 1) + CheckForArcaneLoreSpecSpellcraftBonus(player);

        var playerSpellSkill =
            spell.School == MagicSchool.WarMagic ? player.GetModdedWarMagicSkill() : player.GetModdedLifeMagicSkill();

        return MagicFormulas.GetSelfTargetProcMod(playerSpellSkill, spellcraft, spell.Power);
    }
}
