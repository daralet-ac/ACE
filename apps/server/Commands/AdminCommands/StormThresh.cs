using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.AdminCommands;

public class StormThresh
{
    // stormthresh
    [CommandHandler("stormthresh", AccessLevel.Admin, CommandHandlerFlag.RequiresWorld, 1)]
    public static void Handlestormthresh(Session session, params string[] parameters)
    {
        // @stormthresh - Sets how many character can be in a landblock before we do a portal storm.

        // TODO: output
    }
}
