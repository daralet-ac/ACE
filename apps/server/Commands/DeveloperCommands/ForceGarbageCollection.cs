using System;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.DeveloperCommands;

public class ForceGarbageCollection
{
    [CommandHandler("forcegc", AccessLevel.Developer, CommandHandlerFlag.None, 0, "Forces .NET Garbage Collection")]
    public static void HandleForceGC(Session session, params string[] parameters)
    {
        GC.Collect();

        CommandHandlerHelper.WriteOutputInfo(session, ".NET Garbage Collection forced");
    }
}
