using System.Collections.Generic;
using System.IO;

namespace ACE.DatLoader.Entity;

public class SoundData : IUnpackable
{
    public List<SoundTableData> Data = new List<SoundTableData>();
    public uint Unknown;

    public void Unpack(BinaryReader reader)
    {
        Data.Unpack(reader);
        Unknown = reader.ReadUInt32();
    }
}
