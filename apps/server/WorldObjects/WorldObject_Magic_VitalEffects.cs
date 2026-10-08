using System;
using ACE.Entity.Enum;
using ACE.Server.Entity;

namespace ACE.Server.WorldObjects;

partial class WorldObject
{
    private static void HandlePostDamageRatingEffects(Creature target, float damage, Player sourcePlayer, Player targetPlayer, Creature sourceCreature, Spell spell, ProjectileSpellType projectileSpellType)
    {
        if (sourcePlayer != null)
        {
            Jewel.HandleCasterAttackerRampingQuestStamps(sourcePlayer, target, spell, projectileSpellType);
            Jewel.HandlePlayerAttackerBonuses(sourcePlayer, target, damage, spell.DamageType);
        }

        if (targetPlayer != null)
        {
            Jewel.HandleCasterDefenderRampingQuestStamps(targetPlayer, sourceCreature);
            Jewel.HandlePlayerDefenderBonuses(targetPlayer, sourceCreature, damage);
        }
    }

    private static void HandlePostHealRatingEffects(Player sourcePlayer, Player targetPlayer)
    {
        if (sourcePlayer != null && targetPlayer != null)
        {
             Jewel.HandlePlayerHealerBonuses(sourcePlayer, targetPlayer);
        }
    }

    private static void ResetRatingElementalistQuestStamps(Player player)
    {
        // JEWEL - Elementalist Reset
        if (player != null)
        {
            if (player.QuestManager.HasQuest($"{player.Name},Elementalist"))
            {
                player.QuestManager.Erase($"{player.Name},Elementalist");
            }
        }
    }

    /// <summary>
    /// COMBAT ABILITY - Overload: Increased effectiveness up to 20% with Overload Charged stacks
    /// </summary>
    private static float CheckForCombatAbilityOverloadDamageBonus(Player player)
    {
        return player switch
        {
            { OverloadDischargeIsActive: true } => 1.0f + player.DischargeLevel,
            { OverloadStanceIsActive: true } => 1.0f + player.ManaChargeMeter * 0.2f,
            _ => 1.0f
        };
    }

    /// <summary>
    /// COMBAT ABILITY - Battery: Reduced effectiveness up to 10% with Battery Charged stacks
    /// </summary>
    private static float CheckForCombatAbilityBatteryDamagePenalty(Player player)
    {
        return player is { BatteryStanceIsActive: true } ? 1.0f - player.ManaChargeMeter * 0.1f : 1.0f;
    }

    /// <summary>
    /// The damage bonus from the fellowship leader's dungeon mod (Landblock.GetLandblockLethalityMod) for a spell cast by this object.
    /// Only for the dungeon's traps and the creatures the archetype system doesn't scale: archetype creatures get the bonus
    /// through their lethality (Creature.ApplyDungeonMods), and players and their pets never get it, since the mod makes the
    /// dungeon's enemies stronger, not the fellowship.
    /// </summary>
    public float GetLandblockLethalitySpellMod()
    {
        if (this is Player or CombatPet or Creature { ArchetypeSystemApplies: true } || CurrentLandblock is null)
        {
            return 1.0f;
        }

        return 1.0f + (float)CurrentLandblock.GetLandblockLethalityMod();
    }

    /// <summary>
    /// Checks for death from a boost / transfer spell
    /// </summary>
    private static void HandleBoostTransferDeath(Creature caster, Creature target)
    {
        if (caster is { IsDead: true })
        {
            caster.OnDeath(caster.DamageHistory.LastDamager, DamageType.Health, false);
            caster.Die();
        }

        if (target is { IsDead: true } && target != caster)
        {
            target.OnDeath(target.DamageHistory.LastDamager, DamageType.Health, false);
            target.Die();
        }
    }

    public float GetWardMod(Creature caster, Creature target, float ignoreWardMod)
    {
        var wardLevel = target.GetWardLevel();
        wardLevel += target.EnchantmentManager.GetWardAdditiveMod();

        if (caster is Player)
        {
            wardLevel = Convert.ToInt32(wardLevel * LevelScaling.GetMonsterArmorWardScalar(caster, target));
        }

        var wardBuffDebuffMod = target.EnchantmentManager.GetWardMultiplicativeMod();

        var wardMod = SkillFormula.CalcWardMod(wardLevel * ignoreWardMod * wardBuffDebuffMod);

        // level scaling scales the mitigation, not the ward level -- see LevelScaling.GetPlayerArmorWardModScalar().
        // A player caster only scales it in a scaled arena duel, where the higher fighter's ward counts as at the other's level
        if (target is Player && (caster is not Player || LevelScaling.IsScaledDuel(target, caster)))
        {
            wardMod *= LevelScaling.GetPlayerArmorWardModScalar(target, caster);
        }

        return wardMod;
    }
}
