using System;

namespace ACE.Database.Models.Shard;

/// <summary>
/// One item changing hands between characters (item_transfer_log): given, traded, dropped on the ground, picked up off the
/// ground or a player's corpse by someone else. The Daralet Admin plugin's Items tab reads it as an item's history. From is
/// null for nobody (never, today) and To is null when it went to the ground.
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
    public DateTime CreatedAtUtc { get; set; }
}
