using System;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using ACE.Server.WorldObjects;

namespace ACE.Server.Commands.DeveloperCommands;

public class TurnTo
{
    [CommandHandler(
        "turnto",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        0,
        "Turns the last appraised object to the player",
        "turnto"
    )]
    public static void HandleRequestTurnTo(Session session, params string[] parameters)
    {
        // get the last appraised object
        var targetID = session.Player.CurrentAppraisalTarget;
        if (targetID == null)
        {
            Console.WriteLine("ERROR: no appraisal target");
            return;
        }
        var targetGuid = new ObjectGuid(targetID.Value);
        var target = session.Player.CurrentLandblock?.GetObject(targetGuid);
        if (target == null)
        {
            Console.WriteLine("Couldn't find " + targetGuid);
            return;
        }
        var creature = target as Creature;
        if (creature == null)
        {
            Console.WriteLine(target.Name + " is not a creature / monster");
            return;
        }
        creature.TurnTo(session.Player, true);
    }
}
