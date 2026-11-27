using System;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Entity.Models;
using ACE.Server.Entity;
using ACE.Server.Factories;
using ACE.Server.Entity.Actions;
using ACE.Server.Managers;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.WorldObjects;

public class ArmorPatch : Stackable
{
    /// <summary>
    /// A new biota be created taking all of its values from weenie.
    /// </summary>
    public ArmorPatch(Weenie weenie, ObjectGuid guid)
        : base(weenie, guid)
    {
        SetEphemeralValues();
    }

    /// <summary>
    /// Restore a WorldObject from the database.
    /// </summary>
    public ArmorPatch(Biota biota)
        : base(biota)
    {
        SetEphemeralValues();
    }

    private void SetEphemeralValues() { }

    public static void BroadcastArmorPatch(Player player, string sourceName, WorldObject target, int patchesConsumed)
    {
        var plural = patchesConsumed > 1;

        if (plural)
        {
            player.EnqueueBroadcast(
                new GameMessageSystemChat(
                    $"{player.Name} applies {patchesConsumed} {sourceName}es to the {target.NameWithMaterial}.",
                    ChatMessageType.Craft
                ),
                LocalBroadcastRange,
                ChatMessageType.Craft
            );
        }
        else
        {
            player.EnqueueBroadcast(
                new GameMessageSystemChat(
                    $"{player.Name} applies an {sourceName} to the {target.NameWithMaterial}.",
                    ChatMessageType.Craft
                ),
                LocalBroadcastRange,
                ChatMessageType.Craft
            );
        }

        _log.Debug($"[ArmorPatch] {player.Name} {sourceName} to the {target.NameWithMaterial}.");
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

        if (!RecipeManager.VerifyUse(player, source, target, true) || target.Workmanship == null)
        {
            player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
            return;
        }

        if (DestabilizedLootForge.TryBlockFurtherAlteration(player, target))
        {
            return;
        }

        var maxWieldReq = GetMaxWieldReq(source);

        if (maxWieldReq.HasValue)
        {
            var amountToAdd = GetArmorBonus(target);

            var targetArmorSlots = target.ArmorSlots ?? 1;
            var armorPatchStackSize = source.StackSize ?? 1;

            if (target.ArmorLevel < 1)
            {
                player.Session.Network.EnqueueSend(
                    new GameMessageSystemChat(
                        $"Armor patches can only reinforce vestments with innate armor level.",
                        ChatMessageType.Craft
                    )
                );
                player.SendUseDoneEvent();
                return;
            }
            if (target.ArmorPatchApplied == true)
            {
                player.Session.Network.EnqueueSend(
                    new GameMessageSystemChat(
                        $"The {target.NameWithMaterial} has already had an armor patch applied.",
                        ChatMessageType.Craft
                    )
                );
                player.SendUseDoneEvent();
                return;
            }

            if (!CanPatch(GetTargetWieldReq(target), maxWieldReq.Value))
            {
                player.Session.Network.EnqueueSend(
                    new GameMessageSystemChat(
                        $"The {target.NameWithMaterial} is too fine for a {source.Name}. It can only patch armor with a wield requirement of {maxWieldReq.Value} or less.",
                        ChatMessageType.Craft
                    )
                );
                player.SendUseDoneEvent();
                return;
            }

            if (armorPatchStackSize < targetArmorSlots)
            {
                player.Session.Network.EnqueueSend(
                    new GameMessageSystemChat(
                        $"You need {targetArmorSlots} {source.Name}es to increase the armor level of {target.NameWithMaterial}.",
                        ChatMessageType.Craft
                    )
                );
                player.SendUseDoneEvent();
                return;
            }

            if (!confirmed)
            {
                var plural = targetArmorSlots > 1 ? "es" : "";

                if (
                    !player.ConfirmationManager.EnqueueSend(
                        new Confirmation_CraftInteration(player.Guid, source.Guid, target.Guid),
                        $"Adding {targetArmorSlots} armor patch{plural} to {target.NameWithMaterial}, increasing its armor level by {amountToAdd}."
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
                    target.ArmorLevel += amountToAdd;
                    target.ArmorPatchAmount = amountToAdd;
                    target.ArmorPatchApplied = true;
                    target.LongDesc += "\n\nThis item has had an armor patch applied and cannot receive another.";

                    player.EnqueueBroadcast(new GameMessageUpdateObject(target));

                    player.TryConsumeFromInventoryWithNetworking(source, targetArmorSlots);
                    BroadcastArmorPatch(player, source.Name, target, targetArmorSlots);
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
    }

    /// <summary>
    /// Every armor patch adds this share of the armor's base (untinkered) armor level.
    /// </summary>
    public const double ArmorBonusPercent = 0.10;

    public static int GetArmorBonus(WorldObject target)
    {
        var baseArmor = target.BaseArmor is > 0 ? target.BaseArmor.Value : target.ArmorLevel ?? 0;
        return GetArmorBonus(baseArmor);
    }

    public static int GetArmorBonus(int baseArmor)
    {
        return Math.Max(1, (int)Math.Round(baseArmor * ArmorBonusPercent, MidpointRounding.AwayFromZero));
    }

    /// <summary>
    /// The highest wield requirement of armor this patch can go on. Patches from before the patch rework only carry their
    /// old flat armor amount, so that amount stands for its quality.
    /// </summary>
    public static int? GetMaxWieldReq(WorldObject patch)
    {
        var maxWieldReq = patch.GetProperty(PropertyInt.ArmorPatchMaxWieldReq);
        if (maxWieldReq.HasValue)
        {
            return maxWieldReq;
        }

        return patch.ArmorPatchAmount switch
        {
            null => null,
            <= 25 => 125,
            <= 50 => 200,
            <= 100 => 230,
            _ => 270,
        };
    }

    /// <summary>
    /// The armor's wield requirement: its attribute requirement for loot armor, otherwise the requirement of its tier.
    /// </summary>
    public static int GetTargetWieldReq(WorldObject target)
    {
        if (target.WieldRequirements == WieldRequirement.RawAttrib && target.WieldDifficulty is > 0)
        {
            return target.WieldDifficulty.Value;
        }

        return target.Tier is > 0 ? LootGenerationFactory.GetWieldDifficultyPerTier(target.Tier.Value) : 0;
    }

    public static bool CanPatch(int targetWieldReq, int maxWieldReq)
    {
        return targetWieldReq <= maxWieldReq;
    }
}
