using System.Collections.Generic;
using System.Linq;
using ACE.Entity;
using ACE.Server.Managers;
using ACE.Server.WorldObjects;

namespace ACE.Server.Entity.AdminUi;

/// <summary>
/// A world event and what state it's in (On, Off, Enabled, Disabled, or Undef when the event doesn't exist)
/// </summary>
public record AdminUiEvent(string Name, string State);

/// <summary>
/// One of the Olthoi North camps: its supply level (olthoi_north_camp_*_supply_level) and its Well / Greatly Supplied events
/// </summary>
public record AdminUiCamp(string Camp, long SupplyLevel, AdminUiEvent WellSupplied, AdminUiEvent GreatlySupplied);

public record AdminUiOlthoiPlayer(AdminUiFellow Player, uint DebuffStacks);

public record AdminUiOlthoiNorth(List<AdminUiCamp> Camps, List<AdminUiOlthoiPlayer> Players);

/// <summary>
/// Tide Mage's World Stability: the fragment_stability_phase_one counter, which band it's in, and the events the band
/// controllers turn on and off. The controllers only check the counter while the Mage House landblock is loaded.
/// </summary>
public record AdminUiTideMage(long Stability, string Band, bool MageHouseLoaded, List<AdminUiEvent> Events);

public record AdminUiEsper(List<AdminUiFellow> Players);

/// <summary>
/// The regional tabs: Olthoi North, Tide Mage and the Esper Mountains
/// </summary>
public static class AdminUiRegions
{
    public static readonly string[] OlthoiNorthCamps = { "North", "South", "West" };

    /// <summary>
    /// The camp controllers (2036541 / 2036542 and their South and West copies) turn these on at these supply levels
    /// </summary>
    public const long WellSuppliedLevel = 250;
    public const long GreatlySuppliedLevel = 750;

    // Olthoi North is every landblock world-db's Olthoi North folder builds on, from Lawrence's camp (BFBD) in the south-west
    // to the northern peaks (CBDB and CCDB), whether outdoors or in its caves
    private const byte OlthoiNorthMinX = 0xBF;
    private const byte OlthoiNorthMaxX = 0xD9;
    private const byte OlthoiNorthMinY = 0xB5;
    private const byte OlthoiNorthMaxY = 0xDB;

    /// <summary>
    /// The Mage House (world-db tide mage/Mage House/landblock), where the World Stability controllers are
    /// </summary>
    private static readonly LandblockId MageHouse = new LandblockId(0xCEA2FFFF);

    private static readonly string[] TideMageEvents =
    {
        "TideMageZone1",
        "TideMageZone2",
        "TideMageZone3",
        "ZeroStability",
        "WorldStable",
        "NoEffect",
    };

    public static string SupplyProperty(string camp) => $"olthoi_north_camp_{camp.ToLowerInvariant()}_supply_level";

    public static string WellSuppliedEvent(string camp) => $"OlthoiNorthCamp{camp}WellSupplied";

    public static string GreatlySuppliedEvent(string camp) => $"OlthoiNorthCamp{camp}GreatlySupplied";

    public static AdminUiOlthoiNorth GetOlthoiNorth()
    {
        var camps = OlthoiNorthCamps
            .Select(camp => new AdminUiCamp(
                camp,
                PropertyManager.GetLong(SupplyProperty(camp)).Item,
                GetEvent(WellSuppliedEvent(camp)),
                GetEvent(GreatlySuppliedEvent(camp))
            ))
            .ToList();

        var players = PlayersWhere(IsInOlthoiNorth)
            .Select(p => new AdminUiOlthoiPlayer(AdminUiSnapshots.GetFellow(p), p.GetOlthoiNorthSpellStacks()))
            .ToList();

        return new AdminUiOlthoiNorth(camps, players);
    }

    public static bool IsInOlthoiNorth(Player player) =>
        player.InstanceId == Landblock.PersistentInstance && IsInOlthoiNorth(player.Location.LandblockId);

    public static bool IsInOlthoiNorth(LandblockId landblock) =>
        landblock.LandblockX >= OlthoiNorthMinX
        && landblock.LandblockX <= OlthoiNorthMaxX
        && landblock.LandblockY >= OlthoiNorthMinY
        && landblock.LandblockY <= OlthoiNorthMaxY;

    public static AdminUiTideMage GetTideMage()
    {
        var stability = PropertyManager.GetLong("fragment_stability_phase_one").Item;

        return new AdminUiTideMage(
            stability,
            StabilityBand(stability),
            LandblockManager.IsLoaded(MageHouse),
            TideMageEvents.Select(GetEvent).ToList()
        );
    }

    /// <summary>
    /// The World Stability band the controllers (2023174 and 2023147-2023150) put this value in
    /// </summary>
    public static string StabilityBand(long stability)
    {
        return stability switch
        {
            <= 0 => "Zero",
            < 5000 => "Low",
            < 10000 => "Mid",
            < 15000 => "High",
            _ => "100%",
        };
    }

    public static AdminUiEsper GetEsper()
    {
        return new AdminUiEsper(
            PlayersWhere(p => p.InstanceId == Landblock.PersistentInstance && EsperMountainsZone.Contains(p.Location))
                .Select(AdminUiSnapshots.GetFellow)
                .ToList()
        );
    }

    private static IEnumerable<Player> PlayersWhere(System.Func<Player, bool> predicate)
    {
        return PlayerManager.GetAllOnline().Where(p => p.Location != null && predicate(p)).OrderBy(p => p.Name);
    }

    private static AdminUiEvent GetEvent(string name)
    {
        return new AdminUiEvent(name, EventManager.GetEventStatus(name).ToString());
    }
}
