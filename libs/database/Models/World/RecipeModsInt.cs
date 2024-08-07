#nullable disable

namespace ACE.Database.Models.World;

public partial class RecipeModsInt
{
    public uint Id { get; set; }
    public uint RecipeModId { get; set; }
    public sbyte Index { get; set; }
    public int Stat { get; set; }
    public int Value { get; set; }
    public int Enum { get; set; }
    public int Source { get; set; }

    public virtual RecipeMod RecipeMod { get; set; }
}
