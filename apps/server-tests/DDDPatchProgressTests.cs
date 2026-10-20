using System.Collections.Generic;
using ACE.DatLoader;
using ACE.Server.Managers;
using ACE.Server.Network;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ACE.Server.Tests;

[TestClass]
public class DDDPatchProgressTests
{
    private static Dictionary<DatDatabaseType, Dictionary<uint, List<uint>>> MissingIterations() =>
        new()
        {
            [DatDatabaseType.Portal] = new()
            {
                [5] = new() { 0x01000001, 0x01000002 },
                [6] = new() { 0x01000003 },
                [8] = new() { 0x01000004 },
            },
            [DatDatabaseType.Cell] = new() { [7] = new() { 0x0101FFFF } },
        };

    [TestMethod]
    public void CountsExpectedFilesPerDat()
    {
        var patch = new DDDPatchProgress(4, 400, MissingIterations());

        Assert.AreEqual(4, patch.ExpectedFiles[DatDatabaseType.Portal]);
        Assert.AreEqual(1, patch.ExpectedFiles[DatDatabaseType.Cell]);
        Assert.AreEqual(5, patch.TotalExpectedFiles);
    }

    [TestMethod]
    public void ReportsEachProgressMilestoneOnce()
    {
        var patch = new DDDPatchProgress(4, 400, MissingIterations());

        patch.RecordSent(0x01000001, DatDatabaseType.Portal, 50);
        Assert.IsNull(patch.TryGetNewProgressMilestone());

        patch.RecordSent(0x01000002, DatDatabaseType.Portal, 60);
        Assert.AreEqual(25, patch.TryGetNewProgressMilestone());
        Assert.IsNull(patch.TryGetNewProgressMilestone());

        patch.RecordSent(0x01000003, DatDatabaseType.Portal, 290);
        Assert.AreEqual(100, patch.TryGetNewProgressMilestone());

        Assert.AreEqual(3, patch.TotalSentFiles);
        Assert.AreEqual(400, patch.SentBytes);
    }

    [TestMethod]
    public void SummaryShowsSentAgainstExpected()
    {
        var patch = new DDDPatchProgress(4, 400, MissingIterations());
        patch.RecordSent(0x01000001, DatDatabaseType.Portal, 100);
        patch.FailedFiles = 1;

        var summary = patch.Summary();

        StringAssert.Contains(summary, "files sent 1/5");
        StringAssert.Contains(summary, "Portal 1/4");
        StringAssert.Contains(summary, "Cell 0/1");
        StringAssert.Contains(summary, "bytes sent 100/400");
        StringAssert.Contains(summary, "1 files failed to read");
        StringAssert.Contains(summary, "last file sent 0x01000001 (Portal)");
    }

    [TestMethod]
    public void DescribeMissingIterationsCollapsesRanges()
    {
        var description = DDDManager.DescribeMissingIterations(MissingIterations());

        Assert.AreEqual("Portal iterations 5-6,8 (4 files) | Cell iterations 7 (1 files)", description);
    }
}
