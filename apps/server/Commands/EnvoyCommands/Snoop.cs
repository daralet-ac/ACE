using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.EnvoyCommands;

public class Snoop
{
    // snoop [start / stop] [Character Name]
    [CommandHandler("snoop", AccessLevel.Envoy, CommandHandlerFlag.RequiresWorld, 2)]
    public static void HandleSnoop(Session session, params string[] parameters)
    {
        // @snoop[start / stop][Character Name]
        // - If no character name is supplied, the currently selected character will be used.If neither start nor stop is specified, start will be assumed.
        // @snoop - Listen in on a player's private communication.

        // TODO: output
    }
}
