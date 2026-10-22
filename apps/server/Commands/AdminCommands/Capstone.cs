using System;
using System.Linq;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Entity;
using ACE.Server.Managers;
using ACE.Server.Network;
using ACE.Server.WorldObjects;

namespace ACE.Server.Commands.AdminCommands;

public class Capstone
{
    [CommandHandler(
        "capstone",
        AccessLevel.Admin,
        CommandHandlerFlag.RequiresWorld,
        0,
        "Capstone Dungeon Information Readout."
    )]
    public static void HandleCapstone(Session session, params string[] parameters)
    {
        // @capstone -- outputs a list of capstone dungeons with active instances and player counts
        // @capstone
        // @capstone blacklist add|remove <name>, @capstone blacklist list -- characters whose runs stay off the capstone leaderboard

        if (parameters.Length >= 1 && parameters[0].Equals("blacklist", StringComparison.OrdinalIgnoreCase))
        {
            RunBlacklist(parameters[1..], message => session.Player.SendMessage(message, ChatMessageType.System));
            return;
        }

        if (parameters.Length < 1)
        {
            session.Player.SendMessage("-----------Active Capstone Dungeons-----------", ChatMessageType.System);
            string[] capstones =
            {
                "Glenden Wood Dungeon",
                "Green Mire Grave",
                "Sand Shallow",
                "Manse of Panderlou",
                "Smugglers Hideaway",
                "Halls of the Helm",
                "Colier Mine",
                "Empyrean Garrison",
                "Grievous Vault",
                "Folthid Cellar",
                "Mines of Despair",
                "Gredaline Consulate",
                "Mage Academy",
                "Lugian Mines",
                "Mountain Fortress",
                "Xarabydun Researcher Halls"
            };
            foreach (var capstone in capstones)
            {
                var instanceList = Landblock.CapstoneDungeonLists(capstone);
                var output = "";
                foreach (var instance in instanceList)
                {
                    var active = LandblockManager.IsLoaded(instance);
                    if (active)
                    {
                        output += $"{instance}, ";
                    }
                }

                if (!string.IsNullOrEmpty(output))
                {
                    var outputMsg = $"{capstone}:   " + output + "\n";
                    session.Player.SendMessage(outputMsg, ChatMessageType.System);
                }
            }
            // the dungeons that are opened as a private instance for each fellowship, instead of as one of their copies
            foreach (var instance in InstanceManager.GetInstances())
            {
                if (instance.Template.Name.StartsWith("capstone:"))
                {
                    session.Player.SendMessage(
                        $"{instance.Template.Name.Substring("capstone:".Length)}:   instance {instance.Id}, {instance.MemberCount} player(s){(instance.IsClosing ? ", shutting down" : "")}\n",
                        ChatMessageType.System
                    );
                }
            }

            session.Player.SendMessage(
                "For more detailed information about a specific dungeon, use @capstone followed by the dungeon name.",
                ChatMessageType.System
            );
            session.Player.SendMessage(BlacklistUsage, ChatMessageType.System);
        }

        if (parameters.Length >= 1)
        {
            var input = string.Join(" ", parameters);

            var dungeonName = "";

            switch (input.ToLower())
            {
                case "glenden":
                case "glenden wood":
                case "glenden wood dungeon":
                case "gw":
                case "gwd":
                    dungeonName = "Glenden Wood Dungeon";
                    break;
                case "green mire grave":
                case "green mire":
                case "green":
                case "gmg":
                    dungeonName = "Green Mire Grave";
                    break;
                case "sand shallow":
                case "sand shallows":
                case "sand":
                case "shallow":
                    dungeonName = "Sand Shallow";
                    break;
                case "manse of panderlou":
                case "manse":
                case "panderlou":
                case "dungeon of shadows":
                    dungeonName = "Manse of Panderlou";
                    break;
                case "smuggler's hideaway":
                case "smugglers hideaway":
                case "smuggler's":
                case "smugglers":
                case "smuggler":
                    dungeonName = "Smugglers Hideaway";
                    break;
                case "halls of the helm":
                case "halls of helm":
                case "halls":
                    dungeonName = "Halls of the Helm";
                    break;
                case "colier mine":
                case "colier mines":
                case "mines of colier":
                case "colier":
                    dungeonName = "Colier Mine";
                    break;
                case "empyrean garrison":
                case "garrison":
                case "empyrean":
                    dungeonName = "Empyrean Garrison";
                    break;
                case "grievous vault":
                case "grievous":
                case "vault":
                    dungeonName = "Grievous Vault";
                    break;
                case "folthid cellar":
                case "folthid":
                case "cellar":
                    dungeonName = "Folthid Cellar";
                    break;
                case "mines of despair":
                case "despair":
                case "focusing stone":
                    dungeonName = "Mines of Despair";
                    break;
                case "gredaline consulate":
                case "gredaline":
                case "consulate":
                    dungeonName = "Gredaline Consulate";
                    break;
                case "mage academy":
                case "academy":
                case "mage":
                    dungeonName = "Mage Academy";
                    break;
                case "lugian mines":
                case "lugian":
                case "lugians":
                case "quarry":
                case "lugian quarry":
                    dungeonName = "Lugian Mines";
                    break;
                case "mountain fortress":
                case "mountain":
                case "fortress":
                case "hamud":
                    dungeonName = "Mountain Fortress";
                    break;
                // the Shattered Source, which is one instance of the residence and the boss room
                case "shattered source":
                case "shattered":
                case "ss":
                case "xarabydun researcher halls":
                case "xarabydun":
                case "researcher halls":
                case "residence":
                    dungeonName = "Xarabydun Researcher Halls";
                    break;
            }

            if (dungeonName.Length > 0)
            {
                var instanceList = Landblock.CapstoneDungeonLists(dungeonName);
                var instanceNum = 0;
                foreach (var instance in instanceList)
                {
                    var active = LandblockManager.IsLoaded(instance);
                    if (active)
                    {
                        var landblock = LandblockManager.GetLandblock(instance, false);

                        var activePlayers = "";
                        foreach (var activePlayer in landblock.ActivePlayers)
                        {
                            activePlayers += $"{activePlayer.Name}, ";
                        }

                        var fellowshipPlayers = "";
                        var landblockFellowship = landblock.CapstoneFellowship;
                        var landblockFellowshipCount = 0;
                        if (landblockFellowship != null)
                        {
                            foreach (var fellow in landblockFellowship.GetFellowshipMembers())
                            {
                                fellowshipPlayers += $"{fellow.Value.Name}, ";
                                landblockFellowshipCount++;
                            }
                        }

                        instanceNum++;
                        var dormant = landblock.IsDormant ? $"| Dormant: {Math.Round(landblock.timeDormant.TotalMinutes, 1)} min." : "";
                        var uptime = landblock.CapstoneUptime / 60;
                        session.Player.SendMessage(
                            $"----------{dungeonName}----------\n" +
                            $"Instance Number: {instanceNum} ({landblock.Id}) | Uptime: {Math.Round(uptime, 1)} min.   {dormant}\n" +
                            $"Active Players ({landblock.ActivePlayers.Count}): {activePlayers}\n" +
                            $"Players in Fellowship ({landblockFellowshipCount}): {fellowshipPlayers}",
                            ChatMessageType.System
                        );
                    }
                }
                if (instanceNum == 0)
                {
                    session.Player.SendMessage(
                        $"No instances of {dungeonName} are active at this time.",
                        ChatMessageType.System
                    );
                }
            }
        }
    }

