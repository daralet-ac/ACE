using System;
using System.Text;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.AdvocateCommands.Stats;

public class GcStatus
{
    // gcstatus
    [CommandHandler(
        "gcstatus",
        AccessLevel.Advocate,
        CommandHandlerFlag.None,
        0,
        "Displays a summary of server GC Information"
    )]
    public static void HandleGCStatus(Session session, params string[] parameters)
    {
        var sb = new StringBuilder();

        sb.Append(
            $"GC.GetTotalMemory: {(GC.GetTotalMemory(false) >> 20):N0} MB, GC.GetTotalAllocatedBytes: {(GC.GetTotalAllocatedBytes() >> 20):N0} MB{'\n'}"
        );

        // https://docs.microsoft.com/en-us/dotnet/api/system.gcmemoryinfo?view=net-5.0
        var gcmi = GC.GetGCMemoryInfo();

        sb.Append(
            $"GCMI Index: {gcmi.Index:N0}, Generation: {gcmi.Generation}, Compacted: {gcmi.Compacted}, Concurrent: {gcmi.Concurrent}, PauseTimePercentage: {gcmi.PauseTimePercentage}{'\n'}"
        );
        for (var i = 0; i < gcmi.GenerationInfo.Length; i++)
        {
            sb.Append(
                $"GCMI.GenerationInfo[{i}] FragmentationBeforeBytes: {(gcmi.GenerationInfo[i].FragmentationBeforeBytes >> 20):N0} MB, FragmentationAfterBytes: {(gcmi.GenerationInfo[i].FragmentationAfterBytes >> 20):N0} MB, SizeBeforeBytes: {(gcmi.GenerationInfo[i].SizeBeforeBytes >> 20):N0} MB, SizeAfterBytes: {(gcmi.GenerationInfo[i].SizeAfterBytes >> 20):N0} MB{'\n'}"
            );
        }

        for (var i = 0; i < gcmi.PauseDurations.Length; i++)
        {
            sb.Append($"GCMI.PauseDurations[{i}]: {gcmi.PauseDurations[i].TotalMilliseconds:N0} ms{'\n'}");
        }

        sb.Append(
            $"GCMI PinnedObjectsCount: {gcmi.PinnedObjectsCount}, FinalizationPendingCount: {gcmi.FinalizationPendingCount:N0}{'\n'}"
        );

        sb.Append(
            $"GCMI FragmentedBytes: {(gcmi.FragmentedBytes >> 20):N0} MB, PromotedBytes: {(gcmi.PromotedBytes >> 20):N0} MB, HeapSizeBytes: {(gcmi.HeapSizeBytes >> 20):N0} MB, TotalCommittedBytes: {(gcmi.TotalCommittedBytes >> 20):N0} MB{'\n'}"
        );
        sb.Append(
            $"GCMI MemoryLoadBytes: {(gcmi.MemoryLoadBytes >> 20):N0} MB, HighMemoryLoadThresholdBytes: {(gcmi.HighMemoryLoadThresholdBytes >> 20):N0} MB, TotalAvailableMemoryBytes: {(gcmi.TotalAvailableMemoryBytes >> 20):N0} MB{'\n'}"
        );

        CommandHandlerHelper.WriteOutputInfo(session, sb.ToString());
    }
}
