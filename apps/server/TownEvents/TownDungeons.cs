using System.Collections.Generic;

namespace ACE.Server.TownEvents;

/// <summary>
/// The town each dungeon belongs to: the one nearest its entrance on the map. A dungeon's landblock numbers are only a slot in
/// the dat, so its own position says nothing of where it is.
/// </summary>
public static partial class TownDungeons
{
    /// <summary>
    /// The town nearest this dungeon's entrance, or null for a dungeon with no known way in from the map
    /// </summary>
    public static string GetTown(uint landblock) => Towns.GetValueOrDefault((ushort)landblock);
}
