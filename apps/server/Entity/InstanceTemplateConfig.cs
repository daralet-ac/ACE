using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using ACE.Entity;

namespace ACE.Server.Entity;

/// <summary>
/// Turns the text of instances.json into instance templates.<para />
/// The file lists islands. An island is some landblocks (its interior) and a margin of buffer landblocks around them (its ring).
/// The interior and the ring are what an instance of the island is made of, and players are turned back when they get into the ring.
/// Nothing in here needs the server to be running, and a mistake in one island is reported without losing the others.
/// </summary>
public static class InstanceTemplateConfig
{
    /// <summary>
    /// The most landblocks an island can be made of, ring included. A typing mistake in a rectangle (E74E to FFFF)
    /// must not be able to make the server load the whole map for one instance.
    /// </summary>
    public const int MaxFootprintLandblocks = 400;

    public const int MaxBufferRing = 4;

    /// <summary>
    /// The names of the instances the capstone dungeons make are in here, so an island can't take one
    /// </summary>
    public const string ReservedPrefix = "capstone:";

    // The same as the server's other json files (ConfigManager.SerializerOptions: comments and trailing commas are allowed, and numbers can be text)
    // and the names of the properties don't care about upper and lower case. This is not ConfigManager's so that it doesn't need the server's config.
    private static readonly JsonSerializerOptions Options =
        new()
        {
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
            PropertyNameCaseInsensitive = true
        };

    internal sealed class IslandFile
    {
        public List<Island> Islands { get; set; }
    }

    internal sealed class Island
    {
        public string Name { get; set; }
        public List<string> Landblocks { get; set; }
        public List<Rectangle> Rectangles { get; set; }
        public int BufferRing { get; set; } = 1;
        public bool? InstanceOnly { get; set; }
        public PositionEntry Entry { get; set; }
        public PositionEntry Return { get; set; }
    }

    internal sealed class Rectangle
    {
        public string From { get; set; }
        public string To { get; set; }
    }

    internal sealed class PositionEntry
    {
        public string Cell { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float Qx { get; set; }
        public float Qy { get; set; }
        public float Qz { get; set; }
        public float Qw { get; set; } = 1f;
    }

    /// <summary>
    /// The templates of every island in the text that is right. Whatever is wrong with the others is added to errors.
    /// </summary>
    public static List<InstanceTemplate> Parse(string json, List<string> errors)
    {
        var templates = new List<InstanceTemplate>();

        IslandFile file;

        try
        {
            file = JsonSerializer.Deserialize<IslandFile>(json, Options);
        }
        catch (JsonException ex)
        {
            errors.Add($"The file can't be read: {ex.Message}");
            return templates;
        }

        if (file?.Islands == null)
        {
            return templates;
        }

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < file.Islands.Count; i++)
        {
            var island = file.Islands[i];

            if (island == null)
            {
                errors.Add($"island #{i + 1} is empty");
                continue;
            }

            var label = string.IsNullOrWhiteSpace(island.Name) ? $"island #{i + 1}" : $"island '{island.Name.Trim()}'";

            var template = Build(island, label, names, errors);

            if (template != null)
            {
                templates.Add(template);
            }
        }

        // Where players are sent when an island ends has to be a place they can go, so it can't be in a landblock that only exists as an instance
        var instanceOnly = new HashSet<LandblockId>(templates.Where(t => t.InstanceOnly).SelectMany(t => t.Footprint));

        templates.RemoveAll(template =>
        {
            if (template.ReturnPosition == null || !instanceOnly.Contains(template.ReturnPosition.LandblockId))
            {
                return false;
            }

            errors.Add(
                $"island '{template.Name}': the return position is in a landblock that only exists as an instance, so nobody could go there"
            );
            return true;
        });

        return templates;
    }

