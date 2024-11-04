using System;
using System.Runtime;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.DeveloperCommands;

public class ForceGarbageCollectionLoh
{
    [CommandHandler(
        "forcegc2",
        AccessLevel.Developer,
        CommandHandlerFlag.None,
        0,
        "Forces .NET Garbage Collection with LOH Compact"
    )]
    public static void HandleForceGC2(Session session, params string[] parameters)
    {
        // https://learn.microsoft.com/en-us/dotnet/standard/garbage-collection/fundamentals
        // https://learn.microsoft.com/en-us/dotnet/api/system.runtime.gcsettings.largeobjectheapcompactionmode
        GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;

        GC.Collect();

        CommandHandlerHelper.WriteOutputInfo(session, ".NET Garbage Collection forced with LOH Compact");
    }
}
