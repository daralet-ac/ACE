using System;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using static ACE.Server.Commands.DeveloperCommands.DeveloperCommandUtilities;

namespace ACE.Server.Commands.DeveloperCommands;

public class VisibleTargets
{
    /// <summary>
    /// Shows the list of targets currently visible to a monster
    /// </summary>
    [CommandHandler(
        "visibletargets",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        0,
        "Shows the list of targets currently visible to a monster",
        "<optional guid, or optional 'target' for last appraisal target>"
    )]
    public static void HandleVisibleTargets(Session session, params string[] parameters)
    {
        var target = GetObjectMaintTarget(session, parameters);
        if (target == null)
        {
            return;
        }

        Console.WriteLine($"\nVisible targets to {target.Name}: {target.PhysicsObj.ObjMaint.GetVisibleTargetsCount()}");

        foreach (var obj in target.PhysicsObj.ObjMaint.GetVisibleTargetsValues())
        {
            Console.WriteLine($"{obj.Name} ({obj.ID:X8})");
        }
    }
}
