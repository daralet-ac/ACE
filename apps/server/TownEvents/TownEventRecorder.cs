using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ACE.Database;
using ACE.Database.Models.Shard;
using ACE.Server.Entity;
using ACE.Server.Managers;
using Serilog;

namespace ACE.Server.TownEvents;

/// <summary>
/// The town event log (town_event, town_event_participant): every attack once it ends, with where and when, how it went,
/// and every player's scores, ranks and rewards. A reward waiting at the Captain is updated when it's claimed or runs out.
/// The database work runs off the world thread.
/// </summary>
public static class TownEventRecorder
{
    private static readonly ILogger _log = Log.ForContext(typeof(TownEventRecorder));

    private static int _writeFailureReported;

    /// <summary>
    /// Saves the attack. Call from TownEvent.Finish, on the world thread, before the Town Defense disbands.
    /// </summary>
    public static void Record(TownEvent townEvent)
    {
        TownEventLog log;
        try
        {
            log = Build(townEvent);
        }
        catch (Exception ex)
        {
            _log.Error(ex, "[TOWN EVENT] {Town}: couldn't build the log entry", townEvent.TownName);
            return;
        }

        _log.Information(
            "[TOWN EVENT] {Town}: logged, {Outcome} after {Seconds}s, wave {Wave}/{Waves}, {Participants} took part, {Rewarded} rewarded",
            log.Town,
            log.Outcome,
            log.DurationSeconds,
            log.WavesReached,
            log.WaveCount,
            log.Participants,
            log.Rewarded
        );

        Save(() => DatabaseManager.Shard.BaseDatabase.AddTownEventLog(log));
    }

    /// <summary>
    /// A reward waiting at the Captain was claimed (with its XP) or ran out
    /// </summary>
    public static void RecordClaim(string eventUid, uint characterId, string reward, long xp)
    {
        if (eventUid == null)
        {
            return;
        }

        var claimedAt = reward == "claimed" ? DateTime.UtcNow : (DateTime?)null;

        Save(() => DatabaseManager.Shard.BaseDatabase.UpdateTownEventReward(eventUid, characterId, reward, (ulong)Math.Max(0, xp), claimedAt));
    }

    private static void Save(Action write)
    {
        _ = Task.Run(() =>
        {
            try
            {
                write();
            }
            catch (Exception ex)
            {
                if (Interlocked.Exchange(ref _writeFailureReported, 1) == 0)
                {
                    _log.Error(ex, "[TOWN EVENT] Couldn't save to town_event; the log stays empty until it can. Has the 2026-10-26-00-Town-Event-Log.sql update run?");
                }
            }
        });
    }

