using ACE.Common;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Entity;
using System;
using System.Collections.Generic;
using AttackType = ACE.Entity.Enum.AttackType;
using DamageType = ACE.Entity.Enum.DamageType;
using WeaponType = ACE.Entity.Enum.WeaponType;


namespace ACE.Server.WorldObjects
{
    partial class Jewel : WorldObject
    {
        // Caster and Physical Overlapping Bonuses
        public static void HandlePlayerAttackerBonuses(Player playerAttacker, Creature defender, float damage, ACE.Entity.Enum.DamageType damageType)
        {
            // JEWEL - Bloodstone: Gain life on hit
            if (playerAttacker.GetEquippedItemsRatingSum(PropertyInt.GearLifesteal) > 0)
            {
                var lifeStealChance = ((float)playerAttacker.GetEquippedItemsRatingSum(PropertyInt.GearLifesteal) / 100);

                if (playerAttacker != defender && lifeStealChance >= ThreadSafeRandom.Next(0.0f, 1.0f))
                {
                    var healAmount = (uint)Math.Round(damage / 10);
                    playerAttacker.UpdateVitalDelta(playerAttacker.Health, healAmount);
                    playerAttacker.DamageHistory.OnHeal(healAmount);
                }
            }

            // JEWEL - Opal: Gain mana on hit
            if (playerAttacker.GetEquippedItemsRatingSum(PropertyInt.GearManasteal) > 0)
            {
                var manaStealChance = ((float)playerAttacker.GetEquippedItemsRatingSum(PropertyInt.GearManasteal) / 100);

                if (playerAttacker != defender && manaStealChance >= ThreadSafeRandom.Next(0.0f, 1.0f))
                {
                    var healAmount = (uint)Math.Round((decimal)damage / 10);
                    playerAttacker.UpdateVitalDelta(playerAttacker.Mana, healAmount);
                    playerAttacker.DamageHistory.OnHeal(healAmount);
                }
            }

            // JEWEL - Hematite: Self-harm damage
            if (playerAttacker.GetEquippedItemsRatingSum(PropertyInt.GearSelfHarm) > 0)
            {
                var jewelSelfHarm = (float)(playerAttacker.GetEquippedItemsRatingSum(PropertyInt.GearSelfHarm) / 100);
                var selfHarm = (int)(jewelSelfHarm * damage);
                playerAttacker.UpdateVitalDelta(playerAttacker.Health, -selfHarm);
                playerAttacker.DamageHistory.Add(playerAttacker, DamageType.Health, (uint)selfHarm);
            }

            // JEWEL - Aquamarine, Emerald, Jet, Red Garnet: Check hotspot chance
            if (playerAttacker.GetEquippedItemsRatingSum(PropertyInt.GearFire) > 0 || playerAttacker.GetEquippedItemsRatingSum(PropertyInt.GearFrost) > 0
                || playerAttacker.GetEquippedItemsRatingSum(PropertyInt.GearAcid) > 0 || playerAttacker.GetEquippedItemsRatingSum(PropertyInt.GearLightning) > 0)
            {
                var tier = Math.Round((float)playerAttacker.Level / 10);
                Hotspot.TryGenHotspot(playerAttacker, defender, (int)tier, damageType);
            }

            // JEWEL - Sapphire: Magic Find stamps
            if (playerAttacker.GetEquippedItemsRatingSum(PropertyInt.GearMagicFind) > 0)
            {
                var magicFind = playerAttacker.GetEquippedItemsRatingSum(PropertyInt.GearMagicFind);

                defender.QuestManager.Stamp($"{playerAttacker.Name}/MagicFind/{magicFind}/{damage}");
            }

            // JEWEL - Green Jade: Prosperity stamps
            if (playerAttacker.GetEquippedItemsRatingSum(PropertyInt.GearPyrealFind) > 0)
            {
                var pyrealFind = playerAttacker.GetEquippedItemsRatingSum(PropertyInt.GearPyrealFind);

                defender.QuestManager.Stamp($"{playerAttacker.Name}/Prosperity/{pyrealFind}/{damage}");
            }
        }

        // Caster and Physical Overlapping Defender Bonuses
        public static void HandlePlayerDefenderBonuses(Player playerDefender, Creature attacker, float damage)
        {

            var percentOfHealth = damage / (float)playerDefender.Health.MaxValue;
            // JEWEL - Amber: Chance to gain stamina on taking damage
            if (playerDefender.GetEquippedItemsRatingSum(PropertyInt.GearHealthToStamina) > 0)
            {
                var healthToStamChance = ((float)playerDefender.GetEquippedItemsRatingSum(PropertyInt.GearHealthToStamina) / 100);

                if (percentOfHealth < 0.1f)
                {
                    healthToStamChance *= (percentOfHealth * 10);
                }

                if (attacker != playerDefender && healthToStamChance >= ThreadSafeRandom.Next(0.0f, 1.0f))
                {
                    var stamAmount = (uint)Math.Round(damage / 5);
                    playerDefender.UpdateVitalDelta(playerDefender.Stamina, stamAmount);
                    playerDefender.DamageHistory.OnHeal(stamAmount);
                }
            }
            // JEWEL - Lapis Lazuli: Chance to gain mana on taking damage
            if (playerDefender.GetEquippedItemsRatingSum(PropertyInt.GearHealthToMana) > 0)
            {
                var healthToManaChance = ((float)playerDefender.GetEquippedItemsRatingSum(PropertyInt.GearHealthToMana) / 100);

                if (percentOfHealth < 0.1f)
                {
                    healthToManaChance *= (percentOfHealth * 10);
                }

                if (attacker != playerDefender && healthToManaChance >= ThreadSafeRandom.Next(0.0f, 1.0f))
                {
                    var ManaAmount = (uint)Math.Round(damage / 4);
                    playerDefender.UpdateVitalDelta(playerDefender.Mana, ManaAmount);
                    playerDefender.DamageHistory.OnHeal(ManaAmount);
                }
            }
        }

        public static void HandleMeleeAttackerBonuses(Player playerAttacker, Creature defender, float damage, WorldObject damageSource, ACE.Entity.Enum.DamageType damageType)
        {
            var rampProperty = "";

            var scaledStamps = Math.Round(playerAttacker.ScaleWithPowerAccuracyBar(20f));

            scaledStamps += 10f;

            var numStrikes = playerAttacker.GetNumStrikes(playerAttacker.AttackType);
            if (numStrikes == 2)
            {
                if (playerAttacker.GetEquippedWeapon().W_WeaponType == WeaponType.TwoHanded)
                    scaledStamps /= 2f;
                else
                    scaledStamps /= 1.5f;
            }

            if (playerAttacker.AttackType == AttackType.OffhandPunch || playerAttacker.AttackType == AttackType.Punch || playerAttacker.AttackType == AttackType.Punches)
                scaledStamps /= 1.25f;
            
            if (playerAttacker.GetEquippedItemsRatingSum(PropertyInt.GearPierce) > 0 || playerAttacker.GetEquippedItemsRatingSum(PropertyInt.GearBludgeon) > 0 || playerAttacker.GetEquippedItemsRatingSum(PropertyInt.GearFamiliarity) > 0)
            {
                if (playerAttacker.GetEquippedItemsRatingSum(PropertyInt.GearBludgeon) > 0)
                {
                    if (damageType == ACE.Entity.Enum.DamageType.Bludgeon)
                    {
                        rampProperty = "Bludgeon";
                    }
                }

                if (playerAttacker.GetEquippedItemsRatingSum(PropertyInt.GearPierce) > 0)
                {
                    if (damageType == ACE.Entity.Enum.DamageType.Pierce)
                    {
                        rampProperty = "Pierce";
                    }
                }

                if (rampProperty != "")
                {
                    if (defender.QuestManager.HasQuest($"{playerAttacker.Name},{rampProperty}"))
                    {
                        if (defender.QuestManager.GetCurrentSolves($"{playerAttacker.Name},{rampProperty}") < 500)
                        {
                            defender.QuestManager.Increment($"{playerAttacker.Name},{rampProperty}", (int)scaledStamps);
                        }
                    }
                    else
                    {
                        defender.QuestManager.Stamp($"{playerAttacker.Name},{rampProperty}");

                        defender.QuestManager.Increment($"{playerAttacker.Name},{rampProperty}", (int)scaledStamps);
                    }
                }
                // JEWEL - Fire Opal: Ramping Hit chance stamps
                if (playerAttacker.GetEquippedItemsRatingSum(PropertyInt.GearFamiliarity) > 0)
                    rampProperty = "Familiarity";

                if (rampProperty != "")
                {
                    if (defender.QuestManager.HasQuest($"{playerAttacker.Name},{rampProperty}"))
                    {
                        if (defender.QuestManager.GetCurrentSolves($"{playerAttacker.Name},{rampProperty}") < 500)
                        {
                            defender.QuestManager.Increment($"{playerAttacker.Name},{rampProperty}", (int)scaledStamps);
                        }
                    }
                    else
                    {
                        defender.QuestManager.Stamp($"{playerAttacker.Name},{rampProperty}");

                        defender.QuestManager.Increment($"{playerAttacker.Name},{rampProperty}", (int)scaledStamps);
                    }
                }

            }

            // JEWEL - Citrine: Stam reduction stamps
            if (playerAttacker.GetEquippedItemsRatingSum(PropertyInt.GearStamReduction) > 0)
            { 
                if (playerAttacker.QuestManager.HasQuest($"{playerAttacker.Name},StamReduction"))
                {
                    if (playerAttacker.QuestManager.GetCurrentSolves($"{playerAttacker.Name},StamReduction") < 500)
                    {
                        playerAttacker.QuestManager.Increment($"{playerAttacker.Name},StamReduction", (int)scaledStamps);
                    }
                }
                else
                {
                    playerAttacker.QuestManager.Stamp($"{playerAttacker.Name},StamReduction"); ;
                    playerAttacker.QuestManager.Increment($"{playerAttacker.Name},StamReduction", (int)scaledStamps);
                }
            }
        }

