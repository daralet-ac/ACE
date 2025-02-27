using System;
using System.Collections.Generic;
using ACE.Common;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Entity;
using ACE.Server.Factories.Tables;
using ACE.Server.Network.GameMessages.Messages;
using DamageType = ACE.Entity.Enum.DamageType;

namespace ACE.Server.WorldObjects;

partial class Jewel
{
    // Caster and Physical Overlapping Bonuses
    public static void HandlePlayerAttackerBonuses(Player playerAttacker, Creature defender, float damage, DamageType damageType)
    {
        CheckForRatingLifeOnHit(playerAttacker, defender, damage);
        CheckForRatingManaOnHit(playerAttacker, defender, damage);
        CheckForRatingSelfHarm(playerAttacker, damage);
        CheckForRatingElementalHotspot(playerAttacker, defender, damageType);
        CheckForRatingMagicFindStamps(playerAttacker, defender, damage);
        CheckForRatingProsperityFindStamps(playerAttacker, defender, damage);
    }

    public static void HandlePlayerDefenderBonuses(Player playerDefender, Creature attacker, float damage)
    {
        CheckForRatingHealthToStamina(playerDefender, attacker, damage);
        CheckForRatingHealthToMana(playerDefender, attacker, damage);
    }

    public static void HandleMeleeAttackerRampingQuestStamps(Player playerAttacker, Creature defender, DamageType damageType)
    {
        var scaledStamps = GetBaseMeleeScaledStamps(playerAttacker, defender);

        AddRatingQuestStamps(playerAttacker, defender, PropertyInt.GearStamReduction, "StamReduction", scaledStamps, true);
        AddRatingQuestStamps(playerAttacker, defender, PropertyInt.GearFamiliarity, "Familiarity", scaledStamps);

        switch (damageType)
        {
            case DamageType.Bludgeon:
                AddRatingQuestStamps(playerAttacker, defender, PropertyInt.GearBludgeon, "Bludgeon", scaledStamps);
                break;
            case DamageType.Pierce:
                AddRatingQuestStamps(playerAttacker, defender, PropertyInt.GearPierce, "Pierce", scaledStamps);
                break;
        }
    }

    public static void HandleMeleeDefenderRampingQuestStamps(Player playerDefender)
    {
        AddRatingQuestStamps(playerDefender, null, PropertyInt.GearHardenedDefense, "Hardened Defense", 10);
        AddRatingQuestStamps(playerDefender, null, PropertyInt.GearBravado, "Bravado", 10);
    }

    public static void HandleCasterAttackerRampingQuestStamps(Player sourcePlayer, Creature targetCreature, Spell spell, ProjectileSpellType projectileSpellTyper)
    {
        var scaledStamps = GetBaseCasterScaledStamps(sourcePlayer, spell.Level, projectileSpellTyper);

        AddRatingQuestStamps(sourcePlayer, targetCreature, PropertyInt.GearFamiliarity, "Familiarity", scaledStamps);
        AddRatingQuestStamps(sourcePlayer, targetCreature, PropertyInt.GearWardPen, "WardPen", scaledStamps);
        AddRatingQuestStamps(sourcePlayer, targetCreature, PropertyInt.GearElementalist, "Elementalist", scaledStamps, true);

        switch (spell.DamageType)
        {
            case DamageType.Bludgeon:
                AddRatingQuestStamps(sourcePlayer, targetCreature, PropertyInt.GearBludgeon, "Bludgeon", scaledStamps, true);
                break;
            case DamageType.Pierce:
                AddRatingQuestStamps(sourcePlayer, targetCreature, PropertyInt.GearPierce, "Pierce", scaledStamps, true);
                break;
        }
    }

    public static void HandleCasterDefenderRampingQuestStamps(Player targetPlayer, Creature sourceCreature)
    {
        AddRatingQuestStamps(targetPlayer, sourceCreature, PropertyInt.GearNullification, "Nullification", 10, true);
    }

    public static float HandleElementalBonuses(Player playerAttacker, DamageType damageType)
    {
        var jewelElemental = 1.0f;

        if (playerAttacker == null)
        {
            return jewelElemental;
        }

        jewelElemental = CheckForRatingAcidDamageBonus(playerAttacker, damageType, jewelElemental);
        jewelElemental = CheckForRatingFireDamageBonus(playerAttacker, damageType, jewelElemental);
        jewelElemental = CheckForRatingColdDamageBonus(playerAttacker, damageType, jewelElemental);
        jewelElemental = CheckForRatingLightningDamageBonus(playerAttacker, damageType, jewelElemental);

        return jewelElemental;
    }

    /// <summary>
    /// RATING - Lightning Increased lightning damage by 10% + 0.5% per rating.
    /// (JEWEL - Jet)
    /// </summary>
    private static float CheckForRatingLightningDamageBonus(Player playerAttacker, DamageType damageType, float jewelElemental)
    {
        var equippedRating = playerAttacker.GetEquippedAndActivatedItemRatingSum(PropertyInt.GearLightning);

        if (equippedRating <= 0 || damageType != DamageType.Electric)
        {
            return jewelElemental;
        }

        const float baseRating = 0.1f;
        const float bonusPerRating = 0.005f;

        jewelElemental += baseRating + equippedRating * bonusPerRating;

        return jewelElemental;
    }

    /// <summary>
    /// RATING - Cold: Increased cold damage by 10% + 0.5% per rating.
    /// (JEWEL - Aquamarine)
    /// </summary>
    private static float CheckForRatingColdDamageBonus(Player playerAttacker, DamageType damageType, float jewelElemental)
    {
        var equippedRating = playerAttacker.GetEquippedAndActivatedItemRatingSum(PropertyInt.GearFrost);

        if (equippedRating <= 0 || damageType != DamageType.Cold)
        {
            return jewelElemental;
        }

        const float baseRating = 0.1f;
        const float bonusPerRating = 0.005f;

        jewelElemental += baseRating + equippedRating * bonusPerRating;

        return jewelElemental;
    }

    /// <summary>
    /// RATING - Fire: Increased fire damage by 10% + 0.5% per rating.
    /// (JEWEL - Red Garnet)
    /// </summary>
    private static float CheckForRatingFireDamageBonus(Player playerAttacker, DamageType damageType, float jewelElemental)
    {
        var equippedRating = playerAttacker.GetEquippedAndActivatedItemRatingSum(PropertyInt.GearFire);

        if (equippedRating <= 0 || damageType != DamageType.Fire)
        {
            return jewelElemental;
        }

        const float baseRating = 0.1f;
        const float bonusPerRating = 0.005f;

        jewelElemental += baseRating + equippedRating * bonusPerRating;

        return jewelElemental;
    }

    /// <summary>
    /// RATING - Acid: Increased acid damage by 10% + 0.5% per rating.
    /// (JEWEL - Emerald)
    /// </summary>
    private static float CheckForRatingAcidDamageBonus(Player playerAttacker, DamageType damageType, float jewelElemental)
    {
        var equippedRating = playerAttacker.GetEquippedAndActivatedItemRatingSum(PropertyInt.GearAcid);

        if (equippedRating <= 0 || damageType != DamageType.Acid)
        {
            return jewelElemental;
        }

        const float baseRating = 0.1f;
        const float bonusPerRating = 0.005f;

        jewelElemental += baseRating + equippedRating * bonusPerRating;

        return jewelElemental;
    }

    private static int GetBaseMeleeScaledStamps(Player playerAttacker, Creature defender)
    {
        const int baseStamps = 10;

        var powerBarScalar = playerAttacker.GetPowerAccuracyBar() * 2;

        var equippedWeapon = playerAttacker.GetEquippedMeleeWeapon();
        var weaponAnimationLength = WeaponAnimationLength.GetWeaponAnimLength(equippedWeapon);
        var weaponTime = equippedWeapon.WeaponTime ?? 100;
        var attacksPerSecondScalar = 1 / (weaponAnimationLength / (1.0f + (1 - (weaponTime / 100.0))));

        return Convert.ToInt32(baseStamps * powerBarScalar * attacksPerSecondScalar);
    }

    private static int GetBaseCasterScaledStamps(Player sourcePlayer, uint spellLevel, ProjectileSpellType projectileSpellType)
    {
        const int baseStamps = 10;

        var castAnimationLengthScalar = WeaponAnimationLength.GetSpellCastAnimationLength(projectileSpellType, spellLevel);

        var spellTypeScalar = projectileSpellType switch
        {
            ProjectileSpellType.Blast or ProjectileSpellType.Volley => 0.33f,
            ProjectileSpellType.Ring or ProjectileSpellType.Wall => 0.25f,
            _ => 1.0f
        };

        return Convert.ToInt32(baseStamps * castAnimationLengthScalar * spellTypeScalar);
    }

    private static void AddRatingQuestStamps(Player sourcePlayer, Creature targetCreature, PropertyInt propertyInt, string questString, int amount, bool questManagerOfPlayer = false)
    {
        if (sourcePlayer.GetEquippedAndActivatedItemRatingSum(propertyInt) <= 0)
        {
            return;
        }

        var questTarget = questManagerOfPlayer ? sourcePlayer : targetCreature;

        if (questTarget.QuestManager.HasQuest($"{sourcePlayer.Name},{questString}"))
        {
            if (questTarget.QuestManager.GetCurrentSolves($"{sourcePlayer.Name},{questString}") < 100)
            {
                questTarget.QuestManager.Increment($"{sourcePlayer.Name},{questString}", amount);
            }
        }
        else
        {
            questTarget.QuestManager.Stamp($"{sourcePlayer.Name},{questString}");
            questTarget.QuestManager.Increment($"{sourcePlayer.Name},{questString}", amount);
        }
    }

    /// <summary>
    /// RATING - Pyreal Find: Adds quest stamps for prosperity find.
    /// (JEWEL - Green Jade)
    /// </summary>
    private static void CheckForRatingProsperityFindStamps(Player playerAttacker, Creature defender, float damage)
    {
        if (playerAttacker.GetEquippedAndActivatedItemRatingSum(PropertyInt.GearPyrealFind) <= 0)
        {
            return;
        }

        var pyrealFind = playerAttacker.GetEquippedAndActivatedItemRatingSum(PropertyInt.GearPyrealFind);

        defender.QuestManager.Stamp($"{playerAttacker.Name}/Prosperity/{pyrealFind}/{damage}");
    }

    /// <summary>
    /// RATING - Magic Find: Adds quest stamps for magic find.
    /// (JEWEL - Sapphire)
    /// </summary>
    private static void CheckForRatingMagicFindStamps(Player playerAttacker, Creature defender, float damage)
    {
        if (playerAttacker.GetEquippedAndActivatedItemRatingSum(PropertyInt.GearMagicFind) <= 0)
        {
            return;
        }

        var magicFind = playerAttacker.GetEquippedAndActivatedItemRatingSum(PropertyInt.GearMagicFind);

        defender.QuestManager.Stamp($"{playerAttacker.Name}/MagicFind/{magicFind}/{damage}");
    }

    /// <summary>
    /// RATING - Fire/Frost/Acid/Lightning: Chance to generate elemental hotspot on hit.
    /// (JEWEL - Aquamarine, Emerald, Jet, Red Garnet)
    /// </summary>
    private static void CheckForRatingElementalHotspot(Player playerAttacker, Creature defender, DamageType damageType)
    {
        if (playerAttacker.GetEquippedAndActivatedItemRatingSum(PropertyInt.GearFire) <= 0
            && playerAttacker.GetEquippedAndActivatedItemRatingSum(PropertyInt.GearFrost) <= 0
            && playerAttacker.GetEquippedAndActivatedItemRatingSum(PropertyInt.GearAcid) <= 0
            && playerAttacker.GetEquippedAndActivatedItemRatingSum(PropertyInt.GearLightning) <= 0)
        {
            return;
        }

        if (playerAttacker.Level == null)
        {
            return;
        }

        var tier = Math.Round((float)playerAttacker.Level / 10);
        Hotspot.TryGenHotspot(playerAttacker, defender, (int)tier, damageType);
    }

