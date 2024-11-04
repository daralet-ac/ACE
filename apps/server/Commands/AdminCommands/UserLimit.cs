using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.AdminCommands;

public class UserLimit
{
    // userlimit { num }
    [CommandHandler("userlimit", AccessLevel.Admin, CommandHandlerFlag.RequiresWorld, 1)]
    public static void Handleuserlimit(Session session, params string[] parameters)
    {
        // @userlimit - Sets how many clients are allowed to connect to this world.

        // TODO: output
    }
}
