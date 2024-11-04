using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.DeveloperCommands;

public class Radar
{
    // radar
    [CommandHandler("radar", AccessLevel.Developer, CommandHandlerFlag.RequiresWorld, 0)]
    public static void Handleradar(Session session, params string[] parameters)
    {
        // @radar - Toggles your radar on and off.

        // TODO: output
    }
}
