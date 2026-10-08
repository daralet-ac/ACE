using System;
using System.Collections.Generic;
using System.Linq;
using ACE.Entity.Enum;
using ACE.Server.Arena;
using ACE.Server.Commands.Handlers;
using ACE.Server.Entity;
using ACE.Server.Managers;
using ACE.Server.Network;
using ACE.Server.WorldObjects;

namespace ACE.Server.Commands.PlayerCommands;

/// <summary>
/// /arena: duels between players, in an instance of their own. See ArenaManager and docs/arena.md.
/// </summary>
public class ArenaCommand
{
    private const string Usage =
        "arena [queue [levels] [scaled] [unrated] [fellowship] | challenge <name> [scaled] [unrated] [fellowship] | leave | stats [name] | top [2v2] [scaled] | maps]\n"
        + "  queue [levels]: waits for an opponent. Give a number of levels to only be matched with someone within that many levels of you\n"
        + "  challenge <name>: asks a player to a duel\n"
        + "  scaled: a scaled duel, in which the higher-level fighter fights at the other one's level (level 10 and up). Without it, a raw duel at your own levels\n"
        + "  unrated: a duel that changes no rating and no record. The queue only pairs you with someone who asked for the same kind of duel\n"
        + "  fellowship: (fellowship leaders) your whole fellowship, against a fellowship of the same size: the one <name> is in, or one from the queue. Everyone on both sides is asked\n"
        + "  leave: leaves the queue (with your fellowship, if it is waiting), calls off a duel that has not begun, or gives up the one you are fighting\n"
        + "  stats [name]: your arena ratings and records, on every board you have fought on, or someone else's\n"
        + "  top [2v2] [scaled]: the best arena ratings on a board: 1v1, 2v2, 3v3... raw or scaled (1v1 raw if you say nothing)\n"
        + "  maps: the arenas duels are fought in\n"
        + "Duels are fought as player killer lites in an arena of your own. Nobody loses anything by being defeated, "
        + "and you go back to exactly where you were afterwards. Only non-player killers and player killer lites can duel.";

    private const string StaffUsage = "\nStaff: arena list | arena cancel <duel>";

    private const string RankingUsage =
        "\nAdmin: arena reset <name> [2v2] [scaled] | arena restore <name> | arena unrank <name> | arena rerank <name>\n"
        + "  reset <name>: puts a character's rating back to the start and clears their record, on every board, or only on the one named\n"
        + "  restore <name>: gives back what the last resets took, if one was a mistake\n"
        + "  unrank <name>: takes a character off /arena top (their rating and record are kept)\n"
        + "  rerank <name>: puts them back on it";

    private const string AdminUsage =
        "\nAdmin: /modifybool arena_dueling_enabled false turns the whole arena off at once (true turns it back on). "
        + "/modifylong arena_dueling_minimum_level <level> sets the lowest level that can duel. /showprops lists the other arena_ settings.";

    private const AccessLevel StaffLevel = AccessLevel.Sentinel;

    [CommandHandler(
        "arena",
        AccessLevel.Player,
        CommandHandlerFlag.RequiresWorld,
        0,
        "Duel other players in the arena.",
        Usage
    )]
    public static void HandleArena(Session session, params string[] parameters)
    {
        var player = session.Player;
        var subcommand = parameters.Length > 0 ? parameters[0].ToLowerInvariant() : "";
        var rest = string.Join(" ", parameters.Skip(1)).Trim();
        var staff = session.AccessLevel >= StaffLevel;
        var admin = session.AccessLevel >= AccessLevel.Admin;

        switch (subcommand)
        {
            case "queue":
            case "join":
                Queue(player, parameters.Skip(1));
                break;

            case "challenge":
            case "duel":
                Challenge(player, parameters.Skip(1));
                break;

            case "leave":
                ArenaManager.Leave(player);
                break;

            case "stats":
                Stats(player, rest);
                break;

            case "top":
                Top(player, rest);
                break;

            case "maps":
                Maps(player);
                break;

            case "list" when staff:
                foreach (var line in ArenaManager.DescribeAll())
                {
                    player.SendMessage(line, ChatMessageType.System);
                }
                break;

            case "cancel" when staff:
                if (!int.TryParse(rest.TrimStart('#'), out var id) || !ArenaManager.CallOff(id, player.Name))
                {
                    player.SendMessage(
                        "There is no such duel going on. /arena list shows them.",
                        ChatMessageType.System
                    );
                }
                break;

            case "reset" when admin:
                Reset(player, rest);
                break;

            case "restore" when admin:
                Restore(player, rest);
                break;

            case "unrank" when admin:
                SetExcluded(player, rest, true);
                break;

            case "rerank" when admin:
                SetExcluded(player, rest, false);
                break;

            default:
                player.SendMessage(
                    $"Usage: {Usage}{(staff ? StaffUsage : "")}{(admin ? AdminUsage + RankingUsage : "")}",
                    ChatMessageType.System
                );

                if (ArenaManager.MinimumLevel > 1)
                {
                    player.SendMessage(
                        $"You have to be at least level {ArenaManager.MinimumLevel} to duel.",
                        ChatMessageType.System
                    );
                }

                player.SendMessage(ArenaManager.Status(player));
                break;
        }
    }

