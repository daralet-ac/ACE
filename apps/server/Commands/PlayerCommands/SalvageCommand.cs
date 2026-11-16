using System;
using System.Collections.Generic;
using System.Linq;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Entity;
using ACE.Server.Entity.Actions;
using ACE.Server.Network;
using ACE.Server.Network.GameEvent.Events;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.WorldObjects;

namespace ACE.Server.Commands.PlayerCommands;

public class SalvageCommand
{
    private static readonly HashSet<uint> SalvageCrateWcids = new() { 1055060, 1055070, 1055080 };

    [CommandHandler("salvage", AccessLevel.Player, CommandHandlerFlag.RequiresWorld, 0,
        "Salvage bag management. Use '/salvage combine', '/salvage refinemax <1-9>' or '/salvage refineconfirm <on|off>'.")]
    public static void HandleSalvage(Session session, params string[] parameters)
    {
        if (parameters.Length == 0)
        {
            ShowHelp(session);
            return;
        }

        switch (parameters[0].ToLowerInvariant())
        {
            case "combine":
                HandleCombine(session);
                break;
            case "refine":
                session.Network.EnqueueSend(new GameMessageSystemChat(
                    "Salvage is now refined at the Salvage Refinery: hand it a stack of salvage to refine it to the next workmanship, or your salvage crate to refine every stack in it.",
                    ChatMessageType.System
                ));
                break;
            case "refineconfirm":
                HandleRefineConfirm(session, parameters);
                break;
            case "refinemax":
                HandleRefineMax(session, parameters);
                break;
            default:
                ShowHelp(session);
                break;
        }
    }

    private static void ShowHelp(Session session)
    {
        session.Network.EnqueueSend(new GameMessageSystemChat(
            "Salvage Commands:\n" +
            "  '/salvage combine' — Combines your stacks of salvage of the same material and workmanship.\n" +
            "  '/salvage refinemax <1-9>' — The highest workmanship the Salvage Refinery refines when you hand it a pack of salvage. Stacks above it are left alone.\n" +
            "  '/salvage refineconfirm <on|off>' — Turns the Salvage Refinery's confirmation on or off.\n" +
            "Hand a stack of salvage to the Salvage Refinery to refine it to the next workmanship (2 units become 1), or hand it your salvage crate to refine every stack inside at once.",
            ChatMessageType.System
        ));
    }

    // --- helpers ---

    internal static List<WorldObject> GetAllSalvageBags(Player player)
    {
        var bags = new List<WorldObject>();

        foreach (var item in player.Inventory.Values)
        {
            if (item.WeenieType == WeenieType.Salvage)
            {
                bags.Add(item);
            }

            if (item is Container container)
            {
                foreach (var inner in container.Inventory.Values)
                {
                    if (inner.WeenieType == WeenieType.Salvage)
                    {
                        bags.Add(inner);
                    }
                }
            }
        }

        return bags;
    }

    private static Container GetSalvageCrate(Player player)
    {
        return player.Inventory.Values
            .OfType<Container>()
            .FirstOrDefault(c => SalvageCrateWcids.Contains(c.WeenieClassId));
    }

    /// <summary>
    /// Re-sorts the player's salvage crate (if they have one) a moment after its bags changed
    /// </summary>
    internal static void ResortSalvageCrate(Player player)
    {
        var crate = GetSalvageCrate(player);
        if (crate == null)
        {
            return;
        }

        var sortChain = new ActionChain();
        sortChain.AddDelaySeconds(0.1);
        sortChain.AddAction(player, () => SendSortedCrateContainIds(player.Session, crate));
        sortChain.EnqueueChain();
    }

    private static void SendSortedCrateContainIds(Session session, Container crate)
    {
        var sorted = crate.Inventory.Values
            .OrderBy(Salvage.GetSalvageBagSortKey)
            .ToList();

        for (var i = 0; i < sorted.Count; i++)
        {
            sorted[i].PlacementPosition = i;
            session.Network.EnqueueSend(new GameEventItemServerSaysContainId(session, sorted[i], crate));
        }
    }

    // --- /salvage combine ---

