using System.Collections.Generic;

namespace ACE.Server.Lfg;

public enum LfgActivityKind
{
    Capstone,
    Leveling,
    Quest,
    Other,
}

/// <summary>
/// What a player is looking for a group for (lfg), or what a fellowship wants more players for (lfm). See LfgParser.
/// </summary>
public sealed class LfgActivity
{
    public LfgActivityKind Kind { get; init; }

    /// <summary>
    /// A capstone, or null for any capstone
    /// </summary>
    public LfgCapstone Dungeon { get; init; }

    /// <summary>
    /// lfg for any capstone: the highest capstone level wanted (null for up to the player's own)
    /// </summary>
    public int? MaxLevel { get; init; }

    /// <summary>
    /// The Dungeon: Difficulty tier. For lfm both are the one the fellowship will do (0 for none). For lfg the range wanted, either
    /// end null for no limit.
    /// </summary>
    public int? DifficultyMin { get; init; }

    public int? DifficultyMax { get; init; }

    /// <summary>
    /// Quest and Other: the words as typed (the town, or the free text), and as matched (LfgParser.Words)
    /// </summary>
    public string Text { get; init; } = "";

    public IReadOnlyList<string> Words { get; init; } = new List<string>();

    /// <summary>
    /// For an lfm: the difficulty the fellowship will do, 0 for none
    /// </summary>
    public int Difficulty => DifficultyMin ?? 0;

    /// <summary>
    /// What it is, in a few words, for chat: "Green Mire Grave d10", "any capstone up to level 30, d5+", "Cragstone quests"
    /// </summary>
    public string Describe(bool lfm)
    {
        switch (Kind)
        {
            case LfgActivityKind.Capstone:
                // an lfm never has a MaxLevel
                var what = Dungeon != null
                    ? Dungeon.Name
                    : "any capstone" + (MaxLevel != null ? $" up to level {MaxLevel}" : "");
                var difficulty = DescribeDifficulty(lfm);
                return difficulty.Length == 0 ? what : what + (Dungeon == null && !lfm && MaxLevel != null ? ", " : " ") + difficulty;
            case LfgActivityKind.Leveling:
                return "leveling";
            case LfgActivityKind.Quest:
                return Text.Length == 0 ? "quests" : $"{Text} quests";
            default:
                return Text;
        }
    }

    /// <summary>
    /// What someone looking for a group types to be matched with this lfm: "gmg d10"
    /// </summary>
    public string JoinWords()
    {
        switch (Kind)
        {
            case LfgActivityKind.Capstone:
                var what = Dungeon?.Short ?? "dungeon";
                return Difficulty > 0 ? $"{what} d{Difficulty}" : what;
            case LfgActivityKind.Leveling:
                return "leveling";
            case LfgActivityKind.Quest:
                return Text.Length == 0 ? "quest" : $"quest {Text}";
            default:
                return Text;
        }
    }

    private string DescribeDifficulty(bool lfm)
    {
        if (lfm)
        {
            return Difficulty > 0 ? $"d{Difficulty}" : "";
        }

        if (DifficultyMin == null && DifficultyMax == null)
        {
            return "";
        }

        if (DifficultyMin == DifficultyMax)
        {
            return $"d{DifficultyMin}";
        }

        if (DifficultyMax == null)
        {
            return $"d{DifficultyMin}+";
        }

        return $"d{DifficultyMin ?? 0}-{DifficultyMax}";
    }
}
