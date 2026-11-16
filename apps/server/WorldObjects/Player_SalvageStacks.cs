using System;
using System.Collections.Generic;
using System.Linq;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.WorldObjects;

partial class Player
{
    /// <summary>
    /// Adds units of salvage to the inventory: tops up the stacks of that material and tier already there (those in
    /// preferred first, then the fullest), then makes new stacks, in preferred while it has room and otherwise wherever
    /// the inventory does. Adds the stacks it changes or makes to touched. Returns the units it had no room for.
    /// </summary>
    public int AddSalvageUnits(MaterialType material, int tier, int units, Container preferred = null, List<WorldObject> touched = null)
    {
        var wcid = SalvageStacks.GetWcid(material, tier);

        if (wcid == 0 || units <= 0)
        {
            return Math.Max(units, 0);
        }

        var existing = GetInventoryItemsOfWCID(wcid)
            .Where(s => (s.StackSize ?? 1) < (s.MaxStackSize ?? 1))
            .OrderByDescending(s => preferred != null && s.ContainerId == preferred.Guid.Full)
            .ThenByDescending(s => s.StackSize ?? 1)
            .ToList();

        foreach (var stack in existing)
        {
            if (units <= 0)
            {
                break;
            }

            if (FindObject(stack.Guid.Full, SearchLocations.MyInventory, out var container, out var rootOwner, out _) == null)
            {
                continue;
            }

            var amount = Math.Min(units, (stack.MaxStackSize ?? 1) - (stack.StackSize ?? 1));

            if (!AdjustStack(stack, amount, container, rootOwner))
            {
                continue;
            }

            Session.Network.EnqueueSend(new GameMessageSetStackSize(stack));
            touched?.Add(stack);
            units -= amount;
        }

        while (units > 0)
        {
            var stack = SalvageStacks.Create(material, tier, units);

            if (stack == null)
            {
                break;
            }

            var amount = stack.StackSize ?? 1;

            if (!TryCreateSalvageStack(stack, preferred))
            {
                stack.Destroy();
                break;
            }

            touched?.Add(stack);
            units -= amount;
        }

        Session.Network.EnqueueSend(new GameMessagePrivateUpdatePropertyInt(this, PropertyInt.EncumbranceVal, EncumbranceVal ?? 0));

        return units;
    }

    /// <summary>
    /// Adds amount units to a stack in the inventory (negative to take some away, though never all of it: consume the
    /// stack for that), telling the client. Returns false if the stack isn't carried or can't take the amount.
    /// </summary>
    public bool TryAdjustSalvageStack(WorldObject stack, int amount)
    {
        if (FindObject(stack.Guid.Full, SearchLocations.MyInventory, out var container, out var rootOwner, out _) == null)
        {
            return false;
        }

        if (!AdjustStack(stack, amount, container, rootOwner))
        {
            return false;
        }

        Session.Network.EnqueueSend(
            new GameMessageSetStackSize(stack),
            new GameMessagePrivateUpdatePropertyInt(this, PropertyInt.EncumbranceVal, EncumbranceVal ?? 0)
        );

        return true;
    }

    /// <summary>
    /// Combines the given stacks that share a WCID (material and tier), topping up the fullest from the emptiest, as
    /// dragging one onto another does. Returns how many stacks were emptied and removed.
    /// </summary>
    public int CombineSalvageStacks(IEnumerable<WorldObject> stacks)
    {
        var emptied = 0;

        var groups = stacks
            .Where(SalvageStacks.IsStack)
            .GroupBy(s => s.WeenieClassId)
            .Where(g => g.Count() > 1)
            .ToList();

        foreach (var group in groups)
        {
            var ordered = group.OrderByDescending(s => s.StackSize ?? 1).ToList();
            var targetIndex = 0;

            for (var sourceIndex = ordered.Count - 1; sourceIndex > targetIndex; sourceIndex--)
            {
                var source = ordered[sourceIndex];

                while (sourceIndex > targetIndex && (source.StackSize ?? 0) > 0)
                {
                    var target = ordered[targetIndex];
                    var room = (target.MaxStackSize ?? 1) - (target.StackSize ?? 1);

                    if (room <= 0)
                    {
                        targetIndex++;
                        continue;
                    }

                    var units = source.StackSize ?? 1;
                    var moved = Math.Min(room, units);

                    if (!TryAdjustSalvageStack(target, moved))
                    {
                        targetIndex++;
                        continue;
                    }

                    TryConsumeFromInventoryWithNetworking(source, moved);

                    if (moved == units)
                    {
                        emptied++;
                        break;
                    }
                }
            }
        }

        return emptied;
    }

