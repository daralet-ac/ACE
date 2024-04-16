using System;
using System.Collections.Generic;
using System.Linq;
using ACE.Common;
using ACE.Database.Models.World;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Entity;
using ACE.Server.Factories.Entity;
using ACE.Server.Factories.Enum;
using ACE.Server.Factories.Tables;
using ACE.Server.Managers;
using ACE.Server.WorldObjects;
using WeenieClassName = ACE.Entity.Enum.WeenieClassName;

namespace ACE.Server.Factories
{
    public static partial class LootGenerationFactory
    {
        private static WorldObject CreateArmor(TreasureDeath profile, bool isMagical, bool isArmor, TreasureItemType treasureItenmType = TreasureItemType.Undef, LootBias lootBias = LootBias.UnBiased, bool mutate = true)
        {
            var minType = 0;
            var maxType = 1;

            var armorWeenie = 0;

            if (treasureItenmType == TreasureItemType.ArmorWarrior)
            {
                switch (profile.Tier)
                {
                    case 1:
                    case 2:
                    default:
                        maxType = (int)LootTables.ArmorTypeWarrior.ChainmailArmor;
                        break;
                    case 3:
                    case 4:
                        maxType = (int)LootTables.ArmorTypeWarrior.ScalemailArmor;
                        break;
                    case 5:
                        maxType = (int)LootTables.ArmorTypeWarrior.CovenantArmor;
                        break;
                    case 6:
                        maxType = (int)LootTables.ArmorTypeWarrior.NariyidArmor;
                        break;
                    case 7:
                    case 8:
                        maxType = (int)LootTables.ArmorTypeWarrior.OlthoiCeldonArmor;
                        break;
                }
                LootTables.ArmorTypeWarrior armorType;

                armorType = (LootTables.ArmorTypeWarrior)ThreadSafeRandom.Next(minType, maxType);
                int[] table = LootTables.GetLootTable(armorType);
                int rng = ThreadSafeRandom.Next(0, table.Length - 1);

                armorWeenie = table[rng];
            }
            else if (treasureItenmType == TreasureItemType.ArmorRogue)
            {
                switch (profile.Tier)
                {
                    case 1:
                    case 2:
                    default:
                        maxType = (int)LootTables.ArmorTypeRogue.StuddedLeatherArmor;
                        break;
                    case 3:
                    case 4:
                        maxType = (int)LootTables.ArmorTypeRogue.YoroiArmor;
                        break;
                    case 5:
                        maxType = (int)LootTables.ArmorTypeRogue.KoujiaArmor;
                        break;
                    case 6:
                        maxType = (int)LootTables.ArmorTypeRogue.LoricaArmor;
                        break;
                    case 7:
                    case 8:
                        maxType = (int)LootTables.ArmorTypeRogue.OlthoiKoujiaArmor;
                        break;
                }

                LootTables.ArmorTypeRogue armorType;

                armorType = (LootTables.ArmorTypeRogue)ThreadSafeRandom.Next(minType, maxType);
                int[] table = LootTables.GetLootTable(armorType);
                int rng = ThreadSafeRandom.Next(0, table.Length - 1);

                armorWeenie = table[rng];
            }
            else if (treasureItenmType == TreasureItemType.ArmorCaster)
            {
                switch (profile.Tier)
                {
                    case 1:
                    case 2:
                    case 3:
                    case 4:
                    default:
                        maxType = (int)LootTables.ArmorTypeCaster.RobesAndCloth;
                        break;
                    case 5:
                        maxType = (int)LootTables.ArmorTypeCaster.AmuliArmor;
                        break;
                    case 6:
                        maxType = (int)LootTables.ArmorTypeCaster.ChiranArmor;
                        break;
                    case 7:
                    case 8:
                        maxType = (int)LootTables.ArmorTypeCaster.OlthoiAmuliArmor;
                        break;
                }

                LootTables.ArmorTypeCaster armorType;

                armorType = (LootTables.ArmorTypeCaster)ThreadSafeRandom.Next(minType, maxType);
                int[] table = LootTables.GetLootTable(armorType);
                int rng = ThreadSafeRandom.Next(0, table.Length - 1);

                armorWeenie = table[rng];
            }

            WorldObject wo = WorldObjectFactory.CreateNewWorldObject((uint)armorWeenie);

            if (wo != null && mutate)
                MutateArmor(wo, profile, isMagical);

            return wo;
        }

        private static void MutateArmor(WorldObject wo, TreasureDeath profile, bool isMagical, LootTables.ArmorType armorType = LootTables.ArmorType.Undef, TreasureRoll roll = null)
        {
            // material type
            var materialType = GetMaterialType(wo, profile.Tier);
            if (materialType > 0)
                wo.MaterialType = materialType;

            // item color
            MutateColor(wo);

            // gem count / gem material
            if (wo.GemCode != null)
                wo.GemCount = GemCountChance.Roll(wo.GemCode.Value, profile.Tier);
            else
                wo.GemCount = ThreadSafeRandom.Next(1, 6);

            wo.GemType = RollGemType(profile.Tier);

            // burden
            if (wo.HasMutateFilter(MutateFilter.EncumbranceVal))  // fixme: data
                MutateBurden(wo, profile, false);

            // weight class
            var armorWeightClass = GetArmorWeightClass(wo.WeenieClassId);
            wo.ArmorWeightClass = (int)armorWeightClass;

            // wield requirements (attribute, type, amount)
            wo.WieldSkillType = 0;

            if (profile.Tier > 0)
            {
                // clothing has a level requirement
                if (wo.ArmorWeightClass == (int)ArmorWeightClass.None)
                {
                    wo.WieldRequirements = WieldRequirement.Level;
                    wo.WieldDifficulty = GetArmorLevelReq(profile.Tier);
                }
                // armor req based on weight class
                else
                {
                    wo.WieldRequirements = WieldRequirement.RawAttrib;
                    wo.WieldSkillType = GetWeightClassAttributeReq((ArmorWeightClass)wo.ArmorWeightClass);
                    wo.WieldDifficulty = GetWieldDifficultyPerTier(profile.Tier);
                }
            }

            AssignArmorLevel(wo, profile.Tier, armorType);

            // Set Stamina/Mana Penalty
            var mod = GetArmorResourcePenalty(wo) * (wo.ArmorSlots ?? 1);
            wo.SetProperty(PropertyFloat.ArmorResourcePenalty, mod);

            // Spells
            if (isMagical)
            {
                AssignMagic(wo, profile, roll, true);
            }
            else
            {
                wo.ItemManaCost = null;
                wo.ItemMaxMana = null;
                wo.ItemCurMana = null;
                wo.ItemSpellcraft = null;
                wo.ItemDifficulty = null;
            }

            var totalSkillModPercentile = 0.0;
            var totalGearRatingPercentile = 0.0;
            if (roll != null)
            {
                TryMutateGearRating(wo, profile, roll, out totalGearRatingPercentile);

                TryMutateArmorSkillMod(wo, profile, roll, out totalSkillModPercentile);

                MutateArmorModVsType(wo, profile);
            }

            // workmanship
            //Console.WriteLine($"\n\n{wo.Name}");
            wo.ItemWorkmanship = GetArmorWorkmanship(wo, totalSkillModPercentile, totalGearRatingPercentile);

            // item value
            //if (wo.HasMutateFilter(MutateFilter.Value))   // fixme: data
            MutateValue(wo, profile.Tier, roll);

            wo.LongDesc = GetLongDesc(wo);

            if (wo.IsShield)
                AssignJewelSlots(wo);

        }

