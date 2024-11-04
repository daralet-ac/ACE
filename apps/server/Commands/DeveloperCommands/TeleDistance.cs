using System.Numerics;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Entity;
using ACE.Server.Network;

namespace ACE.Server.Commands.DeveloperCommands;

public class TeleDistance
{
    /// <summary>
    /// Teleport object culling precision test
    /// </summary>
    [CommandHandler(
        "teledist",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        1,
        "Teleports a some distance ahead of the last object spawned",
        "<distance>"
    )]
    public static void HandleTeleportDist(Session session, params string[] parameters)
    {
        if (parameters.Length < 1)
        {
            return;
        }

        var lastSpawnPos = CreateNamed.LastSpawnPos;

        var distance = float.Parse(parameters[0]);

        var newPos = new Position();
        newPos.LandblockId = new LandblockId(lastSpawnPos.LandblockId.Raw);
        newPos.Pos = lastSpawnPos.Pos;
        newPos.Rotation = session.Player.Location.Rotation;

        var dir = Vector3.Normalize(Vector3.Transform(Vector3.UnitY, newPos.Rotation));
        var offset = dir * distance;

        newPos.SetPosition(newPos.Pos + offset);

        session.Player.Teleport(newPos);

        var globLastSpawnPos = lastSpawnPos.ToGlobal();
        var globNewPos = newPos.ToGlobal();

        var totalDist = Vector3.Distance(globLastSpawnPos, globNewPos);

        var totalDist2d = Vector2.Distance(
            new Vector2(globLastSpawnPos.X, globLastSpawnPos.Y),
            new Vector2(globNewPos.X, globNewPos.Y)
        );

        ChatPacket.SendServerMessage(
            session,
            $"Teleporting player to {newPos.Cell:X8} @ {newPos.Pos}",
            ChatMessageType.System
        );

        ChatPacket.SendServerMessage(session, "2D Distance: " + totalDist2d, ChatMessageType.System);
        ChatPacket.SendServerMessage(session, "3D Distance: " + totalDist, ChatMessageType.System);
    }
}
