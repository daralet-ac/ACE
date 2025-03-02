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
    public static float GetJewelEffectMod(Player player, PropertyInt propertyInt, string rampQuestString = "", bool secondary = false)
    {
        if (player is null)
        {
            return 0.0f;
        }

        var rating = player.GetEquippedAndActivatedItemRatingSum(propertyInt);

        if (rating <= 0)
        {
            return 0.0f;
        }

        var jewelEffectInfo = JewelEffectInfo[propertyInt];
        var baseMod = (float)jewelEffectInfo.BasePrimary / 100;
        var bonusPerRating = jewelEffectInfo.BonusSecondary / 100;

        if (secondary)
        {
            baseMod = (float)jewelEffectInfo.BasePrimary / 100;
            bonusPerRating = jewelEffectInfo.BonusSecondary / 100;
        }

        var rampPercentage = rampQuestString is "" ? 1.0f : 0.0f;

        if (player.QuestManager.HasQuest($"{player.Name},{rampQuestString}"))
        {
            rampPercentage = Math.Min((float)player.QuestManager.GetCurrentSolves($"{player.Name},{rampQuestString}") / 100, 1.0f);
        }

        // Console.WriteLine($"\nJewel Mod:\n" +
        //                   $" -Property: {propertyInt}\n" +
        //                   $" -Rating: {rating}\n" +
        //                   $" -baseMod: {baseMod}\n" +
        //                   $" -bonusPerRating {bonusPerRating}\n" +
        //                   $" -bonus: {bonusPerRating * rating}\n" +
        //                   $" -rampPercentage: {rampPercentage * 100}%\n" +
        //                   $" -TOTAL: {(baseMod + bonusPerRating * rating) * rampPercentage}");

        return (baseMod + bonusPerRating * rating) * rampPercentage;
    }

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
        var scaledStamps = GetMeleeScaledStamps(playerAttacker);

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
        var scaledStamps = GetCasterScaledStamps(spell.Level, projectileSpellTyper);

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

    private static int GetMeleeScaledStamps(Player playerAttacker)
    {
        const int baseStamps = 10;

        var powerBarScalar = playerAttacker.GetPowerAccuracyBar() * 2;

        var equippedWeapon = playerAttacker.GetEquippedMeleeWeapon();
        var weaponAnimationLength = WeaponAnimationLength.GetWeaponAnimLength(equippedWeapon);
        var weaponTime = equippedWeapon.WeaponTime ?? 100;
        var attacksPerSecondScalar = 1 / (weaponAnimationLength / (1.0f + (1 - (weaponTime / 100.0))));

        return Convert.ToInt32(baseStamps * powerBarScalar * attacksPerSecondScalar);
    }

    private static int GetCasterScaledStamps(uint spellLevel, ProjectileSpellType projectileSpellType)
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

    public static float HandleElementalBonuses(Player playerAttacker, DamageType damageType)
    {
        var jewelElemental = 1.0f;

        if (playerAttacker == null)
        {
            return jewelElemental;
        }

        jewelElemental = damageType switch
        {
            DamageType.Acid => 1.0f + GetJewelEffectMod(playerAttacker, PropertyInt.GearAcid),
            DamageType.Fire => 1.0f + GetJewelEffectMod(playerAttacker, PropertyInt.GearFire),
            DamageType.Cold => 1.0f + GetJewelEffectMod(playerAttacker, PropertyInt.GearFrost),
            DamageType.Electric => 1.0f + GetJewelEffectMod(playerAttacker, PropertyInt.GearLightning),
            _ => jewelElemental
        };

        return jewelElemental;
    }

    /// <summary>
    /// RATING - Pyreal Find: Adds quest stamps for prosperity find.
    /// (JEWEL - Green Jade)
    /// </summary>
    private static void CheckForRatingProsperityFindStamps(Player playerAttacker, Creature defender, float damage)
    {
        var pyrealFind = GetJewelEffectMod(playerAttacker, PropertyInt.GearPyrealFind);

        defender.QuestManager.Stamp($"{playerAttacker.Name}/Prosperity/{pyrealFind}/{damage}");
    }

    /// <summary>
    /// RATING - Magic Find: Adds quest stamps for magic find.
    /// (JEWEL - Sapphire)
    /// </summary>
    private static void CheckForRatingMagicFindStamps(Player playerAttacker, Creature defender, float damage)
    {
        var magicFind = GetJewelEffectMod(playerAttacker, PropertyInt.GearMagicFind);

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
    /// RATING - Self Harm: 10% chance to damage self when dealing damage. (Also grants bonus damage from a different function)
    /// (JEWEL - Hematite)
    /// </summary>
    private static void CheckForRatingSelfHarm(Player playerAttacker, float damage)
    {
        const float selfHarmChance = 0.1f;

        if (ThreadSafeRandom.Next(0.0f, 1.0f) > selfHarmChance)
        {
            return;
        }

        var selfHarmMod = GetJewelEffectMod(playerAttacker, PropertyInt.GearSelfHarm);
        var selfHarmAmount = Convert.ToInt32(damage * selfHarmMod);

        playerAttacker.UpdateVitalDelta(playerAttacker.Health, -selfHarmAmount);
        playerAttacker.DamageHistory.Add(playerAttacker, DamageType.Health, (uint)selfHarmAmount);

        var message = $"In a blood frenzy, you deal {selfHarmAmount} damage to yourself!";
        playerAttacker.Session.Network.EnqueueSend(new GameMessageSystemChat(message, ChatMessageType.CombatEnemy));

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
        var chance = GetJewelEffectMod(playerAttacker, PropertyInt.GearManasteal);

        if (playerAttacker == defender || ThreadSafeRandom.Next(0.0f, 1.0f) > chance)
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
        var chance = GetJewelEffectMod(playerAttacker, PropertyInt.GearLifesteal);

        if (playerAttacker == defender || ThreadSafeRandom.Next(0.0f, 1.0f) > chance)
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
        var chance = GetJewelEffectMod(playerDefender, PropertyInt.GearHealthToMana);

        if (playerDefender == attacker || ThreadSafeRandom.Next(0.0f, 1.0f) > chance)
        {
            return;
        }

        var manaAmount = Convert.ToUInt32(damage);
        playerDefender.UpdateVitalDelta(playerDefender.Mana, manaAmount);
        playerDefender.DamageHistory.OnHeal(manaAmount);
        playerDefender.Session.Network.EnqueueSend(
            new GameMessageSystemChat(
                $"Austere Anchorite restores {manaAmount} points of Mana!",
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
        var chance = GetJewelEffectMod(playerDefender, PropertyInt.GearHealthToStamina);

        if (playerDefender == attacker || ThreadSafeRandom.Next(0.0f, 1.0f) > chance)
        {
            return;
        }

        var staminaAmount = Convert.ToUInt32(damage);

        playerDefender.UpdateVitalDelta(playerDefender.Stamina, staminaAmount);
        playerDefender.DamageHistory.OnHeal(staminaAmount);
        playerDefender.Session.Network.EnqueueSend(
            new GameMessageSystemChat(
                $"Masochist restores {staminaAmount} points of Stamina!",
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
        if (playerAttacker is null)
        {
            return 0.0f;
        }

        if (playerAttacker.GetEquippedAndActivatedItemRatingSum(PropertyInt.GearRedFury) <= 0)
        {
            return 0.0f;
        }

        var percentHealthRemaining = (float)playerAttacker.Health.Current / playerAttacker.Health.MaxValue;
        var inverseHealthRemaining = Math.Min((1.0f - percentHealthRemaining), 1.0f);
        var ratingMod = GetJewelEffectMod(playerAttacker, PropertyInt.GearRedFury);

        return inverseHealthRemaining * ratingMod;
    }

    /// <summary>
    /// RATING - Yellow Fury: .
    /// (JEWEL - ??)
    /// </summary>
    public static float GetJewelYellowFury(Player playerAttacker)
    {
        if (playerAttacker is null)
        {
            return 0.0f;
        }

        if (playerAttacker.GetEquippedAndActivatedItemRatingSum(PropertyInt.GearYellowFury) <= 0)
        {
            return 0.0f;
        }

        var percentStaminaRemaining = (float)playerAttacker.Stamina.Current / playerAttacker.Stamina.MaxValue;
        var inverseHealthRemaining = Math.Min((1.0f - percentStaminaRemaining), 1.0f);
        var ratingMod = GetJewelEffectMod(playerAttacker, PropertyInt.GearYellowFury);

        return inverseHealthRemaining * ratingMod;
    }

    /// <summary>
    /// RATING - Blue Fury: .
    /// (JEWEL - ??)
    /// </summary>
    public static float GetJewelBlueFury(Player playerAttacker)
    {
        if (playerAttacker is null)
        {
            return 0.0f;
        }

        if (playerAttacker.GetEquippedAndActivatedItemRatingSum(PropertyInt.GearBlueFury) <= 0)
        {
            return 0.0f;
        }

        var percentManaRemaining = (float)playerAttacker.Mana.Current / playerAttacker.Mana.MaxValue;
        var inverseHealthRemaining = Math.Min((1.0f - percentManaRemaining), 1.0f);
        var ratingMod = GetJewelEffectMod(playerAttacker, PropertyInt.GearBlueFury);

        return inverseHealthRemaining * ratingMod;
    }

    // 0 - prepended quality, 1 - gemstone type, 2 - appended name, 3 - property type, 4 - amount of property, 5 - original gem workmanship
    public static string GetJewelDescription(WorldObject jewel)
    {
        var quality = jewel.JewelSocket1Quality ?? 1;
        var materialType = jewel.JewelMaterialType;

        if (materialType is null)
        {
            return "";
        }

        var propertyInt = JewelMaterialToType[materialType.Value];

        var name = JewelEffectInfo[propertyInt].Name;
        var equipmentType = JewelEffectInfo[propertyInt].Slot;
        var baseRating = JewelEffectInfo[propertyInt].BasePrimary;
        var bonusPerQuality = JewelEffectInfo[propertyInt].BonusPrimary;
        var baseRatingSecondary = JewelEffectInfo[propertyInt].BaseSecondary;
        var bonusPerQualitySecondary = JewelEffectInfo[propertyInt].BonusSecondary;

        var description = $"Socket this jewel in a {equipmentType} of workmanship {quality} or greater to gain {name}, while equipped.\n\n";

        switch (materialType)
        {
            //necklace
            case ACE.Entity.Enum.MaterialType.Sunstone:
                description +=
                    $"~ {name}: Gain {baseRating}% increased experience from monster kills (+{bonusPerQuality}% per equipped rating).\n\n" +
                    $"{JewelStatsDescription(baseRating, quality, bonusPerQuality, name)}\n\n";
                break;
            case ACE.Entity.Enum.MaterialType.Sapphire:
                description +=
                    $"~ {name}: Gain a {baseRating}% bonus to loot quality from monster kills (+{bonusPerQuality}% per equipped rating).\n\n" +
                    $"{JewelStatsDescription(baseRating, quality, bonusPerQuality, name)}\n\n";
                break;
            case ACE.Entity.Enum.MaterialType.GreenJade:
                description +=
                    $"~ {name}: Gain a {baseRating}% chance to receive an extra item from monster kills (+{bonusPerQuality}% per equipped rating).\n\n" +
                    $"{JewelStatsDescription(baseRating, quality, bonusPerQuality, name)}\n\n";
                break;

            // ring right
            case ACE.Entity.Enum.MaterialType.Carnelian:
                description +=
                    $"~ {name}: Gain {baseRating} Strength (+{bonusPerQuality}% per equipped rating). " +
                    $"Once socketed, the {equipmentType} can only be worn on the right finger.\n\n" +
                    $"{JewelStatsDescription(baseRating, quality, bonusPerQuality, name)}\n\n";
                break;
            case ACE.Entity.Enum.MaterialType.Azurite:
                description +=
                    $"~ {name}: Gain {baseRating} Self (+{bonusPerQuality}% per equipped rating). " +
                    $"Once socketed, the {equipmentType} can only be worn on the right finger.\n\n" +
                    $"{JewelStatsDescription(baseRating, quality, bonusPerQuality, name)}\n\n";
                break;
            case ACE.Entity.Enum.MaterialType.TigerEye:
                description +=
                    $"~ {name}: Gain {baseRating} Coordination (+{bonusPerQuality}% per equipped rating). " +
                    $"Once socketed, the {equipmentType} can only be worn on the right finger.\n\n" +
                    $"{JewelStatsDescription(baseRating, quality, bonusPerQuality, name)}\n\n";
                break;

            // ring left
            case ACE.Entity.Enum.MaterialType.RedJade:
                description +=
                    $"~ {name}: Gain {baseRating} Focus (+{bonusPerQuality}% per equipped rating). " +
                    $"Once socketed, the {equipmentType} can only be worn on the left finger.\n\n" +
                    $"{JewelStatsDescription(baseRating, quality, bonusPerQuality, name)}\n\n";
                break;
            case ACE.Entity.Enum.MaterialType.YellowTopaz:
                description +=
                    $"~ {name}: Gain {baseRating} Endurance (+{bonusPerQuality}% per equipped rating). " +
                    $"Once socketed, the {equipmentType} can only be worn on the left finger.\n\n" +
                    $"{JewelStatsDescription(baseRating, quality, bonusPerQuality, name)}\n\n";
                break;
            case ACE.Entity.Enum.MaterialType.Peridot:
                description +=
                    $"~ {name}: Gain {baseRating} Quickness (+{bonusPerQuality}% per equipped rating). " +
                    $"Once socketed, the {equipmentType} can only be worn on the left finger.\n\n" +
                    $"{JewelStatsDescription(baseRating, quality, bonusPerQuality, name)}\n\n";
                break;

            // bracelet left
            case ACE.Entity.Enum.MaterialType.Agate:
                description +=
                    $"~ {name}: Gain {baseRating} increased threat from your actions (+{bonusPerQuality}% per equipped rating). " +
                    $"Once socketed, the bracelet can only be worn on the left wrist.\n\n" +
                    $"{JewelStatsDescription(baseRating, quality, bonusPerQuality, name)}\n\n";
                break;
            case ACE.Entity.Enum.MaterialType.SmokeyQuartz:
                description +=
                    $"~ {name}: Gain {baseRating} reduced threat from your actions (+{bonusPerQuality}% per equipped rating). " +
                    $"Once socketed, the bracelet can only be worn on the left wrist.\n\n" +
                    $"{JewelStatsDescription(baseRating, quality, bonusPerQuality, name)}\n\n";
                break;
            case ACE.Entity.Enum.MaterialType.Amber:
                description +=
                    $"~ {name}: Gain a {baseRating}% chance to gain hit damage taken as stamina (+{bonusPerQuality}% per equipped rating). " +
                    $"Once socketed, the bracelet can only be worn on the left wrist.\n\n" +
                    $"{JewelStatsDescription(baseRating, quality, bonusPerQuality, name)}\n\n";
                break;
            case ACE.Entity.Enum.MaterialType.LapisLazuli:
                description +=
                    $"~ {name}: Gain a {baseRating}% chance to gain hit damage taken as mana (+{bonusPerQuality}% per equipped rating). " +
                    $"Once socketed, the bracelet can only be worn on the left wrist.\n\n" +
                    $"{JewelStatsDescription(baseRating, quality, bonusPerQuality, name)}\n\n";
                break;
            case ACE.Entity.Enum.MaterialType.Moonstone:
                description +=
                    $"~ {name}: Gain {baseRating}% reduced mana consumed by items (+{bonusPerQuality}% per equipped rating). " +
                    $"Once socketed, the bracelet can only be worn on the left wrist.\n\n" +
                    $"{JewelStatsDescription(baseRating, quality, bonusPerQuality, name)}\n\n";
                break;
            case ACE.Entity.Enum.MaterialType.Malachite:
                description +=
                    $"~ {name}: Gain {baseRating}% reduced chance to burn spell components (+{bonusPerQuality}% per equipped rating). " +
                    $"Once socketed, the bracelet can only be worn on the left wrist.\n\n" +
                    $"{JewelStatsDescription(baseRating, quality, bonusPerQuality, name)}\n\n";
                break;
            case ACE.Entity.Enum.MaterialType.Citrine:
                description +=
                    $"~ {name}: Gain {baseRating}% reduced stamina costs (+{bonusPerQuality}% per equipped rating). " +
                    $"Once socketed, the bracelet can only be worn on the left wrist.\n\n" +
                    $"{JewelStatsDescription(baseRating, quality, bonusPerQuality, name)}\n\n";
                break;

            // bracelet right
            case ACE.Entity.Enum.MaterialType.Amethyst:
                description +=
                    $"~ {name}: Gain up to {baseRating}% reduced magic damage taken (+{bonusPerQuality}% per equipped rating). " +
                    $"The amount builds up from 0%, based on how often you have recently been hit with a damaging spell. " +
                    $"Once socketed, the bracelet can only be worn on the right wrist.\n\n" +
                    $"{JewelStatsDescription(baseRating, quality, bonusPerQuality, name)}\n\n";
                break;
            case ACE.Entity.Enum.MaterialType.Diamond:
                description +=
                    $"~ {name}: Gain up to {baseRating}% reduced physical damage taken (+{bonusPerQuality}% per equipped rating). " +
                    $"The amount builds up from 0%, based on how often you have recently been hit with a damaging physical attack. " +
                    $"Once socketed, the bracelet can only be worn on the right wrist.\n\n" +
                    $"{JewelStatsDescription(baseRating, quality, bonusPerQuality, name)}\n\n";
                break;
            case ACE.Entity.Enum.MaterialType.Onyx:
                description +=
                    $"~ {name}: Gain {baseRating}% reduced damage taken from slashing, bludgeoning, and piercing damage types (+{bonusPerQuality}% per equipped rating). " +
                    $"Once socketed, the bracelet can only be worn on the right wrist.\n\n" +
                    $"{JewelStatsDescription(baseRating, quality, bonusPerQuality, name)}\n\n";
                break;
            case ACE.Entity.Enum.MaterialType.Zircon:
                description +=
                    $"~ {name}: Gain {baseRating}% reduced damage taken from acid, fire, cold, and electric damage types (+{bonusPerQuality}% per equipped rating). " +
                    $"Once socketed, the bracelet can only be worn on the right wrist.\n\n" +
                    $"{JewelStatsDescription(baseRating, quality, bonusPerQuality, name)}\n\n";
                break;

            // shield
            case ACE.Entity.Enum.MaterialType.Turquoise:
                description +=
                    $"~ {name}: Gain {baseRating}% increased block chance (+{bonusPerQuality}% per equipped rating).\n\n" +
                    $"{JewelStatsDescription(baseRating, quality, bonusPerQuality, name)}\n\n";
                break;
            case ACE.Entity.Enum.MaterialType.WhiteQuartz:
                description +=
                    $"~ {name}: Deflect {baseRating}% damage from a blocked attack back to a close-range attacker (+{bonusPerQuality}% per equipped rating).\n\n" +
                    $"{JewelStatsDescription(baseRating, quality, bonusPerQuality, name)}\n\n";
                break;

            // weapon + shield
            case ACE.Entity.Enum.MaterialType.BlackOpal:
                description +=
                    $"~ {name}: Gain a {baseRating}% chance to evade a critical attack (+{bonusPerQuality}% per equipped rating).  " +
                    $"Your next attack after a the evade is a guaranteed critical.\n\n" +
                    $"{JewelStatsDescription(baseRating, quality, bonusPerQuality, name)}\n\n";
                break;
            case ACE.Entity.Enum.MaterialType.FireOpal:
                description +=
                    $"~ {name}: Gain up to {baseRating}% increased evade and resist chances, against the target you are attacking (+{bonusPerQuality}% per equipped rating). " +
                    $"The amount builds up from 0%, based on how often you have recently hit the target.\n\n" +
                    $"{JewelStatsDescription(baseRating, quality, bonusPerQuality, name)}\n\n";
                break;
            case ACE.Entity.Enum.MaterialType.YellowGarnet:
                description +=
                    $"~ {name}: Gain up to {baseRating}% increased physical attack skill (+{bonusPerQuality}% per equipped rating). " +
                    $"The amount builds up from 0%, based on how often you have recently hit the target.\n\n" +
                    $"{JewelStatsDescription(baseRating, quality, bonusPerQuality, name)}\n\n";
                break;
            case ACE.Entity.Enum.MaterialType.Ruby:
                description +=
                    $"~ {name}: Gain up to {baseRating}% increased damage as your health approaches 0 (+{bonusPerQuality}% per equipped rating).\n\n" +
                    $"{JewelStatsDescription(baseRating, quality, bonusPerQuality, name)}\n\n";
                break;

            // weapon only
            case ACE.Entity.Enum.MaterialType.Bloodstone:
                description +=
                    $"~ {name}: Gain a {baseRating}% chance on hit to gain health (+{bonusPerQuality}% per equipped rating). " +
                    $"Amount stolen is equal to 10% of damage dealt.\n\n" +
                    $"{JewelStatsDescription(baseRating, quality, bonusPerQuality, name)}\n\n";
                break;
            case ACE.Entity.Enum.MaterialType.Opal:
                description +=
                    $"~ {name}: Gain a {baseRating}% chance on hit to gain mana (+{bonusPerQuality}% per equipped rating). " +
                    $"Amount stolen is equal to 10% of damage dealt.\n\n" +
                    $"{JewelStatsDescription(baseRating, quality, bonusPerQuality, name)}\n\n";
                break;
            case ACE.Entity.Enum.MaterialType.Hematite:
                description +=
                    $"~ {name}: Gain {baseRating}% increased damage with all attacks (+{bonusPerQuality}% per equipped rating). " +
                    $"However, 10% of your attacks will deal the extra damage to yourself as well.\n\n" +
                    $"{JewelStatsDescription(baseRating, quality, bonusPerQuality, name)}\n\n";
                break;
            case ACE.Entity.Enum.MaterialType.RoseQuartz:
                description +=
                    $"~ {name}: Gain a {baseRating}% bonus to your transfer spells (+{bonusPerQuality}% per equipped rating). " +
                    $"Receive an equivalent reduction in the effectiveness of your other restoration spells.\n\n" +
                    $"{JewelStatsDescription(baseRating, quality, bonusPerQuality, name)}\n\n";
                break;
            case ACE.Entity.Enum.MaterialType.LavenderJade:
                description +=
                    $"~ {name}: Gain a {baseRating}% bonus to your restoration spells on others (+{bonusPerQuality}% per equipped rating). " +
                    $"Receive an equivalent reduction in the effectiveness when cast on yourself.\n\n" +
                    $"{JewelStatsDescription(baseRating, quality, bonusPerQuality, name)}\n\n";
                break;
            case ACE.Entity.Enum.MaterialType.GreenGarnet:
                description +=
                    $"~ {name}: Gain up to {baseRating}% increased war magic damage (+{bonusPerQuality}% per equipped rating). " +
                    $"The amount builds up from 0%, based on how often you have recently hit the target.\n\n" +
                    $"{JewelStatsDescription(baseRating, quality, bonusPerQuality, name)}\n\n";
                break;
            case ACE.Entity.Enum.MaterialType.Tourmaline:
                description +=
                    $"~ {name}: Gain up to {baseRating}% ward cleaving (+{bonusPerQuality}% per equipped rating). " +
                    $"The amount builds up from 0%, based on how often you have recently hit the target.\n\n" +
                    $"{JewelStatsDescription(baseRating, quality, bonusPerQuality, name)}\n\n";
                break;
            case ACE.Entity.Enum.MaterialType.WhiteJade:
                description +=
                    $"~ {name}: Gain a {baseRating}% bonus to your restoration spells (+{bonusPerQuality}% per equipped rating). " +
                    $"Also grants a {baseRatingSecondary}% chance to create a sphere of healing energy on top of your target when casting a restoration spell (+{bonusPerQualitySecondary}% per equipped rating).\n\n" +
                    $"{JewelStatsDescription(baseRating, quality, bonusPerQuality, name)}\n\n";
                break;
            case ACE.Entity.Enum.MaterialType.Aquamarine:
                description +=
                    $"~ {name}: Gain {baseRating}% increased cold damage (+{bonusPerQuality}% per equipped rating). " +
                    $"Also grants a {baseRatingSecondary}% chance to surround your target with chilling mist (+{bonusPerQualitySecondary}% per equipped rating).\n\n" +
                    $"{JewelStatsDescription(baseRating, quality, bonusPerQuality, name)}\n\n";
                break;
            case ACE.Entity.Enum.MaterialType.BlackGarnet:
                description +=
                    $"~ {name}: Gain {baseRating}% piercing resistance penetration (+{bonusPerQuality}% per equipped rating). " +
                    $"The amount builds up from 0%, based on how often you have recently hit the target.\n\n" +
                    $"{JewelStatsDescription(baseRating, quality, bonusPerQuality, name)}\n\n";
                break;
            case ACE.Entity.Enum.MaterialType.Emerald:
                description +=
                    $"~ {name}: Gain {baseRating}% increased acid damage (+{bonusPerQuality}% per equipped rating). " +
                    $"Also grants a {baseRatingSecondary}% chance to surround your target with acidic mist (+{bonusPerQualitySecondary}% per equipped rating).\n\n" +
                    $"{JewelStatsDescription(baseRating, quality, bonusPerQuality, name)}\n\n";
                break;
            case ACE.Entity.Enum.MaterialType.ImperialTopaz:
                description +=
                    $"~ {name}: Gain a {baseRating}% chance to cleave an additional target (+{bonusPerQuality}% per equipped rating).\n\n" +
                    $"{JewelStatsDescription(baseRating, quality, bonusPerQuality, name)}\n\n";
                break;
            case ACE.Entity.Enum.MaterialType.Jet:
                description +=
                    $"~ {name}: Gain {baseRating}% increased electric damage (+{bonusPerQuality}% per equipped rating). " +
                    $"Also grants a {baseRatingSecondary}% chance to electrify the ground beneath your target (+{bonusPerQualitySecondary}% per equipped rating).\n\n" +
                    $"{JewelStatsDescription(baseRating, quality, bonusPerQuality, name)}\n\n";
                break;
            case ACE.Entity.Enum.MaterialType.RedGarnet:
                description +=
                    $"~ {name}: Gain {baseRating}% increased fire damage (+{bonusPerQuality}% per equipped rating). " +
                    $"Also grants a {baseRatingSecondary}% chance to set the ground beneath your target ablaze (+{bonusPerQualitySecondary}% per equipped rating).\n\n" +
                    $"{JewelStatsDescription(baseRating, quality, bonusPerQuality, name, bonusPerQualitySecondary)}\n\n";
                break;
            case ACE.Entity.Enum.MaterialType.WhiteSapphire:
                description +=
                    $"~ {name}: Gain {baseRating}% bludgeon critical damage (+{bonusPerQuality}% per equipped rating). " +
                    $"The amount builds up from 0%, based on how often you have recently hit the target.\n\n" +
                    $"{JewelStatsDescription(baseRating, quality, bonusPerQuality, name)}\n\n";
                break;
        }

        return description;
    }

    private static string JewelStatsDescription(int baseRating, int amount, float bonusPerQuality, string name, float? bonusPerQualitySecondary = null)
    {
        var secondaryBonus = bonusPerQualitySecondary != null
            ? $"\nSecondary Bonus Rating: {bonusPerQualitySecondary * amount} ({bonusPerQualitySecondary} x Quality)"
            : "";

        return //$"Base Rating: {baseRating}\n" +
            $"Quality: {amount} ({JewelQuality[amount]})\n" +
            $"Bonus Rating: {bonusPerQuality * amount} ({bonusPerQuality} x Quality)" +
            $"{secondaryBonus}" +
        //$"Total Rating: {baseRating + bonusPerQuality * amount}\n\n" +
        $"\n\nAdditional sources of {name} will only add the bonus rating.";
    }

    public static string GetSocketDescription(MaterialType materialType, int quality)
    {
        var materialString = Jewel.MaterialTypeToString[materialType];

        return $"\n\t Socket: {materialString} ({quality})\n";
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

    public static readonly Dictionary<int, string> JewelQuality = new()
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

    private static readonly Dictionary<MaterialType, uint> GemstoneIconMap = new()
    {
        { ACE.Entity.Enum.MaterialType.Agate, 0x06002CAD },
        {ACE.Entity.Enum.MaterialType.Amber, 0x06002CAE },
        {ACE.Entity.Enum.MaterialType.Amethyst, 0x06002CAF },
        {ACE.Entity.Enum.MaterialType.Aquamarine, 0x06002CB0 },
        {ACE.Entity.Enum.MaterialType.Azurite, 0x06002CB1 },
        {ACE.Entity.Enum.MaterialType.BlackGarnet, 0x06002CB2 },
        {ACE.Entity.Enum.MaterialType.BlackOpal, 0x06002CB3 },
        {ACE.Entity.Enum.MaterialType.Bloodstone, 0x06002CA7 },
        {ACE.Entity.Enum.MaterialType.Carnelian, 0x06002CA8 },
        {ACE.Entity.Enum.MaterialType.Citrine, 0x06002CA9 },
        {ACE.Entity.Enum.MaterialType.Diamond, 0x06002CAA },
        {ACE.Entity.Enum.MaterialType.Emerald, 0x06002CAB },
        {ACE.Entity.Enum.MaterialType.FireOpal, 0x06002CAC },
        {ACE.Entity.Enum.MaterialType.GreenGarnet, 0x06002CB4 },
        {ACE.Entity.Enum.MaterialType.GreenJade, 0x06002CB5 },
        {ACE.Entity.Enum.MaterialType.Hematite, 0x06002CB6 },
        {ACE.Entity.Enum.MaterialType.ImperialTopaz, 0x06002CB7 },
        {ACE.Entity.Enum.MaterialType.Jet, 0x06002CB8 },
        {ACE.Entity.Enum.MaterialType.LapisLazuli, 0x06002CB9 },
        {ACE.Entity.Enum.MaterialType.LavenderJade, 0x06002CBA },
        {ACE.Entity.Enum.MaterialType.Malachite, 0x06002CBB },
        {ACE.Entity.Enum.MaterialType.Moonstone, 0x06002CBC },
        {ACE.Entity.Enum.MaterialType.Onyx, 0x06002CBD },
        {ACE.Entity.Enum.MaterialType.Opal, 0x06002CBE },
        {ACE.Entity.Enum.MaterialType.Peridot, 0x06002CBF },
        {ACE.Entity.Enum.MaterialType.RedGarnet, 0x06002CC0 },
        {ACE.Entity.Enum.MaterialType.RedJade, 0x06002C98 },
        {ACE.Entity.Enum.MaterialType.RoseQuartz, 0x06002C99 },
        {ACE.Entity.Enum.MaterialType.Ruby, 0x06002C9A },
        {ACE.Entity.Enum.MaterialType.Sapphire, 0x06002C9B },
        {ACE.Entity.Enum.MaterialType.SmokeyQuartz, 0x06002C9C },
        {ACE.Entity.Enum.MaterialType.Sunstone, 0x06002C9D },
        {ACE.Entity.Enum.MaterialType.TigerEye, 0x06002C9E },
        {ACE.Entity.Enum.MaterialType.Tourmaline, 0x06002C9F },
        {ACE.Entity.Enum.MaterialType.Turquoise, 0x06002CA0 },
        {ACE.Entity.Enum.MaterialType.WhiteJade, 0x06002CA1 },
        {ACE.Entity.Enum.MaterialType.WhiteQuartz, 0x06002CA2 },
        {ACE.Entity.Enum.MaterialType.WhiteSapphire, 0x06002CA3 },
        {ACE.Entity.Enum.MaterialType.YellowGarnet, 0x06002CA4 },
        {ACE.Entity.Enum.MaterialType.YellowTopaz, 0x06002CA5 },
        {ACE.Entity.Enum.MaterialType.Zircon, 0x06002CA6 }
    };

    public static readonly Dictionary<MaterialType, string> MaterialTypeToString = new()
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

    public static readonly Dictionary<PropertyInt, MaterialType> JewelTypeToMaterial = new()
    {
        { PropertyInt.GearThreatGain, ACE.Entity.Enum.MaterialType.Agate },
        { PropertyInt.GearHealthToStamina, ACE.Entity.Enum.MaterialType.Amber },
        { PropertyInt.GearNullification, ACE.Entity.Enum.MaterialType.Amethyst },
        { PropertyInt.GearFrost, ACE.Entity.Enum.MaterialType.Aquamarine },
        { PropertyInt.GearSelf, ACE.Entity.Enum.MaterialType.Azurite },
        { PropertyInt.GearPierce, ACE.Entity.Enum.MaterialType.BlackGarnet },
        { PropertyInt.GearReprisal, ACE.Entity.Enum.MaterialType.BlackOpal },
        { PropertyInt.GearLifesteal, ACE.Entity.Enum.MaterialType.Bloodstone },
        { PropertyInt.GearStrength, ACE.Entity.Enum.MaterialType.Carnelian },
        { PropertyInt.GearStamReduction, ACE.Entity.Enum.MaterialType.Citrine },
        { PropertyInt.GearHardenedDefense, ACE.Entity.Enum.MaterialType.Diamond },
        { PropertyInt.GearAcid, ACE.Entity.Enum.MaterialType.Emerald },
        { PropertyInt.GearFamiliarity, ACE.Entity.Enum.MaterialType.FireOpal },
        { PropertyInt.GearElementalist, ACE.Entity.Enum.MaterialType.GreenGarnet },
        { PropertyInt.GearPyrealFind, ACE.Entity.Enum.MaterialType.GreenJade },
        { PropertyInt.GearSelfHarm, ACE.Entity.Enum.MaterialType.Hematite },
        { PropertyInt.GearSlash, ACE.Entity.Enum.MaterialType.ImperialTopaz },
        { PropertyInt.GearLightning, ACE.Entity.Enum.MaterialType.Jet },
        { PropertyInt.GearHealthToMana, ACE.Entity.Enum.MaterialType.LapisLazuli },
        { PropertyInt.GearSelflessness, ACE.Entity.Enum.MaterialType.LavenderJade },
        { PropertyInt.GearCompBurn, ACE.Entity.Enum.MaterialType.Malachite },
        { PropertyInt.GearItemManaUsage, ACE.Entity.Enum.MaterialType.Moonstone },
        { PropertyInt.GearPhysicalWard, ACE.Entity.Enum.MaterialType.Onyx },
        { PropertyInt.GearManasteal, ACE.Entity.Enum.MaterialType.Opal },
        { PropertyInt.GearQuickness, ACE.Entity.Enum.MaterialType.Peridot },
        { PropertyInt.GearFire, ACE.Entity.Enum.MaterialType.RedGarnet },
        { PropertyInt.GearFocus, ACE.Entity.Enum.MaterialType.RedJade },
        { PropertyInt.GearVitalsTransfer, ACE.Entity.Enum.MaterialType.RoseQuartz },
        { PropertyInt.GearRedFury, ACE.Entity.Enum.MaterialType.Ruby },
        { PropertyInt.GearMagicFind, ACE.Entity.Enum.MaterialType.Sapphire },
        { PropertyInt.GearThreatReduction, ACE.Entity.Enum.MaterialType.SmokeyQuartz },
        { PropertyInt.GearExperienceGain, ACE.Entity.Enum.MaterialType.Sunstone },
        { PropertyInt.GearCoordination, ACE.Entity.Enum.MaterialType.TigerEye },
        { PropertyInt.GearWardPen, ACE.Entity.Enum.MaterialType.Tourmaline },
        { PropertyInt.GearBlock, ACE.Entity.Enum.MaterialType.Turquoise },
        { PropertyInt.GearHealBubble, ACE.Entity.Enum.MaterialType.WhiteJade },
        { PropertyInt.GearThorns, ACE.Entity.Enum.MaterialType.WhiteQuartz },
        { PropertyInt.GearBludgeon, ACE.Entity.Enum.MaterialType.WhiteSapphire },
        { PropertyInt.GearBravado, ACE.Entity.Enum.MaterialType.YellowGarnet },
        { PropertyInt.GearEndurance, ACE.Entity.Enum.MaterialType.YellowTopaz },
        { PropertyInt.GearElementalWard, ACE.Entity.Enum.MaterialType.Zircon },
    };

    private static readonly Dictionary<MaterialType, PropertyInt> JewelMaterialToType = new()
    {
        { ACE.Entity.Enum.MaterialType.Agate, PropertyInt.GearThreatGain },
        { ACE.Entity.Enum.MaterialType.Amber, PropertyInt.GearHealthToStamina },
        { ACE.Entity.Enum.MaterialType.Amethyst, PropertyInt.GearNullification },
        { ACE.Entity.Enum.MaterialType.Aquamarine, PropertyInt.GearFrost },
        { ACE.Entity.Enum.MaterialType.Azurite, PropertyInt.GearSelf },
        { ACE.Entity.Enum.MaterialType.BlackGarnet, PropertyInt.GearPierce },
        { ACE.Entity.Enum.MaterialType.BlackOpal, PropertyInt.GearReprisal },
        { ACE.Entity.Enum.MaterialType.Bloodstone, PropertyInt.GearSelfHarm },
        { ACE.Entity.Enum.MaterialType.Carnelian, PropertyInt.GearStrength },
        { ACE.Entity.Enum.MaterialType.Citrine, PropertyInt.GearStamReduction },
        { ACE.Entity.Enum.MaterialType.Diamond, PropertyInt.GearHardenedDefense },
        { ACE.Entity.Enum.MaterialType.Emerald, PropertyInt.GearAcid },
        { ACE.Entity.Enum.MaterialType.FireOpal, PropertyInt.GearFamiliarity },
        { ACE.Entity.Enum.MaterialType.GreenGarnet, PropertyInt.GearElementalist },
        { ACE.Entity.Enum.MaterialType.GreenJade, PropertyInt.GearPyrealFind },
        { ACE.Entity.Enum.MaterialType.Hematite, PropertyInt.GearSelfHarm },
        { ACE.Entity.Enum.MaterialType.ImperialTopaz, PropertyInt.GearPierce },
        { ACE.Entity.Enum.MaterialType.Jet, PropertyInt.GearLightning },
        { ACE.Entity.Enum.MaterialType.LapisLazuli, PropertyInt.GearHealthToMana },
        { ACE.Entity.Enum.MaterialType.LavenderJade, PropertyInt.GearSelflessness },
        { ACE.Entity.Enum.MaterialType.Malachite, PropertyInt.GearCompBurn },
        { ACE.Entity.Enum.MaterialType.Moonstone, PropertyInt.GearItemManaUsage },
        { ACE.Entity.Enum.MaterialType.Onyx, PropertyInt.GearPhysicalWard },
        { ACE.Entity.Enum.MaterialType.Opal, PropertyInt.GearManasteal },
        { ACE.Entity.Enum.MaterialType.Peridot, PropertyInt.GearQuickness },
        { ACE.Entity.Enum.MaterialType.RedGarnet, PropertyInt.GearFire },
        { ACE.Entity.Enum.MaterialType.RedJade, PropertyInt.GearFocus },
        { ACE.Entity.Enum.MaterialType.RoseQuartz, PropertyInt.GearVitalsTransfer },
        { ACE.Entity.Enum.MaterialType.Ruby, PropertyInt.GearRedFury },
        { ACE.Entity.Enum.MaterialType.Sapphire, PropertyInt.GearMagicFind },
        { ACE.Entity.Enum.MaterialType.SmokeyQuartz, PropertyInt.GearThreatReduction },
        { ACE.Entity.Enum.MaterialType.Sunstone, PropertyInt.GearExperienceGain },
        { ACE.Entity.Enum.MaterialType.TigerEye, PropertyInt.GearCoordination },
        { ACE.Entity.Enum.MaterialType.Tourmaline, PropertyInt.GearWardPen },
        { ACE.Entity.Enum.MaterialType.Turquoise, PropertyInt.GearBlock },
        { ACE.Entity.Enum.MaterialType.WhiteJade, PropertyInt.GearHealBubble },
        { ACE.Entity.Enum.MaterialType.WhiteQuartz, PropertyInt.GearThorns },
        { ACE.Entity.Enum.MaterialType.WhiteSapphire, PropertyInt.GearBludgeon },
        { ACE.Entity.Enum.MaterialType.YellowGarnet, PropertyInt.GearBravado },
        { ACE.Entity.Enum.MaterialType.YellowTopaz, PropertyInt.GearEndurance },
        { ACE.Entity.Enum.MaterialType.Zircon, PropertyInt.GearElementalWard },
    };

    private static readonly Dictionary<string, MaterialType> StringToMaterialType = new()
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

    public static Dictionary<PropertyInt, (string Name, string Slot, int BasePrimary, float BonusPrimary, int BaseSecondary, float BonusSecondary)> JewelEffectInfo = new()
    {
        // neck
        { PropertyInt.GearExperienceGain, ("Illuminated Mind", "ring", 5, 0.25f, 0, 0.0f) },
        { PropertyInt.GearMagicFind, ("Seeker", "ring", 5, 0.25f, 0, 0.0f) },
        { PropertyInt.GearPyrealFind, ("Prosperity", "ring", 5, 0.25f, 0, 0.0f) },

        // ring
        { PropertyInt.GearStrength, ("Mighty Thews", "ring", 10, 1.0f, 0, 0.0f) },
        { PropertyInt.GearSelf, ("Erudite Mind", "ring", 10, 1.0f, 0, 0.0f) },
        { PropertyInt.GearCoordination, ("Dexterous Hand", "ring", 10, 1.0f, 0, 0.0f) },
        { PropertyInt.GearFocus, ("Focused Mind", "ring", 10, 1.0f, 0, 0.0f) },
        { PropertyInt.GearEndurance, ("Perserverence", "ring", 10, 1.0f, 0, 0.0f) },
        { PropertyInt.GearQuickness, ("Swift-footed", "ring", 10, 1.0f, 0, 0.0f) },

        // bracelet
        { PropertyInt.GearThreatGain, ("Provocation", "bracelet", 10, 0.5f, 0, 0.0f) },
        { PropertyInt.GearHealthToStamina, ("Masochist", "bracelet", 10, 0.5f, 0, 0.0f) },
        { PropertyInt.GearStamReduction, ("Third Wind", "bracelet", 10, 0.5f, 0, 0.0f) },
        { PropertyInt.GearHealthToMana, ("Austere Anchorite", "bracelet", 10, 0.5f, 0, 0.0f) },
        { PropertyInt.GearThreatReduction, ("Clouded Vision", "bracelet", 10, 0.5f, 0, 0.0f) },
        { PropertyInt.GearItemManaUsage, ("Meticulous Magus", "bracelet", 20, 1.0f, 0, 0.0f) },
        { PropertyInt.GearCompBurn, ("Thrifty Scholar", "bracelet", 20, 1.0f, 0, 0.0f) },
        { PropertyInt.GearPhysicalWard, ("Black Bulwark", "bracelet", 10, 0.5f, 0, 0.0f) },
        { PropertyInt.GearHardenedDefense, ("Hardened Fortification", "bracelet", 20, 1.0f, 0, 0.0f) },
        { PropertyInt.GearNullification, ("Nullification", "bracelet", 20, 1.0f, 0, 0.0f) },
        { PropertyInt.GearElementalWard, ("Prismatic Ward", "bracelet", 10, 0.5f, 0, 0.0f) },

        // shield
        { PropertyInt.GearBlock, ("Stalwart Defense", "shield", 10, 0.5f, 0, 0.0f) },
        { PropertyInt.GearThorns, ("Swift Retrbution", "shield", 10, 0.5f, 0, 0.0f) },

        // weapon
        { PropertyInt.GearLightning, ("Astyrrian Rage", "weapon", 10, 0.5f, 2, 0.1f) },
        { PropertyInt.GearFire, ("Blazing Brand", "weapon", 10, 0.5f, 2, 0.1f) },
        { PropertyInt.GearSelfHarm, ("Blood Frenzy", "weapon", 10, 0.5f, 0, 0.0f) },
        { PropertyInt.GearFrost, ("Bone-chiller", "weapon", 10, 0.5f, 2, 0.1f) },
        { PropertyInt.GearAcid, ("Devouring Mist", "weapon", 10, 0.5f, 2, 0.1f) },
        { PropertyInt.GearSlash, ("Falcon's Gyre", "weapon", 10, 0.5f, 0, 0.0f) },
        { PropertyInt.GearManasteal, ("Ophidian", "weapon", 10, 0.5f, 0, 0.0f) },
        { PropertyInt.GearPierce, ("Precision Strikes", "weapon", 20, 1.0f, 0, 0.0f) },
        { PropertyInt.GearHealBubble, ("Purified Soul", "weapon", 10, 0.5f, 2, 0.1f) },
        { PropertyInt.GearLifesteal, ("Sanguine Thirst", "weapon", 10, 0.5f, 0, 0.0f) },
        { PropertyInt.GearBludgeon, ("Skull-cracker", "weapon", 10, 0.5f, 0, 0.0f) },
        { PropertyInt.GearVitalsTransfer, ("Tilted-scales", "weapon", 20, 1.0f, 0, 0.0f) },

        // weapon or shield
        { PropertyInt.GearBravado, ("Bravado", "weapon or shield", 20, 1.0f, 0, 0.0f) },
        { PropertyInt.GearFamiliarity, ("Familiar Foe", "weapon or shield", 20, 1.0f, 0, 0.0f) },
        { PropertyInt.GearRedFury, ("Red Fury", "weapon or shield", 20, 1.0f, 0, 0.0f) },
        { PropertyInt.GearReprisal, ("Vicious Reprisal", "weapon or shield", 5, 0.25f, 0, 0.0f) },

        // wand
        { PropertyInt.GearElementalist, ("Elementalist", "wand", 20, 1.0f, 0, 0.0f) },
        { PropertyInt.GearWardPen, ("Ruthless Discernment", "wand", 20, 1.0f, 0, 0.0f) },
        { PropertyInt.GearSelflessness, ("Selfless Spirit", "wand", 10, 0.5f, 0, 0.0f) },

        // other
        { PropertyInt.GearBlueFury, ("Blue Fury", "", 20, 1.0f, 0, 0.0f) },
        { PropertyInt.GearYellowFury, ("Yellow Fury", "", 20, 1.0f, 0, 0.0f) },

    };
}
