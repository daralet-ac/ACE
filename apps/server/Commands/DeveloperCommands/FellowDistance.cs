using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.Commands.DeveloperCommands;

public class FellowDistance
{
    [CommandHandler(
        "fellow-dist",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        "Shows distance to each fellowship member"
    )]
    public static void HandleFellowDist(Session session, params string[] parameters)
    {
        var player = session.Player;

        var fellowship = player.Fellowship;

        if (fellowship == null)
        {
            session.Network.EnqueueSend(
                new GameMessageSystemChat("You must be in a fellowship to use this command.", ChatMessageType.Broadcast)
            );
            return;
        }

        var fellows = fellowship.GetFellowshipMembers();

        foreach (var fellow in fellows.Values)
        {
            var dist2d = session.Player.Location.Distance2D(fellow.Location);
            var dist3d = session.Player.Location.DistanceTo(fellow.Location);

            var scalar = session.Player.Fellowship.GetDistanceScalar(session.Player, fellow, XpType.Kill);

            session.Network.EnqueueSend(
                new GameMessageSystemChat(
                    $"{fellow.Name} | 2d: {dist2d:N0} | 3d: {dist3d:N0} | Scalar: {scalar:N0}",
                    ChatMessageType.Broadcast
                )
            );
        }
    }
}
