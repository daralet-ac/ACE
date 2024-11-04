using System;
using ACE.Database;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using Serilog;

namespace ACE.Server.Commands.AdminCommands;

public class AccountGet
{
    private static readonly ILogger _log = Log.ForContext(typeof(AccountGet));

    [CommandHandler(
        "accountget",
        AccessLevel.Admin,
        CommandHandlerFlag.ConsoleInvoke,
        1,
        "Gets an account.",
        "username"
    )]
    public static void HandleAccountGet(Session session, params string[] parameters)
    {
        var account = DatabaseManager.Authentication.GetAccountByName(parameters[0]);
        Console.WriteLine($"User: {account.AccountName}, ID: {account.AccountId}");
    }
}
