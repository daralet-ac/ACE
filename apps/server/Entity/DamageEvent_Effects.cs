using System;
using ACE.Entity.Enum;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.WorldObjects;

namespace ACE.Server.Entity;

public partial class DamageEvent
{
    private void CheckForOnAttackEffects(bool cleaveHits = false)
    {
        if (cleaveHits)
        {
            return;
        }

        if (Weapon is not null && _playerAttacker is { RelentlessStanceIsActive: true })
        {
            _playerAttacker.IncreaseRelentlessAdrenalineMeter(Weapon);
        }

        if (Weapon is not null && _playerAttacker is { FuryStanceIsActive: true })
        {
            _playerAttacker.IncreaseFuryAdrenalineMeter(Weapon);
        }
    }

    private void PostDamageMitigationEffects(Creature attacker, Creature defender, WorldObject damageSource)
    {
        var playerAttacker = attacker as Player;
        var playerDefender = defender as Player;

        // jewel stamps and procs need the attack to land; the rest also trigger on a late evade (see DoCalculateDamage)
        if (!Evaded)
        {
            CheckForRatingPostDamageEffects(attacker, defender, damageSource, playerAttacker, playerDefender);
        }

        CheckForCombatAbilityFuryBuildUpWhenDamaged(playerDefender);
        CheckForCombatAbilityAegisRestoration(playerDefender);
        CheckForWeaponMasterEffects(playerAttacker, defender);
        CheckForEnchantedBlade(playerAttacker, defender, _attackHeight);
    }

    private void CheckForWeaponMasterEffects(Player playerAttacker, Creature defender)
    {

        if (playerAttacker is not { WeaponMasterSingleUseIsActive: true } || Weapon is null)
        {
            return;
        }

        var powerLevel = playerAttacker.GetPowerAccuracyBar();

        if (powerLevel < 0.5)
        {
            return;
        }

        var weaponTier = Math.Clamp((Weapon.Tier ?? 1) - 1, 1, 7);

        switch (Weapon.WeaponSkill)
        {
            case Skill.Axe:
            case Skill.Dagger:

                WeaponMasterBleed(playerAttacker, defender, Weapon, powerLevel);

                break;
            case Skill.Mace:
            case Skill.Staff:

                WeaponMasterDaze(playerAttacker, defender, weaponTier, powerLevel);

                break;
            case Skill.UnarmedCombat:

                WeaponMasterOffBalance(playerAttacker, defender, weaponTier, powerLevel);

                break;
            case Skill.ThrownWeapon:

                if (Weapon.Name.Contains("Dagger") || Weapon.Name.Contains("Axe"))
                {
                    WeaponMasterBleed(playerAttacker, defender, Weapon, powerLevel);
                }
                else if (Weapon.Name.Contains("Club"))
                {
                    WeaponMasterDaze(playerAttacker, defender, weaponTier, powerLevel);
                }
                else if (Weapon.Name.Contains("Shouken"))
                {
                    WeaponMasterOffBalance(playerAttacker, defender, weaponTier, powerLevel);
                }

                break;
        }
    }

    private static void CheckForEnchantedBlade(Player player, Creature target, AttackHeight attackHeight)
    {
        if (player is null)
        {
            return;
        }

        var spell = attackHeight switch
        {
            AttackHeight.High => player.EnchantedBladeHighStoredSpell,
            AttackHeight.Medium => player.EnchantedBladeMedStoredSpell,
            AttackHeight.Low => player.EnchantedBladeLowStoredSpell,
            _ => null
        };

        if (spell is null)
        {
            return;
        }

        var weapon = player.GetEquippedMeleeWeapon();

        if (weapon is null)
        {
            return;
        }

        var spellCraft = weapon.ItemSpellcraft ?? 1;

        // power bar adds +0% to +100% damage to enchanted blade spells
        var powerBarDamageMultiplier = 1.0 + player.PowerLevel;

        player.TryCastSpell(spell, target, null, weapon, false, true, true, true, spellCraft, powerBarDamageMultiplier);

        player.EnchantedBladeHighStoredSpell = null;
        player.EnchantedBladeMedStoredSpell = null;
        player.EnchantedBladeLowStoredSpell = null;
    }

    private void WeaponMasterOffBalance(Player playerAttacker, Creature defender, int weaponTier, float powerLevel)
    {
        float tierMod;
        tierMod = weaponTier switch
        {
            1 => 1.0f,
            2 => 3.0f,
            3 => 4.0f,
            4 => 5.0f,
            5 => 6.0f,
            6 => 8.0f,
            7 => 10.0f,
            _ => throw new ArgumentOutOfRangeException()
        };

        var defenseDebuffSpell = new Spell(SpellId.Unbalanced);

        if (defenseDebuffSpell.NotFound)
        {
            return;
        }

        defenseDebuffSpell.SpellStatModVal *= powerLevel * tierMod;

        defender.EnchantmentManager.Add(defenseDebuffSpell, playerAttacker, Weapon);

        defender.EnqueueBroadcast(new GameMessageScript(defender.Guid, PlayScript.DirtyFightingDefenseDebuff));

        playerAttacker.Session.Network.EnqueueSend(
            new GameMessageSystemChat(
                $"You put {defender.Name} off-balance, reducing their defense skill!",
                ChatMessageType.Broadcast
            )
        );

        playerAttacker.WeaponMasterSingleUseIsActive = false;
    }

