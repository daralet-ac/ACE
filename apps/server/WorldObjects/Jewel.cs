using System;
using System.Collections.Generic;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
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

        if (!target.JewelSockets.HasValue || !HasEmptySockets(target))
        {
            player.Session.Network.EnqueueSend(new GameMessageSystemChat($"The {target.Name} has no empty sockets.", ChatMessageType.Craft));
            player.SendUseDoneEvent();
            return;
        }

        if (source.JewelSocket1Effect is not null && source.JewelSocket1Quality is not null)
        {
            if (target.Workmanship < source.JewelSocket1Quality)
            {
                var orGreater = source.JewelSocket1Quality < 10 ? " or greater" : "";

                player.Session.Network.EnqueueSend(
                    new GameMessageSystemChat(
                        $"The {source.Name} can only be socketed into an item with a workmanship of {source.JewelSocket1Quality}{orGreater}.",
                        ChatMessageType.Craft
                    )
                );
                player.SendUseDoneEvent();
                return;
            }

            // check for weapon use only
            if (source is {JewelMaterialType: not null})
            {
                var materialWieldRestriction = JewelValidLocations[source.JewelMaterialType];
                switch (materialWieldRestriction)
                {
                    case 1 when target.ValidLocations != EquipMask.MeleeWeapon
                                && target.ValidLocations != EquipMask.MissileWeapon
                                && target.ValidLocations != EquipMask.Held
                                && target.ValidLocations != EquipMask.TwoHanded:
                        player.Session.Network.EnqueueSend(
                            new GameMessageSystemChat(
                                $"The {source.Name} can never be slotted into the {target.Name}.",
                                ChatMessageType.Craft
                            )
                        );
                        player.SendUseDoneEvent();
                        return;
                    // shield only
                    case 2 when target.ValidLocations != EquipMask.Shield:
                        player.Session.Network.EnqueueSend(
                            new GameMessageSystemChat(
                                $"The {source.Name} can never be slotted into the {target.Name}.",
                                ChatMessageType.Craft
                            )
                        );
                        player.SendUseDoneEvent();
                        return;
                    case 3 when target.ValidLocations != EquipMask.MeleeWeapon
                                && target.ValidLocations != EquipMask.MissileWeapon
                                && target.ValidLocations != EquipMask.Held
                                && target.ValidLocations != EquipMask.TwoHanded
                                && target.ValidLocations != EquipMask.Shield:
                        player.Session.Network.EnqueueSend(
                            new GameMessageSystemChat(
                                $"The {source.Name} can never be slotted into the {target.Name}.",
                                ChatMessageType.Craft
                            )
                        );
                        player.SendUseDoneEvent();
                        return;
                }

                // otherwise check the dictionary
                if (materialWieldRestriction != 1 && materialWieldRestriction != 2 && materialWieldRestriction != 3)
                {
                    if (target.ValidLocations != null && materialWieldRestriction != (int)target.ValidLocations)
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
                if (MaterialDamage.TryGetValue(source.JewelMaterialType.Value, out var damageType))
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
                }

                if (
                    target.W_DamageType == DamageType.SlashPierce
                    && source.JewelMaterialType.Value != ACE.Entity.Enum.MaterialType.BlackGarnet
                    && source.JewelMaterialType.Value != ACE.Entity.Enum.MaterialType.ImperialTopaz
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

        if (!confirmed)
        {
            if (
                !player.ConfirmationManager.EnqueueSend(
                    new Confirmation_CraftInteration(player.Guid, source.Guid, target.Guid),
                    $"Adding {source.Name} to {target.Name}, enhancing its properties.\n\n" +
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
                SocketJewel(source, target);

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

    private static void SocketJewel(WorldObject jewel, WorldObject target)
    {
        if (jewel.JewelSocket1Effect is null || jewel.JewelSocket1Quality is null || jewel.JewelMaterialType is null)
        {
            return;
        }

        for (var i = 0; i < target.JewelSockets; i++)
        {
            if (target.GetProperty(JewelSocketEffectIntId[i]) is not null)
            {
                continue;
            }

            target.SetProperty(JewelSocketEffectIntId[i], jewel.JewelSocket1Effect.Value);
            target.SetProperty(JewelSocketEffectIntId[i] + 1, jewel.JewelSocket1Quality.Value);

            var currentTotalRating = target.GetProperty(JewelMaterialToType[jewel.JewelMaterialType.Value]) ?? 0;
            target.SetProperty(JewelMaterialToType[jewel.JewelMaterialType.Value], currentTotalRating + jewel.JewelSocket1Quality.Value);

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

            target.Attuned = AttunedStatus.Attuned;
            target.Bonded = BondedStatus.Bonded;

            break;
        }
    }

    private static bool HasEmptySockets(WorldObject target)
    {
        for (var i = 0; i < (target.JewelSockets ?? 0); i++)
        {
            if (target.GetProperty(JewelSocketEffectIntId[i]) is null)
            {
                return true;
            }
        }

        return false;
    }

    public static void HandleJewelcarving(Player player, WorldObject source, WorldObject target, bool success)
    {
        var jewel = RecipeManager.CreateItem(player, 1053900, 1);

        jewel.IconId = target.IconId;

        ModifyCarvedJewel(player, target, jewel, success);

        player.TryConsumeFromInventoryWithNetworking(target, 1);

        if (!success)
        {

            if (jewel is {JewelSocket1Quality: null})
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
        var baseValue = 1;

        if (target.ItemWorkmanship is null)
        {
            _log.Error("ModifyCarvedJewel(Player {Player}, Target {Target}, Jewel {Jewel}) - Target workmanship is null. Defaulting to 1 workmanship.", player.Name, target.Name, jewel.Name);
        }
        else
        {
            baseValue = (int)target.ItemWorkmanship;
        }

        string appendedName;

        switch (target.MaterialType)
        {
            case ACE.Entity.Enum.MaterialType.Agate:
                appendedName = "of Provocation";
                CalcJewelQuality(player, target, jewel, baseValue, PropertyInt.GearThreatGain, ACE.Entity.Enum.MaterialType.Agate, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.Amber:
                appendedName = "of the Masochist";
                CalcJewelQuality(player, target, jewel, baseValue, PropertyInt.GearHealthToStamina, ACE.Entity.Enum.MaterialType.Amber, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.Amethyst:
                appendedName = "of Nullification";
                CalcJewelQuality(player, target, jewel, baseValue, PropertyInt.GearNullification, ACE.Entity.Enum.MaterialType.Amethyst, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.Aquamarine:
                appendedName = "of the Bone-Chiller";
                CalcJewelQuality(player, target, jewel, baseValue, PropertyInt.GearFrost, ACE.Entity.Enum.MaterialType.Aquamarine, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.Azurite:
                appendedName = "of the Erudite Mind";
                CalcJewelQuality(player, target, jewel, baseValue, PropertyInt.GearSelf, ACE.Entity.Enum.MaterialType.Azurite, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.BlackGarnet:
                appendedName = "of Precision Strikes";
                CalcJewelQuality(player, target, jewel, baseValue, PropertyInt.GearPierce, ACE.Entity.Enum.MaterialType.BlackGarnet, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.BlackOpal:
                appendedName = "of Vicious Reprisal";
                CalcJewelQuality(player, target, jewel, baseValue, PropertyInt.GearReprisal, ACE.Entity.Enum.MaterialType.BlackOpal, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.Bloodstone:
                appendedName = "of Sanguine Thirst";
                CalcJewelQuality(player, target, jewel, baseValue, PropertyInt.GearLifesteal, ACE.Entity.Enum.MaterialType.Bloodstone, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.Carnelian:
                appendedName = "of Mighty Thews";
                CalcJewelQuality(player, target, jewel, baseValue, PropertyInt.GearStrength, ACE.Entity.Enum.MaterialType.Carnelian, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.Citrine:
                appendedName = "of the Third Wind";
                CalcJewelQuality(player, target, jewel, baseValue, PropertyInt.GearStamReduction, ACE.Entity.Enum.MaterialType.Citrine, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.Diamond:
                appendedName = "of the Hardened Fortification";
                CalcJewelQuality(player, target, jewel, baseValue, PropertyInt.GearHardenedDefense, ACE.Entity.Enum.MaterialType.Diamond, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.Emerald:
                appendedName = "of the Devouring Mist";
                CalcJewelQuality(player, target, jewel, baseValue, PropertyInt.GearAcid, ACE.Entity.Enum.MaterialType.Emerald, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.FireOpal:
                appendedName = "of the Familiar Foe";
                CalcJewelQuality(player, target, jewel, baseValue, PropertyInt.GearFamiliarity, ACE.Entity.Enum.MaterialType.FireOpal, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.GreenGarnet:
                appendedName = "of the Elementalist";
                CalcJewelQuality(player, target, jewel, baseValue, PropertyInt.GearElementalist, ACE.Entity.Enum.MaterialType.GreenGarnet, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.GreenJade:
                appendedName = "of Prosperity";
                CalcJewelQuality(player, target, jewel, baseValue, PropertyInt.GearPyrealFind, ACE.Entity.Enum.MaterialType.GreenJade, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.Hematite:
                appendedName = "of Blood Frenzy";
                CalcJewelQuality(player, target, jewel, baseValue, PropertyInt.GearSelfHarm, ACE.Entity.Enum.MaterialType.Hematite, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.ImperialTopaz:
                appendedName = "of the Falcon's Gyre";
                CalcJewelQuality(player, target, jewel, baseValue, PropertyInt.GearSlash, ACE.Entity.Enum.MaterialType.ImperialTopaz, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.Jet:
                appendedName = "of Astyrrian's Rage";
                CalcJewelQuality(player, target, jewel, baseValue, PropertyInt.GearLightning, ACE.Entity.Enum.MaterialType.Jet, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.LapisLazuli:
                appendedName = "of the Austere Anchorite";
                CalcJewelQuality(player, target, jewel, baseValue, PropertyInt.GearHealthToMana, ACE.Entity.Enum.MaterialType.LapisLazuli, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.LavenderJade:
                appendedName = "of the Selfless Spirit";
                CalcJewelQuality(player, target, jewel, baseValue, PropertyInt.GearSelflessness, ACE.Entity.Enum.MaterialType.LavenderJade, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.Malachite:
                appendedName = "of the Meticulous Magus";
                CalcJewelQuality(player, target, jewel, baseValue, PropertyInt.GearCompBurn, ACE.Entity.Enum.MaterialType.Malachite, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.Moonstone:
                appendedName = "of the Thrifty Scholar";
                CalcJewelQuality(player, target, jewel, baseValue, PropertyInt.GearItemManaUsage, ACE.Entity.Enum.MaterialType.Moonstone, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.Onyx:
                appendedName = "of the Black Bulwark";
                CalcJewelQuality(player, target, jewel, baseValue, PropertyInt.GearPhysicalWard, ACE.Entity.Enum.MaterialType.Onyx, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.Opal:
                appendedName = "of the Ophidian";
                CalcJewelQuality(player, target, jewel, baseValue, PropertyInt.GearManasteal, ACE.Entity.Enum.MaterialType.Opal, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.Peridot:
                appendedName = "of the Swift-Footed";
                CalcJewelQuality(player, target, jewel, baseValue, PropertyInt.GearQuickness, ACE.Entity.Enum.MaterialType.Peridot, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.RedGarnet:
                appendedName = "of the Blazing Brand";
                CalcJewelQuality(player, target, jewel, baseValue, PropertyInt.GearFire, ACE.Entity.Enum.MaterialType.RedGarnet, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.RedJade:
                appendedName = "of the Focused Mind";
                CalcJewelQuality(player, target, jewel, baseValue, PropertyInt.GearFocus, ACE.Entity.Enum.MaterialType.RedJade, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.RoseQuartz:
                appendedName = "of the Tilted Scales";
                CalcJewelQuality(player, target, jewel, baseValue, PropertyInt.GearVitalsTransfer, ACE.Entity.Enum.MaterialType.RoseQuartz, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.Ruby:
                appendedName = "of Red Fury";
                CalcJewelQuality(player, target, jewel, baseValue, PropertyInt.GearRedFury, ACE.Entity.Enum.MaterialType.Ruby, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.Sapphire:
                appendedName = "of the Seeker";
                CalcJewelQuality(player, target, jewel, baseValue, PropertyInt.GearMagicFind, ACE.Entity.Enum.MaterialType.Sapphire, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.SmokeyQuartz:
                appendedName = "of Clouded Vision";
                CalcJewelQuality(player, target, jewel, baseValue, PropertyInt.GearThreatReduction, ACE.Entity.Enum.MaterialType.SmokeyQuartz, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.Sunstone:
                appendedName = "of the Illuminated Mind";
                CalcJewelQuality(player, target, jewel, baseValue, PropertyInt.GearExperienceGain, ACE.Entity.Enum.MaterialType.Sunstone, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.TigerEye:
                appendedName = "of the Dexterous Hand";
                CalcJewelQuality(player, target, jewel, baseValue, PropertyInt.GearCoordination, ACE.Entity.Enum.MaterialType.TigerEye, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.Tourmaline:
                appendedName = "of Ruthless Discernment";
                CalcJewelQuality(player, target, jewel, baseValue, PropertyInt.GearWardPen, ACE.Entity.Enum.MaterialType.Tourmaline, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.Turquoise:
                appendedName = "of Stalwart Defense";
                CalcJewelQuality(player, target, jewel, baseValue, PropertyInt.GearBlock, ACE.Entity.Enum.MaterialType.Turquoise, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.WhiteJade:
                appendedName = "of the Purified Soul";
                CalcJewelQuality(player, target, jewel, baseValue, PropertyInt.GearHealBubble, ACE.Entity.Enum.MaterialType.WhiteJade, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.WhiteQuartz:
                appendedName = "of Swift Retribution";
                CalcJewelQuality(player, target, jewel, baseValue, PropertyInt.GearThorns, ACE.Entity.Enum.MaterialType.WhiteQuartz, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.WhiteSapphire:
                appendedName = "of the Skull-Cracker";
                CalcJewelQuality(player, target, jewel, baseValue, PropertyInt.GearBludgeon, ACE.Entity.Enum.MaterialType.WhiteSapphire, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.YellowGarnet:
                appendedName = "of Bravado";
                CalcJewelQuality(player, target, jewel, baseValue, PropertyInt.GearBravado, ACE.Entity.Enum.MaterialType.YellowGarnet, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.YellowTopaz:
                appendedName = "of Perseverence";
                CalcJewelQuality(player, target, jewel, baseValue, PropertyInt.GearEndurance, ACE.Entity.Enum.MaterialType.YellowTopaz, appendedName, success);
                break;
            case ACE.Entity.Enum.MaterialType.Zircon:
                appendedName = "of the Prismatic Ward";
                CalcJewelQuality(player, target, jewel, baseValue, PropertyInt.GearElementalWard, ACE.Entity.Enum.MaterialType.Zircon, appendedName, success);
                break;
        }
    }

    // Jewel quality is based on workmanship with a possible range of -2 to +2 depending on carve quality, for a scale of 1-12.
    // certain properties will receive double this bonus in their particular calculations, others half etc. but all fundamentally on same scale
    private static void CalcJewelQuality(
        Player player,
        WorldObject target,
        WorldObject jewel,
        int baseValue,
        PropertyInt jewelProperty,
        MaterialType materialType,
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

        jewel.JewelSocket1Effect = (int)jewelProperty;
        jewel.JewelSocket1Quality = modifiedQuality;
        jewel.JewelMaterialType = materialType;

        if (JewelQuality.TryGetValue(modifiedQuality, out var qualityName))
        {
            jewel.Name = $"{qualityName} {MaterialTypeToString[materialType]} {appendedName}";
        }

        if (GemstoneIconMap.TryGetValue(materialType, out var gemIcon))
        {
            jewel.IconId = gemIcon;
        }

        if (target.MaterialType != null && JewelUiEffect.TryGetValue((MaterialType)target.MaterialType, out var uiEffect))
        {
            jewel.UiEffects = (UiEffects)uiEffect;
        }
    }

    public static void HandleUnsocketing(Player player, WorldObject source, WorldObject target)
    {
        if (player == null)
        {
            return;
        }

        var numSocketedJewels = GetNumberOfSocketedJewels(target);
        var freeSpace = player.GetFreeInventorySlots(true);

        if (freeSpace < numSocketedJewels)
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
        if (target.ValidLocations is EquipMask.FingerWearRight or EquipMask.FingerWearLeft)
        {
            target.ValidLocations = EquipMask.FingerWear;
            target.Use = "";
        }
        if (target.ValidLocations is EquipMask.WristWearRight or EquipMask.WristWearLeft)
        {
            target.ValidLocations = EquipMask.WristWear;
            target.Use = "";
        }

        // cycle through slots, emptying out ones that aren't already (
        for (var i = 0; i < (target.JewelSockets ?? 0); i++)
        {
            var currentSocketEffectTypeId = target.GetProperty(JewelSocketEffectIntId[i]);
            var currentSocketQualityLevel = target.GetProperty(JewelSocketEffectIntId[i] + 1);

            if (currentSocketEffectTypeId is null or < 1 || currentSocketQualityLevel is null or < 1)
            {
                continue;
            }

            // create jewel for inventory
            var jewel = WorldObjectFactory.CreateNewWorldObject(1053900);

            jewel.JewelMaterialType = JewelTypeToMaterial[(PropertyInt)currentSocketEffectTypeId];

            if (jewel is { JewelMaterialType: null })
            {
                _log.Error("HandleUnsocketing() - MaterialType is null during unsocketing from {Target}", target.Name);
                return;
            }

            jewel.JewelSocket1Effect = currentSocketEffectTypeId;
            jewel.JewelSocket1Quality = currentSocketQualityLevel;

            jewel.UiEffects = (UiEffects)JewelUiEffect[jewel.JewelMaterialType.Value];
            jewel.IconId = GemstoneIconMap[jewel.JewelMaterialType.Value];

            var qualityString = JewelQuality[currentSocketQualityLevel.Value];
            var materialString = MaterialTypeToString[jewel.JewelMaterialType.Value];
            var effectNameString = JewelEffectInfo[JewelMaterialToType[jewel.JewelMaterialType.Value]].Name;
            jewel.Name = $"{qualityString} {materialString} of the {effectNameString}";

            jewel.Attuned = AttunedStatus.Attuned;
            jewel.Bonded = BondedStatus.Bonded;

            player.TryCreateInInventoryWithNetworking(jewel);

            // remove jewel properties from target
            target.RemoveProperty(JewelSocketEffectIntId[i]);
            target.RemoveProperty(JewelSocketEffectIntId[i] + 1);

            var currentTotalRating = target.GetProperty(JewelMaterialToType[jewel.JewelMaterialType.Value]) ?? 0;
            target.SetProperty(JewelMaterialToType[jewel.JewelMaterialType.Value], currentTotalRating - jewel.JewelSocket1Quality.Value);
        }

        target.Attuned = null;
        target.Bonded = null;
        player.EnqueueBroadcast(new GameMessageUpdateObject(target));
    }

    private static int GetNumberOfSocketedJewels(WorldObject target)
    {
        var number = 0;

        if (target.JewelSocket1Effect is not null)
        {
            number++;
        }

        if (target.JewelSocket2Effect is not null)
        {
            number++;
        }

        if (target.JewelSocket3Effect is not null)
        {
            number++;
        }

        if (target.JewelSocket4Effect is not null)
        {
            number++;
        }

        if (target.JewelSocket5Effect is not null)
        {
            number++;
        }

        if (target.JewelSocket6Effect is not null)
        {
            number++;
        }

        if (target.JewelSocket7Effect is not null)
        {
            number++;
        }

        if (target.JewelSocket8Effect is not null)
        {
            number++;
        }

        if (target.JewelSocket9Effect is not null)
        {
            number++;
        }

        if (target.JewelSocket10Effect is not null)
        {
            number++;
        }

        return number;
    }

    public static readonly List<PropertyInt> JewelSocketEffectIntId =
    [
        PropertyInt.JewelSocket1Effect,
        PropertyInt.JewelSocket2Effect,
        PropertyInt.JewelSocket3Effect,
        PropertyInt.JewelSocket4Effect,
        PropertyInt.JewelSocket5Effect,
        PropertyInt.JewelSocket6Effect,
        PropertyInt.JewelSocket7Effect,
        PropertyInt.JewelSocket8Effect,
        PropertyInt.JewelSocket9Effect,
        PropertyInt.JewelSocket10Effect
    ];
}