        public static void HandleMeleeDefenderBonuses(Player playerDefender, Creature attacker, float damage)
        {

            // JEWEL - Diamond: Ramping Damage Reduction stamps
            if (playerDefender.GetEquippedItemsRatingSum(PropertyInt.GearHardenedDefense) > 0)
            {
                if (playerDefender.QuestManager.HasQuest($"{playerDefender.Name},Hardened Defense"))
                {
                    if (playerDefender.QuestManager.GetCurrentSolves($"{playerDefender.Name},Hardened Defense") < 200)
                    {
                        playerDefender.QuestManager.Increment($"{playerDefender.Name},Hardened Defense", 10);
                    }
                }
                else
                {
                    playerDefender.QuestManager.Stamp($"{playerDefender.Name},Hardened Defense");
                    playerDefender.QuestManager.Increment($"{playerDefender.Name},Hardened Defense", 10);
                }
            }

            // JEWEL - Yellow Garnet: Ramping Hit Chance stamps
            if (playerDefender.GetEquippedItemsRatingSum(PropertyInt.GearBravado) > 0)
            {
                if (playerDefender.QuestManager.HasQuest($"{playerDefender.Name},Bravado"))
                {
                    if (playerDefender.QuestManager.GetCurrentSolves($"{playerDefender.Name},Bravado") < 1000)
                    {
                        playerDefender.QuestManager.Increment($"{playerDefender.Name},Bravado", 50);
                    }
                }
                else
                {
                    playerDefender.QuestManager.Stamp($"{playerDefender.Name},Bravado");
                    playerDefender.QuestManager.Increment($"{playerDefender.Name},Bravado", 50);
                }
            }

        }

        public static void HandleCasterAttackerBonuses(Player sourcePlayer, Creature targetCreature, ProjectileSpellType spellType, ACE.Entity.Enum.DamageType damageType, uint spellLevel, int? spellTypeScaler)
        {
            var baseStamps = 25;
            var playerLevel = (int)(sourcePlayer.Level / 10);

            if (playerLevel > 7)
                playerLevel = 7;

            if (spellLevel == 1)
            {
                if (playerLevel > 2)
                    baseStamps /= playerLevel;
            }
            if (spellLevel == 2)
            {
                baseStamps = (int)(baseStamps * 1.5);

                if (playerLevel > 3)
                    baseStamps /= playerLevel;
            }
            if (spellLevel == 3)
            {
                baseStamps = (int)(baseStamps * 2);

                if (playerLevel > 4)
                    baseStamps /= playerLevel;
            }
            if (spellLevel == 4)
            {
                baseStamps = (int)(baseStamps * 2.25);

                if (playerLevel > 5)
                    baseStamps /= playerLevel;
            }
            if (spellLevel == 5)
            {
                baseStamps = (int)(baseStamps * 2.5);

                if (playerLevel > 6)
                    baseStamps /= playerLevel;
            }
            if (spellLevel == 6)
            {
                baseStamps = (int)(baseStamps * 2.5);

                if (playerLevel > 7)
                    baseStamps /= playerLevel;
            }
            if (spellLevel == 7)
                baseStamps = (int)(baseStamps * 2.5);

            if (spellTypeScaler != null)
                baseStamps = baseStamps / (int)spellTypeScaler;

            var propertyType = "";

            if (sourcePlayer.GetEquippedItemsRatingSum(PropertyInt.GearBludgeon) > 0)
            {
                if (damageType == ACE.Entity.Enum.DamageType.Bludgeon)
                {
                    propertyType = "Bludgeon";
                }
            }

            if (sourcePlayer.GetEquippedItemsRatingSum(PropertyInt.GearPierce) > 0)
            {
                if (damageType == ACE.Entity.Enum.DamageType.Pierce)
                {
                    propertyType = "Pierce";
                }
            }

            if (propertyType != "")
            {
                if (targetCreature.QuestManager.HasQuest($"{targetCreature.Name},{propertyType}"))
                {
                    if (targetCreature.QuestManager.GetCurrentSolves($"{targetCreature.Name},{propertyType}") < 500)
                    {
                        targetCreature.QuestManager.Increment($"{targetCreature.Name},{propertyType}", (int)baseStamps);
                    }
                }
                else
                {
                    targetCreature.QuestManager.Stamp($"{targetCreature.Name},{propertyType}");
                    targetCreature.QuestManager.Increment($"{targetCreature.Name},{propertyType}", (int)baseStamps);
                }
            }
            // JEWEL - Fire Opal: Ramping Evade/Resist chance stamps
            if (sourcePlayer.GetEquippedItemsRatingSum(PropertyInt.GearFamiliarity) > 0)
                propertyType = "Familiarity";

            if (propertyType != "")
            {
                if (targetCreature.QuestManager.HasQuest($"{sourcePlayer.Name},{propertyType}"))
                {
                    if (targetCreature.QuestManager.GetCurrentSolves($"{sourcePlayer.Name},{propertyType}") < 500)
                    {
                        targetCreature.QuestManager.Increment($"{sourcePlayer.Name},{propertyType}", (int)baseStamps);
                    }
                }
                else
                {
                    targetCreature.QuestManager.Stamp($"{sourcePlayer.Name},{propertyType}");

                    targetCreature.QuestManager.Increment($"{sourcePlayer.Name},{propertyType}", (int)baseStamps);
                }
            }
            // JEWEL - Tourmaline: Ramping Ward Pen stamps
            if (sourcePlayer.GetEquippedItemsRatingSum(PropertyInt.GearWardPen) > 0)
            {
                if (targetCreature.QuestManager.HasQuest($"{sourcePlayer.Name},WardPen"))
                {
                    if (targetCreature.QuestManager.GetCurrentSolves($"{sourcePlayer.Name},WardPen") < 500)
                    {
                        targetCreature.QuestManager.Increment($"{sourcePlayer.Name},WardPen", (int)baseStamps);
                    }
                }
                else
                {
                    targetCreature.QuestManager.Stamp($"{sourcePlayer.Name},WardPen");
                    targetCreature.QuestManager.Increment($"{sourcePlayer.Name},WardPen", (int)baseStamps);
                }
            }
            // JEWEL - Green Garnet: Ramping War Damage stamps
            if (sourcePlayer.GetEquippedItemsRatingSum(PropertyInt.GearElementalist) > 0)
            {
                if (sourcePlayer.QuestManager.HasQuest($"{sourcePlayer.Name},Elementalist"))
                {
                    if (sourcePlayer.QuestManager.GetCurrentSolves($"{sourcePlayer.Name},Elementalist") < 500)
                        sourcePlayer.QuestManager.Increment($"{sourcePlayer.Name},Elementalist", (int)baseStamps);
                }
                else
                {
                    sourcePlayer.QuestManager.Stamp($"{sourcePlayer.Name},Elementalist");
                    sourcePlayer.QuestManager.Increment($"{sourcePlayer.Name},Elementalist", (int)baseStamps);
                }

            }
        }

