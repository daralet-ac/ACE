using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.Commands.SentinelCommands;

public class NeverSayDie
{
    // neversaydie [on/off]
    [CommandHandler(
        "neversaydie",
        AccessLevel.Sentinel,
        CommandHandlerFlag.RequiresWorld,
        0,
        "Turn immortality on or off.",
        "[ on | off ]\n" + "Defaults to on."
    )]
    public static void HandleNeverSayDie(Session session, params string[] parameters)
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
                session.Player.Invincible = false;
                session.Network.EnqueueSend(
                    new GameMessageSystemChat("You are once again mortal.", ChatMessageType.Broadcast)
                );
                break;
            case "on":
            default:
                session.Player.Invincible = true;
                session.Network.EnqueueSend(
                    new GameMessageSystemChat("You are now immortal.", ChatMessageType.Broadcast)
                );
                break;
        }
    }
}
