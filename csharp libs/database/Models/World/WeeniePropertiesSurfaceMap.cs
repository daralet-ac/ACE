namespace ACE.Database.Models.World;

/// <summary>
/// Surface map overrides for Weenies.
/// Allows mapping one Surface (0x08) DID to another at the weenie level.
/// </summary>
public partial class WeeniePropertiesSurfaceMap
{
    /// <summary>
    /// Unique Id of this Property.
    /// </summary>
    public uint Id { get; set; }

    /// <summary>
    /// Id of the object this property belongs to (WCID).
    /// </summary>
    public uint ObjectId { get; set; }

    /// <summary>
    /// Part index in the model / setup this mapping applies to.
    /// Mirrors the semantics of WeeniePropertiesTextureMap.Index.
    /// </summary>
    public byte Index { get; set; }

    /// <summary>
    /// Original Surface (0x08) DID.
    /// </summary>
    public uint OldSurfaceId { get; set; }

    /// <summary>
    /// Replacement Surface (0x08) DID.
    /// </summary>
    public uint NewSurfaceId { get; set; }

    public virtual Weenie Object { get; set; }
}
