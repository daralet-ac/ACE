using System.Linq;
using ACE.Database.Models.World;
using ACE.Server.WorldObjects;

namespace ACE.Server.Entity;

/// <summary>
/// Builds the "Loc: <name/id> (coords or Indoors) (Nearest town: X)" display string used by
/// both the in-game /listplayers command and the Discord list-players command.
/// </summary>
public static class PlayerLocationInfo
{
    public static string GetDisplayString(Player player)
    {
        var namedLocation = GetLocationName(player.Location.LandblockId.Raw);
        var locationName = namedLocation ?? player.Location.LandblockId.ToString();
        var coordinates = player.Location.GetMapCoordStr();
        var parenthesis = coordinates != null ? $" ({coordinates})" : " (Indoors)";

        var location = $"{locationName}{parenthesis}";

        if (namedLocation is null && Town.HasReliableNearestTown(player))
        {
            var nearestTown = Town.GetNearestTown(player);
            location += $" (Nearest town: {nearestTown})";
        }

        return location;
    }

    private static string GetLocationName(uint cellId)
    {
        using var ctx = new WorldDbContext();

        var name = ctx
            .LandblockName.Where(landblockName => landblockName.ObjCellId == cellId)
            .Select(landblockName => landblockName.Name)
            .FirstOrDefault();

        if (name is null)
        {
            var landblockId = (cellId | 0xFFFF) - 0xFFFF;

            name = ctx
                .LandblockName.Where(landblockName => landblockName.ObjCellId == landblockId)
                .Select(landblockName => landblockName.Name)
                .FirstOrDefault();
        }

        return name;
    }
}
