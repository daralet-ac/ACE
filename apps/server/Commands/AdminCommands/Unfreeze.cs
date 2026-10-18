using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Managers;
using ACE.Server.Network;
using Serilog;

namespace ACE.Server.Commands.AdminCommands;

public class Unfreeze
{
    private static readonly ILogger _log = Log.ForContext(typeof(Unfreeze));

    // unfreeze
    [CommandHandler(
        "unfreeze",
        AccessLevel.Admin,
        CommandHandlerFlag.RequiresWorld,
        0,
        "Unfreezes a character frozen with @freeze.",
        "(char name)\nUnfreezes the named character, or your current selection when no name is given."
    )]
    public static void HandleUnFreeze(Session session, params string[] parameters)
    {
        // @unfreeze - Unfreezes the selected target.

        var player = Freeze.FindTarget(session, parameters);

        if (player == null)
        {
            return;
        }

        if (!player.IsAdminFrozen)
        {
            CommandHandlerHelper.WriteOutputInfo(session, $"{player.Name} is not frozen.", ChatMessageType.Broadcast);
            return;
        }

        player.AdminUnfreeze();

        PlayerManager.BroadcastToAuditChannel(session.Player, $"{session.Player.Name} has unfrozen {player.Name}.");

        CommandHandlerHelper.WriteOutputInfo(session, $"{player.Name} has been unfrozen.", ChatMessageType.Broadcast);
    }
}
