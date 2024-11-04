using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Managers;
using ACE.Server.Network;

namespace ACE.Server.Commands.AdminCommands;

public class ResyncProperties
{
    [CommandHandler("resyncproperties", AccessLevel.Admin, CommandHandlerFlag.None, "Resync the properties database")]
    public static void HandleResyncServerProperties(Session session, params string[] parameters)
    {
        PropertyManager.ResyncVariables();
    }
}
