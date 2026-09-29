using System;
using System.Collections.Generic;
using System.Linq;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Entity;
using ACE.Server.Managers;
using ACE.Server.Network.GameEvent.Events;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.WorldObjects.Logging;

namespace ACE.Server.WorldObjects;

/// <summary>
/// What a /bank deposit or /bank sort did, for the message back to the player.
/// </summary>
public sealed class BankReport
{
    /// <summary>Items moved into (deposit) or within (sort) the bank.</summary>
    public int Moved { get; set; }

    /// <summary>Stacks that were emptied into another stack in the bank.</summary>
    public int StacksCombined { get; set; }

    /// <summary>Items that had somewhere better to go, but it was full.</summary>
    public int NoRoom { get; set; }

    /// <summary>Items that can never be banked (attuned).</summary>
    public int Attuned { get; set; }

    /// <summary>Moves the normal inventory checks turned down.</summary>
    public int Failed { get; set; }

    /// <summary>Bank containers whose items were put back in order.</summary>
    public int Reordered { get; set; }

    /// <summary>
    /// How many items went into each bank container, by name, in the order they were first used.
    /// The bank itself has a null name.
    /// </summary>
    public List<(string Name, int Count)> MovedInto { get; } = [];

    public void CountMove(Container target, Storage bank)
    {
        var name = target == bank ? null : target.Name;

        var index = MovedInto.FindIndex(m => m.Name == name);
        if (index < 0)
        {
            MovedInto.Add((name, 1));
        }
        else
        {
            MovedInto[index] = (name, MovedInto[index].Count + 1);
        }
    }
}

/// <summary>
/// Bulk bank operations for the /bank command.
/// Every move goes through the same checks and code as dragging the item by hand
/// (HandleActionPutItemInContainer_Verify, DoHandleActionPutItemInContainer, DoHandleActionStackableMerge),
/// without the walk-to and pickup animation, so the bank bookkeeping in Player_Inventory_Banking still runs.
/// </summary>
public partial class Player
{
    /// <summary>
    /// A place in the bank an item can go: the bank itself or one of its side packs.
    /// </summary>
    private sealed class BankSpot
    {
        public Container Container { get; init; }
        public bool IsMain { get; init; }
        public BankPackTags Tags { get; init; }
        public int FreeSlots { get; set; }
        public int Order { get; init; }
    }

    // Lower is a better home for an item, see RankBankSpot.
    private const int RankNeutral = 3;
    private const int RankMisfiled = 4;

    /// <summary>
    /// The bank chest this player has open and is standing at, or null.
    /// </summary>
    public Storage GetOpenBank()
    {
        if (CurrentLandblock?.GetObject(LastOpenedContainerId) is not Storage bank)
        {
            return null;
        }

        if (!bank.IsOpen || bank.Viewer != Guid.Full)
        {
            return null;
        }

        if (!CurrentLandblock.WithinUseRadius(this, bank.Guid, out _))
        {
            return null;
        }

        return bank;
    }

    /// <summary>
    /// Moves every item matching filter (null = everything) from the main pack and side packs into the bank.
    /// Packs inscribed "keep" are skipped, and so are the packs themselves.
    /// Stackable items first top up stacks already in the bank, then each item goes to the best place with room:
    /// a bank pack inscribed for it, then an untagged specialized pack that takes it, then the bank, then an untagged pack.
    /// </summary>
    public BankReport DepositToBank(Storage bank, BankCategory? filter)
    {
        var report = new BankReport();
        var touched = new HashSet<Container>();

        var spots = GetBankSpots(bank);
        var bankStacks = GetBankItems(bank).Where(i => i is Stackable && (i.MaxStackSize ?? 1) > 1).ToList();

        var anythingMoved = false;

        foreach (var item in GetDepositCandidates(filter, report))
        {
            var category = BankCategories.Classify(item);

            var mergedAll = TopUpBankStacks(item, bankStacks, touched, out var mergedAny);
            anythingMoved |= mergedAny;

            if (mergedAll)
            {
                report.StacksCombined++;
                continue;
            }

            var target = FindBankSpot(spots, item, category, null, out _);
            if (target == null)
            {
                report.NoRoom++;
                continue;
            }

            if (!TryMoveItemInBank(item, target.Container))
            {
                report.Failed++;
                continue;
            }

            target.FreeSlots--;
            touched.Add(target.Container);
            report.Moved++;
            report.CountMove(target.Container, bank);
            anythingMoved = true;

            if (item is Stackable && (item.StackSize ?? 1) < (item.MaxStackSize ?? 1))
            {
                bankStacks.Add(item);
            }
        }

        if (anythingMoved)
        {
            Session.Network.EnqueueSend(
                new GameMessagePrivateUpdatePropertyInt(this, PropertyInt.EncumbranceVal, EncumbranceVal ?? 0)
            );

            UpdateCoinValue();

            EnqueueBroadcast(new GameMessageSound(Guid, Sound.DropItem));

            SaveBankContainers(bank, touched);
        }

        return report;
    }

