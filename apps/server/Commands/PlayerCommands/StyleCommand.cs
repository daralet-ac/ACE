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
/// /style switches an item flagged with an AlternateSetup between its two looks. It works on the item you last
/// examined, carried or equipped, and switching it again brings the first look back.
/// </summary>
public class StyleCommand
{
    [CommandHandler(
        "style",
        AccessLevel.Player,
        CommandHandlerFlag.RequiresWorld,
        "Switches the item you last examined between its two looks, if it has a second one.",
        "[list | help]"
    )]
    public static void HandleStyle(Session session, params string[] parameters)
    {
        var option = parameters.Length > 0 ? parameters[0].ToLowerInvariant() : null;

        switch (option)
        {
            case null:
                ToggleExamined(session);
                break;
            case "list":
                ListItems(session);
                break;
            default:
                ShowHelp(session);
                break;
        }
    }

    private static void ShowHelp(Session session)
    {
        Send(
            session,
            "Some items have two looks. Examine one you carry or have equipped, then:\n"
                + "  /style - switches it to its other look; use it again to switch back.\n"
                + "  /style list - lists the items you carry or have equipped that have two looks."
        );
    }

    private static void ToggleExamined(Session session)
    {
        var player = session.Player;
        var targetGuid = player.RequestedAppraisalTarget;

        if (targetGuid == null)
        {
            Send(
                session,
                "Examine the item you want to switch first. /style list shows which of your items have two looks."
            );
            return;
        }

        var guid = new ObjectGuid(targetGuid.Value);
        var equipped = player.GetEquippedItem(guid);
        var item = equipped ?? player.GetInventoryItem(guid);

        if (item == null)
        {
            Send(
                session,
                "You can only switch the look of an item you carry or have equipped. Examine it, then use /style."
            );
            return;
        }

        if (!ItemSetupToggle.Toggle(item))
        {
            Send(session, $"The {item.Name} has only one look.");
            return;
        }

        // A held item is drawn for everyone nearby; anything else is only seen by its owner.
        if (equipped != null)
        {
            player.EnqueueBroadcast(new GameMessageUpdateObject(item));
        }
        else
        {
            session.Network.EnqueueSend(new GameMessageUpdateObject(item));
        }

        Send(session, $"You switch the {item.Name} to its other look. Use /style again to switch it back.");
    }

    private static void ListItems(Session session)
    {
        var items = session
            .Player.GetAllPossessions()
            .Where(ItemSetupToggle.HasAlternateSetup)
            .Select(i => i.Name)
            .OrderBy(n => n)
            .ToList();

        if (items.Count == 0)
        {
            Send(session, "None of the items you carry or have equipped has a second look.");
            return;
        }

        Send(session, "Items with two looks (examine one, then use /style):\n  " + string.Join("\n  ", items));
    }

    private static void Send(Session session, string message)
    {
        session.Network.EnqueueSend(new GameMessageSystemChat(message, ChatMessageType.Broadcast));
    }
}
