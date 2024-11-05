using System;
using ACE.Database;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using Serilog;

namespace ACE.Server.Commands.AdminCommands;

public class AccountSetAccess
{
    private static readonly ILogger _log = Log.ForContext(typeof(AccountSetAccess));

    // set-accountaccess accountname (accesslevel)
    [CommandHandler(
        "set-accountaccess",
        AccessLevel.Admin,
        CommandHandlerFlag.None,
        1,
        "Change the access level of an account.",
        "accountname (accesslevel)\n"
            + "accesslevel can be a number or enum name\n"
            + "0 = Player | 1 = Advocate | 2 = Sentinel | 3 = Envoy | 4 = Developer | 5 = Admin"
    )]
    public static void HandleAccountUpdateAccessLevel(Session session, params string[] parameters)
    {
        var accountName = parameters[0].ToLower();

        var accountId = DatabaseManager.Authentication.GetAccountIdByName(accountName);

        if (accountId == 0)
        {
            CommandHandlerHelper.WriteOutputInfo(
                session,
                "Account " + accountName + " does not exist.",
                ChatMessageType.Broadcast
            );
            return;
        }

        var accessLevel = AccessLevel.Player;

        if (parameters.Length > 1)
        {
            if (Enum.TryParse(parameters[1], true, out accessLevel))
            {
                if (!Enum.IsDefined(typeof(AccessLevel), accessLevel))
                {
                    accessLevel = AccessLevel.Player;
                }
            }
        }

        var articleAorAN = "a";
        if (accessLevel == AccessLevel.Advocate || accessLevel == AccessLevel.Admin || accessLevel == AccessLevel.Envoy)
        {
            articleAorAN = "an";
        }

        if (accountId == 0)
        {
            CommandHandlerHelper.WriteOutputInfo(
                session,
                "Account " + accountName + " does not exist.",
                ChatMessageType.Broadcast
            );
            return;
        }

        DatabaseManager.Authentication.UpdateAccountAccessLevel(accountId, accessLevel);

        if (DatabaseManager.AutoPromoteNextAccountToAdmin && accessLevel == AccessLevel.Admin)
        {
            DatabaseManager.AutoPromoteNextAccountToAdmin = false;
        }

        CommandHandlerHelper.WriteOutputInfo(
            session,
            "Account "
                + accountName
                + " updated with access rights set as "
                + articleAorAN
                + " "
                + Enum.GetName(typeof(AccessLevel), accessLevel)
                + ".",
            ChatMessageType.Broadcast
        );
    }
}
