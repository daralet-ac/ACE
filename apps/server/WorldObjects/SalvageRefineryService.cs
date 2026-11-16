using System;
using System.Collections.Generic;
using System.Linq;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Commands.PlayerCommands;
using ACE.Server.Entity;
using ACE.Server.Entity.Actions;
using ACE.Server.Managers;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.WorldObjects;

/// <summary>
/// Refining salvage at a Salvage Refinery. A stack handed to the refinery is refined one workmanship higher: every 2
/// units become 1 unit of the next workmanship (the same 2:1 ratio /salvage refine used, without needing salvage of the
/// next workmanship to pour into). An odd unit stays behind in the stack at its own workmanship. The refined units join
/// the player's stacks of the next workmanship, as any salvage added to their packs does.
///
/// Handing over a pack (a salvage crate, usually) refines every stack in it at or below the player's refine max
/// (/salvage refinemax) in one go, and then combines the pack's stacks of the same material and workmanship.
///
/// The player claps their hands, as other crafting does, and the refine happens when the motion ends.
///
/// Invoked from Player_Inventory.GiveObjectToNPC, which has already told the client the give failed, so whatever was
/// handed over stays where it was in the player's inventory.
/// </summary>
public static class SalvageRefineryService
{
    public const int MaxWorkmanship = SalvageStacks.MaxTier;

    /// <summary>
    /// The highest workmanship refine max can be set to: salvage at 10 can't be refined
    /// </summary>
    public const int MaxRefineMax = MaxWorkmanship - 1;

    public const string ConfirmCommand = "/salvage refineconfirm";
    public const string RefineMaxCommand = "/salvage refinemax";

    /// <summary>
    /// How far from the refinery the player may have wandered by the time they answer the confirmation
    /// </summary>
    private const float MaxConfirmDistance = 10f;

    public static readonly string Instructions =
        "Salvage Refinery\n\n"
        + "Hand a stack of salvage to the Refinery to refine it to the next workmanship. Every 2 units become 1 unit one "
        + $"workmanship higher, up to workmanship {MaxWorkmanship}, and join your salvage of that workmanship. If the stack "
        + "has an odd number of units, the unit left over stays in the stack at its original workmanship.\n\n"
        + "(Optional) Hand it your salvage crate (or any pack) to refine every stack inside at once. Afterwards, stacks of "
        + "the same material and workmanship in the pack are combined. "
        + $"Stacks above your refine max are left alone: type '{RefineMaxCommand} <1-{MaxRefineMax}>' to set it (it starts at {MaxRefineMax}).\n\n"
        + $"You'll be asked to confirm each refine. Type '{ConfirmCommand} off' to skip the confirmation, or "
        + $"'{ConfirmCommand} on' to bring it back.";

    // --- player settings ---

    public static bool IsConfirmationSkipped(Player player)
    {
        return player.GetProperty(PropertyBool.SkipSalvageRefineConfirmation) ?? false;
    }

    public static void SetConfirmationSkipped(Player player, bool skip)
    {
        if (skip)
        {
            player.SetProperty(PropertyBool.SkipSalvageRefineConfirmation, true);
        }
        else
        {
            player.RemoveProperty(PropertyBool.SkipSalvageRefineConfirmation);
        }
    }

    /// <summary>
    /// The highest workmanship a pack refine touches: stacks above it are left alone
    /// </summary>
    public static int GetRefineMax(Player player)
    {
        return Math.Clamp(player.GetProperty(PropertyInt.SalvageRefineMax) ?? MaxRefineMax, 1, MaxRefineMax);
    }

    public static void SetRefineMax(Player player, int max)
    {
        if (max >= MaxRefineMax)
        {
            player.RemoveProperty(PropertyInt.SalvageRefineMax);
        }
        else
        {
            player.SetProperty(PropertyInt.SalvageRefineMax, Math.Max(1, max));
        }
    }

    // --- handing something over ---

    public static void HandleGive(Player player, SalvageRefinery refinery, WorldObject item)
    {
        if (item is Container pack)
        {
            HandleGivePack(player, refinery, pack);
        }
        else
        {
            HandleGiveStack(player, refinery, item);
        }
    }

