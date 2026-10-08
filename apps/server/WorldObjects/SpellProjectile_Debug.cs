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
    private static void ShowInfo(
        Creature observed,
        Spell spell,
        CreatureSkill skill,
        float criticalChance,
        bool criticalHit,
        bool critDefended,
        bool overpower,
        float weaponCritDamageMod,
        int baseDamage,
        float elementalDamageMod,
        float slayerMod,
        float weaponResistanceMod,
        float resistanceMod,
        float absorbMod,
        float lifeProjectileDamage
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

        if (spell.MetaSpellType == ACE.Entity.Enum.SpellType.LifeProjectile)
        {
            // life magic projectile
            info += $"LifeProjectileDamage: {lifeProjectileDamage}\n";
            info += $"DamageRatio: {spell.DamageRatio}\n";
        }
        else
        {
            // war/void projectile
            var difficulty = Math.Min(spell.Power, 350);
            info += $"Difficulty: {difficulty}\n";

            info += $"BaseDamageRange: {spell.MinDamage} - {spell.MaxDamage}\n";
            info += $"BaseDamage: {baseDamage}\n";
            info += $"DamageType: {spell.DamageType}\n";
        }

        if (weaponCritDamageMod != 1.0f)
        {
            info += $"WeaponCritDamageMod: {weaponCritDamageMod}\n";
        }

        if (elementalDamageMod != 1.0f)
        {
            info += $"ElementalDamageMod: {elementalDamageMod}\n";
        }

        if (slayerMod != 1.0f)
        {
            info += $"SlayerMod: {slayerMod}\n";
        }

        if (weaponResistanceMod != 1.0f)
        {
            info += $"WeaponResistanceMod: {weaponResistanceMod}\n";
        }

        if (resistanceMod != 1.0f)
        {
            info += $"ResistanceMod: {resistanceMod}\n";
        }

        if (absorbMod != 1.0f)
        {
            info += $"AbsorbMod: {absorbMod}\n";
        }

        observer.DebugDamageBuffer += info;
    }

    private static void ShowInfo(
        Creature observed,
        float heritageMod,
        float sneakAttackMod,
        float damageRatingMod,
        float damageResistRatingMod,
        float critDamageRatingMod,
        float critDamageResistRatingMod,
        float pkDamageRatingMod,
        float pkDamageResistRatingMod,
        float damage
    )
    {
        var observer = PlayerManager.GetOnlinePlayer(observed.DebugDamageTarget);
        if (observer == null)
        {
            observed.DebugDamage = Creature.DebugDamageType.None;
            return;
        }
        var info = "";

        if (heritageMod != 1.0f)
        {
            info += $"HeritageMod: {heritageMod}\n";
        }

        if (sneakAttackMod != 1.0f)
        {
            info += $"SneakAttackMod: {sneakAttackMod}\n";
        }

        if (critDamageRatingMod != 1.0f)
        {
            info += $"CritDamageRatingMod: {critDamageRatingMod}\n";
        }

        if (pkDamageRatingMod != 1.0f)
        {
            info += $"PkDamageRatingMod: {pkDamageRatingMod}\n";
        }

        if (damageRatingMod != 1.0f)
        {
            info += $"DamageRatingMod: {damageRatingMod}\n";
        }

        if (critDamageResistRatingMod != 1.0f)
        {
            info += $"CritDamageResistRatingMod: {critDamageResistRatingMod}\n";
        }

        if (pkDamageResistRatingMod != 1.0f)
        {
            info += $"PkDamageResistRatingMod: {pkDamageResistRatingMod}\n";
        }

        if (damageResistRatingMod != 1.0f)
        {
            info += $"DamageResistRatingMod: {damageResistRatingMod}\n";
        }

        info += $"Final damage: {damage}";

        observer.Session.Network.EnqueueSend(
            new GameMessageSystemChat(observer.DebugDamageBuffer + info, ChatMessageType.Broadcast)
        );

        observer.DebugDamageBuffer = null;
    }
}
