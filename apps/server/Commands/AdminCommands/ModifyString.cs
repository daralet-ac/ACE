using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Managers;
using ACE.Server.Network;

namespace ACE.Server.Commands.AdminCommands;

public class ModifyString
{
    [CommandHandler(
        "modifystring",
        AccessLevel.Admin,
        CommandHandlerFlag.None,
        2,
        "Modifies a server property that is a string",
        "modifystring (string) (string)"
    )]
    public static void HandleModifyServerStringProperty(Session session, params string[] parameters)
    {
        if (PropertyManager.ModifyString(parameters[0], parameters[1]))
        {
            CommandHandlerHelper.WriteOutputInfo(session, "String property successfully updated!");
            PlayerManager.BroadcastToAuditChannel(
                session?.Player,
                $"Successfully changed server string property {parameters[0]} to {parameters[1]}"
            );
        }
        else
        {
            CommandHandlerHelper.WriteOutputInfo(
                session,
                "Unknown string property was not updated. Type showprops for a list of properties."
            );
        }
    }
}