    /// <summary>
    /// RATING - Self Harm: Damage self when dealing damage. (Also grants bonus damage from a different function)
    /// (JEWEL - Hematite)
    /// </summary>
    private static void CheckForRatingSelfHarm(Player playerAttacker, float damage)
    {
        // JEWEL - Hematite: Self-harm damage
        if (playerAttacker.GetEquippedAndActivatedItemRatingSum(PropertyInt.GearSelfHarm) <= 0)
        {
            return;
        }

        var jewelSelfHarm = (playerAttacker.GetEquippedAndActivatedItemRatingSum(PropertyInt.GearSelfHarm) * 0.01f);
        var selfHarm = (int)(jewelSelfHarm * damage);

        playerAttacker.UpdateVitalDelta(playerAttacker.Health, -selfHarm);
        playerAttacker.DamageHistory.Add(playerAttacker, DamageType.Health, (uint)selfHarm);

        if (!playerAttacker.IsDead)
        {
            return;
        }

        var lastDamager = playerAttacker.DamageHistory.LastDamager;
        playerAttacker.OnDeath(lastDamager, DamageType.Health);
        playerAttacker.Die();
    }

    /// <summary>
    /// RATING - Manasteal: Gain mana on hit
    /// (JEWEL - Opal)
    /// </summary>
    private static void CheckForRatingManaOnHit(Player playerAttacker, Creature defender, float damage)
    {
        if (playerAttacker.GetEquippedAndActivatedItemRatingSum(PropertyInt.GearManasteal) <= 0)
        {
            return;
        }

        var chance = (float)playerAttacker.GetEquippedAndActivatedItemRatingSum(PropertyInt.GearManasteal) / 100;

        if (playerAttacker == defender || !(chance >= ThreadSafeRandom.Next(0.0f, 1.0f)))
        {
            return;
        }

        var restoreAmount = (uint)Math.Round(damage);

        playerAttacker.UpdateVitalDelta(playerAttacker.Mana, restoreAmount);
        playerAttacker.DamageHistory.OnHeal(restoreAmount);
        playerAttacker.Session.Network.EnqueueSend(
            new GameMessageSystemChat(
                $"Mana Leech! Your attack restores {restoreAmount} mana.",
                ChatMessageType.Magic
            )
        );
    }

    /// <summary>
    /// RATING - Lifesteal: Gain life on hit
    /// (JEWEL - Bloodstone)
    /// </summary>
    private static void CheckForRatingLifeOnHit(Player playerAttacker, Creature defender, float damage)
    {
        if (playerAttacker.GetEquippedAndActivatedItemRatingSum(PropertyInt.GearLifesteal) <= 0)
        {
            return;
        }

        var chance = playerAttacker.GetEquippedAndActivatedItemRatingSum(PropertyInt.GearLifesteal) * 0.01f;

        if (playerAttacker == defender || !(chance >= ThreadSafeRandom.Next(0.0f, 1.0f)))
        {
            return;
        }

        var healAmount = (uint)Math.Round(damage);

        playerAttacker.UpdateVitalDelta(playerAttacker.Health, healAmount);
        playerAttacker.DamageHistory.OnHeal(healAmount);
        playerAttacker.Session.Network.EnqueueSend(
            new GameMessageSystemChat(
                $"Life Steal! Your attack restores {healAmount} health.",
                ChatMessageType.CombatSelf
            )
        );
    }

    /// <summary>
    /// RATING - Health to Mana: Chance to gain mana on taking damage.
    /// (JEWEL - Lapis Lazuli)
    /// </summary>
    public static void CheckForRatingHealthToMana(Player playerDefender, Creature attacker, float damage)
    {
        if (playerDefender.GetEquippedAndActivatedItemRatingSum(PropertyInt.GearHealthToMana) <= 0)
        {
            return;
        }

        var chance = playerDefender.GetEquippedAndActivatedItemRatingSum(PropertyInt.GearHealthToMana) * 0.01f;

        if (attacker == playerDefender || !(chance >= ThreadSafeRandom.Next(0.0f, 1.0f)))
        {
            return;
        }

        var manaAmount = (uint)Math.Round(damage / 4);
        playerDefender.UpdateVitalDelta(playerDefender.Mana, manaAmount);
        playerDefender.DamageHistory.OnHeal(manaAmount);
        playerDefender.Session.Network.EnqueueSend(
            new GameMessageSystemChat(
                $"Your Lapis of the Anchorite restores {manaAmount} points of Mana!",
                ChatMessageType.Broadcast
            )
        );
    }

    /// <summary>
    /// RATING - Health to Stamina: Chance to gain stamina on taking damage.
    /// (JEWEL - Amber)
    /// </summary>
    public static void CheckForRatingHealthToStamina(Player playerDefender, Creature attacker, float damage)
    {
        if (playerDefender.GetEquippedAndActivatedItemRatingSum(PropertyInt.GearHealthToStamina) <= 0)
        {
            return;
        }

        var chance = playerDefender.GetEquippedAndActivatedItemRatingSum(PropertyInt.GearHealthToStamina) * 0.01f;

        if (attacker == playerDefender || !(chance >= ThreadSafeRandom.Next(0.0f, 1.0f)))
        {
            return;
        }

        var staminaAmount = (uint)Math.Round(damage);

        playerDefender.UpdateVitalDelta(playerDefender.Stamina, staminaAmount);
        playerDefender.DamageHistory.OnHeal(staminaAmount);
        playerDefender.Session.Network.EnqueueSend(
            new GameMessageSystemChat(
                $"Your Amber of the Masochist restores {staminaAmount} points of Stamina!",
                ChatMessageType.Broadcast
            )
        );
    }

    /// <summary>
    /// RATING - Red Fury: .
    /// (JEWEL - Ruby)
    /// </summary>
    public static float GetJewelRedFury(Player playerAttacker)
    {
        var percentHealthRemaining = (float)playerAttacker.Health.Current / playerAttacker.Health.MaxValue;
        var mod = Math.Min(((1.0f - percentHealthRemaining) / 0.75f), 1.0f);

        return mod * ((float)playerAttacker.GetEquippedAndActivatedItemRatingSum(PropertyInt.GearRedFury) / 100);
    }

    /// <summary>
    /// RATING - Yellow Fury: .
    /// (JEWEL - ??)
    /// </summary>
    public static float GetJewelYellowFury(Player playerAttacker)
    {
        var percentStaminaRemaining = (float)playerAttacker.Stamina.Current / playerAttacker.Stamina.MaxValue;
        var mod = Math.Min(((1.0f - percentStaminaRemaining) / 0.75f), 1.0f);

        return mod * ((float)playerAttacker.GetEquippedAndActivatedItemRatingSum(PropertyInt.GearYellowFury) / 100);
    }

    /// <summary>
    /// RATING - Blue Fury: .
    /// (JEWEL - ??)
    /// </summary>
    public static float GetJewelBlueFury(Player playerAttacker)
    {
        var percentManaRemaining = (float)playerAttacker.Mana.Current / playerAttacker.Mana.MaxValue;
        var mod = Math.Min(((1.0f - percentManaRemaining) / 0.75f), 1.0f);

        return mod * ((float)playerAttacker.GetEquippedAndActivatedItemRatingSum(PropertyInt.GearBlueFury) / 100);
    }

