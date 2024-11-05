using System;
using System.Collections.Generic;
using ACE.Database;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Managers;
using ACE.Server.Network;
using ACE.Server.Network.Managers;

namespace ACE.Server.Commands.SentinelCommands;

public class Ban
{
    // ban < acct > < days > < hours > < minutes >
    [CommandHandler(
        "ban",
        AccessLevel.Sentinel,
        CommandHandlerFlag.None,
        4,
        "Bans the specified player account.",
        "[accountname] [days] [hours] [minutes] (reason)\n"
            + "This command bans the specified player account for the specified time. This player will not be able to enter the game with any character until the time expires.\n"
            + "Example: @ban AccountName 0 0 5\n"
            + "Example: @ban AccountName 1 0 0 banned 1 day because reasons\n"
    )]
    public static void HandleBanAccount(Session session, params string[] parameters)
    {
        // usage: @ban < acct > < days > < hours > < minutes >
        // This command bans the specified player account for the specified time.This player will not be able to enter the game with any character until the time expires.
        // @ban - Bans the specified player account.

        var accountName = parameters[0];
        var banDays = parameters[1];
        var banHours = parameters[2];
        var banMinutes = parameters[3];

        var banReason = string.Empty;
        if (parameters.Length > 4)
        {
            var parametersAfterBanParams = "";
            for (var i = 4; i < parameters.Length; i++)
            {
                parametersAfterBanParams += parameters[i] + " ";
            }
            parametersAfterBanParams = parametersAfterBanParams.Trim();
            banReason = parametersAfterBanParams;
        }

        var account = DatabaseManager.Authentication.GetAccountByName(accountName);

        if (account == null)
        {
            CommandHandlerHelper.WriteOutputInfo(
                session,
                $"Cannot ban \"{accountName}\" because that account cannot be found in database. Check syntax/spelling and try again.",
                ChatMessageType.Broadcast
            );
            return;
        }

        if (!double.TryParse(banDays, out var days) || days < 0)
        {
            CommandHandlerHelper.WriteOutputInfo(session, $"Days must not be less than 0.", ChatMessageType.Broadcast);
            return;
        }
        if (!double.TryParse(banHours, out var hours) || hours < 0)
        {
            CommandHandlerHelper.WriteOutputInfo(session, $"Hours must not be less than 0.", ChatMessageType.Broadcast);
            return;
        }
        if (!double.TryParse(banMinutes, out var minutes) || minutes < 0)
        {
            CommandHandlerHelper.WriteOutputInfo(
                session,
                $"Minutes must not be less than 0.",
                ChatMessageType.Broadcast
            );
            return;
        }

        var bannedOn = DateTime.UtcNow;
        var banExpires = DateTime.UtcNow.AddDays(days).AddHours(hours).AddMinutes(minutes);

        var bannedBy = 0u;
        if (session != null)
        {
            bannedBy = session.AccountId;
        }

        account.BannedTime = bannedOn;
        account.BanExpireTime = banExpires;
        account.BannedByAccountId = bannedBy;
        if (!string.IsNullOrWhiteSpace(banReason))
        {
            account.BanReason = banReason;
        }

        DatabaseManager.Authentication.UpdateAccount(account);

        // Boot the player
        if (NetworkManager.Find(accountName) != null)
        {
            var bootArgs = new List<string> { "account" };
            if (!string.IsNullOrWhiteSpace(banReason))
            {
                bootArgs.Add($"{accountName},");
                bootArgs.Add(banReason);
            }
            else
            {
                bootArgs.Add(accountName);
            }

            Boot.HandleBoot(session, bootArgs.ToArray());
        }

        var banText =
            $"Banned account {accountName} for {days} days, {hours} hours and {minutes} minutes.{(!string.IsNullOrWhiteSpace(banReason) ? $" Reason: {banReason}" : "")}";
        CommandHandlerHelper.WriteOutputInfo(session, banText, ChatMessageType.Broadcast);
        PlayerManager.BroadcastToAuditChannel(session?.Player, banText);
    }
}
