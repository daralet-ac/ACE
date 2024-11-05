using System;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Managers;
using ACE.Server.Network;

namespace ACE.Server.Commands.AdminCommands;

public class ModifyBool
{
    [CommandHandler(
        "modifybool",
        AccessLevel.Admin,
        CommandHandlerFlag.None,
        2,
        "Modifies a server property that is a bool",
        "modifybool (string) (bool)"
    )]
    public static void HandleModifyServerBoolProperty(Session session, params string[] parameters)
    {
        try
        {
            var boolVal = bool.Parse(parameters[1]);

            var prevState = PropertyManager.GetBool(parameters[0]);

            if (prevState.Item == boolVal && !string.IsNullOrWhiteSpace(prevState.Description))
            {
                CommandHandlerHelper.WriteOutputInfo(
                    session,
                    $"Bool property is already {boolVal} for {parameters[0]}!"
                );
                return;
            }

            if (PropertyManager.ModifyBool(parameters[0], boolVal))
            {
                CommandHandlerHelper.WriteOutputInfo(session, "Bool property successfully updated!");
                PlayerManager.BroadcastToAuditChannel(
                    session?.Player,
                    $"Successfully changed server bool property {parameters[0]} to {boolVal}"
                );

                if (parameters[0] == "pk_server" || parameters[0] == "pkl_server")
                {
                    PlayerManager.UpdatePKStatusForAllPlayers(parameters[0], boolVal);
                }
            }
            else
            {
                CommandHandlerHelper.WriteOutputInfo(
                    session,
                    "Unknown bool property was not updated. Type showprops for a list of properties."
                );
            }
        }
        catch (Exception)
        {
            CommandHandlerHelper.WriteOutputInfo(session, "Please input a valid bool", ChatMessageType.Help);
        }
    }
}
