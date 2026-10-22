using System.Collections.Generic;

namespace ACE.Server.Lfg;

/// <summary>
/// A capstone dungeon as /lfgroup and /lfmore know it: its name (as AssignCapstoneDungeon and CapstoneRunTracker.DungeonKeys name it), the level its
/// entrance portal lets in from, and the short names players call it by
/// </summary>
public sealed class LfgCapstone
{
    public LfgCapstone(string name, int level, params string[] aliases)
    {
        Name = name;
        Level = level;
        Aliases = aliases;
    }

    public string Name { get; }

    /// <summary>
    /// The entrance portal's MinLevel (and MaxLevel, which a Shrouded player is let past, and everyone in a capstone is Shrouded)
    /// </summary>
    public int Level { get; }

    /// <summary>
    /// The first is what the LFM line tells others to type
    /// </summary>
    public IReadOnlyList<string> Aliases { get; }

    public string Short => Aliases.Count > 0 ? Aliases[0] : LfgParser.Normalize(Name);
}
