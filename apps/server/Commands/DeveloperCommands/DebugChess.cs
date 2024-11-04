using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.DeveloperCommands;

public class DebugChess
{
    [CommandHandler(
        "debugchess",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        "Shows the chess move history for a player"
    )]
    public static void HandleDebugChess(Session session, params string[] parameters)
    {
        session.Player.ChessMatch?.DebugMove();
    }
}
