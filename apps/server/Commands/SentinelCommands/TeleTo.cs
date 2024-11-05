using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Managers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.Commands.SentinelCommands;

public class TeleTo
{
    // teleto [char]
    [CommandHandler(
        "teleto",
        AccessLevel.Sentinel,
        CommandHandlerFlag.RequiresWorld,
        1,
        "Teleport yourself to a player",
        "[Player's Name]\n"
    )]
    public static void HandleTeleto(Session session, params string[] parameters)
    {
        // @teleto - Teleports you to the specified character.
        var playerName = string.Join(" ", parameters);
        // Lookup the player in the world
        var player = PlayerManager.GetOnlinePlayer(playerName);
        // If the player is found, teleport the admin to the Player's location
        if (player != null)
        {
            session.Player.Teleport(player.Location);
        }
        else
        {
            session.Network.EnqueueSend(
                new GameMessageSystemChat($"Player {playerName} was not found.", ChatMessageType.Broadcast)
            );
        }
    }
}
