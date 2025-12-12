using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace ACE.Server.Factories;

internal sealed record SigilStatConfig(
    string PaletteKey,
    string IconColorKey,
    string NameSuffix,
    string UseText,
    bool SetIntensity,
    bool SetReduction,
    bool SetManaReservedZero,
    double CooldownDelta,
    double CooldownMultiplier,
    double TriggerChanceDelta,
    double TriggerChanceMultiplier,
    bool ZeroTriggerChance
);

internal sealed record SigilTrinketRawConfig(
    List<Dictionary<string, uint>> IconColorIds,
    Dictionary<string, int> PaletteTemplateColors,
    Dictionary<string, string> DuplicationElementString,
    Dictionary<string, int> ElementId,
    Dictionary<string, uint> OverlayIds,
    Dictionary<string, uint> TierIconIds,
    Dictionary<string, Dictionary<string, SigilStatRaw>> Maps
);

internal sealed record SigilStatRaw(
    string paletteKey,
    string iconColorKey,
    string nameSuffix,
    string useText,
    bool setIntensity = false,
    bool setReduction = false,
    bool setManaReservedZero = false,
    double cooldownDelta = 0.0,
    double cooldownMultiplier = 1.0,
    double triggerChanceDelta = 0.0,
    double triggerChanceMultiplier = 1.0,
    bool zeroTriggerChance = false,
    string useTextBuilder = null
);

internal static class SigilTrinketConfig
{
    public static IReadOnlyList<Dictionary<string, uint>> IconColorIds { get; private set; } = new List<Dictionary<string, uint>>();
    public static IReadOnlyDictionary<string, int> PaletteTemplateColors { get; private set; } = new Dictionary<string, int>();
    public static IReadOnlyDictionary<string, string> DuplicationElementString { get; private set; } = new Dictionary<string, string>();
    public static IReadOnlyDictionary<string, int> ElementId { get; private set; } = new Dictionary<string, int>();
    public static IReadOnlyDictionary<string, uint> OverlayIds { get; private set; } = new Dictionary<string, uint>();
    public static IReadOnlyDictionary<int, uint> TierIconIds { get; private set; } = new Dictionary<int, uint>();

    // maps by name (consumer will request the correct map)
    public static IReadOnlyDictionary<int, SigilStatConfig> LifeMagicScarab { get; private set; } = new Dictionary<int, SigilStatConfig>();
    public static IReadOnlyDictionary<int, SigilStatConfig> WarMagicScarab { get; private set; } = new Dictionary<int, SigilStatConfig>();
    public static IReadOnlyDictionary<int, SigilStatConfig> ShieldCompass { get; private set; } = new Dictionary<int, SigilStatConfig>();
    public static IReadOnlyDictionary<int, SigilStatConfig> TwohandedCompass { get; private set; } = new Dictionary<int, SigilStatConfig>();
    public static IReadOnlyDictionary<int, SigilStatConfig> DualWieldPuzzleBox { get; private set; } = new Dictionary<int, SigilStatConfig>();
    public static IReadOnlyDictionary<int, SigilStatConfig> ThieveryPuzzleBox { get; private set; } = new Dictionary<int, SigilStatConfig>();
    public static IReadOnlyDictionary<int, SigilStatConfig> PhysicalDefensePocketWatch { get; private set; } = new Dictionary<int, SigilStatConfig>();
    public static IReadOnlyDictionary<int, SigilStatConfig> MagicDefenseTop { get; private set; } = new Dictionary<int, SigilStatConfig>();
    public static IReadOnlyDictionary<int, SigilStatConfig> PerceptionGoggles { get; private set; } = new Dictionary<int, SigilStatConfig>();
    public static IReadOnlyDictionary<int, SigilStatConfig> DeceptionGoggles { get; private set; } = new Dictionary<int, SigilStatConfig>();

