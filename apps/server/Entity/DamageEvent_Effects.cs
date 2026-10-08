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

    private void PostDamageMitigationEffects()
    {
        // jewel stamps and procs need the attack to land; the rest also trigger on a late evade (see DoCalculateDamage)
        if (!Evaded)
        {
            CheckForRatingPostDamageEffects();
        }

        CheckForCombatAbilityFuryBuildUpWhenDamaged();
        CheckForCombatAbilityAegisRestoration();
        CheckForWeaponMasterEffects();
        CheckForEnchantedBlade(_playerAttacker, _defender, _attackHeight);
    }

    private void CheckForWeaponMasterEffects()
    {
        if (_playerAttacker is not { WeaponMasterSingleUseIsActive: true } || Weapon is null)
        {
            return;
        }

        var powerLevel = _playerAttacker.GetPowerAccuracyBar();

        if (powerLevel < 0.5)
        {
            return;
        }

        var weaponTier = Math.Clamp((Weapon.Tier ?? 1) - 1, 1, 7);

        switch (Weapon.WeaponSkill)
        {
            case Skill.Axe:
            case Skill.Dagger:

                WeaponMasterBleed(powerLevel);

                break;
            case Skill.Mace:
            case Skill.Staff:

                WeaponMasterDaze(weaponTier, powerLevel);

                break;
            case Skill.UnarmedCombat:

                WeaponMasterOffBalance(weaponTier, powerLevel);

                break;
            case Skill.ThrownWeapon:

                if (Weapon.Name.Contains("Dagger") || Weapon.Name.Contains("Axe"))
                {
                    WeaponMasterBleed(powerLevel);
                }
                else if (Weapon.Name.Contains("Club"))
                {
                    WeaponMasterDaze(weaponTier, powerLevel);
                }
                else if (Weapon.Name.Contains("Shouken"))
                {
                    WeaponMasterOffBalance(weaponTier, powerLevel);
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

    private void WeaponMasterOffBalance(int weaponTier, float powerLevel)
    {
        ApplyWeaponMasterDebuff(
            SpellId.Unbalanced,
            PlayScript.DirtyFightingDefenseDebuff,
            $"You put {_defender.Name} off-balance, reducing their defense skill!",
            weaponTier,
            powerLevel
        );
    }

    private void WeaponMasterDaze(int weaponTier, float powerLevel)
    {
        ApplyWeaponMasterDebuff(
            SpellId.Dazed,
            PlayScript.DirtyFightingAttackDebuff,
            $"You daze {_defender.Name}, reducing their attack skill!",
            weaponTier,
            powerLevel
        );
    }

    /// <summary>
    /// Applies a Weapon Master debuff spell to the defender, scaled by power bar and weapon tier, and uses up Weapon Master
    /// </summary>
    private void ApplyWeaponMasterDebuff(SpellId spellId, PlayScript playScript, string message, int weaponTier, float powerLevel)
    {
        var tierMod = GetWeaponMasterTierMod(weaponTier);

        var debuffSpell = new Spell(spellId);

        if (debuffSpell.NotFound)
        {
            return;
        }

        debuffSpell.SpellStatModVal *= powerLevel * tierMod;

        _defender.EnchantmentManager.Add(debuffSpell, _playerAttacker, Weapon);

        _defender.EnqueueBroadcast(new GameMessageScript(_defender.Guid, playScript));

        _playerAttacker.Session.Network.EnqueueSend(new GameMessageSystemChat(message, ChatMessageType.Broadcast));

        _playerAttacker.WeaponMasterSingleUseIsActive = false;
    }

    /// <summary>
    /// Weapon Master debuff strength multiplier for a weapon tier (1-7, see CheckForWeaponMasterEffects)
    /// </summary>
    private static float GetWeaponMasterTierMod(int weaponTier)
    {
        return weaponTier switch
        {
            1 => 1.0f,
            2 => 3.0f,
            3 => 4.0f,
            4 => 5.0f,
            5 => 6.0f,
            6 => 8.0f,
            7 => 10.0f,
            _ => throw new ArgumentOutOfRangeException(nameof(weaponTier), weaponTier, null)
        };
    }

    /// <summary>
    /// Triggers a bleed damage DoT on the target.
    /// Base damage of Bleed spell is 500. Damage is reduced depending on weapon damage roll
    /// and power bar level setting.
    /// </summary>
    private void WeaponMasterBleed(float powerLevel)
    {
        if (Weapon.Damage is null)
        {
            return;
        }

        // todo: use each weapon's subtype for its damage range once all lootgen weapons have one

        (int Min, int Max)? weaponTypeDamageRange = Weapon.WeaponSkill switch
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
                _playerAttacker.Name,
                Weapon.Name,
                Weapon.WeaponSkill
            );
            return;
        }

        var (weaponTypeMinDamage, weaponTypeMaxDamage) = weaponTypeDamageRange.Value;

        var damageRange = weaponTypeMaxDamage - weaponTypeMinDamage;
        var weaponDamageRoll = Weapon.Damage.Value - weaponTypeMinDamage;

        // weapons outside the expected range for their type still bleed for between 0% and 100%
        var weaponDamageRollPercentile = Math.Clamp((float)weaponDamageRoll / damageRange, 0.0f, 1.0f);

        var spell = new Spell(SpellId.Bleed);

        if (spell.NotFound)
        {
            return;
        }

        spell.SpellStatModVal = powerLevel * weaponDamageRollPercentile;

        _defender.EnchantmentManager.Add(spell, _playerAttacker, Weapon);
        _defender.EnqueueBroadcast(new GameMessageScript(_defender.Guid, PlayScript.DirtyFightingDamageOverTime));

        _playerAttacker.Session.Network.EnqueueSend(
            new GameMessageSystemChat(
                $"You cause {_defender.Name} to bleed!",
                ChatMessageType.Broadcast
            )
        );

        _playerAttacker.WeaponMasterSingleUseIsActive = false;
    }

    /// <summary>
    /// RATING (jewels) POST-DAMAGE STAMPS / PROCS / BONUSES
    /// </summary>
    private void CheckForRatingPostDamageEffects()
    {
        if (_playerAttacker != null)
        {
            Jewel.HandlePlayerAttackerBonuses(_playerAttacker, _defender, Damage, DamageType);
            Jewel.HandleMeleeMissileAttackerRampingQuestStamps(_playerAttacker, _defender, DamageType);
        }

        if (_playerDefender != null)
        {
            Jewel.HandlePlayerDefenderBonuses(_playerDefender, _attacker, Damage);
            Jewel.HandleMeleeMissileDefenderRampingQuestStamps(_playerDefender, _attacker);
        }
    }

    /// <summary>
    /// COMBAT ABILITY - Fury (build-up).
    /// </summary>
    private void CheckForCombatAbilityFuryBuildUpWhenDamaged()
    {
        if (_playerDefender is { FuryStanceIsActive: true })
        {
            var furyGained = Damage / _playerDefender.Health.MaxValue / 10;
            _playerDefender.AdrenalineMeter += furyGained;

            if (_playerDefender.AdrenalineMeter > 1.0f)
            {
                _playerDefender.AdrenalineMeter = 1.0f;
            }
        }
    }

    /// <summary>
    /// COMBAT ABILITY - Aegis: Restore stamina and mana equal to 10% of the damage prevented by Aegis.
    /// </summary>
    private void CheckForCombatAbilityAegisRestoration()
    {
        // _mitigationModifiers.Aegis is 1.0 unless Aegis reduced this hit
        if (_playerDefender is null || _mitigationModifiers.Aegis is <= 0.0f or >= 1.0f)
        {
            return;
        }

        // Damage already includes the Aegis reduction, so this is the amount Aegis alone prevented
        var damagePrevented = Damage / _mitigationModifiers.Aegis - Damage;
        var restoreAmount = (int)Math.Round(damagePrevented * Player.AegisRestorationMod);

        if (restoreAmount <= 0)
        {
            return;
        }

        _playerDefender.UpdateVitalDelta(_playerDefender.Stamina, restoreAmount);
        _playerDefender.UpdateVitalDelta(_playerDefender.Mana, restoreAmount);
    }
}