        /// <summary>
        /// Assign a final AL and Ward value based upon tier
        /// Used values given at https://asheron.fandom.com/wiki/Loot#Armor_Levels for setting the AL mod values
        /// so as to not exceed the values listed in that table
        /// </summary>
        private static void AssignArmorLevel(WorldObject wo, int tier, LootTables.ArmorType armorType)
        {
            if (wo.ArmorType == null)
            {
                _log.Warning($"[LOOT] Missing PropertyInt.ArmorType on loot item {wo.WeenieClassId} - {wo.Name}");
                return;
            }

            var baseArmorLevel = wo.ArmorLevel ?? 50;

                if (tier < 2)
                    return;

                var armorSlots = wo.ArmorSlots ?? 1;

                // Get Armor/Ward Level
                var baseWardLevel = wo.ArmorWeightClass == (int)ArmorWeightClass.Cloth ? 10 : 5;


                switch (wo.ArmorStyle)
                {
                    case (int)ArmorStyle.Amuli:
                    case (int)ArmorStyle.Chiran:
                    case (int)ArmorStyle.OlthoiAmuli:
                        baseArmorLevel = 75;
                        baseWardLevel = 7;
                        break;
                    case (int)ArmorStyle.Leather:
                    case (int)ArmorStyle.Yoroi:
                    case (int)ArmorStyle.Lorica:
                        baseArmorLevel = 75;
                        break;
                    case (int)ArmorStyle.StuddedLeather:
                    case (int)ArmorStyle.Koujia:
                    case (int)ArmorStyle.OlthoiKoujia:
                        baseArmorLevel = 90;
                        break;
                    case (int)ArmorStyle.Chainmail:
                    case (int)ArmorStyle.Scalemail:
                    case (int)ArmorStyle.Nariyid:
                        baseArmorLevel = 100;
                        break;
                    case (int)ArmorStyle.Platemail:
                    case (int)ArmorStyle.Celdon:
                    case (int)ArmorStyle.OlthoiCeldon:
                        baseArmorLevel = 110;
                        break;
                    case (int)ArmorStyle.Covenant:
                    case (int)ArmorStyle.OlthoiArmor:
                        baseArmorLevel = 125;
                        break;
                }

                switch ((int)wo.WeenieClassId)
                {
                    case (int)WeenieClassName.W_BUCKLER_CLASS: // Buckler
                        baseArmorLevel = 75;
                        baseWardLevel = 5;
                        break;
                    case (int)WeenieClassName.W_SHIELDKITE_CLASS: // Kite Shield
                    case (int)WeenieClassName.W_SHIELDROUND_CLASS: // Round Shield
                        baseArmorLevel = 100;
                        baseWardLevel = 6;
                        break;
                    case (int)WeenieClassName.W_SHIELDKITELARGE_CLASS: // Large Kite Shield
                    case (int)WeenieClassName.W_SHIELDROUNDLARGE_CLASS: // Large Round Shield
                        baseArmorLevel = 105;
                        baseWardLevel = 7;
                        break;
                    case (int)WeenieClassName.W_SHIELDTOWER_CLASS: // Tower Shield
                        baseArmorLevel = 110;
                        baseWardLevel = 8;
                     break;
                    case (int)WeenieClassName.W_SHIELDCOVENANT_CLASS: // Covenant Shield
                        baseArmorLevel = 125;
                        baseWardLevel = 10;
                        break;
            }

                // Add some variance (+/- 10%)
                var variance = 1.0f + ThreadSafeRandom.Next(-0.1f, 0.1f);

                // Final Calculation
                var newArmorLevel = baseArmorLevel * (tier - 1) * variance;
                var newWardLevel = baseWardLevel * (tier - 1) * armorSlots * variance;

                // Assign levels
                wo.SetProperty(PropertyInt.ArmorLevel, (int)newArmorLevel);
                wo.SetProperty(PropertyInt.WardLevel, (int)newWardLevel);

            if ((wo.ResistMagic == null || wo.ResistMagic < 9999) && wo.ArmorLevel >= 1000)
                _log.Warning($"[LOOT] Standard armor item exceeding upper AL threshold {wo.WeenieClassId} - {wo.Name}");
        }

