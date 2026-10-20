using System;
using System.Collections.Generic;
using System.Linq;
using ACE.DatLoader;

namespace ACE.Server.Network;

/// <summary>
/// Tracks a single DAT patch (BeginDDD -> EndDDD) for a session, so incomplete or mismatched patches can be logged.
/// The client counts every DDD data byte it receives toward the BeginDDD total and ends patching once that total is reached,
/// so comparing what we advertised against what we actually sent is the main signal for a broken patch.
/// </summary>
public class DDDPatchProgress
{
    public DateTime StartTime { get; } = DateTime.UtcNow;

    public DateTime? EndTime { get; set; }

    public uint ExpectedIterations { get; }

    public uint ExpectedBytes { get; }

    public Dictionary<DatDatabaseType, int> ExpectedFiles { get; } = new();

    public Dictionary<DatDatabaseType, int> SentFiles { get; } = new();

    public long SentBytes { get; private set; }

    /// <summary>
    /// Files in the patch list that the server could not queue (not found in the server's DAT)
    /// </summary>
    public int UnqueuedFiles { get; set; }

    /// <summary>
    /// Files that could not be read from the server's DAT when it came time to send them
    /// </summary>
    public int FailedFiles { get; set; }

    public (uint DatFileId, DatDatabaseType DatDatabaseType)? LastFileSent { get; private set; }

    public DateTime LastFileSentTime { get; private set; }

    private int lastLoggedPercent;

    public bool ExpectedBytesReachedEarlyLogged { get; set; }

    public DDDPatchProgress(
        uint expectedIterations,
        uint expectedBytes,
        Dictionary<DatDatabaseType, Dictionary<uint, List<uint>>> missingIterations
    )
    {
        ExpectedIterations = expectedIterations;
        ExpectedBytes = expectedBytes;

        foreach (var dat in missingIterations)
        {
            ExpectedFiles[dat.Key] = dat.Value.Values.Sum(files => files.Count);
        }
    }

    public int TotalExpectedFiles => ExpectedFiles.Values.Sum();

    public int TotalSentFiles => SentFiles.Values.Sum();

    public bool IsComplete => EndTime != null;

    public void RecordSent(uint datFileId, DatDatabaseType datDatabaseType, int bytes)
    {
        SentFiles.TryGetValue(datDatabaseType, out var count);
        SentFiles[datDatabaseType] = count + 1;
        SentBytes += bytes;
        LastFileSent = (datFileId, datDatabaseType);
        LastFileSentTime = DateTime.UtcNow;
    }

    /// <summary>
    /// Returns the next 25% milestone reached by bytes sent, or null if no new milestone has been reached
    /// </summary>
    public int? TryGetNewProgressMilestone()
    {
        if (ExpectedBytes == 0)
        {
            return null;
        }

        var percent = (int)Math.Min(100, SentBytes * 100 / ExpectedBytes);
        var milestone = percent / 25 * 25;

        if (milestone <= lastLoggedPercent || milestone == 0)
        {
            return null;
        }

        lastLoggedPercent = milestone;
        return milestone;
    }

    public string Summary()
    {
        var elapsed = (EndTime ?? DateTime.UtcNow) - StartTime;

        var perDat = string.Join(
            ", ",
            ExpectedFiles
                .Select(e => $"{e.Key} {(SentFiles.TryGetValue(e.Key, out var sent) ? sent : 0):N0}/{e.Value:N0}")
        );

        var summary =
            $"elapsed {elapsed.TotalSeconds:N0}s | files sent {TotalSentFiles:N0}/{TotalExpectedFiles:N0} ({perDat}) | bytes sent {SentBytes:N0}/{ExpectedBytes:N0}";

        if (UnqueuedFiles > 0)
        {
            summary += $" | {UnqueuedFiles:N0} files missing from server DAT";
        }

        if (FailedFiles > 0)
        {
            summary += $" | {FailedFiles:N0} files failed to read";
        }

        if (LastFileSent is { } last)
        {
            summary +=
                $" | last file sent 0x{last.DatFileId:X8} ({last.DatDatabaseType}) {(DateTime.UtcNow - LastFileSentTime).TotalSeconds:N0}s ago";
        }

        return summary;
    }
}
