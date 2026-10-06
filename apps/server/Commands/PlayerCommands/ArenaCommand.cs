using System;
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
        "arena [queue [levels] | challenge <name> | leave | stats [name] | top | maps]\n"
        + "  queue [levels]: waits for an opponent. Give a number of levels to only be matched with someone within that many levels of you\n"
        + "  challenge <name>: asks a player to a duel\n"
        + "  leave: leaves the queue, calls off a duel that has not begun, or gives up the one you are fighting\n"
        + "  stats [name]: your arena rating and record, or someone else's\n"
        + "  top: the best arena ratings\n"
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
                Queue(player, rest);
                break;

            case "challenge":
            case "duel":
                if (rest.Length == 0)
                {
                    player.SendMessage("Who do you want to challenge? /arena challenge <name>");
                    break;
                }

                ArenaManager.Challenge(player, rest);
                break;

            case "leave":
                ArenaManager.Leave(player);
                break;

            case "stats":
                Stats(player, rest);
                break;

            case "top":
                Top(player);
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

    private static void Queue(Player player, string levels)
    {
        int? band = null;

        if (levels.Length > 0)
        {
            if (!int.TryParse(levels, out var parsed) || parsed < 0)
            {
                player.SendMessage(
                    "The level band is a number of levels: /arena queue 10 matches you with someone within 10 levels of you."
                );
                return;
            }

            band = parsed;
        }

        ArenaManager.JoinQueue(player, band);
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

        var (wins, losses, draws) = ArenaManager.RecordOf(character);
        var whose = character == player ? "Your" : $"{character.Name}'s";

        player.SendMessage(
            $"{whose} arena rating is {ArenaManager.RatingOf(character)}, from {wins} win{(wins == 1 ? "" : "s")}, {losses} loss{(losses == 1 ? "" : "es")} and {draws} draw{(draws == 1 ? "" : "s")}."
        );
    }

    private static void Top(Player player)
    {
        var best = PlayerManager
            .GetAllPlayers()
            .Select(p => (Character: p, Record: ArenaManager.RecordOf(p)))
            .Where(p => p.Record.Wins + p.Record.Losses + p.Record.Draws > 0)
            .OrderByDescending(p => ArenaManager.RatingOf(p.Character))
            .ThenBy(p => p.Character.Name, StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .ToList();

        if (best.Count == 0)
        {
            player.SendMessage("Nobody has fought a duel in the arena yet.");
            return;
        }

        player.SendMessage("The best arena ratings:", ChatMessageType.System);

        for (var i = 0; i < best.Count; i++)
        {
            var (character, (wins, losses, draws)) = best[i];

            player.SendMessage(
                $"{i + 1}. {character.Name}: {ArenaManager.RatingOf(character)} ({wins}-{losses}-{draws})",
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
