using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.Commands.DeveloperCommands;

public class ListPositions
{
    /// <summary>
    /// Debug command to print out all of the saved character positions.
    /// </summary>
    [CommandHandler(
        "listpositions",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        0,
        "Displays all available saved character positions from the database."
    )]
    public static void HandleListPositions(Session session, params string[] parameters)
    {
        var posDict = session.Player.GetAllPositions();
        var message = "Saved character positions:\n";

        foreach (var posPair in posDict)
        {
            message += "ID: " + (uint)posPair.Key + " Loc: " + posPair.Value + "\n";
        }

        message += "Total positions: " + posDict.Count + "\n";
        var positionMessage = new GameMessageSystemChat(message, ChatMessageType.Broadcast);
        session.Network.EnqueueSend(positionMessage);
    }
}
