using System;
using ACE.Server.Entity;

namespace ACE.Server.Lfg;

/// <summary>
/// A fellowship looking for more (/lfmore). It belongs to the fellowship, so it carries on if the leader changes.
/// </summary>
public sealed class LfgListing
{
    public Fellowship Fellowship { get; init; }

    public LfgActivity Activity { get; init; }

    /// <summary>
    /// Who posted it, whom its line in the LFG channel names: /lfgroup join finds it by that name too, after they've left
    /// </summary>
    public string PostedBy { get; init; }

    /// <summary>
    /// True: players who match join at once. False: the leader is asked about each.
    /// </summary>
    public bool AutoJoin { get; init; }

    public DateTime Since { get; init; }

    /// <summary>
    /// The player the leader is being asked about now, if any
    /// </summary>
    public LfgSeeker Asking { get; set; }
}
