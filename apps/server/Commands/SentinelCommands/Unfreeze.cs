using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using Serilog;

namespace ACE.Server.Commands.SentinelCommands;

public class Unfreeze
{
    private static readonly ILogger _log = Log.ForContext(typeof(Unfreeze));

    // unfreeze
    [CommandHandler("unfreeze", AccessLevel.Sentinel, CommandHandlerFlag.RequiresWorld, 0)]
    public static void HandleUnFreeze(Session session, params string[] parameters)
    {
        // @unfreeze - Unfreezes the selected target.

        // TODO: output
    }
}
