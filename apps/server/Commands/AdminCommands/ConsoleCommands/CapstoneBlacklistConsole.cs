using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Entity.Actions;
using ACE.Server.Managers;
using ACE.Server.Network;

namespace ACE.Server.Commands.AdminCommands.ConsoleCommands;

public class CapstoneBlacklistConsole
{
    /// <summary>
    /// The server console's version of "@capstone blacklist": a console command can't also be used in game (the command
    /// manager keeps the two apart), so it is a command of its own, doing the same thing and answering in the console.
    /// </summary>
    [CommandHandler(
        "capstoneblacklist",
        AccessLevel.Admin,
        CommandHandlerFlag.ConsoleInvoke,
        1,
        "Keeps characters off the capstone leaderboard (the console version of @capstone blacklist).",
        "add <name> | remove <name> | list"
    )]
    public static void HandleCapstoneBlacklist(Session session, params string[] parameters)
    {
        // The console runs on its own thread, and this changes a character (who may be playing), so it is done on the
        // world thread, as a command typed in game would be
        WorldManager.EnqueueAction(
            new ActionEventDelegate(() =>
                Capstone.RunBlacklist(parameters, message => CommandHandlerHelper.WriteOutputInfo(session, message))
            )
        );
    }
}
