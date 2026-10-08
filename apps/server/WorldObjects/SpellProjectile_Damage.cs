using System;
using ACE.Common;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Entity;
using ACE.Server.Factories;
using ACE.Server.Managers;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.WorldObjects.Entity;
using DamageType = ACE.Entity.Enum.DamageType;

namespace ACE.Server.WorldObjects;

partial class SpellProjectile
{
    /// <summary>
    /// Calculates the damage for a spell projectile
    /// Used by war magic, void magic, and life magic projectiles
    /// </summary>
    private float? CalculateDamage(
        WorldObject source,
        Creature target,
        ref bool criticalHit,
        ref bool critDefended,
        ref bool overpower,
        ref bool resisted
    )
    {
        // COMBAT ABILITY - Reflect: a reflected spell is launched by the reflecting player (source),
        // but its resist check and damage are based on the original caster's stats
        var damageSource = ReflectedCaster ?? source;

        var sourcePlayer = damageSource as Player;
        var targetPlayer = target as Player;

        if (source == null || !target.IsAlive || target.Invincible)
        {
            return null;
        }

        // check lifestone protection
        if (targetPlayer != null && targetPlayer.UnderLifestoneProtection)
        {
            if (source is Player player)
            {
                player.Session.Network.EnqueueSend(
                    new GameMessageSystemChat(
                        $"The Lifestone's magic protects {targetPlayer.Name} from the attack!",
                        ChatMessageType.Magic
                    )
                );
            }

            targetPlayer.HandleLifestoneProtection();
            return null;
        }

        var criticalDamageMod = 1.0f;
        var weaponCritDamageMod = 1.0f;
        var weaponResistanceMod = 1.0f;
        var resistanceMod = 1.0f;

        // war/void magic
        var baseDamage = 0;

        var finalDamage = 0.0f;

        var resistanceType = Creature.GetResistanceType(Spell.DamageType);

        var sourceCreature = damageSource as Creature;
        if (sourceCreature?.Overpower != null)
        {
            overpower = Creature.GetOverpower(sourceCreature, target);
        }

        var weapon = ProjectileLauncher;

        var resistSource = IsWeaponSpell ? weapon : damageSource;

        var weaponAttackMod = 1.0;
        if (sourcePlayer?.GetEquippedWeapon() != null)
        {
            weaponAttackMod = sourcePlayer.GetEquippedWeapon().WeaponOffense ?? 1.0;
        }

        // Overpower pierces resists: the spell can't be resisted, fully or partially
        PartialEvasion partialEvasion;

        if (overpower)
        {
            resisted = false;
            partialEvasion = PartialEvasion.None;
        }
        else
        {
            resisted = source.TryResistSpell(
                target,
                Spell,
                out partialEvasion,
                resistSource,
                true,
                WeaponSpellcraft,
                weaponAttackMod,
                ReflectedCaster != null
            );
        }

        // COMBAT ABILITY - Reflect: a reflected spell never damages the reflecting player.
        // Overpower pierces a regular resist, and a spell that was already reflected can't be reflected again.
        if (
            ReflectedCaster == null
            && CheckForCombatAbilityReflectSpell(!overpower && partialEvasion is PartialEvasion.All or PartialEvasion.Some, targetPlayer, sourceCreature, Spell)
        )
        {
            targetPlayer.CastReflectedSpell(Spell, sourceCreature, this);

            resisted = true;
            return null;
        }

        var resistedMod = 1.0f;
        _partialEvasion = partialEvasion;

        if (!overpower)
        {
            if (GetResistedMod(out resistedMod))
            {
                return null;
            }
        }

        CreatureSkill attackSkill = null;
        if (sourceCreature != null)
        {
            attackSkill = sourceCreature.GetCreatureSkill(Spell.School);
        }

        // critical hit
        var criticalChance = GetWeaponMagicCritFrequency(weapon, sourceCreature, attackSkill, target);
        criticalChance += CheckForWarSpecCriticalChanceBonus(sourcePlayer, weapon);
        criticalChance = CheckForRatingReprisalAutoCrit(target, sourcePlayer, criticalChance);

        // backstab damage multiplier
        var backstabDamageMultiplier = Creature.GetStealthBackstabDamageMultiplier(sourcePlayer, target);

        if (ThreadSafeRandom.Next(0.0f, 1.0f) < criticalChance)
        {
            if (targetPlayer != null && targetPlayer.AugmentationCriticalDefense > 0)
            {
                var criticalDefenseMod = sourcePlayer != null ? 0.05f : 0.25f;
                var criticalDefenseChance = targetPlayer.AugmentationCriticalDefense * criticalDefenseMod;

                if (criticalDefenseChance > ThreadSafeRandom.Next(0.0f, 1.0f))
                {
                    critDefended = true;
                }
            }

            var perceptionDefended =
                !critDefended
                && sourceCreature != null
                && targetPlayer != null
                && CheckForPerceptionSpecCriticalDefense(
                    targetPlayer,
                    source.GetEffectiveMagicSkill(target, Spell, resistSource, WeaponSpellcraft, weaponAttackMod)
                );

            if (!critDefended && !perceptionDefended)
            {
                criticalHit = true;
            }

            if (CheckForRatingReprisalCritResist(criticalHit, ref resisted, targetPlayer, sourceCreature))
            {
                return null;
            }

            // EMPOWERED SCARAB - Crushing
            if (criticalHit && sourcePlayer != null && ReflectedCaster == null && Spell.School == MagicSchool.WarMagic)
            {
                sourcePlayer.CheckForSigilTrinketOnCastEffects(target, Spell, false, Skill.WarMagic, SigilTrinketWarMagicEffect.Crushing, null, true);
            }
        }

        // ward mod, rend, and penetration
        var ignoreWardMod = 1.0f;

        if (sourcePlayer != null)
        {
            ignoreWardMod = sourcePlayer.GetIgnoreWardMod(weapon);
        }

        if (weapon != null && weapon.HasImbuedEffect(ImbuedEffectType.WardRending))
        {
            ignoreWardMod -= GetWardRendingMod(attackSkill);
        }

        ignoreWardMod *= 1.0f - CheckForWarSpecWardPenBonus(sourcePlayer, weapon);
        ignoreWardMod *= 1.0f - Jewel.GetJewelEffectMod(sourcePlayer, PropertyInt.GearWardPen, "WardPen");

        var wardMod = GetWardMod(sourceCreature, target, ignoreWardMod);

        // absorb mod
        var isPVP = source is Player && targetPlayer != null;
        var absorbMod = GetAbsorbMod(target, this);

        absorbMod *= 1.0f - Jewel.GetJewelEffectMod(targetPlayer, PropertyInt.GearNullification, "Nullification");

        //http://acpedia.org/wiki/Announcements_-_2014/01_-_Forces_of_Nature - Aegis is 72% effective in PvP
        if (isPVP && (target.CombatMode == CombatMode.Melee || target.CombatMode == CombatMode.Missile))
        {
            absorbMod = 1 - absorbMod;
            absorbMod *= 0.72f;
            absorbMod = 1 - absorbMod;
        }

        if (isPVP && Spell.IsHarmful)
        {
            Player.UpdatePKTimers(source as Player, targetPlayer);
        }


        var elementalDamageMod = GetCasterElementalDamageModifier(weapon, sourceCreature, target, Spell.DamageType);

        // Possible 2x + damage bonus for the slayer property
        var slayerMod = GetWeaponCreatureSlayerModifier(weapon, sourceCreature, target);

        var overloadDamageMod = CheckForCombatAbilityOverloadDamageMod(sourcePlayer);
        var batteryDamageMod = CheckForCombatAbilityBatteryDamageMod(sourcePlayer);

        var attributeMod = 1.0f;

        if (sourceCreature is not null)
        {
            attributeMod = sourceCreature.GetAttributeMod(weapon, true);
        }

        var specDefenseMod = CheckForMagicDefenseSpecDefenseMod(targetPlayer, sourceCreature);

        var jewelRedFury = 1.0f + Jewel.GetJewelRedFury(sourcePlayer);
        var jewelBlueFury = 1.0f + Jewel.GetJewelBlueFury(sourcePlayer);
        var jewelSelfHarm = 1.0f + Jewel.GetJewelEffectMod(sourcePlayer, PropertyInt.GearSelfHarm);

        var levelScalingMod = GetLevelScalingMod(sourceCreature, target, targetPlayer);

        var damageMultiplier = (float)DamageMultiplier;

        // proc spells & enchanted blade receive 1% of spellcraft as a damage multipler (300 spellcraft = x3 damage)
        var spellcraftMod = 1.0f;
        if (FromProc && weapon?.ItemSpellcraft != null)
        {
            var spellcraft = weapon.ItemSpellcraft.Value + (int)CheckForArcaneLoreSpecSpellcraftBonus(sourceCreature);

            spellcraftMod = spellcraft * 0.01f;
        }

        // for traps and creatures the archetype system doesn't scale,
        // make sure they receive multipliers from landblock mods
        var landblockScalingMod = damageSource.GetLandblockLethalitySpellMod();

        // life magic projectiles: ie., martyr's hecatomb
        if (Spell.MetaSpellType == ACE.Entity.Enum.SpellType.LifeProjectile)
        {
            // overload/battery are applied once in damageBeforeMitigation below
            baseDamage = (int)(LifeProjectileDamage * Spell.DamageRatio);

            if (criticalHit)
            {
                weaponCritDamageMod = GetWeaponCritDamageMod(weapon, sourceCreature, attackSkill, target);
                criticalDamageMod = 1.0f + weaponCritDamageMod;
            }
        }
        // war/void magic projectiles
        else
        {
            if (criticalHit)
            {
                weaponCritDamageMod = GetWeaponCritDamageMod(weapon, sourceCreature, attackSkill, target);
                weaponCritDamageMod += CheckForWarMagicSpecCriticalDamageBonus(sourcePlayer, weapon);

                var jewelBludgeCritDamageMod =
                    1.0f + Jewel.GetJewelEffectMod(sourcePlayer, PropertyInt.GearBludgeon, "Bludgeon");

                criticalDamageMod = (1.0f + weaponCritDamageMod) * jewelBludgeCritDamageMod;

                baseDamage = Spell.MaxDamage;

                // monster spell crits are based on median damage instead of max
                if (sourceCreature is not Player)
                {
                    baseDamage = Spell.MedianDamage;
                }
            }
            else
            {
                baseDamage = ThreadSafeRandom.Next(Spell.MinDamage, Spell.MaxDamage);
            }
        }

        weaponResistanceMod = GetWeaponResistanceModifier(weapon, sourceCreature, attackSkill, Spell.DamageType, target);

            // if attacker/weapon has IgnoreMagicResist directly, do not transfer to spell projectile
            // only pass if SpellProjectile has it directly, such as 2637 - Invoking Aun Tanua

            resistanceMod = (float)Math.Max(0.0f, target.GetResistanceMod(resistanceType, this, null, weaponResistanceMod));

            if (sourcePlayer != null && targetPlayer != null && Spell.DamageType == DamageType.Nether)
            {
                // for direct damage from void spells in pvp,
                // apply void_pvp_modifier *on top of* the player's natural resistance to nether

                // this supposedly brings the direct damage from void spells in pvp closer to retail
                resistanceMod *= (float)PropertyManager.GetDouble("void_pvp_modifier").Item;
            }

            if (Spell.DamageType is DamageType.Pierce)
            {
                resistanceMod += Jewel.GetJewelEffectMod(sourcePlayer, PropertyInt.GearPierce, "Pierce");
            }

            var jewelElementalist = 1.0f + Jewel.GetJewelEffectMod(sourcePlayer, PropertyInt.GearElementalist, "Elementalist");
            var jewelElemental = Jewel.HandleElementalBonuses(sourcePlayer, Spell.DamageType);

            var ratingDamageTypeWard = Spell.DamageType switch
            {
                var dt when (dt & DamageType.Physical) != 0 => 1.0f - Jewel.GetJewelEffectMod(targetPlayer, PropertyInt.GearPhysicalWard),
                var dt when (dt & DamageType.Elemental) != 0 => 1.0f - Jewel.GetJewelEffectMod(targetPlayer, PropertyInt.GearElementalWard),
                _ => 1.0f
            };

            var strikethroughMod = 1.0f / (Strikethrough + 1);

            var archetypeSpellDamageMod = 1.0f;

            if (sourceCreature is not null)
            {
                archetypeSpellDamageMod = (float)(sourceCreature.ArchetypeSpellDamageMultiplier ?? 1.0);
            }

        // ----- FINAL CALCULATION ------------
        var damageBeforeMitigation =
            baseDamage
            * criticalDamageMod
            * attributeMod
            * elementalDamageMod
            * slayerMod
            * overloadDamageMod
            * batteryDamageMod
            * jewelElementalist
            * jewelElemental
            * jewelSelfHarm
            * jewelRedFury
            * jewelBlueFury
            * strikethroughMod
            * archetypeSpellDamageMod
            * levelScalingMod
            * damageMultiplier
            * spellcraftMod
            * landblockScalingMod
            * backstabDamageMultiplier;

        finalDamage =
            damageBeforeMitigation
            * absorbMod
            * wardMod
            * resistanceMod
            * resistedMod
            * specDefenseMod
            * ratingDamageTypeWard;

        // balance testing. TODO: update base spells damage and ward levels once ideal balance is found
        if (sourcePlayer is not null)
            {
                var playerSpellDamageMultiplier = (float)PropertyManager.GetDouble("player_spell_damage_multiplier").Item;
                finalDamage *= playerSpellDamageMultiplier;
            }
            else
            {
                var monsterSpellDamageMultiplier = (float)PropertyManager.GetDouble("monster_spell_damage_multiplier").Item;
                finalDamage *= monsterSpellDamageMultiplier;
            }

        // armor imbue: reduced physical/magical damage taken
        finalDamage *= GetImbuedArmorSpellDamageMod(target);

        // armor imbue: reduced critical damage taken
        if (criticalHit)
        {
            finalDamage *= GetImbuedArmorCritSpellDamageMod(target);
        }

        // COMBAT ABILITY - Phalanx: damage taken from full hits reduced by 30%. Partial resists are unaffected.
        if (resistedMod >= 1.0f)
        {
            finalDamage *= targetPlayer?.GetPhalanxFullHitDamageMod() ?? 1.0f;
        }


        // show debug info
        if (sourceCreature != null && sourceCreature.DebugDamage.HasFlag(Creature.DebugDamageType.Attacker))
        {
            ShowInfo(
                sourceCreature,
                Spell,
                attackSkill,
                criticalChance,
                criticalHit,
                critDefended,
                overpower,
                weaponCritDamageMod,
                baseDamage,
                elementalDamageMod,
                slayerMod,
                weaponResistanceMod,
                resistanceMod,
                absorbMod,
                LifeProjectileDamage
            );
        }
        if (target.DebugDamage.HasFlag(Creature.DebugDamageType.Defender))
        {
            ShowInfo(
                target,
                Spell,
                attackSkill,
                criticalChance,
                criticalHit,
                critDefended,
                overpower,
                weaponCritDamageMod,
                baseDamage,
                elementalDamageMod,
                slayerMod,
                weaponResistanceMod,
                resistanceMod,
                absorbMod,
                LifeProjectileDamage
            );
        }
        return finalDamage;
    }

