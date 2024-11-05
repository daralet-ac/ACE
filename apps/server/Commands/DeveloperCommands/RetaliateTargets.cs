using System;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using static ACE.Server.Commands.DeveloperCommands.DeveloperCommandUtilities;

namespace ACE.Server.Commands.DeveloperCommands;

public class RetaliateTargets
{
    /// <summary>
    /// Shows the list of retaliate targets for a monster
    /// </summary>
    [CommandHandler(
        "retaliatetargets",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        0,
        "Shows the list of retaliate targets for a monster",
        "<optional guid, or optional 'target' for last appraisal target>"
    )]
    public static void HandleRetaliateTargets(Session session, params string[] parameters)
    {
        var target = GetObjectMaintTarget(session, parameters);
        if (target == null)
        {
            return;
        }

        Console.WriteLine(
            $"\nRetaliate targets to {target.Name}: {target.PhysicsObj.ObjMaint.GetRetaliateTargetsCount()}"
        );

        foreach (var obj in target.PhysicsObj.ObjMaint.GetRetaliateTargetsValues())
        {
            Console.WriteLine($"{obj.Name} ({obj.ID:X8})");
        }
    }
}
