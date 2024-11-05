using System;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using ACE.Server.WorldObjects;

namespace ACE.Server.Commands.PlayerCommands;

public class ObjSend
{
    /// <summary>
    /// Force resend of all visible objects known to this player. Can fix rare cases of invisible object bugs.
    /// Can only be used once every 5 mins max.
    /// </summary>
    [CommandHandler(
        "objsend",
        AccessLevel.Player,
        CommandHandlerFlag.RequiresWorld,
        "Force resend of all visible objects known to this player. Can fix rare cases of invisible object bugs. Can only be used once every 5 mins max."
    )]
    public static void HandleObjSend(Session session, params string[] parameters)
    {
        // a good repro spot for this is the first room after the door in facility hub
        // in the portal drop / staircase room, the VisibleCells do not have the room after the door
        // however, the room after the door *does* have the portal drop / staircase room in its VisibleCells (the inverse relationship is imbalanced)
        // not sure how to fix this atm, seems like it triggers a client bug..

        if (DateTime.UtcNow - session.Player.PrevObjSend < TimeSpan.FromMinutes(5))
        {
            session.Player.SendTransientError("You have used this command too recently!");
            return;
        }

        var creaturesOnly =
            parameters.Length > 0 && parameters[0].Contains("creature", StringComparison.OrdinalIgnoreCase);

        var knownObjs = session.Player.GetKnownObjects();

        foreach (var knownObj in knownObjs)
        {
            if (creaturesOnly && !(knownObj is Creature))
            {
                continue;
            }

            session.Player.RemoveTrackedObject(knownObj, false);
            session.Player.TrackObject(knownObj);
        }
        session.Player.PrevObjSend = DateTime.UtcNow;
    }
}
