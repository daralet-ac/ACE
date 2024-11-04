using System;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.DeveloperCommands;

public class Echo
{
    /// <summary>
    /// echo "text to send back to yourself" [ChatMessageType]
    /// </summary>
    [CommandHandler(
        "echo",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        "Send text back to yourself.",
        "\"text to send back to yourself\" [ChatMessageType]\n" + "ChatMessageType can be a uint or enum name"
    )]
    public static void HandleDebugEcho(Session session, params string[] parameters)
    {
        try
        {
            if (Enum.TryParse(parameters[1], true, out ChatMessageType cmt))
            {
                if (Enum.IsDefined(typeof(ChatMessageType), cmt))
                {
                    ChatPacket.SendServerMessage(session, parameters[0], cmt);
                }
                else
                {
                    ChatPacket.SendServerMessage(session, parameters[0], ChatMessageType.Broadcast);
                }
            }
        }
        catch (Exception)
        {
            ChatPacket.SendServerMessage(session, parameters[0], ChatMessageType.Broadcast);
        }
    }
}
