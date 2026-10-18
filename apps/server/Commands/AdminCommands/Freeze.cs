using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Managers;
using ACE.Server.Network;
using ACE.Server.WorldObjects;
using Serilog;

namespace ACE.Server.Commands.AdminCommands;

public class Freeze
{
    private static readonly ILogger _log = Log.ForContext(typeof(Freeze));

    // freeze
    [CommandHandler(
        "freeze",
        AccessLevel.Admin,
        CommandHandlerFlag.RequiresWorld,
        0,
        "Freezes a character in place for 10 minutes or until unfrozen.",
        "(char name)\nFreezes the named character, or your current selection when no name is given."
    )]
    public static void HandleFreeze(Session session, params string[] parameters)
    {
        // @freeze - Freezes the selected target for 10 minutes or until unfrozen.

        var player = FindTarget(session, parameters);

        if (player == null)
        {
            return;
        }

        if (player.IsAdminFrozen)
        {
            CommandHandlerHelper.WriteOutputInfo(
                session,
                $"{player.Name} is already frozen.",
                ChatMessageType.Broadcast
            );
            return;
        }

        player.AdminFreeze(Player.DefaultAdminFreezeSeconds);

        PlayerManager.BroadcastToAuditChannel(
            session.Player,
            $"{session.Player.Name} has frozen {player.Name} for ten minutes."
        );

        CommandHandlerHelper.WriteOutputInfo(
            session,
            $"{player.Name} has been frozen for ten minutes.",
            ChatMessageType.Broadcast
        );
    }

    /// <summary>
    /// The online player named in the parameters, or the last appraised player when there are none. Reports why to the
    /// caller and returns null when there's no such player.
    /// </summary>
    public static Player FindTarget(Session session, string[] parameters)
    {
        if (parameters.Length > 0)
        {
            var playerName = string.Join(" ", parameters);
            var player = PlayerManager.GetOnlinePlayer(playerName);

            if (player == null)
            {
                CommandHandlerHelper.WriteOutputInfo(
                    session,
                    $"There is no character named {playerName} online.",
                    ChatMessageType.Broadcast
                );
            }

            return player;
        }

        var selected = CommandHandlerHelper.GetLastAppraisedObject(session) as Player;

        if (selected == null)
        {
            CommandHandlerHelper.WriteOutputInfo(
                session,
                "Select a player or give a character name.",
                ChatMessageType.Broadcast
            );
        }

        return selected;
    }
}
