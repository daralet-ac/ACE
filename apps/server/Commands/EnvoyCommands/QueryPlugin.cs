using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.EnvoyCommands;

public class QueryPlugin
{
    // queryplugin
    [CommandHandler("queryplugin", AccessLevel.Envoy, CommandHandlerFlag.RequiresWorld, 1)]
    public static void HandleQueryplugin(Session session, params string[] parameters)
    {
        // @queryplugin < pluginname > -View information about a specific plugin.

        // TODO: output
    }
}