    private static InstanceTemplate Build(Island island, string label, HashSet<string> names, List<string> errors)
    {
        var ok = true;

        void Fail(string message)
        {
            errors.Add($"{label}: {message}");
            ok = false;
        }

        if (string.IsNullOrWhiteSpace(island.Name))
        {
            Fail("it has no name");
        }
        else if (island.Name.StartsWith(ReservedPrefix, StringComparison.OrdinalIgnoreCase))
        {
            Fail($"names that start with \"{ReservedPrefix}\" are for the capstone dungeons");
        }
        else if (!names.Add(island.Name.Trim()))
        {
            Fail("another island has the same name");
        }

        if (island.InstanceOnly == null)
        {
            Fail(
                "instanceOnly has to be set, to true or to false. True means these landblocks stop existing in the persistent world: nobody can go there, and nothing is loaded there"
            );
        }

        if (island.BufferRing < 0 || island.BufferRing > MaxBufferRing)
        {
            Fail($"bufferRing has to be from 0 to {MaxBufferRing}");
        }

        var listed = (island.Landblocks?.Count ?? 0) + (island.Rectangles?.Count ?? 0);
        var interior = new HashSet<LandblockId>();

        foreach (var text in island.Landblocks ?? new List<string>())
        {
            if (TryParseLandblock(text, out var landblock))
            {
                interior.Add(landblock);
            }
            else
            {
                Fail($"\"{text}\" is not a landblock. Landblocks are written like E74E");
            }
        }

        foreach (var rectangle in island.Rectangles ?? new List<Rectangle>())
        {
            if (
                rectangle == null
                || !TryParseLandblock(rectangle.From, out var from)
                || !TryParseLandblock(rectangle.To, out var to)
            )
            {
                Fail("a rectangle needs a \"from\" and a \"to\" landblock, written like E74E");
                continue;
            }

            var width = Math.Abs(from.LandblockX - to.LandblockX) + 1;
            var height = Math.Abs(from.LandblockY - to.LandblockY) + 1;

            if (width * height > MaxFootprintLandblocks)
            {
                Fail(
                    $"the rectangle from {rectangle.From} to {rectangle.To} is {width * height} landblocks. An island can't be more than {MaxFootprintLandblocks}, with its ring"
                );
                continue;
            }

            for (var x = Math.Min(from.LandblockX, to.LandblockX); x <= Math.Max(from.LandblockX, to.LandblockX); x++)
            {
                for (
                    var y = Math.Min(from.LandblockY, to.LandblockY);
                    y <= Math.Max(from.LandblockY, to.LandblockY);
                    y++
                )
                {
                    interior.Add(new LandblockId((byte)x, (byte)y));
                }
            }
        }

        if (listed == 0)
        {
            Fail("it has no landblocks. List them in \"landblocks\" and \"rectangles\"");
        }

        Position entry = null;

        if (!TryBuildPosition(island.Entry, out entry, out var problem))
        {
            Fail($"entry: {problem}");
        }
        else if (interior.Count > 0 && !interior.Contains(entry.LandblockId))
        {
            Fail(
                $"the entry position is in landblock {entry.LandblockId.Landblock:X4}, which is not one of the island's landblocks. It can't be in the ring either, players are turned back from there"
            );
        }

        Position returnPosition = null;

        if (island.Return != null && !TryBuildPosition(island.Return, out returnPosition, out problem))
        {
            Fail($"return: {problem}");
        }

        if (!ok)
        {
            return null;
        }

        var ring = InstanceTemplate.Ring(interior, island.BufferRing);

        if (interior.Count + ring.Count > MaxFootprintLandblocks)
        {
            Fail(
                $"it is {interior.Count + ring.Count} landblocks with its ring. An island can't be more than {MaxFootprintLandblocks}"
            );
            return null;
        }

        if (
            returnPosition != null
            && (interior.Contains(returnPosition.LandblockId) || ring.Contains(returnPosition.LandblockId))
        )
        {
            Fail("the return position is inside the island, so leaving it would put players straight back in");
            return null;
        }

        try
        {
            return new InstanceTemplate(
                island.Name.Trim(),
                interior.Concat(ring),
                entry,
                returnPosition,
                island.InstanceOnly.Value,
                ring
            );
        }
        catch (ArgumentException ex)
        {
            Fail(ex.Message);
            return null;
        }
    }

    /// <summary>
    /// A landblock is written as its four hex digits (E74E), or as the start of a cell (0xE74E0019, E74EFFFF)
    /// </summary>
    internal static bool TryParseLandblock(string text, out LandblockId landblock)
    {
        landblock = default;

        if (!TryParseHex(text, out var value, out var digits) || (digits != 4 && digits != 8))
        {
            return false;
        }

        var xy = digits == 4 ? value : value >> 16;
        var x = (xy >> 8) & 0xFF;
        var y = xy & 0xFF;

        // 255 is not a landblock of the map
        if (x > 254 || y > 254)
        {
            return false;
        }

        landblock = new LandblockId((byte)x, (byte)y);
        return true;
    }

    private static bool TryParseHex(string text, out uint value, out int digits)
    {
        value = 0;
        digits = 0;

        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        text = text.Trim();

        if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            text = text.Substring(2);
        }

        digits = text.Length;

        return digits > 0
            && digits <= 8
            && uint.TryParse(text, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out value);
    }

    private static bool TryBuildPosition(PositionEntry entry, out Position position, out string problem)
    {
        position = null;

        if (entry == null)
        {
            problem = "it is missing";
            return false;
        }

        if (
            !TryParseHex(entry.Cell, out var cell, out var digits)
            || digits != 8
            || !TryParseLandblock(entry.Cell, out _)
        )
        {
            problem = $"\"{entry.Cell}\" is not a cell. Cells are written like 0xE74E0019";
            return false;
        }

        if (new Quaternion(entry.Qx, entry.Qy, entry.Qz, entry.Qw).LengthSquared() < 0.0001f)
        {
            problem = "the rotation is empty. qw is 1 (and qx, qy and qz are 0) for a position that is not turned";
            return false;
        }

        position = new Position(cell, entry.X, entry.Y, entry.Z, entry.Qx, entry.Qy, entry.Qz, entry.Qw);
        problem = null;
        return true;
    }
}
