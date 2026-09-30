using System;
using System.Collections.Generic;
using System.Linq;
using ACE.Database;
using ACE.DatLoader;
using ACE.DatLoader.FileTypes;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Entity.Models;
using ACE.Server.WorldObjects;

namespace ACE.Server.Entity;

public enum PackIconStyle
{
    Pack,
    Sack
}

/// <summary>
/// What /bank icon was asked for: a style, a color, either left out to keep the pack's current one, or the pack's
/// own icon back.
/// </summary>
public readonly record struct PackIconRequest(PackIconStyle? Style, string Color, bool Default);

/// <summary>
/// The icons /bank icon gives a pack: a plain Pack's or Sack's icon in a color. Only the icon changes; the pack keeps
/// its model, name, capacity and everything else.
///
/// A color is one of the Pack's or Sack's palette templates, the way dye colors clothing, and its icon comes from that
/// style's clothing table in the portal.dat, so the colors offered are the ones this server's client files draw.
/// </summary>
public static class PackIcons
{
    // the plain Pack and Sack, whose clothing tables hold the icons
    public const uint PackWcid = 136;
    public const uint SackWcid = 166;

    public const string DefaultColor = "brown";

    // Each color's palette templates, the first the clothing table has winning.
    // A plain Pack and Sack are Gold, which draws their brown leather.
    private static readonly (string Name, PaletteTemplate[] Templates)[] Colors =
    {
        ("brown", new[] { PaletteTemplate.Gold, PaletteTemplate.Brown, PaletteTemplate.DeepBrown }),
        ("black", new[] { PaletteTemplate.Black }),
        ("white", new[] { PaletteTemplate.White, PaletteTemplate.SnowyWhite }),
        ("gray", new[] { PaletteTemplate.Grey, PaletteTemplate.MediumGrey, PaletteTemplate.MidGrey }),
        ("blue", new[] { PaletteTemplate.Blue, PaletteTemplate.DarkBlue }),
        ("teal", new[] { PaletteTemplate.Aqua, PaletteTemplate.AquaBlue, PaletteTemplate.BlueGreen }),
        ("green", new[] { PaletteTemplate.Green, PaletteTemplate.DeepGreen }),
        ("yellow", new[] { PaletteTemplate.Yellow }),
        ("red", new[] { PaletteTemplate.Red }),
        ("purple", new[] { PaletteTemplate.Purple }),
        ("orange", new[] { PaletteTemplate.Orange, PaletteTemplate.PaleOrange })
    };

    private static readonly Dictionary<string, string> ColorAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        { "grey", "gray" }
    };

    private static readonly Dictionary<string, PackIconStyle> StyleWords = new(StringComparer.OrdinalIgnoreCase)
    {
        { "pack", PackIconStyle.Pack },
        { "backpack", PackIconStyle.Pack },
        { "sack", PackIconStyle.Sack }
    };

    private static readonly HashSet<string> DefaultWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "default",
        "reset",
        "original"
    };

    public static IEnumerable<string> ColorNames => Colors.Select(c => c.Name);

    /// <summary>
    /// Reads "sack blue", "blue sack", "blue", "sack" or "default". Every word must be a style, a color or "default",
    /// with at most one of each, and "default" alone.
    /// </summary>
    public static bool TryParse(IEnumerable<string> words, out PackIconRequest request)
    {
        request = default;

        PackIconStyle? style = null;
        string color = null;
        var isDefault = false;
        var count = 0;

        foreach (var word in words.Select(w => w.Trim(',', '.')).Where(w => w.Length > 0))
        {
            count++;

            if (StyleWords.TryGetValue(word, out var s) && style == null)
            {
                style = s;
            }
            else if (TryParseColor(word, out var c) && color == null)
            {
                color = c;
            }
            else if (DefaultWords.Contains(word))
            {
                isDefault = true;
            }
            else
            {
                return false;
            }
        }

        if (count == 0 || (isDefault && count > 1))
        {
            return false;
        }

        request = new PackIconRequest(style, color, isDefault);
        return true;
    }

    public static bool TryParseColor(string word, out string color)
    {
        color = ColorAliases.TryGetValue(word, out var alias) ? alias : word.ToLowerInvariant();

        var name = color;
        return Colors.Any(c => c.Name == name);
    }

    /// <summary>
    /// The icon for a color, from a clothing table's icons by palette template, or null if it has none of that color's templates.
    /// </summary>
    public static uint? PickIcon(string color, IReadOnlyDictionary<uint, uint> iconsByTemplate)
    {
        var templates = Colors.FirstOrDefault(c => c.Name == color).Templates ?? Array.Empty<PaletteTemplate>();

        foreach (var template in templates)
        {
            if (iconsByTemplate.TryGetValue((uint)template, out var icon) && icon != 0)
            {
                return icon;
            }
        }

        return null;
    }

    /// <summary>
    /// The icon for a style and color, or null if this server's client files don't draw that color.
    /// </summary>
    public static uint? IconFor(PackIconStyle style, string color)
    {
        return PickIcon(color, IconsByTemplate(style));
    }

    public static List<string> AvailableColors(PackIconStyle style)
    {
        var icons = IconsByTemplate(style);

        return Colors.Where(c => PickIcon(c.Name, icons) != null).Select(c => c.Name).ToList();
    }

    /// <summary>
    /// The style and color a pack shows now, if its icon is one of these; otherwise null.
    /// </summary>
    public static (PackIconStyle Style, string Color)? LookOf(WorldObject pack)
    {
        foreach (var style in Enum.GetValues<PackIconStyle>())
        {
            var icons = IconsByTemplate(style);

            foreach (var (name, _) in Colors)
            {
                if (PickIcon(name, icons) == pack.IconId)
                {
                    return (style, name);
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Gives a pack an icon from here. The clothing table would otherwise put back the icon of the pack's own color
    /// (WorldObject.CalculateObjDesc), so it is told not to.
    /// </summary>
    public static void Apply(WorldObject pack, uint icon)
    {
        pack.IconId = icon;
        pack.IgnoreCloIcons = true;
    }

    /// <summary>
    /// Gives a pack its weenie's icon back. False if the weenie can't be found.
    /// </summary>
    public static bool Restore(WorldObject pack)
    {
        var weenie = DatabaseManager.World.GetCachedWeenie(pack.WeenieClassId);
        if (weenie == null)
        {
            return false;
        }

        pack.IconId = weenie.GetProperty(PropertyDataId.Icon) ?? 0;
        pack.IgnoreCloIcons = weenie.GetProperty(PropertyBool.IgnoreCloIcons);
        return true;
    }

    private static Dictionary<uint, uint> IconsByTemplate(PackIconStyle style)
    {
        var weenie = DatabaseManager.World.GetCachedWeenie(style == PackIconStyle.Sack ? SackWcid : PackWcid);

        if (weenie?.GetProperty(PropertyDataId.ClothingBase) is not { } clothingBase)
        {
            return new Dictionary<uint, uint>();
        }

        return DatManager.PortalDat.ReadFromDat<ClothingTable>(clothingBase)
            .ClothingSubPalEffects.ToDictionary(e => e.Key, e => e.Value.Icon);
    }
}