    static SigilTrinketConfig()
    {
        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            if (!TryLoadRawConfig(options, out var raw))
            {
                throw new FileNotFoundException("Sigil trinket config not found in candidate locations.");
            }

            IconColorIds = raw.IconColorIds ?? new List<Dictionary<string, uint>>();
            PaletteTemplateColors = raw.PaletteTemplateColors ?? new Dictionary<string, int>();
            DuplicationElementString = raw.DuplicationElementString ?? new Dictionary<string, string>();
            ElementId = raw.ElementId ?? new Dictionary<string, int>();
            OverlayIds = raw.OverlayIds ?? new Dictionary<string, uint>();
            TierIconIds = raw.TierIconIds?.ToDictionary(kv => int.Parse(kv.Key), kv => kv.Value) ?? new Dictionary<int, uint>();

            MapToTyped(raw, "lifeMagicScarab", out var lifeScarabMap);
            LifeMagicScarab = lifeScarabMap;

            MapToTyped(raw, "warMagicScarab", out var warScarabMap);
            WarMagicScarab = warScarabMap;

            MapToTyped(raw, "shieldCompass", out var shieldCompassMap);
            ShieldCompass = shieldCompassMap;

            MapToTyped(raw, "twohandedCompass", out var twohandCompassMap);
            TwohandedCompass = twohandCompassMap;

            MapToTyped(raw, "dualWieldPuzzleBox", out var dualPuzzleBoxMap);
            DualWieldPuzzleBox = dualPuzzleBoxMap;

            MapToTyped(raw, "thieveryPuzzleBox", out var thieveryPuzzleBoxMap);
            ThieveryPuzzleBox = thieveryPuzzleBoxMap;

            MapToTyped(raw, "physicalDefensePocketWatch", out var physicalDefensePocketMap);
            PhysicalDefensePocketWatch = physicalDefensePocketMap;

            MapToTyped(raw, "magicDefenseTop", out var magicDefenseTopMap);
            MagicDefenseTop = magicDefenseTopMap;

            MapToTyped(raw, "perceptionGoggles", out var perceptionGogglesMap);
            PerceptionGoggles = perceptionGogglesMap;

            MapToTyped(raw, "deceptionGoggles", out var deceptionGogglesMap);
            DeceptionGoggles = deceptionGogglesMap;
        }
        catch (Exception ex)
        {
            // In production prefer logging. Re-throw as InvalidOperationException so callers see a consistent failure type.
            throw new InvalidOperationException("Failed to load sigil trinket config", ex);
        }
    }

    // Try multiple candidate locations to find the config file (supports dev and published layouts).
    private static bool TryLoadRawConfig(JsonSerializerOptions options, out SigilTrinketRawConfig raw)
    {
        raw = null;

        var candidates = GetCandidateConfigPaths().ToList();
        foreach (var path in candidates)
        {
            try
            {
                if (!File.Exists(path))
                {
                    continue;
                }

                var json = File.ReadAllText(path);

                // strip C-style comments (// and /* */) so designers can keep annotated files
                json = StripJsonComments(json);

                raw = JsonSerializer.Deserialize<SigilTrinketRawConfig>(json, options);
                if (raw != null)
                {
                    return true;
                }
            }
            catch
            {
                // skip and try next candidate
            }
        }

        return false;
    }

    // Ordered list of candidate paths to search for the config file.
    private static IEnumerable<string> GetCandidateConfigPaths()
    {
        // 1) Config under application base
        yield return Path.Combine(AppContext.BaseDirectory, "Config", "sigil_trinket_config.json");

        // 2) Top-level under base (sometimes published to base)
        yield return Path.Combine(AppContext.BaseDirectory, "sigil_trinket_config.json");

        // 3) Current working directory variants (useful in development / test runs)
        yield return Path.Combine(Directory.GetCurrentDirectory(), "Config", "sigil_trinket_config.json");
        yield return Path.Combine(Directory.GetCurrentDirectory(), "sigil_trinket_config.json");

        // 4) Walk up a few directory levels from base to find repo Config/ (covers build output under dist/.../net8.0)
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 6; i++)
        {
            dir = Path.GetDirectoryName(dir) ?? string.Empty;
            if (string.IsNullOrEmpty(dir))
            {
                break;
            }

            yield return Path.Combine(dir, "Config", "sigil_trinket_config.json");
            yield return Path.Combine(dir, "sigil_trinket_config.json");
        }

        // 5) As last resort, look relative to source layout (reasonable default during local development)
        var repoConfig = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Config", "sigil_trinket_config.json");
        yield return Path.GetFullPath(repoConfig);
    }

    // Remove // and /* */ comments while preserving strings.
    private static string StripJsonComments(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        var sb = new StringBuilder(input.Length);
        var inString = false;
        var inSingleLineComment = false;
        var inMultiLineComment = false;
        var prevWasBackslash = false;

        for (var i = 0; i < input.Length; i++)
        {
            var c = input[i];
            var next = i + 1 < input.Length ? input[i + 1] : '\0';

            if (inSingleLineComment)
            {
                if (c == '\r' || c == '\n')
                {
                    inSingleLineComment = false;
                    sb.Append(c);
                }
                else
                {
                    // skip comment char
                }
                continue;
            }

            if (inMultiLineComment)
            {
                if (c == '*' && next == '/')
                {
                    inMultiLineComment = false;
                    i++; // skip '/'
                }
                // skip everything inside multi-line comment
                continue;
            }

            if (inString)
            {
                sb.Append(c);
                if (c == '"' && !prevWasBackslash)
                {
                    inString = false;
                }

                if (c == '\\' && !prevWasBackslash)
                {
                    prevWasBackslash = true;
                }
                else
                {
                    prevWasBackslash = false;
                }

                continue;
            }

            // not in string or comment
            if (c == '/' && next == '/')
            {
                inSingleLineComment = true;
                i++; // skip next '/'
                continue;
            }

            if (c == '/' && next == '*')
            {
                inMultiLineComment = true;
                i++; // skip next '*'
                continue;
            }

            if (c == '"')
            {
                inString = true;
                prevWasBackslash = false;
                sb.Append(c);
                continue;
            }

            sb.Append(c);
        }

        return sb.ToString();
    }

    private static void MapToTyped(SigilTrinketRawConfig raw, string mapName, out IReadOnlyDictionary<int, SigilStatConfig> target)
    {
        target = new Dictionary<int, SigilStatConfig>();

        if (raw.Maps == null || !raw.Maps.TryGetValue(mapName, out var rawMap))
        {
            return;
        }

        var dict = new Dictionary<int, SigilStatConfig>();
        foreach (var kv in rawMap)
        {
            if (!int.TryParse(kv.Key, out var effectId))
            {
                continue;
            }

            var r = kv.Value;
            var useText = r.useText;
            
            if (string.IsNullOrEmpty(useText) && !string.IsNullOrEmpty(r.useTextBuilder))
            {
                useText = string.Empty;
            }

            dict[effectId] = new SigilStatConfig(
                r.paletteKey,
                r.iconColorKey,
                r.nameSuffix,
                useText ?? string.Empty,
                r.setIntensity,
                r.setReduction,
                r.setManaReservedZero,
                r.cooldownDelta,
                r.cooldownMultiplier,
                r.triggerChanceDelta,
                r.triggerChanceMultiplier,
                r.zeroTriggerChance
            );
        }

        target = dict;
    }
}
