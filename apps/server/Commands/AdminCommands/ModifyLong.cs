using System;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Managers;
using ACE.Server.Network;

namespace ACE.Server.Commands.AdminCommands;

public class ModifyLong
{
    [CommandHandler(
        "modifylong",
        AccessLevel.Admin,
        CommandHandlerFlag.None,
        2,
        "Modifies a server property that is a long",
        "modifylong (string) (long)"
    )]
    public static void HandleModifyServerLongProperty(Session session, params string[] paramters)
    {
        try
        {
            var longVal = long.Parse(paramters[1]);
            if (PropertyManager.ModifyLong(paramters[0], longVal))
            {
                CommandHandlerHelper.WriteOutputInfo(session, "Long property successfully updated!");
                PlayerManager.BroadcastToAuditChannel(
                    session?.Player,
                    $"Successfully changed server long property {paramters[0]} to {longVal}"
                );
            }
            else
            {
                CommandHandlerHelper.WriteOutputInfo(
                    session,
                    "Unknown long property was not updated. Type showprops for a list of properties."
                );
            }
        }
        catch (Exception)
        {
            CommandHandlerHelper.WriteOutputInfo(session, "Please input a valid long", ChatMessageType.Help);
        }
    }
}
