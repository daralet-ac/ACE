using System;
using System.Linq;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using ACE.Server.TownEvents;

namespace ACE.Server.Commands.AdminCommands;

/// <summary>
/// @townevent: start, stop and watch town attacks (TownEventManager), and look at or set the towns' unrest and supply
/// </summary>
public class TownEventCommand
{
    private const string Usage =
        "@townevent start <town> [chest | nochest] | stop <town> | status | unrest [<town> <amount>] | supply [<town> [<amount> | level <level>]] | history [<town>] [<count>]. "
        + "start begins an attack on the town now, whatever its unrest and whether or not town_events_enabled is on; chest makes it "
        + "a raid on the town's Supply Chest, nochest rules one out. "
        + "status shows each attack and its defenders' scores, over-level kills included. unrest lists every town's unrest "
        + "against its threshold, or sets one town's. supply shows the towns' guard supply, or sets one town's (to an amount, "
        + "or to the start of a level) with no minimum hold. history lists the latest attacks from the town event log (10, up to 50).";

    [CommandHandler("townevent", AccessLevel.Admin, CommandHandlerFlag.RequiresWorld, 0, "Starts, stops and shows town attacks.", Usage)]
    public static void HandleTownEvent(Session session, params string[] parameters)
    {
        void Reply(string text)
        {
            foreach (var line in text.Split('\n'))
            {
                session.Player.SendMessage(line, ChatMessageType.System);
            }
        }

        var action = parameters.Length > 0 ? parameters[0].ToLowerInvariant() : "";
        var rest = parameters.Skip(1).ToList();

        switch (action)
        {
            case "status":
                TownEventManager.Enqueue(() => Reply(TownEventManager.Status()));
                return;

            case "start":
            case "stop":
            {
                // start <town> chest | start <town> nochest forces a chest raid or rules one out
                bool? chestRaid = null;
                if (action == "start" && rest.Count > 1 && rest[^1].ToLowerInvariant() is "chest" or "nochest")
                {
                    chestRaid = rest[^1].Equals("chest", StringComparison.OrdinalIgnoreCase);
                    rest = rest.Take(rest.Count - 1).ToList();
                }

                var town = TownEventManager.FindTown(string.Join(" ", rest));
                if (town == null)
                {
                    Reply($"No town called \"{string.Join(" ", rest)}\".");
                    return;
                }

                TownEventManager.Enqueue(() => Reply(action == "start" ? TownEventManager.Start(town, chestRaid) : TownEventManager.Stop(town)));
                return;
            }

            case "unrest":
            {
                if (rest.Count == 0)
                {
                    TownEventManager.Enqueue(() => Reply(TownEventManager.DescribeUnrest()));
                    return;
                }

                if (rest.Count < 2 || !double.TryParse(rest[^1], out var amount))
                {
                    Reply(Usage);
                    return;
                }

                var town = TownEventManager.FindTown(string.Join(" ", rest.Take(rest.Count - 1)));
                if (town == null)
                {
                    Reply($"No town called \"{string.Join(" ", rest.Take(rest.Count - 1))}\".");
                    return;
                }

                TownEventManager.Enqueue(() => Reply(TownEventManager.SetUnrest(town, amount)));
                return;
            }

            case "history":
            {
                // history [<town>] [<count>]
                var count = 10;
                if (rest.Count > 0 && int.TryParse(rest[^1], out var n))
                {
                    count = Math.Clamp(n, 1, 50);
                    rest = rest.Take(rest.Count - 1).ToList();
                }

                string town = null;
                if (rest.Count > 0)
                {
                    town = TownEventManager.FindTown(string.Join(" ", rest));
                    if (town == null)
                    {
                        Reply($"No town called \"{string.Join(" ", rest)}\".");
                        return;
                    }
                }

                // the log is in the database: read it off the world thread
                _ = System.Threading.Tasks.Task.Run(() => Reply(TownEventRecorder.DescribeHistory(count, town)));
                return;
            }

            case "supply":
            {
                if (rest.Count == 0)
                {
                    TownEventManager.Enqueue(() => Reply(TownEventManager.DescribeSupply()));
                    return;
                }

                // supply <town> | supply <town> <amount> | supply <town> level <level>
                var byLevel = rest.Count >= 3 && rest[^2].Equals("level", StringComparison.OrdinalIgnoreCase);
                var hasAmount = double.TryParse(rest[^1], out var amount);
                var nameParts = byLevel ? rest.Count - 2 : hasAmount ? rest.Count - 1 : rest.Count;

                var town = TownEventManager.FindTown(string.Join(" ", rest.Take(nameParts)));
                if (town == null)
                {
                    Reply($"No town called \"{string.Join(" ", rest.Take(nameParts))}\".");
                    return;
                }

                if (byLevel && (!hasAmount || amount < 0 || amount > TownSupply.MaxLevel))
                {
                    Reply($"The level is 0 to {TownSupply.MaxLevel}.");
                    return;
                }

                if (!hasAmount)
                {
                    TownEventManager.Enqueue(() => Reply(TownEventManager.DescribeSupply(town)));
                    return;
                }

                TownEventManager.Enqueue(() =>
                    Reply(TownEventManager.SetSupply(town, byLevel ? TownSupply.GetThreshold(town, (int)amount) : amount))
                );
                return;
            }

            default:
                Reply(Usage);
                return;
        }
    }
}
