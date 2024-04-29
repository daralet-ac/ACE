using ACE.Common;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Entity.Models;
using ACE.Server.Entity;
using ACE.Server.Entity.Actions;
using ACE.Server.Factories;
using ACE.Server.Managers;
using ACE.Server.Network.GameMessages.Messages;
using Serilog;
using System;
using DamageType = ACE.Entity.Enum.DamageType;
using MotionCommand = ACE.Entity.Enum.MotionCommand;

namespace ACE.Server.WorldObjects
{
    partial class Jewel : WorldObject
    { 
        private static readonly ILogger _log = Log.ForContext(typeof(Jewel));

        /// <summary>
        /// A new biota be created taking all of its values from weenie.
        /// </summary>
        public Jewel(Weenie weenie, ObjectGuid guid) : base(weenie, guid)
        {
            SetEphemeralValues();
        }

        /// <summary>
        /// Restore a WorldObject from the database.
        /// </summary>
        public Jewel(Biota biota) : base(biota)
        {
            SetEphemeralValues();
        }

        private void SetEphemeralValues()
        {
        }

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

            // empty sockets?

            if (!target.JewelSockets.HasValue || !HasEmptySockets(target))
            {
                player.Session.Network.EnqueueSend(new GameMessageSystemChat($"The {target.NameWithMaterial} has no empty sockets.", ChatMessageType.Craft));
                player.SendUseDoneEvent();
                return;
            }

            // 0 - prepended quality, 1 - gemstone type, 2 - appended name, 3 - property type, 4 - amount of property, 5 - original gem workmanship
            if (source.JewelSocket1 != null && !source.JewelSocket1.StartsWith("Empty"))
            {
                string[] parts = source.JewelSocket1.Split('/');

                if (int.TryParse(parts[5], out var workmanship))
                {
                    workmanship -= 1;
                    if (workmanship >= 1 && target.Workmanship < workmanship)
                    {
                        player.Session.Network.EnqueueSend(new GameMessageSystemChat($"The {source.Name} can only be used on an item with a workmanship of {workmanship} or greater.", ChatMessageType.Craft));
                        player.SendUseDoneEvent();
                        return;
                    }

                }

                if (MaterialTypetoString.TryGetValue(parts[1], out var convertedMaterialType))
                {
                    if (JewelValidLocations.TryGetValue(convertedMaterialType, out var materialWieldRestriction))
                    {
                        // check for weapon use only
                        if (materialWieldRestriction == 1)
                        {
                            if (target.ValidLocations != ACE.Entity.Enum.EquipMask.MeleeWeapon && target.ValidLocations != ACE.Entity.Enum.EquipMask.MissileWeapon && target.ValidLocations != ACE.Entity.Enum.EquipMask.Held && target.ValidLocations != ACE.Entity.Enum.EquipMask.TwoHanded)
                            {
                                player.Session.Network.EnqueueSend(new GameMessageSystemChat($"The {source.Name} can never be slotted into the {target.Name}.", ChatMessageType.Craft));
                                player.SendUseDoneEvent();
                                return;
                            }
                        }
                        // shield only
                        if (materialWieldRestriction == 2)
                        {
                            if (target.ValidLocations != ACE.Entity.Enum.EquipMask.Shield)
                            {
                                player.Session.Network.EnqueueSend(new GameMessageSystemChat($"The {source.Name} can never be slotted into the {target.Name}.", ChatMessageType.Craft));
                                player.SendUseDoneEvent();
                                return;
                            }
                        }
                        if (materialWieldRestriction == 3)
                        {
                            if (target.ValidLocations != ACE.Entity.Enum.EquipMask.MeleeWeapon && target.ValidLocations != ACE.Entity.Enum.EquipMask.MissileWeapon && target.ValidLocations != ACE.Entity.Enum.EquipMask.Held && target.ValidLocations != ACE.Entity.Enum.EquipMask.TwoHanded && target.ValidLocations != ACE.Entity.Enum.EquipMask.Shield)
                            {
                                player.Session.Network.EnqueueSend(new GameMessageSystemChat($"The {source.Name} can never be slotted into the {target.Name}.", ChatMessageType.Craft));
                                player.SendUseDoneEvent();
                                return;
                            }
                        }
                        // otherwise check the dictionary
                        if (materialWieldRestriction != 1 && materialWieldRestriction != 2 && materialWieldRestriction != 3)
                        {
                            if (materialWieldRestriction != (int)target.ValidLocations)
                            {
                                player.Session.Network.EnqueueSend(new GameMessageSystemChat($"The {source.Name} cannot be slotted into the {target.Name}.", ChatMessageType.Craft));
                                player.SendUseDoneEvent();
                                return;
                            }
                        }
                        // check for rending damage type matches
                        if (MaterialDamage.TryGetValue(convertedMaterialType, out var damageType))
                        {
                            if (target.W_DamageType != damageType && target.W_DamageType != ACE.Entity.Enum.DamageType.SlashPierce)
                            {
                                player.Session.Network.EnqueueSend(new GameMessageSystemChat($"The {source.Name} can never be slotted into a weapon of that damage type.", ChatMessageType.Craft));
                                player.SendUseDoneEvent();
                                return;
                            }
                            if (target.W_DamageType == DamageType.SlashPierce && convertedMaterialType != ACE.Entity.Enum.MaterialType.BlackGarnet && convertedMaterialType != ACE.Entity.Enum.MaterialType.ImperialTopaz)
                            {
                                    player.Session.Network.EnqueueSend(new GameMessageSystemChat($"The {source.Name} can never be slotted into a weapon of that damage type.", ChatMessageType.Craft));
                                    player.SendUseDoneEvent();
                                    return;
                            }
                        }
                    }
                }
            }