    // 0 - prepended quality, 1 - gemstone type, 2 - appended name, 3 - property type, 4 - amount of property, 5 - original gem workmanship
    public static string GetJewelDescription(string jewelSocket1)
    {
        var parts = jewelSocket1.Split('/');

        var description = "";
        if (!MaterialTypetoString.TryGetValue(parts[1], out var convertedMaterialType))
        {
            return description;
        }

        if (!int.TryParse(parts[4], out var amount))
        {
            return description;
        }

        if (!int.TryParse(parts[5], out var originalWorkmanship))
        {
            return description;
        }

        var workmanship = originalWorkmanship - 1 < 1 ? 1 : originalWorkmanship - 1;

        switch (convertedMaterialType)
        {
            //necklace
            case ACE.Entity.Enum.MaterialType.Sunstone:
                var material = ACE.Entity.Enum.MaterialType.Sunstone;
                var name = JewelEffectInfo[material].Item1;
                var equipmentType = JewelEffectInfo[material].Item2;
                var baseRating = JewelEffectInfo[material].Item3;
                var bonusPerQuality = JewelEffectInfo[material].Item4;
                var baseRatingSecondary = JewelEffectInfo[material].Item5;
                var bonusPerQualitySecondary = JewelEffectInfo[material].Item6;
                description =
                    $"Socket this jewel in a {equipmentType} of workmanship {workmanship} or greater to grant a {baseRating}% bonus to experience for monster kills, plus an additional {bonusPerQuality}% per equipped {name} rating.\n\n" +
                    $"{JewelStatsDescription(baseRating, amount, bonusPerQuality, name)}\n\n";
                break;
            case ACE.Entity.Enum.MaterialType.Sapphire:
                material = ACE.Entity.Enum.MaterialType.Sapphire;
                name = JewelEffectInfo[material].Item1;
                equipmentType = JewelEffectInfo[material].Item2;
                baseRating = JewelEffectInfo[material].Item3;
                bonusPerQuality = JewelEffectInfo[material].Item4;
                description =
                    $"Socket this jewel in a {equipmentType} of workmanship {workmanship} or greater to grant a {baseRating}% bonus to loot quality for monster kills, plus an additional {bonusPerQuality}% per equipped {name} rating.\n\n" +
                    $"{JewelStatsDescription(baseRating, amount, bonusPerQuality, name)}\n\n";
                break;
            case ACE.Entity.Enum.MaterialType.GreenJade:
                material = ACE.Entity.Enum.MaterialType.GreenJade;
                name = JewelEffectInfo[material].Item1;
                equipmentType = JewelEffectInfo[material].Item2;
                baseRating = JewelEffectInfo[material].Item3;
                bonusPerQuality = JewelEffectInfo[material].Item4;
                description =
                    $"Socket this jewel in a {equipmentType} of workmanship {workmanship} or greater to grant a {baseRating}% chance for a killed monster to drop an additional item, plus an additional {bonusPerQuality}% per equipped {name} rating.\n\n" +
                    $"{JewelStatsDescription(baseRating, amount, bonusPerQuality, name)}\n\n";
                break;

            // ring right
            case ACE.Entity.Enum.MaterialType.Carnelian:
                material = ACE.Entity.Enum.MaterialType.Carnelian;
                name = JewelEffectInfo[material].Item1;
                equipmentType = JewelEffectInfo[material].Item2;
                baseRating = JewelEffectInfo[material].Item3;
                bonusPerQuality = JewelEffectInfo[material].Item4;
                description =
                    $"Socket this jewel in a {equipmentType} of workmanship {workmanship} or greater to grant {baseRating} bonus to current Strength, plus an additional {bonusPerQuality}% per equipped {name} rating. " +
                    $"Once socketed, the {equipmentType} can only be worn on the right finger.\n\n" +
                    $"{JewelStatsDescription(baseRating, amount, bonusPerQuality, name)}\n\n";
                break;
            case ACE.Entity.Enum.MaterialType.Azurite:
                material = ACE.Entity.Enum.MaterialType.Azurite;
                name = JewelEffectInfo[material].Item1;
                equipmentType = JewelEffectInfo[material].Item2;
                baseRating = JewelEffectInfo[material].Item3;
                bonusPerQuality = JewelEffectInfo[material].Item4;
                description =
                    $"Socket this jewel in a {equipmentType} of workmanship {workmanship} or greater to grant {baseRating} bonus to current Self, plus an additional {bonusPerQuality}% per equipped {name} rating. " +
                    $"Once socketed, the {equipmentType} can only be worn on the right finger.\n\n" +
                    $"{JewelStatsDescription(baseRating, amount, bonusPerQuality, name)}\n\n";
                break;
            case ACE.Entity.Enum.MaterialType.TigerEye:
                material = ACE.Entity.Enum.MaterialType.TigerEye;
                name = JewelEffectInfo[material].Item1;
                equipmentType = JewelEffectInfo[material].Item2;
                baseRating = JewelEffectInfo[material].Item3;
                bonusPerQuality = JewelEffectInfo[material].Item4;
                description =
                    $"Socket this jewel in a {equipmentType} of workmanship {workmanship} or greater to grant {baseRating} bonus to current Coordination, plus an additional {bonusPerQuality}% per equipped {name} rating. " +
                    $"Once socketed, the {equipmentType} can only be worn on the right finger.\n\n" +
                    $"{JewelStatsDescription(baseRating, amount, bonusPerQuality, name)}\n\n";
                break;

            // ring left
            case ACE.Entity.Enum.MaterialType.RedJade:
                material = ACE.Entity.Enum.MaterialType.RedJade;
                name = JewelEffectInfo[material].Item1;
                equipmentType = JewelEffectInfo[material].Item2;
                baseRating = JewelEffectInfo[material].Item3;
                bonusPerQuality = JewelEffectInfo[material].Item4;
                description =
                    $"Socket this jewel in a {equipmentType} of workmanship {workmanship} or greater to grant {baseRating} bonus to current Focus, plus an additional {bonusPerQuality}% per equipped {name} rating. " +
                    $"Once socketed, the {equipmentType} can only be worn on the left finger.\n\n" +
                    $"{JewelStatsDescription(baseRating, amount, bonusPerQuality, name)}\n\n";
                break;
            case ACE.Entity.Enum.MaterialType.YellowTopaz:
                material = ACE.Entity.Enum.MaterialType.YellowTopaz;
                name = JewelEffectInfo[material].Item1;
                equipmentType = JewelEffectInfo[material].Item2;
                baseRating = JewelEffectInfo[material].Item3;
                bonusPerQuality = JewelEffectInfo[material].Item4;
                description =
                    $"Socket this jewel in a {equipmentType} of workmanship {workmanship} or greater to grant {baseRating} bonus to current Endurance, plus an additional {bonusPerQuality}% per equipped {name} rating. " +
                    $"Once socketed, the {equipmentType} can only be worn on the left finger.\n\n" +
                    $"{JewelStatsDescription(baseRating, amount, bonusPerQuality, name)}\n\n";
                break;
            case ACE.Entity.Enum.MaterialType.Peridot:
                material = ACE.Entity.Enum.MaterialType.Peridot;
                name = JewelEffectInfo[material].Item1;
                equipmentType = JewelEffectInfo[material].Item2;
                baseRating = JewelEffectInfo[material].Item3;
                bonusPerQuality = JewelEffectInfo[material].Item4;
                description =
                    $"Socket this jewel in a {equipmentType} of workmanship {workmanship} or greater to grant {baseRating} bonus to current Quickness, plus an additional {bonusPerQuality}% per equipped {name} rating. " +
                    $"Once socketed, the {equipmentType} can only be worn on the left finger.\n\n" +
                    $"{JewelStatsDescription(baseRating, amount, bonusPerQuality, name)}\n\n";
                break;

            // bracelet left
            case ACE.Entity.Enum.MaterialType.Agate:
                material = ACE.Entity.Enum.MaterialType.Agate;
                name = JewelEffectInfo[material].Item1;
                equipmentType = JewelEffectInfo[material].Item2;
                baseRating = JewelEffectInfo[material].Item3;
                bonusPerQuality = JewelEffectInfo[material].Item4;
                description =
                    $"Socket this jewel in a {equipmentType} of workmanship {workmanship} or greater to grant {baseRating} addtional threat generated from your actions, plus an additional {bonusPerQuality}% per equipped {name} rating. " +
                    $"Once socketed, the bracelet can only be worn on the left wrist.\n\n" +
                    $"{JewelStatsDescription(baseRating, amount, bonusPerQuality, name)}\n\n";
                break;
            case ACE.Entity.Enum.MaterialType.SmokeyQuartz:
                material = ACE.Entity.Enum.MaterialType.SmokeyQuartz;
                name = JewelEffectInfo[material].Item1;
                equipmentType = JewelEffectInfo[material].Item2;
                baseRating = JewelEffectInfo[material].Item3;
                bonusPerQuality = JewelEffectInfo[material].Item4;
                description =
                    $"Socket this jewel in a {equipmentType} of workmanship {workmanship} or greater to grant {baseRating} reduced threat generated from your actions, plus an additional {bonusPerQuality}% per equipped {name} rating. " +
                    $"Once socketed, the bracelet can only be worn on the left wrist.\n\n" +
                    $"{JewelStatsDescription(baseRating, amount, bonusPerQuality, name)}\n\n";
                break;
            case ACE.Entity.Enum.MaterialType.Amber:
                material = ACE.Entity.Enum.MaterialType.Amber;
                name = JewelEffectInfo[material].Item1;
                equipmentType = JewelEffectInfo[material].Item2;
                baseRating = JewelEffectInfo[material].Item3;
                bonusPerQuality = JewelEffectInfo[material].Item4;
                description =
                    $"Socket this jewel in a {equipmentType} of workmanship {workmanship} or greater to grant a {baseRating}% chance to gain hit damage taken as stamina, plus an additional {bonusPerQuality}% per equipped {name} rating. " +
                    $"Once socketed, the bracelet can only be worn on the left wrist.\n\n" +
                    $"{JewelStatsDescription(baseRating, amount, bonusPerQuality, name)}\n\n";
                break;
            case ACE.Entity.Enum.MaterialType.LapisLazuli:
                material = ACE.Entity.Enum.MaterialType.LapisLazuli;
                name = JewelEffectInfo[material].Item1;
                equipmentType = JewelEffectInfo[material].Item2;
                baseRating = JewelEffectInfo[material].Item3;
                bonusPerQuality = JewelEffectInfo[material].Item4;
                description =
                    $"Socket this jewel in a {equipmentType} of workmanship {workmanship} or greater to grant a {baseRating}% chance to gain hit damage taken as mana, plus an additional {bonusPerQuality}% per equipped {name} rating. " +
                    $"Once socketed, the bracelet can only be worn on the left wrist.\n\n" +
                    $"{JewelStatsDescription(baseRating, amount, bonusPerQuality, name)}\n\n";
                break;
            case ACE.Entity.Enum.MaterialType.Moonstone:
                material = ACE.Entity.Enum.MaterialType.Moonstone;
                name = JewelEffectInfo[material].Item1;
                equipmentType = JewelEffectInfo[material].Item2;
                baseRating = JewelEffectInfo[material].Item3;
                bonusPerQuality = JewelEffectInfo[material].Item4;
                description =
                    $"Socket this jewel in a {equipmentType} of workmanship {workmanship} or greater to grant a {baseRating}% reduction to mana consumed by equipped items, plus an additional {bonusPerQuality}% per equipped {name} rating. " +
                    $"Once socketed, the bracelet can only be worn on the left wrist.\n\n" +
                    $"{JewelStatsDescription(baseRating, amount, bonusPerQuality, name)}\n\n";
                break;
            case ACE.Entity.Enum.MaterialType.Malachite:
                material = ACE.Entity.Enum.MaterialType.Malachite;
                name = JewelEffectInfo[material].Item1;
                equipmentType = JewelEffectInfo[material].Item2;
                baseRating = JewelEffectInfo[material].Item3;
                bonusPerQuality = JewelEffectInfo[material].Item4;
                description =
                    $"Socket this jewel in a {equipmentType} of workmanship {workmanship} or greater to grant a {baseRating}% reduction to your chance to burn spell components, plus an additional {bonusPerQuality}% per equipped {name} rating. " +
                    $"Once socketed, the bracelet can only be worn on the left wrist.\n\n" +
                    $"{JewelStatsDescription(baseRating, amount, bonusPerQuality, name)}\n\n";
                break;
            case ACE.Entity.Enum.MaterialType.Citrine:
                material = ACE.Entity.Enum.MaterialType.Citrine;
                name = JewelEffectInfo[material].Item1;
                equipmentType = JewelEffectInfo[material].Item2;
                baseRating = JewelEffectInfo[material].Item3;
                bonusPerQuality = JewelEffectInfo[material].Item4;
                description =
                    $"Socket this jewel in a {equipmentType} of workmanship {workmanship} or greater to grant {baseRating}% stamina cost reduction, plus an additional {bonusPerQuality}% per equipped {name} rating. " +
                    $"Once socketed, the bracelet can only be worn on the left wrist.\n\n" +
                    $"{JewelStatsDescription(baseRating, amount, bonusPerQuality, name)}\n\n";
                break;

            // bracelet right
            case ACE.Entity.Enum.MaterialType.Amethyst:
                material = ACE.Entity.Enum.MaterialType.Amethyst;
                name = JewelEffectInfo[material].Item1;
                equipmentType = JewelEffectInfo[material].Item2;
                baseRating = JewelEffectInfo[material].Item3;
                bonusPerQuality = JewelEffectInfo[material].Item4;
                description =
                    $"Socket this jewel in a {equipmentType} of workmanship {workmanship} or greater to grant {baseRating}% reduced magic damage taken, plus an additional {bonusPerQuality}% per equipped {name} rating. " +
                    $"The amount builds up from 0%, based on how often you have recently been hit with a damaging spell. " +
                    $"Once socketed, the bracelet can only be worn on the right wrist.\n\n" +
                    $"{JewelStatsDescription(baseRating, amount, bonusPerQuality, name)}\n\n";
                break;
            case ACE.Entity.Enum.MaterialType.Diamond:
                material = ACE.Entity.Enum.MaterialType.Diamond;
                name = JewelEffectInfo[material].Item1;
                equipmentType = JewelEffectInfo[material].Item2;
                baseRating = JewelEffectInfo[material].Item3;
                bonusPerQuality = JewelEffectInfo[material].Item4;
                description =
                    $"Socket this jewel in a {equipmentType} of workmanship {workmanship} or greater to grant {baseRating}% reduced physical damage taken, plus an additional {bonusPerQuality}% per equipped {name} rating. " +
                    $"The amount builds up from 0%, based on how often you have recently been hit with a damaging physical attack. " +
                    $"Once socketed, the bracelet can only be worn on the right wrist.\n\n" +
                    $"{JewelStatsDescription(baseRating, amount, bonusPerQuality, name)}\n\n";
                break;
            case ACE.Entity.Enum.MaterialType.Onyx:
                material = ACE.Entity.Enum.MaterialType.Onyx;
                name = JewelEffectInfo[material].Item1;
                equipmentType = JewelEffectInfo[material].Item2;
                baseRating = JewelEffectInfo[material].Item3;
                bonusPerQuality = JewelEffectInfo[material].Item4;
                description =
                    $"Socket this jewel in a {equipmentType} of workmanship {workmanship} or greater to grant {baseRating}% protection against Slashing, Bludgeoning, and Piercing damage types, plus an additional {bonusPerQuality}% per equipped {name} rating. " +
                    $"Once socketed, the bracelet can only be worn on the right wrist.\n\n" +
                    $"{JewelStatsDescription(baseRating, amount, bonusPerQuality, name)}\n\n";
                break;
            case ACE.Entity.Enum.MaterialType.Zircon:
                material = ACE.Entity.Enum.MaterialType.Zircon;
                name = JewelEffectInfo[material].Item1;
                equipmentType = JewelEffectInfo[material].Item2;
                baseRating = JewelEffectInfo[material].Item3;
                bonusPerQuality = JewelEffectInfo[material].Item4;
                description =
                    $"Socket this jewel in a {equipmentType} of workmanship {workmanship} or greater to grant {baseRating}% protection against Flame, Frost, Lightning, and Acid damage types, plus an additional {bonusPerQuality}% per equipped {name} rating. " +
                    $"Once socketed, the bracelet can only be worn on the right wrist.\n\n" +
                    $"{JewelStatsDescription(baseRating, amount, bonusPerQuality, name)}\n\n";
                break;

            // shield
            case ACE.Entity.Enum.MaterialType.Turquoise:
                material = ACE.Entity.Enum.MaterialType.Turquoise;
                name = JewelEffectInfo[material].Item1;
                equipmentType = JewelEffectInfo[material].Item2;
                baseRating = JewelEffectInfo[material].Item3;
                bonusPerQuality = JewelEffectInfo[material].Item4;
                description =
                    $"Socket this jewel in a {equipmentType} of workmanship {workmanship} or greater to grant {baseRating}% increased block chance, plus an additional {bonusPerQuality}% per equipped {name} rating.\n\n" +
                    $"{JewelStatsDescription(baseRating, amount, bonusPerQuality, name)}\n\n";
                break;
            case ACE.Entity.Enum.MaterialType.WhiteQuartz:
                material = ACE.Entity.Enum.MaterialType.WhiteQuartz;
                name = JewelEffectInfo[material].Item1;
                equipmentType = JewelEffectInfo[material].Item2;
                baseRating = JewelEffectInfo[material].Item3;
                bonusPerQuality = JewelEffectInfo[material].Item4;
                description =
                    $"Socket this jewel in a {equipmentType} of workmanship {workmanship} or greater to deflect {baseRating}% of a blocked attack's damage back to a close-range attacker, plus an additional {bonusPerQuality}% per equipped {name} rating.\n\n" +
                    $"{JewelStatsDescription(baseRating, amount, bonusPerQuality, name)}\n\n";
                break;

            // weapon + shield
            case ACE.Entity.Enum.MaterialType.BlackOpal:
                material = ACE.Entity.Enum.MaterialType.BlackOpal;
                name = JewelEffectInfo[material].Item1;
                equipmentType = JewelEffectInfo[material].Item2;
                baseRating = JewelEffectInfo[material].Item3;
                bonusPerQuality = JewelEffectInfo[material].Item4;
                description =
                    $"Socket this jewel in a {equipmentType} of workmanship {workmanship} or greater to grant a {baseRating}% chance to evade an incoming critical hit, plus an additional {bonusPerQuality}% per equipped {name} rating. " +
                    $"Your next attack after a the evade is a guaranteed critical.\n\n" +
                    $"{JewelStatsDescription(baseRating, amount, bonusPerQuality, name)}\n\n";
                break;
            case ACE.Entity.Enum.MaterialType.FireOpal:
                material = ACE.Entity.Enum.MaterialType.FireOpal;
                name = JewelEffectInfo[material].Item1;
                equipmentType = JewelEffectInfo[material].Item2;
                baseRating = JewelEffectInfo[material].Item3;
                bonusPerQuality = JewelEffectInfo[material].Item4;
                description =
                    $"Socket this jewel in a {equipmentType} of workmanship {workmanship} or greater to grant up to {baseRating}% increased evade and resist chance versus the target you are attacking, plus an additional {bonusPerQuality}% per equipped {name} rating. " +
                    $"The amount builds up from 0%, based on how often you have recently hit the target.\n\n" +
                    $"{JewelStatsDescription(baseRating, amount, bonusPerQuality, name)}\n\n";
                break;
            case ACE.Entity.Enum.MaterialType.YellowGarnet:
                material = ACE.Entity.Enum.MaterialType.YellowGarnet;
                name = JewelEffectInfo[material].Item1;
                equipmentType = JewelEffectInfo[material].Item2;
                baseRating = JewelEffectInfo[material].Item3;
                bonusPerQuality = JewelEffectInfo[material].Item4;
                description =
                    $"Socket this jewel in a {equipmentType} of workmanship {workmanship} or greater to grant up to {baseRating}% increased physical hit chance, plus an additional {bonusPerQuality}% per equipped {name} rating. " +
                    $"The amount builds up from 0%, based on how often you have recently hit the target.\n\n" +
                    $"{JewelStatsDescription(baseRating, amount, bonusPerQuality, name)}\n\n";
                break;
            case ACE.Entity.Enum.MaterialType.Ruby:
                material = ACE.Entity.Enum.MaterialType.Ruby;
                name = JewelEffectInfo[material].Item1;
                equipmentType = JewelEffectInfo[material].Item2;
                baseRating = JewelEffectInfo[material].Item3;
                bonusPerQuality = JewelEffectInfo[material].Item4;
                description =
                    $"Socket this jewel in a {equipmentType} of workmanship {workmanship} or greater to grant increased damage as you lose health, up to a maximum bonus of {baseRating}% at 0 health, plus an additional {bonusPerQuality}% per equipped {name} rating. " +
                    $"{JewelStatsDescription(baseRating, amount, bonusPerQuality, name)}\n\n";
                break;

            // weapon only
            case ACE.Entity.Enum.MaterialType.Bloodstone:
                material = ACE.Entity.Enum.MaterialType.Bloodstone;
                name = JewelEffectInfo[material].Item1;
                equipmentType = JewelEffectInfo[material].Item2;
                baseRating = JewelEffectInfo[material].Item3;
                bonusPerQuality = JewelEffectInfo[material].Item4;
                description =
                    $"Socket this jewel in a {equipmentType} of workmanship {workmanship} or greater to grant a {baseRating}% chance on hit to gain health, plus an additional {bonusPerQuality}% per equipped {name} rating. " +
                    $"Amount stolen is equal to 10% of damage dealt.\n\n" +
                    $"{JewelStatsDescription(baseRating, amount, bonusPerQuality, name)}\n\n";
                break;
            case ACE.Entity.Enum.MaterialType.Opal:
                material = ACE.Entity.Enum.MaterialType.Opal;
                name = JewelEffectInfo[material].Item1;
                equipmentType = JewelEffectInfo[material].Item2;
                baseRating = JewelEffectInfo[material].Item3;
                bonusPerQuality = JewelEffectInfo[material].Item4;
                description =
                    $"Socket this jewel in a {equipmentType} of workmanship {workmanship} or greater to grant a {baseRating}% chance on hit to gain mana, plus an additional {bonusPerQuality}% per equipped {name} rating. " +
                    $"Amount stolen is equal to 10% of damage dealt.\n\n" +
                    $"{JewelStatsDescription(baseRating, amount, bonusPerQuality, name)}\n\n";
                break;
            case ACE.Entity.Enum.MaterialType.Hematite:
                material = ACE.Entity.Enum.MaterialType.Hematite;
                name = JewelEffectInfo[material].Item1;
                equipmentType = JewelEffectInfo[material].Item2;
                baseRating = JewelEffectInfo[material].Item3;
                bonusPerQuality = JewelEffectInfo[material].Item4;
                description =
                    $"Socket this jewel in a {equipmentType} of workmanship {workmanship} or greater to grant {baseRating}% increased damage with all attacks, plus an additional {bonusPerQuality}% per equipped {name} rating. " +
                    $"However, 25% of your attacks will deal the extra damage to yourself as well.\n\n" +
                    $"{JewelStatsDescription(baseRating, amount, bonusPerQuality, name)}\n\n";
                break;
            case ACE.Entity.Enum.MaterialType.RoseQuartz:
                material = ACE.Entity.Enum.MaterialType.RoseQuartz;
                name = JewelEffectInfo[material].Item1;
                equipmentType = JewelEffectInfo[material].Item2;
                baseRating = JewelEffectInfo[material].Item3;
                bonusPerQuality = JewelEffectInfo[material].Item4;
                description =
                    $"Socket this jewel in a {equipmentType} of workmanship {workmanship} or greater to grant {baseRating}% bonus to your Vitals Transfer spells, plus an additional {bonusPerQuality}% per equipped {name} rating. " +
                    $"Receive an equivalent reduction in the effectiveness of your other Restoration spells.\n\n" +
                    $"{JewelStatsDescription(baseRating, amount, bonusPerQuality, name)}\n\n";
                break;
            case ACE.Entity.Enum.MaterialType.LavenderJade:
                material = ACE.Entity.Enum.MaterialType.LavenderJade;
                name = JewelEffectInfo[material].Item1;
                equipmentType = JewelEffectInfo[material].Item2;
                baseRating = JewelEffectInfo[material].Item3;
                bonusPerQuality = JewelEffectInfo[material].Item4;
                description =
                    $"Socket this jewel in a {equipmentType} of workmanship {workmanship} or greater to grant {baseRating}% bonus to your restoration spells when cast on others, plus an additional {bonusPerQuality}% per equipped {name} rating. " +
                    $"Receive an equivalent reduction in the effectiveness when cast on yourself.\n\n" +
                    $"{JewelStatsDescription(baseRating, amount, bonusPerQuality, name)}\n\n";
                break;
            case ACE.Entity.Enum.MaterialType.GreenGarnet:
                material = ACE.Entity.Enum.MaterialType.GreenGarnet;
                name = JewelEffectInfo[material].Item1;
                equipmentType = JewelEffectInfo[material].Item2;
                baseRating = JewelEffectInfo[material].Item3;
                bonusPerQuality = JewelEffectInfo[material].Item4;
                description =
                    $"Socket this jewel in a {equipmentType} of workmanship {workmanship} or greater to grant up to {baseRating}% bonus damage to your War Magic Spells, plus an additional {bonusPerQuality}% per equipped {name} rating. " +
                    $"The amount builds up from 0%, based on how often you have recently hit the target.\n\n" +
                    $"{JewelStatsDescription(baseRating, amount, bonusPerQuality, name)}\n\n";
                break;
            case ACE.Entity.Enum.MaterialType.Tourmaline:
                material = ACE.Entity.Enum.MaterialType.Tourmaline;
                name = JewelEffectInfo[material].Item1;
                equipmentType = JewelEffectInfo[material].Item2;
                baseRating = JewelEffectInfo[material].Item3;
                bonusPerQuality = JewelEffectInfo[material].Item4;
                description =
                    $"Socket this jewel in a {equipmentType} of workmanship {workmanship} or greater to grant up to {baseRating}% ward penetration, plus an additional {bonusPerQuality}% per equipped {name} rating. " +
                    $"The amount builds up from 0%, based on how often you have recently hit the target.\n\n" +
                    $"{JewelStatsDescription(baseRating, amount, bonusPerQuality, name)}\n\n";
                break;
            case ACE.Entity.Enum.MaterialType.WhiteJade:
                material = ACE.Entity.Enum.MaterialType.WhiteJade;
                name = JewelEffectInfo[material].Item1;
                equipmentType = JewelEffectInfo[material].Item2;
                baseRating = JewelEffectInfo[material].Item3;
                bonusPerQuality = JewelEffectInfo[material].Item4;
                baseRatingSecondary = JewelEffectInfo[material].Item5;
                bonusPerQualitySecondary = JewelEffectInfo[material].Item6;
                description =
                    $"Socket this jewel in a {equipmentType} of workmanship {workmanship} or greater to grant a {baseRating}% bonus to your restoration spells, plus an additional {bonusPerQuality}% per equipped {name} rating. " +
                    $"Also grants a {baseRatingSecondary}% chance to create a sphere of healing energy on top of your target when casting a restoration spell, plus an additional {bonusPerQualitySecondary}% per equipped rating .\n\n" +
                    $"{JewelStatsDescription(baseRating, amount, bonusPerQuality, name)}\n\n";
                break;
            case ACE.Entity.Enum.MaterialType.Aquamarine:
                material = ACE.Entity.Enum.MaterialType.Aquamarine;
                name = JewelEffectInfo[material].Item1;
                equipmentType = JewelEffectInfo[material].Item2;
                baseRating = JewelEffectInfo[material].Item3;
                bonusPerQuality = JewelEffectInfo[material].Item4;
                baseRatingSecondary = JewelEffectInfo[material].Item5;
                bonusPerQualitySecondary = JewelEffectInfo[material].Item6;
                description =
                    $"Socket this jewel in a {equipmentType} of workmanship {workmanship} or greater to grant a {baseRating}% bonus to cold damage, plus an additional {bonusPerQuality}% per equipped {name} rating. " +
                    $"Also grants a {baseRatingSecondary}% chance to surround your target with chilling mist, plus an additional {bonusPerQualitySecondary}% per equipped rating .\n\n" +
                    $"{JewelStatsDescription(baseRating, amount, bonusPerQuality, name)}\n\n";
                break;
            case ACE.Entity.Enum.MaterialType.BlackGarnet:
                material = ACE.Entity.Enum.MaterialType.BlackGarnet;
                name = JewelEffectInfo[material].Item1;
                equipmentType = JewelEffectInfo[material].Item2;
                baseRating = JewelEffectInfo[material].Item3;
                bonusPerQuality = JewelEffectInfo[material].Item4;
                description =
                    $"Socket this jewel in a {equipmentType} of workmanship {workmanship} or greater to grant up to {baseRating}% piercing resistance penetration, plus an additional {bonusPerQuality}% per equipped {name} rating. " +
                    $"The amount builds up from 0%, based on how often you have recently hit the target.\n\n" +
                    $"{JewelStatsDescription(baseRating, amount, bonusPerQuality, name)}\n\n";
                break;
            case ACE.Entity.Enum.MaterialType.Emerald:
                material = ACE.Entity.Enum.MaterialType.Emerald;
                name = JewelEffectInfo[material].Item1;
                equipmentType = JewelEffectInfo[material].Item2;
                baseRating = JewelEffectInfo[material].Item3;
                bonusPerQuality = JewelEffectInfo[material].Item4;
                baseRatingSecondary = JewelEffectInfo[material].Item5;
                bonusPerQualitySecondary = JewelEffectInfo[material].Item6;
                description =
                    $"Socket this jewel in a {equipmentType} of workmanship {workmanship} or greater to grant a {baseRating}% bonus to acid damage, plus an additional {bonusPerQuality}% per equipped {name} rating. " +
                    $"Also grants a {baseRatingSecondary}% chance to surround your target with acidic mist, plus an additional {bonusPerQualitySecondary}% per equipped rating .\n\n" +
                    $"{JewelStatsDescription(baseRating, amount, bonusPerQuality, name)}\n\n";
                break;
            case ACE.Entity.Enum.MaterialType.ImperialTopaz:
                material = ACE.Entity.Enum.MaterialType.ImperialTopaz;
                name = JewelEffectInfo[material].Item1;
                equipmentType = JewelEffectInfo[material].Item2;
                baseRating = JewelEffectInfo[material].Item3;
                bonusPerQuality = JewelEffectInfo[material].Item4;
                description =
                    $"Socket this jewel in a {equipmentType} of workmanship {workmanship} or greater to grants a {baseRating}% chance to cleave an additional target, plus an additional {bonusPerQuality}% per equipped {name} rating.\n\n" +
                    $"{JewelStatsDescription(baseRating, amount, bonusPerQuality, name)}\n\n";
                break;
            case ACE.Entity.Enum.MaterialType.Jet:
                material = ACE.Entity.Enum.MaterialType.Jet;
                name = JewelEffectInfo[material].Item1;
                equipmentType = JewelEffectInfo[material].Item2;
                baseRating = JewelEffectInfo[material].Item3;
                bonusPerQuality = JewelEffectInfo[material].Item4;
                baseRatingSecondary = JewelEffectInfo[material].Item5;
                bonusPerQualitySecondary = JewelEffectInfo[material].Item6;
                description =
                    $"Socket this jewel in a {equipmentType} of workmanship {workmanship} or greater to grant a {baseRating}% bonus to lightning damage, plus an additional {bonusPerQuality}% per equipped {name} rating. " +
                    $"Also grants a {baseRatingSecondary}% chance to electrify the ground beneath your target, plus an additional {bonusPerQualitySecondary}% per equipped rating .\n\n" +
                    $"{JewelStatsDescription(baseRating, amount, bonusPerQuality, name)}\n\n";
                break;
            case ACE.Entity.Enum.MaterialType.RedGarnet:
                material = ACE.Entity.Enum.MaterialType.RedGarnet;
                name = JewelEffectInfo[material].Item1;
                equipmentType = JewelEffectInfo[material].Item2;
                baseRating = JewelEffectInfo[material].Item3;
                bonusPerQuality = JewelEffectInfo[material].Item4;
                baseRatingSecondary = JewelEffectInfo[material].Item5;
                bonusPerQualitySecondary = JewelEffectInfo[material].Item6;
                description =
                    $"Socket this jewel in a {equipmentType} of workmanship {workmanship} or greater to grant a {baseRating}% bonus to fire damage, plus an additional {bonusPerQuality}% per equipped {name} rating. " +
                    $"Also grants a {baseRatingSecondary}% chance to set the ground beneath your target ablaze, plus an additional {bonusPerQualitySecondary}% per equipped rating .\n\n" +
                    $"{JewelStatsDescription(baseRating, amount, bonusPerQuality, name)}\n\n";
                break;
            case ACE.Entity.Enum.MaterialType.WhiteSapphire:
                material = ACE.Entity.Enum.MaterialType.WhiteSapphire;
                name = JewelEffectInfo[material].Item1;
                equipmentType = JewelEffectInfo[material].Item2;
                baseRating = JewelEffectInfo[material].Item3;
                bonusPerQuality = JewelEffectInfo[material].Item4;
                description =
                    $"Socket this jewel in a {equipmentType} of workmanship {workmanship} or greater to grant up to {baseRating}% bonus critical bludgeoning damage, plus an additional {bonusPerQuality}% per equipped {name} rating. " +
                    $"The amount builds up from 0%, based on how often you have recently hit the target.\n\n" +
                    $"{JewelStatsDescription(baseRating, amount, bonusPerQuality, name)}\n\n";
                break;
        }
        return description;
    }

