using System;
using System.Linq;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Managers;
using ACE.Server.Network;

namespace ACE.Server.Commands.AdminCommands;

public class Leaderboard
{
    [CommandHandler(
        "leaderboard",
        AccessLevel.Admin,
        CommandHandlerFlag.RequiresWorld,
        0,
        "Keeps characters off the website's general leaderboards.",
        "exclude <name> | include <name> | list"
    )]
    public static void HandleLeaderboard(Session session, params string[] parameters)
    {
        // @leaderboard exclude|include <name>, @leaderboard list -- characters kept off the general leaderboards
        RunExclusion(parameters, message => session.Player.SendMessage(message, ChatMessageType.System));
    }

    private const string Usage =
        "@leaderboard exclude <name> | include <name> | list (in the server console: leaderboardexclude ...): "
        + "an excluded character is left off the website's general leaderboards (level, deaths, allegiance rank, "
        + "town attunements, most dangerous creatures, trade skills). Arena and capstone have /arena unrank and @capstone blacklist.";

    internal enum ExclusionAction
    {
        Usage,
        Exclude,
        Include,
        List,
    }

    /// <summary>
    /// What was typed after "@leaderboard": exclude and include need a name (everything after the word, so names with
    /// spaces work), list does not, and anything else is a request for the usage.
    /// </summary>
    internal static (ExclusionAction Action, string Name) ParseExclusion(string[] words)
    {
        if (words.Length == 0)
        {
            return (ExclusionAction.Usage, "");
        }

        var name = string.Join(" ", words.Skip(1)).Trim();

        return words[0].ToLowerInvariant() switch
        {
            "list" => (ExclusionAction.List, ""),
            "exclude" when name.Length > 0 => (ExclusionAction.Exclude, name),
            "include" when name.Length > 0 => (ExclusionAction.Include, name),
            _ => (ExclusionAction.Usage, ""),
        };
    }

    /// <summary>
    /// Does what was typed after "@leaderboard" and says what happened through say: to the admin's chat in game, to the
    /// console for leaderboardexclude (LeaderboardExcludeConsole), to Discord for /leaderboard. Changes a character, so
    /// call it on the world thread.
    /// </summary>
    internal static void RunExclusion(string[] words, Action<string> say)
    {
        var (action, name) = ParseExclusion(words);

        switch (action)
        {
            case ExclusionAction.Exclude:
                SetExcluded(name, true, say);
                break;

            case ExclusionAction.Include:
                SetExcluded(name, false, say);
                break;

            case ExclusionAction.List:
                var listed = PlayerManager
                    .GetAllPlayers()
                    .Where(LeaderboardExclusion.IsExcluded)
                    .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                say(
                    listed.Count == 0
                        ? "Nobody is excluded from the leaderboards."
                        : $"Excluded from the leaderboards ({listed.Count}): {string.Join(", ", listed.Select(p => p.Name))}"
                );
                break;

            default:
                say(Usage);
                break;
        }
    }

    private static void SetExcluded(string name, bool excluded, Action<string> say)
    {
        var character = PlayerManager.FindByName(name);

        if (character == null)
        {
            say($"There is no character called {name}.");
            return;
        }

        if (LeaderboardExclusion.IsExcluded(character) == excluded)
        {
            say($"{character.Name} is already {(excluded ? "excluded from" : "on")} the leaderboards.");
            return;
        }

        LeaderboardExclusion.Set(character, excluded);

        say(
            excluded
                ? $"{character.Name} is excluded: they are left off the website's general leaderboards (nothing about the character is changed)."
                : $"{character.Name} is included again: they are back on the website's general leaderboards."
        );
    }
}