            if (!confirmed)
            {
                if (!player.ConfirmationManager.EnqueueSend(new Confirmation_CraftInteration(player.Guid, source.Guid, target.Guid), $"Adding {source.Name} to {target.NameWithMaterial}, enhancing its properties."))
                    player.SendUseDoneEvent(WeenieError.ConfirmationInProgress);
                else
                    player.SendUseDoneEvent();

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

            actionChain.AddAction(player, () =>
            {
                SocketJewel(player, source, target);

                player.EnqueueBroadcast(new GameMessageUpdateObject(target));
                player.TryConsumeFromInventoryWithNetworking(source);
            });

            player.EnqueueMotion(actionChain, MotionCommand.Ready);

            actionChain.AddAction(player, () =>
            {
                player.IsBusy = false;
            });

            actionChain.EnqueueChain();

            player.NextUseTime = DateTime.UtcNow.AddSeconds(animTime);
        }


        public static void SocketJewel(Player player, WorldObject jewel, WorldObject target)
        {
            if (jewel.JewelSocket1 != null && !jewel.JewelSocket1.StartsWith("Empty"))
            {
                // parse the log on the jewel, then get values for 3rd and 4th elements -- int property and value, and set them

                string[] parts = jewel.JewelSocket1.Split('/');

                if (StringToIntProperties.ContainsKey(parts[3]))
                {
                    if (StringToIntProperties.TryGetValue(parts[3], out var jewelProperty))
                    {
                        if (int.TryParse(parts[4], out var propertyValue))
                        {
                            target.GetType().GetProperty($"{jewelProperty}").SetValue(target, propertyValue);

                            // find an empty socket and write the jewel log

                            for (int i = 1; i <= 8; i++)
                            {
                                string currentSocket = (string)target.GetType().GetProperty($"JewelSocket{i}").GetValue(target);

                                if (currentSocket == null || !currentSocket.StartsWith("Empty"))
                                {
                                    continue;
                                }
                                else
                                {
                                    target.GetType().GetProperty($"JewelSocket{i}").SetValue(target, $"{parts[0]}/{parts[1]}/{parts[2]}/{parts[3]}/{parts[4]}/{parts[5]}");
                                    break;
                                }
                            }
                        }
                    }
                }

                // if a ring or bracelet, change valid locations to left or right only
                if ((int)target.ValidLocations == 786432)
                {
                    if (jewel.Name.Contains("Carnelian") || jewel.Name.Contains("Azurite") || jewel.Name.Contains("Tiger Eye"))
                    {
                        target.ValidLocations = ACE.Entity.Enum.EquipMask.FingerWearRight;
                        target.Use = "This ring can only be worn on the right hand.";
                    }
                    else
                    {
                        target.ValidLocations = ACE.Entity.Enum.EquipMask.FingerWearLeft;
                        target.Use = "This ring can only be worn on the left hand.";
                    }
                }
                if ((int)target.ValidLocations == 196608)
                {
                    if (jewel.Name.Contains("Amethyst") || jewel.Name.Contains("Diamond") || jewel.Name.Contains("Onyx") || jewel.Name.Contains("Zircon"))
                    {
                        target.ValidLocations = ACE.Entity.Enum.EquipMask.WristWearRight;
                        target.Use = "This bracelet can only be worn on the right wrist.";
                    }
                    else
                    {
                        target.ValidLocations = ACE.Entity.Enum.EquipMask.WristWearLeft;
                        target.Use = "This bracelet can only be worn on the left wrist.";
                    }
                }

                target.Attuned = (AttunedStatus?)1;
                target.Bonded = (BondedStatus?)1;
                return;

            }
        }