    /// <summary>
    /// How many new stacks adding the units would need, after topping up the stacks already in the inventory
    /// </summary>
    public int CountNewSalvageStacks(MaterialType material, int tier, int units)
    {
        var wcid = SalvageStacks.GetWcid(material, tier);

        if (wcid == 0 || units <= 0)
        {
            return 0;
        }

        var room = GetInventoryItemsOfWCID(wcid).Sum(s => Math.Max(0, (s.MaxStackSize ?? 1) - (s.StackSize ?? 1)));
        var over = units - room;

        return over <= 0 ? 0 : (over + SalvageStacks.MaxStackSize - 1) / SalvageStacks.MaxStackSize;
    }

    /// <summary>
    /// Puts a new stack in preferred if it has a free slot, otherwise wherever the inventory has room
    /// </summary>
    private bool TryCreateSalvageStack(WorldObject stack, Container preferred)
    {
        if (
            preferred != null
            && preferred.GetFreeInventorySlots(false) > 0
            && preferred.TryAddToInventory(stack, out _, placementPosition: preferred.Inventory.Count, limitToMainPackOnly: true)
        )
        {
            // the CreateObject carries the container, so no ContainId is needed (one sent this tick would beat the
            // CreateObject to the client)
            Session.Network.EnqueueSend(new GameMessageCreateObject(stack));
            stack.SaveBiotaToDatabase();
            return true;
        }

        return TryCreateInInventoryWithNetworking(stack);
    }

    /// <summary>
    /// Converts the bags of salvage from before stacking in the player's packs to stacks. Runs at login, before the
    /// client is sent the inventory, so it sends nothing.
    /// </summary>
    private void ConvertLegacySalvage()
    {
        var converted = ConvertLegacySalvage(this, null);

        if (converted > 0)
        {
            _log.Information("[SALVAGE] Converted {Count} salvage bags to stacks for {Player}", converted, Name);
        }
    }

    /// <summary>
    /// Converts this account's bags of salvage from before stacking in the bank to stacks. Runs when the bank's
    /// contents have loaded, before they're sent to the client, so it sends nothing.
    /// </summary>
    public void ConvertLegacySalvageInBank(Storage bank)
    {
        var converted = ConvertLegacySalvage(bank, Account.AccountId);

        foreach (var pack in GetBankPacks(bank))
        {
            converted += ConvertLegacySalvage(pack, null);
        }

        if (converted > 0)
        {
            _log.Information("[SALVAGE] Converted {Count} banked salvage bags to stacks for {Player}", converted, Name);
        }
    }

    /// <summary>
    /// Replaces the legacy bags in the container (and the side packs in it, unless it's a bank) with stacks in the
    /// same place: the bags of each material and tier become as few stacks as hold their units, so they never need
    /// more slots than the bags took. In a bank, only the bags stamped with bankAccountId are converted, and the stacks
    /// are stamped the same. Returns how many bags were converted.
    /// </summary>
    private static int ConvertLegacySalvage(Container container, uint? bankAccountId)
    {
        var converted = 0;

        var groups = container.Inventory.Values
            .Where(SalvageStacks.IsLegacyBag)
            .Where(b => bankAccountId == null || b.BankAccountId == bankAccountId)
            .Where(b => b.MaterialType != null && SalvageStacks.HasSalvage(b.MaterialType.Value))
            .GroupBy(b => (Material: b.MaterialType.Value, Tier: SalvageStacks.GetTier(b)))
            .ToList();

        foreach (var group in groups)
        {
            var bags = group.ToList();
            var units = bags.Sum(b => Math.Max(0, (int)(b.Structure ?? 0)));
            var position = bags.Min(b => b.PlacementPosition ?? 0);

            // make the stacks first, so a bag is only removed once its replacement exists
            var stacks = new List<WorldObject>();

            while (units > 0)
            {
                var stack = SalvageStacks.Create(group.Key.Material, group.Key.Tier, units);

                if (stack == null)
                {
                    break;
                }

                units -= stack.StackSize ?? 1;
                stacks.Add(stack);
            }

            if (units > 0)
            {
                foreach (var stack in stacks)
                {
                    stack.Destroy();
                }

                continue;
            }

            foreach (var bag in bags)
            {
                if (container.TryRemoveFromInventory(bag.Guid, out var removed))
                {
                    removed.Destroy();
                    converted++;
                }
            }

            foreach (var stack in stacks)
            {
                if (bankAccountId != null)
                {
                    stack.BankAccountId = bankAccountId;
                }

                container.TryAddToInventory(stack, out _, placementPosition: position, limitToMainPackOnly: true, burdenCheck: false);
                stack.SaveBiotaToDatabase();
            }
        }

        if (bankAccountId == null)
        {
            foreach (var pack in container.Inventory.Values.OfType<Container>().ToList())
            {
                converted += ConvertLegacySalvage(pack, null);
            }
        }

        return converted;
    }
}
