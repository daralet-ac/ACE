using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.EnvoyCommands;

public class QueryPluginList
{
    // querypluginlist
    [CommandHandler("querypluginlist", AccessLevel.Envoy, CommandHandlerFlag.RequiresWorld, 0)]
    public static void HandleQuerypluginlist(Session session, params string[] parameters)
    {
        // @querypluginlist (-fresh) - View list of plug-ins the selected character is running. If you do not use the -fresh paramater, you will get results that where cached at login. That way, you will not be sending an alert to the player that you are querying thier plugin list.  If you use -fresh, then you will get fresh data from the player's client and they will recieve notification that you have asked for thier plugin list.NOTE: Results are dependent upon 3rd party authors providing correct information.
        // @querypluginlist - View list of plug - ins the selected character is running.
        // @querypluginlist<pluginname> - View information about a specific plugin.NOTE: Results are dependent upon 3rd party authors providing correct information.
        // @queryplugin < pluginname > -View information about a specific plugin.

        // TODO: output
    }
}
