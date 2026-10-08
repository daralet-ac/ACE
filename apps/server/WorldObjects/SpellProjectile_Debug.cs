using System;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Entity;
using ACE.Server.Managers;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.WorldObjects.Entity;

namespace ACE.Server.WorldObjects;

partial class SpellProjectile
{
    /// <summary>
    /// Shows a spell projectile's damage calculation to the players debugging its caster (attacker) or target (defender).
    /// The rating modifiers and final damage are added when the damage is applied (SendRatingDebugInfo).
    /// </summary>
    private void ShowDamageDebugInfo(
        in SpellHit hit,
        float criticalChance,
        bool criticalHit,
        bool critDefended,
        bool overpower,
        int baseDamage,
        float weaponCritDamageMod,
        float weaponResistanceMod,
        in SpellDamageModifiers damageMods,
        in SpellMitigationModifiers mitigation
    )
    {
        if (hit.SourceCreature != null && hit.SourceCreature.DebugDamage.HasFlag(Creature.DebugDamageType.Attacker))
        {
            AppendDamageDebugInfo(
                hit.SourceCreature,
                hit.AttackSkill,
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
        }

        if (hit.Target.DebugDamage.HasFlag(Creature.DebugDamageType.Defender))
        {
            AppendDamageDebugInfo(
                hit.Target,
                hit.AttackSkill,
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
        }
    }

    private void AppendDamageDebugInfo(
        Creature observed,
        CreatureSkill skill,
        float criticalChance,
        bool criticalHit,
        bool critDefended,
        bool overpower,
        int baseDamage,
        float weaponCritDamageMod,
        float weaponResistanceMod,
        in SpellDamageModifiers damageMods,
        in SpellMitigationModifiers mitigation
    )
    {
        var observer = PlayerManager.GetOnlinePlayer(observed.DebugDamageTarget);
        if (observer == null)
        {
            observed.DebugDamage = Creature.DebugDamageType.None;
            return;
        }

        var info = $"Skill: {skill.Skill.ToSentence()}\n";
        info += $"CriticalChance: {criticalChance}\n";
        info += $"CriticalHit: {criticalHit}\n";

        if (critDefended)
        {
            info += $"CriticalDefended: {critDefended}\n";
        }

        info += $"Overpower: {overpower}\n";

        if (Spell.MetaSpellType == ACE.Entity.Enum.SpellType.LifeProjectile)
        {
            // life magic projectile
            info += $"LifeProjectileDamage: {(float)LifeProjectileDamage}\n";
            info += $"DamageRatio: {Spell.DamageRatio}\n";
        }
        else
        {
            // war/void projectile
            var difficulty = Math.Min(Spell.Power, 350);
            info += $"Difficulty: {difficulty}\n";

            info += $"BaseDamageRange: {Spell.MinDamage} - {Spell.MaxDamage}\n";
            info += $"BaseDamage: {baseDamage}\n";
            info += $"DamageType: {Spell.DamageType}\n";
        }

        if (weaponCritDamageMod != 1.0f)
        {
            info += $"WeaponCritDamageMod: {weaponCritDamageMod}\n";
        }

        if (damageMods.Elemental != 1.0f)
        {
            info += $"ElementalDamageMod: {damageMods.Elemental}\n";
        }

        if (damageMods.Slayer != 1.0f)
        {
            info += $"SlayerMod: {damageMods.Slayer}\n";
        }

        if (weaponResistanceMod != 1.0f)
        {
            info += $"WeaponResistanceMod: {weaponResistanceMod}\n";
        }

        if (mitigation.Resistance != 1.0f)
        {
            info += $"ResistanceMod: {mitigation.Resistance}\n";
        }

        if (mitigation.Absorb != 1.0f)
        {
            info += $"AbsorbMod: {mitigation.Absorb}\n";
        }

        observer.DebugDamageBuffer += info;
    }

    /// <summary>
    /// Finishes the damage debug output started by ShowDamageDebugInfo: the rating modifiers and the final damage
    /// </summary>
    private static void ShowRatingDebugInfo(
        Creature target,
        Creature sourceCreature,
        in SpellRatingModifiers ratings,
        float damage
    )
    {
        if (sourceCreature != null && sourceCreature.DebugDamage.HasFlag(Creature.DebugDamageType.Attacker))
        {
            SendRatingDebugInfo(sourceCreature, ratings, damage);
        }

        if (target.DebugDamage.HasFlag(Creature.DebugDamageType.Defender))
        {
            SendRatingDebugInfo(target, ratings, damage);
        }
    }

    private static void SendRatingDebugInfo(Creature observed, in SpellRatingModifiers ratings, float damage)
    {
        var observer = PlayerManager.GetOnlinePlayer(observed.DebugDamageTarget);
        if (observer == null)
        {
            observed.DebugDamage = Creature.DebugDamageType.None;
            return;
        }
        var info = "";

        if (ratings.Heritage != 1.0f)
        {
            info += $"HeritageMod: {ratings.Heritage}\n";
        }

        if (ratings.SneakAttack != 1.0f)
        {
            info += $"SneakAttackMod: {ratings.SneakAttack}\n";
        }

        if (ratings.CritDamageRating != 1.0f)
        {
            info += $"CritDamageRatingMod: {ratings.CritDamageRating}\n";
        }

        if (ratings.PkDamageRating != 1.0f)
        {
            info += $"PkDamageRatingMod: {ratings.PkDamageRating}\n";
        }

        if (ratings.DamageRating != 1.0f)
        {
            info += $"DamageRatingMod: {ratings.DamageRating}\n";
        }

        if (ratings.CritDamageResistRating != 1.0f)
        {
            info += $"CritDamageResistRatingMod: {ratings.CritDamageResistRating}\n";
        }

        if (ratings.PkDamageResistRating != 1.0f)
        {
            info += $"PkDamageResistRatingMod: {ratings.PkDamageResistRating}\n";
        }

        if (ratings.DamageResistRating != 1.0f)
        {
            info += $"DamageResistRatingMod: {ratings.DamageResistRating}\n";
        }

        info += $"Final damage: {damage}";

        observer.Session.Network.EnqueueSend(
            new GameMessageSystemChat(observer.DebugDamageBuffer + info, ChatMessageType.Broadcast)
        );

        observer.DebugDamageBuffer = null;
    }
}
