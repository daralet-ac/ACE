using System;
using ACE.Common;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Entity;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.WorldObjects;

partial class SpellProjectile
{
    /// <summary>
    /// Armor imbued with Reduced Magical Damage Taken: -1% magic damage per imbue, down to 50%
    /// </summary>
    private static float GetImbuedArmorSpellDamageMod(Creature target)
    {
        return DamageFormulas.GetImbuedArmorMod(
            target.GetArmorDefenseImbues(ImbuedEffectType.ReducedMagicalDamageTaken)
        );
    }

    /// <summary>
    /// Armor imbued with Reduced Critical Damage Taken: -1% critical damage per imbue, down to 50%
    /// </summary>
    private static float GetImbuedArmorCritSpellDamageMod(Creature target)
    {
        return DamageFormulas.GetImbuedArmorMod(
            target.GetArmorDefenseImbues(ImbuedEffectType.ReducedCriticalDamageTaken)
        );
    }

    /// <summary>
    /// SPEC BONUS - Magic Defense: Magic damage reduced by 10% + 1% per 50 skill level.
    /// </summary>
    private static float CheckForMagicDefenseSpecDefenseMod(Player targetPlayer, Creature sourceCreature)
    {
        if (
            targetPlayer == null
            || targetPlayer.GetCreatureSkill(Skill.MagicDefense).AdvancementClass != SkillAdvancementClass.Specialized
        )
        {
            return 1.0f;
        }

        // float, so the division below isn't integer division
        var magicDefenseSkill = (float)
            LevelScaling.GetScaledPlayerDefenseSkill(
                targetPlayer.GetModdedMagicDefSkill(),
                targetPlayer,
                sourceCreature
            );

        return DamageFormulas.GetSpecDefenseMod(magicDefenseSkill);
    }

    /// <summary>
    /// SPEC BONUS - Perception: Up to 50% chance to prevent a critical hit, based on Perception vs the caster's effective magic skill
    /// </summary>
    private static bool CheckForPerceptionSpecCriticalDefense(Player targetPlayer, uint effectiveMagicSkill)
    {
        var perception = targetPlayer.GetCreatureSkill(Skill.Perception);
        if (perception.AdvancementClass != SkillAdvancementClass.Specialized)
        {
            return false;
        }

        var criticalDefenseChance = SkillCheck.GetSkillRatioChance(
            targetPlayer.GetModdedPerceptionSkill(),
            effectiveMagicSkill
        );

        if (!(criticalDefenseChance > ThreadSafeRandom.Next(0f, 1f)))
        {
            return false;
        }

        targetPlayer.Session.Network.EnqueueSend(
            new GameMessageSystemChat(
                $"Your perception skill allowed you to prevent a critical strike!",
                ChatMessageType.Magic
            )
        );

        return true;
    }

    /// <summary>
    /// RATING - Reprisal: Crit resist.
    /// (JEWEL - Black Opal)
    /// </summary>
    private bool CheckForRatingReprisalCritResist(
        bool criticalHit,
        ref bool resisted,
        Player targetPlayer,
        Creature sourceCreature
    )
    {
        if (!criticalHit || targetPlayer is null || sourceCreature is null)
        {
            return false;
        }

        var chance = Jewel.GetJewelEffectMod(targetPlayer, PropertyInt.GearReprisal);

        if (ThreadSafeRandom.Next(0.0f, 1.0f) > chance)
        {
            return false;
        }

        targetPlayer.QuestManager.HandleReprisalQuest();
        targetPlayer.QuestManager.Stamp($"{sourceCreature.Guid}/Reprisal");

        resisted = true;

        var msg = $"Reprisal! You resist the spell cast by {sourceCreature.Name}";
        targetPlayer.Session.Network.EnqueueSend(new GameMessageSystemChat(msg, ChatMessageType.Magic));
        targetPlayer.Session.Network.EnqueueSend(new GameMessageSound(targetPlayer.Guid, Sound.ResistSpell));

        return true;
    }

    private float GetAbsorbMod(Creature target, WorldObject source)
    {
        switch (target.CombatMode)
        {
            case CombatMode.Melee:

                // does target have shield equipped?
                var shield = target.GetEquippedShield();
                if (shield != null && shield.GetAbsorbMagicDamage() != null)
                {
                    return GetShieldMod(target, shield, source);
                }

                break;

            case CombatMode.Missile:

                var missileLauncherOrShield = target.GetEquippedMissileLauncher() ?? target.GetEquippedShield();
                if (missileLauncherOrShield != null && missileLauncherOrShield.GetAbsorbMagicDamage() != null)
                {
                    return AbsorbMagic(target, missileLauncherOrShield);
                }

                break;

            case CombatMode.Magic:

                var caster = target.GetEquippedWand();
                if (caster != null && caster.GetAbsorbMagicDamage() != null)
                {
                    return AbsorbMagic(target, caster);
                }

                break;
        }
        return 1.0f;
    }

    /// <summary>
    /// Calculates the amount of damage a shield absorbs from magic projectile
    /// </summary>
    private static float GetShieldMod(Creature target, WorldObject shield, WorldObject source)
    {
        // COMBAT ABILITY - Phalanx: shields absorb spells from all angles
        if (target is not Player { PhalanxIsEffective: true })
        {
            // is spell projectile in front of creature target,
            // within shield effectiveness area?
            const float effectiveAngle = 180.0f;
            var angle = target.GetAngle(source);
            if (Math.Abs(angle) > effectiveAngle / 2.0f)
            {
                return 1.0f;
            }
        }

        var shieldSkill = target.GetCreatureSkill(Skill.Shield);
        // ensure trained?
        if (shieldSkill.AdvancementClass < SkillAdvancementClass.Trained || shieldSkill.Base < 100)
        {
            return 1.0f;
        }

        var cap = (float)(shield.GetAbsorbMagicDamage() ?? 0.0f);

        return MagicFormulas.GetShieldMagicAbsorbMod(
            cap,
            shieldSkill.Base,
            shieldSkill.AdvancementClass == SkillAdvancementClass.Specialized
        );
    }

    /// <summary>
    /// Calculates the damage reduction modifier for bows and casters
    /// with 'Magic Absorbing' property
    /// </summary>
    private static float AbsorbMagic(Creature target, WorldObject item)
    {
        var absorbMagicDamage = item.GetAbsorbMagicDamage();

        if (absorbMagicDamage == null)
        {
            return 1.0f;
        }

        return MagicFormulas.GetMagicAbsorbingMod(
            absorbMagicDamage.Value,
            target.GetCreatureSkill(Skill.MagicDefense).Base
        );
    }
}
