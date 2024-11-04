using System;
using ACE.Database;
using ACE.Database.Models.Auth;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using Serilog;

namespace ACE.Server.Commands.PlayerCommands;

public class Password
{
    private static readonly ILogger _log = Log.ForContext(typeof(Password));

    /// <summary>
    /// Rate limiter for /passwd command
    /// </summary>
    private static readonly TimeSpan PasswdInterval = TimeSpan.FromSeconds(5);

    // passwd oldpassword newpassword
    [CommandHandler(
        "passwd",
        AccessLevel.Player,
        CommandHandlerFlag.RequiresWorld,
        2,
        "Change your account password.",
        "oldpassword newpassword\n"
    )]
    public static void HandlePasswd(Session session, params string[] parameters)
    {
        if (session == null)
        {
            CommandHandlerHelper.WriteOutputInfo(
                session,
                "This command is run from ingame client only",
                ChatMessageType.Broadcast
            );
            return;
        }

        _log.Debug("{Player} is changing their password", session.Player.Name);

        var currentTime = DateTime.UtcNow;

        if (currentTime - session.LastPassTime < PasswdInterval)
        {
            CommandHandlerHelper.WriteOutputInfo(
                session,
                $"This command may only be run once every {PasswdInterval.TotalSeconds} seconds.",
                ChatMessageType.Broadcast
            );
            return;
        }
        session.LastPassTime = currentTime;

        if (parameters.Length <= 0)
        {
            CommandHandlerHelper.WriteOutputInfo(
                session,
                "You must specify the current password for the account.",
                ChatMessageType.Broadcast
            );
            return;
        }

        if (parameters.Length < 1)
        {
            CommandHandlerHelper.WriteOutputInfo(
                session,
                "You must specify a new password for the account.",
                ChatMessageType.Broadcast
            );
            return;
        }

        var account = DatabaseManager.Authentication.GetAccountById(session.AccountId);

        if (account == null)
        {
            CommandHandlerHelper.WriteOutputInfo(
                session,
                $"Account {session.Account} ({session.AccountId}) wasn't found in the database! How are you in world without a valid account?",
                ChatMessageType.Broadcast
            );
            return;
        }

        var oldpassword = parameters[0];
        var newpassword = parameters[1];

        if (account.PasswordMatches(oldpassword))
        {
            account.SetPassword(newpassword);
            account.SetSaltForBCrypt();
        }
        else
        {
            CommandHandlerHelper.WriteOutputInfo(
                session,
                $"Unable to change password: Password provided in first parameter does not match current account password for this account!",
                ChatMessageType.Broadcast
            );
            return;
        }

        DatabaseManager.Authentication.UpdateAccount(account);

        CommandHandlerHelper.WriteOutputInfo(
            session,
            "Account password successfully changed.",
            ChatMessageType.Broadcast
        );
    }
}
