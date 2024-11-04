using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.AdminCommands;

public class StormNumStormed
{
    // stormnumstormed
    [CommandHandler("stormnumstormed", AccessLevel.Admin, CommandHandlerFlag.RequiresWorld, 1)]
    public static void Handlestormnumstormed(Session session, params string[] parameters)
    {
        // @stormnumstormed - Sets how many characters are teleported away during a portal storm.

        // TODO: output
    }
}
