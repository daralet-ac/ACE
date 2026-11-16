using System;
using ACE.Entity.Enum;
using ACE.Server.Factories;

namespace ACE.Server.WorldObjects;

/// <summary>
/// Salvage is stackable: one weenie per material and workmanship tier, each unit of the stack a unit of salvage. The
/// weenies are laid out by formula from BaseWcid (BaseWcid + material * 10 + tier - 1), so a material and tier map to a
/// WCID without a table. Bags from before stacking (the per-material weenies in Player.MaterialSalvage) are legacy, and
/// are converted to stacks when their owner logs in.
/// </summary>
public static class SalvageStacks
{
    public const uint BaseWcid = 1059000;
    public const int MinTier = 1;
    public const int MaxTier = 10;

    /// <summary>
    /// The most units a stack holds, as the MaxStackSize of the weenies
    /// </summary>
    public const int MaxStackSize = 1000;

    private const int HighestMaterial = (int)MaterialType.Teak;

    /// <summary>
    /// The stack weenie for the material at the tier, or 0 if the material has no salvage
    /// </summary>
    public static uint GetWcid(MaterialType material, int tier)
    {
        if (!HasSalvage(material))
        {
            return 0;
        }

        return BaseWcid + (uint)material * 10 + (uint)(Math.Clamp(tier, MinTier, MaxTier) - 1);
    }

    public static bool HasSalvage(MaterialType material)
    {
        return Player.MaterialSalvage.TryGetValue((int)material, out var wcid) && wcid != 0;
    }

    /// <summary>
    /// Whether the WCID is one of the salvage stack weenies
    /// </summary>
    public static bool IsStackWcid(uint wcid)
    {
        if (wcid < BaseWcid + 10 || wcid >= BaseWcid + (uint)(HighestMaterial + 1) * 10)
        {
            return false;
        }

        return HasSalvage((MaterialType)((wcid - BaseWcid) / 10));
    }

    public static bool IsStack(WorldObject wo)
    {
        return wo != null && IsStackWcid(wo.WeenieClassId);
    }

    /// <summary>
    /// A bag of salvage from before stacking, still to be converted
    /// </summary>
    public static bool IsLegacyBag(WorldObject wo)
    {
        return wo != null && wo.WeenieType == WeenieType.Salvage && !IsStackWcid(wo.WeenieClassId);
    }

    /// <summary>
    /// The tier of a stack (or legacy bag), the workmanship rounded the way /salvage combine always grouped bags
    /// </summary>
    public static int GetTier(WorldObject wo)
    {
        return Math.Clamp((int)Math.Round(wo.Workmanship ?? 1), MinTier, MaxTier);
    }

    /// <summary>
    /// The units of salvage in a stack, or in a legacy bag
    /// </summary>
    public static int GetUnits(WorldObject wo)
    {
        return IsLegacyBag(wo) ? wo.Structure ?? 0 : wo.StackSize ?? 1;
    }

    /// <summary>
    /// A new stack of units of the material at the tier, or null if the material has no salvage. Units are capped at
    /// MaxStackSize.
    /// </summary>
    public static WorldObject Create(MaterialType material, int tier, int units)
    {
        var wcid = GetWcid(material, tier);

        if (wcid == 0 || units < 1)
        {
            return null;
        }

        var stack = WorldObjectFactory.CreateNewWorldObject(wcid);
        stack?.SetStackSize(Math.Min(units, MaxStackSize));

        return stack;
    }
}
