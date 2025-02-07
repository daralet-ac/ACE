using System;

namespace ACE.Database.Models.World;

/// <summary>
/// Weenie Instances for each Landblock
/// </summary>
public class LandblockName
{
    public uint Id { get; set; }

    public uint ObjCellId { get; set; }

    public string Name { get; set; }

    public DateTime LastModified { get; set; }
}
