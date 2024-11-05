using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Managers;
using ACE.Server.Network;

namespace ACE.Server.Commands.AdminCommands;

public class ShowProps
{
    [CommandHandler(
        "showprops",
        AccessLevel.Admin,
        CommandHandlerFlag.None,
        0,
        "Displays the name of all properties configurable via the modify commands"
    )]
    public static void HandleDisplayProps(Session session, params string[] parameters)
    {
        CommandHandlerHelper.WriteOutputInfo(session, PropertyManager.ListProperties());
    }
}
