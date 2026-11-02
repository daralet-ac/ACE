using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ACE.Database;
using ACE.Entity.Enum.Properties;
using ACE.Entity.Models;
using ACE.Server.WorldObjects;
using Serilog;

namespace ACE.Server.Entity;

/// <summary>
/// One look an item can show: a ClothingBase, a name players pick it by, and optionally a quest flag a player needs to
/// choose it.
/// </summary>
public sealed record ItemStyle(uint ClothingBase, string Name, string QuestFlag)
{
    public bool IsLocked => !string.IsNullOrEmpty(QuestFlag);

    /// <summary>
    /// True when a player with these quests can choose this style. hasQuest is asked only for locked styles.
    /// </summary>
    public bool IsUnlockedFor(Func<string, bool> hasQuest)
    {
        return !IsLocked || hasQuest?.Invoke(QuestFlag) == true;
    }
}

/// <summary>
/// Items can have several looks, and players switch between them with /style. An item's styles are, in order:
///   1. its default look, the ClothingBase it had before it was first switched;
///   2. its AlternateClothingBase, if it has one (the older, two-look way of giving an item a second look);
///   3. each entry of its ItemStyles string, written ClothingBase|Name|QuestFlag, one per line or separated by ';'.
/// A style with a QuestFlag can only be chosen by players who have that quest (ACCOUNT_ flags count for every character
/// on the account); the default and alternate looks never need one.
///
/// Switching only sets ClothingBase. The default look is kept in ItemStyleDefaultClothingBase while the item shows
/// another, so it can always be restored, and that property is cleared again when the item returns to its default.
/// Whoever holds an item can always switch it back to its default, so an item traded while showing a locked style is
/// never stuck in a look its new owner can't choose.
///
/// ItemStyles is read from the item's weenie rather than from the item, so adding or changing styles in content reaches
/// items already in the world. An item whose weenie has no ItemStyles uses its own, so a single item can be given
/// styles.
/// </summary>
public static class ItemStyles
{
    private static readonly ILogger _log = Log.ForContext(typeof(ItemStyles));

    public const string DefaultName = "Default";
    public const string AlternateName = "Alternate";

    private static readonly ConcurrentDictionary<string, IReadOnlyList<ItemStyle>> ParseCache = new();

    /// <summary>
    /// Parses an ItemStyles string. Entries that can't be read are skipped and logged.
    /// </summary>
    public static IReadOnlyList<ItemStyle> Parse(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        return ParseCache.GetOrAdd(text, ParseUncached);
    }

    private static IReadOnlyList<ItemStyle> ParseUncached(string text)
    {
        var styles = new List<ItemStyle>();

        foreach (var entry in text.Split(['\n', '\r', ';'], StringSplitOptions.RemoveEmptyEntries))
        {
            var fields = entry.Split('|').Select(f => f.Trim()).ToArray();

            if (fields[0].Length == 0)
            {
                continue;
            }

            if (!TryParseDataId(fields[0], out var clothingBase) || clothingBase == 0)
            {
                _log.Warning("ItemStyles entry {Entry} has no valid ClothingBase; skipping it", entry.Trim());
                continue;
            }

            var name = fields.Length > 1 && fields[1].Length > 0 ? fields[1] : null;
            var questFlag = fields.Length > 2 && fields[2].Length > 0 ? fields[2] : null;

            styles.Add(new ItemStyle(clothingBase, name, questFlag));
        }

        return styles;
    }

