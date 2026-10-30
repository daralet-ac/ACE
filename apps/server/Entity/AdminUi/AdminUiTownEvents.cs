using System.Collections.Generic;

namespace ACE.Server.Entity.AdminUi;

/// <summary>
/// A town's unrest (TownEventManager): how close it is to an attack
/// </summary>
/// <param name="Tier">The town's tier, from 0 (Holtburg)</param>
/// <param name="CooldownSeconds">How long until the town can be attacked again, 0 if it can be now</param>
/// <param name="TriggerChance">The chance a minute of an attack at this unrest (0 to 1), when nothing else stops one</param>
/// <param name="HasEligiblePlayer">Someone at the town's level, or Shrouded, is in its region: without one no attack starts</param>
/// <param name="Threshold">The unrest an attack takes, already raised by the supply level</param>
/// <param name="SupplyLevel">The guard supply level, 0 to 10 (TownSupply)</param>
/// <param name="SupplyLevelFloor">The supply the current level starts at (0 at level 0)</param>
/// <param name="SupplyNextLevel">The supply the next level starts at (the current level's at level 10)</param>
/// <param name="SupplyLossPerHour">How much supply drains an hour at the current level</param>
/// <param name="SupplyLevelSecondsLeft">How long the current level lasts at that drain, minimum hold included (0 at level 0)</param>
/// <param name="SupplyHoldSecondsLeft">What's left of a newly reached level's minimum hold</param>
public record AdminUiTownUnrest(
    string Town,
    uint Tier,
    double Unrest,
    double Threshold,
    int CooldownSeconds,
    bool UnderAttack,
    double TriggerChance,
    bool HasEligiblePlayer,
    int SupplyLevel,
    double Supply,
    double SupplyLevelFloor,
    double SupplyNextLevel,
    double SupplyLossPerHour,
    int SupplyLevelSecondsLeft,
    int SupplyHoldSecondsLeft
);

/// <summary>
/// One player's part in an attack, the over-level kill log included
/// </summary>
/// <param name="Member">In the Town Defense fellowship right now</param>
public record AdminUiTownDefender(
    string Name,
    double Damage,
    double Healing,
    double DamageTaken,
    int KillingBlows,
    bool Member,
    bool CreditLost,
    int OverLevelKills,
    int PromptsRefused,
    int KillsAfterRefusal
);

/// <summary>
/// An attack going on
/// </summary>
/// <param name="Phase">Preparing, Warning, Wave or Break</param>
/// <param name="PhaseSecondsLeft">Until the warning or break runs out (0 in the other phases)</param>
public record AdminUiTownAttack(
    int Id,
    string Town,
    string Phase,
    int Wave,
    int WaveCount,
    string Family,
    int AttackersAlive,
    int Defenders,
    int SecondsIn,
    int PhaseSecondsLeft,
    List<AdminUiTownDefender> Scores
);

/// <summary>
/// The Town Events tab: whether attacks start on their own (town_events_enabled), every town's unrest, and the attacks going on
/// </summary>
/// <param name="BaseThreshold">town_events_base_threshold: each town's threshold is rolled between it and double it</param>
public record AdminUiTownEvents(bool Enabled, double BaseThreshold, List<AdminUiTownUnrest> Towns, List<AdminUiTownAttack> Attacks);