        private static bool HasEmptySockets(WorldObject target)
        {
            bool hasEmptySockets = false;

            for (int i = 1; i <= 8; i++)
            {
                string currentSocket = (string)target.GetType().GetProperty($"JewelSocket{i}").GetValue(target);

                if (currentSocket == null || !currentSocket.StartsWith("Empty"))
                    continue;
                else
                {
                    hasEmptySockets = true;
                    break;
                }
            }

            return hasEmptySockets;
        }

        public static void HandleJewelcarving(Player player, WorldObject source, WorldObject target)
        {

            var jewel = RecipeManager.CreateItem(player, 1053900, 1);

            jewel.IconId = target.IconId;

            // calculate bonuses to the carving roll  TODO : 100 skill at level 20 gives you 0.5 quality mod. Need to determine what an appropriate skill is by level to set this correctly

            var playerskill = player.GetCreatureSkill((Skill.ItemTinkering));

            double qualityMod = (double)playerskill.Current / (100 * (double)player.Level / 10);

            //Console.WriteLine(qualityMod);

            Random random = new Random();

            double randomDouble = random.NextDouble();
            qualityMod += randomDouble;

            ModifyCarvedJewel(player, target, jewel, qualityMod);

            player.TryCreateInInventoryWithNetworking(jewel);

            player.EnqueueBroadcast(new GameMessageUpdateObject(jewel));

            player.TryConsumeFromInventoryWithNetworking(target, 1);
        }