        private static void AssignArmorLevelCompat(WorldObject wo, int tier, LootTables.ArmorType armorType)
        {
            _log.Debug($"[LOOT] Using AL Assignment Compatibility layer for item {wo.WeenieClassId} - {wo.Name}.");

            var baseArmorLevel = wo.ArmorLevel ?? 0;

            if (baseArmorLevel > 0)
            {
                int armorModValue = 0;

                if (armorType > LootTables.ArmorType.HaebreanArmor && armorType <= LootTables.ArmorType.OlthoiAlduressaArmor)
                {
                    // Even if most are not using T8, made a change to that outcome to ensure that Olthoi Alduressa doesn't go way out of spec
                    // Side effect is that Haebrean to Olthoi Celdon may suffer
                    armorModValue = tier switch
                    {
                        7 => ThreadSafeRandom.Next(0, 40),
                        8 => ThreadSafeRandom.Next(91, 115),
                        _ => 0,
                    };
                }
                else
                {
                    switch (tier)
                    {
                        case 1:
                            if (armorType == LootTables.ArmorType.StuddedLeatherArmor
                             || armorType == LootTables.ArmorType.Helms
                             || armorType == LootTables.ArmorType.Shields)
                                armorModValue = ThreadSafeRandom.Next(0, 27);

                            else if (armorType == LootTables.ArmorType.LeatherArmor
                                  || armorType == LootTables.ArmorType.MiscClothing)
                                armorModValue = ThreadSafeRandom.Next(0, 23);

                            else
                                armorModValue = ThreadSafeRandom.Next(0, 40);
                            break;
                        case 2:
                            if (armorType == LootTables.ArmorType.StuddedLeatherArmor
                             || armorType == LootTables.ArmorType.Helms
                             || armorType == LootTables.ArmorType.Shields)
                                armorModValue = ThreadSafeRandom.Next(27, 54);

                            else if (armorType == LootTables.ArmorType.LeatherArmor
                                  || armorType == LootTables.ArmorType.MiscClothing)
                                armorModValue = ThreadSafeRandom.Next(23, 46);

                            else
                                armorModValue = ThreadSafeRandom.Next(40, 80);
                            break;
                        case 3:
                            if (armorType == LootTables.ArmorType.StuddedLeatherArmor
                             || armorType == LootTables.ArmorType.Helms
                             || armorType == LootTables.ArmorType.Shields)
                                armorModValue = ThreadSafeRandom.Next(54, 81);

                            else if (armorType == LootTables.ArmorType.LeatherArmor
                                  || armorType == LootTables.ArmorType.MiscClothing)
                                armorModValue = ThreadSafeRandom.Next(46, 69);

                            else if (armorType == LootTables.ArmorType.CovenantArmor || armorType == LootTables.ArmorType.OlthoiArmor)
                                armorModValue = ThreadSafeRandom.Next(90, 130);

                            else
                                armorModValue = ThreadSafeRandom.Next(80, 120);
                            break;
                        case 4:
                            if (armorType == LootTables.ArmorType.StuddedLeatherArmor
                             || armorType == LootTables.ArmorType.Helms
                             || armorType == LootTables.ArmorType.Shields)
                                armorModValue = ThreadSafeRandom.Next(81, 108);

                            else if (armorType == LootTables.ArmorType.LeatherArmor
                                  || armorType == LootTables.ArmorType.MiscClothing)
                                armorModValue = ThreadSafeRandom.Next(69, 92);

                            else if (armorType == LootTables.ArmorType.CovenantArmor || armorType == LootTables.ArmorType.OlthoiArmor)
                                armorModValue = ThreadSafeRandom.Next(130, 170);

                            else
                                armorModValue = ThreadSafeRandom.Next(120, 160);
                            break;
                        case 5:
                            if (armorType == LootTables.ArmorType.StuddedLeatherArmor
                             || armorType == LootTables.ArmorType.Helms
                             || armorType == LootTables.ArmorType.Shields)
                                armorModValue = ThreadSafeRandom.Next(108, 135);

                            else if (armorType == LootTables.ArmorType.LeatherArmor
                                  || armorType == LootTables.ArmorType.MiscClothing)
                                armorModValue = ThreadSafeRandom.Next(92, 115);

                            else if (armorType == LootTables.ArmorType.CovenantArmor || armorType == LootTables.ArmorType.OlthoiArmor)
                                armorModValue = ThreadSafeRandom.Next(170, 210);

                            else
                                armorModValue = ThreadSafeRandom.Next(160, 200);
                            break;
                        case 6:
                            if (armorType == LootTables.ArmorType.StuddedLeatherArmor
                             || armorType == LootTables.ArmorType.Helms
                             || armorType == LootTables.ArmorType.Shields)
                                armorModValue = ThreadSafeRandom.Next(135, 162);

                            else if (armorType == LootTables.ArmorType.LeatherArmor
                                  || armorType == LootTables.ArmorType.MiscClothing)
                                armorModValue = ThreadSafeRandom.Next(115, 138);

                            else if (armorType == LootTables.ArmorType.CovenantArmor || armorType == LootTables.ArmorType.OlthoiArmor)
                                armorModValue = ThreadSafeRandom.Next(210, 250);

                            else
                                armorModValue = ThreadSafeRandom.Next(200, 240);
                            break;
                        case 7:
                            if (armorType == LootTables.ArmorType.StuddedLeatherArmor
                             || armorType == LootTables.ArmorType.Helms
                             || armorType == LootTables.ArmorType.Shields)
                                armorModValue = ThreadSafeRandom.Next(162, 189);

                            else if (armorType == LootTables.ArmorType.LeatherArmor
                                  || armorType == LootTables.ArmorType.MiscClothing)
                                armorModValue = ThreadSafeRandom.Next(138, 161);

                            else if (armorType == LootTables.ArmorType.CovenantArmor || armorType == LootTables.ArmorType.OlthoiArmor)
                                armorModValue = ThreadSafeRandom.Next(250, 290);

                            else
                                armorModValue = ThreadSafeRandom.Next(240, 280);
                            break;
                        case 8:
                            if (armorType == LootTables.ArmorType.StuddedLeatherArmor
                             || armorType == LootTables.ArmorType.Helms
                             || armorType == LootTables.ArmorType.Shields)
                                armorModValue = ThreadSafeRandom.Next(189, 216);

                            else if (armorType == LootTables.ArmorType.LeatherArmor
                                || armorType == LootTables.ArmorType.MiscClothing)
                                armorModValue = ThreadSafeRandom.Next(161, 184);

                            else if (armorType == LootTables.ArmorType.CovenantArmor || armorType == LootTables.ArmorType.OlthoiArmor)
                                armorModValue = ThreadSafeRandom.Next(290, 330);

                            else if (armorType == LootTables.ArmorType.SocietyArmor)
                                armorModValue = ThreadSafeRandom.Next(189, 216);
                            else
                                armorModValue = ThreadSafeRandom.Next(280, 320);
                            break;
                        default:
                            armorModValue = 0;
                            break;
                    }
                }

                int adjustedArmorLevel = baseArmorLevel + armorModValue;
                wo.ArmorLevel = adjustedArmorLevel;
            }
        }

        private static int GetCovenantWieldReq(int tier, Skill skill)
        {
            var index = tier switch
            {
                3 => ThreadSafeRandom.Next(1, 3),
                4 => ThreadSafeRandom.Next(1, 4),
                5 => ThreadSafeRandom.Next(1, 5),
                6 => ThreadSafeRandom.Next(1, 6),
                7 => ThreadSafeRandom.Next(1, 7),
                _ => ThreadSafeRandom.Next(1, 8),
            };

            var wield = skill switch
            {
                Skill.MagicDefense => index switch
                {
                    1 => 145,
                    2 => 185,
                    3 => 225,
                    4 => 245,
                    5 => 270,
                    6 => 290,
                    7 => 310,
                    _ => 320,
                },
                Skill.MissileDefense => index switch
                {
                    1 => 160,
                    2 => 205,
                    3 => 245,
                    4 => 270,
                    5 => 290,
                    6 => 305,
                    7 => 330,
                    _ => 340,
                },
                _ => index switch
                {
                    1 => 200,
                    2 => 250,
                    3 => 300,
                    4 => 325,
                    5 => 350,
                    6 => 370,
                    7 => 400,
                    _ => 410,
                },
            };
            return wield;
        }

        private static bool TryRollEquipmentSet(WorldObject wo, TreasureDeath profile, TreasureRoll roll)
        {
            if (roll == null)
            {
                if (!PropertyManager.GetBool("equipmentsetid_enabled").Item)
                    return false;

                if (profile.Tier < 6 || !wo.HasArmorLevel())
                    return false;

                if (wo.ClothingPriority == null || (wo.ClothingPriority & (CoverageMask)CoverageMaskHelper.Outerwear) == 0)
                    return false;

                var dropRate = PropertyManager.GetDouble("equipmentsetid_drop_rate").Item;
                var dropRateMod = 1.0 / dropRate;

                var lootQualityMod = 1.0f;
                if (PropertyManager.GetBool("loot_quality_mod").Item)
                    lootQualityMod = 1.0f - profile.LootQualityMod;

                // initial base 10% chance to add a random EquipmentSet, which can be adjusted via equipmentsetid_drop_rate
                var rng = ThreadSafeRandom.Next(1, (int)(100 * dropRateMod * lootQualityMod));
                if (rng > 10) return false;

                wo.EquipmentSetId = (EquipmentSet)ThreadSafeRandom.Next((int)EquipmentSet.Soldiers, (int)EquipmentSet.Lightningproof);
            }
            else
            {
                wo.EquipmentSetId = EquipmentSetChance.Roll(wo, profile, roll);
            }

            if (wo.EquipmentSetId != null && PropertyManager.GetBool("equipmentsetid_name_decoration").Item)
            {
                var equipSetId = wo.EquipmentSetId;

                var equipSetName = equipSetId.ToString();

                if (equipSetId >= EquipmentSet.Soldiers && equipSetId <= EquipmentSet.Crafters)
                    equipSetName = equipSetName.TrimEnd('s') + "'s";

                wo.Name = $"{equipSetName} {wo.Name}";
            }
            return true;
        }

