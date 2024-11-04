using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Commands.Handlers;
using ACE.Server.Managers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.Commands.SentinelCommands;

public class TeleReturn
{
    /// <summary>
    /// Teleports a player to their previous position
    /// </summary>
    [CommandHandler(
        "telereturn",
        AccessLevel.Sentinel,
        CommandHandlerFlag.RequiresWorld,
        1,
        "Return a player to their previous location.",
        "PlayerName"
    )]
    public static void HandleTeleReturn(Session session, params string[] parameters)
    {
        var playerName = string.Join(" ", parameters);
        var player = PlayerManager.GetOnlinePlayer(playerName);
        if (player == null)
        {
            session.Network.EnqueueSend(
                new GameMessageSystemChat($"Player {playerName} was not found.", ChatMessageType.Broadcast)
            );
            return;
        }

        if (player.TeleportedCharacter == null)
        {
            session.Network.EnqueueSend(
                new GameMessageSystemChat(
                    $"Player {playerName} does not have a return position saved.",
                    ChatMessageType.Broadcast
                )
            );
            return;
        }

        player.Teleport(new Position(player.TeleportedCharacter));
        player.SetPosition(PositionType.TeleportedCharacter, null);
        player.Session.Network.EnqueueSend(
            new GameMessageSystemChat(
                $"{session.Player.Name} has returned you to your previous location.",
                ChatMessageType.Magic
            )
        );

        PlayerManager.BroadcastToAuditChannel(
            session.Player,
            $"{session.Player.Name} has returned {player.Name} to their previous location."
        );
    }
}
