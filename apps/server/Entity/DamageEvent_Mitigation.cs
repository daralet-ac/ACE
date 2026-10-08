using System;
using System.Collections.Generic;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Entity.Models;
using ACE.Server.WorldObjects;

namespace ACE.Server.Entity;

public partial class DamageEvent
{
    /// <summary>
    /// COMBAT ABILITY - Provoke: Damage taken reduced by 15%.
    /// </summary>
    private static float GetCombatAbilityProvokeDamageReduction(Player playerDefender)
    {
        return playerDefender is { ProvokeIsActive: true } ? 0.85f : 1.0f;
    }

    /// <summary>
    /// COMBAT ABILITY - Aegis: Damage taken from weapon attacks reduced by 50%. Attacks can't be evaded while active (see SetEvaded).
    /// </summary>
    private float GetCombatAbilityAegisDamageReduction()
    {
        if (_playerDefender is not { AegisIsActive: true } || Evaded)
        {
            return 1.0f;
        }

        return 1.0f - Player.AegisDamageReduction;
    }

    /// <summary>
    /// COMBAT ABILITY - Phalanx: Damage taken from full hits reduced by 30%. Glancing blows are unaffected.
    /// </summary>
    private float GetCombatAbilityPhalanxDamageReduction()
    {
        if (_playerDefender is null || Evaded || PartialEvasion != PartialEvasion.None)
        {
            return 1.0f;
        }

        return _playerDefender.GetPhalanxFullHitDamageMod();
    }

    private float GetMitigation()
    {
        if (_attacker is null || _defender is null)
        {
            return 1.0f;
        }

        _ignoreArmorMod = GetIgnoreArmorMod();
        _ignoreArmorMod -= GetSpearSpecIgnoreArmorBonus(_playerAttacker);

        _mitigationModifiers.Armor = GetArmorMod();

        _weaponResistanceMod = WorldObject.GetWeaponResistanceModifier(
            Weapon,
            _attacker,
            _attackSkill,
            DamageType,
            _defender
        );

        _mitigationModifiers.Resistance = GetResistanceMod();

        // Piercing resistance penetration (Black Garnet / Precision Strikes) is applied as a damage
        // multiplier via _damageModifiers.PierceRating in GetRatingPierceResistanceBonus(); it must
        // not also be folded into the target's resistance here.

        _mitigationModifiers.DamageResistanceRating = GetDamageResistRatingMod();
        _mitigationModifiers.DamageResistanceRating *=
            1.0f - GetRatingHardenedDefenseDamageResistanceBonus(_playerDefender);

        _mitigationModifiers.SpecDefense = GetSpecDefenseMod(_attacker, _playerDefender);

        _mitigationModifiers.Shield = _defender.GetShieldMod(_attacker, DamageType, Weapon);

        _mitigationModifiers.Provoke = GetCombatAbilityProvokeDamageReduction(_playerDefender);
        _mitigationModifiers.Aegis = GetCombatAbilityAegisDamageReduction();
        _mitigationModifiers.Phalanx = GetCombatAbilityPhalanxDamageReduction();

        _mitigationModifiers.SelfHarm = 1.0f + Jewel.GetJewelEffectMod(_playerAttacker, PropertyInt.GearSelfHarm);
        _mitigationModifiers.RedFury = 1.0f + Jewel.GetJewelRedFury(_playerAttacker);
        _mitigationModifiers.YellowFury = 1.0f + Jewel.GetJewelYellowFury(_playerAttacker);
        _mitigationModifiers.DamageTypeWard = DamageType switch
        {
            var dt when (dt & DamageType.Physical) != 0
                => 1.0f - Jewel.GetJewelEffectMod(_playerDefender, PropertyInt.GearPhysicalWard),
            var dt when (dt & DamageType.Elemental) != 0
                => 1.0f - Jewel.GetJewelEffectMod(_playerDefender, PropertyInt.GearElementalWard),
            _ => 1.0f
        };

        _mitigationModifiers.Swarmed = GetSwarmedMod(_playerDefender);

        _mitigationModifiers.ImbuedArmorPhysical = GetImbuedArmorPhysicalDamageMod(_defender);
        _mitigationModifiers.ImbuedArmorCritical = GetImbuedArmorCritDamageMod();

        _mitigationModifiers.Evasion = _evasionMod;

        return _mitigationModifiers.Product();
    }

    private static float GetImbuedArmorPhysicalDamageMod(Creature defender)
    {
        return DamageFormulas.GetImbuedArmorMod(
            defender.GetArmorDefenseImbues(ImbuedEffectType.ReducedPhysicalDamageTaken)
        );
    }