        private static WorldObject CreateSocietyArmor(TreasureDeath profile, bool mutate = true)
        {
            int society = 0;
            int armortype = 0;

            if (profile.TreasureType >= 2971 && profile.TreasureType <= 2980)
                society = 0; // CH
            else if (profile.TreasureType >= 2981 && profile.TreasureType <= 2990)
                society = 1; // EW
            else if (profile.TreasureType >= 2991 && profile.TreasureType <= 3000)
                society = 2; // RB

            switch (profile.TreasureType)
            {
                case 2971:
                case 2981:
                case 2991:
                    armortype = 0; // BP
                    break;
                case 2972:
                case 2982:
                case 2992:
                    armortype = 1; // Gauntlets
                    break;
                case 2973:
                case 2983:
                case 2993:
                    armortype = 2; // Girth
                    break;
                case 2974:
                case 2984:
                case 2994:
                    armortype = 3; // Greaves
                    break;
                case 2975:
                case 2985:
                case 2995:
                    armortype = 4; // Helm
                    break;
                case 2976:
                case 2986:
                case 2996:
                    armortype = 5; // Pauldrons
                    break;
                case 2977:
                case 2987:
                case 2997:
                    armortype = 6; // Tassets
                    break;
                case 2978:
                case 2988:
                case 2998:
                    armortype = 7; // Vambraces
                    break;
                case 2979:
                case 2989:
                case 2999:
                    armortype = 8; // Sollerets
                    break;
                default:
                    break;
            }

            int societyArmorWeenie = LootTables.SocietyArmorMatrix[armortype][society];
            WorldObject wo = WorldObjectFactory.CreateNewWorldObject((uint)societyArmorWeenie);

            if (wo != null && mutate)
                MutateSocietyArmor(wo, profile, true);

            return wo;
        }

        private static void MutateSocietyArmor(WorldObject wo, TreasureDeath profile, bool isMagical, TreasureRoll roll = null)
        {
            // why is this a separate method??

            var materialType = GetMaterialType(wo, profile.Tier);
            if (materialType > 0)
                wo.MaterialType = materialType;

            if (wo.GemCode != null)
                wo.GemCount = GemCountChance.Roll(wo.GemCode.Value, profile.Tier);
            else
                wo.GemCount = ThreadSafeRandom.Next(1, 6);

            wo.GemType = RollGemType(profile.Tier);

            wo.ItemWorkmanship = WorkmanshipChance.Roll(profile.Tier, profile.LootQualityMod);

            wo.Value = Roll_ItemValue(wo, profile.Tier);

            if (isMagical)
            {
                // looks like society armor always had impen on it
                AssignMagic(wo, profile, roll, true);
            }
            else
            {
                wo.ItemManaCost = null;
                wo.ItemMaxMana = null;
                wo.ItemCurMana = null;
                wo.ItemSpellcraft = null;
                wo.ItemDifficulty = null;
            }
            AssignArmorLevel(wo, profile.Tier, LootTables.ArmorType.SocietyArmor);

            wo.LongDesc = GetLongDesc(wo);

            // try mutate burden, if MutateFilter exists
            if (wo.HasMutateFilter(MutateFilter.EncumbranceVal))
                MutateBurden(wo, profile, false);
        }

        private static WorldObject CreateCloak(TreasureDeath profile, bool mutate = true)
        {
            // even chance between 11 different types of cloaks
            var cloakType = ThreadSafeRandom.Next(0, LootTables.Cloaks.Length - 1);

            var cloakWeenie  = LootTables.Cloaks[cloakType];

            var wo = WorldObjectFactory.CreateNewWorldObject((uint)cloakWeenie);

            if (wo != null && mutate)
                MutateCloak(wo, profile);

            return wo;
        }

        private static void MutateCloak(WorldObject wo, TreasureDeath profile, TreasureRoll roll = null)
        {
            wo.ItemMaxLevel = CloakChance.Roll_ItemMaxLevel(profile);

            // wield difficulty, based on ItemMaxLevel
            switch (wo.ItemMaxLevel)
            {
                case 1:
                    wo.WieldDifficulty = 30;
                    break;
                case 2:
                    wo.WieldDifficulty = 60;
                    break;
                case 3:
                    wo.WieldDifficulty = 90;
                    break;
                case 4:
                    wo.WieldDifficulty = 120;
                    break;
                case 5:
                    wo.WieldDifficulty = 150;
                    break;
            }

            wo.IconOverlayId = IconOverlay_ItemMaxLevel[wo.ItemMaxLevel.Value - 1];

            // equipment set
            wo.EquipmentSetId = CloakChance.RollEquipmentSet();

            // proc spell
            var surgeSpell = CloakChance.RollProcSpell();

            if (surgeSpell != SpellId.Undef)
            {
                wo.ProcSpell = (uint)surgeSpell;

                // Cloaked In Skill is the only self-targeted spell
                if (wo.ProcSpell == (uint)SpellId.CloakAllSkill)
                    wo.ProcSpellSelfTargeted = true;
                else
                    wo.ProcSpellSelfTargeted = false;

                wo.CloakWeaveProc = 1;
            }
            else
            {
                // Damage Reduction proc
                wo.CloakWeaveProc = 2;
            }

            // material type
            wo.MaterialType = GetMaterialType(wo, profile.Tier);

            // workmanship
            wo.Workmanship = WorkmanshipChance.Roll(profile.Tier, profile.LootQualityMod);

            if (roll != null && profile.Tier == 8)
                TryMutateGearRating(wo, profile, roll, out var totalGearRatingPercentile);

            // item value
            //if (wo.HasMutateFilter(MutateFilter.Value))
                MutateValue(wo, profile.Tier, roll);
        }

        private static int RollCloak_ItemMaxLevel(TreasureDeath profile)
        {
            //  These Values are just for starting off.  I haven't gotten the numbers yet to confirm these.
            int cloakLevel = 1;

            int chance = ThreadSafeRandom.Next(1, 1000);
            switch (profile.Tier)
            {
                case 1:
                case 2:
                default:                
                    cloakLevel = 1;
                    break;
                case 3:
                case 4:
                    if (chance <= 440)
                        cloakLevel = 1;
                    else
                        cloakLevel = 2;
                    break;
                case 5:
                    if (chance <= 250)
                        cloakLevel = 1;
                    else if (chance <= 700)
                        cloakLevel = 2;
                    else
                        cloakLevel = 3;
                    break;
                case 6:
                    if (chance <= 36)
                        cloakLevel = 1;
                    else if (chance <= 357)
                        cloakLevel = 2;
                    else if (chance <= 990)
                        cloakLevel = 3;
                    else
                        cloakLevel = 4;
                    break;
                case 7:  // From data, no chance to get a lvl 1 cloak
                    if (chance <= 463)
                        cloakLevel = 2;
                    else if (chance <= 945)
                        cloakLevel = 3;
                    else if (chance <= 984)
                        cloakLevel = 4;
                    else
                        cloakLevel = 5;
                    break;
                case 8:  // From data, no chance to get a lvl 1 cloak
                    if (chance <= 451)
                        cloakLevel = 2;
                    else if (chance <= 920)
                        cloakLevel = 3;
                    else if (chance <= 975)
                        cloakLevel = 4;
                    else
                        cloakLevel = 5;
                    break;
            }
            return cloakLevel;
        }

