using System;
using System.Collections.Generic;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Entity.Models;
using ACE.Server.Entity;
using ACE.Server.Entity.Actions;
using ACE.Server.Factories;
using ACE.Server.Factories.Tables;
using ACE.Server.Managers;
using ACE.Server.Network.GameMessages.Messages;
using Serilog;

namespace ACE.Server.WorldObjects;

public class UpgradeKit : Stackable
{
    private static readonly ILogger _log = Log.ForContext(typeof(UpgradeKit));

    /// <summary>
    /// A new biota be created taking all of its values from weenie.
    /// </summary>
    public UpgradeKit(Weenie weenie, ObjectGuid guid)
        : base(weenie, guid)
    {
        SetEphemeralValues();
    }

    /// <summary>
    /// Restore a WorldObject from the database.
    /// </summary>
    public UpgradeKit(Biota biota)
        : base(biota)
    {
        SetEphemeralValues();
    }

    private static void SetEphemeralValues() { }

    public override void HandleActionUseOnTarget(Player player, WorldObject target)
    {
        UseObjectOnTarget(player, this, target);
    }

    public static void UseObjectOnTarget(Player player, WorldObject source, WorldObject target, bool confirmed = false)
    {
        if (player.IsBusy)
        {
            player.SendUseDoneEvent(WeenieError.YoureTooBusy);
            return;
        }

        if (!RecipeManager.VerifyUse(player, source, target, true))
        {
            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
            return;
        }

        if (!target.UpgradeableQuestItem)
        {
            player.Session.Network.EnqueueSend(
                new GameMessageSystemChat("Only certain quest items can be upgraded.", ChatMessageType.Craft)
            );
            player.SendUseDoneEvent();
            return;
        }

        var requiredUpgradeKits = GetRequiredUpgradeKits(player, target);
        var highestWieldDifficultyForPlayer = GetHighestWieldDifficultyForPlayer(player, target);

        if (source.StackSize < requiredUpgradeKits)
        {
            player.Session.Network.EnqueueSend(
                new GameMessageSystemChat($"Upgrading {target.Name} to the highest difficulty you can wield ({highestWieldDifficultyForPlayer}) requires {requiredUpgradeKits} Upgrade kits.", ChatMessageType.Craft)
            );
            player.SendUseDoneEvent();
            return;
        }

        if (target.ItemType == ItemType.Jewelry && target.WieldDifficulty >= GetRequiredLevelFromPlayerTier(player))
        {
            player.Session.Network.EnqueueSend(
                new GameMessageSystemChat($"{target.Name} is already at the highest difficulty you can wield.", ChatMessageType.Craft)
            );
            player.SendUseDoneEvent();
            return;
        }

        if (target.ItemType != ItemType.Jewelry && target.WieldDifficulty >= highestWieldDifficultyForPlayer)
        {
            player.Session.Network.EnqueueSend(
                new GameMessageSystemChat($"{target.Name} is already at the highest difficulty you can wield.", ChatMessageType.Craft)
            );
            player.SendUseDoneEvent();
            return;
        }

        if (!confirmed)
        {
            var wieldReq = target.ItemType == ItemType.Jewelry
                ? GetRequiredLevelFromPlayerTier(player)
                : GetHighestWieldDifficultyForPlayer(player, target);
            var wieldReqType = target.ItemType == ItemType.Jewelry ? "Required Level" : "Wield Difficulty";
            if (
                !player.ConfirmationManager.EnqueueSend(
                    new Confirmation_CraftInteration(player.Guid, source.Guid, target.Guid),
                    $"This will upgrade {target.Name} to the highest difficulty you can wield. Its {wieldReqType} will be increased to {wieldReq}.\n\n" +
                    $"{requiredUpgradeKits} Upgrade Kits will be consumed."
                )
            )
            {
                player.SendUseDoneEvent(WeenieError.ConfirmationInProgress);
            }
            else
            {
                player.SendUseDoneEvent();
            }

            return;
        }

        var actionChain = new ActionChain();

        var animTime = 0.0f;

        player.IsBusy = true;

        if (player.CombatMode != CombatMode.NonCombat)
        {
            var stanceTime = player.SetCombatMode(CombatMode.NonCombat);
            actionChain.AddDelaySeconds(stanceTime);

            animTime += stanceTime;
        }

        animTime += player.EnqueueMotion(actionChain, MotionCommand.ClapHands);

        actionChain.AddAction(
            player,
            () =>
            {
                if (!UpgradeItem(player, target))
                {
                    player.EnqueueBroadcast(new GameMessageUpdateObject(target));
                    player.Session.Network.EnqueueSend(
                        new GameMessageSystemChat(
                            "The crafting attempt encountered an error. Please report.",
                            ChatMessageType.Broadcast
                        )
                    );
                }
                else
                {
                    player.EnqueueBroadcast(new GameMessageUpdateObject(target));
                    player.Session.Network.EnqueueSend(
                        new GameMessageSystemChat(
                            $"You upgrade {target.Name} to a more powerful version.",
                            ChatMessageType.Broadcast
                        )
                    );
                    player.TryConsumeFromInventoryWithNetworking(source, requiredUpgradeKits);
                }
            }
        );

        player.EnqueueMotion(actionChain, MotionCommand.Ready);

        actionChain.AddAction(
            player,
            () =>
            {
                player.IsBusy = false;
            }
        );

        actionChain.EnqueueChain();

        player.NextUseTime = DateTime.UtcNow.AddSeconds(animTime);
    }

