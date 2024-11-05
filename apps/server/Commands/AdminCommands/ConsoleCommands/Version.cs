using System;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.AdminCommands.ConsoleCommands;

public class Version
{
    [CommandHandler(
        "version",
        AccessLevel.Admin,
        CommandHandlerFlag.ConsoleInvoke,
        0,
        "Show server version information.",
        ""
    )]
    public static void ShowVersion(Session session, params string[] parameters)
    {
        var msg = ServerBuildInfo.GetVersionInfo();
        Console.WriteLine(msg);
    }
}