        private static bool GetMutateCloakData(uint wcid)
        {
            return LootTables.Cloaks.Contains((int)wcid);
        }

        private static void MutateValue_Armor(WorldObject wo)
        {
            var bulkMod = wo.BulkMod ?? 1.0f;
            var sizeMod = wo.SizeMod ?? 1.0f;

            var armorLevel = wo.ArmorLevel ?? 0;

            wo.Value += (int)(armorLevel * bulkMod * sizeMod);
        }

        private static void MutateArmorModVsType(WorldObject wo, TreasureDeath profile)
        {
            // for the PropertyInt.MutateFilters found in py16 data,
            // items either had all of these, or none of these

            // only the elemental types could mutate
            TryMutateArmorModVsType(wo, profile, PropertyFloat.ArmorModVsFire);
            TryMutateArmorModVsType(wo, profile, PropertyFloat.ArmorModVsCold);
            TryMutateArmorModVsType(wo, profile, PropertyFloat.ArmorModVsAcid);
            TryMutateArmorModVsType(wo, profile, PropertyFloat.ArmorModVsElectric);
            TryMutateArmorModVsType(wo, profile, PropertyFloat.ArmorModVsSlash);
            TryMutateArmorModVsType(wo, profile, PropertyFloat.ArmorModVsPierce);
            TryMutateArmorModVsType(wo, profile, PropertyFloat.ArmorModVsBludgeon);
        }

        private static bool TryMutateArmorModVsType(WorldObject wo, TreasureDeath profile, PropertyFloat prop)
        {
            var armorModVsType = wo.GetProperty(prop);

            if (armorModVsType == null)
                return false;

            var baseMod = wo.GetProperty(prop) ?? 0;

            // Let's roll a value between -0.75 and 0.5, with lower chances of rolling a higher value. We will add this value to the base ArmorMod.
            // If base ArmorMod is 1, the final mod will range from 0.25 (poor) to 1.5 (above average).
            var roll = ThreadSafeRandom.Next(-1.25f, 1.0f);
            roll *= Math.Abs(roll);
            roll *= 0.5f;
            roll = Math.Max(roll, -0.75f);

            var newMod = (double)baseMod + roll;

            wo.SetProperty(prop, newMod);

            return true;
        }

        private static bool TryMutateGearRating(WorldObject wo, TreasureDeath profile, TreasureRoll roll, out double totalGearRatingPercentile)
        {
            totalGearRatingPercentile = 0;

            if (profile.Tier < 6)
                return false;

            var tier = profile.Tier;
            var weightType = wo.ArmorWeightClass;

            var gearRatingAmount1 = GetGearRatingAmount(tier, profile, out var gearRatingPercentile1);
            var gearRatingAmount2 = GetGearRatingAmount(tier, profile, out var gearRatingPercentile2);

            totalGearRatingPercentile += gearRatingPercentile1;
            totalGearRatingPercentile += gearRatingPercentile2;
            totalGearRatingPercentile /= 2;

            if (gearRatingAmount1 == 0 && gearRatingAmount2 == 0)
                return false;

            var armorSlots = wo.ArmorSlots ?? 1;

            if (weightType == (int)ArmorWeightClass.Cloth)
            {
                wo.GearDamage = gearRatingAmount1 * armorSlots;
                wo.GearHealingBoost = gearRatingAmount2 * armorSlots;
            }
            else if (weightType == (int)ArmorWeightClass.Light)
            {
                wo.GearCritDamage = gearRatingAmount1 * armorSlots;
                wo.GearCrit = gearRatingAmount2 * armorSlots;
            }
            else if (weightType == (int)ArmorWeightClass.Heavy)
            {
                wo.GearDamageResist = gearRatingAmount1 * armorSlots;
                wo.GearCritResist = (gearRatingAmount2 + 1) * armorSlots;
            }
            else if (roll.ItemType == TreasureItemType_Orig.Clothing)
            {
                return false;
            }
            else
            {
                _log.Error($"TryMutateGearRating({wo.Name}, {weightType}): unknown weight class");
                return false;
            }

            return true;
        }