        public static void HandleCasterDefenderBonuses(Player targetPlayer, Creature sourceCreature, ProjectileSpellType spellType)
        {

            // JEWEL - Amethyst: Ramping Magic Absorb stamps
            if (targetPlayer.GetEquippedItemsRatingSum(PropertyInt.GearNullification) > 0)
            {
                if (targetPlayer.QuestManager.HasQuest($"{targetPlayer.Name},Nullification"))
                {
                    if (targetPlayer.QuestManager.GetCurrentSolves($"{targetPlayer.Name},Nullification") < 200)
                    {
                        targetPlayer.QuestManager.Increment($"{targetPlayer.Name},Nullification", 50);
                    }
                }
                else
                {
                    targetPlayer.QuestManager.Stamp($"{targetPlayer.Name},Nullification");
                    targetPlayer.QuestManager.Increment($"{targetPlayer.Name},Nullification", 50);
                }

            }
        }

        public static float HandleElementalBonuses(Player playerAttacker, DamageType damageType)
        {
            float jewelElemental = 1f;

            if (playerAttacker.GetEquippedItemsRatingSum(PropertyInt.GearAcid) > 0 && damageType == DamageType.Acid)
                jewelElemental += ((float)playerAttacker.GetEquippedItemsRatingSum(PropertyInt.GearAcid) / 100) * 0.75f;
            if (playerAttacker.GetEquippedItemsRatingSum(PropertyInt.GearFire) > 0 && damageType == DamageType.Fire)
                jewelElemental += ((float)playerAttacker.GetEquippedItemsRatingSum(PropertyInt.GearFire) / 100) * 0.75f;  
            if (playerAttacker.GetEquippedItemsRatingSum(PropertyInt.GearFrost) > 0 && damageType == DamageType.Cold)
                jewelElemental += ((float)playerAttacker.GetEquippedItemsRatingSum(PropertyInt.GearFrost) / 100) * 0.75f;  
            if (playerAttacker.GetEquippedItemsRatingSum(PropertyInt.GearLightning) > 0 && damageType == DamageType.Electric)
                jewelElemental += ((float)playerAttacker.GetEquippedItemsRatingSum(PropertyInt.GearLightning) / 100) * 0.75f;  
            
            return jewelElemental;
        }
        public static float GetJewelLastStand(Player playerAttacker, Creature defender)
        {
            // find proportion of players health

            var modifiedLastStand = 0f;

            var percentHealthRemaining = (float)playerAttacker.Health.Current / (float)playerAttacker.Health.MaxValue;

            // reduce the bonus from LastStand proportionately as HP rises from 25% to 50% of HP. Full bonus at 25%, 0 bonus at 50%, penalty at 50%+

            if (percentHealthRemaining <= 0.25f)
            {
                modifiedLastStand = 1f;
            }
            else if (percentHealthRemaining <= 0.5f)
            {
                //  interpolate between 1 and 0 as percentHealthRemaining goes from 0.25 to 0.5
                modifiedLastStand = 1f - (percentHealthRemaining - 0.25f) / 0.25f;
            }
            else if (percentHealthRemaining <= 0.75f)
            {
                modifiedLastStand = -(percentHealthRemaining - 0.5f) / 0.25f;
            }
            else
            {
                modifiedLastStand = -1f;
            }

            // multiply gear last stand as % by the modifier--if negative (above 50% HP), mod goes sub 1, which added to the 1f in DamageEvent results in a damage penalty.

            var lastStandMod = (modifiedLastStand * ((float)playerAttacker.GetEquippedItemsRatingSum(PropertyInt.GearLastStand) / 50));

            return lastStandMod;
        }
        // 0 - prepended quality, 1 - gemstone type, 2 - appended name, 3 - property type, 4 - amount of property, 5 - original gem workmanship
        public static string GetJewelDescription(string jewelSocket1)
        {
            string[] parts = jewelSocket1.Split('/');

            var description = "";
            if (MaterialTypetoString.TryGetValue(parts[1], out var convertedMaterialType))
            {
                if (int.TryParse(parts[4], out var amount))
                {
                    var half = Math.Round((float)amount / 2, 1);
                    var oneHalf = Math.Round((float)amount * 1.5, 1);
                    var oneTenth = Math.Round((float)amount / 10, 2);
                    var threeQ = Math.Round((float)amount * 0.66, 1);
                    if (int.TryParse(parts[5], out var originalWorkmanship))
                    {
                        var workmanship = originalWorkmanship - 1 < 1 ? 1 : originalWorkmanship - 1;

                        switch (convertedMaterialType)
                        {
                            //necklace 
                            case ACE.Entity.Enum.MaterialType.Sunstone:
                                description = $"Socket this jewel in a necklace of workmanship {workmanship} or greater to grant a {half}% bonus to experience gain.\n\n";
                                break;
                            case ACE.Entity.Enum.MaterialType.Sapphire:
                                description = $"Socket this jewel in a necklace of workmanship {workmanship} or greater to grant a {amount}% bonus to monster loot quality.\n\n";
                                break;
                            case ACE.Entity.Enum.MaterialType.GreenJade:
                                description = $"Socket this jewel in a necklace of workmanship {workmanship} or greater to grant a {half}% chance for monsters to drop an additional item on death.\n\n";
                                break;

                            // ring right
                            case ACE.Entity.Enum.MaterialType.Carnelian:
                                description = $"Socket this jewel in a ring of workmanship {workmanship} or greater to grant a {amount} bonus to base Strength.\n\nOnce socketed, the ring can only be worn on the right finger.\n\n";
                                break;
                            case ACE.Entity.Enum.MaterialType.Azurite:
                                description = $"Socket this jewel in a ring of workmanship {workmanship} or greater to grant a {amount} bonus to base Self.\n\nOnce socketed, the ring can only be worn on the right finger.\n\n";
                                break;
                            case ACE.Entity.Enum.MaterialType.TigerEye:
                                description = $"Socket this jewel in a ring of workmanship {workmanship} or greater to grant a {amount} bonus to base Coordination.\n\nOnce socketed, the ring can only be worn on the right finger.\n\n";
                                break;

                            // ring left
                            case ACE.Entity.Enum.MaterialType.RedJade:
                                description = $"Socket this jewel in a ring of workmanship {workmanship} or greater to grant a {amount} bonus to base Focus.\n\nOnce socketed, the ring can only be worn on the left finger.\n\n";
                                break;
                            case ACE.Entity.Enum.MaterialType.YellowTopaz:
                                description = $"Socket this jewel in a ring of workmanship {workmanship} or greater to grant a {amount} bonus to base Endurance.\n\nOnce socketed, the ring can only be worn on the left finger.\n\n";
                                break;
                            case ACE.Entity.Enum.MaterialType.Peridot:
                                description = $"Socket this jewel in a ring of workmanship {workmanship} or greater to grant a {amount} bonus to base Quickness.\n\nOnce socketed, the ring can only be worn on the left finger.\n\n";
                                break;

                            // bracelet left
                            case ACE.Entity.Enum.MaterialType.Agate:
                                description = $"Socket this jewel in a bracelet of workmanship {workmanship} or greater to grant a {amount}% bonus to threat generation.\n\nOnce socketed, the bracelet can only be worn on the left wrist.\n\n";
                                break;
                            case ACE.Entity.Enum.MaterialType.SmokeyQuartz:
                                description = $"Socket this jewel in a bracelet of workmanship {workmanship} or greater to grant a {amount}% bonus to threat reduction.\n\nOnce socketed, the bracelet can only be worn on the left wrist.\n\n";
                                break;
                            case ACE.Entity.Enum.MaterialType.Amber:
                                description = $"Socket this jewel in a bracelet of workmanship {workmanship} or greater to grant a {amount}% chance to regain stamina after being hit. Chance and amount regained scale based on damage recieved.\n\nOnce socketed, the bracelet can only be worn on the left wrist.\n\n";
                                break;
                            case ACE.Entity.Enum.MaterialType.LapisLazuli:
                                description = $"Socket this jewel in a bracelet of workmanship {workmanship} or greater to grant a {amount}% chance to regain mana after being hit. Chance and amount regained scale based on damage recieved.\n\nOnce socketed, the bracelet can only be worn on the left wrist.\n\n";
                                break;
                            case ACE.Entity.Enum.MaterialType.Moonstone:
                                description = $"Socket this jewel in a bracelet of workmanship {workmanship} or greater to grant a {amount * 5}% reduction to mana consumed by equipped items.\n\nOnce socketed, the bracelet can only be worn on the left wrist.\n\n";
                                break;
                            case ACE.Entity.Enum.MaterialType.Malachite:
                                description = $"Socket this jewel in a bracelet of workmanship {workmanship} or greater to grant a {amount * 3}% reduction to your chance to burn spell components.\n\nOnce socketed, the bracelet can only be worn on the left wrist.\n\n";
                                break;
                            case ACE.Entity.Enum.MaterialType.Citrine:
                                description = $"Socket this jewel in a bracelet of workmanship {workmanship} or greater to grant a {amount}% Stamina cost reduction.\n\nOnce socketed, the bracelet can only be worn on the left wrist.\n\n";
                                break;

                            // bracelet right
                            case ACE.Entity.Enum.MaterialType.Amethyst:
                                description = $"Socket this jewel in a bracelet of workmanship {workmanship} or greater to grant magic absorb, the amount ramping from 0 to {oneHalf}% based on how often you have recently have been hit with magic.\n\nOnce socketed, the bracelet can only be worn on the right wrist.\n\n";
                                break;
                            case ACE.Entity.Enum.MaterialType.Diamond:
                                description = $"Socket this jewel in a bracelet of workmanship {workmanship} or greater to grant resistance to physical damage, the amount ramping from 0 to {oneHalf}% based on how often you have recently have been hit.\n\nOnce socketed, the bracelet can only be worn on the right wrist.\n\n";
                                break;
                            case ACE.Entity.Enum.MaterialType.Onyx:
                                description = $"Socket this jewel in a bracelet of workmanship {workmanship} or greater to grant {amount}% protection against Piercing, Bludgeoning and Slashing damage types.\n\nOnce socketed, the bracelet can only be worn on the right wrist.\n\n";
                                break;
                            case ACE.Entity.Enum.MaterialType.Zircon:
                                description = $"Socket this jewel in a bracelet of workmanship {workmanship} or greater to grant {amount}% protection against Flame, Frost, Lightning, and Acid damage types.\n\nOnce socketed, the bracelet can only be worn on the right wrist.\n\n";
                                break;

                            // shield
                            case ACE.Entity.Enum.MaterialType.Turquoise:
                                description = $"Socket this jewel in a shield of workmanship {workmanship} or greater to grant {amount}% chance to block attacks.\n\n";
                                break;
                            case ACE.Entity.Enum.MaterialType.WhiteQuartz:
                                description = $"Socket this jewel in a shield of workmanship {workmanship} or greater to deflect {amount * 5}% of an attacker's damage back at them when successfully blocked, so long as the opponent is within melee range.\n\n";
                                break;

                            // weapon + shield
                            case ACE.Entity.Enum.MaterialType.BlackOpal:
                                description = $"Socket this jewel in a weapon or shield of workmanship {workmanship} or greater to to grant {half}% chance to evade an incoming critical hit. Your next attack against that enemy will be a guaranteed critical strike.\n\n";
                                break;
                            case ACE.Entity.Enum.MaterialType.FireOpal:
                                description = $"Socket this jewel in a weapon or shield of workmanship {workmanship} or greater to to grant an increased evade and resist chance versus the target you are attacking, the amount ramping from 0% to {amount}% based on how often you have hit the target.\n\n";
                                break;
                            case ACE.Entity.Enum.MaterialType.YellowGarnet:
                                description = $"Socket this jewel in a weapon or shield of workmanship {workmanship} or greater to to grant an increased physical hit chance, the amount ramping from a 0% to {amount}% based on how often you have been recently physically attacked.\n\n";
                                break;
                            case ACE.Entity.Enum.MaterialType.Ruby:
                                description = $"Socket this jewel in a weapon or shield of workmanship {workmanship} or greater to to grant increased damage as your health falls below 50%, up to a maximum bonus of {amount}% at 25% HP.\nAbove 50% Health, you receive a damage reduction penalty scaling up to {amount}% at full HP.\n\n";
                                break;

                            // weapon only
                            case ACE.Entity.Enum.MaterialType.Bloodstone:
                                description = $"Socket this jewel in a weapon of workmanship {workmanship} or greater to grant {half}% chance on hit to gain health. Chance and amount scale based on amount of damage done.\n\n";
                                break;
                            case ACE.Entity.Enum.MaterialType.Opal:
                                description = $"Socket this jewel in a weapon of workmanship {workmanship} or greater to grant {half}% chance on hit to gain mana. Chance and amount scale based on amount of damage done.\n\n";
                                break;
                            case ACE.Entity.Enum.MaterialType.Hematite:
                                description = $"Socket this jewel in a weapon of workmanship {workmanship} or greater to deal {amount}% extra damage to your opponent each time you attack, however you will take that much damage as well.\n\n";
                                break;
                            case ACE.Entity.Enum.MaterialType.RoseQuartz:
                                description = $"Socket this jewel in a weapon of workmanship {workmanship} or greater to grant a {oneHalf}% bonus to your Vitals Transfer spells, but an equivalent reduction in the effectiveness of your Restoration spells.\n\n";
                                break;
                            case ACE.Entity.Enum.MaterialType.LavenderJade:
                                description = $"Socket this jewel in a wand of workmanship {workmanship} or greater to grant a {oneHalf}% bonus to your restoration spells when cast on others, but an equivalent reduction in their effectiveness when cast on yourself.\n\n";
                                break;
                            case ACE.Entity.Enum.MaterialType.GreenGarnet:
                                description = $"Socket this jewel in a wand of workmanship {workmanship} or greater to grant a bonus to War Magic spells, the amount ramping from 0% to {oneHalf}% based on how often you have hit your target.\n\n";
                                break;
                            case ACE.Entity.Enum.MaterialType.Tourmaline:
                                description = $"Socket this jewel in a wand of workmanship {workmanship} or greater to grant Ward penetration, the amount ramping from 0% to {oneHalf}% based on how often you have hit your target.\n\n";
                                break;
                            case ACE.Entity.Enum.MaterialType.WhiteJade:
                                description = $"Socket this jewel in a weapon of workmanship {workmanship} or greater to grant {threeQ}% bonus to your restoration spells, and a {oneTenth}% to create a sphere of healing energy on top of your target on casting a restoration spell.\n\n";
                                break;
                            case ACE.Entity.Enum.MaterialType.Aquamarine:
                                description = $"Socket this jewel in a weapon of workmanship {workmanship} or greater to grant a {threeQ}% bonus to Frost damage, and a {oneTenth}% chance on hit to surround your target with chilling mist, damaging nearby enemies.\n\n";
                                break;
                            case ACE.Entity.Enum.MaterialType.BlackGarnet:
                                description = $"Socket this jewel in a weapon of workmanship {workmanship} or greater to grant it piercing resistance penetration, the amount ramping from 0% to {oneHalf}% based on how often you have hit your target.\n\n";
                                break;
                            case ACE.Entity.Enum.MaterialType.Emerald:
                                description = $"Socket this jewel in a weapon of workmanship {workmanship} or greater to grant a {threeQ}% bonus to Acid damage, and a {oneTenth}% chance on hit to surround your target with acidic mist, damaging nearby enemies.\n\n";
                                break;
                            case ACE.Entity.Enum.MaterialType.ImperialTopaz:
                                description = $"Socket this jewel in a weapon of workmanship {workmanship} or greater to grant {half}% chance to cleave a second target on hit.\n\n";
                                break;
                            case ACE.Entity.Enum.MaterialType.Jet:
                                description = $"Socket this jewel in a weapon of workmanship {workmanship} or greater to grant a {threeQ}% bonus to Lightning damage, and a {oneTenth}% chance on hit to electrify the ground beneath your target, damaging nearby enemies.\n\n";
                                break;
                            case ACE.Entity.Enum.MaterialType.RedGarnet:
                                description = $"Socket this jewel in a weapon of workmanship {workmanship} or greater to grant a {threeQ}% bonus to Fire damage, and a {oneTenth}% chance on hit to set the ground beneath your target ablaze, damaging nearby enemies.\n\n";
                                break;
                            case ACE.Entity.Enum.MaterialType.WhiteSapphire:
                                description = $"Socket this jewel in a weapon of workmanship {workmanship} or greater to grant bonus critical bludgeoning damage, the amount ramping from 0% to {amount * 2}% based on how many times you have struck your target.\n\n";
                                break;
                        }
                    }
                }       
            }
                    return description;
        }

