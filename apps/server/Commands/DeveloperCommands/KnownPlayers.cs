using System;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using static ACE.Server.Commands.DeveloperCommands.DeveloperCommandUtilities;

namespace ACE.Server.Commands.DeveloperCommands;

public class KnownPlayers
{
    /// <summary>
    /// Shows the list of players known to an object
    /// KnownPlayers are used for broadcasting
    /// </summary>
    [CommandHandler(
        "knownplayers",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        0,
        "Shows the list of players known to an object",
        "<optional guid, or optional 'target' for last appraisal target>"
    )]
    public static void HandleKnownPlayers(Session session, params string[] parameters)
    {
        var target = GetObjectMaintTarget(session, parameters);
        if (target == null)
        {
            return;
        }

        Console.WriteLine($"\nKnown players to {target.Name}: {target.PhysicsObj.ObjMaint.GetKnownPlayersCount()}");

        foreach (var obj in target.PhysicsObj.ObjMaint.GetKnownPlayersValues())
        {
            Console.WriteLine($"{obj.Name} ({obj.ID:X8})");
        }
    }
}