    private static string JewelStatsDescription(int baseRating, int amount, float bonusPerQuality, string name)
    {
        return $"Base Rating: {baseRating}\n" +
               $"Quality: {amount} ({JewelQuality[amount]})\n" +
               $"Quality Bonus: {bonusPerQuality * amount} ({bonusPerQuality} x {amount})\n" +
               $"Total Rating: {baseRating + bonusPerQuality * amount}\n\n" +
               $"Additional sources of {name} will only add the bonus rating.";
    }

    public static string GetSocketDescription(string jewelSocket1)
    {
        var parts = jewelSocket1.Split('/');

        var description = "";
        if (!MaterialTypetoString.TryGetValue(parts[1], out var convertedMaterialType))
        {
            return description;
        }

        if (!int.TryParse(parts[4], out var amount))
        {
            return description;
        }

        var oneTenth = Math.Round((float)amount / 10, 2);
        var half = Math.Round((float)amount / 2, 1);
        var doubled = Math.Round((float)amount * 2, 1);
        var tripled = Math.Round((float)amount * 3, 1);

        switch (convertedMaterialType)
        {
            // ring right
            case ACE.Entity.Enum.MaterialType.Carnelian:
                description = $"\n\t Socket:  {parts[1]} (+{amount}  {parts[3]})\n";
                break;
            case ACE.Entity.Enum.MaterialType.Azurite:
                description = $"\n\t Socket:  {parts[1]} (+{amount}  {parts[3]})\n";
                break;
            case ACE.Entity.Enum.MaterialType.TigerEye:
                description = $"\n\t Socket:  {parts[1]} (+{amount}  {parts[3]})\n";
                break;

            // ring left
            case ACE.Entity.Enum.MaterialType.RedJade:
                description = $"\n\t Socket:  {parts[1]} (+{amount}  {parts[3]})\n";
                break;
            case ACE.Entity.Enum.MaterialType.YellowTopaz:
                description = $"\n\t Socket:  {parts[1]} (+{amount}  {parts[3]})\n";
                break;
            case ACE.Entity.Enum.MaterialType.Peridot:
                description = $"\n\t Socket:  {parts[1]} (+{amount}  {parts[3]})\n";
                break;

            case ACE.Entity.Enum.MaterialType.Moonstone:
                description = $"\n\t Socket:  {parts[1]} (-{amount * 5}% Item Mana Consumption)\n";
                break;
            case ACE.Entity.Enum.MaterialType.Malachite:
                description = $"\n\t Socket:  {parts[1]} (+{tripled}% Component Burn Reduction)\n";
                break;
            case ACE.Entity.Enum.MaterialType.GreenJade:
                description = $"\n\t Socket:  {parts[1]} (+{half}% Prosperity)\n";
                break;

            // bracelet right
            case ACE.Entity.Enum.MaterialType.Amethyst:
                description = $"\n\t Socket:  {parts[1]} (+{doubled}% Ramping Magic Absorb)\n";
                break;
            case ACE.Entity.Enum.MaterialType.Diamond:
                description = $"\n\t Socket:  {parts[1]} (+{doubled}% Ramping Physical Damage Resistance)\n";
                break;
            // shield
            case ACE.Entity.Enum.MaterialType.WhiteQuartz:
                description = $"\n\t Socket:  {parts[1]} (+{amount * 5}% Shield Reprisal)\n";
                break;
            // weapon + shield
            case ACE.Entity.Enum.MaterialType.BlackOpal:
                description = $"\n\t Socket:  {parts[1]} (+{half}% Critical Reprisal)\n";
                break;

            // weapon only
            case ACE.Entity.Enum.MaterialType.Bloodstone:
                description = $"\n\t Socket:  {parts[1]} (+{half}% Life Steal)\n";
                break;
            case ACE.Entity.Enum.MaterialType.Opal:
                description = $"\n\t Socket:  {parts[1]} (+{half}% Mana Leech)\n";
                break;
            case ACE.Entity.Enum.MaterialType.RoseQuartz:
                description = $"\n\t Socket:  {parts[1]} (+{doubled}% Vitals Transfer)\n";
                break;
            case ACE.Entity.Enum.MaterialType.LavenderJade:
                description = $"\n\t Socket:  {parts[1]} (+{doubled}% Selflessness)\n";
                break;
            case ACE.Entity.Enum.MaterialType.GreenGarnet:
                description = $"\n\t Socket:  {parts[1]} (+{doubled}% Ramping War Damage)\n";
                break;
            case ACE.Entity.Enum.MaterialType.Tourmaline:
                description = $"\n\t Socket:  {parts[1]} (+{doubled}% Ramping Ward Pen)\n";
                break;
            case ACE.Entity.Enum.MaterialType.WhiteJade:
                description = $"\n\t Socket:  {parts[1]} (+{amount}% Restoration)\n";
                break;
            case ACE.Entity.Enum.MaterialType.Aquamarine:
                description = $"\n\t Socket:  {parts[1]} (+{amount}% Frost Damage)\n";
                break;
            case ACE.Entity.Enum.MaterialType.BlackGarnet:
                description = $"\n\t Socket:  {parts[1]} (+{amount}% Ramping Pierce Pen)\n";
                break;
            case ACE.Entity.Enum.MaterialType.Emerald:
                description = $"\n\t Socket:  {parts[1]} (+{amount}% Acid Damage)\n";
                break;
            case ACE.Entity.Enum.MaterialType.ImperialTopaz:
                description = $"\n\t Socket:  {parts[1]} (+{half}% Cleave Chance)\n";
                break;
            case ACE.Entity.Enum.MaterialType.Jet:
                description = $"\n\t Socket:  {parts[1]} (+{amount}% Lightning Damage)\n";
                break;
            case ACE.Entity.Enum.MaterialType.RedGarnet:
                description = $"\n\t Socket:  {parts[1]} (+{amount}% Fire Damage)\n";
                break;
            case ACE.Entity.Enum.MaterialType.WhiteSapphire:
                description = $"\n\t Socket:  {parts[1]} (+{doubled}% Ramping Bludgeon Crit Damage)\n";
                break;
            default:
                description = $"\n\t Socket:  {parts[1]} (+{amount}%  {parts[3]})\n";
                break;
        }
        return description;
    }

