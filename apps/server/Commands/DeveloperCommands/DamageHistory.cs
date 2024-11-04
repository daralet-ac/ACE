using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.Commands.DeveloperCommands;

public class DamageHistory
{
    [CommandHandler("damagehistory", AccessLevel.Developer, CommandHandlerFlag.RequiresWorld)]
    public static void HandleDamageHistory(Session session, params string[] parameters)
    {
        session.Network.EnqueueSend(
            new GameMessageSystemChat(session.Player.DamageHistory.ToString(), ChatMessageType.Broadcast)
        );
    }
}
