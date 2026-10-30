using System;
using System.Collections.Generic;
using System.Linq;
using ACE.Entity.Enum;

namespace ACE.Server.TownEvents;

/// <summary>
/// A raid theme's creatures, as read from the world database (TownCreaturePool.GetRoster), at their own levels
/// </summary>
public sealed record TownRaidRoster(
    TownRaidTheme Theme,
    List<TownPoolCreature> Raiders,
    List<TownPoolCreature> Pets,
    List<TownPoolCreature> Leaders
);

public static partial class TownRaidThemes
{
    // creatures this many levels outside a town's level band come too when some are inside it, so a town gets more than one
    // or two of a ladder; when none are, those this much further out than the nearest
    public const int InBandSlack = 3;
    public const int LevelSlack = 6;

    /// <summary>
    /// The creature types that can lead a raid on a town without a theme: the humanoid races and the other thinking things.
    /// Beasts only come along as a second, smaller family.
    /// </summary>
    public static readonly HashSet<CreatureType> RaiderTypes = new(TownCreaturePool.HumanoidTypes)
    {
        CreatureType.Shadow,
        CreatureType.Virindi,
        CreatureType.Zefir,
    };

    /// <summary>
    /// The themes of a town's raids, or none for a town without any (its raids come from its own creature pool)
    /// </summary>
    public static IReadOnlyList<TownRaidTheme> ForTown(string town) =>
        Towns.TryGetValue(town, out var names)
            ? names.Select(n => Themes[n]).ToList()
            : Array.Empty<TownRaidTheme>();

    /// <summary>
    /// How far a level is outside a band: 0 inside it
    /// </summary>
    public static int LevelGap(int level, int minLevel, int maxLevel) =>
        level < minLevel ? minLevel - level : level > maxLevel ? level - maxLevel : 0;

    /// <summary>
    /// The creatures of a list nearest a town's level band: those in it and within InBandSlack levels of it, or when none are
    /// in it, the nearest and every other within LevelSlack levels of it
    /// </summary>
    public static List<TownPoolCreature> Nearest(IEnumerable<TownPoolCreature> creatures, int minLevel, int maxLevel)
    {
        var list = creatures.ToList();
        if (list.Count == 0)
        {
            return list;
        }

        var nearest = list.Min(c => LevelGap(c.Level, minLevel, maxLevel));
        var slack = nearest == 0 ? InBandSlack : LevelSlack;
        return list.Where(c => LevelGap(c.Level, minLevel, maxLevel) <= nearest + slack).ToList();
    }

    /// <summary>
    /// The level a raider of a theme fights at in a town: its own if that's in the town's band, otherwise a level in the band
    /// (the top of it, for a leader)
    /// </summary>
    public static int ScaleLevel(int level, int minLevel, int maxLevel, bool leader, Func<int, int, int> roll)
    {
        if (LevelGap(level, minLevel, maxLevel) == 0)
        {
            return level;
        }

        return leader ? maxLevel : roll(minLevel, maxLevel);
    }
}
