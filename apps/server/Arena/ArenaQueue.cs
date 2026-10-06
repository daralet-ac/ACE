using System;
using System.Collections.Generic;
using System.Linq;

namespace ACE.Server.Arena;

/// <summary>
/// One of the players who fight together as one entry of the queue
/// </summary>
public sealed class ArenaQueueMember
{
    public uint Guid { get; init; }

    public string Name { get; init; }

    public int Level { get; init; }

    /// <summary>
    /// The IP address the player is connected from, as text, or null if it is not known
    /// </summary>
    public string Address { get; init; }
}

/// <summary>
/// Someone waiting in the arena queue: one player, or a fellowship that its leader put in it
/// </summary>
public sealed class ArenaQueueEntry
{
    /// <summary>
    /// Who put it in the queue: the player, or the fellowship's leader
    /// </summary>
    public uint Guid { get; init; }

    public string Name { get; init; }

    /// <summary>
    /// The level the level band is measured from: the player's, or the highest in the fellowship
    /// </summary>
    public int Level { get; init; }

    /// <summary>
    /// How many levels apart this entry and its opponent may be at most. 0 is any level.
    /// </summary>
    public int LevelBand { get; init; }

    /// <summary>
    /// A scaled duel: the higher-level fighter fights at their opponent's level (LevelScaling)
    /// </summary>
    public bool Scaled { get; init; }

    /// <summary>
    /// A rated duel changes ratings and records. Players who asked for an unrated duel are only paired with each other.
    /// </summary>
    public bool Rated { get; init; } = true;

    /// <summary>
    /// The IP address of whoever put it in the queue, as text, or null if it is not known
    /// </summary>
    public string Address { get; init; }

    /// <summary>
    /// When they joined. Someone who is put back in the queue keeps this, so they don't lose their place.
    /// </summary>
    public DateTime JoinedAt { get; init; }

    private readonly IReadOnlyList<ArenaQueueMember> members;

    /// <summary>
    /// Everyone who fights: the whole fellowship, or just the player (which is what it is when nothing else is said)
    /// </summary>
    public IReadOnlyList<ArenaQueueMember> Members
    {
        get =>
            members
            ?? new[]
            {
                new ArenaQueueMember
                {
                    Guid = Guid,
                    Name = Name,
                    Level = Level,
                    Address = Address
                }
            };
        init => members = value;
    }

    /// <summary>
    /// How many fight on its side: 1 for a player, more for a fellowship
    /// </summary>
    public int Size => Members.Count;

    public bool Includes(uint guid) => Members.Any(m => m.Guid == guid);

    /// <summary>
    /// "Bob", or "Bob's fellowship of 3"
    /// </summary>
    public string Describe() => Size == 1 ? Name : $"{Name}'s fellowship of {Size}";
}

/// <summary>
/// The arena queue: players and fellowships waiting for an opponent, paired in the order they came.<para />
/// Nothing in here is thread safe: ArenaManager only uses it while it holds its lock.
/// </summary>
public sealed class ArenaQueue
{
    // in the order of JoinedAt, oldest first
    private readonly List<ArenaQueueEntry> entries = new List<ArenaQueueEntry>();

    public int Count => entries.Count;

    public IReadOnlyList<ArenaQueueEntry> Entries => entries;

    /// <summary>
    /// Whether this player is waiting, on their own or with their fellowship
    /// </summary>
    public bool Contains(uint guid) => Find(guid) != null;

    /// <summary>
    /// The entry this player is waiting in, on their own or with their fellowship, if any
    /// </summary>
    public ArenaQueueEntry Find(uint guid) => entries.FirstOrDefault(e => e.Includes(guid));

    /// <summary>
    /// Where a player is in the queue: 1 is next. 0 if they are not in it.
    /// </summary>
    public int PositionOf(uint guid) => entries.FindIndex(e => e.Includes(guid)) + 1;

    /// <summary>
    /// Puts an entry in the queue at the place its JoinedAt gives it. Anyone in it who was waiting already is taken out of where they were.
    /// </summary>
    public void Add(ArenaQueueEntry entry)
    {
        entries.RemoveAll(e => e.Guid == entry.Guid || e.Members.Any(m => entry.Includes(m.Guid)));

        var index = entries.FindIndex(e => e.JoinedAt > entry.JoinedAt);
        entries.Insert(index < 0 ? entries.Count : index, entry);
    }

    /// <summary>
    /// Takes the entry this player is waiting in out of the queue (with everyone else in it), and returns it. Null if they were not waiting.
    /// </summary>
    public ArenaQueueEntry Take(uint guid)
    {
        var entry = Find(guid);

        if (entry != null)
        {
            entries.Remove(entry);
        }

        return entry;
    }

    public bool Remove(uint guid) => Take(guid) != null;

    /// <summary>
    /// Takes the next two entries to be matched out of the queue: the one that has waited longest, with whichever has waited longest of
    /// those it can meet. If the one that has waited longest can meet nobody, the next one is tried, and so on.
    /// </summary>
    public bool TryTakePair(bool blockSameAddress, out ArenaQueueEntry first, out ArenaQueueEntry second)
    {
        for (var i = 0; i < entries.Count; i++)
        {
            for (var j = i + 1; j < entries.Count; j++)
            {
                if (!CanMeet(entries[i], entries[j], blockSameAddress))
                {
                    continue;
                }

                first = entries[i];
                second = entries[j];

                entries.RemoveAt(j);
                entries.RemoveAt(i);
                return true;
            }
        }

        first = null;
        second = null;
        return false;
    }

    /// <summary>
    /// Whether two entries can be matched: they are the same size (one against one, or fellowships with as many in each), they asked for
    /// the same kind of duel (scaled or not, rated or not), each is within the level band the other asked for (measured from their highest
    /// levels), nobody is in both, and nobody on one side is connected from the same address as anybody on the other, if that is not allowed
    /// </summary>
    public static bool CanMeet(ArenaQueueEntry a, ArenaQueueEntry b, bool blockSameAddress)
    {
        if (a.Guid == b.Guid || a.Size != b.Size || a.Scaled != b.Scaled || a.Rated != b.Rated)
        {
            return false;
        }

        if (a.Members.Any(m => b.Includes(m.Guid)))
        {
            return false;
        }

        if (blockSameAddress && SharesAnAddress(a.Members.Select(m => m.Address), b.Members.Select(m => m.Address)))
        {
            return false;
        }

        var gap = Math.Abs(a.Level - b.Level);

        return (a.LevelBand <= 0 || gap <= a.LevelBand) && (b.LevelBand <= 0 || gap <= b.LevelBand);
    }

    /// <summary>
    /// Whether anybody on one side is connected from the same address as anybody on the other. An address that is not known is nobody's.
    /// </summary>
    public static bool SharesAnAddress(IEnumerable<string> one, IEnumerable<string> other)
    {
        var addresses = new HashSet<string>(one.Where(a => a != null));

        return other.Any(a => a != null && addresses.Contains(a));
    }
}
