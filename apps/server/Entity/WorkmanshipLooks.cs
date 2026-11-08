using System;
using System.Globalization;
using ACE.Database;
using ACE.Entity.Enum.Properties;
using ACE.Entity.Models;
using ACE.Server.WorldObjects;

namespace ACE.Server.Entity;

/// <summary>
/// Wear levels: an item whose weenie lists WorkmanshipClothingBases draws the one for its workmanship in place of its
/// ClothingBase, while its ClothingBase is one of the list's (the plain look). A list of five is by pairs (1-2, 3-4, 5-6,
/// 7-8, 9-10); a list of ten has one entry per workmanship, so each set can step through its looks its own way. An entry
/// may name alternatives separated by '|' (two battered looks with different marks); each item always draws the same one,
/// picked from its guid. Chosen each time the appearance is built, so items already in the world show their level, and a
/// /style look with a ClothingBase the list doesn't name shows as it is.
/// </summary>
public static class WorkmanshipLooks
{
    public const int Levels = 5;
    public const int PerWorkmanship = 10;
    private const int Plain = Levels / 2;

    /// <summary>
    /// The entries of a WorkmanshipClothingBases value, comma-separated, each one ClothingBase id or several separated by
    /// '|', hex (0x...) or decimal; null unless there are exactly five or ten entries and every id is valid.
    /// </summary>
    public static uint[][] Parse(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var parts = text.Split(',');
        if (parts.Length != Levels && parts.Length != PerWorkmanship)
        {
            return null;
        }

        var entries = new uint[parts.Length][];
        for (var i = 0; i < parts.Length; i++)
        {
            var alternatives = parts[i].Split('|');
            entries[i] = new uint[alternatives.Length];
            for (var j = 0; j < alternatives.Length; j++)
            {
                var part = alternatives[j].Trim();
                var ok = part.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                    ? uint.TryParse(part[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out entries[i][j])
                    : uint.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out entries[i][j]);
                if (!ok || entries[i][j] == 0)
                {
                    return null;
                }
            }
        }

        return entries;
    }

    /// <summary>
    /// The wear level (0 battered .. 4 pristine) for a workmanship; plain when the item has none.
    /// </summary>
    public static int Level(int? workmanship)
    {
        if (workmanship is not > 0)
        {
            return Plain;
        }

        return Math.Clamp((workmanship.Value - 1) / 2, 0, Levels - 1);
    }

    /// <summary>
    /// The entry of a list of the given length for a workmanship: by pairs for five, one each for ten.
    /// </summary>
    public static int Index(int? workmanship, int count)
    {
        if (count != PerWorkmanship)
        {
            return Level(workmanship);
        }

        return workmanship is > 0 ? Math.Clamp(workmanship.Value - 1, 0, PerWorkmanship - 1) : 5;
    }

    /// <summary>
    /// The ClothingBase to draw: the list's entry for the workmanship when clothingBase is in the list, otherwise
    /// clothingBase as it is. An item without workmanship keeps its own. Of an entry's alternatives, seed (the item's
    /// guid) picks one, so the same item always draws the same marks.
    /// </summary>
    public static uint? Resolve(uint? clothingBase, string clothingBases, int? workmanship, uint seed = 0)
    {
        var entries = clothingBase == null ? null : Parse(clothingBases);
        if (entries == null || workmanship is not > 0 || Array.FindIndex(entries, e => Array.IndexOf(e, clothingBase.Value) >= 0) < 0)
        {
            return clothingBase;
        }

        var entry = entries[Index(workmanship, entries.Length)];
        // Mixed, as guids made together are consecutive: a set dropped at once would otherwise alternate piece by piece.
        return entry[(int)(((seed * 2654435761u) >> 16) % (uint)entry.Length)];
    }

    /// <summary>
    /// The ClothingBase the item draws (see Resolve), reading the list from its weenie, so items made before the list
    /// was added show their level too; the item's own value counts when the weenie has none.
    /// </summary>
    public static uint? DrawnClothingBase(WorldObject item)
    {
        var clothingBase = item.ClothingBase;
        if (clothingBase == null)
        {
            return null;
        }

        var weenie = item.Weenie ?? DatabaseManager.World?.GetCachedWeenie(item.WeenieClassId);
        var fromWeenie = weenie?.GetProperty(PropertyString.WorkmanshipClothingBases);
        var list = string.IsNullOrWhiteSpace(fromWeenie)
            ? item.GetProperty(PropertyString.WorkmanshipClothingBases)
            : fromWeenie;

        return Resolve(clothingBase, list, item.ItemWorkmanship, item.Guid.Full);
    }
}