    /// <summary>
    /// What kind of duel was asked for
    /// </summary>
    internal readonly record struct DuelOptions(bool Scaled, bool Unrated, bool Fellowship);

    /// <summary>
    /// Takes the words that say what kind of duel is wanted ("scaled", "unrated", "fellowship") out of what was typed, and returns the rest.
    /// For a name, only the words at the end count, so a name that has one of them in it still works.
    /// </summary>
    internal static List<string> TakeDuelOptions(IEnumerable<string> words, bool onlyAtTheEnd, out DuelOptions options)
    {
        var rest = words.Where(w => !string.IsNullOrWhiteSpace(w)).ToList();
        bool scaled = false,
            unrated = false,
            fellowship = false;

        for (var i = rest.Count - 1; i >= 0; i--)
        {
            switch (rest[i].ToLowerInvariant())
            {
                case "scaled":
                    scaled = true;
                    break;

                case "unrated":
                    unrated = true;
                    break;

                case "fellowship":
                case "fellow":
                case "fellows":
                    fellowship = true;
                    break;

                default:
                    if (onlyAtTheEnd)
                    {
                        i = -1;
                    }

                    continue;
            }

            rest.RemoveAt(i);
        }

        options = new DuelOptions(scaled, unrated, fellowship);
        return rest;
    }

    private static void Queue(Player player, IEnumerable<string> words)
    {
        var rest = TakeDuelOptions(words, onlyAtTheEnd: false, out var options);
        int? band = null;

        if (rest.Count > 0)
        {
            if (rest.Count > 1 || !int.TryParse(rest[0], out var parsed) || parsed < 0)
            {
                player.SendMessage(
                    "/arena queue [levels] [scaled] [unrated] [fellowship]: /arena queue 10 matches you with someone within 10 levels of you."
                );
                return;
            }

            band = parsed;
        }

        if (options.Fellowship)
        {
            ArenaManager.JoinQueueWithFellowship(player, band, options.Scaled, rated: !options.Unrated);
        }
        else
        {
            ArenaManager.JoinQueue(player, band, options.Scaled, rated: !options.Unrated);
        }
    }

    private static void Challenge(Player player, IEnumerable<string> words)
    {
        var name = string.Join(" ", TakeDuelOptions(words, onlyAtTheEnd: true, out var options));

        if (name.Length == 0)
        {
            player.SendMessage("Who do you want to challenge? /arena challenge <name> [scaled] [unrated] [fellowship]");
            return;
        }

        if (options.Fellowship)
        {
            ArenaManager.ChallengeFellowship(player, name, options.Scaled, rated: !options.Unrated);
        }
        else
        {
            ArenaManager.Challenge(player, name, options.Scaled, rated: !options.Unrated);
        }
    }

    private static void Stats(Player player, string name)
    {
        IPlayer character = player;

        if (name.Length > 0)
        {
            character = PlayerManager.FindByName(name);

            if (character == null)
            {
                player.SendMessage($"There is no character called {name}.");
                return;
            }
        }

        var whose = character == player ? "Your" : $"{character.Name}'s";
        var boards = ArenaBoards.All(character);

        if (boards.Count == 0)
        {
            player.SendMessage(
                $"{(character == player ? "You have" : $"{character.Name} has")} not fought a rated duel yet. Every arena rating starts at {ArenaElo.StartingRating}."
            );
            return;
        }

        player.SendMessage($"{whose} arena ratings:", ChatMessageType.System);

        foreach (var (board, standing) in boards)
        {
            player.SendMessage(
                $"{board.Name}: {standing.Rating}, from {standing.Wins} win{(standing.Wins == 1 ? "" : "s")}, {standing.Losses} loss{(standing.Losses == 1 ? "" : "es")} and {standing.Draws} draw{(standing.Draws == 1 ? "" : "s")}",
                ChatMessageType.System
            );
        }
    }

    /// <summary>
    /// Which board was asked for: "2v2", "scaled", "raw", in any order. 1v1 raw if nothing is said. Null if something else was said.
    /// </summary>
    internal static ArenaBoard? ParseBoard(string text)
    {
        var size = 1;
        var scaled = false;

        foreach (var word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (word.Equals("scaled", StringComparison.OrdinalIgnoreCase))
            {
                scaled = true;
            }
            else if (word.Equals("raw", StringComparison.OrdinalIgnoreCase))
            {
                scaled = false;
            }
            else if (!ArenaBoard.TryParseSize(word, out size))
            {
                return null;
            }
        }

        return new ArenaBoard(size, scaled);
    }

