using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.EnvoyCommands;

public class Unlock
{
    // unlock {-all | IID}
    [CommandHandler("unlock", AccessLevel.Envoy, CommandHandlerFlag.RequiresWorld, 1)]
    public static void HandleUnlock(Session session, params string[] parameters)
    {
        // usage: @unlock {-all | IID}
        // Cleans the SQL lock on either everyone or the given player.
        // @unlock - Cleans the SQL lock on either everyone or the given player.

        // TODO: output
    }
}
