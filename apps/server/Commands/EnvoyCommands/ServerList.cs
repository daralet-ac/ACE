using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.EnvoyCommands;

public class ServerList
{
    // serverlist
    [CommandHandler("serverlist", AccessLevel.Envoy, CommandHandlerFlag.RequiresWorld, 0)]
    public static void HandleServerlist(Session session, params string[] parameters)
    {
        // The @serverlist command shows a list of the servers in the current server farm. The format is as follows:
        // -The server ID
        // -The server's speed relative to the master server
        // - Number of users reported by ObjLoc and LoadBalancing
        // - Current total load reported by LoadBalancing
        // - Total load from blocks with no players in them
        // - Blocks with players / blocks loaded / blocks owned
        // - The owned block range from low to high(in hex)
        // - The external IP address to talk to clients on
        // -If the server is your current server or the master
        // @serverlist - Shows a list of the logical servers that control this world.

        // TODO: output
    }
}
