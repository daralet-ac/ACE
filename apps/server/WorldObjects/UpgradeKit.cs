using System;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Entity.Models;
using ACE.Server.Entity;
using ACE.Server.Entity.Actions;
using ACE.Server.Factories;
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

        if (!target.UpgradeableQuestItem)
        {
            player.Session.Network.EnqueueSend(
                new GameMessageSystemChat($"Only certain quest items can be upgraded.", ChatMessageType.Craft)
            );
            player.SendUseDoneEvent();
            return;
        }

        if (target.WieldDifficulty >= PlayerTierWieldDifficulty((player)))
        {
            player.Session.Network.EnqueueSend(
                new GameMessageSystemChat($"The item has already been upgraded to your current wield tier.", ChatMessageType.Craft)
            );
            player.SendUseDoneEvent();
            return;
        }

        if (!confirmed)
        {
            if (
                !player.ConfirmationManager.EnqueueSend(
                    new Confirmation_CraftInteration(player.Guid, source.Guid, target.Guid),
                    $"This will upgrade {target.Name} to your current wield tier. Its wield difficulty will be increased to {PlayerTierWieldDifficulty((player))}."
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
                UpgradeItem(player, source, target);

                player.EnqueueBroadcast(new GameMessageUpdateObject(target));
                player.Session.Network.EnqueueSend(
                    new GameMessageSystemChat(
                        $"You upgrade {target.Name} to a more powerful version.",
                        ChatMessageType.Craft
                    )
                );
                player.TryConsumeFromInventoryWithNetworking(source, 1);
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

    public static void UpgradeItem(Player player, WorldObject source, WorldObject target)
    {
        var currentWieldDifficulty = target.WieldDifficulty ?? 50;
        var newWieldDifficulty = PlayerTierWieldDifficulty((player));

        var currentTier = LootGenerationFactory.GetTierFromWieldDifficulty(currentWieldDifficulty) - 1;
        var newTier = LootGenerationFactory.GetTierFromWieldDifficulty(newWieldDifficulty) - 1;

        target.SetProperty(PropertyInt.WieldDifficulty, newWieldDifficulty);

        // Weapons
        if (target.ItemType is ItemType.Weapon or ItemType.MissileWeapon or ItemType.MeleeWeapon or ItemType.Caster)
        {
            if (target.WeaponSubtype == null)
            {
                _log.Error($"MutateQuestItem() - WeaponSubType is null for ({target.Name})");
                return;
            }

            if (target.Damage != null)
            {
                var currentBaseStat = target.Damage.Value;
                var currentTierMinimum = LootTables.GetMeleeSubtypeMinimumDamage((LootTables.WeaponSubtype)target.WeaponSubtype, currentTier);
                var currentRange = LootTables.GetMeleeSubtypeDamageRange((LootTables.WeaponSubtype)target.WeaponSubtype, currentTier);
                var currentRoll = currentBaseStat - currentTierMinimum;

                var currentPercentile = (float)currentRoll / currentRange;

                var newTierMinimum = LootTables.GetMeleeSubtypeMinimumDamage((LootTables.WeaponSubtype)target.WeaponSubtype, newTier);
                var newTierRange = LootTables.GetMeleeSubtypeDamageRange((LootTables.WeaponSubtype)target.WeaponSubtype, newTier);
                var percentileAddition = newTierRange * currentPercentile;
                var final = Convert.ToInt32(newTierMinimum + percentileAddition);

                target.SetProperty(PropertyInt.Damage, final);
            }
        }
    }

    private static int PlayerTierWieldDifficulty(Player player)
    {
        var playerTier = player.GetPlayerTier(player.Level ?? 1);

        return LootGenerationFactory.GetWieldDifficultyPerTier(playerTier);
    }
}
