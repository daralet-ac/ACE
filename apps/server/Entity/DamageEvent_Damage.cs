using ACE.Common;
using ACE.DatLoader.Entity.AnimationHooks;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.WorldObjects;

namespace ACE.Server.Entity;

public partial class DamageEvent
{
    private float GetDamageBeforeMitigation()
    {
        SetBaseDamage();
        SetDamageModifiers();

        _criticalChance = GetCriticalChance();

        var roll = ThreadSafeRandom.Next(0.0f, 1.0f);
        var criticalRolled = roll <= _criticalChance;
        var criticalDefendedFromPerception = false;

        if (criticalRolled && !GetCriticalDefendedFromAug())
        {
            criticalDefendedFromPerception = CheckForSpecPerceptionCriticalDefense();
        }

        if (!criticalRolled || _criticalDefendedFromAug || criticalDefendedFromPerception)
        {
            _playerAttacker?.CheckForSigilTrinketOnAttackEffects(_defender, this, Skill.TwoHandedCombat, SigilTrinketShieldTwohandedCombatEffect.Might);
            _playerAttacker?.CheckForSigilTrinketOnAttackEffects(_defender, this, Skill.Shield, SigilTrinketShieldTwohandedCombatEffect.Might);

            if (!CriticalOverridedByTrinket)
            {
                if (criticalDefendedFromPerception)
                {
                    _playerDefender.Session.Network.EnqueueSend(
                        new GameMessageSystemChat(
                            "Your perception skill allowed you to prevent a critical strike!",
                            ChatMessageType.Broadcast
                        )
                    );
                }

                return GetNonCriticalDamageBeforeMitigation();
            }
        }

        IsCritical = true;
        return GetCriticalDamageBeforeMitigation();
    }

    private void SetBaseDamage()
    {
        if (_playerAttacker != null)
        {
            SetPlayerBaseDamage();
        }
        else
        {
            SetCreatureBaseDamage();
        }

        if (DamageType == DamageType.Undef)
        {
            if ((_attacker?.Guid.IsPlayer() ?? false) || (_damageSource?.Guid.IsPlayer() ?? false))
            {
                _log.Error(
                    $"DamageEvent.DoCalculateDamage({_attacker?.Name} ({_attacker?.Guid}), {_defender?.Name} ({_defender?.Guid}), {_damageSource?.Name} ({_damageSource?.Guid})) - DamageType == DamageType.Undef"
                );
                _generalFailure = true;
            }
        }
    }

    private void SetDamageModifiers(float? powerMod = null, bool consumeSneakAttackBonuses = true)
    {
        _damageModifiers.Power = powerMod ?? _attacker.GetPowerMod(Weapon);
        _damageModifiers.Attribute = _attacker.GetAttributeMod(Weapon, false);
        _damageModifiers.Slayer = WorldObject.GetWeaponCreatureSlayerModifier(Weapon, _attacker, _defender);
        _damageModifiers.DamageRating = Creature.GetPositiveRatingMod(_attacker.GetDamageRating());
        _damageModifiers.DualWield = GetDualWieldDamageBonus(_playerAttacker, _defender);
        _damageModifiers.TwoHandedCombat = GetTwohandedCombatDamageBonus(_playerAttacker, _defender);
        _damageModifiers.Fury = GetCombatAbilityFuryDamageBonus(_playerAttacker, _playerDefender);
        _damageModifiers.Relentless = GetCombatAbilityRelentlessDamagePenalty(_playerAttacker);
        _damageModifiers.SteadyStrike = GetCombatAbilitySteadyStrikeDamageBonus(_playerAttacker);
        _damageModifiers.Recklessness = Creature.GetRecklessnessMod(_attacker, _defender);

        // Sneak attack / Backstab bonuses (and their one-shot charges) should only be
        // consumed by an attacker's own normal attack - not by ancillary reactive damage
        // calculations like Thorns reflection or a Riposte counter-hit.
        if (consumeSneakAttackBonuses)
        {
            _damageModifiers.SneakAttack = _attacker.GetSneakAttackMod(_defender);
            _damageModifiers.Backstab = Creature.GetStealthBackstabDamageMultiplier(_playerAttacker, _defender);
        }
        else
        {
            _damageModifiers.SneakAttack = 1.0f;
            _damageModifiers.Backstab = 1.0f;
        }

        _damageModifiers.AttackHeight = GetHighAttackHeightBonus();
        _damageModifiers.ElementalRating = Jewel.HandleElementalBonuses(_playerAttacker, DamageType);
        _damageModifiers.PierceRating = GetRatingPierceResistanceBonus();
        _damageModifiers.LevelScaling = GetLevelScalingMod(_attacker, _defender, _playerDefender);
        _damageModifiers.Ammo = GetAmmoEffectMod(Weapon, _playerAttacker);

        if (!_pkBattle)
        {
            return;
        }

        _pkDamageMod = Creature.GetPositiveRatingMod(_attacker.GetPKDamageRating());
        _damageModifiers.DamageRating = Creature.AdditiveCombine(_damageModifiers.DamageRating, _pkDamageMod);
    }