    private static bool UpgradeItem(Player player, WorldObject target)
    {
        if (target.ItemType != ItemType.Jewelry)
        {
            var currentWieldDifficulty = target.WieldDifficulty ?? 50;
            var newWieldDifficulty = GetHighestWieldDifficultyForPlayer(player, target);

            var currentTier = LootGenerationFactory.GetTierFromWieldDifficulty(currentWieldDifficulty) - 1;
            var newTier = LootGenerationFactory.GetTierFromWieldDifficulty(newWieldDifficulty) - 1;

            // Weapons
            if (target.ItemType is ItemType.Weapon or ItemType.MissileWeapon or ItemType.MeleeWeapon or ItemType.Caster)
            {
                if (target.WeaponSubtype == null)
                {
                    _log.Error(
                        $"MutateQuestItem() - WeaponSubType is null for ({target.Name}). Weapon upgrade aborted.");
                    return false;
                }

                var weaponSubtype = (LootTables.WeaponSubtype)target.WeaponSubtype;

                ScaleUpDamage(target, weaponSubtype, currentTier, newTier);
                ScaleUpDamageMod(target, weaponSubtype, currentTier, newTier);
                ScaleUpElementalDamageMod(target, weaponSubtype, currentTier, newTier);
                ScaleUpWeaponOffense(target, currentTier, newTier);
                ScaleUpWeaponPhysicalDefense(target, currentTier, newTier);
                ScaleUpWeaponMagicDefense(target, currentTier, newTier);
                ScaleUpWeaponSkillMod(PropertyFloat.WeaponLifeMagicMod, target, currentTier, newTier);
                ScaleUpWeaponSkillMod(PropertyFloat.WeaponWarMagicMod, target, currentTier, newTier);
            }

            // Armor
            if (target.WeenieType is WeenieType.Clothing || target.ItemType == ItemType.Armor)
            {
                if (target.ArmorStyle == null)
                {
                    _log.Error(
                        $"MutateQuestItem() - ArmorStyle is null for ({target.Name}). Armor upgrade aborted.");
                    return false;
                }

                ScaleUpArmorLevel(target, currentTier, newTier);
                ScaleUpWardLevel(target, currentTier, newTier);

                ScaleUpArmorSkillMod(PropertyFloat.ArmorAttackMod, target, currentTier, newTier);
                ScaleUpArmorSkillMod(PropertyFloat.ArmorDeceptionMod, target, currentTier, newTier);
                ScaleUpArmorSkillMod(PropertyFloat.ArmorDualWieldMod, target, currentTier, newTier);
                ScaleUpArmorSkillMod(PropertyFloat.ArmorHealthMod, target, currentTier, newTier);
                ScaleUpArmorSkillMod(PropertyFloat.ArmorHealthRegenMod, target, currentTier, newTier);
                ScaleUpArmorSkillMod(PropertyFloat.ArmorLifeMagicMod, target, currentTier, newTier);
                ScaleUpArmorSkillMod(PropertyFloat.ArmorMagicDefMod, target, currentTier, newTier);
                ScaleUpArmorSkillMod(PropertyFloat.ArmorManaMod, target, currentTier, newTier);
                ScaleUpArmorSkillMod(PropertyFloat.ArmorManaRegenMod, target, currentTier, newTier);
                ScaleUpArmorSkillMod(PropertyFloat.ArmorPerceptionMod, target, currentTier, newTier);
                ScaleUpArmorSkillMod(PropertyFloat.ArmorPhysicalDefMod, target, currentTier, newTier);
                ScaleUpArmorSkillMod(PropertyFloat.ArmorRunMod, target, currentTier, newTier);
                ScaleUpArmorSkillMod(PropertyFloat.ArmorShieldMod, target, currentTier, newTier);
                ScaleUpArmorSkillMod(PropertyFloat.ArmorStaminaMod, target, currentTier, newTier);
                ScaleUpArmorSkillMod(PropertyFloat.ArmorStaminaRegenMod, target, currentTier, newTier);
                ScaleUpArmorSkillMod(PropertyFloat.ArmorThieveryMod, target, currentTier, newTier);
                ScaleUpArmorSkillMod(PropertyFloat.ArmorTwohandedCombatMod, target, currentTier, newTier);
                ScaleUpArmorSkillMod(PropertyFloat.ArmorWarMagicMod, target, currentTier, newTier);
            }

            ScaleUpSpecialRatings(target, newTier);

            // Wield Difficulty
            target.SetProperty(PropertyInt.WieldDifficulty, newWieldDifficulty);

            // Spells
            ScaleUpSpells(target, currentTier, newTier);

            // Item Mana
            ScaleUpItemMana(target, newTier);
        }

        // Jewelry
        else
        {
            var currentRequiredLevel = target.WieldDifficulty ?? 1;
            var newRequiredLevel = GetRequiredLevelFromPlayerTier((player));

            var currentTier = LootGenerationFactory.GetTierFromRequiredLevel(currentRequiredLevel);
            var newTier = LootGenerationFactory.GetTierFromRequiredLevel(newRequiredLevel);

            ScaleUpJewelryWardLevel(target, currentTier, newTier);
            ScaleUpJewelryRating(PropertyInt.GearMaxHealth, target, currentTier, newTier);
            ScaleUpJewelryRating(PropertyInt.GearMaxStamina, target, currentTier, newTier);
            ScaleUpJewelryRating(PropertyInt.GearMaxMana, target, currentTier, newTier);
            ScaleUpJewelryRating(PropertyInt.GearCritDamage, target, currentTier, newTier);
            ScaleUpJewelryRating(PropertyInt.GearCritResist, target, currentTier, newTier);
            ScaleUpJewelryRating(PropertyInt.GearDamage, target, currentTier, newTier);
            ScaleUpJewelryRating(PropertyInt.GearDamageResist, target, currentTier, newTier);

            ScaleUpSpecialRatings(target, newTier);

            // Level Requirement
            target.SetProperty(PropertyInt.WieldDifficulty, newRequiredLevel);

            // Spells
            ScaleUpSpells(target, currentTier, newTier);

            // Item Mana
            ScaleUpItemMana(target, newTier);
        }

        return true;
    }

