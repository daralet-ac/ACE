using System.Numerics;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.Commands.DeveloperCommands.WorldObjectCommands;

public class BumpVelocity
{
    [CommandHandler(
        "bumpvelocity",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        "Bumps the velocity of the last appraised object."
    )]
    public static void HandleBumpVelocity(Session session, params string[] parameters)
    {
        var obj = CommandHandlerHelper.GetLastAppraisedObject(session);

        if (obj?.PhysicsObj == null)
        {
            return;
        }

        var velocity = new Vector3(0, 0, 0.5f);

        obj.PhysicsObj.Velocity = velocity;

        session.Network.EnqueueSend(new GameMessageVectorUpdate(obj));
    }
}
