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
/// The parts of an item's appearance a style can change. As a style's changes, a null value is one the style leaves
/// as the default look has it; as a whole look, it is one the item doesn't have.
/// </summary>
public sealed record ItemLook(
    uint? ClothingBase = null,
    int? PaletteTemplate = null,
    double? Shade = null,
    uint? Icon = null,
    uint? Setup = null,
    uint? PhysicsScript = null
)
{
    /// <summary>
    /// script=0 in a style's changes removes the default look's physics script.
    /// </summary>
    public const uint NoScript = 0;

    public bool IsEmpty => this == new ItemLook();

    public static ItemLook Of(WorldObject item)
    {
        return new ItemLook(
            item.ClothingBase,
            item.PaletteTemplate,
            item.Shade,
            item.GetProperty(PropertyDataId.Icon),
            item.GetProperty(PropertyDataId.Setup),
            item.GetProperty(PropertyDataId.PhysicsScript)
        );
    }

    /// <summary>
    /// These changes laid over a whole look.
    /// </summary>
    public ItemLook Over(ItemLook look)
    {
        return new ItemLook(
            ClothingBase ?? look.ClothingBase,
            PaletteTemplate ?? look.PaletteTemplate,
            Shade ?? look.Shade,
            Icon ?? look.Icon,
            Setup ?? look.Setup,
            PhysicsScript == NoScript ? null : PhysicsScript ?? look.PhysicsScript
        );
    }

    /// <summary>
    /// Gives the item this whole look, setting only the values that differ so unchanged properties aren't saved again.
    /// </summary>
    public void ApplyTo(WorldObject item)
    {
        if (item.ClothingBase != ClothingBase)
        {
            item.ClothingBase = ClothingBase;
        }

        if (item.PaletteTemplate != PaletteTemplate)
        {
            item.PaletteTemplate = PaletteTemplate;
        }

        if (item.Shade != Shade)
        {
            item.Shade = Shade;
        }

        SetDataId(item, PropertyDataId.Icon, Icon);
        SetDataId(item, PropertyDataId.Setup, Setup);
        SetDataId(item, PropertyDataId.PhysicsScript, PhysicsScript);
    }

    private static void SetDataId(WorldObject item, PropertyDataId property, uint? value)
    {
        if (item.GetProperty(property) == value)
        {
            return;
        }

        if (value == null)
        {
            item.RemoveProperty(property);
        }
        else
        {
            item.SetProperty(property, value.Value);
        }
    }

    /// <summary>
    /// Written the way ItemStyles' changes are, plus clothing=, leaving out values the look doesn't have. Read back
    /// with TryRead.
    /// </summary>
    public string Write()
    {
        var parts = new List<string>();

        if (ClothingBase != null)
        {
            parts.Add($"clothing=0x{ClothingBase:X8}");
        }

        if (PaletteTemplate != null)
        {
            parts.Add($"palette={PaletteTemplate.Value.ToString(CultureInfo.InvariantCulture)}");
        }

        if (Shade != null)
        {
            parts.Add($"shade={Shade.Value.ToString("R", CultureInfo.InvariantCulture)}");
        }

        if (Icon != null)
        {
            parts.Add($"icon=0x{Icon:X8}");
        }

        if (Setup != null)
        {
            parts.Add($"setup=0x{Setup:X8}");
        }

        if (PhysicsScript != null)
        {
            parts.Add($"script=0x{PhysicsScript:X8}");
        }

        return string.Join(",", parts);
    }

    public static bool TryRead(string text, out ItemLook look, out string error)
    {
        return TryReadChanges(text, new ItemLook(), out look, out error);
    }

    /// <summary>
    /// Reads comma-separated key=value changes onto a look. Ids must be of their DAT type, so a value given to the
    /// wrong key is caught here rather than drawing nothing in game.
    /// </summary>
    public static bool TryReadChanges(string text, ItemLook start, out ItemLook look, out string error)
    {
        look = start;
        error = null;

        foreach (var part in text.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var pair = part.Split('=', 2);
            var key = pair[0].Trim().ToLowerInvariant();
            var value = pair.Length > 1 ? pair[1].Trim() : "";

            if (key.Length == 0)
            {
                continue;
            }

            switch (key)
            {
                case "clothing":
                    if (!TryReadDataId(value, 0x10, out var clothingBase))
                    {
                        error = $"clothing={value} is not a clothing table id (0x10...)";
                        return false;
                    }

                    look = look with { ClothingBase = clothingBase };
                    break;

                case "palette":
                    if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var palette)
                        || palette < 0)
                    {
                        error = $"palette={value} is not a palette template number";
                        return false;
                    }

                    look = look with { PaletteTemplate = palette };
                    break;

                case "shade":
                    if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var shade)
                        || shade < 0
                        || shade > 1)
                    {
                        error = $"shade={value} is not a number from 0 to 1";
                        return false;
                    }

                    look = look with { Shade = shade };
                    break;

                case "icon":
                    if (!TryReadDataId(value, 0x06, out var icon))
                    {
                        error = $"icon={value} is not an image id (0x06...)";
                        return false;
                    }

                    look = look with { Icon = icon };
                    break;

                case "setup":
                    if (!TryReadDataId(value, 0x02, out var setup))
                    {
                        error = $"setup={value} is not a setup id (0x02...)";
                        return false;
                    }

                    look = look with { Setup = setup };
                    break;

                case "script":
                    if (ItemStyles.TryParseDataId(value, out var none) && none == NoScript)
                    {
                        look = look with { PhysicsScript = NoScript };
                        break;
                    }

                    if (!TryReadDataId(value, 0x33, out var script))
                    {
                        error = $"script={value} is not a physics script id (0x33...) or 0";
                        return false;
                    }

                    look = look with { PhysicsScript = script };
                    break;

                default:
                    error = $"{key}= is not something a style can change";
                    return false;
            }
        }

        return true;
    }

    private static bool TryReadDataId(string text, uint type, out uint value)
    {
        return ItemStyles.TryParseDataId(text, out value) && value >> 24 == type && (value & 0xFFFFFF) != 0;
    }
}

