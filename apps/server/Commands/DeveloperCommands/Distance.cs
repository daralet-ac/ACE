using System.Numerics;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Entity;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.Commands.DeveloperCommands;

public class Distance
{
    /// <summary>
    /// Returns the distance to the last appraised object
    /// </summary>
    [CommandHandler(
        "dist",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        0,
        "Returns the distance to the last appraised object"
    )]
    public static void HandleDist(Session session, params string[] parameters)
    {
        var obj = CommandHandlerHelper.GetLastAppraisedObject(session);
        if (obj == null || obj.PhysicsObj == null)
        {
            return;
        }

        var sourcePos = session.Player.Location.ToGlobal();
        var targetPos = obj.Location.ToGlobal();

        var dist = Vector3.Distance(sourcePos, targetPos);
        var dist2d = Vector2.Distance(new Vector2(sourcePos.X, sourcePos.Y), new Vector2(targetPos.X, targetPos.Y));

        var cylDist = session.Player.PhysicsObj.get_distance_to_object(obj.PhysicsObj, true);

        session.Network.EnqueueSend(new GameMessageSystemChat($"Dist: {dist}", ChatMessageType.Broadcast));
        session.Network.EnqueueSend(new GameMessageSystemChat($"2D Dist: {dist2d}", ChatMessageType.Broadcast));

        session.Network.EnqueueSend(new GameMessageSystemChat($"CylDist: {cylDist}", ChatMessageType.Broadcast));
    }
}
