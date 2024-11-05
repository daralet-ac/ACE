using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.DeveloperCommands;

public class DumpAttackers
{
    // dumpattackers
    [CommandHandler("dumpattackers", AccessLevel.Developer, CommandHandlerFlag.RequiresWorld, 0)]
    public static void Handledumpattackers(Session session, params string[] parameters)
    {
        // @dumpattackers - Displays the detection and enemy information for the selected creature.

        // TODO: output
    }
}
