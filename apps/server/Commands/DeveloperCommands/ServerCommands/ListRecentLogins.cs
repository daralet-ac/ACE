using System;
using System.Globalization;
using System.Linq;
using ACE.Database;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Entity;
using ACE.Server.Managers;
using ACE.Server.Network;

namespace ACE.Server.Commands.DeveloperCommands.ServerCommands;

public class ListRecentLogins
{
    /// <summary>
    /// Lists the accounts that logged in during the last X hours, from account_session_log.
    /// </summary>
    [CommandHandler(
        "recentlogins",
        AccessLevel.Developer,
        CommandHandlerFlag.None,
        0,
        "Lists the accounts that logged in during the last X hours (default 24).",
        "[hours]"
    )]
    public static void HandleRecentLogins(Session session, params string[] parameters)
    {
        var hours = RecentLogins.DefaultHours;

        if (parameters?.Length > 0)
        {
            if (!double.TryParse(parameters[0], NumberStyles.Float, CultureInfo.InvariantCulture, out hours) || hours <= 0)
            {
                CommandHandlerHelper.WriteOutputInfo(
                    session,
                    "Usage: /recentlogins [hours] - hours must be a number greater than 0.",
                    ChatMessageType.Broadcast
                );
                return;
            }
        }

        var (since, sinceUnixTime) = RecentLogins.GetCutoffs(TimeSpan.FromHours(hours));

        DatabaseManager.Shard.GetRecentLogins(
            since,
            sinceUnixTime,
            (sessions, characters) =>
            {
                var onlineAccountIds = PlayerManager.GetAllOnline().Select(p => p.Account.AccountId).ToHashSet();
                var logins = RecentLogins.Build(sessions, characters, onlineAccountIds);

                CommandHandlerHelper.WriteOutputInfo(
                    session,
                    RecentLogins.FormatForChat(logins, hours, DateTime.Now),
                    ChatMessageType.System
                );
            },
            ex =>
                CommandHandlerHelper.WriteOutputInfo(
                    session,
                    $"Failed to load recent logins: {ex.Message}",
                    ChatMessageType.Broadcast
                )
        );
    }
}
