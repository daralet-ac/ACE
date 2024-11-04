using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using ACE.Server.Network.GameEvent.Events;

namespace ACE.Server.Commands.DeveloperCommands;

public class PortalStormTest
{
    [CommandHandler(
        "portalstorm",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        1,
        "Tests starting a portal storm on yourself",
        "storm_level [0=Brewing, 1=Imminent, 2=Stormed, 3=Subsided]"
    )]
    public static void HandlePortalStorm(Session session, params string[] parameters)
    {
        if (!uint.TryParse(parameters[0], out var storm_level))
        {
            CommandHandlerHelper.WriteOutputInfo(session, $"Invalid storm level {parameters[0]}");
            return;
        }
        if (storm_level > 3)
        {
            storm_level = 3;
        }

        switch (storm_level)
        {
            case 0:
                session.Network.EnqueueSend(new GameEventPortalStormBrewing(session));
                break;
            case 1:
                session.Network.EnqueueSend(new GameEventPortalStormImminent(session));
                break;
            case 2:
                // Portal Storm Event comes immediatley before the teleport
                session.Network.EnqueueSend(new GameEventPortalStorm(session));

                // We're going to move the player to 0,0
                var newPos = new Position(0x7F7F001C, 84, 84, 80, 0, 0, 0, 1);
                session.Player.Teleport(newPos);
                break;
            case 3:
                session.Network.EnqueueSend(new GameEventPortalStormSubsided(session));
                break;
        }
    }
}