        public static void ModifyCarvedJewel(Player player, WorldObject target, WorldObject jewel, double qualityMod)
        {
            var jewelProperty = "";
            var appendedName = "";
            var baseValue = (int)target.ItemWorkmanship;

            switch (target.MaterialType)
            {
                case ACE.Entity.Enum.MaterialType.Agate:
                    jewelProperty = "Threat Enhancement";
                    appendedName = "of Provocation";
                    CalcJewelQuality(player, target, jewel, qualityMod, baseValue, jewelProperty, appendedName);
                    break;
                case ACE.Entity.Enum.MaterialType.Amber:
                    jewelProperty = "Health To Stamina";
                    appendedName = "of the Masochist";
                    CalcJewelQuality(player, target, jewel, qualityMod, baseValue, jewelProperty, appendedName);
                    break;
                case ACE.Entity.Enum.MaterialType.Amethyst:
                    jewelProperty = "Nullification";
                    appendedName = "of Nullification";
                    CalcJewelQuality(player, target, jewel, qualityMod, baseValue, jewelProperty, appendedName);
                    break;
                case ACE.Entity.Enum.MaterialType.Aquamarine:
                    jewelProperty = "Frost";
                    appendedName = "of the Bone-Chiller";
                    CalcJewelQuality(player, target, jewel, qualityMod, baseValue, jewelProperty, appendedName);
                    break;
                case ACE.Entity.Enum.MaterialType.Azurite:
                    jewelProperty = "Self";
                    appendedName = "of the Erudite Mind";
                    CalcJewelQuality(player, target, jewel, qualityMod, baseValue, jewelProperty, appendedName);
                    break;
                case ACE.Entity.Enum.MaterialType.BlackGarnet:
                    jewelProperty = "Pierce";
                    appendedName = "of Precision Strikes";
                    CalcJewelQuality(player, target, jewel, qualityMod, baseValue, jewelProperty, appendedName);
                    break;
                case ACE.Entity.Enum.MaterialType.BlackOpal:
                    jewelProperty = "Reprisal";
                    appendedName = "of Vicious Reprisal";
                    CalcJewelQuality(player, target, jewel, qualityMod, baseValue, jewelProperty, appendedName);
                    break;
                case ACE.Entity.Enum.MaterialType.Bloodstone:
                    jewelProperty = "Life Steal";
                    appendedName = "of Sanguine Thirst";
                    CalcJewelQuality(player, target, jewel, qualityMod, baseValue, jewelProperty, appendedName);
                    break;
                case ACE.Entity.Enum.MaterialType.Carnelian:
                    jewelProperty = "Strength";
                    appendedName = "of Mighty Thews";
                    CalcJewelQuality(player, target, jewel, qualityMod, baseValue, jewelProperty, appendedName);
                    break;
                case ACE.Entity.Enum.MaterialType.Citrine:
                    jewelProperty = "Stamina Reduction";
                    appendedName = "of the Third Wind";
                    CalcJewelQuality(player, target, jewel, qualityMod, baseValue, jewelProperty, appendedName);
                    break;
                case ACE.Entity.Enum.MaterialType.Diamond:
                    jewelProperty = "Hardened Defense";
                    appendedName = "of the Hardened Fortification";
                    CalcJewelQuality(player, target, jewel, qualityMod, baseValue, jewelProperty, appendedName);
                    break;
                case ACE.Entity.Enum.MaterialType.Emerald:
                    jewelProperty = "Acid";
                    appendedName = "of the Devouring Mist";
                    CalcJewelQuality(player, target, jewel, qualityMod, baseValue, jewelProperty, appendedName);
                    break;
                case ACE.Entity.Enum.MaterialType.FireOpal:
                    jewelProperty = "Familiarity";
                    appendedName = "of the Familiar Foe";
                    CalcJewelQuality(player, target, jewel, qualityMod, baseValue, jewelProperty, appendedName);
                    break;
                case ACE.Entity.Enum.MaterialType.GreenGarnet:
                    jewelProperty = "Elementalist";
                    appendedName = "of the Elementalist";
                    CalcJewelQuality(player, target, jewel, qualityMod, baseValue, jewelProperty, appendedName);
                    break;
                case ACE.Entity.Enum.MaterialType.GreenJade:
                    jewelProperty = "Prosperity";
                    appendedName = "of Prosperity";
                    CalcJewelQuality(player, target, jewel, qualityMod, baseValue, jewelProperty, appendedName);
                    break;
                case ACE.Entity.Enum.MaterialType.Hematite:
                    jewelProperty = "Blood Frenzy";
                    appendedName = "of Blood Frenzy";
                    CalcJewelQuality(player, target, jewel, qualityMod, baseValue, jewelProperty, appendedName);
                    break;
                case ACE.Entity.Enum.MaterialType.ImperialTopaz:
                    jewelProperty = "Slash";
                    appendedName = "of the Falcon's Gyre";
                    CalcJewelQuality(player, target, jewel, qualityMod, baseValue, jewelProperty, appendedName);
                    break;
                case ACE.Entity.Enum.MaterialType.Jet:
                    jewelProperty = "Lightning";
                    appendedName = "of Astyrrian's Rage";
                    CalcJewelQuality(player, target, jewel, qualityMod, baseValue, jewelProperty, appendedName);
                    break;
                case ACE.Entity.Enum.MaterialType.LapisLazuli:
                    jewelProperty = "Health To Mana";
                    appendedName = "of the Austere Anchorite";
                    CalcJewelQuality(player, target, jewel, qualityMod, baseValue, jewelProperty, appendedName);
                    break;
                case ACE.Entity.Enum.MaterialType.LavenderJade:
                    jewelProperty = "Selflessness";
                    appendedName = "of the Selfless Spirit";
                    CalcJewelQuality(player, target, jewel, qualityMod, baseValue, jewelProperty, appendedName);
                    break;
                case ACE.Entity.Enum.MaterialType.Malachite:
                    jewelProperty = "Components";
                    appendedName = "of the Meticulous Magus";
                    CalcJewelQuality(player, target, jewel, qualityMod, baseValue, jewelProperty, appendedName);
                    break;
                case ACE.Entity.Enum.MaterialType.Moonstone:
                    jewelProperty = "Item Mana Useage";
                    appendedName = "of the Thrifty Scholar";
                    CalcJewelQuality(player, target, jewel, qualityMod, baseValue, jewelProperty, appendedName);
                    break;
                case ACE.Entity.Enum.MaterialType.Onyx:
                    jewelProperty = "Physical Warding";
                    appendedName = "of the Black Bulwark";
                    CalcJewelQuality(player, target, jewel, qualityMod, baseValue, jewelProperty, appendedName);
                    break;
                case ACE.Entity.Enum.MaterialType.Opal:
                    jewelProperty = "Manasteal";
                    appendedName = "of the Ophidian";
                    CalcJewelQuality(player, target, jewel, qualityMod, baseValue, jewelProperty, appendedName);
                    break;
                case ACE.Entity.Enum.MaterialType.Peridot:
                    jewelProperty = "Quickness";
                    appendedName = "of the Swift-Footed";
                    CalcJewelQuality(player, target, jewel, qualityMod, baseValue, jewelProperty, appendedName);
                    break;
                case ACE.Entity.Enum.MaterialType.RedGarnet:
                    jewelProperty = "Fire";
                    appendedName = "of the Blazing Brand";
                    CalcJewelQuality(player, target, jewel, qualityMod, baseValue, jewelProperty, appendedName);
                    break;
                case ACE.Entity.Enum.MaterialType.RedJade:
                    jewelProperty = "Focus";
                    appendedName = "of the Focused Mind";
                    CalcJewelQuality(player, target, jewel, qualityMod, baseValue, jewelProperty, appendedName);
                    break;
                case ACE.Entity.Enum.MaterialType.RoseQuartz:
                    jewelProperty = "Vitals Transfer";
                    appendedName = "of the Tilted Scales";
                    CalcJewelQuality(player, target, jewel, qualityMod, baseValue, jewelProperty, appendedName);
                    break;
                case ACE.Entity.Enum.MaterialType.Ruby:
                    jewelProperty = "Last Stand";
                    appendedName = "of Red Fury";
                    CalcJewelQuality(player, target, jewel, qualityMod, baseValue, jewelProperty, appendedName);
                    break;
                case ACE.Entity.Enum.MaterialType.Sapphire:
                    jewelProperty = "Magic Find";
                    appendedName = "of the Seeker";
                    CalcJewelQuality(player, target, jewel, qualityMod, baseValue, jewelProperty, appendedName);
                    break;              
                case ACE.Entity.Enum.MaterialType.SmokeyQuartz:
                    jewelProperty = "Threat Reduction";
                    appendedName = "of Clouded Vision";
                    CalcJewelQuality(player, target, jewel, qualityMod, baseValue, jewelProperty, appendedName);
                    break;
                case ACE.Entity.Enum.MaterialType.Sunstone:
                    jewelProperty = "Experience Gain";
                    appendedName = "of the Illuminated Mind";
                    CalcJewelQuality(player, target, jewel, qualityMod, baseValue, jewelProperty, appendedName);
                    break;
                case ACE.Entity.Enum.MaterialType.TigerEye:
                    jewelProperty = "Coordination";
                    appendedName = "of the Dexterous Hand";
                    CalcJewelQuality(player, target, jewel, qualityMod, baseValue, jewelProperty, appendedName);
                    break;
                case ACE.Entity.Enum.MaterialType.Tourmaline:
                    jewelProperty = "Ward Pen";
                    appendedName = "of Ruthless Discernment";
                    CalcJewelQuality(player, target, jewel, qualityMod, baseValue, jewelProperty, appendedName);
                    break;
                case ACE.Entity.Enum.MaterialType.Turquoise:
                    jewelProperty = "Block Rating";
                    appendedName = "of Stalwart Defense";
                    CalcJewelQuality(player, target, jewel, qualityMod, baseValue, jewelProperty, appendedName);
                    break;
                case ACE.Entity.Enum.MaterialType.WhiteJade:
                    jewelProperty = "Heal";
                    appendedName = "of the Purified Soul";
                    CalcJewelQuality(player, target, jewel, qualityMod, baseValue, jewelProperty, appendedName);
                    break;
                case ACE.Entity.Enum.MaterialType.WhiteQuartz:
                    jewelProperty = "Shield Deflection";
                    appendedName = "of Swift Retribution";
                    CalcJewelQuality(player, target, jewel, qualityMod, baseValue, jewelProperty, appendedName);
                    break;
                case ACE.Entity.Enum.MaterialType.WhiteSapphire:
                    jewelProperty = "Bludgeon";
                    appendedName = "of the Skull-Cracker";
                    CalcJewelQuality(player, target, jewel, qualityMod, baseValue, jewelProperty, appendedName);
                    break;
                case ACE.Entity.Enum.MaterialType.YellowGarnet:
                    jewelProperty = "Bravado";
                    appendedName = "of Bravado";
                    CalcJewelQuality(player, target, jewel, qualityMod, baseValue, jewelProperty, appendedName);
                    break;
                case ACE.Entity.Enum.MaterialType.YellowTopaz:
                    jewelProperty = "Endurance";
                    appendedName = "of Perseverence";
                    CalcJewelQuality(player, target, jewel, qualityMod, baseValue, jewelProperty, appendedName);
                    break;
                case ACE.Entity.Enum.MaterialType.Zircon:
                    jewelProperty = "Elemental Warding";
                    appendedName = "of the Prismatic Ward";
                    CalcJewelQuality(player, target, jewel, qualityMod, baseValue, jewelProperty, appendedName);
                    break;
                default:
                    // Default case handling
                    break;
            }

            return;
        }
        // Jewel quality is based on workmanship with a possible range of -2 to +2 depending on carve quality, for a scale of 1-12.
        // certain properties will receive double this bonus in their particular calculations, others half etc. but all fundamentally on same scale
        public static void CalcJewelQuality(Player player, WorldObject target, WorldObject jewel, double qualityMod, int baseValue, string jewelProperty, string appendedName)
        {
            Random random = new Random();
            double randomElement = random.NextDouble();

            var modifiedBase = baseValue;

            if (qualityMod >= 1)
            {
                if (randomElement >= 0.9)
                    modifiedBase += 2;
                if (randomElement < 0.9 && qualityMod >= 0.5)
                    modifiedBase += 1;
                else
                    modifiedBase += 0;
            }

            if (qualityMod < 1 && qualityMod >= 0.5)
            {

                if (randomElement >= 0.75)
                    modifiedBase += 1;
                if (randomElement < 0.75 && qualityMod >= 0.25)
                    modifiedBase += 0;
                if (randomElement < 0.25)
                    modifiedBase -= 1;
            }

            if (qualityMod < 0.5)
            {

                if (randomElement >= 0.75)
                    modifiedBase += 0;
                if (randomElement < 0.75 && qualityMod >= 0.25)
                    modifiedBase -= 1;
                if (randomElement < 0.25)
                    modifiedBase -= 2;
            }

            // Difficulty "Failure" Penalty - Rather than destroying jewels, we just lower their quality by 1 for each 20 points of skill below expected.
            var skill = player.GetCreatureSkill(Skill.ItemTinkering);
            var skillLevel = skill.Current;
            var difficulty = (uint)(target.Workmanship * 20);

            if (skillLevel < difficulty - 20)
            {
                var difference = difficulty - skillLevel;
                var reductionPenalty = (int)difference / 20;
                modifiedBase -= reductionPenalty;
            }

            if (modifiedBase < 1)
                modifiedBase = 1;

            player.TryAwardCraftingXp(player, skill, Skill.ItemTinkering, (int)difficulty);

            // get quality name + write socket and jewel name
            // 0 - prepended quality, 1 - gemstone type, 2 - appended name, 3 - property type, 4 - amount of property, 5 - original gem workmanship

            if (JewelQuality.TryGetValue(modifiedBase, out var qualityName))
            {
                if (StringtoMaterialType.TryGetValue((MaterialType)target.MaterialType, out var materialType))
                {
                    jewel.JewelSocket1 = $"{qualityName}/{materialType}/{appendedName}/{jewelProperty}/{modifiedBase}/{target.Workmanship}";
                    jewel.Name = $"{qualityName} {materialType} {appendedName}";

                    if (GemstoneIconMap.TryGetValue(materialType, out var gemIcon))
                        jewel.IconId = gemIcon;
                }
            }

            if (JewelUiEffect.TryGetValue((MaterialType)target.MaterialType, out var uiEffect))
                jewel.UiEffects = (ACE.Entity.Enum.UiEffects)uiEffect;

            return;
        }