    /// <summary>
    /// Dual Wield Damage Mod
    /// </summary>
    private static float GetDualWieldDamageBonus(Player playerAttacker, Creature defender)
    {
        if (playerAttacker is not {IsDualWieldAttack: true} || defender is null)
        {
            return 1.0f;
        }

        var moddedDualWieldCombatSkill = (uint)(playerAttacker.GetModdedDualWieldSkill() * 1.5f);
        var defenderPhysicalDefense = defender.GetModdedPhysicalDefSkill();

        var damageMod = 0.5f * SkillCheck.GetSkillChance(moddedDualWieldCombatSkill, defenderPhysicalDefense);

        var finalDamageMod = 1.0f + (float)damageMod;

        return finalDamageMod;
    }

    /// <summary>
    /// Two-handed Combat Damage Mod
    /// </summary>
    private static float GetTwohandedCombatDamageBonus(Player playerAttacker, Creature defender)
    {
        if (playerAttacker?.GetEquippedWeapon() is null
            || playerAttacker.GetEquippedWeapon().W_WeaponType is not WeaponType.TwoHanded
            || defender is null)
        {
            return 1.0f;
        }

        var moddedTwohandedCombatSkill = (uint)(playerAttacker.GetModdedTwohandedCombatSkill() * 1.5f);
        var defenderPhysicalDefense = defender.GetModdedPhysicalDefSkill();

        var damageMod = 0.5f * SkillCheck.GetSkillChance(moddedTwohandedCombatSkill, defenderPhysicalDefense);

        var finalDamageMod = 1.0f + (float)damageMod;

        return finalDamageMod;
    }

    /// <summary>
    /// COMBAT ABILITY - Fury (Stance): Damage dealt and taken is increased by up to 25%. Attacking and
    /// taking damage builds up a Adrenaline meter. Adrenaline meter decreases over time.
    /// COMBAT ABILITY - Fury (Enrage): Damage increased by Adrenaline build up amount (%) for 10 seconds.
    /// </summary>
    private static float GetCombatAbilityFuryDamageBonus(Player playerAttacker, Player playerDefender)
    {
        var recklessMod = 1.0f;

        if (playerAttacker is {FuryStanceIsActive: true})
        {
            recklessMod += 0.25f * playerAttacker.AdrenalineMeter;
        }

        if (playerDefender is { FuryStanceIsActive: true } or { FuryEnrageIsActive: true })
        {
            recklessMod += 0.25f * playerDefender.AdrenalineMeter;
        }

        if (playerAttacker is {FuryEnrageIsActive: true})
        {
            recklessMod += playerAttacker.EnrageLevel;
        }

        return recklessMod;
    }

    /// <summary>
    /// COMBAT ABILITY - Relentless (Stance): Up to -10% damage, based on relentless adrenaline stacks.
    /// </summary>
    private static float GetCombatAbilityRelentlessDamagePenalty(Player playerAttacker)
    {
        if (playerAttacker is not { RelentlessStanceIsActive: true })
        {
            return 1.0f;
        }

        return 1.0f - 0.1f * playerAttacker.AdrenalineMeter;
    }

