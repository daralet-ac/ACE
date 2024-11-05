using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.DeveloperCommands;

public class ReloadSystemMessage
{
    // reloadsysmsg
    [CommandHandler("reloadsysmsg", AccessLevel.Developer, CommandHandlerFlag.RequiresWorld, 0)]
    public static void Handlereloadsysmsg(Session session, params string[] parameters)
    {
        // @reloadsysmsg - Causes all servers to reload system_messages.txt.

        // TODO: output
    }
}
