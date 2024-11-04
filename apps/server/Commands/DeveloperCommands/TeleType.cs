using System;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.Commands.DeveloperCommands;

public class TeleType
{
    /// <summary>
    /// Debug command to teleport a player to a saved position, if the position type exists within the database.
    /// </summary>
    [CommandHandler(
        "teletype",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        1,
        "Teleport to a saved character position.",
        "uint 0-22\n" + "@teletype 1"
    )]
    public static void HandleTeleType(Session session, params string[] parameters)
    {
        if (parameters?.Length > 0)
        {
            var parsePositionString = parameters[0].Length > 3 ? parameters[0].Substring(0, 3) : parameters[0];

            if (Enum.TryParse(parsePositionString, true, out PositionType positionType))
            {
                if (session.Player.TeleToPosition(positionType))
                {
                    session.Network.EnqueueSend(
                        new GameMessageSystemChat(
                            $"{PositionType.Location} {session.Player.Location}",
                            ChatMessageType.Broadcast
                        )
                    );
                }
                else
                {
                    session.Network.EnqueueSend(
                        new GameMessageSystemChat(
                            $"Error finding saved character position: {positionType}",
                            ChatMessageType.Broadcast
                        )
                    );
                }
            }
        }
    }
}
