using System;
using System.Collections.Generic;

namespace ACE.Server.Lfg;

/// <summary>
/// A player looking for a group (/lfgroup)
/// </summary>
public sealed class LfgSeeker
{
    public uint Guid { get; init; }

    public string Name { get; init; }

    public LfgActivity Activity { get; init; }

    public DateTime Since { get; init; }

    /// <summary>
    /// The manual lfm whose leader is being asked about them, if any: they aren't put to another meanwhile
    /// </summary>
    public LfgListing AskedBy { get; set; }

    /// <summary>
    /// The lfms whose leaders said no to them, which aren't asked again
    /// </summary>
    public HashSet<LfgListing> Declined { get; } = new();
}
