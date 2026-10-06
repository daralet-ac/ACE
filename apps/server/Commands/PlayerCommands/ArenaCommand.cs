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
        "arena [queue [levels] [scaled] [unrated] | challenge <name> [scaled] [unrated] | leave | stats [name] | top [scaled] | maps]\n"
        + "  queue [levels]: waits for an opponent. Give a number of levels to only be matched with someone within that many levels of you\n"
        + "  challenge <name>: asks a player to a duel\n"
        + "  scaled: a scaled duel, in which the higher-level fighter fights at the other one's level (level 10 and up). Without it, a raw duel at your own levels\n"
        + "  unrated: a duel that changes no rating and no record. The queue only pairs you with someone who asked for the same kind of duel\n"
        + "  leave: leaves the queue, calls off a duel that has not begun, or gives up the one you are fighting\n"
        + "  stats [name]: your arena ratings and records (raw and scaled), or someone else's\n"
        + "  top [scaled]: the best arena ratings, raw or scaled\n"
        + "  maps: the arenas duels are fought in\n"
        + "Duels are fought as player killer lites in an arena of your own. Nobody loses anything by being defeated, "
        + "and you go back to exactly where you were afterwards. Only non-player killers and player killer lites can duel.";

    private const string StaffUsage = "\nStaff: arena list | arena cancel <duel>";

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

            default:
                player.SendMessage(
                    $"Usage: {Usage}{(staff ? StaffUsage : "")}{(admin ? AdminUsage : "")}",
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
    /// Takes the words that say what kind of duel is wanted ("scaled", "unrated") out of what was typed, and returns the rest.
    /// For a name, only the words at the end count, so a name that has one of them in it still works.
    /// </summary>
    internal static List<string> TakeDuelOptions(
        IEnumerable<string> words,
        bool onlyAtTheEnd,
        out bool scaled,
        out bool unrated
    )
    {
        var rest = words.Where(w => !string.IsNullOrWhiteSpace(w)).ToList();
        scaled = false;
        unrated = false;

        for (var i = rest.Count - 1; i >= 0; i--)
        {
            var word = rest[i].ToLowerInvariant();

            if (word == "scaled")
            {
                scaled = true;
            }
            else if (word == "unrated")
            {
                unrated = true;
            }
            else if (onlyAtTheEnd)
            {
                break;
            }
            else
            {
                continue;
            }

            rest.RemoveAt(i);
        }

        return rest;
    }

    private static void Queue(Player player, IEnumerable<string> words)
    {
        var rest = TakeDuelOptions(words, onlyAtTheEnd: false, out var scaled, out var unrated);
        int? band = null;

        if (rest.Count > 0)
        {
            if (rest.Count > 1 || !int.TryParse(rest[0], out var parsed) || parsed < 0)
            {
                player.SendMessage(
                    "/arena queue [levels] [scaled] [unrated]: /arena queue 10 matches you with someone within 10 levels of you."
                );
                return;
            }

            band = parsed;
        }

        ArenaManager.JoinQueue(player, band, scaled, rated: !unrated);
    }

    private static void Challenge(Player player, IEnumerable<string> words)
    {
        var name = string.Join(" ", TakeDuelOptions(words, onlyAtTheEnd: true, out var scaled, out var unrated));

        if (name.Length == 0)
        {
            player.SendMessage("Who do you want to challenge? /arena challenge <name> [scaled] [unrated]");
            return;
        }

        ArenaManager.Challenge(player, name, scaled, rated: !unrated);
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

        foreach (var board in new[] { ArenaBoard.Raw, ArenaBoard.Scaled })
        {
            var (wins, losses, draws) = ArenaManager.RecordOf(character, board);

            // the scaled board only once there is something on it
            if (board == ArenaBoard.Scaled && wins + losses + draws == 0)
            {
                continue;
            }

            player.SendMessage(
                $"{whose} {(board == ArenaBoard.Scaled ? "scaled" : "raw")} arena rating is {ArenaManager.RatingOf(character, board)}, from {wins} win{(wins == 1 ? "" : "s")}, {losses} loss{(losses == 1 ? "" : "es")} and {draws} draw{(draws == 1 ? "" : "s")}."
            );
        }
    }

    private static void Top(Player player, string which)
    {
        if (
            which.Length > 0
            && !which.Equals("scaled", StringComparison.OrdinalIgnoreCase)
            && !which.Equals("raw", StringComparison.OrdinalIgnoreCase)
        )
        {
            player.SendMessage("/arena top shows the best raw ratings, and /arena top scaled the best scaled ones.");
            return;
        }

        var board = which.Equals("scaled", StringComparison.OrdinalIgnoreCase) ? ArenaBoard.Scaled : ArenaBoard.Raw;
        var boardName = board == ArenaBoard.Scaled ? "scaled" : "raw";

        var best = PlayerManager
            .GetAllPlayers()
            .Select(p => (Character: p, Record: ArenaManager.RecordOf(p, board)))
            .Where(p => p.Record.Wins + p.Record.Losses + p.Record.Draws > 0)
            .OrderByDescending(p => ArenaManager.RatingOf(p.Character, board))
            .ThenBy(p => p.Character.Name, StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .ToList();

        if (best.Count == 0)
        {
            player.SendMessage($"Nobody has fought a rated {boardName} duel in the arena yet.");
            return;
        }

        player.SendMessage($"The best {boardName} arena ratings:", ChatMessageType.System);

        for (var i = 0; i < best.Count; i++)
        {
            var (character, (wins, losses, draws)) = best[i];

            player.SendMessage(
                $"{i + 1}. {character.Name}: {ArenaManager.RatingOf(character, board)} ({wins}-{losses}-{draws})",
                ChatMessageType.System
            );
        }
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
