using System;
using System.Linq;
using System.Text;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Managers;
using ACE.Server.Network;
using ACE.Server.WorldObjects;

namespace ACE.Server.Commands.AdvocateCommands.Stats;

public class LandblockStats
{
    [CommandHandler(
        "landblockperformance",
        AccessLevel.Advocate,
        CommandHandlerFlag.None,
        0,
        "Displays a summary of landblock performance statistics"
    )]
    [CommandHandler(
        "landblockstats",
        AccessLevel.Advocate,
        CommandHandlerFlag.None,
        0,
        "Displays a summary of landblock performance statistics"
    )]
    public static void HandleLandblockStats(Session session, params string[] parameters)
    {
        var sb = new StringBuilder();

        var loadedLandblocks = LandblockManager.GetLoadedLandblocks();

        // Filter out landblocks that haven't recorded a certain amount of events
        var sortedBy5mAverage = loadedLandblocks
            .Where(r => r.Monitor5m.EventHistory.TotalEvents >= 10)
            .OrderByDescending(r => r.Monitor5m.EventHistory.AverageEventDuration)
            .Take(10)
            .ToList();
        var sortedBy1hrAverage = loadedLandblocks
            .Where(r => r.Monitor1h.EventHistory.TotalEvents >= 1000)
            .OrderByDescending(r => r.Monitor1h.EventHistory.AverageEventDuration)
            .Take(10)
            .ToList();

        var combinedByAverage = sortedBy5mAverage
            .Concat(sortedBy1hrAverage)
            .Distinct()
            .OrderByDescending(r =>
                Math.Max(r.Monitor5m.EventHistory.AverageEventDuration, r.Monitor1h.EventHistory.AverageEventDuration)
            )
            .Take(10);

        sb.Append($"Most Busy Landblock - By Average{'\n'}");
        sb.Append($"~5m Hits   Avg  Long  Last - ~1h Hits   Avg  Long  Last - Location   Players  Creatures{'\n'}");

        foreach (var entry in combinedByAverage)
        {
            int players = 0,
                creatures = 0;
            foreach (var worldObject in entry.GetAllWorldObjectsForDiagnostics())
            {
                if (worldObject is Player)
                {
                    players++;
                }
                else if (worldObject is Creature)
                {
                    creatures++;
                }
            }

            sb.Append(
                $"{entry.Monitor5m.EventHistory.TotalEvents.ToString().PadLeft(7)} {entry.Monitor5m.EventHistory.AverageEventDuration:N4} {entry.Monitor5m.EventHistory.LongestEvent:N3} {entry.Monitor5m.EventHistory.LastEvent:N3} - "
                    + $"{entry.Monitor1h.EventHistory.TotalEvents.ToString().PadLeft(7)} {entry.Monitor1h.EventHistory.AverageEventDuration:N4} {entry.Monitor1h.EventHistory.LongestEvent:N3} {entry.Monitor1h.EventHistory.LastEvent:N3} - "
                    + $"0x{entry.Id.Raw:X8} {players.ToString().PadLeft(7)}  {creatures.ToString().PadLeft(9)}{'\n'}"
            );
        }

        var sortedBy5mLong = loadedLandblocks.OrderByDescending(r => r.Monitor5m.EventHistory.LongestEvent).Take(10);
        var sortedBy1hrLong = loadedLandblocks.OrderByDescending(r => r.Monitor1h.EventHistory.LongestEvent).Take(10);

        var combinedByLong = sortedBy5mLong
            .Concat(sortedBy1hrLong)
            .Distinct()
            .OrderByDescending(r =>
                Math.Max(r.Monitor5m.EventHistory.LongestEvent, r.Monitor1h.EventHistory.LongestEvent)
            )
            .Take(10);

        sb.Append($"Most Busy Landblock - By Longest{'\n'}");
        sb.Append($"~5m Hits   Avg  Long  Last - ~1h Hits   Avg  Long  Last - Location   Players  Creatures{'\n'}");

        foreach (var entry in combinedByLong)
        {
            int players = 0,
                creatures = 0;
            foreach (var worldObject in entry.GetAllWorldObjectsForDiagnostics())
            {
                if (worldObject is Player)
                {
                    players++;
                }
                else if (worldObject is Creature)
                {
                    creatures++;
                }
            }

            sb.Append(
                $"{entry.Monitor5m.EventHistory.TotalEvents.ToString().PadLeft(7)} {entry.Monitor5m.EventHistory.AverageEventDuration:N4} {entry.Monitor5m.EventHistory.LongestEvent:N3} {entry.Monitor5m.EventHistory.LastEvent:N3} - "
                    + $"{entry.Monitor1h.EventHistory.TotalEvents.ToString().PadLeft(7)} {entry.Monitor1h.EventHistory.AverageEventDuration:N4} {entry.Monitor1h.EventHistory.LongestEvent:N3} {entry.Monitor1h.EventHistory.LastEvent:N3} - "
                    + $"0x{entry.Id.Raw:X8} {players.ToString().PadLeft(7)}  {creatures.ToString().PadLeft(9)}{'\n'}"
            );
        }

        CommandHandlerHelper.WriteOutputInfo(session, sb.ToString());
    }
}
