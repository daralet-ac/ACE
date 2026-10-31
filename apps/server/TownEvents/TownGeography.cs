using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ACE.Entity;
using ACE.Entity.Enum.Properties;
using ACE.Server.Entity;
using ACE.Server.Entity.AdminUi;
using ACE.Server.Managers;
using ACE.Server.WorldObjects;
using ACE.Server.WorldObjects.Patrol;

namespace ACE.Server.TownEvents;

/// <summary>
/// A road into town, from where attackers spawn (the far end of a guard's patrol route) to the town centre
/// </summary>
public sealed class TownEntryRoute
{
    public List<Position> Waypoints { get; init; }

    public Position Entry => Waypoints[0];
}

/// <summary>
/// Where a town is: its region (the nearest-town area town attunement uses), the town area itself, and the roads in.
/// The roads come from the town's patrolling guards, whose routes (town-guards scripts in world-db) were checked for
/// clearance and slope and run out along the roads to the town's edge.
/// </summary>
public static class TownGeography
{
    /// <summary>
    /// How far from the town centre the town area reaches, in meters: where the Town Defense prompt is offered and
    /// /towndefense join works. Members may go farther, out to the roads attackers come in on (TownEvent.InBattleArea)
    /// </summary>
    public const float TownAreaRadius = 150f;

    // guards this far from the centre count as the town's
    internal const float GuardSearchRadius = 300f;

    // a route whose far end is closer than this to the centre doesn't leave town, so it's no road in
    private const float MinEntryDistance = 60f;

    // entry points closer together than this are the same road
    private const float SameEntryDistance = 30f;

    // a lead-in this close to a road's entry, and farther out, becomes the road's new entry
    private const float LeadInReach = 120f;

    // in the Esper Mountains, but with guards to hold off an attack
    private const string RaidableEsperTown = "Stonehold";

    /// <summary>
    /// Where a road should start when the guards' patrol ends too close to town: a landblock cell and its local x, y. The
    /// road begins here and joins the guards' route at its far end.
    /// </summary>
    private static readonly Dictionary<string, (uint Cell, float X, float Y)[]> RoadLeadIns = new()
    {
        // the north-western road, just past the town sign (0x97220016 [50.3 142.1])
        { "Qalaba'r", new[] { (0x9722000Fu, 37.8f, 150.4f) } },
    };

    public static float DistanceToCenter(string town, Position position)
    {
        var center = Town.GetTownCenterGlobal(town);
        if (center == null || position == null)
        {
            return float.MaxValue;
        }

        var global = position.ToGlobal();
        return Vector2.Distance(new Vector2(global.X, global.Y), center.Value);
    }

    /// <summary>
    /// The town whose region the player is in: the same nearest-town check town attunement uses, which includes
    /// dungeons near (or filed near) the town
    /// </summary>
    public static string GetRegion(Player player) => player.Location == null ? null : Town.GetNearestTown(player);

    /// <summary>
    /// The town whose unrest what happens here stirs up: the nearest, or for a dungeon the one nearest its entrance
    /// (TownDungeons). Null for a dungeon with no known way in from the map, and in the endgame zones, except around Stonehold.
    /// </summary>
    public static string GetUnrestTown(WorldObject worldObject)
    {
        var location = worldObject.Location;

        if (!Town.IsOnMap(location.Cell))
        {
            return TownDungeons.GetTown(location.Landblock);
        }

        if (AdminUiRegions.IsInOlthoiNorth(location.LandblockId))
        {
            return null;
        }

        var town = Town.GetNearestTown(worldObject);

        if (IsEndgameTown(town) || (EsperMountainsZone.Contains(location) && town != RaidableEsperTown))
        {
            return null;
        }

        return town;
    }

    /// <summary>
    /// Whether a town is in one of the endgame zones (Olthoi North, the Esper Mountains), other than Stonehold. They stir up no
    /// unrest, since nothing there can yet hold off an attack, and the low-level towns nearest them shouldn't pay for what's
    /// killed there.
    /// </summary>
    public static bool IsEndgameTown(string town)
    {
        var center = Town.GetTownCenterGlobal(town);

        return center != null
            && town != RaidableEsperTown
            && (AdminUiRegions.IsInOlthoiNorth(Town.GetTownCenterLandblock(town)) || EsperMountainsZone.Contains(center.Value.X, center.Value.Y));
    }

    /// <summary>
    /// Whether the player is in the town area: outdoors or in one of its buildings, within TownAreaRadius of the centre
    /// </summary>
    public static bool InTownArea(Player player, string town)
    {
        if (player?.Location == null || !Town.IsOnMap(player.Location.Cell))
        {
            return false;
        }

        return DistanceToCenter(town, player.Location) <= TownAreaRadius;
    }

