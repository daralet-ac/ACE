using ACE.Entity;
using ACE.Entity.Models;
using ACE.Server.Network.GameEvent.Events;

namespace ACE.Server.WorldObjects;

/// <summary>
/// A town crafting station. Give (drag/drop) a bag of salvage onto it to refine the bag one workmanship higher, at
/// 2 units of the bag's workmanship per unit of the next (see SalvageRefineryService). Built on Creature, as the
/// Scribing Table is, because the client's give-object pathway (Player_Inventory.GiveObjectToNPC) only targets
/// containers in the world; the weenie sets NpcLooksLikeObject so it shows as an object, not an NPC.
/// </summary>
public class SalvageRefinery : Creature
{
    /// <summary>
    /// A new biota be created taking all of its values from weenie.
    /// </summary>
    public SalvageRefinery(Weenie weenie, ObjectGuid guid)
        : base(weenie, guid)
    {
    }

    /// <summary>
    /// Restore a WorldObject from the database.
    /// </summary>
    public SalvageRefinery(Biota biota)
        : base(biota)
    {
    }

    /// <summary>
    /// Double-clicking the refinery shows usage instructions. Requires PropertyInt.ActivationResponse to include the
    /// Use flag (0x2) on the weenie, or WorldObject.OnActivate won't call this at all.
    /// </summary>
    public override void ActOnUse(WorldObject activator)
    {
        if (activator is not Player player)
        {
            return;
        }

        player.Session.Network.EnqueueSend(new GameEventPopupString(player.Session, SalvageRefineryService.Instructions));
    }
}