    private static float GetLevelScalingMod(Creature attacker, Creature defender, Player playerDefender)
    {
        var monsterHealthScalingMod = playerDefender != null
            ? LevelScaling.GetMonsterDamageDealtHealthScalar(playerDefender, attacker)
            : LevelScaling.GetMonsterDamageTakenHealthScalar(attacker, defender);

        return monsterHealthScalingMod;
    }

    /// <summary>
    /// SPEC BONUS - War Magic (Wand/Baton): +50% crit damage (additively)
    /// </summary>
    private static float CheckForWarMagicSpecCriticalDamageBonus(Player sourcePlayer, WorldObject weapon)
    {
        if (sourcePlayer == null || weapon == null)
        {
            return 0.0f;
        }

        if (
            weapon.WeaponSkill == Skill.WarMagic
            && sourcePlayer.GetCreatureSkill(Skill.WarMagic).AdvancementClass == SkillAdvancementClass.Specialized
            && LootGenerationFactory.GetCasterSubType(weapon) == 2
        )
        {
            return 0.5f;
        }

        return 0.0f;
    }

    /// <summary>
    /// COMBAT ABILITY - Overload: Increased effectiveness up to 20% with Overload Charged stacks, by up to 100% with Overload Discharge
    /// </summary>
    private static float CheckForCombatAbilityOverloadDamageMod(Player sourcePlayer)
    {
        return sourcePlayer switch
        {
            { OverloadDischargeIsActive: true } => 1.0f + sourcePlayer.DischargeLevel,
            { OverloadStanceIsActive: true } => 1.0f + sourcePlayer.ManaChargeMeter * 0.2f,
            _ => 1.0f
        };
    }

