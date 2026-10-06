using System;
using System.Collections.Generic;
using System.Linq;

namespace ACE.Server.Arena;

/// <summary>
/// Someone waiting in the arena queue
/// </summary>
public sealed class ArenaQueueEntry
{
    public uint Guid { get; init; }

    public string Name { get; init; }

    public int Level { get; init; }

    /// <summary>
    /// How many levels apart this player and their opponent may be at most. 0 is any level.
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
    /// The IP address the player is connected from, as text, or null if it is not known
    /// </summary>
    public string Address { get; init; }

    /// <summary>
    /// When they joined. Someone who is put back in the queue keeps this, so they don't lose their place.
    /// </summary>
    public DateTime JoinedAt { get; init; }
}

/// <summary>
/// The arena queue: players waiting for an opponent, paired in the order they came.<para />
/// Nothing in here is thread safe: ArenaManager only uses it while it holds its lock.
/// </summary>
public sealed class ArenaQueue
{
    // in the order of JoinedAt, oldest first
    private readonly List<ArenaQueueEntry> entries = new List<ArenaQueueEntry>();

    public int Count => entries.Count;

    public IReadOnlyList<ArenaQueueEntry> Entries => entries;

    public bool Contains(uint guid) => entries.Any(e => e.Guid == guid);

    /// <summary>
    /// Where a player is in the queue: 1 is next. 0 if they are not in it.
    /// </summary>
    public int PositionOf(uint guid) => entries.FindIndex(e => e.Guid == guid) + 1;

    /// <summary>
    /// Puts a player in the queue at the place their JoinedAt gives them. Someone who is in it already is replaced.
    /// </summary>
    public void Add(ArenaQueueEntry entry)
    {
        Remove(entry.Guid);

        var index = entries.FindIndex(e => e.JoinedAt > entry.JoinedAt);
        entries.Insert(index < 0 ? entries.Count : index, entry);
    }

    public bool Remove(uint guid) => entries.RemoveAll(e => e.Guid == guid) > 0;

    /// <summary>
    /// Takes the next two players to be matched out of the queue: the one who has waited longest, with whoever has waited longest of
    /// the players they can meet. If the one who has waited longest can meet nobody, the next one is tried, and so on.
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
    /// Whether two players in the queue can be matched: they asked for the same kind of duel (scaled or not, rated or not),
    /// each of them is within the level band the other asked for, and they are not connected from the same address if that is not allowed
    /// </summary>
    public static bool CanMeet(ArenaQueueEntry a, ArenaQueueEntry b, bool blockSameAddress)
    {
        if (a.Guid == b.Guid || a.Scaled != b.Scaled || a.Rated != b.Rated)
        {
            return false;
        }

        if (blockSameAddress && a.Address != null && a.Address == b.Address)
        {
            return false;
        }

        var gap = Math.Abs(a.Level - b.Level);

        return (a.LevelBand <= 0 || gap <= a.LevelBand) && (b.LevelBand <= 0 || gap <= b.LevelBand);
    }
}
