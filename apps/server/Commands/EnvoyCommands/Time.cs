using System;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Entity;
using ACE.Server.Network;

namespace ACE.Server.Commands.EnvoyCommands;

public class Time
{
    // time
    [CommandHandler("time", AccessLevel.Envoy, CommandHandlerFlag.None, 0, "Displays the server's current game time.")]
    public static void HandleTime(Session session, params string[] parameters)
    {
        // @time - Displays the server's current game time.

        var messageUTC = "The current server time in UtcNow is: " + DateTime.UtcNow;
        //var messagePY = "The current server time translated to DerethDateTime is:\n" + Timers.CurrentLoreTime;
        var messageIGPY = "The current server time shown in game client is:\n" + Timers.CurrentInGameTime;
        var messageTOD = $"It is currently {Timers.CurrentInGameTime.TimeOfDay} in game right now.";

        CommandHandlerHelper.WriteOutputInfo(session, messageUTC, ChatMessageType.WorldBroadcast);
        //CommandHandlerHelper.WriteOutputInfo(session, messagePY, ChatMessageType.WorldBroadcast);
        CommandHandlerHelper.WriteOutputInfo(session, messageIGPY, ChatMessageType.WorldBroadcast);
        CommandHandlerHelper.WriteOutputInfo(session, messageTOD, ChatMessageType.WorldBroadcast);
    }
}