    private static void HandleCombine(Session session)
    {
        var player = session.Player;
        var stacks = GetAllSalvageBags(player).Where(SalvageStacks.IsStack).ToList();

        var groups = stacks
            .GroupBy(s => s.WeenieClassId)
            .Where(g => g.Count() > 1)
            .ToList();

        // the stacks combining would empty: all but as many as hold the units
        var emptied = groups.Sum(g => g.Count() - (g.Sum(s => s.StackSize ?? 1) + SalvageStacks.MaxStackSize - 1) / SalvageStacks.MaxStackSize);

        if (emptied <= 0)
        {
            session.Network.EnqueueSend(new GameMessageSystemChat(
                "No salvage can be combined (no stacks of the same material and workmanship with room).",
                ChatMessageType.System
            ));
            return;
        }

        var msg = $"Combine your salvage, emptying {emptied} {(emptied == 1 ? "stack" : "stacks")} into others? Proceed?";

        if (!player.ConfirmationManager.EnqueueSend(
            new Confirmation_Custom(player.Guid, () => ExecuteCombine(session)),
            msg
        ))
        {
            session.Network.EnqueueSend(new GameMessageSystemChat(
                "A confirmation is already pending.",
                ChatMessageType.System
            ));
        }
    }

    private static void ExecuteCombine(Session session)
    {
        var player = session.Player;

        // re-read the stacks: they may have changed while the confirmation was up
        var combined = player.CombineSalvageStacks(GetAllSalvageBags(player));

        session.Network.EnqueueSend(new GameMessageSystemChat(
            $"Combined {combined} {(combined == 1 ? "stack" : "stacks")} of salvage into others.",
            ChatMessageType.System
        ));

        ResortSalvageCrate(player);
    }

    // --- /salvage refinemax ---

    private static void HandleRefineMax(Session session, string[] parameters)
    {
        var player = session.Player;
        var limit = SalvageRefineryService.MaxRefineMax;

        if (parameters.Length < 2)
        {
            session.Network.EnqueueSend(new GameMessageSystemChat(
                $"Your refine max is workmanship {SalvageRefineryService.GetRefineMax(player)}: handing the Salvage Refinery a pack refines the stacks at or below it. Type '{SalvageRefineryService.RefineMaxCommand} <1-{limit}>' to change it.",
                ChatMessageType.System
            ));
            return;
        }

        if (!int.TryParse(parameters[1], out var max) || max < 1 || max > limit)
        {
            session.Network.EnqueueSend(new GameMessageSystemChat(
                $"Usage: '{SalvageRefineryService.RefineMaxCommand} <1-{limit}>'",
                ChatMessageType.System
            ));
            return;
        }

        SalvageRefineryService.SetRefineMax(player, max);

        session.Network.EnqueueSend(new GameMessageSystemChat(
            $"Handing the Salvage Refinery a pack now refines the stacks at workmanship {max} or below.",
            ChatMessageType.System
        ));
    }

    // --- /salvage refineconfirm ---

    private static void HandleRefineConfirm(Session session, string[] parameters)
    {
        var player = session.Player;
        bool skip;

        if (parameters.Length < 2)
        {
            // no argument toggles it
            skip = !SalvageRefineryService.IsConfirmationSkipped(player);
        }
        else if (parameters[1].Equals("off", StringComparison.OrdinalIgnoreCase))
        {
            skip = true;
        }
        else if (parameters[1].Equals("on", StringComparison.OrdinalIgnoreCase))
        {
            skip = false;
        }
        else
        {
            session.Network.EnqueueSend(new GameMessageSystemChat(
                $"Usage: '{SalvageRefineryService.ConfirmCommand} <on|off>'",
                ChatMessageType.System
            ));
            return;
        }

        SalvageRefineryService.SetConfirmationSkipped(player, skip);

        session.Network.EnqueueSend(new GameMessageSystemChat(
            skip
                ? $"The Salvage Refinery will now refine your salvage without asking first. Type '{SalvageRefineryService.ConfirmCommand} on' to bring the confirmation back."
                : "The Salvage Refinery will now ask you to confirm each refine.",
            ChatMessageType.System
        ));
    }
}
