using System.Collections.Generic;
using System.Linq;

namespace ACE.Server.Lfg;

/// <summary>
/// The capstone dungeons /lfgroup and /lfmore can be asked for by name. The levels are the entrance portals' MinLevel in world-db
/// (regional/Dungeons/Capstone Dungeons), the same as the "Level 30 Capstone" notes in the map's locations.xml. A test checks
/// every dungeon in CapstoneRunTracker.DungeonKeys has a row.
/// </summary>
public static class LfgCapstones
{
    public static readonly IReadOnlyList<LfgCapstone> All = new[]
    {
        new LfgCapstone("Glenden Wood Dungeon", 10, "gwd", "glenden", "glenden wood"),
        new LfgCapstone("Green Mire Grave", 10, "gmg", "green mire", "mire grave"),
        new LfgCapstone("Sand Shallow", 10, "sand", "sand shallows", "ss"),
        new LfgCapstone("Halls of the Helm", 20, "hoth", "halls", "helm", "halls of helm"),
        new LfgCapstone("Manse of Panderlou", 20, "manse", "panderlou", "mop"),
        new LfgCapstone("Smugglers Hideaway", 20, "smugglers", "hideaway", "sh"),
        new LfgCapstone("Colier Mine", 30, "colier", "mines of colier", "colier mines", "moc"),
        new LfgCapstone("Empyrean Garrison", 30, "eg", "empyrean", "garrison"),
        new LfgCapstone("Grievous Vault", 30, "gv", "grievous", "vault"),
        new LfgCapstone("Folthid Cellar", 40, "folthid", "cellar", "fc"),
        new LfgCapstone("Mines of Despair", 40, "mod", "despair"),
        new LfgCapstone("Gredaline Consulate", 40, "gredaline", "consulate", "gc"),
        new LfgCapstone("Mage Academy", 50, "academy", "ma"),
        new LfgCapstone("Lugian Mines", 50, "lugian", "lm"),
        new LfgCapstone("Mountain Fortress", 50, "fortress", "mf"),
    };

    /// <summary>
    /// The lowest level any capstone lets in from
    /// </summary>
    public static int LowestLevel => All.Min(c => c.Level);

    /// <summary>
    /// The capstone this names (its whole name or a short one, with case, spaces and apostrophes ignored), or null
    /// </summary>
    public static LfgCapstone Find(string text)
    {
        var wanted = LfgParser.Normalize(text);

        if (wanted.Length == 0)
        {
            return null;
        }

        return All.FirstOrDefault(c =>
            LfgParser.Normalize(c.Name) == wanted || c.Aliases.Any(alias => LfgParser.Normalize(alias) == wanted)
        );
    }
}
