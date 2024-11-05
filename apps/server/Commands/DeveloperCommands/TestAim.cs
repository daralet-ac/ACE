using System;
using System.Numerics;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Entity;
using ACE.Server.Factories;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.Physics.Extensions;
using static ACE.Server.Commands.DeveloperCommands.DeveloperCommandUtilities;

namespace ACE.Server.Commands.DeveloperCommands;

public class TestAim
{
    [CommandHandler(
        "testaim",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        1,
        "Tests the aim high/low motions, and projectile spawn position"
    )]
    public static void HandleTestAim(Session session, params string[] parameters)
    {
        var motionStr = parameters[0];

        if (!motionStr.StartsWith("Aim", StringComparison.OrdinalIgnoreCase))
        {
            session.Network.EnqueueSend(
                new GameMessageSystemChat($"Motion must start with Aim!", ChatMessageType.Broadcast)
            );
            return;
        }

        if (!Enum.TryParse(motionStr, true, out MotionCommand motionCommand))
        {
            session.Network.EnqueueSend(
                new GameMessageSystemChat($"Couldn't find MotionCommand {motionStr}", ChatMessageType.Broadcast)
            );
            return;
        }

        var positive = motionCommand >= MotionCommand.AimHigh15 && motionCommand <= MotionCommand.AimHigh90;

        if (LastTestAim != null)
        {
            LastTestAim.Destroy();
        }

        var motion = new Motion(session.Player, motionCommand);

        session.Player.EnqueueBroadcastMotion(motion);

        // spawn ethereal arrow w/ no velocity or gravity
        var localOrigin = session.Player.GetProjectileSpawnOrigin(300, motionCommand);

        var globalOrigin =
            session.Player.Location.Pos + Vector3.Transform(localOrigin, session.Player.Location.Rotation);

        var wo = WorldObjectFactory.CreateNewWorldObject(300);
        wo.Ethereal = true;
        wo.GravityStatus = false;

        var angle = motionCommand.GetAimAngle().ToRadians();
        var zRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, angle);

        wo.Location = new Position(session.Player.Location);
        wo.Location.Pos = globalOrigin;
        wo.Location.Rotation *= zRotation;

        session.Player.CurrentLandblock.AddWorldObject(wo);

        LastTestAim = wo;
    }
}
