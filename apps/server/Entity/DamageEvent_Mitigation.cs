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
    private float GetCombatAbilityAegisDamageReduction(Player playerDefender)
    {
        if (playerDefender is not { AegisIsActive: true } || Evaded)
        {
            return 1.0f;
        }

        return 1.0f - Player.AegisDamageReduction;
    }

    /// <summary>
    /// COMBAT ABILITY - Phalanx: Damage taken from full hits reduced by 30%. Glancing blows are unaffected.
    /// </summary>
    private float GetCombatAbilityPhalanxDamageReduction(Player playerDefender)
    {
        if (playerDefender is null || Evaded || PartialEvasion != PartialEvasion.None)
        {
            return 1.0f;
        }

        return playerDefender.GetPhalanxFullHitDamageMod();
    }

    private float GetMitigation(Creature attacker, Creature defender)
    {
        if (attacker is null || defender is null)
        {
            return 1.0f;
        }

        var playerAttacker = attacker as Player;
        var playerDefender = defender as Player;

        _ignoreArmorMod = GetIgnoreArmorMod(attacker, defender);
        _ignoreArmorMod -= GetSpearSpecIgnoreArmorBonus(attacker);

        _armorMod = GetArmorMod(attacker, defender);

        _weaponResistanceMod = WorldObject.GetWeaponResistanceModifier(
            Weapon,
            attacker,
            _attackSkill,
            DamageType,
            defender
        );

        _resistanceMod = GetResistanceMod(defender, playerDefender);

        // Piercing resistance penetration (Black Garnet / Precision Strikes) is applied as a damage
        // multiplier via _ratingPierceResistanceBonus in GetRatingPierceResistanceBonus(); it must
        // not also be folded into the target's resistance here.

        _damageResistanceRatingMod = GetDamageResistRatingMod(defender, _pkBattle);
        _damageResistanceRatingMod *= 1.0f - GetRatingHardenedDefenseDamageResistanceBonus(playerDefender);

        _specDefenseMod = GetSpecDefenseMod(attacker, playerDefender);

        ShieldMod = _defender.GetShieldMod(attacker, DamageType, Weapon);

        _combatAbilityProvokeDamageReduction = GetCombatAbilityProvokeDamageReduction(playerDefender);
        _combatAbilityAegisDamageReduction = GetCombatAbilityAegisDamageReduction(playerDefender);
        _combatAbilityPhalanxDamageReduction = GetCombatAbilityPhalanxDamageReduction(playerDefender);

        _ratingSelfHarm = 1.0f + Jewel.GetJewelEffectMod(playerAttacker, PropertyInt.GearSelfHarm);
        _ratingRedFury = 1.0f + Jewel.GetJewelRedFury(playerAttacker);
        _ratingYellowFury = 1.0f + Jewel.GetJewelYellowFury(playerAttacker);
        _ratingDamageTypeWard = DamageType switch
        {
            var dt when (dt & DamageType.Physical) != 0 => 1.0f - Jewel.GetJewelEffectMod(playerDefender, PropertyInt.GearPhysicalWard),
            var dt when (dt & DamageType.Elemental) != 0 => 1.0f - Jewel.GetJewelEffectMod(playerDefender, PropertyInt.GearElementalWard),
            _ => 1.0f
        };

        _swarmedDamageReductionMod = GetSwarmedMod(playerDefender);

        _imbuedArmorPhysicalDamageMod = GetImbuedArmorPhysicalDamageMod(defender);
        _imbuedArmorCritDamageMod = GetImbuedArmorCritDamageMod(defender);

        return _armorMod
               * ShieldMod
               * _resistanceMod
               * _damageResistanceRatingMod
               * _evasionMod
               * _specDefenseMod
               * _combatAbilityProvokeDamageReduction
               * _combatAbilityAegisDamageReduction
               * _combatAbilityPhalanxDamageReduction
               * _ratingDamageTypeWard
               * _ratingSelfHarm
               * _ratingRedFury
               * _ratingYellowFury
               * _swarmedDamageReductionMod
               * _imbuedArmorPhysicalDamageMod
               * _imbuedArmorCritDamageMod;
    }

    private const float ImbuedArmorPhysicalDamageReductionPerPiece = 0.01f;
    private const float ImbuedArmorCritDamageReductionPerPiece = 0.01f;

    private static float GetImbuedArmorPhysicalDamageMod(Creature defender)
    {
        var count = defender.GetArmorDefenseImbues(ImbuedEffectType.ReducedPhysicalDamageTaken);
        if (count > 0)
        {
            return Math.Max(0.5f, 1.0f - count * ImbuedArmorPhysicalDamageReductionPerPiece);
        }
        return 1.0f;
    }

    private float GetImbuedArmorCritDamageMod(Creature defender)
    {
        if (!IsCritical)
        {
            return 1.0f;
        }
        var count = defender.GetArmorDefenseImbues(ImbuedEffectType.ReducedCriticalDamageTaken);
        if (count > 0)
        {
            return Math.Max(0.5f, 1.0f - count * ImbuedArmorCritDamageReductionPerPiece);
        }
        return 1.0f;
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
        var swarmedMod = 1.0f;

        if (playerDefender is null || playerDefender.GetEquippedMeleeWeapon() is null)
        {
            return swarmedMod;
        }

        var numNearbyEnemies = playerDefender.GetNearbyMonsters(3).Count;

        if (numNearbyEnemies <= 1)
        {
            return swarmedMod;
        }

        // start loop at 1 to only count mobs beyond the first
        for (var i = 1; i < numNearbyEnemies; i++)
        {
            swarmedMod *= 0.9f;
        }

        return swarmedMod;
    }

    private float GetIgnoreArmorMod(Creature attacker, Creature defender)
    {
        if (Weapon is null or { SpecialPropertiesRequireMana: true, ItemCurMana: 0 })
        {
            return 1.0f;
        }

        var playerAttacker = attacker as Player;

        var armorRendingMod = GetArmorRendingMod(defender, playerAttacker);
        var armorCleavingMod = attacker.GetArmorCleavingMod(Weapon);

        return armorCleavingMod - (1.0f - armorRendingMod);
    }

    private float GetArmorRendingMod(Creature defender, Player playerAttacker)
    {
        if (Weapon is null or { SpecialPropertiesRequireMana: true, ItemCurMana: 0 })
        {
            return 1.0f;
        }

        if (Weapon.HasImbuedEffect(ImbuedEffectType.ArmorRending))
        {
            return 1.0f - WorldObject.GetArmorRendingMod(_attackSkill, playerAttacker, defender);
        }

        return 1.0f;
    }

    /// <summary>
    /// SPEC BONUS - Martial Weapons (Spear): +10% armor penetration (additively)
    /// </summary>
    private static float GetSpearSpecIgnoreArmorBonus(Creature attacker)
    {
        var playerAttacker = attacker as Player;

        if (playerAttacker?.GetEquippedWeapon() == null)
        {
            return 0.0f;
        }

        return IsWeaponSkillSpecialized(playerAttacker, Skill.Spear, Skill.MartialWeapons) ? 0.1f : 0.0f;
    }

    private float GetArmorMod(Creature attacker, Creature defender)
    {
        var playerDefender = defender as Player;

        if (Weapon != null && Weapon.HasImbuedEffect(ImbuedEffectType.IgnoreAllArmor))
        {
            return 1.0f;
        }

        if (playerDefender != null)
        {
            // select random body part @ current attack height
            GetBodyPart(_attackHeight);

            // get player armor pieces
            _armor = attacker.GetArmorLayers(playerDefender, BodyPart);

            // get armor modifiers
            return attacker.GetArmorMod(playerDefender, DamageType, _armor, Weapon, _ignoreArmorMod);
        }

        // determine height quadrant
        _quadrant = GetQuadrant(defender, attacker, _attackHeight, _damageSource);

        // select random body part @ current attack height
        GetBodyPart(defender, _quadrant);

        // Defensive check: GetBodyPart may have failed to populate _creaturePart when there's no body part table.
        if (_creaturePart == null)
        {
            _log.Error(
                "DamageEvent.GetArmorMod({Attacker} ({AttackerGuid}), {Defender} ({DefenderGuid})) - no creature body part available for wcid {DefenderWeenieClassId}; returning neutral armor mod.",
                attacker?.Name,
                attacker?.Guid,
                defender?.Name,
                defender?.Guid,
                defender.WeenieClassId
            );

            // Mark as evaded (GetBodyPart already sets Evaded when appropriate), but ensure we return a safe neutral modifier.
            Evaded = true;
            return 1.0f;
        }

        _armor = _creaturePart.GetArmorLayers(_propertiesBodyPart.Key);

        // get target armor
        return _creaturePart.GetArmorMod(DamageType, _armor, attacker, Weapon, _ignoreArmorMod);
    }

    private float GetResistanceMod(Creature defender, Player playerDefender)
    {
        if (playerDefender != null)
        {
            return playerDefender.GetResistanceMod(DamageType, _attacker, Weapon, _weaponResistanceMod);
        }

        var resistanceType = Creature.GetResistanceType(DamageType);

        return (float)
            Math.Max(0.0f, defender.GetResistanceMod(resistanceType, _attacker, Weapon, _weaponResistanceMod));
    }

    /// <summary>
    /// SPEC BONUS: Physical Defense
    /// </summary>
    private static float GetSpecDefenseMod(Creature attacker, Player playerDefender)
    {
        if (
            playerDefender == null
            || playerDefender.GetCreatureSkill(Skill.PhysicalDefense).AdvancementClass != SkillAdvancementClass.Specialized
        )
        {
            return 1.0f;
        }

        // float, so the division below isn't integer division
        var playerDefenderPhysicalDefense = (float)LevelScaling.GetScaledPlayerDefenseSkill(
            playerDefender.GetModdedPhysicalDefSkill(),
            playerDefender,
            attacker
        );
        var bonusAmount = Math.Min(playerDefenderPhysicalDefense, 500) / 50;

        return 0.9f - bonusAmount * 0.01f;
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

        var rampPercentage = (float)playerDefender.QuestManager.GetCurrentSolves($"{playerDefender.Name},Hardened Defense") / 100;

        const float baseMod = 0.2f;
        const float bonusPerRating = 0.01f;

        return rampPercentage * (baseMod + bonusPerRating * rating);
    }

    private float GetDamageResistRatingMod(Creature defender, bool pkBattle)
    {
        _damageResistanceRatingBaseMod = defender.GetDamageResistRatingMod(CombatType);

        var damageResistRatingMod = _damageResistanceRatingBaseMod;

        if (IsCritical)
        {
            _criticalDamageResistanceRatingMod = Creature.GetNegativeRatingMod(defender.GetCritDamageResistRating());
            damageResistRatingMod = Creature.AdditiveCombine(damageResistRatingMod, _criticalDamageResistanceRatingMod);
        }

        if (pkBattle)
        {
            _pkDamageResistanceMod = Creature.GetNegativeRatingMod(defender.GetPKDamageResistRating());
            damageResistRatingMod = Creature.AdditiveCombine(damageResistRatingMod, _pkDamageResistanceMod);
        }

        return damageResistRatingMod;
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
    private void GetBodyPart(Creature defender, Quadrant quadrant)
    {
        // get cached body parts table
        var bodyParts = Creature.GetBodyParts(defender.WeenieClassId);

        if (bodyParts == null)
        {
            _log.Debug(
                "DamageEvent.GetBodyPart({Defender} ({DefenderGuid}) ) - no body parts table for wcid {DefenderWeenieClassId}",
                defender.Name,
                defender.Guid,
                defender.WeenieClassId
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
                defender.Name,
                defender.Guid,
                defender.WeenieClassId,
                quadrant
            );
            Evaded = true;
            return;
        }

        defender.Biota.PropertiesBodyPart.TryGetValue(bodyPart, out var value);
        _propertiesBodyPart = new KeyValuePair<CombatBodyPart, PropertiesBodyPart>(bodyPart, value);

        _creaturePart = new Creature_BodyPart(defender, _propertiesBodyPart);
    }
}
