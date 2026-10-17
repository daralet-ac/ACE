using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ACE.Database;
using ACE.Database.Models.Shard;
using ACE.Server.Entity;
using ACE.Server.WorldObjects;
using Serilog;

namespace ACE.Server.Managers;

/// <summary>
/// When a fellowship's clock on a capstone dungeon started: the dungeon (as AssignCapstoneDungeon names it) and the time
/// </summary>
public sealed class CapstoneRunState
{
    public CapstoneRunState(string dungeon, DateTime startedUtc)
    {
        Dungeon = dungeon;
        StartedUtc = startedUtc;
    }

    public string Dungeon { get; }

    public DateTime StartedUtc { get; }
}

/// <summary>
/// Times the capstone dungeons for the website's capstone leaderboard (capstone_run, /api/leaderboards/capstone).
///
/// The clock starts when a fellowship's instance of a dungeon is created (Landblock.AssignInstancedCapstoneDungeon), or when it is
/// given one of the numbered copies (FindOpenInstanceFellowship), and stops when the dungeon's completion quest is stamped
/// (EmoteManager, StampQuest). It is kept on the fellowship (Fellowship.CapstoneRun), so it ends with the fellowship, and a
/// server restart in the middle of a run means the run isn't recorded. A run that is recorded is used up: the next one needs
/// the dungeon to be entered again.
///
/// The roster is the whole fellowship when the quest is stamped, whoever is in the dungeon at that moment. Nothing yet stops
/// someone from joining at the last minute or leaving a fellowship early; that is to be dealt with later.
/// </summary>
public static class CapstoneRunTracker
{
    private static readonly ILogger _log = Log.ForContext(typeof(CapstoneRunTracker));

