using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.Commands.DeveloperCommands;

public class PkTimer
{
    [CommandHandler(
        "pktimer",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        "Sets your PK timer to the current time"
    )]
    public static void HandlePKTimer(Session session, params string[] parameters)
    {
        session.Player.UpdatePKTimer();

        session.Network.EnqueueSend(new GameMessageSystemChat($"Updated PK timer", ChatMessageType.Broadcast));
    }
}
