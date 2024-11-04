using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.WorldObjects;

namespace ACE.Server.Commands.DeveloperCommands;

public class ShowSession
{
    [CommandHandler(
        "showsession",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        "Show IP and ID for network session of last appraised character"
    )]
    public static void HandleShowSession(Session session, params string[] parameters)
    {
        var target = CommandHandlerHelper.GetLastAppraisedObject(session);

        if (target != null && target is Player player)
        {
            if (player.Session != null)
            {
                session.Network.EnqueueSend(
                    new GameMessageSystemChat(
                        $"Session IP: {player.Session.EndPointC2S.Address} | C2S Port: {player.Session.EndPointC2S.Port} | S2C Port: {player.Session.EndPointS2C?.Port} | ClientId: {player.Session.Network.ClientId} is connected to Character: {player.Name} (0x{player.Guid.Full.ToString("X8")}), Account: {player.Account.AccountName} ({player.Account.AccountId})",
                        ChatMessageType.Broadcast
                    )
                );
            }
            else
            {
                session.Network.EnqueueSend(
                    new GameMessageSystemChat(
                        $"Session is null for {player.Name} which shouldn't occur.",
                        ChatMessageType.Broadcast
                    )
                );
            }
        }
    }
}