        public static string GetSocketDescription(string jewelSocket1)
        {
            string[] parts = jewelSocket1.Split('/');

            var description = "";
            if (MaterialTypetoString.TryGetValue(parts[1], out var convertedMaterialType))
            {
                if (int.TryParse(parts[4], out var amount))
                {
                    var half = Math.Round((float)amount / 2, 1);
                    var oneHalf = Math.Round((float)amount * 1.5, 1);
                    var oneTenth = Math.Round((float)amount / 10, 2);
                    var threeQ = Math.Round((float)amount * 0.66, 1);
                    
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
                            description = $"\n\t Socket:  {parts[1]} (+{amount * 3}% Component Burn Reduction)\n"; 
                            break;
                        case ACE.Entity.Enum.MaterialType.GreenJade:
                            description = $"\n\t Socket:  {parts[1]} (+{half}% Prosperity)\n";
                            break;

                        // bracelet right
                        case ACE.Entity.Enum.MaterialType.Amethyst:
                            description = $"\n\t Socket:  {parts[1]} (+{oneHalf}% Ramping Magic Absorb)\n"; 
                            break;
                        case ACE.Entity.Enum.MaterialType.Diamond:
                            description = $"\n\t Socket:  {parts[1]} (+{oneHalf}% Ramping Physical Damage Resistance)\n"; 
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
                            description = $"\n\t Socket:  {parts[1]} (+{oneHalf}% Vitals Transfer)\n"; 
                            break;
                        case ACE.Entity.Enum.MaterialType.LavenderJade:
                            description = $"\n\t Socket:  {parts[1]} (+{oneHalf}% Selflessness)\n"; 
                            break;
                        case ACE.Entity.Enum.MaterialType.GreenGarnet:
                            description = $"\n\t Socket:  {parts[1]} (+{oneHalf}% Ramping War Damage)\n"; 
                            break;
                        case ACE.Entity.Enum.MaterialType.Tourmaline:
                            description = $"\n\t Socket:  {parts[1]} (+{oneHalf}% Ramping Ward Pen)\n"; 
                            break;
                        case ACE.Entity.Enum.MaterialType.WhiteJade:
                            description = $"\n\t Socket:  {parts[1]} (+{threeQ}% Restoration)\n"; 
                            break;
                        case ACE.Entity.Enum.MaterialType.Aquamarine:
                            description = $"\n\t Socket:  {parts[1]} (+{threeQ}% Frost Damage)\n"; 
                            break;
                        case ACE.Entity.Enum.MaterialType.BlackGarnet:
                            description = $"\n\t Socket:  {parts[1]} (+{amount}% Ramping Pierce Pen)\n";
                            break;
                        case ACE.Entity.Enum.MaterialType.Emerald:
                            description = $"\n\t Socket:  {parts[1]} (+{threeQ}% Acid Damage)\n";
                            break;
                        case ACE.Entity.Enum.MaterialType.ImperialTopaz:
                            description = $"\n\t Socket:  {parts[1]} (+{half}% Cleave Chance)\n";
                            break;
                        case ACE.Entity.Enum.MaterialType.Jet:
                            description = $"\n\t Socket:  {parts[1]} (+{threeQ}% Lightning Damage)\n";
                            break;
                        case ACE.Entity.Enum.MaterialType.RedGarnet:
                            description = $"\n\t Socket:  {parts[1]} (+{threeQ}% Fire Damage)\n";
                            break;
                        case ACE.Entity.Enum.MaterialType.WhiteSapphire:
                            description = $"\n\t Socket:  {parts[1]} (+{amount * 2}% Ramping Bludgeon Crit Damage)\n";
                            break;
                        default:   
                            description = $"\n\t Socket:  {parts[1]} (+{amount}%  {parts[3]})\n"; 
                            break;
                    }
                }       
            }
                    return description;
        }