        public static void HandleUnsocketing(Player player, WorldObject source, WorldObject target)
        {
            if (player != null)
            {

                var numRequired = target.JewelSockets ?? 1;

                var freeSpace = player.GetFreeInventorySlots(true);

                if (freeSpace < target.JewelSockets)
                {
                    player.Session.Network.EnqueueSend(new GameMessageSystemChat($"You must free up additional packspace in order to unsocket these gems!", ChatMessageType.Craft));
                    player.SendUseDoneEvent();
                    return;
                }

                // if a ring or bracelet, set back to left or right wield and remove added text

                if (target.ValidLocations == (ACE.Entity.Enum.EquipMask)0x00080000 || target.ValidLocations == (ACE.Entity.Enum.EquipMask)0x00040000)
                {
                    target.ValidLocations = ACE.Entity.Enum.EquipMask.FingerWear;
                    target.Use = "";
                }
                if (target.ValidLocations == (ACE.Entity.Enum.EquipMask)0x00010000 || target.ValidLocations == (ACE.Entity.Enum.EquipMask)0x00020000)
                {
                    target.ValidLocations = ACE.Entity.Enum.EquipMask.WristWear;
                    target.Use = "";

                }
                // cycle through slots, emptying out ones that aren't already

                for (int i = 1; i <= 2; i++)
                {
                    string currentSocket = (string)target.GetType().GetProperty($"JewelSocket{i}").GetValue(target);

                    if (currentSocket == null || currentSocket.StartsWith("Empty"))
                        continue;

                    else
                    {
                        var jewel = WorldObjectFactory.CreateNewWorldObject(1053900);

                        string[] socketArray = currentSocket.Split('/');

                        // 0 - prepended quality, 1 - gemstone type, 2 - appended name, 3 - property type, 4 - amount of property, 5 - original gem workmanship

                        if (socketArray[0] != null && socketArray[1] != null && socketArray[2] != null && socketArray[3] != null && socketArray[4] != null && socketArray[5] != null)
                        {
                            // get materialtype in order to set ui and ID

                            if (MaterialTypetoString.TryGetValue(socketArray[1], out var convertedMaterialType))
                            {
                                if (JewelUiEffect.TryGetValue((MaterialType)convertedMaterialType, out var uiEffect))
                                    jewel.UiEffects = (ACE.Entity.Enum.UiEffects)uiEffect;

                                if (GemstoneIconMap.TryGetValue(socketArray[1], out var gemstoneIcon))
                                    jewel.IconId = gemstoneIcon;
                            }

                            jewel.JewelSocket1 = currentSocket;
                            jewel.Name = $"{socketArray[0]} {socketArray[1]} {socketArray[2]}";

                            if (StringToIntProperties.TryGetValue(socketArray[3], out var jewelProperty))
                            {
                                target.GetType().GetProperty($"{jewelProperty}").SetValue(target, null);
                                target.GetType().GetProperty($"JewelSocket{i}").SetValue(target, $"Empty");
                            }

                            jewel.Attuned = (AttunedStatus?)1;
                            jewel.Bonded = (BondedStatus?)1;
                            player.TryCreateInInventoryWithNetworking(jewel);
                        }

                    }

                }

                target.Attuned = null;
                target.Bonded = null;
                player.EnqueueBroadcast(new GameMessageUpdateObject(target));
                return;
            }

        }






    }
    }

