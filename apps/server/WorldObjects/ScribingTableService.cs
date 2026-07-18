using ACE.Database;
using ACE.Entity.Enum;
using ACE.Server.Network.GameEvent.Events;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.WorldObjects;

/// <summary>
/// Reverse of Scroll Writing: breaking a real spell scroll back down into Spell Dust
/// (amount equal to the scroll's spell level) at a player-summoned Scribing Table.
/// Invoked from Player_Inventory.GiveObjectToNPC when a scroll is given (dragged/dropped) onto
/// a Scribing Table — the same "give object to a landblock object" pathway real quest NPCs use
/// when they accept and consume an item (see the RemoveItemForGive/EmoteCategory.Give branch of
/// GiveObjectToNPC), since a landblock object like the table can never be a use-with-target source.
///
/// The scroll is removed via TryRemoveFromInventoryWithNetworking(..., RemoveFromInventoryAction.GiveItem)
/// rather than TryConsumeFromInventoryWithNetworking, and followed by GameEventItemServerSaysContainId —
/// this is the exact protocol the client expects to resolve a give-object transaction it initiated.
/// The generic "consume" removal path sends a different network message shape, which left the client
/// waiting on an acknowledgment that never came (a stuck loading cursor).
/// </summary>
public static class ScribingTableService
{
    /// <summary>
    /// Lazily-cached WCID of the Spell Dust weenie, found by WeenieType rather than hardcoded,
    /// since Spell Dust's WCID is picked ad hoc in world DB content.
    /// </summary>
    private static uint? _spellDustWcid;

    private static uint? SpellDustWcid
    {
        get
        {
            _spellDustWcid ??= DatabaseManager.World.GetFirstWeenieClassIdByType(WeenieType.SpellDust);
            return _spellDustWcid;
        }
    }

    /// <summary>
    /// Default range (in the absence of a weenie-configured UseRadius) within which a summoned
    /// Scribing Table must be to scribe/finalize a scroll or disenchant one.
    /// </summary>
    private const float DefaultUseRadius = 6f;

    /// <summary>
    /// Returns TRUE if the player currently has a Scribing Table summoned as their active pet and is
    /// within range of it. Required for scribing components onto a Blank Scroll, finalizing one with
    /// Spell Dust, and disenchanting a scroll back into Spell Dust — all Scroll Writing interactions
    /// happen at the table, not just the scroll->dust direction.
    /// </summary>
    public static bool IsNearActiveTable(Player player)
    {
        return player.CurrentActivePet is ScribingTable table
            && player.GetDistance(table) <= (table.UseRadius ?? DefaultUseRadius);
    }

    /// <summary>
    /// Returns TRUE if the given item was consumed (destroyed), FALSE if it's untouched. The caller
    /// uses this to decide whether to tell the client the item "failed to save" back into the target
    /// container (correct only when the item still exists) — sending that for an item this method
    /// already destroyed (and already separately notified the client about via
    /// TryConsumeFromInventoryWithNetworking) would be a contradictory, stale message.
    /// </summary>
    public static bool TryConvertScrollToDust(Player player, ScribingTable table, WorldObject target)
    {
        if (target is not Scroll scroll)
        {
            player.Session.Network.EnqueueSend(
                new GameMessageSystemChat("You can only use the Scribing Table on a scroll.", ChatMessageType.Craft)
            );
            return false;
        }

        if (scroll.Spell == null)
        {
            player.Session.Network.EnqueueSend(
                new GameMessageSystemChat("That scroll does not have a valid spell.", ChatMessageType.Craft)
            );
            return false;
        }

        var level = scroll.Spell.Formula.Level;

        if (level == 0)
        {
            player.Session.Network.EnqueueSend(
                new GameMessageSystemChat("That scroll's spell formula is invalid.", ChatMessageType.Craft)
            );
            return false;
        }

        var dustWcid = SpellDustWcid;

        if (dustWcid == null)
        {
            player.SendTransientError("Spell Dust does not exist in this world's content yet.");
            return false;
        }

        var dustAmount = (int)level;
        var spellName = scroll.Spell.Name;

        // Same removal + acknowledgment sequence as RemoveItemForGive's whole-object branch
        // (Player_Inventory.cs): remove with the GiveItem reason, tell the client the item is
        // contained by the target, then destroy it.
        if (!player.TryRemoveFromInventoryWithNetworking(scroll.Guid, out _, Player.RemoveFromInventoryAction.GiveItem))
        {
            player.SendTransientError("Failed to remove the scroll from your inventory.");
            return false;
        }

        player.Session.Network.EnqueueSend(new GameEventItemServerSaysContainId(player.Session, scroll, table));

        scroll.Destroy();

        // Merges into an existing Spell Dust stack with room instead of always fragmenting into a
        // separate stack, only creating a new one for any remainder.
        if (!player.TryCreateOrMergeStackInInventoryWithNetworking(dustWcid.Value, dustAmount))
        {
            player.SendTransientError("Failed to add Spell Dust to your inventory.");
            return true; // scroll was already consumed above, even though dust creation failed
        }

        player.Session.Network.EnqueueSend(
            new GameMessageSystemChat(
                $"You disenchant the Scroll of {spellName} into {dustAmount} Spell Dust.",
                ChatMessageType.Craft
            )
        );

        return true;
    }
}
