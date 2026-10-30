using System;
using System.Linq;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Entity.Actions;
using ACE.Server.Managers;
using ACE.Server.Network;
using ACE.Server.TownEvents;

namespace ACE.Server.Commands.AdminCommands;

/// <summary>
/// @fakeplayer: fake players for testing fellowships and town attacks (FakePlayerManager). They're never saved.
/// </summary>
public class FakePlayerCommand
{
    private const string Usage =
        "@fakeplayer add [<count>] [<level>] | list | join | leave [<count>] | remove [all | <number>]. "
        + "add makes fake players in front of you (1 by default, up to 50 at a time), at your level unless you give one; "
        + "they count as online players but can't be attacked and are never saved. join puts every fake player in a town "
        + "under attack into its Town Defense (they also say yes to its prompt by themselves). leave takes fake players out of "
        + "their fellowships (all of them, or count). remove takes them out of the world (all by default, or Fake Player <number>).";

    [CommandHandler("fakeplayer", AccessLevel.Admin, CommandHandlerFlag.RequiresWorld, 0, "Makes and removes fake players for testing.", Usage)]
    public static void HandleFakePlayer(Session session, params string[] parameters)
    {
        var admin = session.Player;

        void Reply(string text) => admin.SendMessage(text, ChatMessageType.System);

        var action = parameters.Length > 0 ? parameters[0].ToLowerInvariant() : "";

        switch (action)
        {
            case "add":
            {
                var count = 1;
                var level = admin.Level ?? 1;

                if (parameters.Length > 1 && (!int.TryParse(parameters[1], out count) || count < 1 || count > FakePlayerManager.MaxPerSpawn))
                {
                    Reply($"The count has to be 1 to {FakePlayerManager.MaxPerSpawn}.");
                    return;
                }

                if (parameters.Length > 2 && (!int.TryParse(parameters[2], out level) || level < 1 || level > 999))
                {
                    Reply("The level has to be 1 to 999.");
                    return;
                }

                WorldManager.EnqueueAction(
                    new ActionEventDelegate(() =>
                    {
                        var made = FakePlayerManager.Spawn(admin, count, level);
                        Reply(
                            made.Count == 0
                                ? "No fake players could be made."
                                : $"Made {made.Count} level {level} fake player{(made.Count == 1 ? "" : "s")}: {string.Join(", ", made.Select(p => p.Name))}."
                        );
                    })
                );
                return;
            }

            case "list":
            {
                var fakes = FakePlayerManager.GetAll();
                if (fakes.Count == 0)
                {
                    Reply("There are no fake players.");
                    return;
                }

                Reply($"{fakes.Count} fake player{(fakes.Count == 1 ? "" : "s")}:");
                foreach (var fake in fakes.OrderBy(p => p.Name))
                {
                    var where = fake.Location != null ? $"0x{fake.Location.Cell:X8}" : "nowhere";
                    var fellowship = fake.Fellowship != null ? $", in {fake.Fellowship.FellowshipName} ({fake.Fellowship.TotalMembers})" : "";
                    Reply($"  {fake.Name}, level {fake.Level ?? 1}, {where}{fellowship}");
                }
                return;
            }

            case "join":
            {
                var fakes = FakePlayerManager.GetAll();
                foreach (var fake in fakes)
                {
                    TownEventManager.HandleJoin(fake);
                }

                Reply($"{fakes.Count} fake player{(fakes.Count == 1 ? "" : "s")} asked to join the Town Defense of the town they're in.");
                return;
            }

            case "leave":
            {
                var count = int.MaxValue;
                if (parameters.Length > 1 && (!int.TryParse(parameters[1], out count) || count < 1))
                {
                    Reply("The count has to be 1 or more.");
                    return;
                }

                WorldManager.EnqueueAction(
                    new ActionEventDelegate(() =>
                    {
                        var leaving = FakePlayerManager.GetAll().Where(p => p.Fellowship != null).OrderBy(p => p.Name).Take(count).ToList();
                        foreach (var fake in leaving)
                        {
                            fake.FellowshipQuit(false);
                        }

                        Reply($"{leaving.Count} fake player{(leaving.Count == 1 ? "" : "s")} left their fellowship.");
                    })
                );
                return;
            }

            case "remove":
            {
                var which = parameters.Length > 1 ? string.Join(" ", parameters.Skip(1)) : "all";

                WorldManager.EnqueueAction(
                    new ActionEventDelegate(() =>
                    {
                        if (which.Equals("all", StringComparison.OrdinalIgnoreCase))
                        {
                            var removed = FakePlayerManager.RemoveAll();
                            Reply($"Removed {removed} fake player{(removed == 1 ? "" : "s")}.");
                            return;
                        }

                        var name = int.TryParse(which, out var number) ? $"Fake Player {number}" : which;
                        var fake = FakePlayerManager.GetAll().FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                        if (fake == null)
                        {
                            Reply($"No fake player called {name}.");
                            return;
                        }

                        FakePlayerManager.Remove(fake);
                        Reply($"Removed {fake.Name}.");
                    })
                );
                return;
            }

            default:
                Reply(Usage);
                return;
        }
    }
}
