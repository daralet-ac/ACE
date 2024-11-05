using System;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Managers;
using ACE.Server.Network;

namespace ACE.Server.Commands.AdminCommands;

public class ModifyDouble
{
    [CommandHandler(
        "modifydouble",
        AccessLevel.Admin,
        CommandHandlerFlag.None,
        2,
        "Modifies a server property that is a double",
        "modifyfloat (string) (double)"
    )]
    public static void HandleModifyServerFloatProperty(Session session, params string[] parameters)
    {
        try
        {
            var doubleVal = double.Parse(parameters[1]);
            if (PropertyManager.ModifyDouble(parameters[0], doubleVal))
            {
                CommandHandlerHelper.WriteOutputInfo(session, "Double property successfully updated!");
                PlayerManager.BroadcastToAuditChannel(
                    session?.Player,
                    $"Successfully changed server double property {parameters[0]} to {doubleVal}"
                );
            }
            else
            {
                CommandHandlerHelper.WriteOutputInfo(
                    session,
                    "Unknown double property was not updated. Type showprops for a list of properties."
                );
            }
        }
        catch (Exception)
        {
            CommandHandlerHelper.WriteOutputInfo(session, "Please input a valid double", ChatMessageType.Help);
        }
    }
}
