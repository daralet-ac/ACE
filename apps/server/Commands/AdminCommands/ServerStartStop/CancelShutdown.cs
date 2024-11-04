using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Managers;
using ACE.Server.Network;
using Serilog;

namespace ACE.Server.Commands.AdminCommands.ServerStartStop;

public class CancelShutdown
{
    private static readonly ILogger _log = Log.ForContext(typeof(CancelShutdown));

    /// <summary>
    /// Cancels an in-progress shutdown event.
    /// </summary>
    [CommandHandler(
        "cancel-shutdown",
        AccessLevel.Admin,
        CommandHandlerFlag.None,
        0,
        "Stops an active server shutdown.",
        ""
    )]
    public static void HandleCancelShutdown(Session session, params string[] parameters)
    {
        var adminName = (session == null) ? "CONSOLE" : session.Player.Name;
        _log.Information(
            "{AdminName} has requested the pending shut down @ {ShutdownLocalTime} ({ShutdownUtcTime} UTC) be cancelled.",
            adminName,
            ServerManager.ShutdownTime.ToLocalTime(),
            ServerManager.ShutdownTime
        );

        var msg =
            $"{adminName} has requested the pending shut down @ {ServerManager.ShutdownTime.ToLocalTime()} ({ServerManager.ShutdownTime} UTC) be cancelled.";
        PlayerManager.BroadcastToAuditChannel(session?.Player, msg);

        ServerManager.CancelShutdown();
    }
}
