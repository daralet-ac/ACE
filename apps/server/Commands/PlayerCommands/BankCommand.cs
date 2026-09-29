using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Entity;
using ACE.Server.Entity.Actions;
using ACE.Server.Managers;
using ACE.Server.Network;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.WorldObjects;

namespace ACE.Server.Commands.PlayerCommands;

public class BankCommand
{
    private const int MaxSearchResults = 60;
    private const int MaxInscriptionLength = 100;

    private const string CategoryList =
        "all, gear, weapons, armor, jewelry, trinkets, salvage, ammo, components, consumables, gems, keys, manastones, trophies";

    // /bank balance reads every offline character's possessions from the database, so it can't be spammed.
    private static readonly TimeSpan BalanceCooldown = TimeSpan.FromSeconds(30);
    private static readonly ConcurrentDictionary<uint, DateTime> LastBalanceCheck = new();

    [CommandHandler(
        "bank",
        AccessLevel.Player,
        CommandHandlerFlag.RequiresWorld,
        0,
        "Bank tools: deposit, sort, search and combine salvage in your bank, check your balance, and tag packs by inscription. Use /bank for help.",
        "deposit|sort|search|combine|balance|packs|inscribe ..."
    )]
    public static void HandleBank(Session session, params string[] parameters)
    {
        if (parameters.Length == 0)
        {
            ShowHelp(session);
            return;
        }

        var rest = parameters.Skip(1).ToArray();

        switch (parameters[0].ToLowerInvariant())
        {
            case "deposit":
            case "d":
                HandleDeposit(session, rest);
                break;
            case "sort":
                HandleSort(session, rest);
                break;
            case "search":
            case "find":
                HandleSearch(session, rest);
                break;
            case "combine":
                HandleCombine(session);
                break;
            case "balance":
            case "bal":
                HandleBalance(session);
                break;
            case "packs":
            case "info":
                HandlePacks(session);
                break;
            case "inscribe":
            case "tag":
                HandleInscribe(session, rest);
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
            "Bank commands (stand at your open bank):\n"
                + "  /bank deposit <category> - Moves those items from your packs into your bank.\n"
                + "  /bank sort [category] - Files items into their inscribed packs, combines stacks and puts everything in order.\n"
                + "  /bank search <name, category or type> - Lists matching items in your bank and where they are.\n"
                + "  /bank combine - Combines salvage bags in your bank that have the same material and workmanship.\n"
                + "  /bank balance - Shows the pyreals and trade notes in your bank and across your account. Works anywhere.\n"
                + "  /bank packs - Shows your bank space and how each pack is tagged.\n"
                + "  /bank inscribe <tags> - Inscribes the pack you last examined (\"/bank inscribe clear\" to clear it).\n"
                + $"Categories: {CategoryList}.\n"
                + "Tag a pack by inscribing it with category words, like \"weapons\" or \"gems, keys\". "
                + "Deposits and sorts fill those bank packs first. "
                + "Inscribe a pack you carry with \"keep\" and /bank deposit leaves it alone.\n"
                + "Gear is weapons (including casters), armor (including shields and clothing), jewelry and trinkets. "
                + "Consumables are food, potions and healing kits; keys include lockpicks; gems include jewels."
        );
    }

    // --- /bank deposit ---

    private static void HandleDeposit(Session session, string[] parameters)
    {
        if (parameters.Length == 0 || !TryParseFilter(parameters[0], out var filter))
        {
            Send(session, $"Usage: /bank deposit <{CategoryList}>");
            return;
        }

        var player = session.Player;
        var bank = GetReadyBank(session);
        if (bank == null)
        {
            return;
        }

        var report = player.DepositToBank(bank, filter);
        var what = filter == null ? "items" : BankCategories.Describe(filter.Value);

        var lines = new List<string>();

        if (report.Moved > 0)
        {
            lines.Add($"Deposited {Items(report.Moved)} ({DescribeDestinations(report)}).");
        }

        if (report.StacksCombined > 0)
        {
            lines.Add($"Added {Plural(report.StacksCombined, "stack")} to stacks already in your bank.");
        }

        if (report.NoRoom > 0)
        {
            lines.Add($"{Items(report.NoRoom)} didn't fit: your bank and its packs are full.");
        }

        if (report.Failed > 0)
        {
            lines.Add($"{Items(report.Failed)} couldn't be moved.");
        }

        if (report.Attuned > 0)
        {
            lines.Add($"{Items(report.Attuned)} {(report.Attuned == 1 ? "is" : "are")} attuned and can't be banked.");
        }

        if (lines.Count == 0)
        {
            lines.Add($"You have no {what} to deposit.");
        }

        Send(session, string.Join("\n", lines));
    }

