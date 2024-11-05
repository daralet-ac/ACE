using System;
using System.Linq;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Managers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.Commands.PlayerCommands;

public class Invite
{
    [CommandHandler(
        "invite",
        AccessLevel.Player,
        CommandHandlerFlag.RequiresWorld,
        "Adds a player to the inviter's fellowship"
    )]
    public static void AddFellow(Session session, params string[] parameters)
    {
        var inviter = session.Player;
        var fellowship = inviter.Fellowship;

        if (fellowship == null)
        {
            session.Network.EnqueueSend(
                new GameMessageSystemChat(
                    $"You must be in a fellowship to invite players to a fellowship.",
                    ChatMessageType.Fellowship
                )
            );

            return;
        }

        if (fellowship.Open == false && fellowship.FellowshipLeaderGuid != inviter.Guid.Full)
        {
            session.Network.EnqueueSend(
                new GameMessageSystemChat(
                    $"You must be the leader of your fellowship or in an open fellowship to invite players.",
                    ChatMessageType.Fellowship
                )
            );

            return;
        }

        var input = string.Join(" ", parameters).ToLower();

        var allPlayers = PlayerManager.GetAllPlayers();
        var existingPlayer = allPlayers.FirstOrDefault(ePlayer => input.Equals(ePlayer.Name, StringComparison.CurrentCultureIgnoreCase));

        if (existingPlayer == null)
        {
            session.Network.EnqueueSend(
                new GameMessageSystemChat(
                    $"'{input}' does not exist.",
                    ChatMessageType.Fellowship
                )
            );

            return;
        }

        var onlinePlayers = PlayerManager.GetAllOnline();
        var newMember = onlinePlayers.FirstOrDefault(onlinePlayer => input.Equals(onlinePlayer.Name, StringComparison.CurrentCultureIgnoreCase));

        if (newMember == null)
        {
            session.Network.EnqueueSend(
                new GameMessageSystemChat(
                    $"'{input}' is not online.",
                    ChatMessageType.Fellowship
                )
            );

            return;
        }

        if (newMember.Fellowship != null)
        {
            session.Network.EnqueueSend(
                new GameMessageSystemChat(
                    $"'{input}' is already in a fellowship.",
                    ChatMessageType.Fellowship
                )
            );

            return;
        }

        session.Network.EnqueueSend(
            new GameMessageSystemChat(
                $"'Fellowship invite sent to {newMember.Name}.",
                ChatMessageType.Fellowship
            )
        );

        inviter.FellowshipRecruit(newMember);
    }
}
