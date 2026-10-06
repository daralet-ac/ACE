using ACE.Server.WorldObjects;

namespace ACE.Server.Entity;

/// <summary>
/// Items flagged with an AlternateSetup have two looks, and players switch between them with /style.
/// Switching swaps the item's Setup with its AlternateSetup, so the look the item isn't showing is always the
/// alternate one and switching again brings the first look back. Nothing else about the item changes.
/// </summary>
public static class ItemSetupToggle
{
    /// <summary>
    /// True when the item has a second Setup to switch to that differs from the one it shows.
    /// An item with no Setup of its own can't switch, since switching back would have nothing to return to.
    /// </summary>
    public static bool HasAlternateSetup(WorldObject item)
    {
        var alternate = item?.AlternateSetup ?? 0;
        return alternate != 0 && item.SetupTableId != 0 && alternate != item.SetupTableId;
    }

    /// <summary>
    /// Swaps the item's Setup with its AlternateSetup. Returns false, changing nothing, for an item without one.
    /// </summary>
    public static bool Toggle(WorldObject item)
    {
        if (!HasAlternateSetup(item))
        {
            return false;
        }

        var current = item.SetupTableId;
        item.SetupTableId = item.AlternateSetup.Value;
        item.AlternateSetup = current;

        return true;
    }
}
