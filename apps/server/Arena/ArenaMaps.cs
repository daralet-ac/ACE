using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ACE.Common;
using ACE.Server.Managers;
using Serilog;

namespace ACE.Server.Arena;

/// <summary>
/// The arena maps of arenas.json, which is next to the server. It is read once, when the server starts.
/// Every map's template is registered with the InstanceManager, so admins can look at a map with /instance open arena:&lt;name&gt;.
/// </summary>
public static class ArenaMaps
{
    private static readonly ILogger _log = Log.ForContext(typeof(ArenaMaps));

    private static volatile IReadOnlyList<ArenaMap> maps = Array.Empty<ArenaMap>();

    public static IReadOnlyList<ArenaMap> All => maps;

    public static IReadOnlyList<ArenaMap> Enabled => maps.Where(m => m.Enabled).ToList();

    public static ArenaMap Get(string name) =>
        maps.FirstOrDefault(m => string.Equals(m.Name, name, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// One of the enabled maps, picked at random. Null if there is none.
    /// </summary>
    public static ArenaMap PickRandom()
    {
        var enabled = Enabled;
        return enabled.Count == 0 ? null : enabled[ThreadSafeRandom.Next(0, enabled.Count - 1)];
    }

    public static void Load(string path = null)
    {
        path ??= Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "arenas.json");

        if (!File.Exists(path))
        {
            _log.Warning("[ARENA] There is no {Path}, so there are no arena maps and nobody can duel", path);
            return;
        }

        var errors = new List<string>();
        List<ArenaMap> loaded;

        try
        {
            loaded = ArenaMapConfig.Parse(File.ReadAllText(path), errors);
        }
        catch (Exception ex)
        {
            _log.Error(ex, "[ARENA] {Path} can't be read, so there are no arena maps", path);
            return;
        }

        foreach (var error in errors)
        {
            _log.Error("[ARENA] {Path}: {Error}", path, error);
        }

        Register(loaded);
    }

    internal static void Register(IReadOnlyList<ArenaMap> loaded)
    {
        foreach (var map in loaded)
        {
            InstanceManager.RegisterTemplate(map.Template);

            _log.Information(
                "[ARENA] Map {Map}: {Landblocks} landblock(s), {Starts} starts{Radius}{Enabled}",
                map.Name,
                map.Template.Footprint.Count,
                map.Starts.Count,
                map.HasRadius ? $", radius {map.Radius:N0}m" : "",
                map.Enabled ? "" : ", not enabled"
            );
        }

        maps = loaded;

        if (!loaded.Any(m => m.Enabled))
        {
            _log.Warning("[ARENA] No arena map is enabled, so nobody can duel");
        }
    }
}