        private static bool TryMutateArmorSkillMod(WorldObject wo, TreasureDeath profile, TreasureRoll roll, out double highestModPercentile)
        {
            highestModPercentile = 0.0f;
            var modPercentile = 0.0f;

            var qualityMod = profile.LootQualityMod != 0.0f ? profile.LootQualityMod : 0.0f;

            var potentialTypes = new List<int>();
            var numTypes = wo.ArmorType == (int)LootTables.ArmorType.MiscClothing ? 3 : 5;
            for (int i = 1; i <= numTypes; i++)
                potentialTypes.Add(i);
            var rolledTypes = GetRolledTypes(potentialTypes, qualityMod);

            float numRolledTypesMultiplier;
            switch (rolledTypes.Count)
            {
                default:
                case 1: numRolledTypesMultiplier = 1.0f; break; // 100% per mod, 100% total.
                case 2: numRolledTypesMultiplier = 0.75f; break; // 75% per mod, 150% total.
                case 3: numRolledTypesMultiplier = 0.5833f; break; // 58.33% per mod, 175% total.
                case 4: numRolledTypesMultiplier = 0.475f; break; // 47.5% per mod, 190% total.
                case 5: numRolledTypesMultiplier = 0.4f; break; // 40% per mod, 200% total.
            }

            var weightType = wo.ArmorWeightClass;
            var armorSlotsMod = (wo.ArmorSlots ?? 1.0f) / 10;

            // roll mod values for types
            if (wo.ArmorType == (int)LootTables.ArmorType.MiscClothing && wo.ArmorWeightClass == 0)
            {
                var miscClothingMultiplier = 0.5f;

                foreach (var type in rolledTypes)
                {
                    var amount = GetArmorSkillAmount(profile, wo, out modPercentile) * numRolledTypesMultiplier * miscClothingMultiplier;
                    highestModPercentile = modPercentile > highestModPercentile ? modPercentile : highestModPercentile;

                    switch (type)
                    {
                        case 1: wo.ArmorHealthMod = amount; break;
                        case 2: wo.ArmorStaminaMod = amount; break;
                        case 3: wo.ArmorManaMod = amount; break;
                    }
                }

            }
            else if (weightType == (int)ArmorWeightClass.Cloth)
            {
                wo.ArmorWarMagicMod = 0.0;
                wo.ArmorLifeMagicMod = 0.0;
                wo.ArmorPerceptionMod = 0.0;
                wo.ArmorDeceptionMod = 0.0;
                wo.ArmorManaRegenMod = 0.0;
                wo.ManaConversionMod = 0.0;

                foreach (var type in rolledTypes)
                {
                    var amount = GetArmorSkillAmount(profile, wo, out modPercentile) * numRolledTypesMultiplier * armorSlotsMod;
                    highestModPercentile = modPercentile > highestModPercentile ? modPercentile : highestModPercentile;

                    switch (type)
                    {
                        case 1: wo.ArmorWarMagicMod = amount; break;
                        case 2: wo.ArmorLifeMagicMod = amount; break;
                        case 3:
                            if (ThreadSafeRandom.Next(0, 1) == 0)
                                wo.ArmorPerceptionMod = amount;
                            else
                                wo.ArmorDeceptionMod = amount;
                            break;
                        case 4: wo.ArmorManaRegenMod = amount; break;
                        case 5: wo.ManaConversionMod = amount; break;
                    }
                }
            }
            else if (weightType == (int)ArmorWeightClass.Light)
            {
                wo.ArmorAttackMod = 0.0;
                wo.ArmorDualWieldMod = 0.0;
                wo.ArmorShieldMod = 0.0;
                wo.ArmorThieveryMod = 0.0;
                wo.ArmorRunMod = 0.0;
                wo.ArmorStaminaMod = 0.0;
                wo.ArmorPerceptionMod = 0.0;
                wo.ArmorDeceptionMod = 0.0;

                foreach (var type in rolledTypes)
                {
                    var amount = GetArmorSkillAmount(profile, wo, out modPercentile) * numRolledTypesMultiplier * armorSlotsMod;
                    highestModPercentile = modPercentile > highestModPercentile ? modPercentile : highestModPercentile;

                    switch (type)
                    {
                        case 1: wo.ArmorAttackMod = amount; break;
                        case 2:
                            if (IsShieldWcid(wo))
                                wo.ArmorShieldMod = amount;
                            else
                                wo.ArmorDualWieldMod = amount;
                            break;
                        case 3:
                            if (ThreadSafeRandom.Next(0, 1) == 0)
                                wo.ArmorThieveryMod = amount;
                            else
                                wo.ArmorRunMod = amount;
                            break;
                        case 4: wo.ArmorStaminaRegenMod = amount; break;
                        case 5:
                            if (ThreadSafeRandom.Next(0, 1) == 0)
                                wo.ArmorPerceptionMod = amount;
                            else
                                wo.ArmorDeceptionMod = amount;
                            break;
                    }
                }
            }
            else if (weightType == (int)ArmorWeightClass.Heavy)
            {
                wo.ArmorAttackMod = 0.0;
                wo.ArmorPhysicalDefMod = 0.0;
                wo.ArmorMagicDefMod = 0.0;
                wo.ArmorShieldMod = 0.0;
                wo.ArmorTwohandedCombatMod = 0.0;
                wo.ArmorPerceptionMod = 0.0;
                wo.ArmorDeceptionMod = 0.0;
                wo.ArmorHealthRegenMod = 0.0;

                foreach (var type in rolledTypes)
                {
                    var amount = GetArmorSkillAmount(profile, wo, out modPercentile) * numRolledTypesMultiplier * armorSlotsMod;
                    highestModPercentile = modPercentile > highestModPercentile ? modPercentile : highestModPercentile;

                    switch (type)
                    {
                        case 1:
                            wo.ArmorAttackMod = amount; break;
                        case 2:
                            wo.ArmorPhysicalDefMod = amount;
                            wo.ArmorMagicDefMod = amount; break;
                        case 3:
                            if (IsShieldWcid(wo) || ThreadSafeRandom.Next(0, 1) == 0)
                                wo.ArmorShieldMod = amount;
                            else
                                wo.ArmorTwohandedCombatMod = amount;
                            break;
                        case 4:
                            if (ThreadSafeRandom.Next(0, 1) == 0)
                                wo.ArmorPerceptionMod = amount;
                            else
                                wo.ArmorDeceptionMod = amount;
                            break;
                        case 5:
                            wo.ArmorHealthRegenMod = amount; break;
                    }
                }
            }
            else
            {
                _log.Error($"TryMutateGearRating({wo.Name}, {weightType}): unknown weight class");
                return false;
            }

            if (wo.ArmorLevel != null)
                wo.BaseArmor = wo.ArmorLevel;

            if (wo.WardLevel != null)
                wo.BaseWard = wo.WardLevel;

            if (wo.ArmorWarMagicMod != null)
                wo.BaseArmorWarMagicMod = wo.ArmorWarMagicMod;

            if (wo.ArmorLifeMagicMod != null)
                wo.BaseArmorLifeMagicMod = wo.ArmorLifeMagicMod;

            if (wo.ArmorMagicDefMod != null)
                wo.BaseArmorMagicDefMod = wo.ArmorMagicDefMod;

            if (wo.ArmorPhysicalDefMod != null)
                wo.BaseArmorPhysicalDefMod = wo.ArmorPhysicalDefMod;

            if (wo.ArmorMissileDefMod != null)
                wo.BaseArmorMissileDefMod = wo.ArmorMissileDefMod;

            if (wo.ArmorDualWieldMod != null)
                wo.BaseArmorDualWieldMod = wo.ArmorDualWieldMod;

            if (wo.ArmorRunMod != null)
                wo.BaseArmorRunMod = wo.ArmorRunMod;

            if (wo.ArmorAttackMod != null)
                wo.BaseArmorAttackMod = wo.ArmorAttackMod;

            if (wo.ArmorHealthRegenMod != null)
                wo.BaseArmorHealthRegenMod = wo.ArmorHealthRegenMod;

            if (wo.ArmorStaminaRegenMod != null)
                wo.BaseArmorStaminaRegenMod = wo.ArmorStaminaRegenMod;

            if (wo.ArmorManaRegenMod != null)
                wo.BaseArmorManaRegenMod = wo.ArmorManaRegenMod;

            if (wo.ArmorShieldMod != null)
                wo.BaseArmorShieldMod = wo.ArmorShieldMod;

            if (wo.ArmorPerceptionMod != null)
                wo.BaseArmorPerceptionMod = wo.ArmorPerceptionMod;

            if (wo.ArmorThieveryMod != null)
                wo.BaseArmorThieveryMod = wo.ArmorThieveryMod;

            if (wo.ArmorHealthMod != null)
                wo.BaseArmorHealthMod = wo.ArmorHealthMod;

            if (wo.ArmorStaminaMod != null)
                wo.BaseArmorStaminaMod = wo.ArmorStaminaMod;

            if (wo.ArmorManaMod != null)
                wo.BaseArmorManaMod = wo.ArmorManaMod ;

            if (wo.ArmorResourcePenalty != null)
                wo.BaseArmorResourcePenalty = wo.ArmorResourcePenalty;

            if (wo.ArmorDeceptionMod != null)
                wo.BaseArmorDeceptionMod = wo.ArmorDeceptionMod;

            if (wo.ArmorTwohandedCombatMod != null)
                wo.BaseArmorTwohandedCombatMod = wo.ArmorTwohandedCombatMod;


            return true;
        }

        private static int GetRollForArmorMod(float lootQualityMod)
        {
            var qualityMod = lootQualityMod * 100;
            //var tempRateIncrease = 60;

            return ThreadSafeRandom.Next((int)qualityMod, 100);
        }

