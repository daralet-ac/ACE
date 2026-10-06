using System;

namespace ACE.Server.Arena;

/// <summary>
/// The arena rating: an Elo rating, which every character starts with and which only rated duels change.
/// The winner takes points from the loser, more of them the less the winner was expected to win.
/// </summary>
public static class ArenaElo
{
    public const int StartingRating = 1400;

    /// <summary>
    /// The chance a player with this rating had of beating one with the other rating, from 0 to 1
    /// </summary>
    public static double ExpectedScore(int rating, int opponentRating)
    {
        return 1.0 / (1.0 + Math.Pow(10.0, (opponentRating - rating) / 400.0));
    }

    /// <summary>
    /// The ratings of the winner and the loser after a duel. The points the winner gains are the points the loser loses
    /// (but a rating never goes below 0).
    /// </summary>
    /// <param name="k">How many points an upset is worth at most (the K-factor)</param>
    public static (int Winner, int Loser) Rate(int winnerRating, int loserRating, int k)
    {
        var change = (int)
            Math.Round(k * (1.0 - ExpectedScore(winnerRating, loserRating)), MidpointRounding.AwayFromZero);

        // a win is never worth nothing, even against someone far below
        if (k > 0 && change < 1)
        {
            change = 1;
        }

        return (winnerRating + change, Math.Max(0, loserRating - change));
    }
}
