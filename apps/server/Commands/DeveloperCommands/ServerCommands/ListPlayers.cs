using System;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Managers;
using ACE.Server.Network;

namespace ACE.Server.Commands.DeveloperCommands.ServerCommands;

public class ListPlayers
{
    /// <summary>
    /// Debug command to print out all of the active players connected too the server.
    /// </summary>
    [CommandHandler(
        "listplayers",
        AccessLevel.Developer,
        CommandHandlerFlag.None,
        0,
        "Displays all of the active players connected too the server."
    )]
    public static void HandleListPlayers(Session session, params string[] parameters)
    {
        var message = "";
        uint playerCounter = 0;

        AccessLevel? targetAccessLevel = null;
        if (parameters?.Length > 0)
        {
            if (Enum.TryParse(parameters[0], true, out AccessLevel parsedAccessLevel))
            {
                targetAccessLevel = parsedAccessLevel;
            }
            else
            {
                try
                {
                    uint accessLevel = Convert.ToUInt16(parameters[0]);
                    targetAccessLevel = (AccessLevel)accessLevel;
                }
                catch (Exception)
                {
                    CommandHandlerHelper.WriteOutputInfo(
                        session,
                        "Invalid AccessLevel value",
                        ChatMessageType.Broadcast
                    );
                    return;
                }
            }
        }

        if (targetAccessLevel.HasValue)
        {
            message += $"Listing only {targetAccessLevel.Value.ToString()}s:\n";
        }

        foreach (var player in PlayerManager.GetAllOnline())
        {
            if (targetAccessLevel.HasValue && player.Account.AccessLevel != ((uint)targetAccessLevel.Value))
            {
                continue;
            }

            message += $"{player.Name} - Lv: {player.Level}, Acct: {player.Session.AccountId}\n";
            playerCounter++;
        }

        message += $"Total connected Players: {playerCounter}\n";

        CommandHandlerHelper.WriteOutputInfo(session, message, ChatMessageType.Broadcast);
    }
}