    /// <summary>
    /// Tidies the bank. For a full sort (null filter) partial stacks are combined first.
    /// Then items matching filter move into the packs inscribed for them, items in a pack inscribed for
    /// something else move out, and the bank and its packs are put in order: by category, then name.
    /// A sort for one category only reorders the bank and the packs inscribed for that category.
    /// </summary>
    public BankReport SortBank(Storage bank, BankCategory? filter)
    {
        var report = new BankReport();
        var touched = new HashSet<Container>();

        if (filter == null)
        {
            report.StacksCombined = CombineBankStacks(bank, touched);
        }

        var spots = GetBankSpots(bank);

        foreach (var spot in spots)
        {
            foreach (var item in GetMyBankItems(spot.Container, bank))
            {
                if (!BankCategories.Matches(item, filter))
                {
                    continue;
                }

                var category = BankCategories.Classify(item);
                var currentRank = RankBankSpot(spot, item, category) ?? RankMisfiled;

                var target = FindBankSpot(spots, item, category, spot, out var bestRankIgnoringRoom);

                // Only a strictly better home is worth a move, so an item sitting in the bank or an
                // untagged pack stays put unless a pack is meant for it.
                var targetRank = target == null ? int.MaxValue : RankBankSpot(target, item, category).Value;
                if (targetRank >= currentRank)
                {
                    if (bestRankIgnoringRoom < currentRank)
                    {
                        report.NoRoom++;
                    }

                    continue;
                }

                if (!TryMoveItemInBank(item, target.Container))
                {
                    report.Failed++;
                    continue;
                }

                target.FreeSlots--;
                spot.FreeSlots++;
                touched.Add(spot.Container);
                touched.Add(target.Container);
                report.Moved++;
                report.CountMove(target.Container, bank);
            }
        }

        foreach (var spot in spots)
        {
            if (filter != null && !spot.IsMain && (spot.Tags.Categories & filter.Value) == 0)
            {
                continue;
            }

            if (ReorderBankContainer(spot.Container, bank))
            {
                touched.Add(spot.Container);
                report.Reordered++;
            }
        }

        SaveBankContainers(bank, touched);

        return report;
    }

    /// <summary>
    /// How many salvage bags in the bank /bank combine would empty into others.
    /// </summary>
    public int CountBankSalvageToCombine(Storage bank)
    {
        return GetBankSalvageGroups(bank).Sum(bags => SalvagePourPlanner.CountEmptied(GetBagUnits(bags), SalvagePourPlanner.Plan(GetBagUnits(bags))));
    }

    /// <summary>
    /// The pyreals in the bank and its packs: coins, and trade notes at face value (their Value, as house payments count them).
    /// </summary>
    public (long Coins, long Notes, int NoteCount) GetBankMoney(Storage bank)
    {
        long coins = 0;
        long notes = 0;
        var noteCount = 0;

        foreach (var (item, _) in GetBankContents(bank))
        {
            if (item.WeenieType == WeenieType.Coin)
            {
                coins += item.Value ?? 0;
            }
            else if (item.IsTradeNote)
            {
                notes += item.Value ?? 0;
                noteCount += item.StackSize ?? 1;
            }
        }

        return (coins, notes, noteCount);
    }

