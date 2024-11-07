using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using Serilog;

namespace ACE.Server.Commands.AdminCommands;

public class Freeze
{
    private static readonly ILogger _log = Log.ForContext(typeof(Freeze));

    // freeze
    [CommandHandler("freeze", AccessLevel.Admin, CommandHandlerFlag.RequiresWorld, 0)]
    public static void HandleFreeze(Session session, params string[] parameters)
    {
        // @freeze - Freezes the selected target for 10 minutes or until unfrozen.

        // TODO: output
    }
}
