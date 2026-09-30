using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
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

    // /bank withdraw asks first before taking more than this many items without a count
    private const int WithdrawConfirmAbove = 20;

    private const string CategoryList =
        "all, gear, weapons, armor, jewelry, trinkets, salvage, ammo, animal, components, consumables, gems, keys, manastones, trophies";

    // The kinds inside a category (BankCategory.Kinds): a pack tagged for one beats a pack tagged for the whole category.
    private const string TypeList =
        "Weapons: swords, maces, axes, spears, daggers, staffs, unarmed (ua), two-handed (2h), bows, crossbows, atlatls, thrown, casters.\n"
        + "Armor: heavy, light and cloth (by weight class), clothing.\n"
        + "Jewelry: necklaces, rings, bracelets.\n"
        + "Salvage: blacksmithing, tailoring, spellcrafting, woodworking, jewelcrafting, imbue.";

    // /bank balance reads every offline character's possessions from the database, so it can't be spammed.
    private static readonly TimeSpan BalanceCooldown = TimeSpan.FromSeconds(30);
    private static readonly ConcurrentDictionary<uint, DateTime> LastBalanceCheck = new();

    [CommandHandler(
        "bank",
        AccessLevel.Player,
        CommandHandlerFlag.RequiresWorld,
        0,
        "Bank tools: deposit, withdraw, sort, search and combine salvage in your bank, check your balance, and see how your packs are tagged. Use /bank for help.",
        "deposit|withdraw|sort|search|combine|balance|packs ..."
    )]
    public static void HandleBank(Session session, params string[] parameters)
    {
        // The bank stops mentioning /bank when it's opened (Storage.Open) once the player has used it.
        if (!session.Player.BankCommandsUsed)
        {
            session.Player.QuestManager.Stamp(Player.BankCommandsUsedQuest);
        }

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
            case "withdraw":
            case "w":
            case "take":
                HandleWithdraw(session, rest);
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
                + "  /bank deposit <category or type> - Moves those items from your packs into your bank.\n"
                + "  /bank withdraw [how many] <name, category or type> - Takes those items out of your bank into your packs, "
                + "filling named and inscribed packs first, then your main pack. With a number, takes that many (\"/bank withdraw 200 arrows\"). "
                + "Stops at your burden limit, and never fills a pack inscribed \"deposit\".\n"
                + "  /bank sort [category or type] - Files items into their inscribed packs, combines stacks and puts everything in order.\n"
                + "  /bank search <name, category or type> - Lists matching items in your bank and where they are. "
                + "A weapon's own name works too, like longsword or jitte.\n"
                + "  /bank combine - Combines salvage bags in your bank that have the same material and workmanship.\n"
                + "  /bank balance - Shows the pyreals and trade notes in your bank and across your account. Works anywhere.\n"
                + "  /bank packs - Shows your bank space and how each pack is tagged.\n\n"
                + $"Categories: {CategoryList}.\n\n"
                + "Types, each part of a category:\n"
                + TypeList
                + "\n\n"
                + "Tag a pack by inscribing it with category and type words, like \"weapons\", \"gems, keys\" or \"heavy armor\": "
                + "examine the pack and type in its inscription box, whether you carry it or it is in your open bank. "
                + "A Salvage Crate, Quiver, Component Pouch or Trophy Pack is filled first with what its name says, then inscribed packs, "
                + "and a pack tagged for a type beats one tagged for its whole category (\"swords\" before \"weapons\"). "
                + "That goes for bank deposits, withdrawals and sorts, and for /sort with the packs you carry. "
                + "Inscribe a pack you carry with \"keep\" and neither /bank deposit nor /sort takes anything out of it. "
                + "Inscribe one with \"deposit\" and opening your bank offers to deposit everything in it.\n\n"
                + "Gear is weapons (including casters), armor (including shields and clothing), jewelry and trinkets. "
                + "Consumables are food, potions and healing kits; keys include lockpicks; gems include jewels; animal is hides, bones and meat. "
                + "The salvage types are the salvage each tinkering skill uses, and imbue is the imbue gems."
        );
    }

    // --- /bank deposit ---

    private static void HandleDeposit(Session session, string[] parameters)
    {
        if (parameters.Length == 0 || !TryParseFilter(parameters[0], out var filter))
        {
            Send(session, $"Usage: /bank deposit <category or type>. Categories: {CategoryList}. Types like swords, heavy or rings work too; /bank lists them.");
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

        Send(session, report.DescribeDeposit(what));
    }

    // --- /bank sort ---

    private static void HandleSort(Session session, string[] parameters)
    {
        BankCategory? filter = null;

        if (parameters.Length > 0 && !TryParseFilter(parameters[0], out filter))
        {
            Send(session, $"Usage: /bank sort [category or type]. Categories: {CategoryList}. Types like swords, heavy or rings work too; /bank lists them.");
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
            lines.Add($"Moved {Items(report.Moved)} ({report.DescribeDestinations()}).");
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

        if (filter != null && !player.GetBankPacks(bank).Any(p => (NamedPacks.Collects(p) & filter.Value) != 0))
        {
            var what = BankCategories.Describe(filter.Value);
            lines.Add($"No pack in your bank collects {what}. Inscribe one with \"{what}\" to collect them.");
        }

        Send(session, string.Join("\n", lines));
    }

    // --- /bank search ---

    private static void HandleSearch(Session session, string[] parameters)
    {
        var query = string.Join(" ", parameters).Trim();

        if (query.Length == 0)
        {
            Send(session, "Usage: /bank search <name, category or type>. For example: /bank search pyreal, /bank search trinkets, /bank search mace, /bank search longsword");
            return;
        }

        var bank = GetReadyBank(session);
        if (bank == null)
        {
            return;
        }

        var found = FindInBank(session.Player, bank, query);

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
                .Select(c => (c.Item.StackSize ?? 1) > 1 ? $"{c.Item.NameWithMaterial} ({c.Item.StackSize:N0})" : c.Item.NameWithMaterial)
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

    // --- /bank withdraw ---

    private static void HandleWithdraw(Session session, string[] parameters)
    {
        int? limit = null;

        if (parameters.Length > 0 && TryParseCount(parameters[0], out var count))
        {
            limit = count;
            parameters = parameters.Skip(1).ToArray();
        }

        var query = string.Join(" ", parameters).Trim();

        if (query.Length == 0 || query.Equals("all", StringComparison.OrdinalIgnoreCase) || limit <= 0)
        {
            Send(
                session,
                "Usage: /bank withdraw [how many] <name, category or type>. For example: /bank withdraw healing kits, /bank withdraw 200 arrows, /bank withdraw mace"
            );
            return;
        }

        var player = session.Player;
        var bank = GetReadyBank(session);
        if (bank == null)
        {
            return;
        }

        var found = FindInBank(player, bank, query);

        if (found.Count == 0)
        {
            Send(session, $"Nothing in your bank matches \"{query}\".");
            return;
        }

        if (limit != null || found.Count <= WithdrawConfirmAbove)
        {
            Withdraw(session, bank, query, limit);
            return;
        }

        // A short name like "a" matches most of the bank, so a big withdrawal is asked about first.
        var confirmation = new Confirmation_Custom(
            player.Guid,
            () =>
            {
                // The bank may have closed or changed while the question was up.
                var openBank = GetReadyBank(session);
                if (openBank != null)
                {
                    Withdraw(session, openBank, query, null);
                }
            }
        );

        if (!player.ConfirmationManager.EnqueueSend(confirmation, $"Withdraw all {Items(found.Count)} in your bank that match \"{query}\"?"))
        {
            Send(session, "A confirmation is already pending.");
        }
    }

    private static void Withdraw(Session session, Storage bank, string query, int? limit)
    {
        var items = FindInBank(session.Player, bank, query).Select(c => c.Item).ToList();

        if (items.Count == 0)
        {
            Send(session, $"Nothing in your bank matches \"{query}\" any more.");
            return;
        }

        var report = session.Player.WithdrawFromBank(bank, items, limit);

        var lines = new List<string>();

        if (report.Taken.Count > 0)
        {
            lines.Add("Withdrew from your bank:");

            var listed = 0;

            foreach (var group in report.Taken.GroupBy(t => t.Into))
            {
                if (listed >= MaxSearchResults)
                {
                    break;
                }

                var names = group
                    .Take(MaxSearchResults - listed)
                    .Select(t => t.Amount == null ? t.Name : $"{t.Name} ({t.Amount:N0})")
                    .ToList();

                listed += names.Count;
                lines.Add($"  Into {group.Key}: {string.Join(", ", names)}");
            }

            if (report.Taken.Count > listed)
            {
                lines.Add($"  ...and {report.Taken.Count - listed} more.");
            }
        }

        var stoppedShort = report.NoRoom + report.TooHeavy + report.Failed > 0;

        if (limit != null && report.Units < limit && !stoppedShort)
        {
            lines.Add($"Your bank only had {report.Units:N0} of those.");
        }

        if (report.NoRoom > 0)
        {
            lines.Add($"{Items(report.NoRoom)} didn't fit: your packs are full.");
        }

        if (report.TooHeavy > 0)
        {
            lines.Add($"{Items(report.TooHeavy)} stayed in your bank, all or in part: you can't carry more without going over your burden limit.");
        }

        if (report.Failed > 0)
        {
            lines.Add($"{Items(report.Failed)} couldn't be moved.");
        }

        Send(session, string.Join("\n", lines));
    }

    /// <summary>
    /// A leading count for /bank withdraw: digits, commas allowed ("1,000").
    /// </summary>
    private static bool TryParseCount(string word, out int count)
    {
        return int.TryParse(word.Replace(",", ""), NumberStyles.None, CultureInfo.InvariantCulture, out count);
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
        var inscribed = BankCategories.ParseInscription(pack.Inscription);
        var tags = BankCategories.Describe(inscribed);

        // Named packs (Quiver, Trophy Pack, ...) collect their kind by name, first.
        var named = NamedPacks.CategoryOf(NamedPacks.KindOf(pack));
        if (named != BankCategory.None)
        {
            var byName = $"{BankCategories.Describe(named)} by name";
            tags = inscribed.IsEmpty ? byName : $"{byName}, {tags}";
        }

        return $"{pack.Name} [{tags}] {used} of {pack.ItemCapacity ?? 0} slots used";
    }

    // --- helpers ---

    /// <summary>
    /// Your items in the bank and its packs that match query, in bank order.
    /// A category (weapons), weapon class (mace, two-handed) or item type (gem) word matches by kind.
    /// Every query also matches by name, material included and spaces ignored, so "portal" still finds a
    /// "Portal Gem", "longsword" finds a "Long Sword" and "iron jitte" finds an iron jitte.
    /// </summary>
    private static List<(WorldObject Item, Container Container)> FindInBank(Player player, Storage bank, string query)
    {
        var isCategory = BankCategories.TryParse(query, out var category);
        var isWeaponClass = BankSearch.TryParseWeaponClass(query, out var weaponClass);
        var isItemType = BankCategories.TryParseItemType(query, out var itemType);

        bool Matches(WorldObject item) =>
            (isCategory && (BankCategories.Tags(item) & category) != 0)
            || (isWeaponClass && (BankSearch.GetWeaponClass(item) & weaponClass) != 0)
            || (isItemType && (item.ItemType & itemType) != 0)
            || BankSearch.NameMatches(item.NameWithMaterial, query);

        return player.GetBankContents(bank).Where(c => Matches(c.Item)).ToList();
    }

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

    private static string Items(int count) => Plural(count, "item");

    private static string Plural(int count, string noun) => BankReport.Plural(count, noun);

    private static void Send(Session session, string text)
    {
        session.Network.EnqueueSend(new GameMessageSystemChat(text, ChatMessageType.System));
    }
}
