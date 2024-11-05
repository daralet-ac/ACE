using System;
using ACE.Database;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.AdminCommands;

public class Watchmen
{
    // watchmen
    [CommandHandler(
        "watchmen",
        AccessLevel.Admin,
        CommandHandlerFlag.None,
        0,
        "Displays a list of accounts with the specified level of admin access.",
        "(accesslevel)"
    )]
    public static void Handlewatchmen(Session session, params string[] parameters)
    {
        // @watchmen - Displays a list of accounts with the specified level of admin access.

        var defaultAccessLevel = AccessLevel.Advocate;

        var accessLevel = defaultAccessLevel;

        if (parameters.Length > 0)
        {
            if (Enum.TryParse(parameters[0], true, out accessLevel))
            {
                if (!Enum.IsDefined(typeof(AccessLevel), accessLevel))
                {
                    accessLevel = defaultAccessLevel;
                }
            }
        }

        var list = DatabaseManager.Authentication.GetListofAccountsByAccessLevel(accessLevel);
        var message = "";

        if (list.Count > 0)
        {
            message = $"The following accounts have been granted {accessLevel.ToString()} rights:\n";
            foreach (var item in list)
            {
                message += item + "\n";
            }
        }
        else
        {
            message = $"There are no accounts with {accessLevel.ToString()} rights.";
        }

        CommandHandlerHelper.WriteOutputInfo(session, message, ChatMessageType.WorldBroadcast);
    }
}