    private void WeaponMasterDaze(Player playerAttacker, Creature defender, int weaponTier, float powerLevel)
    {
        float tierMod;
        tierMod = weaponTier switch
        {
            1 => 1.0f,
            2 => 3.0f,
            3 => 4.0f,
            4 => 5.0f,
            5 => 6.0f,
            6 => 8.0f,
            7 => 10.0f,
            _ => throw new ArgumentOutOfRangeException()
        };

        var attackDebuffSpell = new Spell(SpellId.Dazed);

        if (attackDebuffSpell.NotFound)
        {
            return;
        }

        attackDebuffSpell.SpellStatModVal *= powerLevel * tierMod;

        defender.EnchantmentManager.Add(attackDebuffSpell, playerAttacker, Weapon);

        defender.EnqueueBroadcast(new GameMessageScript(defender.Guid, PlayScript.DirtyFightingAttackDebuff));

        playerAttacker.Session.Network.EnqueueSend(
            new GameMessageSystemChat(
                $"You daze {defender.Name}, reducing their attack skill!",
                ChatMessageType.Broadcast
            )
        );

        playerAttacker.WeaponMasterSingleUseIsActive = false;
    }

    /// <summary>
    /// Triggers a bleed damage DoT on the target.
    /// Base damage of Bleed spell is 500. Damage is reduced depending on weapon damage roll
    /// and power bar level setting.
    /// </summary>
    private void WeaponMasterBleed(Player playerAttacker, Creature defender, WorldObject weapon, float powerLevel)
    {
        if (weapon.Damage is null)
        {
            return;
        }

        // todo: use each weapon's subtype for its damage range once all lootgen weapons have one

        (int Min, int Max)? weaponTypeDamageRange = weapon.WeaponSkill switch
        {
            Skill.Axe => (9, 132),
            Skill.Dagger => (4, 95),
            Skill.ThrownWeapon => (10, 296),
            Skill.TwoHandedCombat => (5, 107),
            _ => null
        };

        if (weaponTypeDamageRange is null)
        {
            _log.Warning(
                "WeaponMasterBleed({Attacker}, {Weapon}) - no damage range for weapon skill {WeaponSkill}",
                playerAttacker.Name,
                weapon.Name,
                weapon.WeaponSkill
            );
            return;
        }

        var (weaponTypeMinDamage, weaponTypeMaxDamage) = weaponTypeDamageRange.Value;

        var damageRange = weaponTypeMaxDamage - weaponTypeMinDamage;
        var weaponDamageRoll = weapon.Damage.Value - weaponTypeMinDamage;

        // weapons outside the expected range for their type still bleed for between 0% and 100%
        var weaponDamageRollPercentile = Math.Clamp((float)weaponDamageRoll / damageRange, 0.0f, 1.0f);

        var spell = new Spell(SpellId.Bleed);

        if (spell.NotFound)
        {
            return;
        }

        spell.SpellStatModVal = powerLevel * weaponDamageRollPercentile;

        defender.EnchantmentManager.Add(spell, playerAttacker, Weapon);
        defender.EnqueueBroadcast(new GameMessageScript(defender.Guid, PlayScript.DirtyFightingDamageOverTime));

        playerAttacker.Session.Network.EnqueueSend(
            new GameMessageSystemChat(
                $"You cause {defender.Name} to bleed!",
                ChatMessageType.Broadcast
            )
        );

        playerAttacker.WeaponMasterSingleUseIsActive = false;
    }

    /// <summary>
    /// RATING (jewels) POST-DAMAGE STAMPS / PROCS / BONUSES
    /// </summary>
    private void CheckForRatingPostDamageEffects(
        Creature attacker,
        Creature defender,
        WorldObject damageSource,
        Player playerAttacker,
        Player playerDefender
    )
    {
        if (playerAttacker != null)
        {
            Jewel.HandlePlayerAttackerBonuses(playerAttacker, defender, Damage, DamageType);
            Jewel.HandleMeleeMissileAttackerRampingQuestStamps(playerAttacker, defender, DamageType);
        }

        if (playerDefender != null)
        {
            Jewel.HandlePlayerDefenderBonuses(playerDefender, attacker, Damage);
            Jewel.HandleMeleeMissileDefenderRampingQuestStamps(playerDefender, attacker);
        }
    }

    /// <summary>
    /// COMBAT ABILITY - Fury (build-up).
    /// </summary>
    private void CheckForCombatAbilityFuryBuildUpWhenDamaged(Player playerDefender)
    {
        if (playerDefender is { FuryStanceIsActive: true })
        {
            var furyGained = Damage / playerDefender.Health.MaxValue / 10;
            playerDefender.AdrenalineMeter += furyGained;

            if (playerDefender.AdrenalineMeter > 1.0f)
            {
                playerDefender.AdrenalineMeter = 1.0f;
            }
        }
    }

    /// <summary>
    /// COMBAT ABILITY - Aegis: Restore stamina and mana equal to 10% of the damage prevented by Aegis.
    /// </summary>
    private void CheckForCombatAbilityAegisRestoration(Player playerDefender)
    {
        // _combatAbilityAegisDamageReduction is 1.0 unless Aegis reduced this hit
        if (playerDefender is null || _combatAbilityAegisDamageReduction is <= 0.0f or >= 1.0f)
        {
            return;
        }

        // Damage already includes the Aegis reduction, so this is the amount Aegis alone prevented
        var damagePrevented = Damage / _combatAbilityAegisDamageReduction - Damage;
        var restoreAmount = (int)Math.Round(damagePrevented * Player.AegisRestorationMod);

        if (restoreAmount <= 0)
        {
            return;
        }

        playerDefender.UpdateVitalDelta(playerDefender.Stamina, restoreAmount);
        playerDefender.UpdateVitalDelta(playerDefender.Mana, restoreAmount);
    }
}
