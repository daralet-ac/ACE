using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.AdminCommands;

public class LbThresh
{
    // lbthresh
    [CommandHandler("lbthresh", AccessLevel.Admin, CommandHandlerFlag.RequiresWorld, 1)]
    public static void Handlelbthresh(Session session, params string[] parameters)
    {
        // the @lbthresh command sets the maximum amount of load servers can trade at each balance. Large load transfers at once can cause poor server performance.  (Large would be about 400, small is about 20.)
        // @lbthresh - Set how much load can be transferred between two servers during a single load balance.

        // TODO: output
    }
}