    private static TownEventLog Build(TownEvent e)
    {
        var endedAt = DateTime.UtcNow;
        var duration = Math.Max(0, e.EndTime - e.StartTime);

        // fake players (@fakeplayer) are left out of the log
        var scores = e.Scores.Values.Where(s => !FakePlayerManager.WasFake(s.PlayerGuid)).ToList();
        var credited = scores.Where(s => s.HasContributed && !s.CreditLost).ToList();

        var log = new TownEventLog
        {
            Uid = e.Uid,
            Town = e.TownName,
            Tier = (byte)Town.GetTownTier(e.TownName),
            StartedBy = e.StartedBy,
            Outcome = e.Outcome ?? "stopped",
            Family = e.Family,
            MinorFamily = e.MinorFamilyName,
            Roads = e.Roads,
            ChestRaid = e.ChestRaid,
            ChestLootedPercent = (byte)Math.Round(e.ChestLooted * 100),
            SupplyLevelStart = (byte)e.SupplyLevelStart,
            SupplyLevelEnd = (byte)TownSupply.GetLevel(e.TownName),
            WavesReached = (byte)e.Wave,
            WaveCount = (byte)e.WaveCount,
            AttackersSpawned = (ushort)Math.Min(ushort.MaxValue, e.AttackersSpawned),
            Participants = (ushort)credited.Count,
            Rewarded = (ushort)scores.Count(s => s.Reward != "none"),
            WarlordPyreals = (uint)e.WarlordPyreals,
            WarlordPaid = (ushort)e.WarlordPaid,
            StartedAtUtc = endedAt.AddSeconds(-duration),
            EndedAtUtc = endedAt,
            DurationSeconds = (uint)Math.Round(duration),
        };

        var damageRanks = Ranks(credited, s => s.Damage);
        var healingRanks = Ranks(credited, s => s.Healing);
        var takenRanks = Ranks(credited, s => s.DamageTaken);
        var killRanks = Ranks(credited, s => s.KillingBlows);

        foreach (var score in scores)
        {
            log.Participations.Add(
                new TownEventLogParticipant
                {
                    CharacterId = score.PlayerGuid,
                    CharacterName = score.Name ?? "",
                    Level = (ushort)Math.Max(0, score.Level),
                    MemberAtEnd = e.Fellowship != null && e.Fellowship.FellowshipMembers.ContainsKey(score.PlayerGuid),
                    Damage = (uint)Math.Round(score.Damage),
                    Healing = (uint)Math.Round(score.Healing),
                    DamageTaken = (uint)Math.Round(score.DamageTaken),
                    KillingBlows = (ushort)score.KillingBlows,
                    RankDamage = damageRanks.GetValueOrDefault(score),
                    RankHealing = healingRanks.GetValueOrDefault(score),
                    RankDamageTaken = takenRanks.GetValueOrDefault(score),
                    RankKillingBlows = killRanks.GetValueOrDefault(score),
                    CategoriesTopped = (byte)score.CategoriesTopped,
                    OverLevelKills = (ushort)score.OverLevelKills,
                    PromptsRefused = (ushort)score.PromptsRefused,
                    KillsAfterRefusal = (ushort)score.KillsAfterRefusal,
                    CreditLost = score.CreditLost,
                    Reward = score.Reward,
                    RewardMod = Math.Round(score.RewardMod, 4),
                    Xp = (ulong)Math.Max(0, score.Xp),
                    WarlordPyreals = (uint)score.WarlordPyreals,
                }
            );
        }

        return log;
    }

    /// <summary>
    /// Each player's place in a score, 1 for the top; players level on it share a place. Those with none aren't ranked.
    /// </summary>
    public static Dictionary<TownDefenseScore, ushort?> Ranks(List<TownDefenseScore> scores, Func<TownDefenseScore, double> value)
    {
        var ranks = new Dictionary<TownDefenseScore, ushort?>();

        var ordered = scores.Where(s => value(s) > 0).OrderByDescending(value).ToList();
        for (var i = 0; i < ordered.Count; i++)
        {
            var tied = i > 0 && value(ordered[i]) == value(ordered[i - 1]);
            ranks[ordered[i]] = tied ? ranks[ordered[i - 1]] : (ushort)(i + 1);
        }

        return ranks;
    }

    /// <summary>
    /// @townevent history: the latest attacks, newest first, for one town or all. Reads the database, so call it off the
    /// world thread.
    /// </summary>
    public static string DescribeHistory(int count, string town)
    {
        List<TownEventLog> logs;
        try
        {
            logs = DatabaseManager.Shard.BaseDatabase.GetRecentTownEventLogs(count, town);
        }
        catch (Exception ex)
        {
            _log.Error(ex, "[TOWN EVENT] Couldn't read town_event");
            return "Couldn't read the town event log. Has the 2026-10-26-00-Town-Event-Log.sql update run?";
        }

        if (logs.Count == 0)
        {
            return town == null ? "No town attacks have been logged yet." : $"No attacks on {town} have been logged yet.";
        }

        var lines = new List<string>();
        foreach (var log in logs)
        {
            var top = log.Participations.Where(p => p.RankDamage == 1).Select(p => p.CharacterName).FirstOrDefault();

            lines.Add(
                $"{log.EndedAtUtc:yyyy-MM-dd HH:mm} UTC  {log.Town}: {log.Outcome} in {log.DurationSeconds / 60}m{log.DurationSeconds % 60:00}s, "
                    + $"wave {log.WavesReached}/{log.WaveCount}, {log.Family}{(log.ChestRaid ? $", chest raid ({log.ChestLootedPercent}% looted)" : "")}, "
                    + $"supply {log.SupplyLevelStart}->{log.SupplyLevelEnd}, {log.Participants} took part, {log.Rewarded} rewarded"
                    + (log.WarlordPaid > 0 ? $", Warlord paid {log.WarlordPyreals:N0} to {log.WarlordPaid}" : "")
                    + (top != null ? $", top damage {top}" : "")
                    + $" ({log.StartedBy})"
            );
        }

        return string.Join("\n", lines);
    }
}
