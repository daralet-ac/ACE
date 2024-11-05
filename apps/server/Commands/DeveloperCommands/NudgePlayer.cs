using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.WorldObjects;
using static ACE.Server.Commands.DeveloperCommands.DeveloperCommandUtilities;

namespace ACE.Server.Commands.DeveloperCommands;

public class NudgePlayer
{
    [CommandHandler(
        "nudge",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        0,
        "Correct player position cell ID after teleporting into black space."
    )]
    public static void HandleNudge(Session session, params string[] parameters)
    {
        var pos = session.Player.GetPosition(PositionType.Location);
        if (WorldObject.AdjustDungeonCells(pos))
        {
            pos.PositionZ += 0.005000f;
            var posReadable = PostionAsLandblocksGoogleSpreadsheetFormat(pos);
            TeleLoc.HandleTeleportLOC(session, posReadable.Split(' '));
            var positionMessage = new GameMessageSystemChat(
                $"Nudge player to {posReadable}",
                ChatMessageType.Broadcast
            );
            session.Network.EnqueueSend(positionMessage);
        }
    }
}