    private float GetImbuedArmorCritDamageMod()
    {
        if (!IsCritical)
        {
            return 1.0f;
        }

        return DamageFormulas.GetImbuedArmorMod(
            _defender.GetArmorDefenseImbues(ImbuedEffectType.ReducedCriticalDamageTaken)
        );
    }

    /// <summary>
    /// Calculates a modifier that reduces damage taken for a defending player based on the number of
    /// nearby enemies.
    /// </summary>
    /// <remarks>The modifier is multiplicatively reduced by 10% for each nearby enemy beyond the first,
    /// within a radius of 3 units. This effect only applies if the player has a melee weapon equipped.</remarks>
    /// <param name="playerDefender">The player who is defending and whose damage reduction will be modified. Cannot be null.</param>
    /// <returns>A floating-point value representing the swarmed modifier. Returns 1.0 if the player or their equipped melee
    /// weapon is null, or if there is one or no nearby enemy; otherwise, returns a value less than 1.0 that decreases
    /// as the number of nearby enemies increases.</returns>
    private static float GetSwarmedMod(Player playerDefender)
    {
        if (playerDefender is null || playerDefender.GetEquippedMeleeWeapon() is null)
        {
            return 1.0f;
        }

        return DamageFormulas.GetSwarmedMod(playerDefender.GetNearbyMonsters(3).Count);
    }

    private float GetIgnoreArmorMod()
    {
        if (Weapon is null or { SpecialPropertiesRequireMana: true, ItemCurMana: 0 })
        {
            return 1.0f;
        }

        var armorRendingMod = GetArmorRendingMod();
        var armorCleavingMod = _attacker.GetArmorCleavingMod(Weapon);

        return armorCleavingMod - (1.0f - armorRendingMod);
    }

    private float GetArmorRendingMod()
    {
        if (Weapon is null or { SpecialPropertiesRequireMana: true, ItemCurMana: 0 })
        {
            return 1.0f;
        }

        if (Weapon.HasImbuedEffect(ImbuedEffectType.ArmorRending))
        {
            return 1.0f - WorldObject.GetArmorRendingMod(_attackSkill, _playerAttacker, _defender);
        }

        return 1.0f;
    }

    /// <summary>
    /// SPEC BONUS - Martial Weapons (Spear): +10% armor penetration (additively)
    /// </summary>
    private static float GetSpearSpecIgnoreArmorBonus(Player playerAttacker)
    {
        if (playerAttacker?.GetEquippedWeapon() == null)
        {
            return 0.0f;
        }

        return IsWeaponSkillSpecialized(playerAttacker, Skill.Spear) ? 0.1f : 0.0f;
    }

    private float GetArmorMod()
    {
        if (Weapon != null && Weapon.HasImbuedEffect(ImbuedEffectType.IgnoreAllArmor))
        {
            return 1.0f;
        }

        if (_playerDefender != null)
        {
            // select random body part @ current attack height
            GetBodyPart(_attackHeight);

            // get player armor pieces
            _armor = _attacker.GetArmorLayers(_playerDefender, BodyPart);

            // get armor modifiers
            return _attacker.GetArmorMod(_playerDefender, DamageType, _armor, Weapon, _ignoreArmorMod);
        }

        // determine height quadrant
        _quadrant = GetQuadrant(_defender, _attacker, _attackHeight, _damageSource);

        // select random body part @ current attack height
        GetBodyPart(_quadrant);

        // Defensive check: GetBodyPart may have failed to populate _creaturePart when there's no body part table.
        if (_creaturePart == null)
        {
            _log.Error(
                "DamageEvent.GetArmorMod({Attacker} ({AttackerGuid}), {Defender} ({DefenderGuid})) - no creature body part available for wcid {DefenderWeenieClassId}; returning neutral armor mod.",
                _attacker?.Name,
                _attacker?.Guid,
                _defender?.Name,
                _defender?.Guid,
                _defender.WeenieClassId
            );

            // Mark as evaded (GetBodyPart already sets Evaded when appropriate), but ensure we return a safe neutral modifier.
            Evaded = true;
            return 1.0f;
        }

        _armor = _creaturePart.GetArmorLayers(_propertiesBodyPart.Key);

        // get target armor
        return _creaturePart.GetArmorMod(DamageType, _armor, _attacker, Weapon, _ignoreArmorMod);
    }

    private float GetResistanceMod()
    {
        if (_playerDefender != null)
        {
            return _playerDefender.GetResistanceMod(DamageType, _attacker, Weapon, _weaponResistanceMod);
        }

        var resistanceType = Creature.GetResistanceType(DamageType);

        return (float)
            Math.Max(0.0f, _defender.GetResistanceMod(resistanceType, _attacker, Weapon, _weaponResistanceMod));
    }