    private static bool TryParseDataId(string text, out uint value)
    {
        if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return uint.TryParse(text[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
        }

        return uint.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    /// <summary>
    /// The item's ItemStyles string: its weenie's, or its own when the weenie has none.
    /// </summary>
    private static string GetStylesText(WorldObject item)
    {
        var weenie = item.Weenie ?? DatabaseManager.World?.GetCachedWeenie(item.WeenieClassId);
        var fromWeenie = weenie?.GetProperty(PropertyString.ItemStyles);

        return string.IsNullOrWhiteSpace(fromWeenie) ? item.GetProperty(PropertyString.ItemStyles) : fromWeenie;
    }

    /// <summary>
    /// The look the item returns to when switched to its default.
    /// </summary>
    public static uint GetDefaultClothingBase(WorldObject item)
    {
        return item?.ItemStyleDefaultClothingBase ?? item?.ClothingBase ?? 0;
    }

    /// <summary>
    /// Every style of the item, locked or not, starting with its default. Styles that repeat a ClothingBase already
    /// listed are dropped, except that the first ItemStyles entry naming the default or alternate look names it. An
    /// item with no ClothingBase of its own has no styles, since there would be no default to return to.
    /// </summary>
    public static IReadOnlyList<ItemStyle> GetStyles(WorldObject item)
    {
        var defaultLook = GetDefaultClothingBase(item);

        if (defaultLook == 0)
        {
            return [];
        }

        var styles = new List<ItemStyle> { new(defaultLook, DefaultName, null) };

        var alternate = item.AlternateClothingBase ?? 0;
        var hasAlternate = alternate != 0 && alternate != defaultLook;
        if (hasAlternate)
        {
            styles.Add(new ItemStyle(alternate, AlternateName, null));
        }

        foreach (var style in Parse(GetStylesText(item)))
        {
            if (style.ClothingBase == defaultLook)
            {
                if (style.Name != null && styles[0].Name == DefaultName)
                {
                    styles[0] = styles[0] with { Name = style.Name };
                }

                continue;
            }

            var existing = styles.FindIndex(s => s.ClothingBase == style.ClothingBase);
            if (existing >= 0)
            {
                // AlternateClothingBase listed again in ItemStyles: let the entry name it, but keep it unlocked.
                if (hasAlternate && existing == 1 && style.Name != null && styles[1].Name == AlternateName)
                {
                    styles[1] = styles[1] with { Name = style.Name };
                }

                continue;
            }

            styles.Add(style with { Name = style.Name ?? $"Style {styles.Count + 1}" });
        }

        return styles;
    }

    /// <summary>
    /// The styles a player with these quests can choose, starting with the default.
    /// </summary>
    public static IReadOnlyList<ItemStyle> GetAvailableStyles(WorldObject item, Func<string, bool> hasQuest)
    {
        return GetStyles(item).Where(s => s.IsUnlockedFor(hasQuest)).ToList();
    }

    public static IReadOnlyList<ItemStyle> GetAvailableStyles(WorldObject item, Player player)
    {
        return GetAvailableStyles(item, HasQuest(player));
    }

    /// <summary>
    /// True when a player with these quests could switch the item to a look other than the one it shows.
    /// </summary>
    public static bool CanSwitch(WorldObject item, Func<string, bool> hasQuest)
    {
        var current = item?.ClothingBase ?? 0;
        return GetAvailableStyles(item, hasQuest).Any(s => s.ClothingBase != current);
    }

    public static bool CanSwitch(WorldObject item, Player player)
    {
        return CanSwitch(item, HasQuest(player));
    }

    /// <summary>
    /// The style the item shows, or null when it shows a look that isn't one of its styles any more.
    /// </summary>
    public static ItemStyle GetCurrentStyle(WorldObject item)
    {
        var current = item?.ClothingBase ?? 0;
        return GetStyles(item).FirstOrDefault(s => s.ClothingBase == current);
    }

    /// <summary>
    /// The next style after the one the item shows that a player with these quests can choose, wrapping around to the
    /// default. Null when there is no other look to switch to.
    /// </summary>
    public static ItemStyle GetNextStyle(WorldObject item, Func<string, bool> hasQuest)
    {
        var current = item?.ClothingBase ?? 0;
        var styles = GetStyles(item);
        var start = styles.ToList().FindIndex(s => s.ClothingBase == current);

        for (var step = 1; step <= styles.Count; step++)
        {
            var style = styles[(start + step) % styles.Count];

            if (style.ClothingBase != current && style.IsUnlockedFor(hasQuest))
            {
                return style;
            }
        }

        return null;
    }

    /// <summary>
    /// Finds a style a player with these quests can choose, by its number in the available list (1 is the default)
    /// or by name; "default" is always the default look.
    /// </summary>
    public static ItemStyle FindAvailableStyle(WorldObject item, string numberOrName, Func<string, bool> hasQuest)
    {
        var available = GetAvailableStyles(item, hasQuest);

        if (available.Count == 0)
        {
            return null;
        }

        // "default" always means the default look, even when ItemStyles gives it another name.
        if (string.Equals(numberOrName, DefaultName, StringComparison.OrdinalIgnoreCase))
        {
            return available[0];
        }

        if (int.TryParse(numberOrName, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
        {
            return number >= 1 && number <= available.Count ? available[number - 1] : null;
        }

        return available.FirstOrDefault(s => string.Equals(s.Name, numberOrName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Switches the item to a style. Callers check the style is one the player can choose. Returns false, changing
    /// nothing, when the item already shows it.
    /// </summary>
    public static bool Apply(WorldObject item, ItemStyle style)
    {
        if (item == null || style == null || item.ClothingBase == style.ClothingBase)
        {
            return false;
        }

        var defaultLook = GetDefaultClothingBase(item);

        if (defaultLook == 0)
        {
            return false;
        }

        if (style.ClothingBase == defaultLook)
        {
            item.ClothingBase = defaultLook;
            item.ItemStyleDefaultClothingBase = null;
            return true;
        }

        item.ItemStyleDefaultClothingBase = defaultLook;
        item.ClothingBase = style.ClothingBase;
        return true;
    }

    private static Func<string, bool> HasQuest(Player player)
    {
        return player == null ? _ => false : player.QuestManager.HasQuest;
    }
}
