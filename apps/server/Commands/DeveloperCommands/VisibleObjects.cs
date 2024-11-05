using System;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using static ACE.Server.Commands.DeveloperCommands.DeveloperCommandUtilities;

namespace ACE.Server.Commands.DeveloperCommands;

public class VisibleObjects
{
    /// <summary>
    /// Shows the list of objects currently visible to an object
    /// </summary>
    [CommandHandler(
        "visibleobjs",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        0,
        "Shows the list of objects currently visible to an object",
        "<optional guid, or optional 'target' for last appraisal target>"
    )]
    public static void HandleVisibleObjs(Session session, params string[] parameters)
    {
        var target = GetObjectMaintTarget(session, parameters);
        if (target == null)
        {
            return;
        }

        Console.WriteLine($"\nVisible objects to {target.Name}: {target.PhysicsObj.ObjMaint.GetVisibleObjectsCount()}");

        foreach (var obj in target.PhysicsObj.ObjMaint.GetVisibleObjectsValues())
        {
            Console.WriteLine($"{obj.Name} ({obj.ID:X8})");
        }
    }
}
