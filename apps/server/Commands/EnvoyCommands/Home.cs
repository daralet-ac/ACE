using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.Commands.EnvoyCommands;

public class Home
{
    /// <summary>
    /// Teleports an admin to their sanctuary position. If a single uint value from 1 to 9 is provided as a parameter then the admin is teleported to the cooresponding named recall point.
    /// </summary>
    /// <param name="parameters">A single uint value from 0 to 9. Value 0 recalls to Sanctuary, values 1 through 9 teleports too the corresponding saved recall point.</param>
    [CommandHandler(
        "home",
        AccessLevel.Envoy,
        CommandHandlerFlag.RequiresWorld,
        0,
        "Teleports you to your sanctuary position.",
        "< recall number > - Recalls to a saved position, valid values are 1 - 9.\n"
            + "NOTE: Calling @home without a number recalls your sanctuary position; calling it with a number will teleport you to the corresponding saved position."
    )]
    public static void HandleHome(Session session, params string[] parameters)
    {
        // @home has the alias @recall
        var parsePositionString = "0";

        // Limit the incoming parameter to 1 character
        if (parameters?.Length >= 1)
        {
            parsePositionString = parameters[0].Length > 1 ? parameters[0].Substring(0, 1) : parameters[0];
        }

        // Attempt to parse the integer
        if (uint.TryParse(parsePositionString, out var parsedPositionInt))
        {
            // parsedPositionInt value should be limited too a value from, 0-9
            // Create a new position from the current player location
            var positionType = new PositionType();
            // Transform too the correct PositionType, based on the "Saved Positions" subset:
            switch (parsedPositionInt)
            {
                case 0:
                {
                    positionType = PositionType.Sanctuary;
                    break;
                }
                case 1:
                {
                    positionType = PositionType.Save1;
                    break;
                }
                case 2:
                {
                    positionType = PositionType.Save2;
                    break;
                }
                case 3:
                {
                    positionType = PositionType.Save3;
                    break;
                }
                case 4:
                {
                    positionType = PositionType.Save4;
                    break;
                }
                case 5:
                {
                    positionType = PositionType.Save5;
                    break;
                }
                case 6:
                {
                    positionType = PositionType.Save6;
                    break;
                }
                case 7:
                {
                    positionType = PositionType.Save7;
                    break;
                }
                case 8:
                {
                    positionType = PositionType.Save8;
                    break;
                }
                case 9:
                {
                    positionType = PositionType.Save9;
                    break;
                }
            }

            // If we have the position, teleport the player
            var position = session.Player.GetPosition(positionType);
            if (position != null)
            {
                session.Player.TeleToPosition(positionType);
                var positionMessage = new GameMessageSystemChat(
                    $"Recalling to {positionType}",
                    ChatMessageType.Broadcast
                );
                session.Network.EnqueueSend(positionMessage);
                return;
            }
        }
        // Invalid character was receieved in the input (it was not 0-9)
        var homeErrorMessage = new GameMessageSystemChat(
            "Could not find a valid recall position.",
            ChatMessageType.Broadcast
        );
        session.Network.EnqueueSend(homeErrorMessage);
    }
}
