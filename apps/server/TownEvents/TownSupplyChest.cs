using System;
using System.Linq;
using ACE.Entity.Enum.Properties;
using ACE.Server.WorldObjects;

namespace ACE.Server.TownEvents;

/// <summary>
/// A town's Supply Chest: the guards' supplies, standing near the Captain of the Guard (world-db, Template "Town Supply
/// Chest"). Some attacks on a town with one are chest raids, which march on to it; raiders who stay at it long enough
/// loot it, and the attack fails (TownEvent).
/// </summary>
public static class TownSupplyChest
{
    public const string TemplateName = "Town Supply Chest";

    // the chest counts as the town's this far from its centre
    private const float SearchRadius = 300f;

    public static bool IsSupplyChest(WorldObject wo) =>
        wo != null && string.Equals(wo.GetProperty(PropertyString.Template), TemplateName, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// The town's chest on a loaded landblock near its centre, or null. Call on the world thread.
    /// </summary>
    public static WorldObject Find(string town) => TownGeography.FindNear(town, SearchRadius, IsSupplyChest).FirstOrDefault();
}
