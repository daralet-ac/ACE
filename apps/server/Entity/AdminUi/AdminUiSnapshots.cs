using System.Collections.Generic;
using System.Linq;
using ACE.Database;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Entity.Models;
using ACE.Server.Managers;
using ACE.Server.WorldObjects;

namespace ACE.Server.Entity.AdminUi;

/// <summary>
/// Who the plugin is talking for. Tabs is the access level each of the plugin's tabs needs, and Commands the level each
/// command its buttons send needs (both from AdminUiAccess), so it only offers what this account can use.
/// </summary>
public record AdminUiHello(
    int Protocol,
    string Name,
    string AccessLevel,
    int AccessLevelValue,
    Dictionary<string, int> Tabs,
    Dictionary<string, int> Commands
);

public record AdminUiPlayer(
    uint Guid,
    string Name,
    string Account,
    int Level,
    string AccessLevel,
    string Allegiance,
    string Monarch,
    string Location,
    string Cell,
    float? MapX,
    float? MapY,
    string Fellowship,
    string PkStatus,
    bool Gagged,
    bool Frozen
);

public record AdminUiFellow(
    uint Guid,
    string Name,
    int Level,
    uint Health,
    uint MaxHealth,
    uint Stamina,
    uint MaxStamina,
    uint Mana,
    uint MaxMana,
    string Location
);

public record AdminUiFellowship(
    string Name,
    string Leader,
    bool Open,
    bool ShareXp,
    bool EvenShare,
    bool Locked,
    List<AdminUiFellow> Members
);

/// <summary>
/// A point of interest @telepoi can go to. MapX is east (+) / west (-) and MapY is north (+) / south (-), in map units, or
/// null when the POI is in a dungeon.
/// </summary>
public record AdminUiPoi(string Name, float? MapX, float? MapY);

/// <summary>
/// Reads what the Daralet Admin Decal plugin shows from the players that are online right now.
/// </summary>
public static class AdminUiSnapshots
{
    public static List<AdminUiPlayer> GetPlayers()
    {
        return PlayerManager.GetAllOnline().OrderBy(p => p.Name).Select(GetPlayer).ToList();
    }

    public static AdminUiPlayer GetPlayer(Player player)
    {
        var allegiance = player.Allegiance;
        var monarch = allegiance?.Monarch?.Player?.Name;
        var mapCoords = player.Location.GetMapCoords();

        return new AdminUiPlayer(
            player.Guid.Full,
            player.Name,
            player.Account?.AccountName,
            player.Level ?? 0,
            (player.Session?.AccessLevel ?? ACE.Entity.Enum.AccessLevel.Player).ToString(),
            allegiance?.AllegianceName ?? monarch,
            monarch,
            PlayerLocationInfo.GetDisplayString(player),
            $"0x{player.Location.Cell:X8}",
            mapCoords?.X,
            mapCoords?.Y,
            player.Fellowship?.FellowshipName,
            player.PlayerKillerStatus.ToString(),
            player.IsGagged,
            player.IsAdminFrozen
        );
    }

    public static List<AdminUiFellowship> GetFellowships()
    {
        var results = new List<AdminUiFellowship>();
        var seen = new HashSet<Fellowship>();

        foreach (var player in PlayerManager.GetAllOnline())
        {
            var fellowship = player.Fellowship;

            if (fellowship == null || !seen.Add(fellowship))
            {
                continue;
            }

            var members = fellowship.GetFellowshipMembers().Values;

            results.Add(
                new AdminUiFellowship(
                    fellowship.FellowshipName,
                    PlayerManager.FindByGuid(fellowship.FellowshipLeaderGuid)?.Name,
                    fellowship.Open,
                    fellowship.ShareXP,
                    fellowship.EvenShare,
                    fellowship.IsLocked,
                    members
                        .OrderByDescending(m => m.Guid.Full == fellowship.FellowshipLeaderGuid)
                        .ThenBy(m => m.Name)
                        .Select(GetFellow)
                        .ToList()
                )
            );
        }

        return results.OrderBy(f => f.Name).ToList();
    }

    /// <summary>
    /// A player with their level, vitals and location: a fellowship member, or anyone the other tabs list.
    /// </summary>
    public static AdminUiFellow GetFellow(Player player)
    {
        return new AdminUiFellow(
            player.Guid.Full,
            player.Name,
            player.Level ?? 0,
            player.Health.Current,
            player.Health.MaxValue,
            player.Stamina.Current,
            player.Stamina.MaxValue,
            player.Mana.Current,
            player.Mana.MaxValue,
            PlayerLocationInfo.GetDisplayString(player)
        );
    }

    private static List<AdminUiPoi> cachedPois;

    /// <summary>
    /// Every point of interest. They only change with the world database, so they're only looked up once.
    /// </summary>
    public static List<AdminUiPoi> GetPois()
    {
        if (cachedPois != null)
        {
            return cachedPois;
        }

        DatabaseManager.World.CacheAllPointsOfInterest();

        var results = new List<AdminUiPoi>();

        foreach (var poi in DatabaseManager.World.GetPointsOfInterestCache().Values)
        {
            if (poi == null)
            {
                continue;
            }

            var destination = DatabaseManager
                .World.GetCachedWeenie(poi.WeenieClassId)
                ?.GetPosition(PositionType.Destination);

            var mapCoords = destination != null ? new Position(destination).GetMapCoords() : null;

            results.Add(new AdminUiPoi(poi.Name, mapCoords?.X, mapCoords?.Y));
        }

        cachedPois = results.OrderBy(p => p.Name).ToList();

        return cachedPois;
    }
}
