using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.SentinelCommands;

public class Mrt
{
    // mrt
    [CommandHandler(
        "mrt",
        AccessLevel.Sentinel,
        CommandHandlerFlag.RequiresWorld,
        0,
        "Toggles the ability to bypass housing boundaries",
        ""
    )]
    public static void HandleMRT(Session session, params string[] parameters)
    {
        // @mrt - Toggles the ability to bypass housing boundaries.
        session.Player.HandleMRT();
    }
}