        public static Dictionary<MaterialType, DamageType> MaterialDamage = new Dictionary<MaterialType, DamageType>
        {
            {ACE.Entity.Enum.MaterialType.Aquamarine, DamageType.Cold },
            {ACE.Entity.Enum.MaterialType.BlackGarnet, DamageType.Pierce },
            {ACE.Entity.Enum.MaterialType.Emerald, DamageType.Acid },
            {ACE.Entity.Enum.MaterialType.ImperialTopaz, DamageType.Slash },
            {ACE.Entity.Enum.MaterialType.Jet, DamageType.Electric },
            {ACE.Entity.Enum.MaterialType.RedGarnet, DamageType.Fire },
            {ACE.Entity.Enum.MaterialType.WhiteSapphire, DamageType.Bludgeon },

        };

        public static Dictionary<string, ACE.Entity.Enum.Properties.PropertyInt> StringToIntProperties = new Dictionary<string, ACE.Entity.Enum.Properties.PropertyInt>()
            {
                {"Strength", PropertyInt.GearStrength},
                {"Endurance", PropertyInt.GearEndurance},
                {"Coordination", PropertyInt.GearCoordination},
                {"Quickness", PropertyInt.GearQuickness},
                {"Focus", PropertyInt.GearFocus},
                {"Self", PropertyInt.GearSelf},
                {"Threat Enhancement", PropertyInt.GearThreatGain },
                {"Threat Reduction", PropertyInt.GearThreatReduction },
                {"Elemental Warding", PropertyInt.GearElementalWard },
                {"Physical Warding", PropertyInt.GearPhysicalWard },
                {"Block Rating", PropertyInt.GearBlock },
                {"Magic Find", PropertyInt.GearMagicFind },
                {"Item Mana Useage", PropertyInt.GearItemManaUseage },
                {"Shield Deflection", PropertyInt.GearThorns },
                {"Life Steal", PropertyInt.GearLifesteal },
                {"Blood Frenzy", PropertyInt.GearSelfHarm },
                {"Selflessness", PropertyInt.GearSelflessness },
                {"Focused Assault", PropertyInt.GearFamiliarity},
                {"Bravado", PropertyInt.GearBravado },
                {"Health To Stamina", PropertyInt.GearHealthToStamina },
                {"Health To Mana", PropertyInt.GearHealthToMana },
                {"Experience Gain", PropertyInt.GearExperienceGain },
                {"Last Stand", PropertyInt.GearLastStand },    
                {"Manasteal", PropertyInt.GearManasteal },
                {"Vitals Transfer", PropertyInt.GearVitalsTransfer },
                {"Bludgeon", PropertyInt.GearBludgeon },
                {"Pierce", PropertyInt.GearPierce },
                {"Slash", PropertyInt.GearSlash },
                {"Fire", PropertyInt.GearFire },
                {"Frost", PropertyInt.GearFrost },
                {"Acid", PropertyInt.GearAcid },
                {"Lightning", PropertyInt.GearLightning },
                {"Heal", PropertyInt.GearHealBubble },
                {"Components", PropertyInt.GearCompBurn },
                {"Nullification", PropertyInt.GearNullification },
                {"Ward Pen", PropertyInt.GearWardPen },
                {"Stamina Reduction", PropertyInt.GearStamReduction },
                {"Hardened Defense",PropertyInt.GearHardenedDefense },
                {"Prosperity", PropertyInt.GearPyrealFind },
                {"Familiarity", PropertyInt.GearFamiliarity },
                {"Reprisal", PropertyInt.GearReprisal },
                {"Elementalist", PropertyInt.GearElementalist }

            };


