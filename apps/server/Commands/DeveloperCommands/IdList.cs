using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Managers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.Commands.DeveloperCommands;

public class IdList
{
    // idlist
    [CommandHandler(
        "idlist",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        0,
        "Shows the next ID that will be allocated from GuidManager."
    )]
    public static void HandleIDlist(Session session, params string[] parameters)
    {
        // @idlist - Shows the next ID that will be allocated from SQL.

        var sysChatMsg = new GameMessageSystemChat(
            GuidManager.GetIdListCommandOutput(),
            ChatMessageType.WorldBroadcast
        );
        session.Network.EnqueueSend(sysChatMsg);
    }
}
