using System;
using System.Globalization;
using ACE.Common.Extensions;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Entity;
using ACE.Server.Network;
using ACE.Server.WorldObjects;

namespace ACE.Server.Commands.DeveloperCommands;

public class PlayAnimation
{
    [CommandHandler(
        "animation",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        1,
        "Plays an animation on the current player, or optionally another object",
        "MotionCommand (optional target guid)\n"
    )]
    public static void Animation(Session session, params string[] parameters)
    {
        if (!Enum.TryParse(parameters[0], out MotionCommand motionCommand))
        {
            ChatPacket.SendServerMessage(
                session,
                $"MotionCommand: {parameters[0]} not found",
                ChatMessageType.Broadcast
            );
            return;
        }
        WorldObject obj = session.Player;

        if (parameters.Length > 1)
        {
            if (
                !uint.TryParse(
                    parameters[1].TrimStart("0x"),
                    NumberStyles.HexNumber,
                    CultureInfo.InvariantCulture,
                    out var guid
                )
            )
            {
                ChatPacket.SendServerMessage(session, $"Invalid guid: {parameters[1]}", ChatMessageType.Broadcast);
                return;
            }
            obj = session.Player.FindObject(guid, Player.SearchLocations.Everywhere);
            if (obj == null)
            {
                ChatPacket.SendServerMessage(
                    session,
                    $"Couldn't find guid: {parameters[1]}",
                    ChatMessageType.Broadcast
                );
                return;
            }
            if (obj.CurrentMotionState == null)
            {
                ChatPacket.SendServerMessage(
                    session,
                    $"{obj.Name} ({obj.Guid}) has no CurrentMotionState",
                    ChatMessageType.Broadcast
                );
                return;
            }
        }
        var stance = obj.CurrentMotionState.Stance;

        var suffix = "";
        if (obj != session.Player)
        {
            suffix = $" on {obj.Name} ({obj.Guid})";
        }

        ChatPacket.SendServerMessage(
            session,
            $"Playing animation {stance}.{motionCommand}{suffix}",
            ChatMessageType.Broadcast
        );

        obj.EnqueueBroadcastMotion(new Motion(stance, motionCommand));
    }
}
