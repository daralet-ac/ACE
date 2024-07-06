using System.IO;

namespace ACE.DatLoader.Entity;

public class Attribute2ndBase : IUnpackable
{
    public SkillFormula Formula { get; private set; } = new SkillFormula();

    public void Unpack(BinaryReader reader)
    {
        Formula.Unpack(reader);
    }
}
