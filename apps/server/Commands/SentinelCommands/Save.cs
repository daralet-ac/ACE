using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.Commands.SentinelCommands;

public class Save
{
    /// <summary>
    /// Command for saving the Admin's current location as the sanctuary position. If a uint between 1-9 is provided as a parameter, the corresponding named recall is saved.
    /// </summary>
    /// <param name="parameters">A single uint value from 0 to 9. Value 0 saves the Sanctuary recall (default), values 1 through 9 save the corresponding named recall point.</param>
    [CommandHandler(
        "save",
        AccessLevel.Sentinel,
        CommandHandlerFlag.RequiresWorld,
        0,
        "Sets your sanctuary position or a named recall point.",
        "< recall number > - Saves your position into the numbered recall, valid values are 1 - 9.\n"
            + "NOTE: Calling @save without a number saves your sanctuary (Lifestone Recall) position."
    )]
    public static void HandleSave(Session session, params string[] parameters)
    {
        // Set the default of 0 to save the sanctuary portal if no parameter is passed.
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
            var playerPosition = session.Player.Location;
            var positionType = PositionType.Sanctuary;
            // Set the correct PositionType, based on the "Saved Positions" position type subset:
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

            // Save the position
            session.Player.SetPosition(positionType, new Position(playerPosition));
            // Report changes to client
            var positionMessage = new GameMessageSystemChat(
                $"Set: {positionType} to Loc: {playerPosition}",
                ChatMessageType.Broadcast
            );
            session.Network.EnqueueSend(positionMessage);
            return;
        }
        // Error parsing the text input, from parameter[0]
        var positionErrorMessage = new GameMessageSystemChat(
            "Could not determine the correct PositionType. Please use an integer value from 1 to 9; or omit the parmeter entirely.",
            ChatMessageType.Broadcast
        );
        session.Network.EnqueueSend(positionErrorMessage);
    }
}
