using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.EnvoyCommands;

public class MyServer
{
    // myserver
    [CommandHandler("myserver", AccessLevel.Envoy, CommandHandlerFlag.RequiresWorld, 0)]
    public static void HandleMyServer(Session session, params string[] parameters)
    {
        // @myserver - Displays the number of the game server on which you are currently located.

        // TODO: output
    }
}
