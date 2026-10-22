using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ACE.Server.Lfg;

/// <summary>
/// Reads the words after "lfg" or "lfm" into an LfgActivity:
///
///   dungeon [level] [d5]   any capstone (lfg: up to that level, and at least that difficulty)
///   capstone [level] [d5]  the same
///   gmg [d10]              one capstone, by any of its names (LfgCapstones), at that difficulty (d5+ for at least, d5-10 a range)
///   leveling               a group to level with
///   quest [town]           quests (in that town)
///   anything else          free text, matched by its words
///
/// An lfm can end with "auto" (the default: players who match join the fellowship at once) or "manual" (its leader is asked first).
/// </summary>
public static class LfgParser
{
    private static readonly Regex DifficultyToken = new(@"^d(\d{1,2})(\+|-(\d{1,2}))?$", RegexOptions.IgnoreCase);

    private static readonly HashSet<string> AnyCapstoneWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "dungeon",
        "dungeons",
        "capstone",
        "capstones",
    };

    private static readonly HashSet<string> LevelingWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "leveling",
        "levelling",
        "level",
        "xp",
        "exp",
    };

    private static readonly HashSet<string> QuestWords = new(StringComparer.OrdinalIgnoreCase) { "quest", "quests" };

    /// <summary>
    /// Left out when free text is matched, so "the olthoi" matches "olthoi"
    /// </summary>
    private static readonly HashSet<string> IgnoredWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "a",
        "an",
        "and",
        "at",
        "for",
        "in",
        "of",
        "the",
        "to",
    };

    public const int MaxDifficulty = 20;

    /// <summary>
    /// Lower case, without apostrophes, one space between words
    /// </summary>
    public static string Normalize(string text)
    {
        var words = (text ?? "")
            .ToLowerInvariant()
            .Replace("'", "")
            .Replace("’", "")
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return string.Join(" ", words);
    }

    /// <summary>
    /// The words free text and quests are matched by
    /// </summary>
    public static IReadOnlyList<string> Words(string text)
    {
        return Normalize(text).Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(w => !IgnoredWords.Contains(w)).Distinct().ToList();
    }

    /// <param name="words">What came after "lfg" or "lfm"</param>
    /// <param name="autoJoin">For an lfm: false if it ended with "manual"</param>
    /// <param name="error">Why it couldn't be read, when null is returned</param>
    public static LfgActivity Parse(IReadOnlyList<string> words, bool lfm, out bool autoJoin, out string error)
    {
        autoJoin = true;
        error = null;

        var tokens = words.Where(w => !string.IsNullOrWhiteSpace(w)).Select(w => w.Trim()).ToList();

        if (lfm && tokens.Count > 0)
        {
            var last = tokens[^1].ToLowerInvariant();

            if (last is "auto" or "autojoin")
            {
                tokens.RemoveAt(tokens.Count - 1);
            }
            else if (last is "manual" or "noauto" or "ask")
            {
                autoJoin = false;
                tokens.RemoveAt(tokens.Count - 1);
            }
        }

        if (tokens.Count == 0)
        {
            error = "Say what for: a capstone (gmg d10), dungeon, leveling, quest <town>, or anything else.";
            return null;
        }

        var first = tokens[0];

        if (AnyCapstoneWords.Contains(first))
        {
            return ParseCapstone(null, tokens.Skip(1).ToList(), lfm, out error);
        }

        if (tokens.Count == 1 && LevelingWords.Contains(first))
        {
            return new LfgActivity { Kind = LfgActivityKind.Leveling };
        }

        if (QuestWords.Contains(first))
        {
            var town = string.Join(" ", tokens.Skip(1));

            return new LfgActivity
            {
                Kind = LfgActivityKind.Quest,
                Text = town,
                Words = Words(town),
            };
        }

        // a capstone's name, then its difficulty: "green mire grave d10"
        var nameEnd = tokens.Count;

        while (nameEnd > 1 && DifficultyToken.IsMatch(tokens[nameEnd - 1]))
        {
            nameEnd--;
        }

        var capstone = LfgCapstones.Find(string.Join(" ", tokens.Take(nameEnd)));

        if (capstone != null)
        {
            return ParseCapstone(capstone, tokens.Skip(nameEnd).ToList(), lfm, out error);
        }

        var text = string.Join(" ", tokens);
        var textWords = Words(text);

        if (textWords.Count == 0)
        {
            error = "Say what for: a capstone (gmg d10), dungeon, leveling, quest <town>, or anything else.";
            return null;
        }

        return new LfgActivity
        {
            Kind = LfgActivityKind.Other,
            Text = text,
            Words = textWords,
        };
    }

    /// <param name="dungeon">null for any capstone</param>
    /// <param name="rest">The words after the dungeon: a difficulty, and (lfg for any capstone) a level</param>
    private static LfgActivity ParseCapstone(LfgCapstone dungeon, List<string> rest, bool lfm, out string error)
    {
        error = null;

        int? maxLevel = null;
        int? difficultyMin = null;
        int? difficultyMax = null;
        var difficultyGiven = false;

        foreach (var token in rest)
        {
            var difficulty = DifficultyToken.Match(token);

            if (difficulty.Success && !difficultyGiven)
            {
                difficultyGiven = true;

                var low = int.Parse(difficulty.Groups[1].Value);
                int? high = difficulty.Groups[3].Success ? int.Parse(difficulty.Groups[3].Value) : null;
                var atLeast = difficulty.Groups[2].Value == "+";

                if (low > MaxDifficulty || high > MaxDifficulty || high < low)
                {
                    error = $"Difficulty goes from d0 to d{MaxDifficulty}.";
                    return null;
                }

                if (lfm && (atLeast || high != null))
                {
                    error = "Say the one difficulty your fellowship will do, like d10.";
                    return null;
                }

                difficultyMin = low;

                // "dungeon d5" is at least 5, and "gmg d10" exactly 10
                difficultyMax = high ?? (atLeast || (dungeon == null && !lfm) ? null : low);
                continue;
            }

            if (!lfm && dungeon == null && maxLevel == null && int.TryParse(token, out var level) && level > 0)
            {
                maxLevel = level;
                continue;
            }

            error = lfm || dungeon != null
                ? $"I didn't understand \"{token}\". Add a difficulty like d10, or nothing."
                : $"I didn't understand \"{token}\". Add a level like 30, a difficulty like d5, or nothing.";
            return null;
        }

        if (lfm)
        {
            difficultyMin ??= 0;
            difficultyMax = difficultyMin;
        }

        return new LfgActivity
        {
            Kind = LfgActivityKind.Capstone,
            Dungeon = dungeon,
            MaxLevel = maxLevel,
            DifficultyMin = difficultyMin,
            DifficultyMax = difficultyMax,
        };
    }
}