        private static List<int> GetRolledTypes(List<int> potentialTypes, float qualityMod)
        {
            List<int> rolledTypes = new List<int>();
            var numPotentialTypes = potentialTypes.Count;
            var numTypes = ThreadSafeRandom.Next(1, numPotentialTypes);

            for (int i = 0; i < numTypes; i++)
            {
                var type = potentialTypes[ThreadSafeRandom.Next(0, potentialTypes.Count - 1)];
                potentialTypes.Remove(type);
                rolledTypes.Add(type);
            }

            return rolledTypes;
        }

        private static void SetWieldLevelReq(WorldObject wo, int level)
        {
            if (wo.WieldRequirements == WieldRequirement.Invalid)
            {
                wo.WieldRequirements = WieldRequirement.Level;
                wo.WieldSkillType = (int)Skill.Axe;  // set from examples in pcap data
                wo.WieldDifficulty = level;
            }
            else if (wo.WieldRequirements == WieldRequirement.Level)
            {
                if (wo.WieldDifficulty < level)
                    wo.WieldDifficulty = level;
            }
            else
            {
                // this can either be empty, or in the case of covenant / olthoi armor,
                // it could already contain a level requirement of 180, or possibly 150 in tier 8

                // we want to set this level requirement to 180, in all cases

                // magloot logs indicated that even if covenant / olthoi armor was not upgraded to 180 in its mutation script,
                // a gear rating could still drop on it, and would "upgrade" the 150 to a 180

                wo.WieldRequirements2 = WieldRequirement.Level;
                wo.WieldSkillType2 = (int)Skill.Axe;  // set from examples in pcap data
                wo.WieldDifficulty2 = level;
            }
        }

        private static bool GetMutateArmorData(uint wcid, out LootTables.ArmorType? armorType)
        {
            foreach (var kvp in LootTables.armorTypeMap)
            {
                armorType = kvp.Key;
                var table = kvp.Value;

                if (kvp.Value.Contains((int)wcid))
                    return true;
            }
            armorType = null;
            return false;
        }

        private static ArmorWeightClass GetArmorWeightClass(uint armorWcid)
        {
            foreach (int wcid in LootTables.Cloth)
            {
                if (wcid == armorWcid)
                    return ArmorWeightClass.Cloth;
            }
            foreach (int wcid in LootTables.Light)
            {
                if (wcid == armorWcid)
                    return ArmorWeightClass.Light;
            }
            foreach (int wcid in LootTables.Heavy)
            {
                if (wcid == armorWcid)
                    return ArmorWeightClass.Heavy;
            }
            return ArmorWeightClass.None;
        }

        private static WieldAttributeType GetWieldAttributeType(ArmorWeightClass armorWeightClass)
        {
            switch (armorWeightClass)
            {
                case ArmorWeightClass.Cloth:
                    return WieldAttributeType.Self;

                case ArmorWeightClass.Light:
                    return WieldAttributeType.Quickness;

                case ArmorWeightClass.Heavy:
                    return WieldAttributeType.Strength;

                default:
                    return WieldAttributeType.Invalid;
            }
        }

        public static int GetWieldDifficultyPerTier(int tier)
        {
            switch (tier)
            {
                case 1:
                    return 50;
                case 2:
                    return 100;
                case 3:
                    return 150;
                case 4:
                    return 175;
                case 5:
                    return 200;
                case 6:
                    return 220;
                case 7:
                    return 240;
                case 8:
                    return 260;
                default:
                    return 0;
            }
        }

        private static int GetGearRatingAmount(int tier, TreasureDeath td, out float gearRatingPercentile)
        {
            var lootQualityMod = td.LootQualityMod;
            var roll = ThreadSafeRandom.Next(lootQualityMod, 1);

            var maxMod = Math.Max(tier - 4, 0);
            var mod = (int)(maxMod * roll);

            var maxPossibleMod = 3;
            gearRatingPercentile = (float)mod / maxPossibleMod;

            return mod;
        }

        private static double GetArmorSkillAmount(TreasureDeath treasureDeath ,WorldObject wo, out float modPercentile)
        {
            var tier = Math.Clamp(treasureDeath.Tier - 1, 0, 7);
            float[] bonusModRollPerTier = { 0.0f, 0.01f, 0.02f, 0.03f, 0.04f, 0.05f, 0.075f, 0.1f };

            var doubleMod = wo.ValidLocations == EquipMask.HeadWear ? 2.0f : 1.0f; // Headwear gets double-value mods
            var minMod = 0.1f * doubleMod;
            var rollPercentile = GetDiminishingRoll(treasureDeath);
            var statRoll = minMod * rollPercentile;
            var armorMod = minMod + statRoll + bonusModRollPerTier[tier];

            var maxPossibleMod = minMod + minMod + bonusModRollPerTier[7];
            modPercentile = (armorMod - minMod) / (maxPossibleMod - minMod);

            //Console.WriteLine($"GetArmorSkillAmount() \n" +
            //    $" -Tier: {tier}\n" +
            //    $" -DiminishingRoll: {statRoll}, Mod: {armorMod}, MaxMod: {maxPossibleMod}, ModPercentile: {modPercentile}");

            return armorMod;
        }

        /// <summary>
        /// Returns the correct Impenetrbility SpellId, based on the world object's tier.
        /// Used for Cloth armor only.
        /// </summary>
        /// <param name="wo"></param>
        /// <returns></returns>
        private static SpellId GetImpenetribilityLevel(WorldObject wo)
        {
            switch(wo.Tier)
            {
                case 3: return SpellId.Impenetrability2;
                case 4: return SpellId.Impenetrability3;
                case 5: return SpellId.Impenetrability4;
                case 6: return SpellId.Impenetrability5;
                case 7: return SpellId.Impenetrability6;
                case 8: return SpellId.Impenetrability7;
                default: return SpellId.Impenetrability1;
            }
        }

        private static float GetArmorResourcePenalty(WorldObject wo)
        {
            var mod = 0.0f;

            if (wo.ArmorWeightClass == (int)ArmorWeightClass.Heavy)
                mod = 0.05f;

            switch (wo.ArmorStyle)
            {
                case (int)ArmorStyle.Amuli:
                case (int)ArmorStyle.Chiran:
                case (int)ArmorStyle.OlthoiAmuli:
                    return 0.02f;
                case (int)ArmorStyle.StuddedLeather:
                case (int)ArmorStyle.Koujia:
                case (int)ArmorStyle.OlthoiKoujia:
                    return 0.02f;
                case (int)ArmorStyle.Chainmail:
                case (int)ArmorStyle.Scalemail:
                case (int)ArmorStyle.Nariyid:
                    return 0.03f;
                case (int)ArmorStyle.Platemail:
                case (int)ArmorStyle.Celdon:
                case (int)ArmorStyle.OlthoiCeldon:
                    return 0.04f;
                case (int)ArmorStyle.Covenant:
                case (int)ArmorStyle.OlthoiArmor:
                    return 0.05f;
            }

            switch ((int)wo.WeenieClassId)
            {
                case 44: // Buckler
                    return 0.05f;
                case 91: // Kite Shield
                case 93: // Round Shield
                    return 0.1f;
                case 92: // Large Kite Shield
                case 94: // Large Round Shield
                    return 0.15f;
                case 95: // Tower Shield
                    return 0.2f;
                case 21158: // Covenant Shield
                    return 0.25f;
            }

            return mod;
        }

