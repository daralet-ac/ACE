using System.Linq;
using System.Text;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Managers;
using ACE.Server.Network;

namespace ACE.Server.Commands.DeveloperCommands.Stats;

public class LandblockGroupStats
{
    // lbgroupstats
    [CommandHandler(
        "lbgroupstats",
        AccessLevel.Developer,
        CommandHandlerFlag.None,
        0,
        "Displays a summary of landblock group stats"
    )]
    public static void HandleLBGroupStats(Session session, params string[] parameters)
    {
        var sb = new StringBuilder();

        sb.Append(
            $"TickPhysicsEfficiencyTracker: {LandblockManager.TickPhysicsEfficiencyTracker.AverageAmount, 3:N0} %, TickMultiThreadedWorkEfficiencyTracker: {LandblockManager.TickMultiThreadedWorkEfficiencyTracker.AverageAmount, 3:N0} %{'\n'}"
        );

        var loadedLanblockGroups = LandblockManager.GetLoadedLandblockGroups();

        var sortedByLargest = loadedLanblockGroups.OrderByDescending(r => r.Count).Take(5);

        sb.Append($"Largest Landblock Groups{'\n'}");
        sb.Append(
            $"Cnt, XMin - XMax, YMin - YMax, TickPhysicsTracker avg   max, TickMultiThreadedWorkTracker avg   max (s){'\n'}"
        );

        foreach (var landblockGroup in sortedByLargest)
        {
            sb.Append(
                $"{landblockGroup.Count, 3},   {landblockGroup.XMin, 2:X2} - {landblockGroup.XMax, 2:X2},     {landblockGroup.YMin, 2:X2} - {landblockGroup.YMax, 2:X2}  ,                  {landblockGroup.TickPhysicsTracker.AverageAmount, 5:N3} {landblockGroup.TickPhysicsTracker.LargestAmount, 5:N3},                            {landblockGroup.TickMultiThreadedWorkTracker.AverageAmount, 5:N3} {landblockGroup.TickMultiThreadedWorkTracker.LargestAmount, 5:N3}{'\n'}"
            );
        }

        var sortedByTopTickPhysicsTracker = loadedLanblockGroups
            .OrderByDescending(r => r.TickPhysicsTracker.AverageAmount)
            .Take(5);

        sb.Append($"Top TickPhysicsTracker Landblock Groups{'\n'}");

        foreach (var landblockGroup in sortedByTopTickPhysicsTracker)
        {
            sb.Append(
                $"{landblockGroup.Count, 3},   {landblockGroup.XMin, 2:X2} - {landblockGroup.XMax, 2:X2},     {landblockGroup.YMin, 2:X2} - {landblockGroup.YMax, 2:X2}  ,                  {landblockGroup.TickPhysicsTracker.AverageAmount, 5:N3} {landblockGroup.TickPhysicsTracker.LargestAmount, 5:N3},                            {landblockGroup.TickMultiThreadedWorkTracker.AverageAmount, 5:N3} {landblockGroup.TickMultiThreadedWorkTracker.LargestAmount, 5:N3}{'\n'}"
            );
        }

        var sortedByTopTickMultiThreadedWorkTracker = loadedLanblockGroups
            .OrderByDescending(r => r.TickMultiThreadedWorkTracker.AverageAmount)
            .Take(5);

        sb.Append($"Top TickMultiThreadedWorkTracker Landblock Groups{'\n'}");

        foreach (var landblockGroup in sortedByTopTickMultiThreadedWorkTracker)
        {
            sb.Append(
                $"{landblockGroup.Count, 3},   {landblockGroup.XMin, 2:X2} - {landblockGroup.XMax, 2:X2},     {landblockGroup.YMin, 2:X2} - {landblockGroup.YMax, 2:X2}  ,                  {landblockGroup.TickPhysicsTracker.AverageAmount, 5:N3} {landblockGroup.TickPhysicsTracker.LargestAmount, 5:N3},                            {landblockGroup.TickMultiThreadedWorkTracker.AverageAmount, 5:N3} {landblockGroup.TickMultiThreadedWorkTracker.LargestAmount, 5:N3}{'\n'}"
            );
        }

        CommandHandlerHelper.WriteOutputInfo(session, sb.ToString());
    }
}
