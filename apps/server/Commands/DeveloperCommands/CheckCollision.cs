using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.Commands.DeveloperCommands;

public class CheckCollision
{
    [CommandHandler(
        "check-collision",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        "Checks if the player is currently colliding with any other objects."
    )]
    public static void HandleCheckEthereal(Session session, params string[] parameters)
    {
        var colliding = session.Player.PhysicsObj.ethereal_check_for_collisions();

        session.Network.EnqueueSend(new GameMessageSystemChat($"IsColliding: {colliding}", ChatMessageType.Broadcast));
    }
}
