using System;
using System.Collections.Generic;
using System.Linq;
using ACE.Entity;

namespace ACE.Server.Entity;

/// <summary>
/// Describes something that can be instanced: which landblocks it is made of, where players arrive, and where they go when it ends.
/// </summary>
public class InstanceTemplate
{
    public string Name { get; }

    /// <summary>
    /// The landblocks an instance is made of. An instance never contains any other landblock,
    /// whatever is next to these in the world: nothing loads a landblock that isn't in here, and nothing can walk into one.
    /// </summary>
    public IReadOnlyCollection<LandblockId> Footprint { get; }

    /// <summary>
    /// Where players arrive when they enter an instance made from this. This is a position in the instance, not in the persistent world.
    /// </summary>
    public Position EntryPosition { get; }

    /// <summary>
    /// Where players are sent when they leave, when the instance ends, and when they log out or log in again after a crash.
    /// If this is null they go to their sanctuary (the lifestone they are bound to).
    /// </summary>
    public Position ReturnPosition { get; }

    /// <summary>
    /// True if the landblocks only exist as instances: there is nothing at these coordinates in the persistent world.
    /// Anything that ends up there is a mistake, so a player who is saved there is moved out when they log in,
    /// and the persistent world refuses to load these landblocks.
    /// </summary>
    public bool InstanceOnly { get; }

    public InstanceTemplate(
        string name,
        IEnumerable<LandblockId> footprint,
        Position entryPosition,
        Position returnPosition = null,
        bool instanceOnly = false
    )
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("An instance template needs a name", nameof(name));
        }

        // a set, so a landblock listed twice is only there once. LandblockId compares X/Y only, whatever the cell bits are.
        var landblocks = new HashSet<LandblockId>(footprint ?? throw new ArgumentNullException(nameof(footprint)));

        if (landblocks.Count == 0)
        {
            throw new ArgumentException("An instance template needs at least one landblock", nameof(footprint));
        }

        Name = name;
        Footprint = landblocks.ToList().AsReadOnly();
        EntryPosition = entryPosition ?? throw new ArgumentNullException(nameof(entryPosition));
        ReturnPosition = returnPosition;
        InstanceOnly = instanceOnly;
    }

    public bool Contains(LandblockId landblockId)
    {
        return Footprint.Contains(landblockId);
    }

    public override string ToString()
    {
        return $"{Name} ({Footprint.Count} landblock{(Footprint.Count == 1 ? "" : "s")})";
    }
}
