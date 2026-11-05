using System;

namespace ACE.Database.Models.Shard;

/// <summary>
/// One item changing hands (item_transfer_log): given, traded, dropped on the ground, picked up off the ground or a player's
/// corpse by someone else, sold to or bought from a vendor, salvaged, or copied or restored by an admin. The Daralet Admin
/// plugin's Items tab reads it as an item's history. From is null when it came from nobody (a vendor, an admin's copy), and
/// To when it went to nobody (the ground, a vendor, salvage).
/// </summary>
public class ItemTransfer
{
    public ulong Id { get; set; }
    public string Kind { get; set; }
    public uint ItemId { get; set; }
    public uint ItemWcid { get; set; }
    public string ItemName { get; set; }
    public int Amount { get; set; }
    public uint? FromCharacterId { get; set; }
    public string FromName { get; set; }
    public uint? FromAccountId { get; set; }
    public uint? ToCharacterId { get; set; }
    public string ToName { get; set; }
    public uint? ToAccountId { get; set; }
    public string Details { get; set; }

    /// <summary>
    /// The whole item as it was (ItemSnapshots), for one that's gone: sold, salvaged or dropped. @itemrestore makes it
    /// again from this. Null for the rest, and once item_history_destroyed_days have passed.
    /// </summary>
    public string Snapshot { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
