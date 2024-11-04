using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.DeveloperCommands;

public class ChatDump
{
    [CommandHandler(
        "chatdump",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        0,
        "Spews 1000 lines of text to you."
    )]
    public static void HandleChatDump(Session session, params string[] parameters)
    {
        for (var i = 0; i < 1000; i++)
        {
            ChatPacket.SendServerMessage(session, "Test Message " + i, ChatMessageType.Broadcast);
        }
    }
}