    // --- /bank sort ---

    private static void HandleSort(Session session, string[] parameters)
    {
        BankCategory? filter = null;

        if (parameters.Length > 0 && !TryParseFilter(parameters[0], out filter))
        {
            Send(session, $"Usage: /bank sort [{CategoryList}]");
            return;
        }

        var player = session.Player;
        var bank = GetReadyBank(session);
        if (bank == null)
        {
            return;
        }

        var report = player.SortBank(bank, filter);

        var lines = new List<string>();

        if (report.Moved > 0)
        {
            lines.Add($"Moved {Items(report.Moved)} ({DescribeDestinations(report)}).");
        }

        if (report.StacksCombined > 0)
        {
            lines.Add($"Combined {Plural(report.StacksCombined, "stack")}.");
        }

        if (report.Reordered > 0)
        {
            lines.Add($"Put {Plural(report.Reordered, "container")} in order.");
        }

        if (report.NoRoom > 0)
        {
            lines.Add($"{Items(report.NoRoom)} belong in a pack that is full.");
        }

        if (report.Failed > 0)
        {
            lines.Add($"{Items(report.Failed)} couldn't be moved.");
        }

        lines.Insert(0, lines.Count == 0 ? "Your bank is already sorted." : "Bank sorted.");

        if (filter != null && !player.GetBankPacks(bank).Any(p => (BankCategories.ParseInscription(p.Inscription).Categories & filter.Value) != 0))
        {
            var what = BankCategories.Describe(filter.Value);
            lines.Add($"No pack in your bank is inscribed for {what}. Inscribe one with \"{what}\" to collect them.");
        }

        Send(session, string.Join("\n", lines));
    }

    // --- /bank search ---

    private static void HandleSearch(Session session, string[] parameters)
    {
        var query = string.Join(" ", parameters).Trim();

        if (query.Length == 0)
        {
            Send(session, "Usage: /bank search <name, category or type>. For example: /bank search pyreal, /bank search trinkets, /bank search gem");
            return;
        }

        var bank = GetReadyBank(session);
        if (bank == null)
        {
            return;
        }

        // A category (weapons) or item type (gem) word matches by kind, and every search also matches by name,
        // so "portal" still finds a "Portal Gem".
        var isCategory = BankCategories.TryParse(query, out var category);
        var isItemType = BankCategories.TryParseItemType(query, out var itemType);

        bool Matches(WorldObject item) =>
            (isCategory && (BankCategories.Classify(item) & category) != 0)
            || (isItemType && (item.ItemType & itemType) != 0)
            || (item.Name?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false);

        var found = session.Player.GetBankContents(bank).Where(c => Matches(c.Item)).ToList();

        if (found.Count == 0)
        {
            Send(session, $"Nothing in your bank matches \"{query}\".");
            return;
        }

        var lines = new List<string> { $"Bank search for \"{query}\": {Items(found.Count)}." };
        var listed = 0;

        foreach (var group in found.GroupBy(c => c.Container))
        {
            if (listed >= MaxSearchResults)
            {
                break;
            }

            var names = group
                .Take(MaxSearchResults - listed)
                .Select(c => (c.Item.StackSize ?? 1) > 1 ? $"{c.Item.Name} ({c.Item.StackSize:N0})" : c.Item.Name)
                .ToList();

            listed += names.Count;

            var where = group.Key == bank ? "In your bank" : $"In {group.Key.Name}";
            lines.Add($"{where}: {string.Join(", ", names)}");
        }

        if (found.Count > listed)
        {
            lines.Add($"...and {found.Count - listed} more. Search for something more specific to see them.");
        }

        Send(session, string.Join("\n", lines));
    }