    /// <summary>
    /// The dungeons that have a leaderboard: the dungeon as AssignCapstoneDungeon names it, and its key on the website, which is
    /// its completion quest (QuestManager.CapstoneCompletionQuests) without "Completed". The names differ for Colier Mine.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> DungeonKeys = new Dictionary<string, string>(
        StringComparer.OrdinalIgnoreCase
    )
    {
        ["Glenden Wood Dungeon"] = "GlendenWoodDungeon",
        ["Green Mire Grave"] = "GreenMireGrave",
        ["Sand Shallow"] = "SandShallow",
        ["Manse of Panderlou"] = "ManseOfPanderlou",
        ["Smugglers Hideaway"] = "SmugglersHideaway",
        ["Halls of the Helm"] = "HallsOfTheHelm",
        ["Colier Mine"] = "MinesOfColier",
        ["Empyrean Garrison"] = "EmpyreanGarrison",
        ["Grievous Vault"] = "GrievousVault",
        ["Folthid Cellar"] = "FolthidCellar",
        ["Mines of Despair"] = "MinesOfDespair",
        ["Gredaline Consulate"] = "GredalineConsulate",
        ["Mage Academy"] = "MageAcademy",
        ["Lugian Mines"] = "LugianMines",
        ["Mountain Fortress"] = "MountainFortress",
    };

    /// <summary>
    /// Some capstone dungeons are in two parts, and the second part is a dungeon of its own: it has its own name in
    /// AssignCapstoneDungeon and its own landblocks, and (when instanced) its own instance, made when the fellowship goes
    /// through to it. It is the same run, though, so reaching it doesn't start a clock: the run goes on from the first part.
    /// The key is the second part, the value the first.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> SecondParts = new Dictionary<string, string>(
        StringComparer.OrdinalIgnoreCase
    )
    {
        ["Lugian Mines2"] = "Lugian Mines",
        ["Beyond the Mines"] = "Mines of Despair",
    };

    /// <summary>
    /// The clock for a fellowship that has just got a dungeon: null for a second part, and for a dungeon with no leaderboard
    /// (the Olthoi Queen's Lair raid), which don't start one
    /// </summary>
    internal static CapstoneRunState Begin(string dungeonName, DateTime nowUtc)
    {
        if (dungeonName == null || SecondParts.ContainsKey(dungeonName) || !DungeonKeys.ContainsKey(dungeonName))
        {
            return null;
        }

        return new CapstoneRunState(dungeonName, nowUtc);
    }

    /// <summary>
    /// Starts the fellowship's clock on a dungeon it has just been given (an instance made, or a copy taken). A dungeon that
    /// doesn't start one (a second part) leaves the clock the fellowship already has, and a first part replaces whatever it had.
    /// </summary>
    public static void StartRun(Fellowship fellowship, string dungeonName)
    {
        var state = Begin(dungeonName, DateTime.UtcNow);

        if (fellowship != null && state != null)
        {
            Volatile.Write(ref fellowship.CapstoneRun, state);
        }
    }

    /// <summary>
    /// Whether this completion quest ("MinesOfColierCompleted") is the one that ends the run
    /// </summary>
    internal static bool IsCompletionOf(CapstoneRunState state, string questName)
    {
        return state != null
            && questName != null
            && DungeonKeys.TryGetValue(state.Dungeon, out var key)
            && string.Equals(questName, key + "Completed", StringComparison.Ordinal);
    }

    /// <summary>
    /// The row for a run that has finished. Seconds are rounded to the nearest and are never less than 1.
    /// </summary>
    internal static CapstoneRun BuildRun(
        CapstoneRunState state,
        DateTime completedUtc,
        int difficultyTier,
        IReadOnlyCollection<(uint Id, string Name)> roster
    )
    {
        var members = roster.OrderBy(m => m.Id).ToList();
        var seconds = Math.Max(1, (long)Math.Round((completedUtc - state.StartedUtc).TotalSeconds));

        return new CapstoneRun
        {
            Dungeon = DungeonKeys[state.Dungeon],
            FellowshipSize = (byte)Math.Min(members.Count, byte.MaxValue),
            DifficultyTier = (byte)Math.Clamp(difficultyTier, 0, 20),
            CompletionSeconds = (uint)Math.Min(seconds, uint.MaxValue),
            RosterKey = string.Join(",", members.Select(m => m.Id)),
            CompletedAtUtc = completedUtc,
            Members = members
                .Select(m => new CapstoneRunMember { CharacterId = m.Id, CharacterName = m.Name })
                .ToList(),
        };
    }

    // A missing table (its update script not applied yet) would fail every run, so that is reported once.
    private static int _writeFailureReported;

    /// <summary>
    /// A completion quest was stamped on player: if it ends the run their fellowship has, records it. Called from the
    /// StampQuest emote for every capstone completion quest, so it does nothing for a fellowship that has no run
    /// (it didn't enter the dungeon since the server started, or the run has already been recorded).
    /// </summary>
    public static void OnCompletionQuest(Player player, string questName)
    {
        var fellowship = player?.Fellowship;

        if (fellowship == null)
        {
            return;
        }

        var state = Volatile.Read(ref fellowship.CapstoneRun);

        if (!IsCompletionOf(state, questName))
        {
            return;
        }

        // Every member's emote can stamp this, on different landblocks' threads: whoever gets here first records it
        if (Interlocked.CompareExchange(ref fellowship.CapstoneRun, null, state) != state)
        {
            return;
        }

        var roster = fellowship
            .GetFellowshipMembers()
            .Values.Select(p => (p.Guid.Full, p.Name))
            .ToList();

        if (roster.Count == 0)
        {
            return;
        }

        var run = BuildRun(state, DateTime.UtcNow, player.CurrentLandblock?.GetDifficultyTier() ?? 0, roster);

        _log.Information(
            "[CAPSTONE] {Dungeon} completed in {Seconds}s by {Size}: {Names} (difficulty {Tier})",
            run.Dungeon,
            run.CompletionSeconds,
            run.FellowshipSize,
            string.Join(", ", run.Members.Select(m => m.CharacterName)),
            run.DifficultyTier
        );

        // the database work runs off the world thread
        _ = Task.Run(() =>
        {
            try
            {
                DatabaseManager.Shard.BaseDatabase.AddCapstoneRun(run);
            }
            catch (Exception ex)
            {
                if (Interlocked.Exchange(ref _writeFailureReported, 1) == 0)
                {
                    _log.Error(
                        ex,
                        "[CAPSTONE] Couldn't save to capstone_run; the capstone leaderboard stays empty until it can. Has the 2026-10-16-00-Capstone-Runs.sql update run?"
                    );
                }
            }
        });
    }
}