    /// <summary>
    /// COMBAT ABILITY - Battery: Reduced effectiveness up to 10% with Battery Charged stacks
    /// </summary>
    private static float CheckForCombatAbilityBatteryDamageMod(Player sourcePlayer)
    {
        return sourcePlayer is { BatteryStanceIsActive: true } ? 1.0f - sourcePlayer.ManaChargeMeter * 0.1f : 1.0f;
    }

    /// <summary>
    /// SPEC BONUS - War Magic (Scepter): +5% critical chance (additively).
    /// </summary>
    private static float CheckForWarSpecCriticalChanceBonus(Player sourcePlayer, WorldObject weapon)
    {
        if (sourcePlayer == null || weapon == null)
        {
            return 0.0f;
        }

        if (
            weapon.WeaponSkill == Skill.WarMagic
            && sourcePlayer.GetCreatureSkill(Skill.WarMagic).AdvancementClass == SkillAdvancementClass.Specialized
            && LootGenerationFactory.GetCasterSubType(weapon) == 1
        )
        {
            return 0.05f;
        }

        return 0.0f;
    }

    /// <summary>
    /// SPEC BONUS - War Magic (Orb): +10% ward penetration (additively).
    /// </summary>
    private static float CheckForWarSpecWardPenBonus(Player sourcePlayer, WorldObject weapon)
    {
        if (sourcePlayer == null || weapon == null)
        {
            return 0.0f;
        }

        if (
            weapon.WeaponSkill == Skill.WarMagic
            && sourcePlayer.GetCreatureSkill(Skill.WarMagic).AdvancementClass == SkillAdvancementClass.Specialized
            && LootGenerationFactory.GetCasterSubType(weapon) == 0
        )
        {
            return 0.1f;
        }

        return 0.0f;
    }

    /// <summary>
    /// RATING - Reprisal: Auto-crit.
    /// (JEWEL - Black Opal)
    /// </summary>
    private static float CheckForRatingReprisalAutoCrit(Creature target, Player sourcePlayer, float criticalChance)
    {
        if (sourcePlayer == null)
        {
            return criticalChance;
        }

        if (sourcePlayer.GetEquippedAndActivatedItemRatingSum(PropertyInt.GearReprisal) <= 0)
        {
            return criticalChance;
        }

        if (!sourcePlayer.QuestManager.HasQuest($"{target.Guid}/Reprisal"))
        {
            return criticalChance;
        }

        sourcePlayer.QuestManager.Erase($"{target.Guid}/Reprisal");

        return 1.0f;
    }
}
