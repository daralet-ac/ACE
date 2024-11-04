using System;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.Commands.DeveloperCommands;

public class SetPosition
{
    /// <summary>
    /// Debug command to save the player's current location as specific position type.
    /// </summary>
    [CommandHandler(
        "setposition",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        1,
        "Saves the supplied character position type to the database.",
        "uint 1-27\n" + "@setposition 1"
    )]
    public static void HandleSetPosition(Session session, params string[] parameters)
    {
        if (parameters?.Length == 1)
        {
            var parsePositionString = parameters[0].Length > 19 ? parameters[0].Substring(0, 19) : parameters[0];

            // The enum labels max character length has been observered as length 19
            // int value can be: 0-27

            if (Enum.TryParse(parsePositionString, true, out PositionType positionType))
            {
                if (positionType != PositionType.Undef)
                {
                    // Create a new position from the current player location
                    var playerPosition = new Position(session.Player.Location);

                    // Save the position
                    session.Player.SetPosition(positionType, playerPosition);

                    // Report changes to client
                    var positionMessage = new GameMessageSystemChat(
                        $"Set: {positionType} to Loc: {playerPosition}",
                        ChatMessageType.Broadcast
                    );
                    session.Network.EnqueueSend(positionMessage);
                    return;
                }
            }
        }

        session.Network.EnqueueSend(
            new GameMessageSystemChat(
                "Could not determine the correct position type.\nPlease supply a single integer value from within the range of 1 through 27.",
                ChatMessageType.Broadcast
            )
        );
    }
}