    private static int GetHighestWieldDifficultyForPlayer(Player player, WorldObject target)
    {
        if (target.WieldSkillType == null)
        {
            return 0;
        }

        var targetWieldAttribute = (PropertyAttribute)target.WieldSkillType;
        var playerBaseAttributeLevel = player.Attributes[targetWieldAttribute].Base;

        return playerBaseAttributeLevel switch
        {
            >= 270 => 270,
            >= 250 => 250,
            >= 230 => 230,
            >= 215 => 215,
            >= 200 => 200,
            >= 175 => 175,
            >= 125 => 125,
            _ => 50
        };
    }

    private static int GetRequiredLevelFromPlayerTier(Player player)
    {
        var playerTier = player.GetPlayerTier(player.Level ?? 1);

        return LootGenerationFactory.GetRequiredLevelPerTier(playerTier);
    }

    private static void ScaleUpDamage(WorldObject target, LootTables.WeaponSubtype weaponSubtype, int currentTier, int newTier)
    {
        if (target.Damage == null)
        {
            return;
        }

        var currentBaseStat = target.Damage.Value;
        var currentTierMinimum = LootTables.GetMeleeSubtypeMinimumDamage(weaponSubtype, currentTier);
        var currentRange = LootTables.GetMeleeSubtypeDamageRange(weaponSubtype, currentTier);
        var currentRoll = currentBaseStat - currentTierMinimum;
        var rollPercentile = (float)currentRoll / currentRange;

        var newTierMinimum = LootTables.GetMeleeSubtypeMinimumDamage(weaponSubtype, newTier);
        var newTierRange = LootTables.GetMeleeSubtypeDamageRange(weaponSubtype, newTier);
        var amountAboveMinimum = newTierRange * rollPercentile;
        var final = Convert.ToInt32(newTierMinimum + amountAboveMinimum);

        target.SetProperty(PropertyInt.Damage, final);
    }

