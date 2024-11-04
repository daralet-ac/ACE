using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.DeveloperCommands;

public class Location
{
    // location
    [CommandHandler("location", AccessLevel.Developer, CommandHandlerFlag.RequiresWorld, 0)]
    public static void HandleLocation(Session session, params string[] parameters)
    {
        // @location - Causes your current location to be continuously displayed on the screen.

        // TODO: output
    }
}
