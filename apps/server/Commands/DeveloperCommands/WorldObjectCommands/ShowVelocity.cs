using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.Commands.DeveloperCommands.WorldObjectCommands;

public class ShowVelocity
{
    [CommandHandler(
        "showvelocity",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        "Shows the velocity of the last appraised object."
    )]
    public static void HandleShowVelocity(Session session, params string[] parameters)
    {
        var obj = CommandHandlerHelper.GetLastAppraisedObject(session);

        if (obj?.PhysicsObj == null)
        {
            return;
        }

        session.Network.EnqueueSend(new GameMessageSystemChat($"Velocity: {obj.Velocity}", ChatMessageType.Broadcast));
        session.Network.EnqueueSend(
            new GameMessageSystemChat($"Physics.Velocity: {obj.PhysicsObj.Velocity}", ChatMessageType.Broadcast)
        );
        session.Network.EnqueueSend(
            new GameMessageSystemChat($"CachedVelocity: {obj.PhysicsObj.CachedVelocity}", ChatMessageType.Broadcast)
        );
    }
}