    private static void ScaleUpDamageMod(WorldObject target, LootTables.WeaponSubtype weaponSubtype, int currentTier, int newTier)
    {
        if (target.DamageMod == null)
        {
            return;
        }

        var currentBaseStat = target.DamageMod.Value;
        var currentTierMinimum = LootTables.GetMissileCasterSubtypeMinimumDamage(weaponSubtype, currentTier);
        var currentRange = LootTables.GetMissileCasterSubtypeDamageRange(weaponSubtype, currentTier);
        var currentRoll = currentBaseStat - currentTierMinimum;
        var rollPercentile = (float)currentRoll / currentRange;

        var newTierMinimum = LootTables.GetMissileCasterSubtypeMinimumDamage(weaponSubtype, newTier);
        var newTierRange = LootTables.GetMissileCasterSubtypeDamageRange(weaponSubtype, newTier);
        var amountAboveMinimum = newTierRange * rollPercentile;
        var final = newTierMinimum + amountAboveMinimum;

        target.SetProperty(PropertyFloat.DamageMod, final);
    }

    private static void ScaleUpElementalDamageMod(WorldObject target, LootTables.WeaponSubtype weaponSubtype, int currentTier, int newTier)
    {
        if (target.ElementalDamageMod == null || target.WeaponRestorationSpellsMod == null)
        {
            return;
        }

        var magicSkill = target.WieldSkillType2 == 34 ? Skill.WarMagic : Skill.LifeMagic;
        var elementalDamageMod = target.ElementalDamageMod.Value;
        var restorationSpellMod = target.WeaponRestorationSpellsMod.Value;

        var currentBaseStat = magicSkill == Skill.WarMagic ? elementalDamageMod : restorationSpellMod;
        var currentTierMinimum = LootTables.GetMissileCasterSubtypeMinimumDamage(weaponSubtype, currentTier);
        var currentRange = LootTables.GetMissileCasterSubtypeDamageRange(weaponSubtype, currentTier);
        var currentRoll = currentBaseStat - currentTierMinimum;
        var rollPercentile = (float)currentRoll / currentRange;

        var newTierMinimum = LootTables.GetMissileCasterSubtypeMinimumDamage(weaponSubtype, newTier);
        var newTierRange = LootTables.GetMissileCasterSubtypeDamageRange(weaponSubtype, newTier);
        var amountAboveMinimum = newTierRange * rollPercentile;
        var final = newTierMinimum + amountAboveMinimum;

        if (magicSkill == Skill.WarMagic)
        {
            target.SetProperty(PropertyFloat.ElementalDamageMod, final);
            target.SetProperty(PropertyFloat.WeaponRestorationSpellsMod, 1 + (final - 1) / 2);
        }
        else
        {
            target.SetProperty(PropertyFloat.WeaponRestorationSpellsMod, final);
            target.SetProperty(PropertyFloat.ElementalDamageMod, 1 + (final - 1) / 2);
        }
    }

    private static void ScaleUpWeaponOffense(WorldObject target, int currentTier, int newTier)
    {
        var currentStat = target.WeaponOffense;

        if (currentStat == null)
        {
            return;
        }

        var currentTierBonus = LootTables.WeaponOffenseModBonusPerTier[currentTier];
        var newTierBonus = LootTables.WeaponOffenseModBonusPerTier[newTier];
        var difference = newTierBonus - currentTierBonus;

        var final = currentStat.Value + difference;

        target.SetProperty(PropertyFloat.WeaponOffense, final);
    }

    private static void ScaleUpWeaponPhysicalDefense(WorldObject target, int currentTier, int newTier)
    {
        var currentStat = target.WeaponPhysicalDefense;

        if (currentStat == null)
        {
            return;
        }

        var currentTierBonus = LootTables.WeaponDefenseModBonusPerTier[currentTier];
        var newTierBonus = LootTables.WeaponDefenseModBonusPerTier[newTier];
        var difference = newTierBonus - currentTierBonus;

        var final = currentStat.Value + difference;

        target.SetProperty(PropertyFloat.WeaponPhysicalDefense, final);
    }