        public static Dictionary<int, string> JewelQuality = new Dictionary<int, string>
            {
                {1, "Scuffed"},
                {2, "Flawed"},
                {3, "Mediocre"},
                {4, "Fine"},
                {5, "Admirable"},
                {6, "Superior"},
                {7, "Excellent"},
                {8, "Magnificent"},
                {9, "Peerless"},
                {10, "Flawless"}
            };

        public static Dictionary<ACE.Entity.Enum.MaterialType?, int> JewelValidLocations = new Dictionary<ACE.Entity.Enum.MaterialType?, int>()
            {

                // weapon only
                {ACE.Entity.Enum.MaterialType.BlackOpal, 1},
                {ACE.Entity.Enum.MaterialType.FireOpal, 1},
                {ACE.Entity.Enum.MaterialType.YellowGarnet, 1},
                {ACE.Entity.Enum.MaterialType.ImperialTopaz, 1},
                {ACE.Entity.Enum.MaterialType.BlackGarnet, 1},
                {ACE.Entity.Enum.MaterialType.Jet, 1},
                {ACE.Entity.Enum.MaterialType.RedGarnet, 1},
                {ACE.Entity.Enum.MaterialType.Aquamarine, 1},
                {ACE.Entity.Enum.MaterialType.WhiteSapphire, 1},
                {ACE.Entity.Enum.MaterialType.Emerald, 1},
                {ACE.Entity.Enum.MaterialType.Tourmaline, 1},
                {ACE.Entity.Enum.MaterialType.Opal, 1},
                {ACE.Entity.Enum.MaterialType.RoseQuartz, 1},
                {ACE.Entity.Enum.MaterialType.Hematite, 1},
                {ACE.Entity.Enum.MaterialType.Bloodstone, 1},
                {ACE.Entity.Enum.MaterialType.Ruby, 1},
                {ACE.Entity.Enum.MaterialType.WhiteJade, 1},
                {ACE.Entity.Enum.MaterialType.GreenGarnet, 1},
                {ACE.Entity.Enum.MaterialType.LavenderJade, 1},

                // shield or melee weapons only
                {ACE.Entity.Enum.MaterialType.WhiteQuartz, 2},
                {ACE.Entity.Enum.MaterialType.Turquoise, 2},

                // bracelet only (left rest)
                {ACE.Entity.Enum.MaterialType.SmokeyQuartz, 196608},
                {ACE.Entity.Enum.MaterialType.Agate, 196608},
                {ACE.Entity.Enum.MaterialType.Moonstone, 196608},
                {ACE.Entity.Enum.MaterialType.Citrine, 196608},
                {ACE.Entity.Enum.MaterialType.LapisLazuli, 196608},
                {ACE.Entity.Enum.MaterialType.Malachite, 196608},
                {ACE.Entity.Enum.MaterialType.Amber, 196608},
                // bracelet only (right rest)
                {ACE.Entity.Enum.MaterialType.Zircon, 196608},
                {ACE.Entity.Enum.MaterialType.Diamond, 196608},
                {ACE.Entity.Enum.MaterialType.Onyx, 196608},
                {ACE.Entity.Enum.MaterialType.Amethyst, 196608},

                // ring only (left rest)
                {ACE.Entity.Enum.MaterialType.Peridot, 786432},
                {ACE.Entity.Enum.MaterialType.RedJade, 786432},
                {ACE.Entity.Enum.MaterialType.YellowTopaz, 786432},
                // ring only (right rest)
                {ACE.Entity.Enum.MaterialType.Carnelian, 786432},
                {ACE.Entity.Enum.MaterialType.Azurite, 786432},
                {ACE.Entity.Enum.MaterialType.TigerEye, 786432},

                // necklace only
                {ACE.Entity.Enum.MaterialType.Sapphire, 32768},
                {ACE.Entity.Enum.MaterialType.Sunstone, 32768},
                {ACE.Entity.Enum.MaterialType.GreenJade, 32768},


            };

        public static Dictionary<ACE.Entity.Enum.MaterialType?, int> JewelUiEffect = new Dictionary<ACE.Entity.Enum.MaterialType?, int>
            {
                {ACE.Entity.Enum.MaterialType.Diamond, 512},
                {ACE.Entity.Enum.MaterialType.Moonstone, 512},
                {ACE.Entity.Enum.MaterialType.WhiteJade, 512},
                {ACE.Entity.Enum.MaterialType.WhiteQuartz, 512},
                {ACE.Entity.Enum.MaterialType.WhiteSapphire, 512},
                {ACE.Entity.Enum.MaterialType.SmokeyQuartz, 512},
                {ACE.Entity.Enum.MaterialType.Zircon, 512},
                // Black
                {ACE.Entity.Enum.MaterialType.BlackGarnet, 512},
                {ACE.Entity.Enum.MaterialType.BlackOpal, 512},
                {ACE.Entity.Enum.MaterialType.Jet, 512},
                {ACE.Entity.Enum.MaterialType.Onyx, 512},
                // Gray
                {ACE.Entity.Enum.MaterialType.Agate, 512},
                {ACE.Entity.Enum.MaterialType.Hematite, 512},
                {ACE.Entity.Enum.MaterialType.Bloodstone, 512},
                // Red
                {ACE.Entity.Enum.MaterialType.Carnelian, 4},
                {ACE.Entity.Enum.MaterialType.FireOpal, 4},
                {ACE.Entity.Enum.MaterialType.RedGarnet, 4},
                {ACE.Entity.Enum.MaterialType.Ruby, 4},
                // Orange
                {ACE.Entity.Enum.MaterialType.Citrine, 16},
                {ACE.Entity.Enum.MaterialType.Sunstone, 16},
                // Yellow
                {ACE.Entity.Enum.MaterialType.Amber, 16},
                {ACE.Entity.Enum.MaterialType.ImperialTopaz, 16},
                {ACE.Entity.Enum.MaterialType.TigerEye, 16},
                {ACE.Entity.Enum.MaterialType.YellowGarnet, 16},
                {ACE.Entity.Enum.MaterialType.YellowTopaz, 16},
                // Green
                {ACE.Entity.Enum.MaterialType.Emerald, 256},
                {ACE.Entity.Enum.MaterialType.GreenGarnet, 256},
                {ACE.Entity.Enum.MaterialType.GreenJade, 256},
                {ACE.Entity.Enum.MaterialType.Malachite, 256},
                {ACE.Entity.Enum.MaterialType.Peridot, 256},
                // Blue
                {ACE.Entity.Enum.MaterialType.Aquamarine, 1},
                {ACE.Entity.Enum.MaterialType.Azurite, 1},
                {ACE.Entity.Enum.MaterialType.LapisLazuli, 1},
                {ACE.Entity.Enum.MaterialType.Opal, 1},
                {ACE.Entity.Enum.MaterialType.Sapphire, 1},
                {ACE.Entity.Enum.MaterialType.Tourmaline, 1},
                // Purple and Pink
                {ACE.Entity.Enum.MaterialType.Amethyst, 64},
                {ACE.Entity.Enum.MaterialType.LavenderJade, 64},
                {ACE.Entity.Enum.MaterialType.RedJade, 64},
                {ACE.Entity.Enum.MaterialType.RoseQuartz, 64},
                // Teal
                {ACE.Entity.Enum.MaterialType.Turquoise, 1}
            };