    private const string BlacklistUsage =
        "@capstone blacklist add <name> | remove <name> | list (in the server console: capstoneblacklist ...): "
        + "a character on the blacklist keeps its runs (and any run it is in) off the website's capstone leaderboard.";

    internal enum BlacklistAction
    {
        Usage,
        Add,
        Remove,
        List,
    }

    /// <summary>
    /// What was typed after "@capstone blacklist": add and remove need a name (everything after the word, so names with
    /// spaces work), list does not, and anything else is a request for the usage.
    /// </summary>
    internal static (BlacklistAction Action, string Name) ParseBlacklist(string[] words)
    {
        if (words.Length == 0)
        {
            return (BlacklistAction.Usage, "");
        }

        var name = string.Join(" ", words.Skip(1)).Trim();

        return words[0].ToLowerInvariant() switch
        {
            "list" => (BlacklistAction.List, ""),
            "add" when name.Length > 0 => (BlacklistAction.Add, name),
            "remove" when name.Length > 0 => (BlacklistAction.Remove, name),
            _ => (BlacklistAction.Usage, ""),
        };
    }

    /// <summary>
    /// Does what "blacklist" was typed with (the words after it) and says what happened through say: to the admin's chat
    /// for @capstone in game, to the console for capstoneblacklist (CapstoneBlacklistConsole). Changes a character, so
    /// call it on the world thread.
    /// </summary>
    internal static void RunBlacklist(string[] words, Action<string> say)
    {
        var (action, name) = ParseBlacklist(words);

        switch (action)
        {
            case BlacklistAction.Add:
                SetBlacklisted(name, true, say);
                break;

            case BlacklistAction.Remove:
                SetBlacklisted(name, false, say);
                break;

            case BlacklistAction.List:
                var listed = PlayerManager
                    .GetAllPlayers()
                    .Where(CapstoneBlacklist.IsBlacklisted)
                    .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                say(
                    listed.Count == 0
                        ? "Nobody is on the capstone blacklist."
                        : $"On the capstone blacklist ({listed.Count}): {string.Join(", ", listed.Select(p => p.Name))}"
                );
                break;

            default:
                say(BlacklistUsage);
                break;
        }
    }

    private static void SetBlacklisted(string name, bool blacklisted, Action<string> say)
    {
        var character = PlayerManager.FindByName(name);

        if (character == null)
        {
            say($"There is no character called {name}.");
            return;
        }

        if (CapstoneBlacklist.IsBlacklisted(character) == blacklisted)
        {
            say($"{character.Name} is already {(blacklisted ? "" : "not ")}on the capstone blacklist.");
            return;
        }

        CapstoneBlacklist.Set(character, blacklisted);

        say(
            blacklisted
                ? $"{character.Name} is on the capstone blacklist: every capstone run they are in is hidden from the leaderboard (the runs are kept)."
                : $"{character.Name} is off the capstone blacklist: their capstone runs are on the leaderboard again."
        );
    }
}
