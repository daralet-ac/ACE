using System;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using static ACE.Server.Commands.DeveloperCommands.DeveloperCommandUtilities;

namespace ACE.Server.Commands.DeveloperCommands;

public class DestructionQueue
{
    /// <summary>
    /// Shows the list of previously visible objects queued for destruction for a player
    /// </summary>
    [CommandHandler(
        "destructionqueue",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        0,
        "Shows the list of previously visible objects queued for destruction for a player",
        "<optional guid, or optional 'target' for last appraisal target>"
    )]
    public static void HandleDestructionQueue(Session session, params string[] parameters)
    {
        var target = GetObjectMaintTarget(session, parameters);
        if (target == null)
        {
            return;
        }

        Console.WriteLine(
            $"\nDestruction queue for {target.Name}: {target.PhysicsObj.ObjMaint.GetDestructionQueueCount()}"
        );

        var currentTime = Physics.Common.PhysicsTimer.CurrentTime;

        foreach (var obj in target.PhysicsObj.ObjMaint.GetDestructionQueueCopy())
        {
            Console.WriteLine($"{obj.Key.Name} ({obj.Key.ID:X8}): {obj.Value - currentTime}");
        }
    }
}