    private static readonly Dictionary<MaterialType, DamageType> MaterialDamage = new()
    {
        { ACE.Entity.Enum.MaterialType.Aquamarine, DamageType.Cold },
        { ACE.Entity.Enum.MaterialType.BlackGarnet, DamageType.Pierce },
        { ACE.Entity.Enum.MaterialType.Emerald, DamageType.Acid },
        { ACE.Entity.Enum.MaterialType.ImperialTopaz, DamageType.Slash },
        { ACE.Entity.Enum.MaterialType.Jet, DamageType.Electric },
        { ACE.Entity.Enum.MaterialType.RedGarnet, DamageType.Fire },
        { ACE.Entity.Enum.MaterialType.WhiteSapphire, DamageType.Bludgeon },
    };

    private static readonly Dictionary<string, PropertyInt> StringToIntProperties = new()
    {
        { "Strength", PropertyInt.GearStrength },
        { "Endurance", PropertyInt.GearEndurance },
        { "Coordination", PropertyInt.GearCoordination },
        { "Quickness", PropertyInt.GearQuickness },
        { "Focus", PropertyInt.GearFocus },
        { "Self", PropertyInt.GearSelf },
        { "Threat Enhancement", PropertyInt.GearThreatGain },
        { "Threat Reduction", PropertyInt.GearThreatReduction },
        { "Elemental Warding", PropertyInt.GearElementalWard },
        { "Physical Warding", PropertyInt.GearPhysicalWard },
        { "Block Rating", PropertyInt.GearBlock },
        { "Magic Find", PropertyInt.GearMagicFind },
        { "Item Mana Usage", PropertyInt.GearItemManaUsage },
        { "Shield Deflection", PropertyInt.GearThorns },
        { "Life Steal", PropertyInt.GearLifesteal },
        { "Blood Frenzy", PropertyInt.GearSelfHarm },
        { "Selflessness", PropertyInt.GearSelflessness },
        { "Focused Assault", PropertyInt.GearFamiliarity },
        { "Bravado", PropertyInt.GearBravado },
        { "Health To Stamina", PropertyInt.GearHealthToStamina },
        { "Health To Mana", PropertyInt.GearHealthToMana },
        { "Experience Gain", PropertyInt.GearExperienceGain },
        { "Red Fury", PropertyInt.GearRedFury },
        { "Yellow Fury", PropertyInt.GearYellowFury },
        { "Blue Fury", PropertyInt.GearBlueFury },
        { "Manasteal", PropertyInt.GearManasteal },
        { "Vitals Transfer", PropertyInt.GearVitalsTransfer },
        { "Bludgeon", PropertyInt.GearBludgeon },
        { "Pierce", PropertyInt.GearPierce },
        { "Slash", PropertyInt.GearSlash },
        { "Fire", PropertyInt.GearFire },
        { "Frost", PropertyInt.GearFrost },
        { "Acid", PropertyInt.GearAcid },
        { "Lightning", PropertyInt.GearLightning },
        { "Heal", PropertyInt.GearHealBubble },
        { "Components", PropertyInt.GearCompBurn },
        { "Nullification", PropertyInt.GearNullification },
        { "Ward Pen", PropertyInt.GearWardPen },
        { "Stamina Reduction", PropertyInt.GearStamReduction },
        { "Hardened Defense", PropertyInt.GearHardenedDefense },
        { "Prosperity", PropertyInt.GearPyrealFind },
        { "Familiarity", PropertyInt.GearFamiliarity },
        { "Reprisal", PropertyInt.GearReprisal },
        { "Elementalist", PropertyInt.GearElementalist }
    };

