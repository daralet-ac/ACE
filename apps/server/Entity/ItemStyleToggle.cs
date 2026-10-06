using ACE.Server.WorldObjects;

namespace ACE.Server.Entity;

/// <summary>
/// Items flagged with an AlternateClothingBase have two looks, and players switch between them with /style.
/// Switching swaps the item's ClothingBase with its AlternateClothingBase, so the look the item isn't showing is
/// always the alternate one and switching again brings the first look back. Nothing else about the item changes.
/// </summary>
public static class ItemStyleToggle
{
    /// <summary>
    /// True when the item has a second ClothingBase to switch to that differs from the one it shows.
    /// An item with no ClothingBase of its own can't switch, since switching back would have nothing to return to.
    /// </summary>
    public static bool HasAlternateStyle(WorldObject item)
    {
        var alternate = item?.AlternateClothingBase ?? 0;
        var current = item?.ClothingBase ?? 0;
        return alternate != 0 && current != 0 && alternate != current;
    }

    /// <summary>
    /// Swaps the item's ClothingBase with its AlternateClothingBase. Returns false, changing nothing, for an item
    /// without one.
    /// </summary>
    public static bool Toggle(WorldObject item)
    {
        if (!HasAlternateStyle(item))
        {
            return false;
        }

        var current = item.ClothingBase.Value;
        item.ClothingBase = item.AlternateClothingBase.Value;
        item.AlternateClothingBase = current;

        return true;
    }
}
