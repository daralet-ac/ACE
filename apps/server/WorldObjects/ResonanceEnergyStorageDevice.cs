using System;
using System.Linq;
using ACE.Common.Extensions;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Entity.Models;
using ACE.Server.Entity;
using ACE.Server.Entity.Actions;
using ACE.Server.Network.GameEvent.Events;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.WorldObjects;

// RIMS 1.0's Resonance Energy Storage Device (2023139) - the release side of the charge/release
// ladder. Charging is handled by StabilizationDevice.cs (2023161 used on this item).
public class ResonanceEnergyStorageDevice : WorldObject
{
    public ResonanceEnergyStorageDevice(Weenie weenie, ObjectGuid guid)
        : base(weenie, guid)
    {
        SetEphemeralValues();
    }

    public ResonanceEnergyStorageDevice(Biota biota)
        : base(biota)
    {
        SetEphemeralValues();
    }

    private static void SetEphemeralValues() { }

    private const string RimsChargeLevelQuest = "RIMSChargeLevel";
    private const string RimsReleaseCooldownQuest = "RIMSReleaseCooldown";
    private const double RimsReleaseTopOffPercent = 0.25;

    public override void HandleActionUseOnTarget(Player player, WorldObject target)
    {
        UseObjectOnTarget(player, this, target);
    }

    public static void UseObjectOnTarget(Player player, WorldObject source, WorldObject target, bool confirmed = false)
    {
        if (target != player)
        {
            player.Session.Network.EnqueueSend(
                new GameEventCommunicationTransientString(
                    player.Session,
                    "You can only use this device on yourself."
                )
            );
            player.SendUseDoneEvent(WeenieError.ActionCancelled);
            return;
        }

        if (!player.QuestManager.IsMaxSolves(RimsChargeLevelQuest))
        {
            player.Session.Network.EnqueueSend(
                new GameMessageSystemChat(
                    "The device stays quiet - there's nothing here for it to read.",
                    ChatMessageType.Broadcast
                )
            );
            player.SendUseDoneEvent();
            return;
        }

        if (player.QuestManager.HasQuest(RimsReleaseCooldownQuest) && !player.QuestManager.CanSolve(RimsReleaseCooldownQuest))
        {
            player.Session.Network.EnqueueSend(
                new GameMessageSystemChat(
                    $"The device is still cooling from its last release - try again in {player.QuestManager.GetNextSolveTime(RimsReleaseCooldownQuest).GetFriendlyString()}.",
                    ChatMessageType.Broadcast
                )
            );
            player.SendUseDoneEvent();
            return;
        }

        if (!confirmed)
        {
            if (
                !player.ConfirmationManager.EnqueueSend(
                    new Confirmation_CraftInteration(player.Guid, source.Guid, target.Guid),
                    "Release the device's stored charge into your equipped items now? This fully drains the device and starts a seven-day cooldown before it can be used this way again - it cannot be undone."
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

        var topOffPool = (int)
            Math.Round(
                RimsReleaseTopOffPercent
                    * player.EquippedObjects.Values.Where(k => k.ItemMaxMana.HasValue).Sum(k => k.ItemMaxMana.Value)
            );

        if (topOffPool > 0)
        {
            player.TopOffEquippedItemsMana(topOffPool);
        }

        player.QuestManager.Stamp(RimsReleaseCooldownQuest);
        player.QuestManager.Erase(RimsChargeLevelQuest);

        player.Session.Network.EnqueueSend(
            new GameMessageSystemChat(
                "The device empties itself, its stored resonance flowing into your equipped gear.",
                ChatMessageType.Broadcast
            )
        );

        player.SendUseDoneEvent();
    }
}
