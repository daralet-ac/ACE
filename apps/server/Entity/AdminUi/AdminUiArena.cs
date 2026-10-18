using System.Collections.Generic;

namespace ACE.Server.Entity.AdminUi;

/// <summary>
/// One fighter in a duel. Vitals is null while they're offline.
/// </summary>
public record AdminUiArenaFighter(uint Guid, string Name, int Level, string Status, AdminUiFellow Vitals);

/// <summary>
/// A duel going on. Sides are in side order (0, 1). FightSeconds is how long the fight itself has been going, and
/// SecondsLeft how long until whatever the duel is waiting for times out.
/// </summary>
public record AdminUiArenaMatch(
    int Id,
    string Kind,
    string State,
    string Map,
    bool Rated,
    bool Scaled,
    int SideSize,
    uint? InstanceId,
    double? FightSeconds,
    double? SecondsLeft,
    List<List<AdminUiArenaFighter>> Sides,
    List<string> Spectators
);

/// <summary>
/// A duel that has ended. WinningSide is null for a draw or a duel that was called off.
/// </summary>
public record AdminUiArenaResult(
    int Id,
    string Kind,
    string Map,
    bool Rated,
    bool Scaled,
    int SideSize,
    double SecondsAgo,
    double? FightSeconds,
    int? WinningSide,
    bool CalledOff,
    List<List<string>> Sides
);

public record AdminUiArenaReport(List<AdminUiArenaMatch> Matches, List<string> Queue, List<AdminUiArenaResult> Recent);
