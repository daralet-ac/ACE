using ACE.Entity;
using ACE.Entity.Models;

namespace ACE.Server.WorldObjects;

/// <summary>
/// Reagent used to finalize a scribed Blank Scroll into a real spell scroll (Scroll Writing / Spellcrafting).
/// </summary>
public class SpellDust : Stackable
{
    /// <summary>
    /// A new biota be created taking all of its values from weenie.
    /// </summary>
    public SpellDust(Weenie weenie, ObjectGuid guid)
        : base(weenie, guid)
    {
        SetEphemeralValues();
    }

    /// <summary>
    /// Restore a WorldObject from the database.
    /// </summary>
    public SpellDust(Biota biota)
        : base(biota)
    {
        SetEphemeralValues();
    }

    private void SetEphemeralValues() { }

    public override void HandleActionUseOnTarget(Player player, WorldObject target)
    {
        ScrollFinalizer.TryFinalizeScroll(player, this, target);
    }
}
