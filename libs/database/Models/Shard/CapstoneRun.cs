using System;
using System.Collections.Generic;

namespace ACE.Database.Models.Shard;

/// <summary>
/// A capstone dungeon a fellowship completed, and how long it took (capstone_run). The website's capstone leaderboard.
/// </summary>
public class CapstoneRun
{
    public uint Id { get; set; }

    /// <summary>
    /// The dungeon's key on the website: its completion quest without "Completed" (GlendenWoodDungeon)
    /// </summary>
    public string Dungeon { get; set; }

    public byte FellowshipSize { get; set; }

    /// <summary>
    /// 0 for no Dungeon: Difficulty mod, 1-20 for I-XX
    /// </summary>
    public byte DifficultyTier { get; set; }

    public uint CompletionSeconds { get; set; }

    /// <summary>
    /// The members' character ids, smallest first, joined with commas. The same key is the same group of characters.
    /// </summary>
    public string RosterKey { get; set; }

    public DateTime CompletedAtUtc { get; set; }

    public List<CapstoneRunMember> Members { get; set; } = new();
}

/// <summary>
/// One character in the fellowship of a CapstoneRun (capstone_run_member)
/// </summary>
public class CapstoneRunMember
{
    public uint RunId { get; set; }
    public uint CharacterId { get; set; }
    public string CharacterName { get; set; }

    public CapstoneRun Run { get; set; }
}