    private static readonly Dictionary<int, string> JewelQuality = new()
    {
        { 1, "Scuffed" },
        { 2, "Flawed" },
        { 3, "Mediocre" },
        { 4, "Fine" },
        { 5, "Admirable" },
        { 6, "Superior" },
        { 7, "Excellent" },
        { 8, "Magnificent" },
        { 9, "Peerless" },
        { 10, "Flawless" }
    };

    private static readonly Dictionary<MaterialType?, int> JewelValidLocations = new()
    {
        // weapon only
        { ACE.Entity.Enum.MaterialType.ImperialTopaz, 1 },
        { ACE.Entity.Enum.MaterialType.BlackGarnet, 1 },
        { ACE.Entity.Enum.MaterialType.Jet, 1 },
        { ACE.Entity.Enum.MaterialType.RedGarnet, 1 },
        { ACE.Entity.Enum.MaterialType.Aquamarine, 1 },
        { ACE.Entity.Enum.MaterialType.WhiteSapphire, 1 },
        { ACE.Entity.Enum.MaterialType.Emerald, 1 },
        { ACE.Entity.Enum.MaterialType.Tourmaline, 1 },
        { ACE.Entity.Enum.MaterialType.Opal, 1 },
        { ACE.Entity.Enum.MaterialType.RoseQuartz, 1 },
        { ACE.Entity.Enum.MaterialType.Hematite, 1 },
        { ACE.Entity.Enum.MaterialType.Bloodstone, 1 },
        { ACE.Entity.Enum.MaterialType.WhiteJade, 1 },
        { ACE.Entity.Enum.MaterialType.GreenGarnet, 1 },
        { ACE.Entity.Enum.MaterialType.LavenderJade, 1 },
        // shield only
        { ACE.Entity.Enum.MaterialType.WhiteQuartz, 2 },
        { ACE.Entity.Enum.MaterialType.Turquoise, 2 },
        // shield or melee weapon
        { ACE.Entity.Enum.MaterialType.Ruby, 3 },
        { ACE.Entity.Enum.MaterialType.BlackOpal, 3 },
        { ACE.Entity.Enum.MaterialType.FireOpal, 3 },
        { ACE.Entity.Enum.MaterialType.YellowGarnet, 3 },
        // bracelet only (left rest)
        { ACE.Entity.Enum.MaterialType.SmokeyQuartz, 196608 },
        { ACE.Entity.Enum.MaterialType.Agate, 196608 },
        { ACE.Entity.Enum.MaterialType.Moonstone, 196608 },
        { ACE.Entity.Enum.MaterialType.Citrine, 196608 },
        { ACE.Entity.Enum.MaterialType.LapisLazuli, 196608 },
        { ACE.Entity.Enum.MaterialType.Malachite, 196608 },
        { ACE.Entity.Enum.MaterialType.Amber, 196608 },
        // bracelet only (right rest)
        { ACE.Entity.Enum.MaterialType.Zircon, 196608 },
        { ACE.Entity.Enum.MaterialType.Diamond, 196608 },
        { ACE.Entity.Enum.MaterialType.Onyx, 196608 },
        { ACE.Entity.Enum.MaterialType.Amethyst, 196608 },
        // ring only (left rest)
        { ACE.Entity.Enum.MaterialType.Peridot, 786432 },
        { ACE.Entity.Enum.MaterialType.RedJade, 786432 },
        { ACE.Entity.Enum.MaterialType.YellowTopaz, 786432 },
        // ring only (right rest)
        { ACE.Entity.Enum.MaterialType.Carnelian, 786432 },
        { ACE.Entity.Enum.MaterialType.Azurite, 786432 },
        { ACE.Entity.Enum.MaterialType.TigerEye, 786432 },
        // necklace only
        { ACE.Entity.Enum.MaterialType.Sapphire, 32768 },
        { ACE.Entity.Enum.MaterialType.Sunstone, 32768 },
        { ACE.Entity.Enum.MaterialType.GreenJade, 32768 },
    };

    private static readonly Dictionary<MaterialType?, int> JewelUiEffect = new()
    {
        { ACE.Entity.Enum.MaterialType.Diamond, 512 },
        { ACE.Entity.Enum.MaterialType.Moonstone, 512 },
        { ACE.Entity.Enum.MaterialType.WhiteJade, 512 },
        { ACE.Entity.Enum.MaterialType.WhiteQuartz, 512 },
        { ACE.Entity.Enum.MaterialType.WhiteSapphire, 512 },
        { ACE.Entity.Enum.MaterialType.SmokeyQuartz, 512 },
        { ACE.Entity.Enum.MaterialType.Zircon, 512 },
        // Black
        { ACE.Entity.Enum.MaterialType.BlackGarnet, 512 },
        { ACE.Entity.Enum.MaterialType.BlackOpal, 512 },
        { ACE.Entity.Enum.MaterialType.Jet, 512 },
        { ACE.Entity.Enum.MaterialType.Onyx, 512 },
        // Gray
        { ACE.Entity.Enum.MaterialType.Agate, 512 },
        { ACE.Entity.Enum.MaterialType.Hematite, 512 },
        { ACE.Entity.Enum.MaterialType.Bloodstone, 512 },
        // Red
        { ACE.Entity.Enum.MaterialType.Carnelian, 4 },
        { ACE.Entity.Enum.MaterialType.FireOpal, 4 },
        { ACE.Entity.Enum.MaterialType.RedGarnet, 4 },
        { ACE.Entity.Enum.MaterialType.Ruby, 4 },
        // Orange
        { ACE.Entity.Enum.MaterialType.Citrine, 16 },
        { ACE.Entity.Enum.MaterialType.Sunstone, 16 },
        // Yellow
        { ACE.Entity.Enum.MaterialType.Amber, 16 },
        { ACE.Entity.Enum.MaterialType.ImperialTopaz, 16 },
        { ACE.Entity.Enum.MaterialType.TigerEye, 16 },
        { ACE.Entity.Enum.MaterialType.YellowGarnet, 16 },
        { ACE.Entity.Enum.MaterialType.YellowTopaz, 16 },
        // Green
        { ACE.Entity.Enum.MaterialType.Emerald, 256 },
        { ACE.Entity.Enum.MaterialType.GreenGarnet, 256 },
        { ACE.Entity.Enum.MaterialType.GreenJade, 256 },
        { ACE.Entity.Enum.MaterialType.Malachite, 256 },
        { ACE.Entity.Enum.MaterialType.Peridot, 256 },
        // Blue
        { ACE.Entity.Enum.MaterialType.Aquamarine, 1 },
        { ACE.Entity.Enum.MaterialType.Azurite, 1 },
        { ACE.Entity.Enum.MaterialType.LapisLazuli, 1 },
        { ACE.Entity.Enum.MaterialType.Opal, 1 },
        { ACE.Entity.Enum.MaterialType.Sapphire, 1 },
        { ACE.Entity.Enum.MaterialType.Tourmaline, 1 },
        // Purple and Pink
        { ACE.Entity.Enum.MaterialType.Amethyst, 64 },
        { ACE.Entity.Enum.MaterialType.LavenderJade, 64 },
        { ACE.Entity.Enum.MaterialType.RedJade, 64 },
        { ACE.Entity.Enum.MaterialType.RoseQuartz, 64 },
        // Teal
        { ACE.Entity.Enum.MaterialType.Turquoise, 1 }
    };

