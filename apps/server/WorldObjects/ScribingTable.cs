using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Entity.Models;
using ACE.Server.Network.GameEvent.Events;

namespace ACE.Server.WorldObjects;

/// <summary>
/// A player-summoned crafting station. Give (drag/drop) a scroll from inventory onto it to break
/// the scroll back down into Spell Dust (amount equal to the scroll's spell level). Handled via
/// the "give object" pathway (Player_Inventory.GiveObjectToNPC).
/// </summary>
public class ScribingTable : Pet
{
    /// <summary>
    /// A new biota be created taking all of its values from weenie.
    /// </summary>
    public ScribingTable(Weenie weenie, ObjectGuid guid)
        : base(weenie, guid)
    {
        SetEphemeralValues();
    }

    /// <summary>
    /// Restore a WorldObject from the database.
    /// </summary>
    public ScribingTable(Biota biota)
        : base(biota)
    {
        SetEphemeralValues();
    }

    private void SetEphemeralValues()
    {
        // Pet.SetEphemeralValues() sets IsPassivePet based on WeenieType == WeenieType.Pet, which is
        // false for us (we're WeenieType.ScribingTable so the factory constructs this class instead of
        // the base Pet). Force it back on so Pet.Init() takes the passive-placement / no-rot path.
        IsPassivePet = true;

        // Pet.SetEphemeralValues() unconditionally sets ItemUseable = Usable.No, which would make
        // double-clicking the table do nothing. Giving scrolls to the table doesn't go through this
        // property at all (see Player_Inventory.GiveObjectToNPC) — this is purely for the plain
        // double-click popup (ActOnUse below), so it must NOT be a SourceXTargetY value (that arms a
        // use-with-target reticle instead of firing a plain use).
        ItemUseable = Usable.Remote;
    }

    /// <summary>
    /// Double-clicking the table shows usage instructions. Requires PropertyInt.ActivationResponse to
    /// include the Use flag (0x2) on the weenie, or WorldObject.OnActivate won't call this at all.
    /// </summary>
    public override void ActOnUse(WorldObject activator)
    {
        if (activator is not Player player)
        {
            return;
        }

        player.Session.Network.EnqueueSend(
            new GameEventPopupString(
                player.Session,
                "Disenchant Scroll: Drag a scroll to the Scribing Table to convert it to Spell Dust, obtaining 1 dust per spell level.\n\n"
                    + "Scroll Crafting: Use a Blank Scroll, then target each of the spell's components (excluding tapers) in your "
                    + "inventory, one at a time and in the correct order, to scribe them onto the scroll. Once all components are scribed, use "
                    + "Spell Dust on the finished scroll to complete it."
            )
        );
    }
}
