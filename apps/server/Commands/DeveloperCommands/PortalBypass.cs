using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.Commands.DeveloperCommands;

public class PortalBypass
{
    // portalbypass [on/off]
    [CommandHandler(
        "portalbypass",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        0,
        "Toggles the ability to bypass portal restrictions.",
        "[ on | off ]\n" + "Defaults to on."
    )]
    public static void HandlePortalBypass(Session session, params string[] parameters)
    {
        string param;

        if (parameters.Length > 0)
        {
            param = parameters[0];
        }
        else
        {
            param = "on";
        }

        switch (param)
        {
            case "off":
                session.Player.IgnorePortalRestrictions = false;
                session.Network.EnqueueSend(
                    new GameMessageSystemChat(
                        "You are once again bound by portal restrictions.",
                        ChatMessageType.Broadcast
                    )
                );
                break;
            case "on":
            default:
                session.Player.IgnorePortalRestrictions = true;
                session.Network.EnqueueSend(
                    new GameMessageSystemChat(
                        "You are no longer bound by portal restrictions.",
                        ChatMessageType.Broadcast
                    )
                );
                break;
        }
    }
}