    /// <summary>
    /// Combines salvage bags anywhere in the bank that share a material and workmanship, like /salvage combine
    /// does for the bags you carry: the fullest bags are topped up from the emptiest, and empty bags are removed.
    /// Returns how many bags were emptied.
    /// </summary>
    public int CombineBankSalvage(Storage bank)
    {
        var touched = new HashSet<Container>();
        var emptied = 0;

        foreach (var bags in GetBankSalvageGroups(bank))
        {
            var units = GetBagUnits(bags);
            var plan = SalvagePourPlanner.Plan(units);

            // Pouring that frees no bag would only nudge workmanship around.
            if (SalvagePourPlanner.CountEmptied(units, plan) == 0)
            {
                continue;
            }

            var changed = new HashSet<WorldObject>();

            foreach (var pour in plan)
            {
                var source = bags[pour.Source];
                var target = bags[pour.Target];

                if (Salvage.PourSalvageBag(source, target, pour.Amount) <= 0)
                {
                    continue;
                }

                changed.Add(source);
                changed.Add(target);
            }

            foreach (var bag in changed)
            {
                if (bag.Container is Container container)
                {
                    touched.Add(container);
                }

                if ((bag.Structure ?? 0) > 0)
                {
                    var material = (ACE.Entity.Enum.MaterialType)(bag.GetProperty(PropertyInt.MaterialType) ?? 0);
                    Salvage.RefreshSalvageBagIcon(this, bag, material, (int)(bag.Workmanship ?? 1));
                    Session.Network.EnqueueSend(new GameMessageUpdateObject(bag));
                    continue;
                }

                // Emptied: take it out of the bank (or its pack) and delete it, as a merge that empties a stack does.
                Session.Network.EnqueueSend(new GameMessageInventoryRemoveObject(bag));

                if (bank.TryRemoveFromInventory(bag.Guid, out var removed))
                {
                    removed.Destroy();
                    emptied++;
                }
            }
        }

        SaveBankContainers(bank, touched);

        if (emptied > 0 && PropertyManager.GetBool("banking_system_logging").Item)
        {
            _log.Information(
                "(BANKING - SALVAGE COMBINE in BANK)\n PLAYER: {@Player}\n BAGS EMPTIED: {Count}",
                new BankLogPlayer(Name, Account.AccountId),
                emptied
            );
        }

        return emptied;
    }

    /// <summary>
    /// The salvage bags in the bank, in groups that /bank combine may pour together:
    /// the same material and the same workmanship to the nearest whole number, as /salvage combine groups them.
    /// </summary>
    private List<List<WorldObject>> GetBankSalvageGroups(Storage bank)
    {
        return GetBankItems(bank)
            .Where(i => i.WeenieType == WeenieType.Salvage)
            .GroupBy(i => ((int)(i.MaterialType ?? ACE.Entity.Enum.MaterialType.Unknown), (int)Math.Round(i.Workmanship ?? 1)))
            .Where(g => g.Count() > 1)
            .Select(g => g.ToList())
            .ToList();
    }

    private static List<(int Units, int MaxUnits)> GetBagUnits(List<WorldObject> bags)
    {
        return bags.Select(b => ((int)(b.Structure ?? 0), (int)(b.MaxStructure ?? 1000))).ToList();
    }

    /// <summary>
    /// Every item of yours in the bank and its side packs, not counting the packs themselves.
    /// </summary>
    public List<(WorldObject Item, Container Container)> GetBankContents(Storage bank)
    {
        var contents = new List<(WorldObject, Container)>();

        foreach (var item in GetMyBankItems(bank, bank))
        {
            contents.Add((item, bank));
        }

        foreach (var pack in GetBankPacks(bank))
        {
            foreach (var item in GetMyBankItems(pack, bank))
            {
                contents.Add((item, pack));
            }
        }

        return contents;
    }

