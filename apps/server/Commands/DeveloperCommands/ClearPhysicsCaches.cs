using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using ACE.Server.Physics.Entity;

namespace ACE.Server.Commands.DeveloperCommands;

public class ClearPhysicsCaches
{
    [CommandHandler(
        "clearphysicscaches",
        AccessLevel.Developer,
        CommandHandlerFlag.None,
        0,
        "Clears Physics Object Caches"
    )]
    public static void HandleClearPhysicsCaches(Session session, params string[] parameters)
    {
        BSPCache.Clear();
        GfxObjCache.Clear();
        PolygonCache.Clear();
        VertexCache.Clear();

        CommandHandlerHelper.WriteOutputInfo(session, "Physics caches cleared");
    }
}
