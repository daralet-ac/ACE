namespace ACE.Database.Models.Shard;

/// <summary>
/// An object as the Daralet Admin plugin's item search sees it, read straight from the biota tables: what it is, and
/// what holds it. The search walks ContainerId / WielderId up to the character, bank or corpse at the top.
/// </summary>
public class ItemSearchRow
{
    public uint Id { get; set; }
    public uint Wcid { get; set; }
    public int WeenieType { get; set; }
    public string Name { get; set; }
    public int? StackSize { get; set; }
    public uint? ContainerId { get; set; }
    public uint? WielderId { get; set; }
    public uint? BankAccountId { get; set; }

    /// <summary>Whose corpse this is, for a corpse.</summary>
    public uint? VictimId { get; set; }
}