    /// <summary>
    /// Your side packs in the bank, in order.
    /// </summary>
    public List<Container> GetBankPacks(Storage bank)
    {
        return bank.Inventory.Values
            .OfType<Container>()
            .Where(p => p.BankAccountId == Account.AccountId)
            .OrderBy(p => p.PlacementPosition ?? int.MaxValue)
            .ToList();
    }

    /// <summary>
    /// The items (not packs) in the bank itself or one of its packs, in slot order.
    /// Like SendBankVaultInventory, only items in the bank itself stamped with this account count;
    /// items inside a pack belong to whoever owns the pack.
    /// </summary>
    private List<WorldObject> GetMyBankItems(Container container, Storage bank)
    {
        return container.Inventory.Values
            .Where(i => !i.UseBackpackSlot)
            .Where(i => container != bank || i.BankAccountId == Account.AccountId)
            .OrderBy(i => i.PlacementPosition ?? int.MaxValue)
            .ToList();
    }

    private List<WorldObject> GetBankItems(Storage bank)
    {
        return GetBankContents(bank).Select(c => c.Item).ToList();
    }

    private List<BankSpot> GetBankSpots(Storage bank)
    {
        var spots = new List<BankSpot>
        {
            new()
            {
                Container = bank,
                IsMain = true,
                FreeSlots = bank.GetFreeInventorySlots(false),
                Order = 0,
            },
        };

        var order = 1;
        foreach (var pack in GetBankPacks(bank))
        {
            spots.Add(
                new BankSpot
                {
                    Container = pack,
                    Tags = BankCategories.ParseInscription(pack.Inscription),
                    FreeSlots = pack.GetFreeInventorySlots(false),
                    Order = order++,
                }
            );
        }

        return spots;
    }

    /// <summary>
    /// How good a home spot is for item, lower is better, or null if the item must not go there.
    /// 0: a pack inscribed for exactly this category. 1: a pack inscribed for it among other things (gear).
    /// 2: an untagged specialized pack that takes it (a quiver for arrows).
    /// 3 (neutral): the bank itself, or an untagged pack.
    /// null: a pack inscribed for other things, or a specialized pack that does not take this type.
    /// </summary>
    private static int? RankBankSpot(BankSpot spot, WorldObject item, BankCategory category)
    {
        if (!spot.Container.CanHoldItemType(item))
        {
            return null;
        }

        if (spot.IsMain)
        {
            return RankNeutral;
        }

        if (spot.Tags.Categories != BankCategory.None)
        {
            var fit = BankCategories.PackFit(spot.Tags.Categories, category);
            return fit == 0 ? null : 2 - fit;
        }

        return (spot.Container.MerchandiseItemTypes ?? 0) != 0 ? 2 : RankNeutral;
    }

    /// <summary>
    /// The best place with a free slot for item, not counting exclude.
    /// Neutral places are tried bank first, then untagged packs in order.
    /// bestRankIgnoringRoom: the rank of the best place whether or not it has room, int.MaxValue if none.
    /// </summary>
    private static BankSpot FindBankSpot(
        List<BankSpot> spots,
        WorldObject item,
        BankCategory category,
        BankSpot exclude,
        out int bestRankIgnoringRoom
    )
    {
        BankSpot best = null;
        var bestRank = int.MaxValue;
        bestRankIgnoringRoom = int.MaxValue;

        foreach (var spot in spots)
        {
            if (spot == exclude)
            {
                continue;
            }

            var rank = RankBankSpot(spot, item, category);
            if (rank == null)
            {
                continue;
            }

            bestRankIgnoringRoom = Math.Min(bestRankIgnoringRoom, rank.Value);

            if (spot.FreeSlots <= 0)
            {
                continue;
            }

            // spots are in bank-then-pack order, so on a tie the earlier one wins
            if (rank.Value < bestRank)
            {
                best = spot;
                bestRank = rank.Value;
            }
        }

        return best;
    }

