using System;
using ACE.Entity.Enum;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.WorldObjects;

namespace ACE.Server.Entity;

public partial class DamageEvent
{
    /// <summary>
    /// Effects of making an attack, whether or not it hits
    /// </summary>
    private void ApplyOnAttackEffects(bool cleaveHits = false)
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

    /// <summary>
    /// Effects of an attack that got past evade, block and parry, once its damage is final
    /// </summary>
    private void ApplyOnHitEffects()
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

        var weaponTier = DamageFormulas.GetWeaponMasterTier(Weapon.Tier);

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
    private void ApplyWeaponMasterDebuff(
        SpellId spellId,
        PlayScript playScript,
        string message,
        int weaponTier,
        float powerLevel
    )
    {
        var tierMod = DamageFormulas.GetWeaponMasterTierMod(weaponTier);

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

        var weaponTypeDamageRange = DamageFormulas.GetBleedWeaponDamageRange(Weapon.WeaponSkill);

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

        var weaponDamageRollPercentile = DamageFormulas.GetBleedDamagePercentile(
            Weapon.Damage.Value,
            weaponTypeDamageRange.Value
        );

        var spell = new Spell(SpellId.Bleed);

        if (spell.NotFound)
        {
            return;
        }

        spell.SpellStatModVal = powerLevel * weaponDamageRollPercentile;

        _defender.EnchantmentManager.Add(spell, _playerAttacker, Weapon);
        _defender.EnqueueBroadcast(new GameMessageScript(_defender.Guid, PlayScript.DirtyFightingDamageOverTime));

        _playerAttacker.Session.Network.EnqueueSend(
            new GameMessageSystemChat($"You cause {_defender.Name} to bleed!", ChatMessageType.Broadcast)
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
