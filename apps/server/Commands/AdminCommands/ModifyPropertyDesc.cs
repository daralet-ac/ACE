using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Managers;
using ACE.Server.Network;

namespace ACE.Server.Commands.AdminCommands;

public class ModifyPropertyDesc
{
    [CommandHandler(
        "modifypropertydesc",
        AccessLevel.Admin,
        CommandHandlerFlag.None,
        3,
        "Modifies a server property's description",
        "modifypropertydesc <STRING|BOOL|DOUBLE|LONG> (string) (string)"
    )]
    public static void HandleModifyPropertyDescription(Session session, params string[] parameters)
    {
        var isSession = session != null;
        switch (parameters[0])
        {
            case "STRING":
                PropertyManager.ModifyStringDescription(parameters[1], parameters[2]);
                break;
            case "BOOL":
                PropertyManager.ModifyBoolDescription(parameters[1], parameters[2]);
                break;
            case "DOUBLE":
                PropertyManager.ModifyDoubleDescription(parameters[1], parameters[2]);
                break;
            case "LONG":
                PropertyManager.ModifyLongDescription(parameters[1], parameters[2]);
                break;
            default:
                CommandHandlerHelper.WriteOutputInfo(
                    session,
                    "Please pick from STRING, BOOL, DOUBLE, or LONG",
                    ChatMessageType.Help
                );
                return;
        }

        CommandHandlerHelper.WriteOutputInfo(
            session,
            "Successfully updated property description!",
            ChatMessageType.Help
        );
    }
}