    /// <summary>
    /// COMBAT ABILITY - Steady Strike: +25% damage with melee/missile weapons.
    /// </summary>
    /// <param name="playerAttacker"></param>
    /// <returns></returns>
    private static float GetCombatAbilitySteadyStrikeDamageBonus(Player playerAttacker)
    {
        if (playerAttacker?.GetEquippedWeapon() is null)
        {
            return 1.0f;
        }

        if (playerAttacker.GetPowerAccuracyBar() < 0.5f)
        {
            return 1.0f;
        }

        return playerAttacker is {SteadyStrikeIsActive: true} ? 1.25f : 1.0f;
    }

    /// <summary>
    /// ATTACK HEIGHT BONUS - High: (10% increased damage, 20% if weapon is specialized)
    /// </summary>
    private float GetHighAttackHeightBonus()
    {
        if (_playerAttacker is { AttackHeight: AttackHeight.High })
        {
            return WeaponIsSpecialized() ? 1.2f : 1.10f;
        }

        return 1.0f;
    }

    private static float GetLevelScalingMod(Creature attacker, Creature defender, Player playerDefender)
    {
        var monsterHealthScalingMod = playerDefender != null
            ? LevelScaling.GetMonsterDamageDealtHealthScalar(playerDefender, attacker)
            : LevelScaling.GetMonsterDamageTakenHealthScalar(attacker, defender);

        return monsterHealthScalingMod;
    }

    private float GetCriticalChance()
    {
        if (_playerDefender != null && (_playerDefender.IsLoggingOut || _playerDefender.PKLogout || _playerDefender.CombatMode is CombatMode.NonCombat))
        {
            return 1.0f;
        }

        if (CheckForRatingReprisal())
        {
            return 1.0f;
        }

        var criticalChance = WorldObject.GetWeaponCriticalChance(Weapon, _attacker, _attackSkill, _defender);
        criticalChance += GetPlayerSpecSkillCriticalChanceBonus();

        return criticalChance;
    }

    private bool GetCriticalDefendedFromAug()
    {
        _criticalDefendedFromAug = CheckForAugmentationCriticalDefense(_playerDefender, _playerAttacker);

        return _criticalDefendedFromAug;
    }

    private float GetCriticalDamageBeforeMitigation()
    {
        CriticalDamageBonusFromTrinket = 1.0f;
        _playerAttacker?.CheckForSigilTrinketOnAttackEffects(_defender, this, Skill.Thievery, SigilTrinketThieveryEffect.Treachery, true);

        _criticalDamageMod = 1.0f + WorldObject.GetWeaponCritDamageMod(Weapon, _attacker, _attackSkill, _defender);
        _criticalDamageMod += GetMaceSpecCriticalDamageBonus(_playerAttacker);
        _criticalDamageMod += GetStaffSpecCriticalDamageBonus(_playerAttacker);
        _criticalDamageMod *= 1.0f + Jewel.GetJewelEffectMod(_playerAttacker, PropertyInt.GearBludgeon, "Bludgeon", rampQuestSource: _defender);
        _criticalDamageMod *= CriticalDamageBonusFromTrinket;

        // RATING - Reprisal: the defender may evade the critical hit (see DoCalculateDamage)
        CheckForRatingReprisalCriticalDefense();

        // _damageModifiers.DamageRating already includes the PK damage rating (see SetDamageModifiers)
        _criticalDamageRating = Creature.GetPositiveRatingMod(_attacker.GetCritDamageRating());
        _damageModifiers.DamageRating = Creature.AdditiveCombine(_damageModifiers.DamageRating, _criticalDamageRating);

        if (_baseDamageMod is null)
        {
            _log.Error("GetCriticalDamageBeforeMitigation({Attacker}, {Defender}) - _baseDamageMod is null", _attacker.Name, _defender.Name);
            return 0;
        }

        // Intentional: player crits always use the top of the weapon's damage range, while monster crits use
        // the median of their attack's range. Non-critical hits use a random roll for both (see _baseDamage).
        var baseDamage = _playerAttacker != null ? _baseDamageMod.MaxDamage : _baseDamageMod.MedianDamage;

        return _damageModifiers.Apply(baseDamage) * _criticalDamageMod;
    }

    private float GetNonCriticalDamageBeforeMitigation()
    {
        return _damageModifiers.Apply(_baseDamage);
    }

