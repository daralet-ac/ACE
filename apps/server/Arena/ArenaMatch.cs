using System;
using System.Collections.Generic;
using System.Linq;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Server.Entity;
using ACE.Server.WorldObjects;

namespace ACE.Server.Arena;

public enum ArenaMatchState
{
    /// <summary>Waiting for everyone to say yes</summary>
    Accepting,

    /// <summary>Everyone said yes. The arena's instance is made on the next tick.</summary>
    Starting,

    /// <summary>The instance is being made</summary>
    Opening,

    /// <summary>The fighters are on their way into the instance</summary>
    Arriving,

    /// <summary>Everyone is there. Nobody can be harmed until the countdown ends.</summary>
    Countdown,

    Fighting,

    /// <summary>Over, or called off. The fighters are sent home, and the instance is closed a little later.</summary>
    Ended
}

public enum ArenaMatchKind
{
    /// <summary>One player challenged another by name</summary>
    Challenge,

    /// <summary>The queue paired two players</summary>
    Queue
}

/// <summary>
/// One fighter in a duel, and everything that has to be put back the way it was when they leave
/// </summary>
public sealed class ArenaFighter
{
    public uint Guid { get; init; }

    public string Name { get; init; }

    public int Level { get; init; }

    public int Side { get; init; }

    public string Address { get; init; }

    /// <summary>
    /// For a fighter the queue paired: their place and level band in the queue, so they can be put back if the other one says no
    /// </summary>
    public ArenaQueueEntry QueueEntry { get; init; }

    public bool Accepted { get; set; }

    public Confirmation_Arena Confirmation { get; set; }

    /// <summary>
    /// Where they were before the duel, and where they are sent back to. Set before they are sent into the instance, and never changed after.
    /// </summary>
    public Position Home { get; set; }

    public PlayerKillerStatus OriginalStatus { get; set; }

    public double OriginalLastPkAttack { get; set; }

    public Position Start { get; set; }

    public bool Arrived { get; set; }

    /// <summary>
    /// Out of the fight: defeated, disqualified, or gone
    /// </summary>
    public bool Eliminated { get; set; }

    /// <summary>
    /// They have been sent home, or don't need to be (they left by themselves, or logged out), and their status has been put back
    /// </summary>
    public bool Returned { get; set; }

    /// <summary>
    /// When they went outside the arena's radius, while they are outside it
    /// </summary>
    public DateTime? OutsideSince { get; set; }

    public int NextBoundaryWarning { get; set; }
}

/// <summary>
/// A duel, from the question to the fighters until they are all home again. It owns its instance: the instance's Owner is this.<para />
/// Its state is only changed by the ArenaManager, while it holds its lock.
/// </summary>
public sealed class ArenaMatch : IInstanceReturnPositions
{
    public int Id { get; init; }

    public ArenaMatchKind Kind { get; init; }

    /// <summary>
    /// Whether the result changes the fighters' arena ratings
    /// </summary>
    public bool Rated { get; init; }

    public List<ArenaFighter> Fighters { get; init; }

    public ArenaMatchState State { get; set; }

    /// <summary>
    /// When the current state runs out: the time to answer, to arrive, the end of the countdown, or the time limit of the fight
    /// </summary>
    public DateTime Deadline { get; set; }

    public ArenaMap Map { get; set; }

    public WorldInstance Instance { get; set; }

    /// <summary>
    /// The side that won, once the duel is over. Null for a draw, or a duel that was called off.
    /// </summary>
    public int? WinningSide { get; set; }

    public bool CalledOff { get; set; }

    public DateTime EndedAt { get; set; }

    /// <summary>
    /// The last number of seconds left that the countdown told the fighters
    /// </summary>
    public int LastAnnounced { get; set; }

    public ArenaFighter Get(uint guid) => Fighters.FirstOrDefault(f => f.Guid == guid);

    public IEnumerable<ArenaFighter> Opponents(ArenaFighter fighter) => Fighters.Where(f => f.Side != fighter.Side);

    public bool InInstance(Player player) => Instance != null && player.InstanceId == Instance.Id;

    /// <summary>
    /// Where a fighter goes when they leave the instance any other way than being sent home by the duel (logging out, /instance close):
    /// back to where they were before it
    /// </summary>
    public Position GetReturnPosition(Player player) => Get(player.Guid.Full)?.Home;

    public string Describe()
    {
        var names = string.Join(
            " vs ",
            Fighters.GroupBy(f => f.Side).Select(side => string.Join(", ", side.Select(f => f.Name)))
        );
        return $"#{Id} {names} ({Kind}{(Rated ? ", rated" : "")}) - {State}{(Map != null ? $" in {Map.Name}" : "")}{(Instance != null ? $", instance {Instance.Id}" : "")}";
    }
}
