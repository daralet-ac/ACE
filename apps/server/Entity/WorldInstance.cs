using System;
using System.Collections.Generic;

namespace ACE.Server.Entity;

/// <summary>
/// One live copy of an InstanceTemplate. Everything in it exists for as long as it does, and nothing in it is ever saved.<para />
/// The state in here is protected by the InstanceManager, which is the only thing that changes it.
/// </summary>
public class WorldInstance
{
    /// <summary>
    /// The id of the instance. 0 is the persistent world, so this is never 0, and it is never used again while the server runs.
    /// </summary>
    public uint Id { get; }

    public InstanceTemplate Template { get; }

    public DateTime CreatedAt { get; }

    /// <summary>
    /// Whatever the instance belongs to, if anything (the fellowship a dungeon was opened for, for example). Not used by the instance itself.
    /// </summary>
    public object Owner { get; }

    /// <summary>
    /// When the last player left, or when the instance was made if nobody has been in it yet. Null while there are players in it.
    /// </summary>
    internal DateTime? EmptySince { get; set; }

    /// <summary>
    /// True once the instance has been told to shut down. Nobody can be sent into it any more, and it is deleted as soon as it is empty.
    /// </summary>
    public bool IsClosing { get; internal set; }

    internal HashSet<uint> MemberGuids { get; } = new HashSet<uint>();

    public int MemberCount => MemberGuids.Count;

    internal WorldInstance(uint id, InstanceTemplate template, object owner, DateTime createdAt)
    {
        Id = id;
        Template = template;
        Owner = owner;
        CreatedAt = createdAt;
        EmptySince = createdAt;
    }

    public override string ToString()
    {
        return $"instance {Id}: {Template}";
    }
}
