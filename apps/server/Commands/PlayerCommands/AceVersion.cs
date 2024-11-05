using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Managers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.Commands.PlayerCommands;

public class AceVersion
{
    // show player ace server versions
    [CommandHandler(
        "aceversion",
        AccessLevel.Player,
        CommandHandlerFlag.RequiresWorld,
        "Shows this server's version data"
    )]
    public static void HandleACEversion(Session session, params string[] parameters)
    {
        if (!PropertyManager.GetBool("version_info_enabled").Item)
        {
            session.Network.EnqueueSend(
                new GameMessageSystemChat(
                    "The command \"aceversion\" is not currently enabled on this server.",
                    ChatMessageType.Broadcast
                )
            );
            return;
        }

        var msg = ServerBuildInfo.GetVersionInfo();

        session.Network.EnqueueSend(new GameMessageSystemChat(msg, ChatMessageType.WorldBroadcast));
    }
}