    /// <summary>
    /// Checks that the player can use the refinery with the item right now, telling them why not if they can't
    /// </summary>
    private static bool CanUse(Player player, WorldObject item)
    {
        if (player.IsBusy || player.Teleporting)
        {
            player.SendWeenieError(WeenieError.YoureTooBusy);
            return false;
        }

        if (player.IsTrading && item.IsBeingTradedOrContainsItemBeingTraded(player.ItemsInTradeWindow))
        {
            player.SendWeenieError(WeenieError.TradeItemBeingTraded);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Checks the player still has the item and is still near the refinery when they answer the confirmation
    /// </summary>
    private static bool IsStillValid(Player player, SalvageRefinery refinery, WorldObject item)
    {
        if (!StillHas(player, item))
        {
            return false;
        }

        if (refinery.CurrentLandblock == null || player.GetDistance(refinery) > MaxConfirmDistance)
        {
            SendCraftMessage(player, "You've moved too far from the Refinery.");
            return false;
        }

        return CanUse(player, item);
    }

    private static bool StillHas(Player player, WorldObject item)
    {
        if (player.FindObject(item.Guid.Full, Player.SearchLocations.MyInventory) != item)
        {
            SendCraftMessage(player, $"You no longer have the {item.Name}.");
            return false;
        }

        return true;
    }

    private static void Confirm(Player player, string message, Action action)
    {
        if (IsConfirmationSkipped(player))
        {
            action();
            return;
        }

        message += $"\n\nTo stop seeing this confirmation, type '{ConfirmCommand} off'";

        if (!player.ConfirmationManager.EnqueueSend(new Confirmation_Custom(player.Guid, action), message))
        {
            player.SendWeenieError(WeenieError.ConfirmationInProgress);
        }
    }

    /// <summary>
    /// Plays the crafting motion (clapping hands, as other crafting does), running craft as it ends. The player is busy
    /// throughout, so nothing else can be handed over in the meantime.
    /// </summary>
    private static void Animate(Player player, Action craft)
    {
        var chain = new ActionChain();

        player.IsBusy = true;

        var animTime = 0.0f;

        if (player.CombatMode != CombatMode.NonCombat)
        {
            var stanceTime = player.SetCombatMode(CombatMode.NonCombat);
            chain.AddDelaySeconds(stanceTime);
            animTime += stanceTime;
        }

        animTime += player.EnqueueMotion(chain, MotionCommand.ClapHands);

        chain.AddAction(player, craft);

        player.EnqueueMotion(chain, MotionCommand.Ready);

        chain.AddAction(player, () => player.IsBusy = false);

        chain.EnqueueChain();

        player.NextUseTime = DateTime.UtcNow.AddSeconds(animTime);
    }

    // --- a single stack ---

    private static void HandleGiveStack(Player player, SalvageRefinery refinery, WorldObject stack)
    {
        if (!CanRefineStack(player, stack, out var tier, out var units))
        {
            return;
        }

        var material = stack.MaterialType.Value;
        var unitsGained = units / 2;
        var prior = CountUnits(player, material, tier + 1);

        var message =
            $"Refine your {GetMaterialName(stack)} salvage (workmanship {tier}, {units} units) into {unitsGained} "
            + $"{Units(unitsGained)} of workmanship {tier + 1}?";

        if (units % 2 > 0)
        {
            message += $"\n\nThe 1 unit left over stays at workmanship {tier}.";
        }

        if (prior > 0)
        {
            message += $"\n\nIt joins your {prior:N0} {Units(prior)} of workmanship {tier + 1} {GetMaterialName(stack)}.";
        }

        // snapshot the stack so a changed stack (split, merged, tinkered with) isn't refined on a stale answer
        var snapshot = StackSnapshot.Of(stack);

        Confirm(
            player,
            message,
            () =>
            {
                if (IsStillValid(player, refinery, stack) && CheckSnapshot(player, stack, snapshot))
                {
                    Animate(player, () => RefineStack(player, stack, snapshot));
                }
            }
        );
    }

    /// <summary>
    /// Checks that the stack can be refined, telling the player why not if it can't
    /// </summary>
    private static bool CanRefineStack(Player player, WorldObject stack, out int tier, out int units)
    {
        tier = 0;
        units = 0;

        if (SalvageStacks.IsLegacyBag(stack))
        {
            SendCraftMessage(player, "That's an old bag of salvage. It becomes a stack of salvage the next time you log in.");
            return false;
        }

        if (!SalvageStacks.IsStack(stack) || stack.MaterialType == null)
        {
            SendCraftMessage(player, "The Refinery only accepts salvage, or a pack of it.");
            return false;
        }

        if (!CanUse(player, stack))
        {
            return false;
        }

        tier = SalvageStacks.GetTier(stack);
        units = stack.StackSize ?? 1;

        if (tier >= MaxWorkmanship)
        {
            SendCraftMessage(player, $"That salvage is already at workmanship {MaxWorkmanship} and can't be refined further.");
            return false;
        }

        if (units < 2)
        {
            SendCraftMessage(player, "The Refinery needs at least 2 units of salvage to refine.");
            return false;
        }

        if (!HasRoom(player, new[] { (stack, units) }))
        {
            return false;
        }

        return true;
    }

    private static bool CheckSnapshot(Player player, WorldObject stack, StackSnapshot snapshot)
    {
        if (snapshot.Matches(stack))
        {
            return true;
        }

        SendCraftMessage(player, "That salvage has changed since you handed it over. Hand it to the Refinery again.");
        return false;
    }

    private static void RefineStack(Player player, WorldObject stack, StackSnapshot snapshot)
    {
        // the player was busy throughout the motion, but the stack may still have gone (dropped or destroyed)
        if (!StillHas(player, stack) || !CheckSnapshot(player, stack, snapshot))
        {
            return;
        }

        var material = stack.MaterialType.Value;
        var tier = snapshot.Tier;
        var units = snapshot.Units;
        var refined = units - units % 2;
        var unitsGained = units / 2;
        var preferred = stack.Container as Container;
        var prior = CountUnits(player, material, tier + 1);

        if (!Refine(player, stack, preferred, material, tier, refined))
        {
            return;
        }

        var result =
            $"The Refinery refines your {refined} units of workmanship {tier} {GetMaterialName(material)} into {unitsGained} "
            + $"{Units(unitsGained)} of workmanship {tier + 1}.";

        if (prior > 0)
        {
            result += $" They were combined with your existing workmanship {tier + 1} salvage, now {CountUnits(player, material, tier + 1):N0} units.";
        }

        if (units % 2 > 0)
        {
            result += $" The unit left over stays at workmanship {tier}.";
        }

        SendCraftMessage(player, result);

        SalvageCommand.ResortSalvageCrate(player);
    }

    /// <summary>
    /// Takes refined units (an even number) from the stack and adds half as many of the next tier to the inventory,
    /// in preferred first. Gives the units back if, against the room check, they don't all fit.
    /// </summary>
    private static bool Refine(Player player, WorldObject stack, Container preferred, MaterialType material, int tier, int refined)
    {
        var unitsGained = refined / 2;

        // the refined units go first, so a stack used up frees its slot for the result
        if (!player.TryConsumeFromInventoryWithNetworking(stack, refined))
        {
            SendCraftMessage(player, "The Refinery couldn't refine that salvage.");
            return false;
        }

        var left = player.AddSalvageUnits(material, tier + 1, unitsGained, preferred);

        if (left > 0)
        {
            // give back what didn't fit, at the original workmanship
            player.AddSalvageUnits(material, tier, left * 2, preferred);

            SendCraftMessage(player, "You ran out of room for the refined salvage, so some was left unrefined. Make room in your packs and try again.");
        }

        return true;
    }

    // --- a pack of stacks ---

    /// <summary>
    /// What refining a pack would do: the stacks it refines, and how many it leaves alone and why
    /// </summary>
    private class PackPlan
    {
        public readonly List<StackSnapshot> Stacks = new();
        public int AboveMax;
        public int TooSmall;
        public int Legacy;

        public int UnitsIn => Stacks.Sum(b => b.Units - b.Units % 2);
        public int UnitsOut => Stacks.Sum(b => b.Units / 2);
        public int Spares => Stacks.Sum(b => b.Units % 2);

        public bool Matches(PackPlan other)
        {
            return Stacks.Count == other.Stacks.Count && Stacks.Zip(other.Stacks).All(p => p.First == p.Second);
        }
    }

    private static PackPlan PlanPack(Player player, Container pack)
    {
        var refineMax = GetRefineMax(player);
        var plan = new PackPlan();

        // the highest workmanship first, so the units one stack gains aren't refined again by the stack above it
        foreach (var stack in pack.Inventory.Values.OrderByDescending(s => s.Workmanship ?? 0).ThenBy(s => s.Guid.Full))
        {
            if (SalvageStacks.IsLegacyBag(stack))
            {
                plan.Legacy++;
            }
            else if (!SalvageStacks.IsStack(stack) || stack.MaterialType == null)
            {
                continue;
            }
            else if (SalvageStacks.GetTier(stack) > refineMax)
            {
                plan.AboveMax++;
            }
            else if ((stack.StackSize ?? 1) < 2)
            {
                plan.TooSmall++;
            }
            else
            {
                plan.Stacks.Add(StackSnapshot.Of(stack));
            }
        }

        return plan;
    }

    private static void HandleGivePack(Player player, SalvageRefinery refinery, Container pack)
    {
        if (!CanUse(player, pack))
        {
            return;
        }

        var plan = PlanPack(player, pack);
        var refineMax = GetRefineMax(player);

        if (plan.Stacks.Count == 0)
        {
            string reason;

            if (plan.Legacy > 0 && plan.AboveMax + plan.TooSmall == 0)
            {
                reason = $"The salvage in your {pack.Name} is in old bags. They become stacks of salvage the next time you log in.";
            }
            else if (plan.AboveMax + plan.TooSmall == 0)
            {
                reason = $"There's no salvage in your {pack.Name}.";
            }
            else
            {
                reason = $"None of the salvage in your {pack.Name} can be refined: stacks need at least 2 units and to be at or below your refine max of workmanship {refineMax} ('{RefineMaxCommand} <1-{MaxRefineMax}>').";
            }

            SendCraftMessage(player, reason);
            return;
        }

        if (!HasRoom(player, plan.Stacks.Select(s => (s.Stack, s.Units))))
        {
            return;
        }

        var message =
            $"Refine {plan.Stacks.Count} {Stacks(plan.Stacks.Count)} of salvage in your {pack.Name}?\n\n"
            + $"{plan.UnitsIn:N0} units become {plan.UnitsOut:N0} {Units(plan.UnitsOut)}, one workmanship higher.";

        if (plan.Spares > 0)
        {
            message += $" {plan.Spares} odd {Units(plan.Spares)} {(plan.Spares == 1 ? "stays" : "stay")} at {(plan.Spares == 1 ? "its" : "their")} own workmanship.";
        }

        message += "\n\nAfterwards, stacks of the same material and workmanship in the pack are combined.";

        // always shown, so a player who wants a different cap knows to say No and set it first
        message += $"\n\nOnly stacks up to workmanship {refineMax} are refined";

        if (plan.AboveMax > 0)
        {
            message += $" ({plan.AboveMax} {Stacks(plan.AboveMax)} above it {(plan.AboveMax == 1 ? "is" : "are")} skipped)";
        }

        message += $". To change that, choose No and type '{RefineMaxCommand} <1-{MaxRefineMax}>' first.";

        Confirm(
            player,
            message,
            () =>
            {
                if (IsStillValid(player, refinery, pack) && CheckPlan(player, pack, plan))
                {
                    Animate(player, () => RefinePack(player, pack, plan));
                }
            }
        );
    }

    private static bool CheckPlan(Player player, Container pack, PackPlan expected)
    {
        if (PlanPack(player, pack).Matches(expected))
        {
            return true;
        }

        SendCraftMessage(player, $"The salvage in your {pack.Name} has changed since you handed it over. Hand it to the Refinery again.");
        return false;
    }

    private static void RefinePack(Player player, Container pack, PackPlan plan)
    {
        if (!StillHas(player, pack) || !CheckPlan(player, pack, plan))
        {
            return;
        }

        var refinedStacks = 0;

        foreach (var entry in plan.Stacks)
        {
            var refined = entry.Units - entry.Units % 2;

            if (Refine(player, entry.Stack, pack, entry.Stack.MaterialType.Value, entry.Tier, refined))
            {
                refinedStacks++;
            }
        }

        var combined = player.CombineSalvageStacks(pack.Inventory.Values.ToList());

        var result =
            $"The Refinery refines {refinedStacks} {Stacks(refinedStacks)} of salvage: {plan.UnitsIn:N0} units became "
            + $"{plan.UnitsOut:N0} {Units(plan.UnitsOut)}, one workmanship higher.";

        if (plan.Spares > 0)
        {
            result += $" {plan.Spares} odd {Units(plan.Spares)} stayed at {(plan.Spares == 1 ? "its" : "their")} own workmanship.";
        }

        if (combined > 0)
        {
            result += $" {combined} {Stacks(combined)} {(combined == 1 ? "was" : "were")} combined into others.";
        }

        SendCraftMessage(player, result);

        SalvageCommand.ResortSalvageCrate(player);
    }

    // --- helpers ---

    /// <summary>
    /// The state of a stack when the player was asked, so a changed stack isn't refined on a stale answer
    /// </summary>
    private readonly record struct StackSnapshot(WorldObject Stack, uint Wcid, int Units, int Tier)
    {
        public static StackSnapshot Of(WorldObject stack)
        {
            return new StackSnapshot(stack, stack.WeenieClassId, stack.StackSize ?? 1, SalvageStacks.GetTier(stack));
        }

        public bool Matches(WorldObject stack)
        {
            return this == Of(stack);
        }
    }

    /// <summary>
    /// Checks the inventory has room for what refining the stacks makes, telling the player if it hasn't. Counts the
    /// worst case: a new stack for every 1,000 units of each material and tier made, less the slots of the stacks that
    /// are used up.
    /// </summary>
    private static bool HasRoom(Player player, IEnumerable<(WorldObject Stack, int Units)> stacks)
    {
        var list = stacks.ToList();

        var needed = list
            .GroupBy(s => (s.Stack.MaterialType.Value, Tier: SalvageStacks.GetTier(s.Stack) + 1))
            .Sum(g => (g.Sum(s => s.Units / 2) + SalvageStacks.MaxStackSize - 1) / SalvageStacks.MaxStackSize);

        var freed = list.Count(s => s.Units % 2 == 0);
        var free = player.GetFreeInventorySlots();

        if (needed <= free + freed)
        {
            return true;
        }

        var missing = needed - free - freed;

        SendCraftMessage(player, $"You don't have room for the refined salvage. Make room for {missing} more {Stacks(missing)} and try again.");
        return false;
    }

    /// <summary>
    /// The units of the material at the tier the player carries
    /// </summary>
    private static int CountUnits(Player player, MaterialType material, int tier)
    {
        return player.GetInventoryItemsOfWCID(SalvageStacks.GetWcid(material, tier)).Sum(s => s.StackSize ?? 1);
    }

    private static string GetMaterialName(WorldObject stack)
    {
        return GetMaterialName(stack.MaterialType ?? MaterialType.Unknown);
    }

    private static string GetMaterialName(MaterialType material)
    {
        return RecipeManager.GetMaterialName(material);
    }

    private static string Units(int count)
    {
        return count == 1 ? "unit" : "units";
    }

    private static string Stacks(int count)
    {
        return count == 1 ? "stack" : "stacks";
    }

    private static void SendCraftMessage(Player player, string message)
    {
        player.Session.Network.EnqueueSend(new GameMessageSystemChat(message, ChatMessageType.Craft));
    }
}
