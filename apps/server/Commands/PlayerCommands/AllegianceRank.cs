using System;
using System.Linq;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;

namespace ACE.Server.Commands.PlayerCommands;

public class AllegianceRank
{
    // allegiance rank
    [CommandHandler("allegiance-rank", AccessLevel.Player, CommandHandlerFlag.None, 0, "Show your current allegiance rank breakdown", "")]
    public static void HandleAllegianceRank(Session session, params string[] parameters)
    {
        var player = session.Player;
        var rank = player.AllegianceRank ?? 0;

        var followerRank = player.GetFollowerRank();
        var leadershipRank = player.GetLeadershipRank();

        var currentRankFollowers = player.GetCurrentRankFollowers();
        var nextRankFollowers = player.GetNextRankFollowers();

        var currentRankLeadership = player.GetCurrentRankLeadership();
        var nextRankLeadership = player.GetNextRankLeadership();

        CommandHandlerHelper.WriteOutputInfo(
            session,
            $"Allegiance Rank: {rank}\n" +
            $" - Follower Rank: {followerRank}, for having at least {currentRankFollowers} unique followers. Next rank at {nextRankFollowers} unique followers. " +
            $"(The 'unique follower' value of a character is determined by its level relative to the total amount of character levels on the same account)\n" +
            $" - Leadership Bonus Rank: {leadershipRank}, for having at least {currentRankLeadership} leadership skill. Next bonus rank at {nextRankLeadership} leadership skill."
        );

        player.FollowerRankContributions.Clear();

        var uniqueFollowers = Math.Round(player.GetFollowerAllegianceRankContributions(player.AllegianceNode), 2);
        var ordered = player.FollowerRankContributions.OrderByDescending(x => x.Value);

        CommandHandlerHelper.WriteOutputInfo(
            session,
            $"Current Unique Followers: {uniqueFollowers}"
        );

        foreach (var followerNameAndContribution in ordered)
        {
            CommandHandlerHelper.WriteOutputInfo(
                session,
                $" - {followerNameAndContribution.Key}: {followerNameAndContribution.Value}"
            );
        }
    }
}
