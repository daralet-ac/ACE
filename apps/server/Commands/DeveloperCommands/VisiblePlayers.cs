using System;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using static ACE.Server.Commands.DeveloperCommands.DeveloperCommandUtilities;

namespace ACE.Server.Commands.DeveloperCommands;

public class VisiblePlayers
{
    /// <summary>
    /// Shows the list of players visible to a player
    /// </summary>
    [CommandHandler(
        "visibleplayers",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        0,
        "Shows the list of players visible to a player",
        "<optional guid, or optional 'target' for last appraisal target>"
    )]
    public static void HandleVisiblePlayers(Session session, params string[] parameters)
    {
        var target = GetObjectMaintTarget(session, parameters);
        if (target == null)
        {
            return;
        }

        Console.WriteLine(
            $"\nVisible players to {target.Name}: {target.PhysicsObj.ObjMaint.GetVisibleObjectsValuesWhere(o => o.IsPlayer).Count}"
        );

        foreach (var obj in target.PhysicsObj.ObjMaint.GetVisibleObjectsValuesWhere(o => o.IsPlayer))
        {
            Console.WriteLine($"{obj.Name} ({obj.ID:X8})");
        }
    }
}
