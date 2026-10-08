using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using ACE.Entity.Enum.Properties;
using ACE.Server.Entity;

namespace ACE.Server.Arena;

/// <summary>
/// A kind of rated duel, which has a rating and a record of its own: how many fight on each side (1v1, 2v2, ...),
/// and whether it was scaled
/// </summary>
public readonly record struct ArenaBoard(int Size, bool Scaled)
{
    public static readonly ArenaBoard OneOnOne = new ArenaBoard(1, false);

    public static readonly ArenaBoard OneOnOneScaled = new ArenaBoard(1, true);

    /// <summary>
    /// "1v1", "3v3 scaled"
    /// </summary>
    public string Name => $"{Size}v{Size}{(Scaled ? " scaled" : "")}";

    /// <summary>
    /// The size of a board as it is typed: "2v2". Just a number ("2") is taken too.
    /// </summary>
    public static bool TryParseSize(string text, out int size)
    {
        size = 0;

        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var parts = text.Trim().ToLowerInvariant().Split('v');

        if (parts.Length > 2 || (parts.Length == 2 && parts[0] != parts[1]))
        {
            return false;
        }

        return int.TryParse(parts[0], out size) && size >= 1 && size <= Fellowship.MaxFellows;
    }

    public override string ToString() => Name;
}

/// <summary>
/// A character's rating and record on one board
/// </summary>
public sealed class ArenaStanding
{
    public int Rating { get; set; } = ArenaElo.StartingRating;

    public int Wins { get; set; }

    public int Losses { get; set; }

    public int Draws { get; set; }

    [JsonIgnore]
    public int Duels => Wins + Losses + Draws;
}

/// <summary>
/// Where a character's boards are kept. The 1v1 boards have properties of their own (ArenaRating, ArenaWins, ...,
/// ArenaScaledRating, ...), which they had before there were teams. Every team board is kept in ArenaTeamBoards,
/// as JSON keyed by the board's name, so that a new size needs nothing new on the character.
/// </summary>
public static class ArenaBoards
{
    private static (PropertyInt Rating, PropertyInt Wins, PropertyInt Losses, PropertyInt Draws) OneOnOneProperties(
        bool scaled
    ) =>
        scaled
            ? (
                PropertyInt.ArenaScaledRating,
                PropertyInt.ArenaScaledWins,
                PropertyInt.ArenaScaledLosses,
                PropertyInt.ArenaScaledDraws
            )
            : (PropertyInt.ArenaRating, PropertyInt.ArenaWins, PropertyInt.ArenaLosses, PropertyInt.ArenaDraws);

    public static ArenaStanding Get(IPlayer player, ArenaBoard board)
    {
        if (board.Size == 1)
        {
            var properties = OneOnOneProperties(board.Scaled);

            return new ArenaStanding
            {
                Rating = player.GetProperty(properties.Rating) ?? ArenaElo.StartingRating,
                Wins = player.GetProperty(properties.Wins) ?? 0,
                Losses = player.GetProperty(properties.Losses) ?? 0,
                Draws = player.GetProperty(properties.Draws) ?? 0
            };
        }

        return ParseTeamBoards(player.GetProperty(PropertyString.ArenaTeamBoards))
            .TryGetValue(board.Name, out var standing)
            ? standing
            : new ArenaStanding();
    }

    public static void Set(IPlayer player, ArenaBoard board, ArenaStanding standing)
    {
        if (board.Size == 1)
        {
            var properties = OneOnOneProperties(board.Scaled);

            player.SetProperty(properties.Rating, standing.Rating);
            player.SetProperty(properties.Wins, standing.Wins);
            player.SetProperty(properties.Losses, standing.Losses);
            player.SetProperty(properties.Draws, standing.Draws);
            return;
        }

        var boards = ParseTeamBoards(player.GetProperty(PropertyString.ArenaTeamBoards));
        boards[board.Name] = standing;

        player.SetProperty(PropertyString.ArenaTeamBoards, WriteTeamBoards(boards));
    }

    /// <summary>
    /// Takes a character back to where they started on a board: the starting rating and no record
    /// </summary>
    public static void Reset(IPlayer player, ArenaBoard board)
    {
        Set(player, board, new ArenaStanding());
    }

    /// <summary>
    /// Whether an admin has taken the character off the rankings (/arena top)
    /// </summary>
    public static bool IsExcluded(IPlayer player) => player.GetProperty(PropertyBool.ArenaRankingExcluded) ?? false;

    public static void SetExcluded(IPlayer player, bool excluded)
    {
        if (excluded)
        {
            player.SetProperty(PropertyBool.ArenaRankingExcluded, true);
        }
        else
        {
            player.RemoveProperty(PropertyBool.ArenaRankingExcluded);
        }
    }

    /// <summary>
    /// Every board a character has fought a rated duel on, smallest first and raw before scaled
    /// </summary>
    public static List<(ArenaBoard Board, ArenaStanding Standing)> All(IPlayer player)
    {
        var all = new List<(ArenaBoard, ArenaStanding)>();

        foreach (var board in new[] { ArenaBoard.OneOnOne, ArenaBoard.OneOnOneScaled })
        {
            var standing = Get(player, board);

            if (standing.Duels > 0)
            {
                all.Add((board, standing));
            }
        }

        foreach (var (name, standing) in ParseTeamBoards(player.GetProperty(PropertyString.ArenaTeamBoards)))
        {
            if (TryParseName(name, out var board) && standing.Duels > 0)
            {
                all.Add((board, standing));
            }
        }

        return all.OrderBy(b => b.Item1.Size).ThenBy(b => b.Item1.Scaled).ToList();
    }

    private static bool TryParseName(string name, out ArenaBoard board)
    {
        board = default;

        var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (words.Length is < 1 or > 2 || (words.Length == 2 && words[1] != "scaled"))
        {
            return false;
        }

        if (!ArenaBoard.TryParseSize(words[0], out var size))
        {
            return false;
        }

        board = new ArenaBoard(size, words.Length == 2);
        return true;
    }

    /// <summary>
    /// The team boards in ArenaTeamBoards. Something that can't be read is taken as no boards, rather than losing the duel being recorded.
    /// </summary>
    internal static Dictionary<string, ArenaStanding> ParseTeamBoards(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, ArenaStanding>();
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, ArenaStanding>>(json)
                ?? new Dictionary<string, ArenaStanding>();
        }
        catch (JsonException)
        {
            return new Dictionary<string, ArenaStanding>();
        }
    }

    internal static string WriteTeamBoards(Dictionary<string, ArenaStanding> boards)
    {
        return JsonSerializer.Serialize(boards);
    }
}
