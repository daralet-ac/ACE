using ACE.Entity.Enum;
using ACE.Server.Commands.AdminCommands.ServerStartStop;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.AdminCommands.ConsoleCommands;

public class Exit
{
    [CommandHandler(
        "exit",
        AccessLevel.Admin,
        CommandHandlerFlag.ConsoleInvoke,
        0,
        "Shut down server immediately.",
        ""
    )]
    public static void HandleExit(Session session, params string[] parameters)
    {
        StopNow.ShutdownServerNow(session, parameters);
    }
}
