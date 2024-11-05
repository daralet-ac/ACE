using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.DeveloperCommands;

public class DebugBoard
{
    [CommandHandler(
        "debugboard",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        "Shows the current chess board state"
    )]
    public static void HandleDebugBoard(Session session, params string[] parameters)
    {
        session.Player.ChessMatch?.Logic?.DebugBoard();
    }
}