        public static Dictionary<string, uint> GemstoneIconMap = new Dictionary<string, uint>
            {
            {"Agate", 0x06002CAD},
            {"Amber", 0x06002CAE},
            {"Amethyst", 0x06002CAF},
            {"Aquamarine", 0x06002CB0},
            {"Azurite",0x06002CB1},
            {"Black Garnet", 0x06002CB2},
            {"Black Opal", 0x06002CB3},
            {"Bloodstone", 0x06002CA7},
            {"Carnelian", 0x06002CA8},
            {"Citrine", 0x06002CA9},
            {"Diamond", 0x06002CAA},
            {"Emerald", 0x06002CAB},
            {"Fire Opal", 0x06002CAC},
            {"Green Garnet", 0x06002CB4},
            {"Green Jade", 0x06002CB5},
            {"Hematite", 0x06002CB6},
            {"Imperial Topaz", 0x06002CB7},
            {"Jet", 0x06002CB8},
            {"Lapis Lazuli", 0x06002CB9},
            {"Lavender Jade", 0x06002CBA},
            {"Malachite", 0x06002CBB},
            {"Moonstone", 0x06002CBC},
            {"Onyx", 0x06002CBD},
            {"Opal", 0x06002CBE},
            {"Peridot", 0x06002CBF},
            {"Red Garnet", 0x06002CC0},
            {"Red Jade", 0x06002C98},
            {"Rose Quartz", 0x06002C99},
            {"Ruby", 0x06002C9A},
            {"Sapphire", 0x06002C9B},
            {"Smokey Quartz", 0x06002C9C},
            {"Sunstone", 0x06002C9D},
            {"Tiger Eye", 0x06002C9E},
            {"Tourmaline", 0x06002C9F},
            {"Turquoise", 0x06002CA0},
            {"White Jade", 0x06002CA1},
            {"White Quartz", 0x06002CA2},
            {"White Sapphire", 0x06002CA3},
            {"Yellow Garnet", 0x06002CA4},
            {"Yellow Topaz", 0x06002CA5},
            {"Zircon", 0x06002CA6}
            };

        public static Dictionary<ACE.Entity.Enum.MaterialType, string> StringtoMaterialType = new Dictionary<ACE.Entity.Enum.MaterialType, string>
        {
            {ACE.Entity.Enum.MaterialType.Unknown, "Unknown"},
            {ACE.Entity.Enum.MaterialType.Ceramic, "Ceramic"},
            {ACE.Entity.Enum.MaterialType.Porcelain, "Porcelain"},
            {ACE.Entity.Enum.MaterialType.Cloth, "Cloth"},
            {ACE.Entity.Enum.MaterialType.Linen, "Linen"},
            {ACE.Entity.Enum.MaterialType.Satin, "Satin"},
            {ACE.Entity.Enum.MaterialType.Silk, "Silk"},
            {ACE.Entity.Enum.MaterialType.Velvet, "Velvet"},
            {ACE.Entity.Enum.MaterialType.Wool, "Wool"},
            {ACE.Entity.Enum.MaterialType.Gem, "Gem"},
            {ACE.Entity.Enum.MaterialType.Agate, "Agate"},
            {ACE.Entity.Enum.MaterialType.Amber, "Amber"},
            {ACE.Entity.Enum.MaterialType.Amethyst, "Amethyst"},
            {ACE.Entity.Enum.MaterialType.Aquamarine, "Aquamarine"},
            {ACE.Entity.Enum.MaterialType.Azurite, "Azurite"},
            {ACE.Entity.Enum.MaterialType.BlackGarnet, "Black Garnet"},
            {ACE.Entity.Enum.MaterialType.BlackOpal, "Black Opal"},
            {ACE.Entity.Enum.MaterialType.Bloodstone, "Bloodstone"},
            {ACE.Entity.Enum.MaterialType.Carnelian, "Carnelian"},
            {ACE.Entity.Enum.MaterialType.Citrine, "Citrine"},
            {ACE.Entity.Enum.MaterialType.Diamond, "Diamond"},
            {ACE.Entity.Enum.MaterialType.Emerald, "Emerald"},
            {ACE.Entity.Enum.MaterialType.FireOpal, "Fire Opal"},
            {ACE.Entity.Enum.MaterialType.GreenGarnet, "Green Garnet"},
            {ACE.Entity.Enum.MaterialType.GreenJade, "Green Jade"},
            {ACE.Entity.Enum.MaterialType.Hematite, "Hematite"},
            {ACE.Entity.Enum.MaterialType.ImperialTopaz, "Imperial Topaz"},
            {ACE.Entity.Enum.MaterialType.Jet, "Jet"},
            {ACE.Entity.Enum.MaterialType.LapisLazuli, "Lapis Lazuli"},
            {ACE.Entity.Enum.MaterialType.LavenderJade, "Lavender Jade"},
            {ACE.Entity.Enum.MaterialType.Malachite, "Malachite"},
            {ACE.Entity.Enum.MaterialType.Moonstone, "Moonstone"},
            {ACE.Entity.Enum.MaterialType.Onyx, "Onyx"},
            {ACE.Entity.Enum.MaterialType.Opal, "Opal"},
            {ACE.Entity.Enum.MaterialType.Peridot, "Peridot"},
            {ACE.Entity.Enum.MaterialType.RedGarnet, "Red Garnet"},
            {ACE.Entity.Enum.MaterialType.RedJade, "Red Jade"},
            {ACE.Entity.Enum.MaterialType.RoseQuartz, "Rose Quartz"},
            {ACE.Entity.Enum.MaterialType.Ruby, "Ruby"},
            {ACE.Entity.Enum.MaterialType.Sapphire, "Sapphire"},
            {ACE.Entity.Enum.MaterialType.SmokeyQuartz, "Smokey Quartz"},
            {ACE.Entity.Enum.MaterialType.Sunstone, "Sunstone"},
            {ACE.Entity.Enum.MaterialType.TigerEye, "Tiger Eye"},
            {ACE.Entity.Enum.MaterialType.Tourmaline, "Tourmaline"},
            {ACE.Entity.Enum.MaterialType.Turquoise, "Turquoise"},
            {ACE.Entity.Enum.MaterialType.WhiteJade, "White Jade"},
            {ACE.Entity.Enum.MaterialType.WhiteQuartz, "White Quartz"},
            {ACE.Entity.Enum.MaterialType.WhiteSapphire, "White Sapphire"},
            {ACE.Entity.Enum.MaterialType.YellowGarnet, "Yellow Garnet"},
            {ACE.Entity.Enum.MaterialType.YellowTopaz, "Yellow Topaz"},
            {ACE.Entity.Enum.MaterialType.Zircon, "Zircon"},
            {ACE.Entity.Enum.MaterialType.Ivory, "Ivory"},
            {ACE.Entity.Enum.MaterialType.Leather, "Leather"},
            {ACE.Entity.Enum.MaterialType.ArmoredilloHide, "Armoredillo Hide"},
            {ACE.Entity.Enum.MaterialType.GromnieHide, "Gromnie Hide"},
            {ACE.Entity.Enum.MaterialType.ReedSharkHide, "Reed Shark Hide"},
            {ACE.Entity.Enum.MaterialType.Metal, "Metal"},
            {ACE.Entity.Enum.MaterialType.Brass, "Brass"},
            {ACE.Entity.Enum.MaterialType.Bronze, "Bronze"},
            {ACE.Entity.Enum.MaterialType.Copper, "Copper"},
            {ACE.Entity.Enum.MaterialType.Gold, "Gold"},
            {ACE.Entity.Enum.MaterialType.Iron, "Iron"},
            {ACE.Entity.Enum.MaterialType.Pyreal, "Pyreal"},
            {ACE.Entity.Enum.MaterialType.Silver, "Silver"},
            {ACE.Entity.Enum.MaterialType.Steel, "Steel"},
            {ACE.Entity.Enum.MaterialType.Stone, "Stone"},
            {ACE.Entity.Enum.MaterialType.Alabaster, "Alabaster"},
            {ACE.Entity.Enum.MaterialType.Granite, "Granite"},
            {ACE.Entity.Enum.MaterialType.Marble, "Marble"},
            {ACE.Entity.Enum.MaterialType.Obsidian, "Obsidian"},
            {ACE.Entity.Enum.MaterialType.Sandstone, "Sandstone"},
            {ACE.Entity.Enum.MaterialType.Serpentine, "Serpentine"},
            {ACE.Entity.Enum.MaterialType.Wood, "Wood"},
            {ACE.Entity.Enum.MaterialType.Ebony, "Ebony"},
            {ACE.Entity.Enum.MaterialType.Mahogany, "Mahogany"},
            {ACE.Entity.Enum.MaterialType.Oak, "Oak"},
            {ACE.Entity.Enum.MaterialType.Pine, "Pine"},
            {ACE.Entity.Enum.MaterialType.Teak, "Teak"}
        };

