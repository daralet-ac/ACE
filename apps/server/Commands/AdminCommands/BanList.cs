using ACE.Database;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.AdminCommands;

public class BanList
{
    // banlist
    [CommandHandler(
        "banlist",
        AccessLevel.Admin,
        CommandHandlerFlag.None,
        0,
        "Lists all banned accounts on this world.",
        ""
    )]
    public static void HandleBanlist(Session session, params string[] parameters)
    {
        // @banlist - Lists all banned accounts on this world.

        var bannedAccounts = DatabaseManager.Authentication.GetListofBannedAccounts();

        if (bannedAccounts.Count != 0)
        {
            var msg = "The following accounts are banned:\n";
            msg += "-------------------\n";
            foreach (var account in bannedAccounts)
            {
                msg += account + "\n";
            }

            CommandHandlerHelper.WriteOutputInfo(session, msg, ChatMessageType.Broadcast);
        }
        else
        {
            CommandHandlerHelper.WriteOutputInfo(
                session,
                $"There are no accounts currently banned.",
                ChatMessageType.Broadcast
            );
        }
    }
}
