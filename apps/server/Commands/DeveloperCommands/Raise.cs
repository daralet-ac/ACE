using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.DeveloperCommands;

public class Raise
{
    // raise
    [CommandHandler("raise", AccessLevel.Developer, CommandHandlerFlag.RequiresWorld, 2)]
    public static void HandleRaise(Session session, params string[] parameters)
    {
        // @raise - Raises your experience (or the experience in a skill) by the given amount.

        // TODO: output
    }
}
