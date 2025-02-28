using System;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Entity.Models;
using ACE.Server.Entity;
using ACE.Server.Entity.Actions;
using ACE.Server.Factories;
using ACE.Server.Managers;
using ACE.Server.Network.GameMessages.Messages;
using DamageType = ACE.Entity.Enum.DamageType;
using MotionCommand = ACE.Entity.Enum.MotionCommand;

namespace ACE.Server.WorldObjects;

partial class Jewel : WorldObject
{
    /// <summary>
    /// A new biota be created taking all of its values from weenie.
    /// </summary>
    public Jewel(Weenie weenie, ObjectGuid guid)
        : base(weenie, guid)
    {
        SetEphemeralValues();
    }

    /// <summary>
    /// Restore a WorldObject from the database.
    /// </summary>
    public Jewel(Biota biota)
        : base(biota)
    {
        SetEphemeralValues();
    }

    private void SetEphemeralValues() { }

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
            player.Session.Network.EnqueueSend(
                new GameMessageSystemChat($"The {target.NameWithMaterial} has no empty sockets.", ChatMessageType.Craft)
            );
            player.SendUseDoneEvent();
            return;
        }

        // 0 - prepended quality, 1 - gemstone type, 2 - appended name, 3 - property type, 4 - amount of property, 5 - original gem workmanship
        if (source.JewelSocket1 != null && !source.JewelSocket1.StartsWith("Empty"))
        {
            var parts = source.JewelSocket1.Split('/');

            if (int.TryParse(parts[5], out var workmanship))
            {
                workmanship -= 1;
                if (workmanship >= 1 && target.Workmanship < workmanship)
                {
                    player.Session.Network.EnqueueSend(
                        new GameMessageSystemChat(
                            $"The {source.Name} can only be used on an item with a workmanship of {workmanship} or greater.",
                            ChatMessageType.Craft
                        )
                    );
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
                        if (
                            target.ValidLocations != EquipMask.MeleeWeapon
                            && target.ValidLocations != EquipMask.MissileWeapon
                            && target.ValidLocations != EquipMask.Held
                            && target.ValidLocations != EquipMask.TwoHanded
                        )
                        {
                            player.Session.Network.EnqueueSend(
                                new GameMessageSystemChat(
                                    $"The {source.Name} can never be slotted into the {target.Name}.",
                                    ChatMessageType.Craft
                                )
                            );
                            player.SendUseDoneEvent();
                            return;
                        }
                    }
                    // shield only
                    if (materialWieldRestriction == 2)
                    {
                        if (target.ValidLocations != EquipMask.Shield)
                        {
                            player.Session.Network.EnqueueSend(
                                new GameMessageSystemChat(
                                    $"The {source.Name} can never be slotted into the {target.Name}.",
                                    ChatMessageType.Craft
                                )
                            );
                            player.SendUseDoneEvent();
                            return;
                        }
                    }
                    if (materialWieldRestriction == 3)
                    {
                        if (
                            target.ValidLocations != EquipMask.MeleeWeapon
                            && target.ValidLocations != EquipMask.MissileWeapon
                            && target.ValidLocations != EquipMask.Held
                            && target.ValidLocations != EquipMask.TwoHanded
                            && target.ValidLocations != EquipMask.Shield
                        )
                        {
                            player.Session.Network.EnqueueSend(
                                new GameMessageSystemChat(
                                    $"The {source.Name} can never be slotted into the {target.Name}.",
                                    ChatMessageType.Craft
                                )
                            );
                            player.SendUseDoneEvent();
                            return;
                        }
                    }
                    // otherwise check the dictionary
                    if (materialWieldRestriction != 1 && materialWieldRestriction != 2 && materialWieldRestriction != 3)
                    {
                        if (materialWieldRestriction != (int)target.ValidLocations)
                        {
                            player.Session.Network.EnqueueSend(
                                new GameMessageSystemChat(
                                    $"The {source.Name} cannot be slotted into the {target.Name}.",
                                    ChatMessageType.Craft
                                )
                            );
                            player.SendUseDoneEvent();
                            return;
                        }
                    }
                    // check for rending damage type matches
                    if (MaterialDamage.TryGetValue(convertedMaterialType, out var damageType))
                    {
                        if (target.W_DamageType != damageType && target.W_DamageType != DamageType.SlashPierce)
                        {
                            player.Session.Network.EnqueueSend(
                                new GameMessageSystemChat(
                                    $"The {source.Name} can never be slotted into a weapon of that damage type.",
                                    ChatMessageType.Craft
                                )
                            );
                            player.SendUseDoneEvent();
                            return;
                        }
                        if (
                            target.W_DamageType == DamageType.SlashPierce
                            && convertedMaterialType != ACE.Entity.Enum.MaterialType.BlackGarnet
                            && convertedMaterialType != ACE.Entity.Enum.MaterialType.ImperialTopaz
                        )
                        {
                            player.Session.Network.EnqueueSend(
                                new GameMessageSystemChat(
                                    $"The {source.Name} can never be slotted into a weapon of that damage type.",
                                    ChatMessageType.Craft
                                )
                            );
                            player.SendUseDoneEvent();
                            return;
                        }
                    }
                }
            }
        }

        if (!confirmed)
        {
            if (
                !player.ConfirmationManager.EnqueueSend(
                    new Confirmation_CraftInteration(player.Guid, source.Guid, target.Guid),
                    $"Adding {source.Name} to {target.NameWithMaterial}, enhancing its properties.\n\n" +
                    $"Once socketed into an item, this jewel becomes permanently attuned to your character. Items with contained jewels become attuned and will remain so until all jewels are removed.\n\n" +
                    $"Jewels may be unsocketed using an Intricate Carving Tool. There is no skill check or destruction chance.\n\n"
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
                SocketJewel(player, source, target);

                player.EnqueueBroadcast(new GameMessageUpdateObject(target));
                player.TryConsumeFromInventoryWithNetworking(source);
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

    private static void SocketJewel(Player player, WorldObject jewel, WorldObject target)
    {
        if (jewel.JewelSocket1 != null && !jewel.JewelSocket1.StartsWith("Empty"))
        {
            // parse the log on the jewel, then get values for 3rd and 4th elements -- int property and value, and set them

            var parts = jewel.JewelSocket1.Split('/');

            if (StringToIntProperties.ContainsKey(parts[3]))
            {
                if (StringToIntProperties.TryGetValue(parts[3], out var jewelProperty))
                {
                    if (int.TryParse(parts[4], out var propertyValue))
                    {
                        target.GetType().GetProperty($"{jewelProperty}")?.SetValue(target, propertyValue);

                        // find an empty socket and write the jewel log

                        for (var i = 1; i <= 8; i++)
                        {
                            var currentSocket = (string)target.GetType().GetProperty($"JewelSocket{i}")?.GetValue(target);

                            if (currentSocket == null || !currentSocket.StartsWith("Empty"))
                            {
                                continue;
                            }
                            else
                            {
                                target
                                    .GetType()
                                    .GetProperty($"JewelSocket{i}")
                                    ?.SetValue(
                                        target,
                                        $"{parts[0]}/{parts[1]}/{parts[2]}/{parts[3]}/{parts[4]}/{parts[5]}"
                                    );
                                break;
                            }
                        }
                    }
                }
            }

            // if a ring or bracelet, change valid locations to left or right only
            if (target.ValidLocations != null && (int)target.ValidLocations == 786432)
            {
                if (
                    jewel.Name.Contains("Carnelian")
                    || jewel.Name.Contains("Azurite")
                    || jewel.Name.Contains("Tiger Eye")
                )
                {
                    target.ValidLocations = EquipMask.FingerWearRight;
                    target.Use = "This ring can only be worn on the right hand.";
                }
                else
                {
                    target.ValidLocations = EquipMask.FingerWearLeft;
                    target.Use = "This ring can only be worn on the left hand.";
                }
            }
            if (target.ValidLocations != null && (int)target.ValidLocations == 196608)
            {
                if (
                    jewel.Name.Contains("Amethyst")
                    || jewel.Name.Contains("Diamond")
                    || jewel.Name.Contains("Onyx")
                    || jewel.Name.Contains("Zircon")
                )
                {
                    target.ValidLocations = EquipMask.WristWearRight;
                    target.Use = "This bracelet can only be worn on the right wrist.";
                }
                else
                {
                    target.ValidLocations = EquipMask.WristWearLeft;
                    target.Use = "This bracelet can only be worn on the left wrist.";
                }
            }

            target.Attuned = (AttunedStatus?)1;
            target.Bonded = (BondedStatus?)1;
        }
    }

    private static bool HasEmptySockets(WorldObject target)
    {
        var hasEmptySockets = false;

        for (var i = 1; i <= 8; i++)
        {
            var currentSocket = (string)target.GetType().GetProperty($"JewelSocket{i}")?.GetValue(target);

            if (currentSocket == null || !currentSocket.StartsWith("Empty"))
            {
                continue;
            }

            hasEmptySockets = true;
            break;
        }

        return hasEmptySockets;
    }

    public static void HandleJewelcarving(Player player, WorldObject source, WorldObject target, bool success)
    {
        var jewel = RecipeManager.CreateItem(player, 1053900, 1);

        jewel.IconId = target.IconId;

        ModifyCarvedJewel(player, target, jewel, success);

        player.TryConsumeFromInventoryWithNetworking(target, 1);

        if (!success)
        {

            if (jewel is {JewelSocket1: null})
            {
                player.Session.Network.EnqueueSend(new GameMessageSystemChat("You botch your attempt to carve the gem and the jewel shatters.", ChatMessageType.Craft));
                player.TryConsumeFromInventoryWithNetworking(jewel);
                player.EnqueueBroadcast(new GameMessageUpdateObject(jewel));
            }
            else
            {
                player.Session.Network.EnqueueSend(new GameMessageSystemChat("You botch your attempt to carve the gem and some quality is lost.", ChatMessageType.Craft));
                player.TryCreateInInventoryWithNetworking(jewel);
                player.EnqueueBroadcast(new GameMessageUpdateObject(jewel));
            }
        }
        else
        {
            player.TryCreateInInventoryWithNetworking(jewel);
            player.EnqueueBroadcast(new GameMessageUpdateObject(jewel));
        }
    }

    private static void ModifyCarvedJewel(Player player, WorldObject target, WorldObject jewel, bool success)
    {
        var jewelProperty = "";
        var appendedName = "";

        var baseValue = 1;
        if (target.ItemWorkmanship is null)
        {
            _log.Error("ModifyCarvedJewel(Player {Player}, Target {Target}, Jewel {Jewel}) - Target workmanship is null. Defaulting to 1 workmanship.", player.Name, target.Name, jewel.Name);
        }
        else
        {
            baseValue = (int)target.ItemWorkmanship;
        }

        switch (target.MaterialType)
        {
            case ACE.Entity.Enum.MaterialType.Agate:
                jewelProperty = "Threat Enhancement";
                appendedName = "of Provocation";
                CalcJewelQuality(player, target, jewel, baseValue, jewelProperty, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.Amber:
                jewelProperty = "Health To Stamina";
                appendedName = "of the Masochist";
                CalcJewelQuality(player, target, jewel, baseValue, jewelProperty, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.Amethyst:
                jewelProperty = "Nullification";
                appendedName = "of Nullification";
                CalcJewelQuality(player, target, jewel, baseValue, jewelProperty, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.Aquamarine:
                jewelProperty = "Frost";
                appendedName = "of the Bone-Chiller";
                CalcJewelQuality(player, target, jewel, baseValue, jewelProperty, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.Azurite:
                jewelProperty = "Self";
                appendedName = "of the Erudite Mind";
                CalcJewelQuality(player, target, jewel, baseValue, jewelProperty, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.BlackGarnet:
                jewelProperty = "Pierce";
                appendedName = "of Precision Strikes";
                CalcJewelQuality(player, target, jewel, baseValue, jewelProperty, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.BlackOpal:
                jewelProperty = "Reprisal";
                appendedName = "of Vicious Reprisal";
                CalcJewelQuality(player, target, jewel, baseValue, jewelProperty, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.Bloodstone:
                jewelProperty = "Life Steal";
                appendedName = "of Sanguine Thirst";
                CalcJewelQuality(player, target, jewel, baseValue, jewelProperty, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.Carnelian:
                jewelProperty = "Strength";
                appendedName = "of Mighty Thews";
                CalcJewelQuality(player, target, jewel, baseValue, jewelProperty, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.Citrine:
                jewelProperty = "Stamina Reduction";
                appendedName = "of the Third Wind";
                CalcJewelQuality(player, target, jewel, baseValue, jewelProperty, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.Diamond:
                jewelProperty = "Hardened Defense";
                appendedName = "of the Hardened Fortification";
                CalcJewelQuality(player, target, jewel, baseValue, jewelProperty, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.Emerald:
                jewelProperty = "Acid";
                appendedName = "of the Devouring Mist";
                CalcJewelQuality(player, target, jewel, baseValue, jewelProperty, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.FireOpal:
                jewelProperty = "Familiarity";
                appendedName = "of the Familiar Foe";
                CalcJewelQuality(player, target, jewel, baseValue, jewelProperty, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.GreenGarnet:
                jewelProperty = "Elementalist";
                appendedName = "of the Elementalist";
                CalcJewelQuality(player, target, jewel, baseValue, jewelProperty, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.GreenJade:
                jewelProperty = "Prosperity";
                appendedName = "of Prosperity";
                CalcJewelQuality(player, target, jewel, baseValue, jewelProperty, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.Hematite:
                jewelProperty = "Blood Frenzy";
                appendedName = "of Blood Frenzy";
                CalcJewelQuality(player, target, jewel, baseValue, jewelProperty, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.ImperialTopaz:
                jewelProperty = "Slash";
                appendedName = "of the Falcon's Gyre";
                CalcJewelQuality(player, target, jewel, baseValue, jewelProperty, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.Jet:
                jewelProperty = "Lightning";
                appendedName = "of Astyrrian's Rage";
                CalcJewelQuality(player, target, jewel, baseValue, jewelProperty, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.LapisLazuli:
                jewelProperty = "Health To Mana";
                appendedName = "of the Austere Anchorite";
                CalcJewelQuality(player, target, jewel, baseValue, jewelProperty, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.LavenderJade:
                jewelProperty = "Selflessness";
                appendedName = "of the Selfless Spirit";
                CalcJewelQuality(player, target, jewel, baseValue, jewelProperty, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.Malachite:
                jewelProperty = "Components";
                appendedName = "of the Meticulous Magus";
                CalcJewelQuality(player, target, jewel, baseValue, jewelProperty, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.Moonstone:
                jewelProperty = "Item Mana Usage";
                appendedName = "of the Thrifty Scholar";
                CalcJewelQuality(player, target, jewel, baseValue, jewelProperty, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.Onyx:
                jewelProperty = "Physical Warding";
                appendedName = "of the Black Bulwark";
                CalcJewelQuality(player, target, jewel, baseValue, jewelProperty, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.Opal:
                jewelProperty = "Manasteal";
                appendedName = "of the Ophidian";
                CalcJewelQuality(player, target, jewel, baseValue, jewelProperty, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.Peridot:
                jewelProperty = "Quickness";
                appendedName = "of the Swift-Footed";
                CalcJewelQuality(player, target, jewel, baseValue, jewelProperty, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.RedGarnet:
                jewelProperty = "Fire";
                appendedName = "of the Blazing Brand";
                CalcJewelQuality(player, target, jewel, baseValue, jewelProperty, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.RedJade:
                jewelProperty = "Focus";
                appendedName = "of the Focused Mind";
                CalcJewelQuality(player, target, jewel, baseValue, jewelProperty, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.RoseQuartz:
                jewelProperty = "Vitals Transfer";
                appendedName = "of the Tilted Scales";
                CalcJewelQuality(player, target, jewel, baseValue, jewelProperty, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.Ruby:
                jewelProperty = "Red Fury";
                appendedName = "of Red Fury";
                CalcJewelQuality(player, target, jewel, baseValue, jewelProperty, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.Sapphire:
                jewelProperty = "Magic Find";
                appendedName = "of the Seeker";
                CalcJewelQuality(player, target, jewel, baseValue, jewelProperty, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.SmokeyQuartz:
                jewelProperty = "Threat Reduction";
                appendedName = "of Clouded Vision";
                CalcJewelQuality(player, target, jewel, baseValue, jewelProperty, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.Sunstone:
                jewelProperty = "Experience Gain";
                appendedName = "of the Illuminated Mind";
                CalcJewelQuality(player, target, jewel, baseValue, jewelProperty, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.TigerEye:
                jewelProperty = "Coordination";
                appendedName = "of the Dexterous Hand";
                CalcJewelQuality(player, target, jewel, baseValue, jewelProperty, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.Tourmaline:
                jewelProperty = "Ward Pen";
                appendedName = "of Ruthless Discernment";
                CalcJewelQuality(player, target, jewel, baseValue, jewelProperty, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.Turquoise:
                jewelProperty = "Block Rating";
                appendedName = "of Stalwart Defense";
                CalcJewelQuality(player, target, jewel, baseValue, jewelProperty, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.WhiteJade:
                jewelProperty = "Heal";
                appendedName = "of the Purified Soul";
                CalcJewelQuality(player, target, jewel, baseValue, jewelProperty, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.WhiteQuartz:
                jewelProperty = "Shield Deflection";
                appendedName = "of Swift Retribution";
                CalcJewelQuality(player, target, jewel, baseValue, jewelProperty, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.WhiteSapphire:
                jewelProperty = "Bludgeon";
                appendedName = "of the Skull-Cracker";
                CalcJewelQuality(player, target, jewel, baseValue, jewelProperty, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.YellowGarnet:
                jewelProperty = "Bravado";
                appendedName = "of Bravado";
                CalcJewelQuality(player, target, jewel, baseValue, jewelProperty, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.YellowTopaz:
                jewelProperty = "Endurance";
                appendedName = "of Perseverence";
                CalcJewelQuality(player, target, jewel, baseValue, jewelProperty, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.Zircon:
                jewelProperty = "Elemental Warding";
                appendedName = "of the Prismatic Ward";
                CalcJewelQuality(player, target, jewel, baseValue, jewelProperty, appendedName, success);
                break;
            default:
                // Default case handling
                break;
        }

        return;
    }

    // Jewel quality is based on workmanship with a possible range of -2 to +2 depending on carve quality, for a scale of 1-12.
    // certain properties will receive double this bonus in their particular calculations, others half etc. but all fundamentally on same scale
    private static void CalcJewelQuality(
        Player player,
        WorldObject target,
        WorldObject jewel,
        int baseValue,
        string jewelProperty,
        string appendedName,
        bool success
    )
    {
        if (target.Workmanship is null)
        {
            _log.Error("CalcJewelQuality() - Target workmanship is null for: {Target} ({Wcid})", target.Name, target.WeenieClassId);
            return;
        }

        var modifiedQuality = baseValue;

        var skill = player.GetCreatureSkill(Skill.Jewelcrafting);
        var difficulty = (int)(target.Workmanship * 20 - 20);

        // Difficulty "Failure" Penalty - Rather than destroying jewels, we just lower their quality by 1 for each 10 points of skill below expected, plus 1.
        if (!success)
        {
            if (PropertyManager.GetBool("bypass_crafting_checks").Item == false)
            {
                var difference = Math.Max(difficulty - skill.Current, 0);
                var reductionPenalty = (int)Math.Round((float)difference / 10) + 1;

                modifiedQuality -= reductionPenalty;
            }

            Player.TryAwardCraftingXp(player, skill, Skill.Jewelcrafting, (int)difficulty);
        }
        else
        {
            Player.TryAwardCraftingXp(player, skill, Skill.Jewelcrafting, (int)difficulty, 1, 3.0f);
        }

        // get quality name + write socket and jewel name
        // 0 - prepended quality, 1 - gemstone type, 2 - appended name, 3 - property type, 4 - amount of property, 5 - original gem workmanship

        if (JewelQuality.TryGetValue(modifiedQuality, out var qualityName))
        {
            if (target.MaterialType != null && StringtoMaterialType.TryGetValue((MaterialType)target.MaterialType, out var materialType))
            {
                jewel.JewelSocket1 = $"{qualityName}/{materialType}/{appendedName}/{jewelProperty}/{modifiedQuality}/{target.Workmanship}";
                jewel.Name = $"{qualityName} {materialType} {appendedName}";

                if (GemstoneIconMap.TryGetValue(materialType, out var gemIcon))
                {
                    jewel.IconId = gemIcon;
                }
            }
        }

        if (target.MaterialType != null && JewelUiEffect.TryGetValue((MaterialType)target.MaterialType, out var uiEffect))
        {
            jewel.UiEffects = (UiEffects)uiEffect;
        }
    }

    public static void HandleUnsocketing(Player player, WorldObject source, WorldObject target)
    {
        if (player != null)
        {
            var numRequired = target.JewelSockets ?? 1;

            var freeSpace = player.GetFreeInventorySlots(true);

            if (freeSpace < target.JewelSockets)
            {
                player.Session.Network.EnqueueSend(
                    new GameMessageSystemChat(
                        $"You must free up additional packspace in order to unsocket these gems!",
                        ChatMessageType.Craft
                    )
                );
                player.SendUseDoneEvent();
                return;
            }

            // if a ring or bracelet, set back to left or right wield and remove added text

            if (target.ValidLocations == (EquipMask)0x00080000 || target.ValidLocations == (EquipMask)0x00040000)
            {
                target.ValidLocations = EquipMask.FingerWear;
                target.Use = "";
            }
            if (target.ValidLocations == (EquipMask)0x00010000 || target.ValidLocations == (EquipMask)0x00020000)
            {
                target.ValidLocations = EquipMask.WristWear;
                target.Use = "";
            }
            // cycle through slots, emptying out ones that aren't already

            for (var i = 1; i <= 2; i++)
            {
                var currentSocket = (string)target.GetType().GetProperty($"JewelSocket{i}")?.GetValue(target);

                if (currentSocket == null || currentSocket.StartsWith("Empty"))
                {
                    continue;
                }
                else
                {
                    var jewel = WorldObjectFactory.CreateNewWorldObject(1053900);

                    var socketArray = currentSocket.Split('/');

                    // 0 - prepended quality, 1 - gemstone type, 2 - appended name, 3 - property type, 4 - amount of property, 5 - original gem workmanship

                    // get materialtype in order to set ui and ID

                    if (MaterialTypetoString.TryGetValue(socketArray[1], out var convertedMaterialType))
                    {
                        if (JewelUiEffect.TryGetValue((MaterialType)convertedMaterialType, out var uiEffect))
                        {
                            jewel.UiEffects = (UiEffects)uiEffect;
                        }

                        if (GemstoneIconMap.TryGetValue(socketArray[1], out var gemstoneIcon))
                        {
                            jewel.IconId = gemstoneIcon;
                        }
                    }

                    jewel.JewelSocket1 = currentSocket;
                    jewel.Name = $"{socketArray[0]} {socketArray[1]} {socketArray[2]}";

                    if (StringToIntProperties.TryGetValue(socketArray[3], out var jewelProperty))
                    {
                        target.GetType().GetProperty($"{jewelProperty}")?.SetValue(target, null);
                        target.GetType().GetProperty($"JewelSocket{i}")?.SetValue(target, $"Empty");
                    }

                    jewel.Attuned = (AttunedStatus?)1;
                    jewel.Bonded = (BondedStatus?)1;
                    player.TryCreateInInventoryWithNetworking(jewel);
                }
            }

            target.Attuned = null;
            target.Bonded = null;
            player.EnqueueBroadcast(new GameMessageUpdateObject(target));
            return;
        }
    }
}
