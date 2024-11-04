using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.DeveloperCommands;

public class RaresDump
{
    // rares dump
    [CommandHandler("rares dump", AccessLevel.Developer, CommandHandlerFlag.RequiresWorld, 0)]
    public static void HandleRaresDump(Session session, params string[] parameters)
    {
        // @rares dump - Lists all tiers of rare items.

        // TODO: output
    }
}
