using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.DeveloperCommands;

public class NetStats
{
    [CommandHandler("netstats", AccessLevel.Developer, CommandHandlerFlag.None, "View network statistics")]
    public static void HandleNetStats(Session session, params string[] parameters)
    {
        CommandHandlerHelper.WriteOutputInfo(session, NetworkStatistics.Summary(), ChatMessageType.Broadcast);
    }
}
