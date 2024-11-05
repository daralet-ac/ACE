using System;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Entity;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.Commands.DeveloperCommands;

public class Movement
{
    /// <summary>
    /// This function is just used to exercise the ability to have player movement without animation.   Once we are solid on this it can be removed.   Og II
    /// </summary>
    [CommandHandler(
        "movement",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        1,
        "Movement testing command, to be removed soon"
    )]
    public static void HandleMovement(Session session, params string[] parameters)
    {
        var forwardCommand = (MotionCommand)Convert.ToInt16(parameters[0]);

        var movement = new Motion(session.Player, forwardCommand);
        session.Network.EnqueueSend(new GameMessageUpdateMotion(session.Player, movement));

        movement = new Motion(session.Player, MotionCommand.Ready);
        session.Network.EnqueueSend(new GameMessageUpdateMotion(session.Player, movement));
    }
}