    private List<WorldObject> GetDepositCandidates(BankCategory? filter, BankReport report)
    {
        var sources = new List<Container> { this };
        sources.AddRange(
            Inventory.Values
                .OfType<Container>()
                .Where(p => !BankCategories.ParseInscription(p.Inscription).Keep)
                .OrderBy(p => p.PlacementPosition ?? int.MaxValue)
        );

        var candidates = new List<WorldObject>();

        foreach (var source in sources)
        {
            var items = source.Inventory.Values
                .Where(i => !i.UseBackpackSlot)
                .OrderBy(i => i.PlacementPosition ?? int.MaxValue)
                .ToList();

            foreach (var item in items)
            {
                if (!BankCategories.Matches(item, filter))
                {
                    continue;
                }

                if (item.IsAttunedOrContainsAttuned)
                {
                    report.Attuned++;
                    continue;
                }

                // The same things HandleActionPutItemInContainer_Verify turns down, checked here so a bulk deposit
                // quietly leaves them behind instead of sending an error for each one.
                if (item.Stuck || !item.Guid.IsDynamic())
                {
                    continue;
                }

                if (IsTrading && item.IsBeingTradedOrContainsItemBeingTraded(ItemsInTradeWindow))
                {
                    continue;
                }

                if (item is PetDevice { Pet: not null })
                {
                    continue;
                }

                candidates.Add(item);
            }
        }

        return candidates;
    }

