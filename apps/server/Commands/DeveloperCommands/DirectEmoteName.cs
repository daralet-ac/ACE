using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Managers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.Commands.DeveloperCommands;

public class DirectEmoteName
{
    // de_n name, text
    [CommandHandler(
        "de_n",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        2,
        "Sends text to named player, formatted exactly as entered.",
        "<name>, <text>"
    )]
    public static void Handlede_n(Session session, params string[] parameters)
    {
        // usage: @de_n name, text
        // usage: @direct_emote_name name, text
        // Sends text to named player, formatted exactly as entered, with no prefix of any kind.
        // @direct_emote_name - Sends text to named player, formatted exactly as entered.

        Handledirect_emote_name(session, parameters);
    }

    // direct_emote_name name, text
    [CommandHandler(
        "direct_emote_name",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        2,
        "Sends text to named player, formatted exactly as entered.",
        "<name>, <text>"
    )]
    public static void Handledirect_emote_name(Session session, params string[] parameters)
    {
        // usage: @de_n name, text
        // usage: @direct_emote_name name, text
        // Sends text to named player, formatted exactly as entered, with no prefix of any kind.
        // @direct_emote_name - Sends text to named player, formatted exactly as entered.

        var args = string.Join(" ", parameters);
        if (!args.Contains(","))
        {
            session.Network.EnqueueSend(
                new GameMessageSystemChat($"There was no player name specified.", ChatMessageType.Broadcast)
            );
        }
        else
        {
            var split = args.Split(",");
            var playerName = split[0];
            var msg = string.Join(" ", parameters).Remove(0, playerName.Length + 2);

            var player = PlayerManager.GetOnlinePlayer(playerName);
            if (player != null)
            {
                player.SendMessage(msg);
            }
            else
            {
                session.Network.EnqueueSend(
                    new GameMessageSystemChat($"Player {playerName} is not online.", ChatMessageType.Broadcast)
                );
            }
        }
    }
}
