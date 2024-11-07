using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.Commands.EnvoyCommands;

public class MyIID
{
    // myiid
    [CommandHandler("myiid", AccessLevel.Envoy, CommandHandlerFlag.RequiresWorld, 0, "Displays your Instance ID (IID)")]
    public static void HandleMyIID(Session session, params string[] parameters)
    {
        // @myiid - Displays your Instance ID(IID).

        session.Network.EnqueueSend(
            new GameMessageSystemChat(
                $"GUID: {session.Player.Guid.Full}  - Low: {session.Player.Guid.Low} - High: {session.Player.Guid.High} - (0x{session.Player.Guid.Full:X})",
                ChatMessageType.Broadcast
            )
        );
    }
}
