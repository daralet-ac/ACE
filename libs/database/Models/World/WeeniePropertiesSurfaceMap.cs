namespace ACE.Database.Models.World;

/// <summary>
/// Surface (0x08) map overrides for Weenies.
/// Backed by table: weenie_properties_surface_map
/// Columns: object_Id, index, old_Id, new_Id
/// </summary>
public partial class WeeniePropertiesSurfaceMap
{
    public uint Id { get; set; }

    /// <summary>
    /// Weenie class id (object_Id).
    /// </summary>
    public uint ObjectId { get; set; }

    /// <summary>
    /// Part index in the model / setup this mapping applies to,
    /// or 0 for object-global (index column).
    /// </summary>
    public byte Index { get; set; }

    /// <summary>
    /// Original Surface (0x08xxxxxx) DID (old_Id).
    /// </summary>
    public uint OldId { get; set; }

    /// <summary>
    /// Replacement Surface (0x08xxxxxx) DID (new_Id).
    /// </summary>
    public uint NewId { get; set; }

    public virtual Weenie Object { get; set; }
}