    private static void ScaleUpWeaponMagicDefense(WorldObject target, int currentTier, int newTier)
    {
        var currentStat = target.WeaponMagicalDefense;

        if (currentStat == null)
        {
            return;
        }

        var currentTierBonus = LootTables.WeaponDefenseModBonusPerTier[currentTier];
        var newTierBonus = LootTables.WeaponDefenseModBonusPerTier[newTier];
        var difference = newTierBonus - currentTierBonus;

        var final = currentStat.Value + difference;

        target.SetProperty(PropertyFloat.WeaponMagicalDefense, final);
    }

    private static void ScaleUpWeaponSkillMod(PropertyFloat property, WorldObject target, int currentTier, int newTier)
    {
        var currentStat = target.GetProperty(property);

        if (currentStat == null)
        {
            return;
        }

        var currentTierBonus = LootTables.WeaponSkillModBonusPerTier[currentTier];
        var newTierBonus = LootTables.WeaponSkillModBonusPerTier[newTier];
        var difference = newTierBonus - currentTierBonus;

        var final = currentStat.Value + difference;

        target.SetProperty(property, final);
    }

    private static void ScaleUpArmorLevel(WorldObject target, int currentTier, int newTier)
    {
        if (target.ArmorLevel == null)
        {
            return;
        }

        var armorStyleBaseArmorLevel = 50;

        if (target.ArmorStyle != null)
        {
            armorStyleBaseArmorLevel = (ArmorStyle)target.ArmorStyle switch
            {
                ACE.Entity.Enum.ArmorStyle.Amuli or ACE.Entity.Enum.ArmorStyle.Chiran
                    or ACE.Entity.Enum.ArmorStyle.OlthoiAmuli or ACE.Entity.Enum.ArmorStyle.Leather
                    or ACE.Entity.Enum.ArmorStyle.Yoroi or ACE.Entity.Enum.ArmorStyle.Lorica
                    or ACE.Entity.Enum.ArmorStyle.Buckler or ACE.Entity.Enum.ArmorStyle.SmallShield
                    => 75,
                ACE.Entity.Enum.ArmorStyle.StuddedLeather or ACE.Entity.Enum.ArmorStyle.Koujia
                    or ACE.Entity.Enum.ArmorStyle.OlthoiKoujia
                    => 90,
                ACE.Entity.Enum.ArmorStyle.Chainmail or ACE.Entity.Enum.ArmorStyle.Scalemail
                    or ACE.Entity.Enum.ArmorStyle.Nariyid or ACE.Entity.Enum.ArmorStyle.StandardShield
                    => 100,
                ACE.Entity.Enum.ArmorStyle.LargeShield
                    => 105,
                ACE.Entity.Enum.ArmorStyle.Platemail or ACE.Entity.Enum.ArmorStyle.Celdon
                    or ACE.Entity.Enum.ArmorStyle.OlthoiCeldon or ACE.Entity.Enum.ArmorStyle.TowerShield
                    => 110,
                ACE.Entity.Enum.ArmorStyle.Covenant or ACE.Entity.Enum.ArmorStyle.OlthoiArmor
                    or ACE.Entity.Enum.ArmorStyle.CovenantShield
                    => 125,
                _ => armorStyleBaseArmorLevel
            };
        }

        var currentLevel = target.ArmorLevel.Value;
        var currentTierMinimum = armorStyleBaseArmorLevel * Math.Clamp(currentTier, 1, 7);
        var rolledAmount = currentLevel - currentTierMinimum;

        var newTierMinimum = armorStyleBaseArmorLevel * Math.Clamp(newTier, 1, 7);
        var final = newTierMinimum + rolledAmount;

        target.SetProperty(PropertyInt.ArmorLevel, final);
    }

