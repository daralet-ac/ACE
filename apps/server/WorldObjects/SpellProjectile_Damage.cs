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
    /// Who and what is involved when a spell projectile hits its target
    /// </summary>
    private readonly struct SpellHit
    {
        /// <summary>
        /// The projectile's launcher (ProjectileSource)
        /// </summary>
        public readonly WorldObject Source;

        /// <summary>
        /// Whose stats the damage is based on. COMBAT ABILITY - Reflect: a reflected spell is launched by the
        /// reflecting player (Source), but its resist check and damage are based on the original caster's stats
        /// </summary>
        public readonly WorldObject DamageSource;

        public readonly Creature SourceCreature;
        public readonly Player SourcePlayer;
        public readonly Creature Target;
        public readonly Player TargetPlayer;

        /// <summary>
        /// The weapon or casting item the spell came from
        /// </summary>
        public readonly WorldObject Weapon;

        /// <summary>
        /// What the target resists: the casting item for a built-in spell, otherwise the damage source
        /// </summary>
        public readonly WorldObject ResistSource;

        /// <summary>
        /// The offense mod of the caster's equipped weapon, which raises their effective magic skill
        /// </summary>
        public readonly double WeaponAttackMod;

        /// <summary>
        /// The caster's skill in the spell's school. Set once the spell wasn't fully resisted
        /// (GetCreatureSkill adds the skill if the caster doesn't have it).
        /// </summary>
        public CreatureSkill AttackSkill { get; init; }

        /// <summary>
        /// A player's spell (launched by a player, so reflected spells count) hitting a player
        /// </summary>
        public readonly bool IsPvp;

        public SpellHit(SpellProjectile projectile, WorldObject source, Creature target)
        {
            Source = source;
            DamageSource = projectile.ReflectedCaster ?? source;
            SourceCreature = DamageSource as Creature;
            SourcePlayer = DamageSource as Player;
            Target = target;
            TargetPlayer = target as Player;
            Weapon = projectile.ProjectileLauncher;
            ResistSource = projectile.IsWeaponSpell ? Weapon : DamageSource;
            WeaponAttackMod = SourcePlayer?.GetEquippedWeapon()?.WeaponOffense ?? 1.0;
            IsPvp = source is Player && TargetPlayer != null;
        }
    }

    /// <summary>
    /// Calculates the damage for a spell projectile hitting its target, or null if it does none
    /// (the target is protected, or resisted or reflected the spell).
    /// Used by war magic, void magic, and life magic projectiles
    /// </summary>
    private float? CalculateDamage(
        WorldObject source,
        Creature target,
        ref bool criticalHit,
        ref bool critDefended,
        ref bool overpower,
        ref bool resisted,
        out PartialEvasion partialEvasion
    )
    {
        partialEvasion = PartialEvasion.None;

        if (source == null || !target.IsAlive || target.Invincible)
        {
            return null;
        }

        if (IsUnderLifestoneProtection(source, target as Player))
        {
            return null;
        }

        var hit = new SpellHit(this, source, target);

        // resist and reflect
        if (hit.SourceCreature?.Overpower != null)
        {
            overpower = Creature.GetOverpower(hit.SourceCreature, target);
        }

        resisted = TryResistSpellHit(hit, overpower, out partialEvasion);

        if (TryReflectSpellHit(hit, overpower, partialEvasion))
        {
            resisted = true;
            return null;
        }

        var resistedMod = 1.0f;

        if (!overpower)
        {
            resistedMod = MagicFormulas.GetResistedMod(partialEvasion);

            // fully resisted
            if (resistedMod == 0.0f)
            {
                return null;
            }
        }

        hit = hit with { AttackSkill = hit.SourceCreature?.GetCreatureSkill(Spell.School) };

        // critical hit
        var criticalChance = GetCriticalChance(hit);
        var backstabDamageMultiplier = Creature.GetStealthBackstabDamageMultiplier(hit.SourcePlayer, target);

        if (ThreadSafeRandom.Next(0.0f, 1.0f) < criticalChance)
        {
            criticalHit = !TryDefendCriticalHit(hit, out critDefended);

            if (CheckForRatingReprisalCritResist(criticalHit, ref resisted, hit.TargetPlayer, hit.SourceCreature))
            {
                return null;
            }

            // EMPOWERED SCARAB - Crushing
            if (
                criticalHit
                && hit.SourcePlayer != null
                && ReflectedCaster == null
                && Spell.School == MagicSchool.WarMagic
            )
            {
                hit.SourcePlayer.CheckForSigilTrinketOnCastEffects(
                    target,
                    Spell,
                    false,
                    Skill.WarMagic,
                    SigilTrinketWarMagicEffect.Crushing,
                    null,
                    true
                );
            }
        }

        var wardMod = GetTargetWardMod(hit);
        var absorbMod = GetTargetAbsorbMod(hit);

        if (hit.IsPvp && Spell.IsHarmful)
        {
            Player.UpdatePKTimers(source as Player, hit.TargetPlayer);
        }

        // damage
        var baseDamage = GetBaseDamage(hit, criticalHit, out var criticalDamageMod, out var weaponCritDamageMod);
        var damageMods = GetDamageModifiers(hit, criticalDamageMod, backstabDamageMultiplier);
        var mitigation = GetMitigationModifiers(hit, absorbMod, wardMod, resistedMod, out var weaponResistanceMod);

        var damageBeforeMitigation = damageMods.Apply(baseDamage);
        var finalDamage = mitigation.Apply(damageBeforeMitigation);

        finalDamage = ApplyPostMitigationMods(hit, finalDamage, criticalHit, resistedMod);

        ShowDamageDebugInfo(
            hit,
            criticalChance,
            criticalHit,
            critDefended,
            overpower,
            baseDamage,
            weaponCritDamageMod,
            weaponResistanceMod,
            damageMods,
            mitigation
        );

        return finalDamage;
    }

    /// <summary>
    /// A player under lifestone protection can't be damaged by spells: tells the caster, and returns TRUE
    /// </summary>
    private static bool IsUnderLifestoneProtection(WorldObject source, Player targetPlayer)
    {
        if (targetPlayer == null || !targetPlayer.UnderLifestoneProtection)
        {
            return false;
        }

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
        return true;
    }

    /// <summary>
    /// Rolls the target's resist against the spell, returning TRUE if it fully resisted.
    /// Overpower pierces resists: the spell can't be resisted, fully or partially.
    /// </summary>
    private bool TryResistSpellHit(in SpellHit hit, bool overpower, out PartialEvasion partialEvasion)
    {
        if (overpower)
        {
            partialEvasion = PartialEvasion.None;
            return false;
        }

        return hit.Source.TryResistSpell(
            hit.Target,
            Spell,
            out partialEvasion,
            hit.ResistSource,
            true,
            WeaponSpellcraft,
            hit.WeaponAttackMod,
            ReflectedCaster != null
        );
    }

    /// <summary>
    /// COMBAT ABILITY - Reflect: a player may reflect a spell they resisted back at its caster.
    /// A reflected spell never damages the reflecting player.
    /// Overpower pierces a regular resist, and a spell that was already reflected can't be reflected again.
    /// </summary>
    private bool TryReflectSpellHit(in SpellHit hit, bool overpower, PartialEvasion partialEvasion)
    {
        if (
            ReflectedCaster != null
            || !CheckForCombatAbilityReflectSpell(
                !overpower && partialEvasion is PartialEvasion.All or PartialEvasion.Some,
                hit.TargetPlayer,
                hit.SourceCreature,
                Spell
            )
        )
        {
            return false;
        }

        hit.TargetPlayer.CastReflectedSpell(Spell, hit.SourceCreature, this);
        return true;
    }

    private float GetCriticalChance(in SpellHit hit)
    {
        var criticalChance = GetWeaponMagicCritFrequency(hit.Weapon, hit.SourceCreature, hit.AttackSkill, hit.Target);
        criticalChance += CheckForWarSpecCriticalChanceBonus(hit.SourcePlayer, hit.Weapon);

        return CheckForRatingReprisalAutoCrit(hit.Target, hit.SourcePlayer, criticalChance);
    }

    /// <summary>
    /// The target's critical defenses: the Critical Protection augmentation, then SPEC BONUS - Perception.
    /// Returns TRUE if one of them prevents the critical hit.
    /// </summary>
    /// <param name="critDefended">TRUE if the augmentation prevented it</param>
    private bool TryDefendCriticalHit(in SpellHit hit, out bool critDefended)
    {
        critDefended = false;

        if (hit.TargetPlayer != null && hit.TargetPlayer.AugmentationCriticalDefense > 0)
        {
            var criticalDefenseMod = hit.SourcePlayer != null ? 0.05f : 0.25f;
            var criticalDefenseChance = hit.TargetPlayer.AugmentationCriticalDefense * criticalDefenseMod;

            if (criticalDefenseChance > ThreadSafeRandom.Next(0.0f, 1.0f))
            {
                critDefended = true;
            }
        }

        if (critDefended)
        {
            return true;
        }

        return hit.SourceCreature != null
            && hit.TargetPlayer != null
            && CheckForPerceptionSpecCriticalDefense(
                hit.TargetPlayer,
                hit.Source.GetEffectiveMagicSkill(
                    hit.Target,
                    Spell,
                    hit.ResistSource,
                    WeaponSpellcraft,
                    hit.WeaponAttackMod
                )
            );
    }

    /// <summary>
    /// The target's ward, after the caster's ward rending and ward penetration
    /// </summary>
    private float GetTargetWardMod(in SpellHit hit)
    {
        var ignoreWardMod = 1.0f;

        if (hit.SourcePlayer != null)
        {
            ignoreWardMod = hit.SourcePlayer.GetIgnoreWardMod(hit.Weapon);
        }

        if (hit.Weapon != null && hit.Weapon.HasImbuedEffect(ImbuedEffectType.WardRending))
        {
            ignoreWardMod -= GetWardRendingMod(hit.AttackSkill);
        }

        ignoreWardMod *= 1.0f - CheckForWarSpecWardPenBonus(hit.SourcePlayer, hit.Weapon);
        ignoreWardMod *= 1.0f - Jewel.GetJewelEffectMod(hit.SourcePlayer, PropertyInt.GearWardPen, "WardPen");

        return GetWardMod(hit.SourceCreature, hit.Target, ignoreWardMod);
    }

    /// <summary>
    /// The target's magic absorption and RATING - Nullification. Aegis is weaker in PvP, Nullification isn't.
    /// </summary>
    private float GetTargetAbsorbMod(in SpellHit hit)
    {
        var absorbMod = GetAbsorbMod(hit.Target, this);

        //http://acpedia.org/wiki/Announcements_-_2014/01_-_Forces_of_Nature - Aegis is 72% effective in PvP
        if (hit.IsPvp && (hit.Target.CombatMode == CombatMode.Melee || hit.Target.CombatMode == CombatMode.Missile))
        {
            absorbMod = MagicFormulas.GetPvpAbsorbMod(absorbMod);
        }

        absorbMod *= 1.0f - Jewel.GetJewelEffectMod(hit.TargetPlayer, PropertyInt.GearNullification, "Nullification");

        return absorbMod;
    }

    /// <summary>
    /// The spell's base damage, and its critical damage multiplier on a critical hit.
    /// Life projectiles (ie. Martyr's Hecatomb) use the damage worked out when they were cast.
    /// War/void projectiles roll their damage range, or use their max damage on a critical hit
    /// (monster spell crits are based on median damage instead of max).
    /// </summary>
    private int GetBaseDamage(
        in SpellHit hit,
        bool criticalHit,
        out float criticalDamageMod,
        out float weaponCritDamageMod
    )
    {
        criticalDamageMod = 1.0f;
        weaponCritDamageMod = 1.0f;

        if (Spell.MetaSpellType == ACE.Entity.Enum.SpellType.LifeProjectile)
        {
            if (criticalHit)
            {
                weaponCritDamageMod = GetWeaponCritDamageMod(
                    hit.Weapon,
                    hit.SourceCreature,
                    hit.AttackSkill,
                    hit.Target
                );
                criticalDamageMod = 1.0f + weaponCritDamageMod;
            }

            return (int)(LifeProjectileDamage * Spell.DamageRatio);
        }

        if (!criticalHit)
        {
            return ThreadSafeRandom.Next(Spell.MinDamage, Spell.MaxDamage);
        }

        weaponCritDamageMod = GetWeaponCritDamageMod(hit.Weapon, hit.SourceCreature, hit.AttackSkill, hit.Target);
        weaponCritDamageMod += CheckForWarMagicSpecCriticalDamageBonus(hit.SourcePlayer, hit.Weapon);

        var jewelBludgeCritDamageMod =
            1.0f + Jewel.GetJewelEffectMod(hit.SourcePlayer, PropertyInt.GearBludgeon, "Bludgeon");

        criticalDamageMod = (1.0f + weaponCritDamageMod) * jewelBludgeCritDamageMod;

        return hit.SourceCreature is Player ? Spell.MaxDamage : Spell.MedianDamage;
    }

    private SpellDamageModifiers GetDamageModifiers(
        in SpellHit hit,
        float criticalDamageMod,
        float backstabDamageMultiplier
    )
    {
        return new SpellDamageModifiers
        {
            Critical = criticalDamageMod,
            Attribute = hit.SourceCreature?.GetAttributeMod(hit.Weapon, true) ?? 1.0f,
            Elemental = GetCasterElementalDamageModifier(hit.Weapon, hit.SourceCreature, hit.Target, Spell.DamageType),

            // Possible 2x + damage bonus for the slayer property
            Slayer = GetWeaponCreatureSlayerModifier(hit.Weapon, hit.SourceCreature, hit.Target),

            Overload = CheckForCombatAbilityOverloadDamageMod(hit.SourcePlayer),
            Battery = CheckForCombatAbilityBatteryDamageMod(hit.SourcePlayer),
            JewelElementalist =
                1.0f + Jewel.GetJewelEffectMod(hit.SourcePlayer, PropertyInt.GearElementalist, "Elementalist"),
            JewelElemental = Jewel.HandleElementalBonuses(hit.SourcePlayer, Spell.DamageType),
            JewelSelfHarm = 1.0f + Jewel.GetJewelEffectMod(hit.SourcePlayer, PropertyInt.GearSelfHarm),
            JewelRedFury = 1.0f + Jewel.GetJewelRedFury(hit.SourcePlayer),
            JewelBlueFury = 1.0f + Jewel.GetJewelBlueFury(hit.SourcePlayer),
            StrikethroughPenalty = MagicFormulas.GetStrikethroughPenalty(Strikethrough),
            Archetype = (float)(hit.SourceCreature?.ArchetypeSpellDamageMultiplier ?? 1.0),
            LevelScaling = LevelScaling.GetMonsterHealthDamageScalar(hit.SourceCreature, hit.Target),
            DamageMultiplier = (float)DamageMultiplier,
            Spellcraft = GetProcSpellcraftDamageMod(hit),
            LandblockScaling = hit.DamageSource.GetLandblockLethalitySpellMod(),
            Backstab = backstabDamageMultiplier,
        };
    }

    /// <summary>
    /// Proc spells and Enchanted Blade receive 1% of spellcraft as a damage multiplier (300 spellcraft = x3 damage)
    /// </summary>
    private float GetProcSpellcraftDamageMod(in SpellHit hit)
    {
        if (!FromProc || hit.Weapon?.ItemSpellcraft == null)
        {
            return 1.0f;
        }

        var spellcraft =
            hit.Weapon.ItemSpellcraft.Value + (int)CheckForArcaneLoreSpecSpellcraftBonus(hit.SourceCreature);

        return MagicFormulas.GetProcSpellcraftDamageMod(spellcraft);
    }

    private SpellMitigationModifiers GetMitigationModifiers(
        in SpellHit hit,
        float absorbMod,
        float wardMod,
        float resistedMod,
        out float weaponResistanceMod
    )
    {
        weaponResistanceMod = GetWeaponResistanceModifier(
            hit.Weapon,
            hit.SourceCreature,
            hit.AttackSkill,
            Spell.DamageType,
            hit.Target
        );

        return new SpellMitigationModifiers
        {
            Absorb = absorbMod,
            Ward = wardMod,
            Resistance = GetTargetResistanceMod(hit, weaponResistanceMod),
            Resisted = resistedMod,
            SpecDefense = CheckForMagicDefenseSpecDefenseMod(hit.TargetPlayer, hit.SourceCreature),
            DamageTypeWard = Spell.DamageType switch
            {
                var dt when (dt & DamageType.Physical) != 0
                    => 1.0f - Jewel.GetJewelEffectMod(hit.TargetPlayer, PropertyInt.GearPhysicalWard),
                var dt when (dt & DamageType.Elemental) != 0
                    => 1.0f - Jewel.GetJewelEffectMod(hit.TargetPlayer, PropertyInt.GearElementalWard),
                _ => 1.0f
            },
        };
    }

    /// <summary>
    /// The target's resistance to the spell's damage type, after the weapon's resistance modifier
    /// </summary>
    private float GetTargetResistanceMod(in SpellHit hit, float weaponResistanceMod)
    {
        // if attacker/weapon has IgnoreMagicResist directly, do not transfer to spell projectile
        // only pass if SpellProjectile has it directly, such as 2637 - Invoking Aun Tanua
        var resistanceType = Creature.GetResistanceType(Spell.DamageType);

        var resistanceMod = (float)
            Math.Max(0.0f, hit.Target.GetResistanceMod(resistanceType, this, null, weaponResistanceMod));

        if (hit.SourcePlayer != null && hit.TargetPlayer != null && Spell.DamageType == DamageType.Nether)
        {
            // for direct damage from void spells in pvp,
            // apply void_pvp_modifier *on top of* the player's natural resistance to nether

            // this supposedly brings the direct damage from void spells in pvp closer to retail
            resistanceMod *= (float)PropertyManager.GetDouble("void_pvp_modifier").Item;
        }

        // RATING - Pierce: piercing resistance penetration (JEWEL - Black Garnet)
        if (Spell.DamageType is DamageType.Pierce)
        {
            resistanceMod += Jewel.GetJewelEffectMod(hit.SourcePlayer, PropertyInt.GearPierce, "Pierce");
        }

        return resistanceMod;
    }

    /// <summary>
    /// The multipliers applied after mitigation: the balance multipliers, armor imbues and COMBAT ABILITY - Phalanx
    /// </summary>
    private float ApplyPostMitigationMods(in SpellHit hit, float damage, bool criticalHit, float resistedMod)
    {
        // balance testing. TODO: update base spells damage and ward levels once ideal balance is found
        if (hit.SourcePlayer is not null)
        {
            var playerSpellDamageMultiplier = (float)PropertyManager.GetDouble("player_spell_damage_multiplier").Item;
            damage *= playerSpellDamageMultiplier;
        }
        else
        {
            var monsterSpellDamageMultiplier = (float)PropertyManager.GetDouble("monster_spell_damage_multiplier").Item;
            damage *= monsterSpellDamageMultiplier;
        }

        // armor imbue: reduced magical damage taken
        damage *= GetImbuedArmorSpellDamageMod(hit.Target);

        // armor imbue: reduced critical damage taken
        if (criticalHit)
        {
            damage *= GetImbuedArmorCritSpellDamageMod(hit.Target);
        }

        // COMBAT ABILITY - Phalanx: damage taken from full hits reduced by 30%. Partial resists are unaffected.
        if (resistedMod >= 1.0f)
        {
            damage *= hit.TargetPlayer?.GetPhalanxFullHitDamageMod() ?? 1.0f;
        }

        return damage;
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