    // --- /bank combine ---

    private static void HandleCombine(Session session)
    {
        var player = session.Player;
        var bank = GetReadyBank(session);
        if (bank == null)
        {
            return;
        }

        var toEmpty = player.CountBankSalvageToCombine(bank);
        if (toEmpty == 0)
        {
            Send(session, "No salvage bags in your bank can be combined. Bags combine with others of the same material and workmanship.");
            return;
        }

        var question = $"Combine the salvage in your bank? {Plural(toEmpty, "bag")} will be poured into others of the same material and workmanship.";

        var confirmation = new Confirmation_Custom(
            player.Guid,
            () =>
            {
                // The bank may have closed or changed while the question was up.
                var openBank = GetReadyBank(session);
                if (openBank == null)
                {
                    return;
                }

                var emptied = player.CombineBankSalvage(openBank);

                Send(
                    session,
                    emptied == 0
                        ? "No salvage bags in your bank can be combined any more."
                        : $"Combined the salvage in your bank: {Plural(emptied, "bag")} poured into others."
                );
            }
        );

        if (!player.ConfirmationManager.EnqueueSend(confirmation, question))
        {
            Send(session, "A confirmation is already pending.");
        }
    }

    // --- /bank balance ---

    private static void HandleBalance(Session session)
    {
        var player = session.Player;
        var now = DateTime.UtcNow;

        if (LastBalanceCheck.TryGetValue(player.Guid.Full, out var last) && now - last < BalanceCooldown)
        {
            var wait = (int)Math.Ceiling((BalanceCooldown - (now - last)).TotalSeconds);
            Send(session, $"You counted your pyreals a moment ago. Try again in {Plural(wait, "second")}.");
            return;
        }

        LastBalanceCheck[player.Guid.Full] = now;

        // With the bank open, count what is in it now; the database may be a moment behind the last deposit.
        var bank = player.GetOpenBank();
        long? liveBank = null;

        if (bank is { BankInventoryLoaded: true })
        {
            var (coins, notes, noteCount) = player.GetBankMoney(bank);
            liveBank = coins + notes;

            Send(
                session,
                $"Your bank holds {coins + notes:N0} pyreals: {coins:N0} in coin and {notes:N0} in {Plural(noteCount, "trade note")}."
            );
        }
        else
        {
            Send(session, "Counting your pyreals...");
        }

        AccountWealthTracker.Update(
            player,
            wealth =>
                WorldManager.EnqueueAction(
                    new ActionEventDelegate(() =>
                    {
                        // gone while we counted
                        if (session.Player != player)
                        {
                            return;
                        }

                        if (wealth == null)
                        {
                            Send(session, "Your pyreals couldn't be counted right now. Try again later.");
                            return;
                        }

                        var bankPyreals = liveBank ?? wealth.Value.BankPyreals;
                        var accountPyreals = wealth.Value.TotalPyreals - wealth.Value.BankPyreals + bankPyreals;

                        var lines = new List<string>();

                        if (liveBank == null)
                        {
                            lines.Add($"Your bank holds {bankPyreals:N0} pyreals in coin and trade notes.");
                        }

                        lines.Add($"Your account holds {accountPyreals:N0} pyreals in all, counting every character and your bank.");

                        Send(session, string.Join("\n", lines));
                    })
                )
        );
    }

    // --- /bank packs ---

    private static void HandlePacks(Session session)
    {
        var player = session.Player;
        var lines = new List<string>();

        var bank = player.GetOpenBank();

        if (bank == null || !bank.BankInventoryLoaded)
        {
            lines.Add("Open your bank to see its space and packs.");
        }
        else
        {
            var used = bank.Inventory.Values.Count(i => !i.UseBackpackSlot);
            lines.Add($"Your bank: {used} of {bank.ItemCapacity ?? 0} slots used.");

            foreach (var pack in player.GetBankPacks(bank))
            {
                lines.Add($"  {DescribePack(pack)}");
            }
        }

        var carried = player.Inventory.Values
            .OfType<Container>()
            .OrderBy(p => p.PlacementPosition ?? int.MaxValue)
            .ToList();

        if (carried.Count > 0)
        {
            lines.Add("Packs you carry:");

            foreach (var pack in carried)
            {
                lines.Add($"  {DescribePack(pack)}");
            }
        }

        Send(session, string.Join("\n", lines));
    }

