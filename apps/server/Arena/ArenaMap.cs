using System;
using System.Collections.Generic;
using System.Linq;
using ACE.Entity;
using ACE.Server.Entity;

namespace ACE.Server.Arena;

/// <summary>
/// A place duels are fought in. Every duel gets an instance of its own of the map's landblocks, so any number of duels
/// can use the same map at once, and nobody else is ever in there with them.<para />
/// An indoor map (a dungeon) needs nothing more than its landblock and where the fighters start: its walls keep them in.
/// An outdoor map has a ring of buffer landblocks around it, so the instance's edge is somewhere nobody can get to,
/// and a radius around a center that fighters are disqualified for staying out of.
/// </summary>
public sealed class ArenaMap
{
    /// <summary>
    /// The names of the instance templates of arena maps start with this, so that they are told apart in /instance list
    /// and can't be taken by an island of instances.json
    /// </summary>
    public const string TemplatePrefix = "arena:";

    public string Name { get; }

    /// <summary>
    /// What players are told the map is called
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Only enabled maps are picked for duels. A map that is not is still set up, so an admin can look at it with /instance open.
    /// </summary>
    public bool Enabled { get; }

    public InstanceTemplate Template { get; }

    /// <summary>
    /// Where fighters start. Every fighter in a duel gets a different one of these, picked at random.
    /// </summary>
    public IReadOnlyList<Position> Starts { get; }

    /// <summary>
    /// The middle of the fighting area, for a map that has a radius
    /// </summary>
    public Position Center { get; }

    /// <summary>
    /// How far from the center fighters may go, in meters. 0 for a map that has no radius (an indoor one, whose walls are its edge).
    /// </summary>
    public float Radius { get; }

    public bool HasRadius => Radius > 0 && Center != null;

    /// <param name="landblocks">The landblocks of the map, not counting the ring</param>
    /// <param name="bufferRing">How many landblocks around them are loaded as a margin that players are turned back from. 0 for a dungeon.</param>
    public ArenaMap(
        string name,
        string description,
        bool enabled,
        IEnumerable<LandblockId> landblocks,
        int bufferRing,
        IReadOnlyList<Position> starts,
        Position center = null,
        float radius = 0
    )
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("An arena map needs a name", nameof(name));
        }

        if (starts == null || starts.Count < 2)
        {
            throw new ArgumentException("An arena map needs at least two places for fighters to start", nameof(starts));
        }

        if (radius > 0 && center == null)
        {
            throw new ArgumentException("An arena map that has a radius needs a center", nameof(center));
        }

        var interior = landblocks.ToList();
        var ring = InstanceTemplate.Ring(interior, bufferRing);

        Name = name;
        Description = string.IsNullOrWhiteSpace(description) ? name : description;
        Enabled = enabled;
        Starts = starts;
        Center = radius > 0 ? center : null;
        Radius = Math.Max(0, radius);

        Template = new InstanceTemplate(
            TemplatePrefix + name,
            interior.Concat(ring),
            starts[0],
            returnPosition: null,
            instanceOnly: false,
            boundary: ring
        );
    }

    /// <summary>
    /// How far a fighter is outside the map's radius, in meters. 0 if they are inside it, or the map has no radius.
    /// </summary>
    public float DistanceOutside(Position position)
    {
        if (!HasRadius || position == null)
        {
            return 0;
        }

        return Math.Max(0, Center.Distance2D(position) - Radius);
    }

    public override string ToString()
    {
        return $"{Name} ({Description})";
    }
}
