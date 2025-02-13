using System;
using ACE.Database;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Managers;
using ACE.Server.Network;

namespace ACE.Server.Commands.PlayerCommands;

public class Pop
{
    // pop
    [CommandHandler("pop", AccessLevel.Player, CommandHandlerFlag.None, 0, "Show current world population", "")]
    public static void HandlePop(Session session, params string[] parameters)
    {
        ShowPop(session);
    }

    private static void ShowPop(Session session, ulong discordChannel = 0)
        {
            var showCurrent = PropertyManager.GetBool("pop_show_current").Item;
            var showUnique24Hours = PropertyManager.GetBool("pop_show_24_hours").Item;
            var showUnique7Days = PropertyManager.GetBool("pop_show_7_days").Item;
            var showUnique30Days = PropertyManager.GetBool("pop_show_30_days").Item;

            if (!showCurrent && !showUnique24Hours && !showUnique7Days && !showUnique30Days)
            {
                CommandHandlerHelper.WriteOutputInfo(session, "This command has been disabled.", ChatMessageType.Broadcast);
                return;
            }

            if (showCurrent)
            {
                CommandHandlerHelper.WriteOutputInfo(session, $"Current world population: {PlayerManager.GetOnlineCount():N0}", ChatMessageType.Broadcast);
            }

            if (showUnique24Hours)
            {
                DatabaseManager.Shard.GetUniqueIPsInTheLast(TimeSpan.FromHours(24), result => CommandHandlerHelper.WriteOutputInfo(session, $"Unique IPs connected in the last 24 hours: {result:N0}", ChatMessageType.Broadcast));
            }

            if (showUnique7Days)
            {
                DatabaseManager.Shard.GetUniqueIPsInTheLast(TimeSpan.FromDays(7), result => CommandHandlerHelper.WriteOutputInfo(session, $"Unique IPs connected in the last 7 days: {result:N0}", ChatMessageType.Broadcast));
            }

            if (showUnique30Days)
            {
                DatabaseManager.Shard.GetUniqueIPsInTheLast(TimeSpan.FromDays(30), result => CommandHandlerHelper.WriteOutputInfo(session, $"Unique IPs connected in the last 30 days: {result:N0}", ChatMessageType.Broadcast));
            }
        }
}
