using ACE.Database;
using ACE.Database.Models.Auth;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Managers;
using ACE.Server.Network;

namespace ACE.Server.Commands.AdminCommands;

public class Unban
{
    // unban < acct >
    [CommandHandler(
        "unban",
        AccessLevel.Admin,
        CommandHandlerFlag.None,
        1,
        "Unbans the specified player account.",
        "[accountname]\n"
        + "This command removes the ban from the specified account. The player will then be able to log into the game."
    )]
    public static void HandleUnBanAccount(Session session, params string[] parameters)
    {
        // usage: @unban acct
        // This command removes the ban from the specified account.The player will then be able to log into the game.
        // @unban - Unbans the specified player account.

        var accountName = parameters[0];

        var account = DatabaseManager.Authentication.GetAccountByName(accountName);

        if (account == null)
        {
            CommandHandlerHelper.WriteOutputInfo(
                session,
                $"Cannot unban \"{accountName}\" because that account cannot be found in database. Check spelling and try again.",
                ChatMessageType.Broadcast
            );
            return;
        }

        if (account.BanExpireTime == null)
        {
            CommandHandlerHelper.WriteOutputInfo(
                session,
                $"Cannot unban\"{accountName}\" because that account is not banned.",
                ChatMessageType.Broadcast
            );
            return;
        }

        account.UnBan();
        var banText = $"UnBanned account {accountName}.";
        CommandHandlerHelper.WriteOutputInfo(session, banText, ChatMessageType.Broadcast);
        PlayerManager.BroadcastToAuditChannel(session?.Player, banText);
    }
}