    /// <summary>
    /// SPEC BONUS: Physical Defense
    /// </summary>
    private static float GetSpecDefenseMod(Creature attacker, Player playerDefender)
    {
        if (
            playerDefender == null
            || playerDefender.GetCreatureSkill(Skill.PhysicalDefense).AdvancementClass
                != SkillAdvancementClass.Specialized
        )
        {
            return 1.0f;
        }

        // float, so the division below isn't integer division
        var playerDefenderPhysicalDefense = (float)
            LevelScaling.GetScaledPlayerDefenseSkill(
                playerDefender.GetModdedPhysicalDefSkill(),
                playerDefender,
                attacker
            );
        return DamageFormulas.GetSpecDefenseMod(playerDefenderPhysicalDefense);
    }

    /// <summary>
    /// RATING - Hardened Defense: Ramping Physical Damage Reduction.
    /// Up to +20% + 1% per rating (at max quest stamps).
    /// (JEWEL - Diamond)
    /// </summary>
    private static float GetRatingHardenedDefenseDamageResistanceBonus(Player playerDefender)
    {
        if (playerDefender == null)
        {
            return 0.0f;
        }

        var rating = playerDefender.GetEquippedAndActivatedItemRatingSum(PropertyInt.GearHardenedDefense);

        if (rating <= 0)
        {
            return 0.0f;
        }

        var rampPercentage =
            (float)playerDefender.QuestManager.GetCurrentSolves($"{playerDefender.Name},Hardened Defense") / 100;

        const float baseMod = 0.2f;
        const float bonusPerRating = 0.01f;

        return rampPercentage * (baseMod + bonusPerRating * rating);
    }

    private float GetDamageResistRatingMod()
    {
        _damageResistanceRatingBaseMod = _defender.GetDamageResistRatingMod(CombatType);

        if (IsCritical)
        {
            _criticalDamageResistanceRatingMod = Creature.GetNegativeRatingMod(_defender.GetCritDamageResistRating());
        }

        if (_pkBattle)
        {
            _pkDamageResistanceMod = Creature.GetNegativeRatingMod(_defender.GetPKDamageResistRating());
        }

        return DamageFormulas.CombineDamageResistRatings(
            _damageResistanceRatingBaseMod,
            IsCritical ? _criticalDamageResistanceRatingMod : null,
            _pkBattle ? _pkDamageResistanceMod : null
        );
    }

    private static Quadrant GetQuadrant(
        Creature defender,
        Creature attacker,
        AttackHeight attackHeight,
        WorldObject damageSource
    )
    {
        var quadrant = attackHeight.ToQuadrant();

        var wo = damageSource.CurrentLandblock != null ? damageSource : attacker;

        quadrant |= wo.GetRelativeDir(defender);

        return quadrant;
    }

    /// <summary>
    /// Returns a body part for a player defender
    /// </summary>
    private void GetBodyPart(AttackHeight attackHeight)
    {
        // select random body part @ current attack height
        BodyPart = BodyParts.GetBodyPart(attackHeight);
    }

    /// <summary>
    /// Returns a body part for a creature defender
    /// </summary>
    private void GetBodyPart(Quadrant quadrant)
    {
        // get cached body parts table
        var bodyParts = Creature.GetBodyParts(_defender.WeenieClassId);

        if (bodyParts == null)
        {
            _log.Debug(
                "DamageEvent.GetBodyPart({Defender} ({DefenderGuid}) ) - no body parts table for wcid {DefenderWeenieClassId}",
                _defender.Name,
                _defender.Guid,
                _defender.WeenieClassId
            );
            Evaded = true;
            return;
        }

        // rng roll for body part
        var bodyPart = bodyParts.RollBodyPart(quadrant);

        if (bodyPart == CombatBodyPart.Undefined)
        {
            _log.Debug(
                "DamageEvent.GetBodyPart({Defender} ({DefenderGuid}) ) - couldn't find body part for wcid {DefenderWeenieClassId}, Quadrant {BodyPartQuadrant}",
                _defender.Name,
                _defender.Guid,
                _defender.WeenieClassId,
                quadrant
            );
            Evaded = true;
            return;
        }

        _defender.Biota.PropertiesBodyPart.TryGetValue(bodyPart, out var value);
        _propertiesBodyPart = new KeyValuePair<CombatBodyPart, PropertiesBodyPart>(bodyPart, value);

        _creaturePart = new Creature_BodyPart(_defender, _propertiesBodyPart);
    }
}
