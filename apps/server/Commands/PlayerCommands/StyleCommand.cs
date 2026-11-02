using System;
using System.Linq;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Entity;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.WorldObjects;

namespace ACE.Server.Commands.PlayerCommands;

/// <summary>
/// /style switches an item between its looks (see ItemStyles). It works on the item you last examined, carried or
/// equipped, and only offers the styles you can choose: locked styles need their quest flag.
/// </summary>
public class StyleCommand
{
    [CommandHandler(
        "style",
        AccessLevel.Player,
        CommandHandlerFlag.RequiresWorld,
        "Switches the item you last examined between its looks, if it has more than one.",
        "[show | list | <number or name> | help]"
    )]
    public static void HandleStyle(Session session, params string[] parameters)
    {
        var option = parameters.Length > 0 ? string.Join(" ", parameters).Trim() : null;

        switch (option?.ToLowerInvariant())
        {
            case null:
                SwitchExamined(session, null);
                break;
            case "show":
                ShowExamined(session);
                break;
            case "list":
                ListItems(session);
                break;
            case "help":
                ShowHelp(session);
                break;
            default:
                SwitchExamined(session, option);
                break;
        }
    }

    private static void ShowHelp(Session session)
    {
        Send(
            session,
            "Some items have more than one look. Examine one you carry or have equipped, then:\n"
                + "  /style - switches it to its next look; keep using it to go through them all.\n"
                + "  /style show - lists the looks it has that you can use.\n"
                + "  /style <number or name> - switches it to that look; /style default switches it back.\n"
                + "  /style list - lists the items you carry or have equipped that have more than one look."
        );
    }

    /// <summary>
    /// The item the player last examined, if they carry or wear it. Tells them why not otherwise.
    /// </summary>
    private static WorldObject GetExaminedItem(Session session, out bool equipped)
    {
        var player = session.Player;
        var targetGuid = player.RequestedAppraisalTarget;
        equipped = false;

        if (targetGuid == null)
        {
            Send(
                session,
                "Examine the item you want to switch first. /style list shows which of your items have more than one look."
            );
            return null;
        }

        var guid = new ObjectGuid(targetGuid.Value);
        var equippedItem = player.GetEquippedItem(guid);
        var item = equippedItem ?? player.GetInventoryItem(guid);
        equipped = equippedItem != null;

        if (item == null)
        {
            Send(
                session,
                "You can only switch the look of an item you carry or have equipped. Examine it, then use /style."
            );
        }

        return item;
    }

    private static void SwitchExamined(Session session, string numberOrName)
    {
        var player = session.Player;
        var item = GetExaminedItem(session, out var equipped);

        if (item == null)
        {
            return;
        }

        Func<string, bool> hasQuest = player.QuestManager.HasQuest;

        if (!ItemStyles.CanSwitch(item, hasQuest))
        {
            Send(session, $"The {item.Name} has no other look you can use.");
            return;
        }

        ItemStyle style;

        if (numberOrName == null)
        {
            style = ItemStyles.GetNextStyle(item, hasQuest);
        }
        else
        {
            style = ItemStyles.FindAvailableStyle(item, numberOrName, hasQuest);

            if (style == null)
            {
                Send(
                    session,
                    $"The {item.Name} has no look \"{numberOrName}\" you can use. /style show lists the ones it has."
                );
                return;
            }
        }

        if (!ItemStyles.Apply(item, style))
        {
            Send(session, $"The {item.Name} already shows its {style.Name} look.");
            return;
        }

        // A carried item is only seen by its owner. An equipped one is seen by everyone nearby: a wielded weapon or
        // shield as an object of its own (its setup, colors and effect), a worn item as part of its wearer.
        if (equipped)
        {
            player.EnqueueBroadcast(new GameMessageUpdateObject(item));
            player.EnqueueBroadcast(new GameMessageObjDescEvent(player));
        }
        else
        {
            session.Network.EnqueueSend(new GameMessageUpdateObject(item));
        }

        Send(session, $"You switch the {item.Name} to its {style.Name} look.");
    }

    private static void ShowExamined(Session session)
    {
        var player = session.Player;
        var item = GetExaminedItem(session, out _);

        if (item == null)
        {
            return;
        }

        if (!ItemStyles.CanSwitch(item, player))
        {
            Send(session, $"The {item.Name} has no other look you can use.");
            return;
        }

        var available = ItemStyles.GetAvailableStyles(item, player);
        var current = ItemLook.Of(item);
        var lines = available.Select(
            (s, i) => $"  {i + 1}. {s.Name}" + (s.Look == current ? " (showing)" : "") + (s.IsLocked ? " *" : "")
        );

        var footnote = available.Any(s => s.IsLocked) ? "\n  * a look you have earned" : "";

        Send(
            session,
            $"Looks of the {item.Name} (use /style <number or name>):\n" + string.Join("\n", lines) + footnote
        );
    }

    private static void ListItems(Session session)
    {
        var player = session.Player;
        var items = player
            .GetAllPossessions()
            .Where(i => ItemStyles.CanSwitch(i, player))
            .Select(i => $"{i.Name} ({ItemStyles.GetAvailableStyles(i, player).Count} looks)")
            .OrderBy(n => n)
            .ToList();

        if (items.Count == 0)
        {
            Send(session, "None of the items you carry or have equipped has another look you can use.");
            return;
        }

        Send(
            session,
            "Items with more than one look (examine one, then use /style or /style show):\n  "
                + string.Join("\n  ", items)
        );
    }

    private static void Send(Session session, string message)
    {
        session.Network.EnqueueSend(new GameMessageSystemChat(message, ChatMessageType.Broadcast));
    }
}
