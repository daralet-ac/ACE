using System;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Managers;
using ACE.Server.Network;

namespace ACE.Server.Commands.AdminCommands.ServerStartStop;

public class SetShutdownInterval
{
    /// <summary>
    /// Increase or decrease the server shutdown interval in seconds
    /// </summary>
    [CommandHandler(
        "set-shutdown-interval",
        AccessLevel.Admin,
        CommandHandlerFlag.None,
        1,
        "Changes the delay, in seconds, before the server will shutdown.",
        "< 0-99999 > in seconds"
    )]
    public static void HandleSetShutdownInterval(Session session, params string[] parameters)
    {
        if (parameters?.Length > 0)
        {
            // delay server shutdown for up to x minutes
            // limit to uint length 65535
            var parseInt = parameters[0].Length > 5 ? parameters[0].Substring(0, 5) : parameters[0];
            if (uint.TryParse(parseInt, out var newShutdownInterval))
            {
                // newShutdownInterval is represented as a time element
                if (newShutdownInterval > uint.MaxValue)
                {
                    newShutdownInterval = uint.MaxValue;
                }

                var adminName = (session == null) ? "CONSOLE" : session.Player.Name;
                var msg =
                    $"{adminName} has requested the shut down interval be changed from {ServerManager.ShutdownInterval} seconds to {newShutdownInterval} seconds.";
                //log.Info(msg);
                PlayerManager.BroadcastToAuditChannel(session?.Player, msg);

                // set the interval
                ServerManager.SetShutdownInterval(Convert.ToUInt32(newShutdownInterval));

                // message the admin
                CommandHandlerHelper.WriteOutputInfo(
                    session,
                    $"Shutdown Interval (seconds to shutdown server) has been set to {ServerManager.ShutdownInterval}.",
                    ChatMessageType.Broadcast
                );
                return;
            }
        }
        CommandHandlerHelper.WriteOutputInfo(
            session,
            "Usage: /set-shutdown-interval <00000>",
            ChatMessageType.Broadcast
        );
    }
}
