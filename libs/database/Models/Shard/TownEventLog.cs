using System;
using System.Collections.Generic;

namespace ACE.Database.Models.Shard;

/// <summary>
/// A town attack that ended (town_event): where and when, how it went, and its participants
/// </summary>
public class TownEventLog
{
    public uint Id { get; set; }

    /// <summary>
    /// The attack's own id, so a reward claimed from the Captain later can find its row
    /// </summary>
    public string Uid { get; set; }

    public string Town { get; set; }
    public byte Tier { get; set; }

    /// <summary>
    /// unrest or admin
    /// </summary>
    public string StartedBy { get; set; }

    /// <summary>
    /// held, fell, looted, stopped or cancelled
    /// </summary>
    public string Outcome { get; set; }

    public string Family { get; set; }
    public string MinorFamily { get; set; }

    /// <summary>
    /// The roads the attackers came in on, by compass direction
    /// </summary>
    public string Roads { get; set; }

    public bool ChestRaid { get; set; }
    public byte ChestLootedPercent { get; set; }
    public byte SupplyLevelStart { get; set; }
    public byte SupplyLevelEnd { get; set; }
    public byte WavesReached { get; set; }
    public byte WaveCount { get; set; }
    public ushort AttackersSpawned { get; set; }

    /// <summary>
    /// Players who earned credit, and of them those rewarded at the end
    /// </summary>
    public ushort Participants { get; set; }

    public ushort Rewarded { get; set; }

    /// <summary>
    /// What each defender got when the Warlord fell (0 if it didn't), and how many got it
    /// </summary>
    public uint WarlordPyreals { get; set; }

    public ushort WarlordPaid { get; set; }

    public DateTime StartedAtUtc { get; set; }
    public DateTime EndedAtUtc { get; set; }
    public uint DurationSeconds { get; set; }

    public List<TownEventLogParticipant> Participations { get; set; } = new();
}

/// <summary>
/// One player's part in a TownEventLog (town_event_participant)
/// </summary>
public class TownEventLogParticipant
{
    public uint EventId { get; set; }
    public uint CharacterId { get; set; }
    public string CharacterName { get; set; }
    public ushort Level { get; set; }

    /// <summary>
    /// In the Town Defense fellowship when the attack ended
    /// </summary>
    public bool MemberAtEnd { get; set; }

    public uint Damage { get; set; }
    public uint Healing { get; set; }
    public uint DamageTaken { get; set; }
    public ushort KillingBlows { get; set; }

    /// <summary>
    /// The player's place in each score among those who earned credit (1 is top), or null for none
    /// </summary>
    public ushort? RankDamage { get; set; }

    public ushort? RankHealing { get; set; }
    public ushort? RankDamageTaken { get; set; }
    public ushort? RankKillingBlows { get; set; }

    public byte CategoriesTopped { get; set; }
    public ushort OverLevelKills { get; set; }
    public ushort PromptsRefused { get; set; }
    public ushort KillsAfterRefusal { get; set; }
    public bool CreditLost { get; set; }

    /// <summary>
    /// paid, claim, claimed, expired or none
    /// </summary>
    public string Reward { get; set; }

    public double RewardMod { get; set; }

    /// <summary>
    /// The XP paid or claimed, before the player's own XP bonuses
    /// </summary>
    public ulong Xp { get; set; }

    public uint WarlordPyreals { get; set; }
    public DateTime? ClaimedAtUtc { get; set; }

    public TownEventLog Event { get; set; }
}
