using System.Collections.Generic;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Managers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.Commands.DeveloperCommands;

public class GrantLuminance
{
    [CommandHandler(
        "grantluminance",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        1,
        "Give luminance to yourself (or the specified character).",
        "ulong\n" + "@grantluminance [name] 1500000 is max luminance"
    )]
    public static void HandleGrantLuminance(Session session, params string[] parameters)
    {
        if (parameters?.Length > 0)
        {
            var aceParams = new List<CommandParameterHelpers.ACECommandParameter>()
            {
                new CommandParameterHelpers.ACECommandParameter()
                {
                    Type = CommandParameterHelpers.ACECommandParameterType.OnlinePlayerNameOrIid,
                    Required = false,
                    DefaultValue = session.Player
                },
                new CommandParameterHelpers.ACECommandParameter()
                {
                    Type = CommandParameterHelpers.ACECommandParameterType.PositiveLong,
                    Required = true,
                    ErrorMessage = "You must specify the amount of luminance."
                }
            };
            if (CommandParameterHelpers.ResolveACEParameters(session, parameters, aceParams))
            {
                try
                {
                    var amount = aceParams[1].AsLong;
                    aceParams[0].AsPlayer.GrantLuminance(amount, XpType.Admin, ShareType.None);

                    session.Network.EnqueueSend(
                        new GameMessageSystemChat($"{amount:N0} luminance granted.", ChatMessageType.Advancement)
                    );

                    PlayerManager.BroadcastToAuditChannel(
                        session.Player,
                        $"{session.Player.Name} granted {amount:N0} luminance to {aceParams[0].AsPlayer.Name}."
                    );

                    return;
                }
                catch
                {
                    //overflow
                }
            }
        }

        ChatPacket.SendServerMessage(
            session,
            "Usage: /grantluminance [name] 1234 (max 999999999999)",
            ChatMessageType.Broadcast
        );
    }
}
