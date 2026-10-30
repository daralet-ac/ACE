using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Network.GameEvent.Events;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.Network.Structure;
using ACE.Server.TownEvents;
using Spell = ACE.Server.Entity.Spell;

namespace ACE.Server.WorldObjects;

partial class Player
{
    /// <summary>
    /// What Guard's Gratitude, the town donor buff, adds to XP (its GlobalXpMod in the spell table), for the Captain to tell
    /// </summary>
    public const float TownDonorXpBonus = 0.10f;

    /// <summary>
    /// The pyreals this player has given toward Guard's Gratitude since they last earned it
    /// </summary>
    public double TownDonorProgress
    {
        get => GetProperty(PropertyFloat.TownDonorProgress) ?? 0;
        set
        {
            if (value <= 0)
            {
                RemoveProperty(PropertyFloat.TownDonorProgress);
            }
            else
            {
                SetProperty(PropertyFloat.TownDonorProgress, value);
            }
        }
    }

    public bool HasTownDonorXpBuff => EnchantmentManager.HasSpell((uint)SpellId.GuardsGratitude);

    /// <summary>
    /// Seconds left on Guard's Gratitude, or 0
    /// </summary>
    public double TownDonorXpBuffSecondsLeft
    {
        get
        {
            var enchantment = EnchantmentManager.GetEnchantment((uint)SpellId.GuardsGratitude);
            return enchantment == null ? 0 : System.Math.Max(0, enchantment.Duration + enchantment.StartTime);
        }
    }

    /// <summary>
    /// Casts Guard's Gratitude on the player, or tops it back up to its full hour: it doesn't stack
    /// </summary>
    public void GrantTownDonorXpBuff()
    {
        var spell = new Spell((int)SpellId.GuardsGratitude);
        if (spell.NotFound)
        {
            _log.Error("[TOWN EVENT] Guard's Gratitude ({SpellId}) isn't in the spell table or the dat", (int)SpellId.GuardsGratitude);
            return;
        }

        var addResult = EnchantmentManager.Add(spell, null, null, true);
        Session.Network.EnqueueSend(new GameEventMagicUpdateEnchantment(Session, new Enchantment(this, addResult.Enchantment)));
        HandleSpellHooks(spell);
        PlayParticleEffect(PlayScript.SkillUpYellow, Guid);
    }

    /// <summary>
    /// Giving something to a Captain of the Guard: pyreals, trade notes and Supply Requisitions are donated to the town's
    /// supply, any amount, and anything else is handed back
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
        var requisition = TownCaptain.IsSupplyRequisition(item);

        if (pyreals <= 0)
        {
            Session.Network.EnqueueSend(
                new GameEventInventoryServerSaveFailed(Session, item.Guid.Full, WeenieError.TradeAiRefuseEmote)
            );
            TownCaptain.Tell(this, captain, "I can't use that. Pyreals, trade notes or supply requisitions keep the guards supplied.");
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

        TownCaptain.OnDonation(this, captain, pyreals, requisition);
    }
}