    /// <summary>
    /// The town's patrolling guards: patrollers near its centre on loaded landblocks
    /// </summary>
    public static List<Creature> FindGuards(string town) =>
        FindNear(town, GuardSearchRadius, wo => wo is Creature { HasPatrol: true, IsDead: false } and not Player)
            .Cast<Creature>()
            .ToList();

    /// <summary>
    /// Objects on the loaded landblocks around the town centre, within this distance of it, that match
    /// </summary>
    public static List<WorldObject> FindNear(string town, float radius, Func<WorldObject, bool> match)
    {
        var found = new List<WorldObject>();
        var centerBlock = Town.GetTownCenterLandblock(town);

        for (var dx = -1; dx <= 1; dx++)
        {
            for (var dy = -1; dy <= 1; dy++)
            {
                var x = centerBlock.LandblockX + dx;
                var y = centerBlock.LandblockY + dy;
                if (x is < 0 or > 0xFE || y is < 0 or > 0xFE)
                {
                    continue;
                }

                var id = new LandblockId((byte)x, (byte)y);
                if (!LandblockManager.IsLoaded(id))
                {
                    continue;
                }

                var landblock = LandblockManager.GetLandblock(id, false);
                foreach (var wo in landblock.GetAllWorldObjectsForDiagnostics())
                {
                    if (match(wo) && DistanceToCenter(town, wo.Location) <= radius)
                    {
                        found.Add(wo);
                    }
                }
            }
        }

        return found;
    }

    /// <summary>
    /// The waypoints of a looping route, by index, from the one farthest from the town centre to the one nearest it, going
    /// the shorter way round the loop
    /// </summary>
    /// <param name="distances">Each waypoint's distance from the town centre, in route order</param>
    public static List<int> InboundOrder(IReadOnlyList<float> distances)
    {
        var far = 0;
        var near = 0;
        for (var i = 1; i < distances.Count; i++)
        {
            if (distances[i] > distances[far])
            {
                far = i;
            }
            if (distances[i] < distances[near])
            {
                near = i;
            }
        }

        var count = distances.Count;
        var forward = (near - far + count) % count;
        var backward = (far - near + count) % count;
        var step = forward <= backward ? 1 : -1;
        var legs = Math.Min(forward, backward);

        var order = new List<int>();
        for (var i = 0; i <= legs; i++)
        {
            order.Add(((far + step * i) % count + count) % count);
        }

        return order;
    }

    /// <summary>
    /// The roads into town, read off the guards' patrol routes: each route's waypoint farthest from the centre is an
    /// entry point, and the route walked from there toward the waypoint nearest the centre is the way in
    /// </summary>
    public static List<TownEntryRoute> FindEntryRoutes(string town, IEnumerable<Creature> guards)
    {
        var routes = new List<TownEntryRoute>();

        foreach (var guard in guards)
        {
            var path = PatrolPath.Parse(guard.GetProperty(PropertyString.PatrolPath));
            if (path == null || path.Count < 2)
            {
                continue;
            }

            // the patrol's offsets are from its home (the route start, for a guard sharing a route)
            var home = guard.Home ?? guard.Location;
            if (home == null || home.Indoors)
            {
                continue;
            }

            var waypoints = new List<Position>();
            for (var i = 0; i < path.Count; i++)
            {
                waypoints.Add(PatrolComponent.BuildOutdoorPosition(home, path[i].Dx, path[i].Dy, guard.InstanceId));
            }

            var distances = waypoints.Select(w => DistanceToCenter(town, w)).ToList();

            if (distances.Max() < MinEntryDistance)
            {
                continue;
            }

            var way = InboundOrder(distances).Select(i => waypoints[i]).ToList();

            if (routes.Any(r => r.Entry.DistanceTo(way[0]) < SameEntryDistance))
            {
                continue;
            }

            routes.Add(new TownEntryRoute { Waypoints = way });
        }

        if (RoadLeadIns.TryGetValue(town, out var leadIns) && routes.Count > 0)
        {
            var instance = guards.FirstOrDefault()?.InstanceId ?? 0;

            foreach (var (cell, x, y) in leadIns)
            {
                var start = new Position(cell, new Vector3(x, y, 0), Quaternion.Identity);
                start = PatrolComponent.BuildOutdoorPosition(start, 0, 0, instance);

                var route = routes.MinBy(r => r.Entry.DistanceTo(start));
                if (route.Entry.DistanceTo(start) <= LeadInReach && DistanceToCenter(town, start) > DistanceToCenter(town, route.Entry))
                {
                    route.Waypoints.Insert(0, start);
                }
            }
        }

        return routes;
    }
}