    private static readonly Dictionary<string, uint> GemstoneIconMap = new()
    {
        { "Agate", 0x06002CAD },
        { "Amber", 0x06002CAE },
        { "Amethyst", 0x06002CAF },
        { "Aquamarine", 0x06002CB0 },
        { "Azurite", 0x06002CB1 },
        { "Black Garnet", 0x06002CB2 },
        { "Black Opal", 0x06002CB3 },
        { "Bloodstone", 0x06002CA7 },
        { "Carnelian", 0x06002CA8 },
        { "Citrine", 0x06002CA9 },
        { "Diamond", 0x06002CAA },
        { "Emerald", 0x06002CAB },
        { "Fire Opal", 0x06002CAC },
        { "Green Garnet", 0x06002CB4 },
        { "Green Jade", 0x06002CB5 },
        { "Hematite", 0x06002CB6 },
        { "Imperial Topaz", 0x06002CB7 },
        { "Jet", 0x06002CB8 },
        { "Lapis Lazuli", 0x06002CB9 },
        { "Lavender Jade", 0x06002CBA },
        { "Malachite", 0x06002CBB },
        { "Moonstone", 0x06002CBC },
        { "Onyx", 0x06002CBD },
        { "Opal", 0x06002CBE },
        { "Peridot", 0x06002CBF },
        { "Red Garnet", 0x06002CC0 },
        { "Red Jade", 0x06002C98 },
        { "Rose Quartz", 0x06002C99 },
        { "Ruby", 0x06002C9A },
        { "Sapphire", 0x06002C9B },
        { "Smokey Quartz", 0x06002C9C },
        { "Sunstone", 0x06002C9D },
        { "Tiger Eye", 0x06002C9E },
        { "Tourmaline", 0x06002C9F },
        { "Turquoise", 0x06002CA0 },
        { "White Jade", 0x06002CA1 },
        { "White Quartz", 0x06002CA2 },
        { "White Sapphire", 0x06002CA3 },
        { "Yellow Garnet", 0x06002CA4 },
        { "Yellow Topaz", 0x06002CA5 },
        { "Zircon", 0x06002CA6 }
    };

    private static readonly Dictionary<MaterialType, string> StringtoMaterialType = new()
    {
        { ACE.Entity.Enum.MaterialType.Unknown, "Unknown" },
        { ACE.Entity.Enum.MaterialType.Ceramic, "Ceramic" },
        { ACE.Entity.Enum.MaterialType.Porcelain, "Porcelain" },
        { ACE.Entity.Enum.MaterialType.Cloth, "Cloth" },
        { ACE.Entity.Enum.MaterialType.Linen, "Linen" },
        { ACE.Entity.Enum.MaterialType.Satin, "Satin" },
        { ACE.Entity.Enum.MaterialType.Silk, "Silk" },
        { ACE.Entity.Enum.MaterialType.Velvet, "Velvet" },
        { ACE.Entity.Enum.MaterialType.Wool, "Wool" },
        { ACE.Entity.Enum.MaterialType.Gem, "Gem" },
        { ACE.Entity.Enum.MaterialType.Agate, "Agate" },
        { ACE.Entity.Enum.MaterialType.Amber, "Amber" },
        { ACE.Entity.Enum.MaterialType.Amethyst, "Amethyst" },
        { ACE.Entity.Enum.MaterialType.Aquamarine, "Aquamarine" },
        { ACE.Entity.Enum.MaterialType.Azurite, "Azurite" },
        { ACE.Entity.Enum.MaterialType.BlackGarnet, "Black Garnet" },
        { ACE.Entity.Enum.MaterialType.BlackOpal, "Black Opal" },
        { ACE.Entity.Enum.MaterialType.Bloodstone, "Bloodstone" },
        { ACE.Entity.Enum.MaterialType.Carnelian, "Carnelian" },
        { ACE.Entity.Enum.MaterialType.Citrine, "Citrine" },
        { ACE.Entity.Enum.MaterialType.Diamond, "Diamond" },
        { ACE.Entity.Enum.MaterialType.Emerald, "Emerald" },
        { ACE.Entity.Enum.MaterialType.FireOpal, "Fire Opal" },
        { ACE.Entity.Enum.MaterialType.GreenGarnet, "Green Garnet" },
        { ACE.Entity.Enum.MaterialType.GreenJade, "Green Jade" },
        { ACE.Entity.Enum.MaterialType.Hematite, "Hematite" },
        { ACE.Entity.Enum.MaterialType.ImperialTopaz, "Imperial Topaz" },
        { ACE.Entity.Enum.MaterialType.Jet, "Jet" },
        { ACE.Entity.Enum.MaterialType.LapisLazuli, "Lapis Lazuli" },
        { ACE.Entity.Enum.MaterialType.LavenderJade, "Lavender Jade" },
        { ACE.Entity.Enum.MaterialType.Malachite, "Malachite" },
        { ACE.Entity.Enum.MaterialType.Moonstone, "Moonstone" },
        { ACE.Entity.Enum.MaterialType.Onyx, "Onyx" },
        { ACE.Entity.Enum.MaterialType.Opal, "Opal" },
        { ACE.Entity.Enum.MaterialType.Peridot, "Peridot" },
        { ACE.Entity.Enum.MaterialType.RedGarnet, "Red Garnet" },
        { ACE.Entity.Enum.MaterialType.RedJade, "Red Jade" },
        { ACE.Entity.Enum.MaterialType.RoseQuartz, "Rose Quartz" },
        { ACE.Entity.Enum.MaterialType.Ruby, "Ruby" },
        { ACE.Entity.Enum.MaterialType.Sapphire, "Sapphire" },
        { ACE.Entity.Enum.MaterialType.SmokeyQuartz, "Smokey Quartz" },
        { ACE.Entity.Enum.MaterialType.Sunstone, "Sunstone" },
        { ACE.Entity.Enum.MaterialType.TigerEye, "Tiger Eye" },
        { ACE.Entity.Enum.MaterialType.Tourmaline, "Tourmaline" },
        { ACE.Entity.Enum.MaterialType.Turquoise, "Turquoise" },
        { ACE.Entity.Enum.MaterialType.WhiteJade, "White Jade" },
        { ACE.Entity.Enum.MaterialType.WhiteQuartz, "White Quartz" },
        { ACE.Entity.Enum.MaterialType.WhiteSapphire, "White Sapphire" },
        { ACE.Entity.Enum.MaterialType.YellowGarnet, "Yellow Garnet" },
        { ACE.Entity.Enum.MaterialType.YellowTopaz, "Yellow Topaz" },
        { ACE.Entity.Enum.MaterialType.Zircon, "Zircon" },
        { ACE.Entity.Enum.MaterialType.Ivory, "Ivory" },
        { ACE.Entity.Enum.MaterialType.Leather, "Leather" },
        { ACE.Entity.Enum.MaterialType.ArmoredilloHide, "Armoredillo Hide" },
        { ACE.Entity.Enum.MaterialType.GromnieHide, "Gromnie Hide" },
        { ACE.Entity.Enum.MaterialType.ReedSharkHide, "Reed Shark Hide" },
        { ACE.Entity.Enum.MaterialType.Metal, "Metal" },
        { ACE.Entity.Enum.MaterialType.Brass, "Brass" },
        { ACE.Entity.Enum.MaterialType.Bronze, "Bronze" },
        { ACE.Entity.Enum.MaterialType.Copper, "Copper" },
        { ACE.Entity.Enum.MaterialType.Gold, "Gold" },
        { ACE.Entity.Enum.MaterialType.Iron, "Iron" },
        { ACE.Entity.Enum.MaterialType.Pyreal, "Pyreal" },
        { ACE.Entity.Enum.MaterialType.Silver, "Silver" },
        { ACE.Entity.Enum.MaterialType.Steel, "Steel" },
        { ACE.Entity.Enum.MaterialType.Stone, "Stone" },
        { ACE.Entity.Enum.MaterialType.Alabaster, "Alabaster" },
        { ACE.Entity.Enum.MaterialType.Granite, "Granite" },
        { ACE.Entity.Enum.MaterialType.Marble, "Marble" },
        { ACE.Entity.Enum.MaterialType.Obsidian, "Obsidian" },
        { ACE.Entity.Enum.MaterialType.Sandstone, "Sandstone" },
        { ACE.Entity.Enum.MaterialType.Serpentine, "Serpentine" },
        { ACE.Entity.Enum.MaterialType.Wood, "Wood" },
        { ACE.Entity.Enum.MaterialType.Ebony, "Ebony" },
        { ACE.Entity.Enum.MaterialType.Mahogany, "Mahogany" },
        { ACE.Entity.Enum.MaterialType.Oak, "Oak" },
        { ACE.Entity.Enum.MaterialType.Pine, "Pine" },
        { ACE.Entity.Enum.MaterialType.Teak, "Teak" }
    };

