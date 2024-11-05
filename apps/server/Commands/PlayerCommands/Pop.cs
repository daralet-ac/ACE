using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Managers;
using ACE.Server.Network;

namespace ACE.Server.Commands.PlayerCommands;

public class Pop
{
    // pop
    [CommandHandler("pop", AccessLevel.Player, CommandHandlerFlag.None, 0, "Show current world population", "")]
    public static void HandlePop(Session session, params string[] parameters)
    {
        CommandHandlerHelper.WriteOutputInfo(
            session,
            $"Current world population: {PlayerManager.GetOnlineCount():N0}",
            ChatMessageType.Broadcast
        );
    }
}
