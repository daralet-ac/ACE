using ACE.Database;
using ACE.Database.Models.Auth;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using Serilog;

namespace ACE.Server.Commands.AdminCommands;

public class AccountSetPassword
{
    private static readonly ILogger _log = Log.ForContext(typeof(AccountSetPassword));

    // set-accountpassword accountname newpassword
    [CommandHandler(
        "set-accountpassword",
        AccessLevel.Admin,
        CommandHandlerFlag.None,
        2,
        "Set the account password.",
        "accountname newpassword\n"
    )]
    public static void HandleAccountSetPassword(Session session, params string[] parameters)
    {
        var accountName = parameters[0].ToLower();

        var account = DatabaseManager.Authentication.GetAccountByName(accountName);

        if (account == null)
        {
            CommandHandlerHelper.WriteOutputInfo(
                session,
                "Account " + accountName + " does not exist.",
                ChatMessageType.Broadcast
            );
            return;
        }

        if (parameters.Length < 1)
        {
            CommandHandlerHelper.WriteOutputInfo(
                session,
                "You must specify a password for the account.",
                ChatMessageType.Broadcast
            );
            return;
        }

        account.SetPassword(parameters[1]);
        account.SetSaltForBCrypt();

        DatabaseManager.Authentication.UpdateAccount(account);

        CommandHandlerHelper.WriteOutputInfo(
            session,
            $"Account password for {accountName} successfully changed.",
            ChatMessageType.Broadcast
        );
    }
}
