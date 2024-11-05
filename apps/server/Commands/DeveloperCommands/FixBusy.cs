using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.DeveloperCommands;

public class FixBusy
{
    [CommandHandler(
        "fixbusy",
        AccessLevel.Player,
        CommandHandlerFlag.RequiresWorld,
        0,
        "Attempts to remove the hourglass / fix the busy state for the player"
    )]
    public static void HandleFixBusy(Session session, params string[] parameters)
    {
        session.Player.SendUseDoneEvent();
    }
}
