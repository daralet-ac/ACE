using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Managers;
using ACE.Server.Network;

namespace ACE.Server.Commands.DeveloperCommands.Stats;

public class ServerPerformance
{
    // serverperformance
    [CommandHandler(
        "serverperformance",
        AccessLevel.Developer,
        CommandHandlerFlag.None,
        0,
        "Displays a summary of server performance statistics"
    )]
    public static void HandleServerPerformance(Session session, params string[] parameters)
    {
        if (parameters != null && (parameters.Length == 1 || parameters.Length == 2))
        {
            if (parameters.Length >= 1 && parameters[0].ToLower() == "start")
            {
                if (parameters.Length >= 2 && parameters[1].ToLower() == "cumulative")
                {
                    ServerPerformanceMonitor.StartCumulative();
                    CommandHandlerHelper.WriteOutputInfo(session, "Cumulative Server Performance Monitor started");
                    return;
                }
                else
                {
                    ServerPerformanceMonitor.Start();
                    CommandHandlerHelper.WriteOutputInfo(session, "Server Performance Monitor started");
                    return;
                }
            }

            if (parameters.Length >= 1 && parameters[0].ToLower() == "stop")
            {
                if (parameters.Length >= 2 && parameters[1].ToLower() == "cumulative")
                {
                    ServerPerformanceMonitor.StopCumulative();
                    CommandHandlerHelper.WriteOutputInfo(session, "Cumulative Server Performance Monitor stopped");
                    return;
                }
                else
                {
                    ServerPerformanceMonitor.Stop();
                    CommandHandlerHelper.WriteOutputInfo(session, "Server Performance Monitor stopped");
                    return;
                }
            }

            if (parameters.Length >= 1 && parameters[0].ToLower() == "reset")
            {
                ServerPerformanceMonitor.Reset();
                CommandHandlerHelper.WriteOutputInfo(session, "Server Performance Monitor reset");
                return;
            }
        }

        if (!ServerPerformanceMonitor.IsRunning)
        {
            CommandHandlerHelper.WriteOutputInfo(
                session,
                "Server Performance Monitor not running. To start use /serverperformance start"
            );
            return;
        }

        CommandHandlerHelper.WriteOutputInfo(session, ServerPerformanceMonitor.ToString());
    }
}
