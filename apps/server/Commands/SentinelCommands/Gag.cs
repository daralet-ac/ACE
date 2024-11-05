using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Managers;
using ACE.Server.Network;
using Serilog;

namespace ACE.Server.Commands.SentinelCommands;

public class Gag
{
    private static readonly ILogger _log = Log.ForContext(typeof(Gag));

    // gag < char name >
    [CommandHandler(
        "gag",
        AccessLevel.Sentinel,
        CommandHandlerFlag.RequiresWorld,
        1,
        "Prevents a character from talking.",
        "< char name >\nThe character will not be able to @tell or use chat normally."
    )]
    public static void HandleGag(Session session, params string[] parameters)
    {
        // usage: @gag < char name >
        // This command gags the specified character for five minutes.  The character will not be able to @tell or use chat normally.
        // @gag - Prevents a character from talking.
        // @ungag -Allows a gagged character to talk again.

        if (parameters.Length > 0)
        {
            var playerName = string.Join(" ", parameters);

            var msg = "";
            if (PlayerManager.GagPlayer(session.Player, playerName))
            {
                msg = $"{playerName} has been gagged for five minutes.";
            }
            else
            {
                msg = $"Unable to gag a character named {playerName}, check the name and re-try the command.";
            }

            CommandHandlerHelper.WriteOutputInfo(session, msg, ChatMessageType.WorldBroadcast);
        }
    }
}