    private static void Top(Player player, string which)
    {
        if (ParseBoard(which) is not ArenaBoard board)
        {
            player.SendMessage(
                "/arena top shows the best 1v1 ratings. Name a board for another: /arena top 3v3, /arena top scaled, /arena top 2v2 scaled."
            );
            return;
        }

        var best = PlayerManager
            .GetAllPlayers()
            .Select(p => (Character: p, Standing: ArenaBoards.Get(p, board)))
            .Where(p => p.Standing.Duels > 0 && !ArenaBoards.IsExcluded(p.Character))
            .OrderByDescending(p => p.Standing.Rating)
            .ThenBy(p => p.Character.Name, StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .ToList();

        if (best.Count == 0)
        {
            player.SendMessage($"Nobody has fought a rated {board.Name} duel in the arena yet.");
            return;
        }

        player.SendMessage($"The best {board.Name} arena ratings:", ChatMessageType.System);

        for (var i = 0; i < best.Count; i++)
        {
            var (character, standing) = best[i];

            player.SendMessage(
                $"{i + 1}. {character.Name}: {standing.Rating} ({standing.Wins}-{standing.Losses}-{standing.Draws})",
                ChatMessageType.System
            );
        }
    }

    /// <summary>
    /// /arena reset &lt;name&gt; [board]: the board is the last words, as in /arena top. Without one, every board is reset.
    /// </summary>
    private static void Reset(Player admin, string text)
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
        var boardWords = new List<string>();

        while (words.Count > 1 && ParseBoard(words[^1]) != null)
        {
            boardWords.Insert(0, words[^1]);
            words.RemoveAt(words.Count - 1);
        }

        var name = string.Join(" ", words);

        if (name.Length == 0)
        {
            admin.SendMessage("/arena reset <name> [2v2] [scaled]: puts a character's arena rating and record back to the start.");
            return;
        }

        var character = PlayerManager.FindByName(name);

        if (character == null)
        {
            admin.SendMessage($"There is no character called {name}.");
            return;
        }

        if (boardWords.Count > 0)
        {
            var board = ParseBoard(string.Join(" ", boardWords));

            if (board == null)
            {
                admin.SendMessage("That is not a board. Try 2v2, scaled or 3v3 scaled.");
                return;
            }

            var changed = ArenaBoards.Reset(character, board.Value);
            admin.SendMessage(
                changed
                    ? $"{character.Name}'s {board.Value.Name} arena rating is back to {ArenaElo.StartingRating}, with no record. /arena restore {character.Name} undoes it."
                    : $"{character.Name} has nothing to reset on {board.Value.Name}.",
                ChatMessageType.System
            );
            return;
        }

        var boards = ArenaBoards.All(character);

        foreach (var (board, _) in boards)
        {
            ArenaBoards.Reset(character, board);
        }

        admin.SendMessage(
            boards.Count == 0
                ? $"{character.Name} has no arena record to reset."
                : $"{character.Name}'s arena ratings are back to {ArenaElo.StartingRating} on {string.Join(", ", boards.Select(b => b.Board.Name))}, with no records. /arena restore {character.Name} undoes it.",
            ChatMessageType.System
        );
    }

    private static void Restore(Player admin, string name)
    {
        if (name.Length == 0)
        {
            admin.SendMessage("/arena restore <name>: gives back the arena ratings and records the last /arena reset took.");
            return;
        }

        var character = PlayerManager.FindByName(name);

        if (character == null)
        {
            admin.SendMessage($"There is no character called {name}.");
            return;
        }

        var restored = ArenaBoards.Restore(character);

        admin.SendMessage(
            restored.Count == 0
                ? $"There is nothing to restore for {character.Name}."
                : $"{character.Name}'s arena ratings and records are back on {string.Join(", ", restored.Select(b => b.Name))}.",
            ChatMessageType.System
        );
    }

    private static void SetExcluded(Player admin, string name, bool excluded)
    {
        var verb = excluded ? "unrank" : "rerank";

        if (name.Length == 0)
        {
            admin.SendMessage($"/arena {verb} <name>");
            return;
        }

        var character = PlayerManager.FindByName(name);

        if (character == null)
        {
            admin.SendMessage($"There is no character called {name}.");
            return;
        }

        if (ArenaBoards.IsExcluded(character) == excluded)
        {
            admin.SendMessage(
                $"{character.Name} is already {(excluded ? "off" : "on")} the arena rankings.",
                ChatMessageType.System
            );
            return;
        }

        ArenaBoards.SetExcluded(character, excluded);
        admin.SendMessage(
            $"{character.Name} is {(excluded ? "off" : "back on")} the arena rankings.",
            ChatMessageType.System
        );
    }

    private static void Maps(Player player)
    {
        var maps = ArenaMaps.Enabled;

        player.SendMessage(
            maps.Count == 0
                ? "There is no arena to duel in."
                : $"Duels are fought in {string.Join(", ", maps.Select(m => m.Description))}, picked at random."
        );
    }
}