    private static void ScaleUpWardLevel(WorldObject target, int currentTier, int newTier)
    {
        if (target.WardLevel == null)
        {
            return;
        }

        var armorStyleBaseWardLevel = 10;

        if (target.ArmorStyle != null)
        {
            armorStyleBaseWardLevel = (ArmorStyle)target.ArmorStyle switch
            {
                ACE.Entity.Enum.ArmorStyle.CovenantShield
                    => 10,
                ACE.Entity.Enum.ArmorStyle.TowerShield
                    => 8,
                ACE.Entity.Enum.ArmorStyle.Amuli or ACE.Entity.Enum.ArmorStyle.Chiran
                    or ACE.Entity.Enum.ArmorStyle.OlthoiAmuli or ACE.Entity.Enum.ArmorStyle.LargeShield
                    => 7,
                ACE.Entity.Enum.ArmorStyle.StandardShield
                    => 6,
                ACE.Entity.Enum.ArmorStyle.Buckler or ACE.Entity.Enum.ArmorStyle.SmallShield
                    => 5,
                ACE.Entity.Enum.ArmorStyle.Leather or ACE.Entity.Enum.ArmorStyle.StuddedLeather
                    or ACE.Entity.Enum.ArmorStyle.Koujia or ACE.Entity.Enum.ArmorStyle.OlthoiKoujia
                    or ACE.Entity.Enum.ArmorStyle.Chainmail or ACE.Entity.Enum.ArmorStyle.Scalemail
                    or ACE.Entity.Enum.ArmorStyle.Nariyid or ACE.Entity.Enum.ArmorStyle.Platemail
                    or ACE.Entity.Enum.ArmorStyle.Celdon or ACE.Entity.Enum.ArmorStyle.OlthoiCeldon
                    or ACE.Entity.Enum.ArmorStyle.Covenant or ACE.Entity.Enum.ArmorStyle.OlthoiArmor
                    => 5,
                _ => armorStyleBaseWardLevel
            };
        }

        var currentLevel = target.WardLevel.Value;
        var currentTierMinimum = armorStyleBaseWardLevel * Math.Clamp(currentTier, 1, 7);
        var rolledAmount = currentLevel - currentTierMinimum;

        var newTierMinimum = armorStyleBaseWardLevel * Math.Clamp(newTier, 1, 7);
        var final = newTierMinimum + rolledAmount;

        target.SetProperty(PropertyInt.WardLevel, final);
    }

    private static void ScaleUpArmorSkillMod(PropertyFloat property, WorldObject target, int currentTier, int newTier)
    {
        var currentStat = target.GetProperty(property);

        if (currentStat == null)
        {
            return;
        }

        var currentTierBonus = LootTables.ArmorSkillModBonusPerTier[currentTier];
        var newTierBonus = LootTables.ArmorSkillModBonusPerTier[newTier];
        var difference = newTierBonus - currentTierBonus;

        var final = currentStat.Value + difference;

        target.SetProperty(property, final);
    }

    private static void ScaleUpJewelryWardLevel(WorldObject target, int currentTier, int newTier)
    {
        var currentBaseStat = target.GetProperty(PropertyInt.WardLevel);

        if (currentBaseStat == null)
        {
            return;
        }

        var jewelryBaseWardLevelPerTier = LootTables.JewelryBaseWardLeverPerTier;

        var currentBaseLevelFromTier = jewelryBaseWardLevelPerTier[currentTier];
        var currentRange = jewelryBaseWardLevelPerTier[currentTier + 1] - jewelryBaseWardLevelPerTier[currentTier];
        var currentRoll = currentBaseStat - currentBaseLevelFromTier;

        var rollPercentile = (float)currentRoll / currentRange;

        var newTierRange = jewelryBaseWardLevelPerTier[newTier + 1] - jewelryBaseWardLevelPerTier[newTier];
        var amountAboveMinimum = newTierRange * rollPercentile;

        var newBaseLevelFromTier = jewelryBaseWardLevelPerTier[newTier];
        var final = Convert.ToInt32(newBaseLevelFromTier + amountAboveMinimum);

        target.SetProperty(PropertyInt.WardLevel, final);
    }

    private static void ScaleUpJewelryRating(PropertyInt property, WorldObject target, int currentTier, int newTier)
    {
        var currentBaseStat = target.GetProperty(property);

        if (currentBaseStat == null)
        {
            return;
        }

        var jewelryBaseRatingPerTier = LootTables.JewelryBaseRatingPerTier;

        var currentBaseLevelFromTier = jewelryBaseRatingPerTier[currentTier];
        var currentRange = currentBaseLevelFromTier;
        var currentRoll = currentBaseStat - currentBaseLevelFromTier;

        var rollPercentile = (float)currentRoll / currentRange;

        var newTierRange = jewelryBaseRatingPerTier[newTier];
        var amountAboveMinimum = newTierRange * rollPercentile;

        var newBaseLevelFromTier = jewelryBaseRatingPerTier[newTier];

        var final = Convert.ToInt32(newBaseLevelFromTier + amountAboveMinimum);

        target.SetProperty(property, final);
    }

