using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Managers;
using ACE.Server.Network;

namespace ACE.Server.Commands.SentinelCommands;

public class Ungag
{
    // ungag < char name >
    [CommandHandler(
        "ungag",
        AccessLevel.Sentinel,
        CommandHandlerFlag.RequiresWorld,
        1,
        "Allows a gagged character to talk again.",
        "< char name >\nThe character will again be able to @tell and use chat normally."
    )]
    public static void HandleUnGag(Session session, params string[] parameters)
    {
        // usage: @ungag < char name >
        // @ungag -Allows a gagged character to talk again.

        if (parameters.Length > 0)
        {
            var playerName = string.Join(" ", parameters);

            var msg = "";
            if (PlayerManager.UnGagPlayer(session.Player, playerName))
            {
                msg = $"{playerName} has been ungagged.";
            }
            else
            {
                msg = $"Unable to ungag a character named {playerName}, check the name and re-try the command.";
            }

            CommandHandlerHelper.WriteOutputInfo(session, msg, ChatMessageType.WorldBroadcast);
        }
    }
}