/// <summary>
/// One look an item can show: a name players pick it by, optionally a quest flag a player needs to choose it, and what
/// it changes. From Parse, Look holds only the entry's changes; from GetStyles, it is the whole look.
/// </summary>
public sealed record ItemStyle(string Name, string QuestFlag, ItemLook Look)
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
///   1. its default look, the one it had before it was first switched;
///   2. its AlternateClothingBase, if it has one (the older, two-look way of giving an item a second look);
///   3. each entry of its ItemStyles string, one per line or separated by ';', written
///      ClothingBase|Name|QuestFlag|Changes, where Changes is a comma-separated list of
///        palette=&lt;PaletteTemplate&gt;  shade=&lt;0 to 1&gt;  icon=0x06...  setup=0x02...  script=0x33... (or 0 for none)
///      e.g. "0x10000ABC|Shadow", "|Crimson||palette=14,shade=0.2", "|Frost|ACCOUNT_Frost|setup=0x02001234".
/// Every part is optional, but an entry has to change something or name the default. What an entry leaves out stays
/// as the default look has it, so a recolor works on every loot roll of the item.
/// A style with a QuestFlag can only be chosen by players who have that quest (ACCOUNT_ flags count for every character
/// on the account); the default and alternate looks never need one.
///
/// A style is identified by the whole look it gives, so two entries giving the same look are one style. The default
/// look is kept in ItemStyleDefaultLook while the item shows another, so it can always be restored, and that property
/// is cleared again when the item returns to its default. Whoever holds an item can always switch it back to its
/// default, so an item traded while showing a locked style is never stuck in a look its new owner can't choose.
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
            var changes = new ItemLook();

            if (fields[0].Length > 0)
            {
                if (!ItemLook.TryReadChanges($"clothing={fields[0]}", changes, out changes, out var error))
                {
                    _log.Warning("ItemStyles entry {Entry}: {Error}; skipping it", entry.Trim(), error);
                    continue;
                }
            }

            if (fields.Length > 3 && !ItemLook.TryReadChanges(fields[3], changes, out changes, out var changeError))
            {
                _log.Warning("ItemStyles entry {Entry}: {Error}; skipping it", entry.Trim(), changeError);
                continue;
            }

            var name = fields.Length > 1 && fields[1].Length > 0 ? fields[1] : null;
            var questFlag = fields.Length > 2 && fields[2].Length > 0 ? fields[2] : null;

            if (changes.IsEmpty && name == null)
            {
                continue;
            }

            styles.Add(new ItemStyle(name, questFlag, changes));
        }

        return styles;
    }

    internal static bool TryParseDataId(string text, out uint value)
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
    public static ItemLook GetDefaultLook(WorldObject item)
    {
        var saved = item.ItemStyleDefaultLook;

        if (saved != null)
        {
            if (ItemLook.TryRead(saved, out var look, out var error))
            {
                return look;
            }

            _log.Error(
                "{Item} (0x{Guid:X8}) has an ItemStyleDefaultLook that can't be read ({Error}); treating its current look as its default",
                item.Name,
                item.Guid.Full,
                error
            );
        }

        var current = ItemLook.Of(item);

        // Switched before the whole default look was kept: only its ClothingBase differs.
        var legacy = item.ItemStyleDefaultClothingBase;
        return legacy != null ? current with { ClothingBase = legacy } : current;
    }

    /// <summary>
    /// Every style of the item, locked or not, starting with its default, each with the whole look it gives. Styles
    /// giving a look already listed are dropped, except that the first ItemStyles entry giving the default or alternate
    /// look names it.
    /// </summary>
    public static IReadOnlyList<ItemStyle> GetStyles(WorldObject item)
    {
        if (item == null)
        {
            return [];
        }

        var defaultLook = GetDefaultLook(item);
        var styles = new List<ItemStyle> { new(DefaultName, null, defaultLook) };

        var alternate = item.AlternateClothingBase ?? 0;
        var hasAlternate = alternate != 0 && alternate != defaultLook.ClothingBase;
        if (hasAlternate)
        {
            styles.Add(new ItemStyle(AlternateName, null, defaultLook with { ClothingBase = alternate }));
        }

        foreach (var style in Parse(GetStylesText(item)))
        {
            var look = style.Look.Over(defaultLook);
            var existing = styles.FindIndex(s => s.Look == look);

            if (existing == 0)
            {
                if (style.Name != null && styles[0].Name == DefaultName)
                {
                    styles[0] = styles[0] with { Name = style.Name };
                }

                continue;
            }

            if (existing > 0)
            {
                // AlternateClothingBase listed again in ItemStyles: let the entry name it, but keep it unlocked.
                if (hasAlternate && existing == 1 && style.Name != null && styles[1].Name == AlternateName)
                {
                    styles[1] = styles[1] with { Name = style.Name };
                }

                continue;
            }

            styles.Add(style with { Name = style.Name ?? $"Style {styles.Count + 1}", Look = look });
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
        if (item == null)
        {
            return false;
        }

        var current = ItemLook.Of(item);
        return GetAvailableStyles(item, hasQuest).Any(s => s.Look != current);
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
        if (item == null)
        {
            return null;
        }

        var current = ItemLook.Of(item);
        return GetStyles(item).FirstOrDefault(s => s.Look == current);
    }

    /// <summary>
    /// The next style after the one the item shows that a player with these quests can choose, wrapping around to the
    /// default. Null when there is no other look to switch to.
    /// </summary>
    public static ItemStyle GetNextStyle(WorldObject item, Func<string, bool> hasQuest)
    {
        if (item == null)
        {
            return null;
        }

        var current = ItemLook.Of(item);
        var styles = GetStyles(item);
        var start = styles.ToList().FindIndex(s => s.Look == current);

        for (var step = 1; step <= styles.Count; step++)
        {
            var style = styles[(start + step) % styles.Count];

            if (style.Look != current && style.IsUnlockedFor(hasQuest))
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
    /// Switches the item to a style from GetStyles (whose Look is a whole look). Callers check the style is one the
    /// player can choose. Returns false, changing nothing, when the item already shows it.
    /// </summary>
    public static bool Apply(WorldObject item, ItemStyle style)
    {
        if (item == null || style == null || style.Look == ItemLook.Of(item))
        {
            return false;
        }

        var defaultLook = GetDefaultLook(item);

        item.ItemStyleDefaultLook = style.Look == defaultLook ? null : defaultLook.Write();
        item.ItemStyleDefaultClothingBase = null;
        style.Look.ApplyTo(item);
        return true;
    }

    private static Func<string, bool> HasQuest(Player player)
    {
        return player == null ? _ => false : player.QuestManager.HasQuest;
    }
}