    private static void ScaleUpItemMana(WorldObject target, int newTier)
    {
        var slots = 1;

        if (target.IsTwoHanded || target.WeenieType == WeenieType.MissileLauncher)
        {
            slots = 2;
        }
        else if (target.ArmorSlots != null)
        {
            slots = target.ArmorSlots.Value;
        }

        var manaRate = LootTables.QuestItemManaRate[slots - 1][newTier];
        target.SetProperty(PropertyFloat.ManaRate, manaRate);

        var totalMana = LootTables.QuestItemTotalMana[slots - 1][newTier];
        target.SetProperty(PropertyInt.ItemMaxMana, totalMana);
        target.SetProperty(PropertyInt.ItemCurMana, totalMana);
    }

    private static void ScaleUpSpells(WorldObject target, int currentTier, int newTier)
    {
        if (newTier is < 3 or > 7)
        {
            return;
        }

        if (currentTier % 2 == 0 && newTier == currentTier + 1)
        {
            return;
        }

        var spellBook = target.Biota.PropertiesSpellBook;

        if (spellBook == null)
        {
            return;
        }

        var spellsToRemove = new List<int>();
        var spellsToAdd = new List<int>();

        foreach (var spellId in spellBook.Keys)
        {
            spellsToRemove.Add(spellId);

            var minimumLevelSpellId = SpellLevelProgression.GetLevel1SpellId((SpellId)spellId);

            switch (newTier)
            {
                case 3:
                case 4:
                    spellsToAdd.Add((int)SpellLevelProgression.GetSpellAtLevel(minimumLevelSpellId, 2, true));
                    break;
                case 5:
                case 6:
                    spellsToAdd.Add((int)SpellLevelProgression.GetSpellAtLevel(minimumLevelSpellId, 3, true));
                    break;
                case 7:
                    spellsToAdd.Add((int)SpellLevelProgression.GetSpellAtLevel(minimumLevelSpellId, 4, true));
                    break;
            }
        }

        foreach (var spellId in spellsToRemove)
        {
            target.Biota.TryRemoveKnownSpell(spellId, target.BiotaDatabaseLock);
        }

        foreach (var spellId in spellsToAdd)
        {
            target.Biota.GetOrAddKnownSpell(spellId, target.BiotaDatabaseLock, out _);
        }
    }

    /// <summary>
    /// Scale up special ratings. (jewel ratings)
    /// </summary>
    private static void ScaleUpSpecialRatings(WorldObject target, int newTier)
    {
        var ratingList = new List<PropertyInt>();
        // var totalRatings = 0; TODO: To be used if jewel ratings get added to mutable quest item system

        const int firstGearRatingId = 409;
        const int lastGearRatingId = 450;

        for (var i = firstGearRatingId; i < lastGearRatingId; i++)
        {
            var ratingValue = target.GetProperty((PropertyInt)i);

            if (ratingValue == null)
            {
                continue;
            }

            ratingList.Add((PropertyInt)i);
            //totalRatings += ratingValue.Value;
        }

        int[] ratingValuesPerSlotPerTier = [4, 6, 8, 10, 12, 16, 18, 20];
        var isItemTwoHanded = target.IsTwoHanded || target.WeenieType == WeenieType.MissileLauncher;

        var multiplier = isItemTwoHanded ? 2.0f : 1.0f;

        var newRatingValue = Convert.ToInt32(ratingValuesPerSlotPerTier[newTier] * multiplier / ratingList.Count);

        foreach (var itemRating in ratingList)
        {
            target.SetProperty(itemRating, newRatingValue);
        }
    }

    private static int GetRequiredUpgradeKits(Player player, WorldObject target)
    {
        var newWieldReq = GetHighestWieldDifficultyForPlayer(player, target);

        switch (newWieldReq)
        {
            case 125: return 1;
            case 175: return 2;
            case 200: return 3;
            case 215: return 4;
            case 230: return 5;
            case 250: return 6;
            case 270: return 7;
        }

        return 1;
    }
}
