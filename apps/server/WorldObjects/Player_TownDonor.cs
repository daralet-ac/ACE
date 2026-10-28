using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Network.GameEvent.Events;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.TownEvents;

namespace ACE.Server.WorldObjects;

partial class Player
{
    /// <summary>
    /// What the town donor buff adds to XP, on top of trinkets and the like (TownCaptain)
    /// </summary>
    public const float TownDonorXpBonus = 0.10f;

    /// <summary>
    /// Seconds left on the XP buff from donating to a Captain of the Guard, or 0. It isn't a spell (a new spell would need
    /// the client's dat), so it shows no icon; the Captain says how long is left.
    /// </summary>
    public double TownDonorXpBuffDuration
    {
        get => GetProperty(PropertyFloat.TownDonorXpBuffDuration) ?? 0;
        set
        {
            if (value <= 0)
            {
                RemoveProperty(PropertyFloat.TownDonorXpBuffDuration);
            }
            else
            {
                SetProperty(PropertyFloat.TownDonorXpBuffDuration, value);
            }
        }
    }

    public bool HasTownDonorXpBuff => TownDonorXpBuffDuration > 0;

    /// <summary>
    /// Starts the buff, or tops it back up to the full time: donations don't stack the time
    /// </summary>
    public void GrantTownDonorXpBuff(double seconds)
    {
        TownDonorXpBuffDuration = System.Math.Max(TownDonorXpBuffDuration, seconds);
    }

    /// <summary>
    /// Giving something to a Captain of the Guard: pyreals and trade notes are donated to the town's supply, any amount,
    /// and anything else is handed back
    /// </summary>
    private void GiveToTownCaptain(
        WorldObject captain,
        WorldObject item,
        Container itemFoundInContainer,
        Container itemRootOwner,
        bool itemWasEquipped,
        int amount
    )
    {
        // worked out before the stack is split, while the item still holds the whole stack's value
        var pyreals = TownCaptain.GetDonationValue(item, amount);

        if (pyreals <= 0)
        {
            Session.Network.EnqueueSend(
                new GameEventInventoryServerSaveFailed(Session, item.Guid.Full, WeenieError.TradeAiRefuseEmote)
            );
            TownCaptain.Tell(this, captain, "I can't use that. Pyreals or trade notes keep the guards supplied.");
            return;
        }

        if (!RemoveItemForGive(item, itemFoundInContainer, itemWasEquipped, itemRootOwner, amount, out var itemToGive))
        {
            return;
        }

        if (item == itemToGive)
        {
            Session.Network.EnqueueSend(new GameEventItemServerSaysContainId(Session, item, captain));
        }

        var stackSize = itemToGive.StackSize ?? 1;
        var stackMsg = stackSize != 1 ? $"{stackSize:N0} " : "";

        Session.Network.EnqueueSend(
            new GameMessageSystemChat($"You give {captain.Name} {stackMsg}{itemToGive.GetNameWithMaterial(stackSize)}.", ChatMessageType.Broadcast)
        );
        captain.EnqueueBroadcast(new GameMessageSound(captain.Guid, Sound.ReceiveItem));

        itemToGive.Destroy();

        TownCaptain.OnDonation(this, captain, pyreals);
    }

    public void TownDonorXpBuffTick()
    {
        if (!HasTownDonorXpBuff)
        {
            return;
        }

        var remaining = TownDonorXpBuffDuration - CachedHeartbeatInterval;
        TownDonorXpBuffDuration = remaining;

        if (remaining <= 0)
        {
            Session?.Network.EnqueueSend(
                new GameMessageSystemChat("The town's gratitude fades: your donor's XP bonus has ended.", ChatMessageType.Magic)
            );
        }
    }
}
