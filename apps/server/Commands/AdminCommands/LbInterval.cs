using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.AdminCommands;

public class LbInterval
{
    // lbinterval
    [CommandHandler("lbinterval", AccessLevel.Admin, CommandHandlerFlag.RequiresWorld, 1)]
    public static void Handlelbinterval(Session session, params string[] parameters)
    {
        // @lbinterval - Sets how often in seconds the server farm will rebalance the server farm load.

        // TODO: output
    }
}