    private static string DescribePack(Container pack)
    {
        var used = pack.Inventory.Values.Count(i => !i.UseBackpackSlot);
        var tags = BankCategories.Describe(BankCategories.ParseInscription(pack.Inscription));

        return $"{pack.Name} [{tags}] {used} of {pack.ItemCapacity ?? 0} slots used";
    }

    // --- /bank inscribe ---

    private static void HandleInscribe(Session session, string[] parameters)
    {
        var text = string.Join(" ", parameters).Trim();

        if (text.Length == 0)
        {
            Send(session, "Usage: examine one of your packs, then /bank inscribe <tags>. For example: /bank inscribe weapons, armor. Use /bank inscribe clear to remove it.");
            return;
        }

        if (text.Length > MaxInscriptionLength)
        {
            Send(session, $"That inscription is too long ({MaxInscriptionLength} characters at most).");
            return;
        }

        var player = session.Player;
        var targetGuid = player.RequestedAppraisalTarget;

        var pack = targetGuid == null
            ? null
            : player.FindObject(targetGuid.Value, Player.SearchLocations.MyInventory, out _, out _, out _)
                ?? player.FindItemInOpenBank(targetGuid.Value);

        if (pack is not Container { WeenieType: WeenieType.Container })
        {
            Send(session, "Examine one of your packs first (in your inventory or your open bank), then use /bank inscribe.");
            return;
        }

        var clear = text.Equals("clear", StringComparison.OrdinalIgnoreCase);

        if (!player.TrySetInscription(pack, clear ? null : text))
        {
            Send(session, $"{pack.Name} was inscribed by {pack.ScribeName ?? "someone else"}. Only they can change it.");
            return;
        }

        // A pack in the bank is saved with the bank, which only happens when the landblock saves; do it now.
        if (pack.Container is Storage)
        {
            pack.SaveBiotaToDatabase();
        }

        if (clear)
        {
            Send(session, $"Cleared the inscription on {pack.Name}.");
            return;
        }

        var tags = BankCategories.ParseInscription(text);

        Send(
            session,
            tags.IsEmpty
                ? $"Inscribed {pack.Name}. It has no bank tags: use salvage, weapons, armor, jewelry, trinkets, gear or keep."
                : $"Inscribed {pack.Name} [{BankCategories.Describe(tags)}]."
        );
    }

    // --- helpers ---

    private static Storage GetReadyBank(Session session)
    {
        var player = session.Player;
        var bank = player.GetOpenBank();

        if (bank == null)
        {
            Send(session, "Open your bank and stand at it to use that.");
            return null;
        }

        if (!bank.BankInventoryLoaded)
        {
            Send(session, "Your bank is still opening. Try again in a moment.");
            return null;
        }

        if (player.IsBusy)
        {
            Send(session, "You're too busy to do that right now.");
            return null;
        }

        return bank;
    }

    /// <summary>
    /// "all" is a null filter; anything else must be a category word.
    /// </summary>
    private static bool TryParseFilter(string word, out BankCategory? filter)
    {
        filter = null;

        if (word.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (BankCategories.TryParse(word, out var category))
        {
            filter = category;
            return true;
        }

        return false;
    }

    private static string DescribeDestinations(BankReport report)
    {
        return string.Join(", ", report.MovedInto.Select(m => $"{m.Count:N0} into {m.Name ?? "your bank"}"));
    }

    private static string Items(int count) => Plural(count, "item");

    private static string Plural(int count, string noun) => $"{count:N0} {noun}{(count == 1 ? "" : "s")}";

    private static void Send(Session session, string text)
    {
        session.Network.EnqueueSend(new GameMessageSystemChat(text, ChatMessageType.System));
    }
}