    /// <summary>
    /// Merges item into stacks already in the bank that have room. Returns true if all of it went.
    /// mergedAny: some of it went.
    /// </summary>
    private bool TopUpBankStacks(
        WorldObject item,
        List<WorldObject> bankStacks,
        HashSet<Container> touched,
        out bool mergedAny
    )
    {
        mergedAny = false;

        if (item is not Stackable)
        {
            return false;
        }

        foreach (var target in bankStacks.ToList())
        {
            if (!CanStackTogether(item, target))
            {
                continue;
            }

            var room = (target.MaxStackSize ?? 1) - (target.StackSize ?? 1);
            if (room <= 0)
            {
                bankStacks.Remove(target);
                continue;
            }

            var amount = Math.Min(room, item.StackSize ?? 1);
            var mergesAll = amount == (item.StackSize ?? 1);
            var targetContainer = target.Container as Container;

            if (!DoHandleActionStackableMerge(item, target, amount))
            {
                return false;
            }

            mergedAny = true;

            if (targetContainer != null)
            {
                touched.Add(targetContainer);
            }

            if (mergesAll)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Combines partial stacks of the same thing anywhere in the bank, largest stacks absorbing the smallest,
    /// the same way /sort does for the player's own packs. Returns how many stacks were emptied.
    /// </summary>
    private int CombineBankStacks(Storage bank, HashSet<Container> touched)
    {
        var combined = 0;

        var groups = GetBankItems(bank)
            .Where(i => i is Stackable && (i.MaxStackSize ?? 1) > 1)
            .GroupBy(i => (i.WeenieClassId, i.TrophyQuality, i.SpellDID, i.Spell2, i.BoostValue))
            .Where(g => g.Count() > 1);

        foreach (var group in groups)
        {
            var members = group.OrderByDescending(i => i.StackSize ?? 1).ToList();

            var targetIdx = 0;
            var sourceIdx = members.Count - 1;

            while (targetIdx < sourceIdx)
            {
                var target = members[targetIdx];
                var source = members[sourceIdx];

                var room = (target.MaxStackSize ?? 1) - (target.StackSize ?? 1);
                if (room <= 0)
                {
                    targetIdx++;
                    continue;
                }

                var amount = Math.Min(room, source.StackSize ?? 1);
                var mergesAll = amount == (source.StackSize ?? 1);
                var sourceContainer = source.Container as Container;
                var targetContainer = target.Container as Container;

                if (!DoHandleActionStackableMerge(source, target, amount))
                {
                    break;
                }

                if (sourceContainer != null)
                {
                    touched.Add(sourceContainer);
                }

                if (targetContainer != null)
                {
                    touched.Add(targetContainer);
                }

                if (mergesAll)
                {
                    combined++;
                    sourceIdx--;
                }
            }
        }

        return combined;
    }

    /// <summary>
    /// The same test HandleActionStackableMerge uses to decide whether two stacks may merge.
    /// </summary>
    private static bool CanStackTogether(WorldObject a, WorldObject b)
    {
        return a is Stackable
            && b is Stackable
            && a.Guid != b.Guid
            && a.WeenieClassId == b.WeenieClassId
            && a.TrophyQuality == b.TrophyQuality
            && a.SpellDID == b.SpellDID
            && a.Spell2 == b.Spell2
            && a.BoostValue == b.BoostValue;
    }

    /// <summary>
    /// Moves item into target (the bank or one of its packs), added at the end.
    /// The item can be in the player's packs or already in the bank.
    /// </summary>
    private bool TryMoveItemInBank(WorldObject item, Container target)
    {
        // While busy, _Verify would queue the move as the next pickup instead of doing it now.
        if (IsBusy)
        {
            return false;
        }

        var placement = target.Inventory.Values.Count(i => !i.UseBackpackSlot);

        if (
            !HandleActionPutItemInContainer_Verify(
                item.Guid.Full,
                target.Guid.Full,
                placement,
                out var itemRootOwner,
                out var foundItem,
                out var containerRootOwner,
                out var container,
                out var itemWasEquipped
            )
        )
        {
            return false;
        }

        if (foundItem != item || container != target || itemWasEquipped)
        {
            return false;
        }

        if (!DoHandleActionPutItemInContainer(item, itemRootOwner, false, container, containerRootOwner, placement))
        {
            return false;
        }

        if (itemRootOwner == this)
        {
            item.EmoteManager.OnDrop(this);
        }

        return true;
    }

    /// <summary>
    /// Puts a bank container's items in /bank sort order. Returns false if they already were.
    /// The client is told each item's new slot, the same way /sort reorders a salvage crate.
    /// </summary>
    private bool ReorderBankContainer(Container container, Storage bank)
    {
        var sorted = GetMyBankItems(container, bank)
            .OrderBy(i => BankCategories.SortOrder(BankCategories.Classify(i)))
            .ThenBy(i => i.WeenieType == WeenieType.Salvage ? Salvage.GetSalvageBagSortKey(i) : default)
            .ThenBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
            .ThenByDescending(i => i.StackSize ?? 1)
            .ThenBy(i => i.Guid.Full)
            .ToList();

        var inOrder = true;
        for (var i = 0; i < sorted.Count; i++)
        {
            if (sorted[i].PlacementPosition != i)
            {
                inOrder = false;
                break;
            }
        }

        if (inOrder)
        {
            return false;
        }

        for (var i = 0; i < sorted.Count; i++)
        {
            sorted[i].PlacementPosition = i;
            Session.Network.EnqueueSend(new GameEventItemServerSaysContainId(Session, sorted[i], container));
        }

        return true;
    }

    /// <summary>
    /// Saves what changed in the bank containers a bulk operation touched. Moves into the bank itself are
    /// saved as they happen; this catches stack sizes and slots changed inside the bank's side packs.
    /// </summary>
    private void SaveBankContainers(Storage bank, HashSet<Container> touched)
    {
        foreach (var container in touched)
        {
            if (container == bank || container.Container == bank)
            {
                DeepSave(container);
            }
        }

        RefreshBankViews(bank, touched);
    }

    /// <summary>
    /// Re-sends the contents lists of the bank and the packs a bulk operation touched, as opening the bank does.
    /// The client moves an item it dragged itself, but when the server moves one, the bank window adds it to
    /// its new container and keeps showing it in the old one until the bank is reopened. Re-sending a list
    /// replaces it (picking up a pack from the bank already re-sends that pack's list), which clears the copies.
    /// </summary>
    private void RefreshBankViews(Storage bank, HashSet<Container> touched)
    {
        if (touched.Count == 0)
        {
            return;
        }

        // The bank's own list holds its packs, and items move in and out of it, so it is always re-sent.
        Session.Network.EnqueueSend(new GameEventViewContents(Session, bank));

        foreach (var pack in touched)
        {
            if (pack != bank && pack.Container == bank)
            {
                Session.Network.EnqueueSend(new GameEventViewContents(Session, pack));
            }
        }
    }
}