    /// <summary>
    /// RATING - Pierce: Ramping Piercing Resistance Penetration.
    /// Up to +20% + 1% per rating (at max quest stamps).
    /// (JEWEL - Black Garnet)
    /// </summary>
    private float GetRatingPierceResistanceBonus()
    {
        if (_playerAttacker == null)
        {
            return 1.0f;
        }

        var rating = _playerAttacker.GetEquippedAndActivatedItemRatingSum(PropertyInt.GearPierce);

        if (rating <= 0 || DamageType != DamageType.Pierce)
        {
            return 1.0f;
        }

        var rampPercentage = (float)_defender.QuestManager.GetCurrentSolves($"{_playerAttacker.Name},Pierce") / 100;

        const float baseMod = 0.2f;
        const float bonusPerRating = 0.01f;

        return 1.0f + (rampPercentage * (baseMod + bonusPerRating * rating));
    }

    /// <summary>
    /// RATING - Reprisal: Evade an Incoming Crit, auto crit in return
    /// (JEWEL - Black Opal)
    /// </summary>
    private void CheckForRatingReprisalCriticalDefense()
    {
        if (_playerDefender == null)
        {
            return;
        }

        var rating = _playerDefender.GetEquippedAndActivatedItemRatingSum(PropertyInt.GearReprisal);

        if (rating <= 0)
        {
            return;
        }

        var chance = Jewel.GetJewelEffectMod(_playerDefender, PropertyInt.GearReprisal);

        if (ThreadSafeRandom.Next(0.0f, 1.0f) > chance)
        {
            return;
        }

        _playerDefender.QuestManager.HandleReprisalQuest();
        _playerDefender.QuestManager.Stamp($"{_attacker.Guid}/Reprisal");
        Evaded = true;
        PartialEvasion = PartialEvasion.All;
        _playerDefender.Reprisal = true;

        var msg = $"Reprisal! You evade the attack by {_attacker.Name}";
        _playerDefender.Session.Network.EnqueueSend(new GameMessageSystemChat(msg, ChatMessageType.CombatEnemy));
    }

    /// <summary>
    /// SPEC BONUS - Staff: +50% crit damage (additively)
    /// </summary>
    private static float GetStaffSpecCriticalDamageBonus(Player playerAttacker)
    {
        if (playerAttacker?.GetEquippedWeapon() == null)
        {
            return 0.0f;
        }

        return IsWeaponSkillSpecialized(playerAttacker, Skill.Staff) ? 0.5f : 0.0f;
    }

    /// <summary>
    /// SPEC BONUS - Martial Weapons (Mace): +50% crit damage (additively)
    /// </summary>
    private static float GetMaceSpecCriticalDamageBonus(Player playerAttacker)
    {
        if (playerAttacker?.GetEquippedWeapon() == null)
        {
            return 0.0f;
        }

        return IsWeaponSkillSpecialized(playerAttacker, Skill.Mace) ? 0.5f : 0.0f;
    }

    /// <summary>
    /// SPEC BONUS - Perception - Up to 50% chance to defend against a critical hit, based on Perception vs the attacker's effective attack skill
    /// </summary>
    private bool CheckForSpecPerceptionCriticalDefense()
    {
        if (_playerDefender == null)
        {
            return false;
        }

        var perception = _playerDefender.GetCreatureSkill(Skill.Perception);
        if (perception.AdvancementClass != SkillAdvancementClass.Specialized)
        {
            return false;
        }

        // an attack skill of 0 can't beat any Perception, so it gets the full 50%
        var skillCheck = EffectiveAttackSkill > 0 ? _playerDefender.GetModdedPerceptionSkill() / (float)EffectiveAttackSkill : 1.0f;
        var criticalDefenseChance = skillCheck > 1f ? 0.5f : skillCheck * 0.5f;

        return criticalDefenseChance > ThreadSafeRandom.Next(0f, 1f);
    }

    private static bool CheckForAugmentationCriticalDefense(Player playerDefender, Player playerAttacker)
    {
        if (playerDefender == null || playerDefender.AugmentationCriticalDefense <= 0)
        {
            return false;
        }

        var criticalDefenseMod = playerAttacker != null ? 0.05f : 0.25f;
        var criticalDefenseChance = playerDefender.AugmentationCriticalDefense * criticalDefenseMod;

        return !(criticalDefenseChance < ThreadSafeRandom.Next(0.0f, 1.0f));
    }

