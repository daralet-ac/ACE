using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
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

    // /bank withdraw asks first before taking more than this many items without a count
    private const int WithdrawConfirmAbove = 20;

    private const string CategoryList =
        "all, gear, weapons, armor, jewelry, trinkets, salvage, ammo, animal, components, consumables, gems, keys, manastones, trophies, currency, misc";

    // The kinds inside a category (BankCategory.Kinds): a pack tagged for one beats a pack tagged for the whole category.
    private const string TypeList =
        "Weapons: swords, maces, axes, spears, daggers, staffs, unarmed (ua), two-handed (2h), bows, crossbows, atlatls, thrown, casters.\n"
        + "Armor: heavy, light and cloth (by weight class), clothing.\n"
        + "Jewelry: necklaces, rings, bracelets.\n"
        + "Salvage: blacksmithing, tailoring, spellcrafting, woodworking, jewelcrafting, imbue.\n"
        + "Tiers: t0 to t7, the loot tier a piece of gear dropped at, lowest to highest.";

    // /bank log reads the database too.
    private const int LogDefaultEntries = 20;
    private static readonly TimeSpan LogCooldown = TimeSpan.FromSeconds(10);
    private static readonly ConcurrentDictionary<uint, DateTime> LastLogCheck = new();

    // /bank balance reads every offline character's possessions from the database, so it can't be spammed.
    private static readonly TimeSpan BalanceCooldown = TimeSpan.FromSeconds(30);
    private static readonly ConcurrentDictionary<uint, DateTime> LastBalanceCheck = new();

    [CommandHandler(
        "bank",
        AccessLevel.Player,
        CommandHandlerFlag.RequiresWorld,
        0,
        "Bank tools: deposit, withdraw, sort, search and combine salvage in your bank, check your balance and bank log, and see how your packs are tagged. Use /bank for help.",
        "deposit|withdraw|sort|search|combine|balance|packs|inscribe|autosort|log ..."
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
            case "inscribe":
            case "tag":
                HandleInscribe(session, rest);
                break;
            case "autosort":
                HandleAutoSort(session, rest);
                break;
            case "log":
            case "history":
                HandleLog(session, rest);
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
                + "  /bank deposit <category, type or tier> - Moves those items from your packs into your bank.\n"
                + "  /bank deposit packs - Empties your packs inscribed \"deposit\" into your bank.\n"
                + "  /bank withdraw [how many] <name, category, type or tier> - Takes those items out of your bank into your packs, "
                + "filling named and inscribed packs first, then your main pack. With a number, takes that many (\"/bank withdraw 200 arrows\"). "
                + "Stops at your burden limit, and never fills a pack inscribed \"deposit\".\n"
                + "  /bank sort [category, type or tier] - Files items into their inscribed packs, combines stacks and puts everything in order.\n"
                + "  /bank autosort on|off - Sorts your bank after every deposit and withdrawal.\n"
                + "  /bank search <name, category, type or tier> - Lists matching items in your bank and where they are. "
                + "A weapon's own name works too, like longsword or jitte.\n"
                + "  /bank combine - Combines salvage bags in your bank that have the same material and workmanship.\n"
                + "  /bank balance - Shows the pyreals and trade notes in your bank and across your account. Works anywhere.\n"
                + "  /bank packs - Shows your bank space and how each pack is tagged.\n"
                + "  /bank inscribe [number] <tags> - Inscribes a pack in your bank: the one with that number in /bank packs, "
                + "or the one you last examined (\"/bank inscribe 2 clear\" clears pack 2).\n"
                + "  /bank log [how many] - Your account's recent bank deposits, withdrawals and salvage combines, by any of your characters. Works anywhere.\n\n"
                + $"Categories: {CategoryList}.\n\n"
                + "Types, each part of a category, and tiers:\n"
                + TypeList
                + "\n\n"
                + "Tag a pack by inscribing it with any number of category and type words, like \"weapons\", \"gems, keys\" or \"heavy armor\", "
                + "and tiers to take only those: \"swords t6\" is swords of tier t6, \"t6\" alone any gear of tier t6. "
                + "To inscribe a pack you carry, examine it and type in its inscription box; "
                + "for a pack in your bank, use /bank inscribe with its number from /bank packs. "
                + "A Salvage Crate, Quiver, Component Pouch or Trophy Pack is filled first with what its name says, then inscribed packs, "
                + "and a pack tagged for a type beats one tagged for its whole category (\"swords\" before \"weapons\"). "
                + "That goes for bank deposits, withdrawals and sorts, and for /sort with the packs you carry. "
                + "Inscribe a pack you carry with \"keep\" and neither /bank deposit nor /sort takes anything out of it. "
                + "Inscribe one with \"deposit\" and opening your bank offers to deposit everything in it.\n\n"
                + "Gear is weapons (including casters), armor (including shields and clothing), jewelry and trinkets. "
                + "Consumables are food, potions and healing kits; keys include lockpicks; gems include jewels; animal is hides, bones and meat; "
                + "currency is pyreals and trade notes; misc is everything no other category takes. "
                + "The salvage types are the salvage each tinkering skill uses, and imbue is the imbue gems."
        );
    }

    // --- /bank deposit ---

    private static void HandleDeposit(Session session, string[] parameters)
    {
        var text = string.Join(" ", parameters).Trim();
        var fromDepositPacks = text.Equals("packs", StringComparison.OrdinalIgnoreCase) || text.Equals("pack", StringComparison.OrdinalIgnoreCase);

        BankCategory? filter = null;

        if (!fromDepositPacks && (text.Length == 0 || !TryParseFilter(text, out filter)))
        {
            Send(
                session,
                $"Usage: /bank deposit <category, type or tier>, or /bank deposit packs. Categories: {CategoryList}. "
                    + "Types like swords, heavy or rings and tiers like t6 work too, and together (\"swords t6\"); /bank lists them."
            );
            return;
        }

        var player = session.Player;
        var bank = GetReadyBank(session);
        if (bank == null)
        {
            return;
        }

        if (fromDepositPacks && !player.HasDepositPacks)
        {
            Send(session, "You carry no pack inscribed \"deposit\". Inscribe one, and /bank deposit packs empties it into your bank.");
            return;
        }

        var report = player.DepositToBank(bank, filter, fromDepositPacks);

        var what = fromDepositPacks ? "items in your \"deposit\" packs"
            : filter == null ? "items"
            : BankCategories.Describe(filter.Value);

        SendWithAutoSort(session, bank, report.DescribeDeposit(what), report.Moved + report.StacksCombined > 0);
    }

    // --- /bank sort ---

    private static void HandleSort(Session session, string[] parameters)
    {
        BankCategory? filter = null;

        if (parameters.Length > 0 && !TryParseFilter(string.Join(" ", parameters), out filter))
        {
            Send(
                session,
                $"Usage: /bank sort [category, type or tier]. Categories: {CategoryList}. "
                    + "Types like swords, heavy or rings and tiers like t6 work too; /bank lists them."
            );
            return;
        }

        var player = session.Player;
        var bank = GetReadyBank(session);
        if (bank == null)
        {
            return;
        }

        var report = player.SortBank(bank, filter);

        var lines = report.DescribeSort();

        lines.Insert(0, lines.Count == 0 ? "Your bank is already sorted." : "Bank sorted.");

        if (filter != null && !player.GetBankPacks(bank).Any(p => BankCategories.CouldCollect(NamedPacks.Collects(p), filter.Value)))
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
                "Usage: /bank withdraw [how many] <name, category, type or tier>. For example: /bank withdraw healing kits, /bank withdraw 200 arrows, "
                    + "/bank withdraw mace, /bank withdraw 2 swords t6."
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

        SendWithAutoSort(session, bank, string.Join("\n", lines), report.Units > 0);
    }

    /// <summary>
    /// A leading count for /bank withdraw: digits, commas allowed ("1,000").
    /// </summary>
    private static bool TryParseCount(string word, out int count)
    {
        return int.TryParse(word.Replace(",", ""), NumberStyles.None, CultureInfo.InvariantCulture, out count);
    }

    // --- /bank autosort ---

    private static void HandleAutoSort(Session session, string[] parameters)
    {
        var player = session.Player;
        var setting = parameters.Length > 0 ? parameters[0].ToLowerInvariant() : "";

        switch (setting)
        {
            case "on":
                player.BankAutoSort = true;
                Send(session, "Autosort is on: every /bank deposit and withdrawal is followed by a /bank sort.");
                break;
            case "off":
                player.BankAutoSort = false;
                Send(session, "Autosort is off.");
                break;
            default:
                Send(session, $"Autosort is {(player.BankAutoSort ? "on" : "off")}. Use /bank autosort on or /bank autosort off.");
                break;
        }
    }

    // --- /bank inscribe ---

    // The client only lets a player type an inscription on something they carry (admins excepted), so a pack in the
    // bank is inscribed with this. Carried packs are inscribed from the appraisal panel.
    //
    // "The pack you last examined" is RequestedAppraisalTarget, which every appraisal request overwrites, including the
    // ones an add-on sends when it scans items. So the pack can also be named by its number in /bank packs, which no
    // scan can change, and a last-examined item that isn't one of your bank packs is refused with its name.
    private static void HandleInscribe(Session session, string[] parameters)
    {
        int? packNumber = null;

        if (parameters.Length > 0 && int.TryParse(parameters[0], NumberStyles.None, CultureInfo.InvariantCulture, out var number))
        {
            packNumber = number;
            parameters = parameters.Skip(1).ToArray();
        }

        var text = string.Join(" ", parameters).Trim();

        if (text.Length == 0)
        {
            Send(
                session,
                "Usage: /bank inscribe <number> <tags>, with the pack's number from /bank packs, or examine a pack in your bank and use /bank inscribe <tags>. "
                    + "For example: /bank inscribe 2 weapons, armor. Use /bank inscribe <number> clear to remove it."
            );
            return;
        }

        if (text.Length > MaxInscriptionLength)
        {
            Send(session, $"That inscription is too long ({MaxInscriptionLength} characters at most).");
            return;
        }

        var player = session.Player;
        var bank = GetReadyBank(session);
        if (bank == null)
        {
            return;
        }

        var bankPacks = player.GetBankPacks(bank);
        Container pack;

        if (packNumber != null)
        {
            if (packNumber < 1 || packNumber > bankPacks.Count)
            {
                Send(session, $"Your bank has {Plural(bankPacks.Count, "pack")}. /bank packs shows their numbers.");
                return;
            }

            pack = bankPacks[packNumber.Value - 1];
        }
        else
        {
            var targetGuid = player.RequestedAppraisalTarget;
            var examined = targetGuid == null ? null : player.FindItemInOpenBank(targetGuid.Value);

            if (examined is not Container examinedPack || !bankPacks.Contains(examinedPack))
            {
                var name = examined?.Name
                    ?? (targetGuid == null ? null : player.FindObject(targetGuid.Value, Player.SearchLocations.Everywhere, out _, out _, out _)?.Name);

                Send(
                    session,
                    (name == null ? "Examine a pack in your bank first" : $"The last thing you examined was {name}, not a pack in your bank")
                        + ". Use /bank inscribe <number> <tags> with the pack's number from /bank packs; add-ons that examine items can change what you examined last."
                );
                return;
            }

            pack = examinedPack;
        }

        var clear = text.Equals("clear", StringComparison.OrdinalIgnoreCase);

        if (!player.TrySetInscription(pack, clear ? null : text))
        {
            Send(session, $"{pack.Name} was inscribed by {pack.ScribeName ?? "someone else"}. Only they can change it.");
            return;
        }

        // A pack in the bank is saved with the bank, which only happens when the landblock saves; do it now.
        pack.SaveBiotaToDatabase();

        // the number lets the player check it was the pack they meant
        var which = $"{pack.Name} (bank pack {bankPacks.IndexOf(pack) + 1})";

        if (clear)
        {
            Send(session, $"Cleared the inscription on {which}.");
            return;
        }

        var tags = BankCategories.ParseInscription(text);

        Send(
            session,
            tags.IsEmpty
                ? $"Inscribed {which}. It has no bank tags; see /bank for the words (categories, types, tiers, keep, deposit)."
                : $"Inscribed {which} [{BankCategories.Describe(tags)}]."
        );
    }

    // --- /bank log ---

    private static void HandleLog(Session session, string[] parameters)
    {
        var count = LogDefaultEntries;

        if (parameters.Length > 0 && (!TryParseCount(parameters[0], out count) || count <= 0))
        {
            Send(session, $"Usage: /bank log [how many], up to {BankActivityLog.KeepPerAccount}.");
            return;
        }

        count = Math.Min(count, BankActivityLog.KeepPerAccount);

        var player = session.Player;
        var now = DateTime.UtcNow;

        if (LastLogCheck.TryGetValue(player.Guid.Full, out var last) && now - last < LogCooldown)
        {
            var wait = (int)Math.Ceiling((LogCooldown - (now - last)).TotalSeconds);
            Send(session, $"You read your bank log a moment ago. Try again in {Plural(wait, "second")}.");
            return;
        }

        LastLogCheck[player.Guid.Full] = now;

        BankActivityLog.Read(
            player.Account.AccountId,
            count,
            entries =>
                WorldManager.EnqueueAction(
                    new ActionEventDelegate(() =>
                    {
                        // gone while we read
                        if (session.Player != player)
                        {
                            return;
                        }

                        if (entries == null)
                        {
                            Send(session, "Your bank log couldn't be read right now. Try again later.");
                            return;
                        }

                        if (entries.Count == 0)
                        {
                            Send(session, "Your bank log is empty. Deposits, withdrawals and salvage combines by any character on your account show up here.");
                            return;
                        }

                        var readAt = DateTime.UtcNow;
                        var lines = new List<string> { $"Your account's last {Plural(entries.Count, "bank move")}, newest first:" };

                        foreach (var entry in entries)
                        {
                            lines.Add($"  {BankActivityLog.DescribeAge(readAt - entry.CreatedAtUtc)}: {entry.CharacterName} {entry.Action} {entry.Details}");
                        }

                        Send(session, string.Join("\n", lines));
                    })
                )
        );
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

            var bankPacks = player.GetBankPacks(bank);

            for (var i = 0; i < bankPacks.Count; i++)
            {
                lines.Add($"  {i + 1}. {DescribePack(bankPacks[i])}");
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
        // Tiers narrow whatever else the query names: "swords t6", "iron jitte tier 7", or "t6" alone.
        var tiers = BankCategory.None;
        var rest = new List<string>();

        query = Regex.Replace(query, @"\btier\s+(\d)\b", "t$1", RegexOptions.IgnoreCase);

        foreach (var word in query.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (BankCategories.TryParse(word, out var tag) && (tag & ~BankCategory.Tiers) == 0)
            {
                tiers |= tag;
            }
            else if (!word.Equals("tier", StringComparison.OrdinalIgnoreCase))
            {
                rest.Add(word);
            }
        }

        var contents = player.GetBankContents(bank);

        if (tiers != BankCategory.None)
        {
            contents = contents.Where(c => BankCategories.Matches(BankCategories.Tags(c.Item), tiers)).ToList();

            if (rest.Count == 0)
            {
                return contents;
            }

            query = string.Join(" ", rest);
        }

        var isCategory = BankCategories.TryParse(query, out var category);
        var isWeaponClass = BankSearch.TryParseWeaponClass(query, out var weaponClass);
        var isItemType = BankCategories.TryParseItemType(query, out var itemType);

        bool Matches(WorldObject item) =>
            (isCategory && (BankCategories.Tags(item) & category) != 0)
            || (isWeaponClass && (BankSearch.GetWeaponClass(item) & weaponClass) != 0)
            || (isItemType && (item.ItemType & itemType) != 0)
            || BankSearch.NameMatches(item.NameWithMaterial, query);

        return contents.Where(c => Matches(c.Item)).ToList();
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
    /// "all" is a null filter; anything else must name categories, types or tiers, as a pack inscription does:
    /// "weapons", "two handed", "heavy armor", "swords t6".
    /// </summary>
    private static bool TryParseFilter(string text, out BankCategory? filter)
    {
        filter = null;

        if (text.Trim().Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (BankCategories.TryParse(text, out var category))
        {
            filter = category;
            return true;
        }

        var tags = BankCategories.ParseInscription(text).Categories;
        if (tags == BankCategory.None)
        {
            return false;
        }

        filter = tags;
        return true;
    }

    /// <summary>
    /// Sends the message for a deposit or withdrawal, followed by what autosort did, if it is on.
    /// </summary>
    private static void SendWithAutoSort(Session session, Storage bank, string text, bool anythingMoved)
    {
        var autoSort = BankReport.DescribeAutoSort(session.Player.AutoSortBank(bank, anythingMoved));

        Send(session, autoSort == null ? text : $"{text}\n{autoSort}");
    }

    private static string Items(int count) => Plural(count, "item");

    private static string Plural(int count, string noun) => BankReport.Plural(count, noun);

    private static void Send(Session session, string text)
    {
        session.Network.EnqueueSend(new GameMessageSystemChat(text, ChatMessageType.System));
    }
}
