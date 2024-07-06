using System.Collections.Generic;
using System.IO;

namespace ACE.DatLoader.FileTypes;

[DatFileType(DatFileType.QualityFilter)]
public class QualityFilter : FileType
{
    public List<uint> IntStatFilter { get; private set; } = new List<uint>();
    public List<uint> Int64StatFilter { get; private set; } = new List<uint>();
    public List<uint> BoolStatFilter { get; private set; } = new List<uint>();
    public List<uint> FloatStatFilter { get; private set; } = new List<uint>();
    public List<uint> DidStatFilter { get; private set; } = new List<uint>();
    public List<uint> IidStatFilter { get; private set; } = new List<uint>();
    public List<uint> StringStatFilter { get; private set; } = new List<uint>();
    public List<uint> PositionStatFilter { get; private set; } = new List<uint>();

    public List<uint> AttributeStatFilter { get; private set; } = new List<uint>();
    public List<uint> Attribute2ndStatFilter { get; private set; } = new List<uint>();
    public List<uint> SkillStatFilter { get; private set; } = new List<uint>();

    public override void Unpack(BinaryReader reader)
    {
        Id = reader.ReadUInt32();
        var numInt = reader.ReadUInt32();
        var numInt64 = reader.ReadUInt32();
        var numBool = reader.ReadUInt32();
        var numFloat = reader.ReadUInt32();
        var numDid = reader.ReadUInt32();
        var numIid = reader.ReadUInt32();
        var numString = reader.ReadUInt32();
        var numPosition = reader.ReadUInt32();

        for (var i = 0; i < numInt; i++)
        {
            IntStatFilter.Add(reader.ReadUInt32());
        }

        for (var i = 0; i < numInt64; i++)
        {
            Int64StatFilter.Add(reader.ReadUInt32());
        }

        for (var i = 0; i < numBool; i++)
        {
            BoolStatFilter.Add(reader.ReadUInt32());
        }

        for (var i = 0; i < numFloat; i++)
        {
            FloatStatFilter.Add(reader.ReadUInt32());
        }

        for (var i = 0; i < numDid; i++)
        {
            DidStatFilter.Add(reader.ReadUInt32());
        }

        for (var i = 0; i < numIid; i++)
        {
            IidStatFilter.Add(reader.ReadUInt32());
        }

        for (var i = 0; i < numString; i++)
        {
            StringStatFilter.Add(reader.ReadUInt32());
        }

        for (var i = 0; i < numPosition; i++)
        {
            PositionStatFilter.Add(reader.ReadUInt32());
        }

        var numAttribute = reader.ReadUInt32();
        var numAttribute2nd = reader.ReadUInt32();
        var numSkill = reader.ReadUInt32();

        for (var i = 0; i < numAttribute; i++)
        {
            AttributeStatFilter.Add(reader.ReadUInt32());
        }

        for (var i = 0; i < numAttribute2nd; i++)
        {
            Attribute2ndStatFilter.Add(reader.ReadUInt32());
        }

        for (var i = 0; i < numSkill; i++)
        {
            SkillStatFilter.Add(reader.ReadUInt32());
        }
    }
}
