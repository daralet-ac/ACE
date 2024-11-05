using System;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using static ACE.Server.Commands.DeveloperCommands.DeveloperCommandUtilities;

namespace ACE.Server.Commands.DeveloperCommands;

public class KnownObjects
{
    /// <summary>
    /// Shows the list of objects currently known to an object
    /// </summary>
    [CommandHandler(
        "knownobjs",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        0,
        "Shows the list of objects currently known to an object",
        "<optional guid, or optional 'target' for last appraisal target>"
    )]
    public static void HandleKnownObjs(Session session, params string[] parameters)
    {
        var target = GetObjectMaintTarget(session, parameters);
        if (target == null)
        {
            return;
        }

        Console.WriteLine($"\nKnown objects to {target.Name}: {target.PhysicsObj.ObjMaint.GetKnownObjectsCount()}");

        foreach (var obj in target.PhysicsObj.ObjMaint.GetKnownObjectsValues())
        {
            Console.WriteLine($"{obj.Name} ({obj.ID:X8})");
        }
    }
}
