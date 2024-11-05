using System;
using System.Linq;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.WorldObjects;

namespace ACE.Server.Commands.DeveloperCommands;

public class FellowInfo
{
    [CommandHandler(
        "fellow-info",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        "Shows debug info for fellowships."
    )]
    public static void HandleFellowInfo(Session session, params string[] parameters)
    {
        var player = CommandHandlerHelper.GetLastAppraisedObject(session) as Player;

        if (player == null)
        {
            player = session.Player;
        }

        var fellowship = player.Fellowship;

        if (fellowship == null)
        {
            session.Network.EnqueueSend(
                new GameMessageSystemChat(
                    "Player target must be in a fellowship to use this command.",
                    ChatMessageType.Broadcast
                )
            );
            return;
        }

        var fellows = fellowship.GetFellowshipMembers();

        //var levelSum = fellows.Values.Select(f => f.Level.Value).Sum();
        var levelXPSum = fellows.Values.Select(f => f.GetXPToNextLevel(f.Level.Value)).Sum();

        // this should match up with the client
        foreach (var fellow in fellows.Values.OrderBy(f => f.Level))
        {
            //var levelScale = (double)fellow.Level.Value / levelSum;
            var levelXPScale = (double)fellow.GetXPToNextLevel(fellow.Level.Value) / levelXPSum;

            //session.Network.EnqueueSend(new GameMessageSystemChat($"{fellow.Name}: {Math.Round(levelScale * 100, 2)}% / {Math.Round(levelXPScale * 100, 2)}%", ChatMessageType.Broadcast));
            session.Network.EnqueueSend(
                new GameMessageSystemChat(
                    $"{fellow.Name}: {Math.Round(levelXPScale * 100, 2)}%",
                    ChatMessageType.Broadcast
                )
            );
        }

        session.Network.EnqueueSend(new GameMessageSystemChat($"----------", ChatMessageType.Broadcast));

        session.Network.EnqueueSend(
            new GameMessageSystemChat($"ShareXP: {fellowship.ShareXP}", ChatMessageType.Broadcast)
        );
        session.Network.EnqueueSend(
            new GameMessageSystemChat($"EvenShare: {fellowship.EvenShare}", ChatMessageType.Broadcast)
        );

        session.Network.EnqueueSend(new GameMessageSystemChat($"Distance scale:", ChatMessageType.Broadcast));

        foreach (var fellow in fellows.Values)
        {
            var dist = player.Location.Distance2D(fellow.Location);

            var distanceScalar = fellowship.GetDistanceScalar(player, fellow, XpType.Kill);

            session.Network.EnqueueSend(
                new GameMessageSystemChat(
                    $"{fellow.Name}: {Math.Round(dist):N0} ({distanceScalar:F2}) - {fellow.Location}",
                    ChatMessageType.Broadcast
                )
            );
        }
    }
}