    private static readonly Dictionary<string, MaterialType> MaterialTypetoString = new()
    {
        { "Unknown", ACE.Entity.Enum.MaterialType.Unknown },
        { "Ceramic", ACE.Entity.Enum.MaterialType.Ceramic },
        { "Porcelain", ACE.Entity.Enum.MaterialType.Porcelain },
        { "Cloth", ACE.Entity.Enum.MaterialType.Cloth },
        { "Linen", ACE.Entity.Enum.MaterialType.Linen },
        { "Satin", ACE.Entity.Enum.MaterialType.Satin },
        { "Silk", ACE.Entity.Enum.MaterialType.Silk },
        { "Velvet", ACE.Entity.Enum.MaterialType.Velvet },
        { "Wool", ACE.Entity.Enum.MaterialType.Wool },
        { "Gem", ACE.Entity.Enum.MaterialType.Gem },
        { "Agate", ACE.Entity.Enum.MaterialType.Agate },
        { "Amber", ACE.Entity.Enum.MaterialType.Amber },
        { "Amethyst", ACE.Entity.Enum.MaterialType.Amethyst },
        { "Aquamarine", ACE.Entity.Enum.MaterialType.Aquamarine },
        { "Azurite", ACE.Entity.Enum.MaterialType.Azurite },
        { "Black Garnet", ACE.Entity.Enum.MaterialType.BlackGarnet },
        { "Black Opal", ACE.Entity.Enum.MaterialType.BlackOpal },
        { "Bloodstone", ACE.Entity.Enum.MaterialType.Bloodstone },
        { "Carnelian", ACE.Entity.Enum.MaterialType.Carnelian },
        { "Citrine", ACE.Entity.Enum.MaterialType.Citrine },
        { "Diamond", ACE.Entity.Enum.MaterialType.Diamond },
        { "Emerald", ACE.Entity.Enum.MaterialType.Emerald },
        { "Fire Opal", ACE.Entity.Enum.MaterialType.FireOpal },
        { "Green Garnet", ACE.Entity.Enum.MaterialType.GreenGarnet },
        { "Green Jade", ACE.Entity.Enum.MaterialType.GreenJade },
        { "Hematite", ACE.Entity.Enum.MaterialType.Hematite },
        { "Imperial Topaz", ACE.Entity.Enum.MaterialType.ImperialTopaz },
        { "Jet", ACE.Entity.Enum.MaterialType.Jet },
        { "Lapis Lazuli", ACE.Entity.Enum.MaterialType.LapisLazuli },
        { "Lavender Jade", ACE.Entity.Enum.MaterialType.LavenderJade },
        { "Malachite", ACE.Entity.Enum.MaterialType.Malachite },
        { "Moonstone", ACE.Entity.Enum.MaterialType.Moonstone },
        { "Onyx", ACE.Entity.Enum.MaterialType.Onyx },
        { "Opal", ACE.Entity.Enum.MaterialType.Opal },
        { "Peridot", ACE.Entity.Enum.MaterialType.Peridot },
        { "Red Garnet", ACE.Entity.Enum.MaterialType.RedGarnet },
        { "Red Jade", ACE.Entity.Enum.MaterialType.RedJade },
        { "Rose Quartz", ACE.Entity.Enum.MaterialType.RoseQuartz },
        { "Ruby", ACE.Entity.Enum.MaterialType.Ruby },
        { "Sapphire", ACE.Entity.Enum.MaterialType.Sapphire },
        { "Smokey Quartz", ACE.Entity.Enum.MaterialType.SmokeyQuartz },
        { "Sunstone", ACE.Entity.Enum.MaterialType.Sunstone },
        { "Tiger Eye", ACE.Entity.Enum.MaterialType.TigerEye },
        { "Tourmaline", ACE.Entity.Enum.MaterialType.Tourmaline },
        { "Turquoise", ACE.Entity.Enum.MaterialType.Turquoise },
        { "White Jade", ACE.Entity.Enum.MaterialType.WhiteJade },
        { "White Quartz", ACE.Entity.Enum.MaterialType.WhiteQuartz },
        { "White Sapphire", ACE.Entity.Enum.MaterialType.WhiteSapphire },
        { "Yellow Garnet", ACE.Entity.Enum.MaterialType.YellowGarnet },
        { "Yellow Topaz", ACE.Entity.Enum.MaterialType.YellowTopaz },
        { "Zircon", ACE.Entity.Enum.MaterialType.Zircon },
        { "Ivory", ACE.Entity.Enum.MaterialType.Ivory },
        { "Leather", ACE.Entity.Enum.MaterialType.Leather },
        { "Armoredillo Hide", ACE.Entity.Enum.MaterialType.ArmoredilloHide },
        { "Gromnie Hide", ACE.Entity.Enum.MaterialType.GromnieHide },
        { "Reed Shark Hide", ACE.Entity.Enum.MaterialType.ReedSharkHide },
        { "Metal", ACE.Entity.Enum.MaterialType.Metal },
        { "Brass", ACE.Entity.Enum.MaterialType.Brass },
        { "Bronze", ACE.Entity.Enum.MaterialType.Bronze },
        { "Copper", ACE.Entity.Enum.MaterialType.Copper },
        { "Gold", ACE.Entity.Enum.MaterialType.Gold },
        { "Iron", ACE.Entity.Enum.MaterialType.Iron },
        { "Pyreal", ACE.Entity.Enum.MaterialType.Pyreal },
        { "Silver", ACE.Entity.Enum.MaterialType.Silver },
        { "Steel", ACE.Entity.Enum.MaterialType.Steel },
        { "Stone", ACE.Entity.Enum.MaterialType.Stone },
        { "Alabaster", ACE.Entity.Enum.MaterialType.Alabaster },
        { "Granite", ACE.Entity.Enum.MaterialType.Granite },
        { "Marble", ACE.Entity.Enum.MaterialType.Marble },
        { "Obsidian", ACE.Entity.Enum.MaterialType.Obsidian },
        { "Sandstone", ACE.Entity.Enum.MaterialType.Sandstone },
        { "Serpentine", ACE.Entity.Enum.MaterialType.Serpentine },
        { "Wood", ACE.Entity.Enum.MaterialType.Wood },
        { "Ebony", ACE.Entity.Enum.MaterialType.Ebony },
        { "Mahogany", ACE.Entity.Enum.MaterialType.Mahogany },
        { "Oak", ACE.Entity.Enum.MaterialType.Oak },
        { "Pine", ACE.Entity.Enum.MaterialType.Pine },
        { "Teak", ACE.Entity.Enum.MaterialType.Teak }
    };

    private static Dictionary<MaterialType, (string, string, int, float, int, float)> JewelEffectInfo = new()
    {
        // neck
        { ACE.Entity.Enum.MaterialType.Sunstone, ("Illuminated Mind", "ring", 5, 0.25f, 0, 0.0f) },
        { ACE.Entity.Enum.MaterialType.Sapphire, ("Seeker", "ring", 5, 0.25f, 0, 0.0f) },
        { ACE.Entity.Enum.MaterialType.GreenJade, ("Prosperity", "ring", 5, 0.25f, 0, 0.0f) },

        // ring
        { ACE.Entity.Enum.MaterialType.Carnelian, ("Mighty Thews", "ring", 10, 1.0f, 0, 0.0f) },
        { ACE.Entity.Enum.MaterialType.Azurite, ("Erudite Mind", "ring", 10, 1.0f, 0, 0.0f) },
        { ACE.Entity.Enum.MaterialType.TigerEye, ("Dexterous Hand", "ring", 10, 1.0f, 0, 0.0f) },
        { ACE.Entity.Enum.MaterialType.RedJade, ("Focused Mind", "ring", 10, 1.0f, 0, 0.0f) },
        { ACE.Entity.Enum.MaterialType.YellowTopaz, ("Perserverence", "ring", 10, 1.0f, 0, 0.0f) },
        { ACE.Entity.Enum.MaterialType.Peridot, ("Swift-footed", "ring", 10, 1.0f, 0, 0.0f) },

        // bracelet
        { ACE.Entity.Enum.MaterialType.Agate, ("Provocation", "bracelet", 10, 0.5f, 0, 0.0f) },
        { ACE.Entity.Enum.MaterialType.Amber, ("Masochist", "bracelet", 10, 0.5f, 0, 0.0f) },
        { ACE.Entity.Enum.MaterialType.Citrine, ("Third Wind", "bracelet", 10, 0.5f, 0, 0.0f) },
        { ACE.Entity.Enum.MaterialType.LapisLazuli, ("Austere Anchorite", "bracelet", 10, 0.5f, 0, 0.0f) },
        { ACE.Entity.Enum.MaterialType.SmokeyQuartz, ("Clouded Vision", "bracelet", 10, 0.5f, 0, 0.0f) },
        { ACE.Entity.Enum.MaterialType.Malachite, ("Meticulous Magus", "bracelet", 20, 1.0f, 0, 0.0f) },
        { ACE.Entity.Enum.MaterialType.Moonstone, ("Thrifty Scholar", "bracelet", 20, 1.0f, 0, 0.0f) },
        { ACE.Entity.Enum.MaterialType.Onyx, ("Black Bulwark", "bracelet", 10, 0.5f, 0, 0.0f) },
        { ACE.Entity.Enum.MaterialType.Diamond, ("Hardened Fortification", "bracelet", 20, 1.0f, 0, 0.0f) },
        { ACE.Entity.Enum.MaterialType.Amethyst, ("Nullification", "bracelet", 20, 1.0f, 0, 0.0f) },
        { ACE.Entity.Enum.MaterialType.Zircon, ("Prismatic Ward", "bracelet", 10, 0.5f, 0, 0.0f) },

        // shield
        { ACE.Entity.Enum.MaterialType.Turquoise, ("Stalwart Defense", "shield", 10, 0.5f, 0, 0.0f) },
        { ACE.Entity.Enum.MaterialType.WhiteQuartz, ("Swift Retrbution", "shield", 10, 0.5f, 0, 0.0f) },

        // weapon
        { ACE.Entity.Enum.MaterialType.Jet, ("Astyrrian Rage", "weapon", 10, 0.5f, 2, 0.1f) },
        { ACE.Entity.Enum.MaterialType.RedGarnet, ("Blazing Brand", "weapon", 10, 0.5f, 2, 0.1f) },
        { ACE.Entity.Enum.MaterialType.Hematite, ("Blood Frenzy", "weapon", 10, 0.5f, 0, 0.0f) },
        { ACE.Entity.Enum.MaterialType.Aquamarine, ("Bone-chiller", "weapon", 10, 0.5f, 2, 0.1f) },
        { ACE.Entity.Enum.MaterialType.Emerald, ("Devouring Mist", "weapon", 10, 0.5f, 2, 0.1f) },
        { ACE.Entity.Enum.MaterialType.ImperialTopaz, ("Falcon's Gyre", "weapon", 10, 0.5f, 0, 0.0f) },
        { ACE.Entity.Enum.MaterialType.Opal, ("Ophidian", "weapon", 10, 0.5f, 0, 0.0f) },
        { ACE.Entity.Enum.MaterialType.BlackGarnet, ("Precision Strikes", "weapon", 20, 1.0f, 0, 0.0f) },
        { ACE.Entity.Enum.MaterialType.WhiteJade, ("Purified Soul", "weapon", 10, 0.5f, 2, 0.1f) },
        { ACE.Entity.Enum.MaterialType.Bloodstone, ("Sanguine Thirst", "weapon", 10, 0.5f, 0, 0.0f) },
        { ACE.Entity.Enum.MaterialType.WhiteSapphire, ("Skull-cracker", "weapon", 10, 0.5f, 0, 0.0f) },
        { ACE.Entity.Enum.MaterialType.RoseQuartz, ("Tilted-scales", "weapon", 20, 1.0f, 0, 0.0f) },

        // weapon or shield
        { ACE.Entity.Enum.MaterialType.YellowGarnet, ("Bravado", "weapon or shield", 20, 1.0f, 0, 0.0f) },
        { ACE.Entity.Enum.MaterialType.FireOpal, ("Familiar Foe", "weapon or shield", 20, 1.0f, 0, 0.0f) },
        { ACE.Entity.Enum.MaterialType.Ruby, ("Red Fury", "weapon or shield", 20, 1.0f, 0, 0.0f) },
        { ACE.Entity.Enum.MaterialType.BlackOpal, ("Vicious Reprisal", "weapon or shield", 5, 0.25f, 0, 0.0f) },

        // wand
        { ACE.Entity.Enum.MaterialType.GreenGarnet, ("Elementalist", "wand", 20, 1.0f, 0, 0.0f) },
        { ACE.Entity.Enum.MaterialType.Tourmaline, ("Ruthless Discernment", "wand", 20, 1.0f, 0, 0.0f) },
        { ACE.Entity.Enum.MaterialType.LavenderJade, ("Selfless Spirit", "wand", 10, 0.5f, 0, 0.0f) },
    };
}
