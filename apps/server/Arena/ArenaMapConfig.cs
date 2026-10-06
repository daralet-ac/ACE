using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using ACE.Entity;
using ACE.Server.Entity;

namespace ACE.Server.Arena;

/// <summary>
/// Turns the text of arenas.json into arena maps. A mistake in one map is reported without losing the others.
/// Nothing in here needs the server to be running.
/// </summary>
public static class ArenaMapConfig
{
    /// <summary>
    /// The most landblocks a map can be made of, ring included. A duel needs very little room, and every landblock is loaded for every duel.
    /// </summary>
    public const int MaxFootprintLandblocks = 49;

    public const int MaxBufferRing = 2;

    // names go into /instance and admin commands, so they are kept to what is easy to type
    private static readonly Regex NamePattern = new Regex("^[A-Za-z0-9][A-Za-z0-9_-]*$", RegexOptions.Compiled);

    // the same as instances.json: comments and trailing commas are allowed, numbers can be text, and names don't care about case
    private static readonly JsonSerializerOptions Options =
        new()
        {
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
            PropertyNameCaseInsensitive = true
        };

    private static readonly JsonDocumentOptions DocumentOptions =
        new() { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip };

    internal sealed class Entry
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public bool Enabled { get; set; } = true;
        public List<string> Landblocks { get; set; }
        public int BufferRing { get; set; }
        public List<InstanceTemplateConfig.PositionEntry> Starts { get; set; }
        public InstanceTemplateConfig.PositionEntry Center { get; set; }
        public float Radius { get; set; }
    }

    /// <summary>
    /// The maps in the text that are right. Whatever is wrong with the others is added to errors.
    /// </summary>
    public static List<ArenaMap> Parse(string json, List<string> errors)
    {
        var maps = new List<ArenaMap>();

        JsonDocument document;

        try
        {
            document = JsonDocument.Parse(json, DocumentOptions);
        }
        catch (JsonException ex)
        {
            errors.Add($"The file can't be read: {ex.Message}");
            return maps;
        }

        using (document)
        {
            var root = document.RootElement;

            if (root.ValueKind == JsonValueKind.Null)
            {
                return maps;
            }

            if (root.ValueKind != JsonValueKind.Object)
            {
                errors.Add(
                    "The file can't be read: it has to be an object with a list of arenas, { \"arenas\": [ ... ] }"
                );
                return maps;
            }

            JsonElement? list = null;

            foreach (var property in root.EnumerateObject())
            {
                if (string.Equals(property.Name, "arenas", StringComparison.OrdinalIgnoreCase))
                {
                    list = property.Value;
                }
            }

            if (list == null || list.Value.ValueKind == JsonValueKind.Null)
            {
                return maps;
            }

            if (list.Value.ValueKind != JsonValueKind.Array)
            {
                errors.Add("The file can't be read: \"arenas\" has to be a list, [ ... ]");
                return maps;
            }

            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var number = 0;

            // each map is read on its own, so that a value of the wrong kind in one of them only leaves that map out
            foreach (var element in list.Value.EnumerateArray())
            {
                number++;

                Entry entry;

                try
                {
                    entry = element.Deserialize<Entry>(Options);
                }
                catch (JsonException ex)
                {
                    errors.Add(
                        $"arena #{number}: {(ex.Path is { Length: > 2 } path ? $"{path[2..]} is not the right kind of value" : "it has to be an object, { ... }")}"
                    );
                    continue;
                }

                if (entry == null)
                {
                    errors.Add($"arena #{number} is empty");
                    continue;
                }

                var label = string.IsNullOrWhiteSpace(entry.Name) ? $"arena #{number}" : $"arena '{entry.Name.Trim()}'";

                var map = Build(entry, label, names, errors);

                if (map != null)
                {
                    maps.Add(map);
                }
            }
        }

        return maps;
    }

    private static ArenaMap Build(Entry entry, string label, HashSet<string> names, List<string> errors)
    {
        var ok = true;

        void Fail(string message)
        {
            errors.Add($"{label}: {message}");
            ok = false;
        }

        var name = entry.Name?.Trim();

        if (string.IsNullOrEmpty(name))
        {
            Fail("it has no name");
        }
        else if (!NamePattern.IsMatch(name))
        {
            Fail("a name can only have letters, digits, - and _ in it (pkl-arena)");
        }
        else if (!names.Add(name))
        {
            Fail("another arena has the same name");
        }

        var landblocks = new List<LandblockId>();

        if (entry.Landblocks == null || entry.Landblocks.Count == 0)
        {
            Fail(
                "it has no landblocks. They are written as the first four digits of a cell: 0x00670117 is landblock 0067"
            );
        }
        else
        {
            foreach (var text in entry.Landblocks)
            {
                if (InstanceTemplateConfig.TryParseLandblock(text, out var landblock))
                {
                    landblocks.Add(landblock);
                }
                else
                {
                    Fail($"\"{text}\" is not a landblock. Landblocks are written like 0067");
                }
            }
        }

        if (entry.BufferRing < 0 || entry.BufferRing > MaxBufferRing)
        {
            Fail($"bufferRing has to be from 0 to {MaxBufferRing}");
        }
        else if (landblocks.Count > 0)
        {
            var footprint = landblocks.Count + InstanceTemplate.Ring(landblocks, entry.BufferRing).Count;

            if (footprint > MaxFootprintLandblocks)
            {
                Fail(
                    $"it would be {footprint} landblocks with its ring, and a map can be at most {MaxFootprintLandblocks}"
                );
            }
        }

        var interior = new HashSet<LandblockId>(landblocks);

        bool InMap(Position position) => interior.Contains(position.LandblockId);

        var starts = new List<Position>();

        if (entry.Starts == null || entry.Starts.Count < 2)
        {
            Fail("it needs at least two starts, the places fighters are put at");
        }
        else
        {
            for (var i = 0; i < entry.Starts.Count; i++)
            {
                if (!InstanceTemplateConfig.TryBuildPosition(entry.Starts[i], out var start, out var problem))
                {
                    Fail($"start #{i + 1}: {problem}");
                }
                else if (interior.Count > 0 && !InMap(start))
                {
                    Fail($"start #{i + 1} is not in one of the arena's landblocks");
                }
                else
                {
                    starts.Add(start);
                }
            }
        }

        Position center = null;

        if (entry.Radius < 0)
        {
            Fail("radius can't be negative");
        }
        else if (entry.Radius > 0)
        {
            if (entry.Center == null || entry.Center.IsEmpty())
            {
                Fail("a map with a radius needs a center, the middle of the fighting area");
            }
            else if (!InstanceTemplateConfig.TryBuildPosition(entry.Center, out center, out var problem))
            {
                Fail($"center: {problem}");
            }
            else if (interior.Count > 0 && !InMap(center))
            {
                Fail("the center is not in one of the arena's landblocks");
            }
            else
            {
                for (var i = 0; i < starts.Count; i++)
                {
                    if (center.Distance2D(starts[i]) > entry.Radius)
                    {
                        Fail(
                            $"start #{i + 1} is outside the radius, so its fighter would be disqualified for standing where they were put"
                        );
                    }
                }
            }
        }

        // Outdoors there are no walls: the ring keeps the edge of the instance out of reach, and the radius is the edge of the fight
        if (starts.Any(s => !s.Indoors))
        {
            if (entry.BufferRing < 1)
            {
                Fail(
                    "an outdoor map needs a bufferRing of at least 1, or fighters could run off the edge of what is loaded"
                );
            }

            if (entry.Radius <= 0)
            {
                Fail(
                    "an outdoor map needs a radius (and a center): the fighting area fighters are disqualified for leaving"
                );
            }
        }

        if (!ok)
        {
            return null;
        }

        try
        {
            return new ArenaMap(
                name,
                entry.Description?.Trim(),
                entry.Enabled,
                landblocks,
                entry.BufferRing,
                starts,
                center,
                entry.Radius
            );
        }
        catch (ArgumentException ex)
        {
            Fail(ex.Message);
            return null;
        }
    }
}