        public static Dictionary<string, ACE.Entity.Enum.MaterialType> MaterialTypetoString = new Dictionary<string, ACE.Entity.Enum.MaterialType>
            {
            {"Unknown", ACE.Entity.Enum.MaterialType.Unknown},
            {"Ceramic", ACE.Entity.Enum.MaterialType.Ceramic},
            {"Porcelain", ACE.Entity.Enum.MaterialType.Porcelain},
            {"Cloth", ACE.Entity.Enum.MaterialType.Cloth},
            {"Linen", ACE.Entity.Enum.MaterialType.Linen},
            {"Satin", ACE.Entity.Enum.MaterialType.Satin},
            {"Silk", ACE.Entity.Enum.MaterialType.Silk},
            {"Velvet", ACE.Entity.Enum.MaterialType.Velvet},
            {"Wool", ACE.Entity.Enum.MaterialType.Wool},
            {"Gem", ACE.Entity.Enum.MaterialType.Gem},
            {"Agate", ACE.Entity.Enum.MaterialType.Agate},
            {"Amber", ACE.Entity.Enum.MaterialType.Amber},
            {"Amethyst", ACE.Entity.Enum.MaterialType.Amethyst},
            {"Aquamarine", ACE.Entity.Enum.MaterialType.Aquamarine},
            {"Azurite", ACE.Entity.Enum.MaterialType.Azurite},
            {"Black Garnet", ACE.Entity.Enum.MaterialType.BlackGarnet},
            {"Black Opal", ACE.Entity.Enum.MaterialType.BlackOpal},
            {"Bloodstone", ACE.Entity.Enum.MaterialType.Bloodstone},
            {"Carnelian", ACE.Entity.Enum.MaterialType.Carnelian},
            {"Citrine", ACE.Entity.Enum.MaterialType.Citrine},
            {"Diamond", ACE.Entity.Enum.MaterialType.Diamond},
            {"Emerald", ACE.Entity.Enum.MaterialType.Emerald},
            {"Fire Opal", ACE.Entity.Enum.MaterialType.FireOpal},
            {"Green Garnet", ACE.Entity.Enum.MaterialType.GreenGarnet},
            {"Green Jade", ACE.Entity.Enum.MaterialType.GreenJade},
            {"Hematite", ACE.Entity.Enum.MaterialType.Hematite},
            {"Imperial Topaz", ACE.Entity.Enum.MaterialType.ImperialTopaz},
            {"Jet", ACE.Entity.Enum.MaterialType.Jet},
            {"Lapis Lazuli", ACE.Entity.Enum.MaterialType.LapisLazuli},
            {"Lavender Jade", ACE.Entity.Enum.MaterialType.LavenderJade},
            {"Malachite", ACE.Entity.Enum.MaterialType.Malachite},
            {"Moonstone", ACE.Entity.Enum.MaterialType.Moonstone},
            {"Onyx", ACE.Entity.Enum.MaterialType.Onyx},
            {"Opal", ACE.Entity.Enum.MaterialType.Opal},
            {"Peridot", ACE.Entity.Enum.MaterialType.Peridot},
            {"Red Garnet", ACE.Entity.Enum.MaterialType.RedGarnet},
            {"Red Jade", ACE.Entity.Enum.MaterialType.RedJade},
            {"Rose Quartz", ACE.Entity.Enum.MaterialType.RoseQuartz},
            {"Ruby", ACE.Entity.Enum.MaterialType.Ruby},
            {"Sapphire", ACE.Entity.Enum.MaterialType.Sapphire},
            {"Smokey Quartz", ACE.Entity.Enum.MaterialType.SmokeyQuartz},
            {"Sunstone", ACE.Entity.Enum.MaterialType.Sunstone},
            {"Tiger Eye", ACE.Entity.Enum.MaterialType.TigerEye},
            {"Tourmaline", ACE.Entity.Enum.MaterialType.Tourmaline},
            {"Turquoise", ACE.Entity.Enum.MaterialType.Turquoise},
            {"White Jade", ACE.Entity.Enum.MaterialType.WhiteJade},
            {"White Quartz", ACE.Entity.Enum.MaterialType.WhiteQuartz},
            {"White Sapphire", ACE.Entity.Enum.MaterialType.WhiteSapphire},
            {"Yellow Garnet", ACE.Entity.Enum.MaterialType.YellowGarnet},
            {"Yellow Topaz", ACE.Entity.Enum.MaterialType.YellowTopaz},
            {"Zircon", ACE.Entity.Enum.MaterialType.Zircon},
            {"Ivory", ACE.Entity.Enum.MaterialType.Ivory},
            {"Leather", ACE.Entity.Enum.MaterialType.Leather},
            {"Armoredillo Hide", ACE.Entity.Enum.MaterialType.ArmoredilloHide},
            {"Gromnie Hide", ACE.Entity.Enum.MaterialType.GromnieHide},
            {"Reed Shark Hide", ACE.Entity.Enum.MaterialType.ReedSharkHide},
            {"Metal", ACE.Entity.Enum.MaterialType.Metal},
            {"Brass", ACE.Entity.Enum.MaterialType.Brass},
            {"Bronze", ACE.Entity.Enum.MaterialType.Bronze},
            {"Copper", ACE.Entity.Enum.MaterialType.Copper},
            {"Gold", ACE.Entity.Enum.MaterialType.Gold},
            {"Iron", ACE.Entity.Enum.MaterialType.Iron},
            {"Pyreal", ACE.Entity.Enum.MaterialType.Pyreal},
            {"Silver", ACE.Entity.Enum.MaterialType.Silver},
            {"Steel", ACE.Entity.Enum.MaterialType.Steel},
            {"Stone", ACE.Entity.Enum.MaterialType.Stone},
            {"Alabaster", ACE.Entity.Enum.MaterialType.Alabaster},
            {"Granite", ACE.Entity.Enum.MaterialType.Granite},
            {"Marble", ACE.Entity.Enum.MaterialType.Marble},
            {"Obsidian", ACE.Entity.Enum.MaterialType.Obsidian},
            {"Sandstone", ACE.Entity.Enum.MaterialType.Sandstone},
            {"Serpentine", ACE.Entity.Enum.MaterialType.Serpentine},
            {"Wood", ACE.Entity.Enum.MaterialType.Wood},
            {"Ebony", ACE.Entity.Enum.MaterialType.Ebony},
            {"Mahogany", ACE.Entity.Enum.MaterialType.Mahogany},
            {"Oak", ACE.Entity.Enum.MaterialType.Oak},
            {"Pine", ACE.Entity.Enum.MaterialType.Pine},
            {"Teak", ACE.Entity.Enum.MaterialType.Teak}
            };
    }
}
