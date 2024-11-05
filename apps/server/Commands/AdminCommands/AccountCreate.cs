using System;
using System.Net;
using ACE.Database;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using Serilog;

namespace ACE.Server.Commands.AdminCommands;

public class AccountCreate
{
    private static readonly ILogger _log = Log.ForContext(typeof(AccountCreate));

    // accountcreate username password (accesslevel)
    [CommandHandler(
        "accountcreate",
        AccessLevel.Admin,
        CommandHandlerFlag.None,
        2,
        "Creates a new account.",
        "username password (accesslevel)\n"
            + "accesslevel can be a number or enum name\n"
            + "0 = Player | 1 = Advocate | 2 = Sentinel | 3 = Envoy | 4 = Developer | 5 = Admin"
    )]
    public static void HandleAccountCreate(Session session, params string[] parameters)
    {
        var defaultAccessLevel = (AccessLevel)Common.ConfigManager.Config.Server.Accounts.DefaultAccessLevel;

        if (!Enum.IsDefined(typeof(AccessLevel), defaultAccessLevel))
        {
            defaultAccessLevel = AccessLevel.Player;
        }

        var accessLevel = defaultAccessLevel;

        if (parameters.Length > 2)
        {
            if (Enum.TryParse(parameters[2], true, out accessLevel))
            {
                if (!Enum.IsDefined(typeof(AccessLevel), accessLevel))
                {
                    accessLevel = defaultAccessLevel;
                }
            }
        }

        var articleAorAN = "a";
        if (accessLevel == AccessLevel.Advocate || accessLevel == AccessLevel.Admin || accessLevel == AccessLevel.Envoy)
        {
            articleAorAN = "an";
        }

        var message = "";

        var accountExists = DatabaseManager.Authentication.GetAccountByName(parameters[0]);

        if (accountExists != null)
        {
            message = "Account already exists. Try a new name.";
        }
        else
        {
            try
            {
                var account = DatabaseManager.Authentication.CreateAccount(
                    parameters[0].ToLower(),
                    parameters[1],
                    accessLevel,
                    IPAddress.Parse("127.0.0.1")
                );

                if (DatabaseManager.AutoPromoteNextAccountToAdmin && accessLevel == AccessLevel.Admin)
                {
                    DatabaseManager.AutoPromoteNextAccountToAdmin = false;
                }

                message =
                    "Account successfully created for "
                    + account.AccountName
                    + " ("
                    + account.AccountId
                    + ") with access rights as "
                    + articleAorAN
                    + " "
                    + Enum.GetName(typeof(AccessLevel), accessLevel)
                    + ".";
            }
            catch
            {
                message = "Account already exists. Try a new name.";
            }
        }

        CommandHandlerHelper.WriteOutputInfo(session, message, ChatMessageType.WorldBroadcast);
    }
}