        private static int GetArmorWorkmanship(WorldObject wo, double skillModsPercentile, double gearRatingPercentile)
        {
            var divisor = 0;
            var sum = 0.0;

            // Armor + Protection Levels
            var maxArmorLevel = GetMaxArmorLevel(wo);
            var armorLevelPercentile = 0.0f;

            var avgProtectionLevel = GetAverageProtectionLevel(wo);
            var minProtectionLevel = 0.25f;
            var maxProtectionLevel = 1.5f;
            var protectionLevelPercentile = 1.0f;
            if (avgProtectionLevel != 0)
                protectionLevelPercentile = (avgProtectionLevel - minProtectionLevel) / (maxProtectionLevel - minProtectionLevel);

            if (wo.ItemType != ItemType.Clothing)
            {
                if (wo.ArmorLevel > 0 && maxArmorLevel > 0)
                {
                    armorLevelPercentile = (float)wo.ArmorLevel / maxArmorLevel;
                    armorLevelPercentile += protectionLevelPercentile;
                    armorLevelPercentile /= 2;

                    sum += armorLevelPercentile;
                    divisor++;
                }
                else
                {
                    armorLevelPercentile = (avgProtectionLevel - minProtectionLevel) / (maxProtectionLevel - minProtectionLevel);

                    sum += armorLevelPercentile;
                    divisor++;
                }
            }

            // Ward
            var maxWardLevel = GetMaxWardLevel(wo);
            var wardLevelPercentile = 0.0f;
            if (wo.WardLevel > 0 && maxWardLevel > 0)
            { 
                wardLevelPercentile = (float)wo.WardLevel / maxWardLevel;

                sum += wardLevelPercentile;
                divisor++;
            }

            // Armor Skill Mods
            if (skillModsPercentile == float.NaN)
                skillModsPercentile = 0;

            sum += skillModsPercentile;
            divisor++;

            // Gear Ratings
            if (wo.ItemType != ItemType.Clothing)
            {
                sum += gearRatingPercentile;
                divisor++;
            }
            // Average Percentile
            var finalPercentile = sum / divisor;
            //Console.WriteLine($" -MaxArmor: {maxArmorLevel} - ArmorLevel: {wo.ArmorLevel} -MaxProt: 1.5 -ProtLevel: {avgProtectionLevel} -Armor/Protection %: {armorLevelPercentile}\n" + $" -MaxWard: {maxWardLevel} -WardLevel: {wo.WardLevel} -Ward %: {wardLevelPercentile}\n" + $" -Mods %: {skillModsPercentile}\n" + $" -Ratings %: {gearRatingPercentile}\n" + $" -Divisor: {divisor}\n" +
            //    $" --FINAL: {finalPercentile}\n\n");

            // Workmanship Calculation
            return (int)Math.Clamp(Math.Round(finalPercentile * 10, 0), 1, 10);
        }

        private static int GetMaxArmorLevel(WorldObject wo)
        {
            var weightClass = (ArmorWeightClass)(wo.ArmorWeightClass ?? 0);
            var armorStyle = (ArmorStyle)(wo.ArmorStyle ?? 0);
            int maxArmorLevel;

            switch (weightClass)
            {
                case ArmorWeightClass.Cloth:
                    maxArmorLevel = 350;
                    break;
                case ArmorWeightClass.Light:
                    maxArmorLevel = 525;
                    break;
                case ArmorWeightClass.Heavy:
                    maxArmorLevel = 700;
                    break;
                default:
                    maxArmorLevel = 0;
                    break;
            }

            switch (armorStyle)
            {
                case ArmorStyle.Cloth:
                    maxArmorLevel = 0;
                    break;
                case ArmorStyle.Amuli:
                case ArmorStyle.Chiran:
                    maxArmorLevel = 525;
                    break;
                case ArmorStyle.Leather:
                case ArmorStyle.Yoroi:
                case ArmorStyle.Lorica:
                    maxArmorLevel = 525;
                    break;
                case ArmorStyle.StuddedLeather:
                case ArmorStyle.Koujia:
                case ArmorStyle.OlthoiKoujia:
                    maxArmorLevel = 595;
                    break;
                case ArmorStyle.Chainmail:
                case ArmorStyle.Scalemail:
                    maxArmorLevel = 630;
                    break;
                case ArmorStyle.Platemail:
                case ArmorStyle.Celdon:
                case ArmorStyle.OlthoiCeldon:
                case ArmorStyle.Nariyid:
                    maxArmorLevel = 700;
                    break;
                case ArmorStyle.OlthoiArmor:
                case ArmorStyle.Covenant:
                    maxArmorLevel = 875;
                    break;
            }

            return (int)(maxArmorLevel * 1.1f);
        }

        private static int GetMaxWardLevel(WorldObject wo)
        {
            var armorStyle = (ArmorStyle)(wo.ArmorStyle ?? 0);
            var weightClass = (ArmorWeightClass)(wo.ArmorWeightClass ?? 0);
            var armorSlots = wo.ArmorSlots ?? 1;

            int wardLevel;

            switch(weightClass)
            {
                case ArmorWeightClass.Cloth:
                    wardLevel = 70;
                    break;
                default:
                    wardLevel = 35;
                    break;
            }

            switch (armorStyle)
            {
                case ArmorStyle.OlthoiArmor:
                case ArmorStyle.Covenant:
                    wardLevel = 0;
                    break;
                case ArmorStyle.Amuli:
                case ArmorStyle.Chiran:
                    wardLevel = 35;
                    break;
            }

            return (int)(wardLevel * armorSlots * 1.1f);
        }

        private static float GetAverageProtectionLevel(WorldObject wo)
        {
            var amount = 0.0;
            if (wo.ArmorModVsSlash != null)
            {
                amount += (float)wo.ArmorModVsSlash;
                amount += (float)wo.ArmorModVsBludgeon;
                amount += (float)wo.ArmorModVsPierce;
                amount += (float)wo.ArmorModVsFire;
                amount += (float)wo.ArmorModVsCold;
                amount += (float)wo.ArmorModVsAcid;
                amount += (float)wo.ArmorModVsElectric;

                amount /= 7;
            }
            else
                _log.Error($"Mutate Error during GetAverageProtectionLevel() - Armor Protection Levels returned null for {wo.ItemType} {wo.Name}.");

            return (float)amount;
        }

        private static bool IsShieldWcid(WorldObject wo)
        {
            int[] shieldWcids = LootTables.Shields;

            if (shieldWcids.Contains((int)wo.WeenieClassId))
                return true;

            return false;
        }

        private static int GetWeightClassAttributeReq(ArmorWeightClass weightClass)
        {
            switch (weightClass)
            {
                default:
                case ArmorWeightClass.Heavy: return 1; // Strength
                case ArmorWeightClass.Light: return 4; // Coordination
                case ArmorWeightClass.Cloth: return 6; // Self
            }
        }
    }
}