    private bool CheckForRatingReprisal()
    {
        if (_playerAttacker == null)
        {
            return false;
        }

        if (_playerAttacker.GetEquippedAndActivatedItemRatingSum(PropertyInt.GearReprisal) <= 0)
        {
            return false;
        }

        if (!_playerAttacker.QuestManager.HasQuest($"{_defender.Guid}/Reprisal"))
        {
            return false;
        }

        _playerAttacker.QuestManager.Erase($"{_defender.Guid}/Reprisal");
        return true;
    }

    /// <summary>
    /// SPEC BONUS - Axe/Dagger: +5% crit chance
    /// </summary>
    private float GetPlayerSpecSkillCriticalChanceBonus()
    {
        if (_playerAttacker?.GetEquippedWeapon() == null)
        {
            return 0.0f;
        }

        // SPEC BONUS - Martial Weapons (Axe): +5% crit chance (additively)
        if (IsWeaponSkillSpecialized(_playerAttacker, Skill.Axe))
        {
            return 0.05f;
        }

        // SPEC BONUS - Dagger: +5% crit chance (additively)
        if (IsWeaponSkillSpecialized(_playerAttacker, Skill.Dagger))
        {
            return 0.05f;
        }

        return 0.0f;
    }

    /// <summary>
    /// Sets the damage type, damage range and rolled base damage for a player attacker
    /// </summary>
    private void SetPlayerBaseDamage()
    {
        if (_damageSource.ItemType == ItemType.MissileWeapon)
        {
            DamageType = _damageSource.W_DamageType;

            // handle prismatic arrows
            if (DamageType == DamageType.Base)
            {
                if (Weapon != null && Weapon.W_DamageType != DamageType.Undef)
                {
                    DamageType = Weapon.W_DamageType;
                }
                else
                {
                    DamageType = DamageType.Pierce;
                }
            }
        }
        else
        {
            DamageType = _playerAttacker.GetDamageType(false, CombatType.Melee);
        }

        // TODO: combat maneuvers for player?
        _baseDamageMod = _playerAttacker.GetBaseDamageMod(_damageSource);

        // some quest bows can have built-in damage bonus
        if (Weapon?.WeenieType == WeenieType.MissileLauncher)
        {
            _baseDamageMod.DamageBonus += Weapon.Damage ?? 0;
        }

        if (_damageSource.ItemType == ItemType.MissileWeapon)
        {
            _baseDamageMod.ElementalBonus = WorldObject.GetMissileElementalDamageBonus(Weapon, _playerAttacker, DamageType);
            _baseDamageMod.DamageMod = WorldObject.GetMissileElementalDamageModifier(Weapon, DamageType);
        }

        _baseDamage = (float)ThreadSafeRandom.Next(_baseDamageMod.MinDamage, _baseDamageMod.MaxDamage);
    }

    /// <summary>
    /// Sets the attacking body part, damage range, rolled base damage and damage type for a non-player attacker
    /// </summary>
    private void SetCreatureBaseDamage()
    {
        _attackPart = _attacker.GetAttackPart(_attackMotion ?? MotionCommand.Invalid, _attackHook);
        if (_attackPart.Value == null)
        {
            _generalFailure = true;
            return;
        }

        _baseDamageMod = _attacker.GetBaseDamage(_attackPart.Value);
        _baseDamage = (float)ThreadSafeRandom.Next(_baseDamageMod.MinDamage, _baseDamageMod.MaxDamage);

        DamageType = _attacker.GetDamageType(_attackPart.Value, CombatType);
    }

    private static float GetAmmoEffectMod(WorldObject weapon, Player player)
    {
        if (weapon is {IsAmmoLauncher: not true} || player is null)
        {
            return 1.0f;
        }

        var ammo = player.GetEquippedAmmo() as Ammunition;

        if (ammo?.AmmoEffectUsesRemaining is null)
        {
            return 1.0f;
        }

        switch ((AmmoEffect)(ammo.AmmoEffect ?? 0))
        {
            case AmmoEffect.Sharpened: return 1.1f;
            default: return 1.0f;
        }
    }
}
