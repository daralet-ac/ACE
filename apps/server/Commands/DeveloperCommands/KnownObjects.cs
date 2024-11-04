using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.DeveloperCommands;

public class KnownObjects
{
    // knownobjs
    [CommandHandler("knownobjs", AccessLevel.Developer, CommandHandlerFlag.RequiresWorld, 0)]
    public static void Handleknownobjs(Session session, params string[] parameters)
    {
        // @knownobjs - Display a list of objects that the client is aware of.

        // TODO: output
    }
}
