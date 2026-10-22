using System.Linq;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Entity.AdminUi;
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
        var value = ValueOf(parameters);

        if (PropertyManager.ModifyString(parameters[0], value))
        {
            CommandHandlerHelper.WriteOutputInfo(session, "String property successfully updated!");
            // the audit channel goes to every Admin online and into the logs, so a key like proxycheck_api_key isn't repeated
            PlayerManager.BroadcastToAuditChannel(
                session?.Player,
                AdminUiSettings.IsSecret(parameters[0])
                    ? $"Successfully changed server string property {parameters[0]} (a secret, so not shown)"
                    : $"Successfully changed server string property {parameters[0]} to {value}"
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

    /// <summary>
    /// Everything after the name of the property, as one value, so that a value with spaces doesn't need quotes
    /// (without this only its first word was stored)
    /// </summary>
    internal static string ValueOf(string[] parameters)
    {
        return string.Join(" ", parameters.Skip(1));
    }
}
