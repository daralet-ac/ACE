using System;
using System.Collections.Generic;
using System.Linq;
using ACE.Entity;
using ACE.Server.Managers;
using ACE.Server.WorldObjects;

namespace ACE.Server.Entity.AdminUi;

/// <summary>
/// A fellowship's capstone run that is going on: where it is (an instance, or one of the dungeon's numbered copies), how long
/// its clock has been running, and its members, with the guids of the ones inside the dungeon right now.
/// </summary>
public record AdminUiCapstoneRun(
    string Dungeon,
    string Fellowship,
    string Leader,
    string Where,
    uint? InstanceId,
    double? RunSeconds,
    int DifficultyTier,
    List<string> Mods,
    List<AdminUiFellow> Members,
    List<uint> Inside
);

/// <summary>
/// A finished run from the capstone leaderboard
/// </summary>
public record AdminUiCapstoneCompletion(
    string Dungeon,
    double SecondsAgo,
    uint CompletionSeconds,
    int DifficultyTier,
    List<string> Members
);

public record AdminUiCapstoneReport(List<AdminUiCapstoneRun> Active, List<AdminUiCapstoneCompletion> Recent);

public static class AdminUiCapstones
{
    private static readonly Dictionary<string, string> dungeonNamesByKey = CapstoneRunTracker.DungeonKeys.ToDictionary(
        kv => kv.Value,
        kv => kv.Key
    );

    public static AdminUiCapstoneReport Get()
    {
        return new AdminUiCapstoneReport(GetActive(), GetRecent());
    }

    private static List<AdminUiCapstoneRun> GetActive()
    {
        var now = DateTime.UtcNow;
        var results = new List<AdminUiCapstoneRun>();
        var seen = new HashSet<Fellowship>();

        var capstoneInstances = InstanceManager
            .GetInstances()
            .Where(i => i.Template.Name.StartsWith(InstanceTemplateConfig.ReservedPrefix, StringComparison.Ordinal))
            .ToList();

        foreach (var player in PlayerManager.GetAllOnline())
        {
            var fellowship = player.Fellowship;

            if (fellowship?.CapstoneDungeon == null || !seen.Add(fellowship))
            {
                continue;
            }

            var dungeonLandblock = fellowship.CapstoneDungeon.Value;
            var run = fellowship.CapstoneRun;
            var instance = capstoneInstances.FirstOrDefault(i => i.Owner == fellowship && !i.IsClosing);
            var instanceId = instance?.Id ?? Landblock.PersistentInstance;
            var members = fellowship.GetFellowshipMembers().Values.OrderBy(m => m.Name).ToList();

            var inside = members
                .Where(m =>
                    m.InstanceId == instanceId && m.Location.LandblockId.Landblock == dungeonLandblock.Landblock
                )
                .Select(m => m.Guid.Full)
                .ToList();

            // a fellowship keeps its dungeon after the run, so one that's finished and left isn't listed
            if (run == null && inside.Count == 0)
            {
                continue;
            }

            var dungeon =
                run?.Dungeon
                ?? instance?.Template.Name.Substring(InstanceTemplateConfig.ReservedPrefix.Length)
                ?? FindDungeonName(dungeonLandblock);

            var landblock = LandblockManager.TryGetLandblock(dungeonLandblock, instanceId);

            results.Add(
                new AdminUiCapstoneRun(
                    dungeon ?? $"Landblock 0x{dungeonLandblock.Landblock:X4}",
                    fellowship.FellowshipName,
                    PlayerManager.FindByGuid(fellowship.FellowshipLeaderGuid)?.Name,
                    instance != null ? $"instance {instance.Id}" : $"copy 0x{dungeonLandblock.Landblock:X4}",
                    instance?.Id,
                    run != null ? (now - run.StartedUtc).TotalSeconds : null,
                    landblock?.GetDifficultyTier() ?? 0,
                    fellowship.CapstoneDungeonMods?.ToList() ?? new List<string>(),
                    members.Select(AdminUiSnapshots.GetFellow).ToList(),
                    inside
                )
            );
        }

        return results.OrderBy(r => r.Dungeon).ThenBy(r => r.Fellowship).ToList();
    }

    /// <summary>
    /// The capstone dungeon one of whose copies this is, for a fellowship whose clock has already stopped
    /// </summary>
    private static string FindDungeonName(LandblockId landblock)
    {
        foreach (var name in CapstoneRunTracker.DungeonKeys.Keys.Concat(CapstoneRunTracker.SecondParts.Keys))
        {
            if (Landblock.CapstoneDungeonLists(name)?.Contains(landblock) ?? false)
            {
                return name;
            }
        }

        return null;
    }

    private static List<AdminUiCapstoneCompletion> GetRecent()
    {
        var now = DateTime.UtcNow;

        return CapstoneRunTracker
            .GetRecentRuns()
            .Select(r => new AdminUiCapstoneCompletion(
                dungeonNamesByKey.TryGetValue(r.Dungeon, out var name) ? name : r.Dungeon,
                (now - DateTime.SpecifyKind(r.CompletedAtUtc, DateTimeKind.Utc)).TotalSeconds,
                r.CompletionSeconds,
                r.DifficultyTier,
                r.Members.Select(m => m.CharacterName).OrderBy(n => n).ToList()
            ))
            .ToList();
    }
}
