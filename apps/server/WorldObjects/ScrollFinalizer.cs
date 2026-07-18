using System;
using ACE.Entity.Enum;
using ACE.Server.Entity;
using ACE.Server.Entity.Actions;
using ACE.Server.Factories;
using ACE.Server.Managers;
using ACE.Server.Network.GameMessages.Messages;
using MotionCommand = ACE.Entity.Enum.MotionCommand;

namespace ACE.Server.WorldObjects;

/// <summary>
/// Finalize phase of Scroll Writing: applying Spell Dust to a scribed Blank Scroll.
/// The scribed component order must match a real spell's formula (checked via SpellFormulaMatcher),
/// and the player must have enough Spell Dust (spell level squared) and enough Spellcrafting skill.
/// A wrong formula destroys the Blank Scroll outright. Insufficient dust/skill on an otherwise-correct
/// formula is a no-cost rejection instead, since the player did nothing wrong there.
/// </summary>
public static class ScrollFinalizer
{
    public static void TryFinalizeScroll(Player player, WorldObject dustSource, WorldObject target, bool confirmed = false)
    {
        if (player.IsBusy)
        {
            player.SendUseDoneEvent(WeenieError.YoureTooBusy);
            return;
        }

        if (!ScribingTableService.IsNearActiveTable(player))
        {
            player.Session.Network.EnqueueSend(
                new GameMessageSystemChat("You must be near a summoned Scribing Table to do this.", ChatMessageType.Craft)
            );
            player.SendUseDoneEvent();
            return;
        }

        if (target is not BlankScroll blankScroll)
        {
            player.Session.Network.EnqueueSend(
                new GameMessageSystemChat("You can only use Scroll Dust on a Blank Scroll.", ChatMessageType.Craft)
            );
            player.SendUseDoneEvent();
            return;
        }

        if (!RecipeManager.VerifyUse(player, dustSource, blankScroll, true))
        {
            player.SendTransientError(
                "Either you or one of the items involved does not pass the requirements for this craft interaction."
            );
            player.SendUseDoneEvent();
            return;
        }

        var scribed = blankScroll.ScribedComponents;

        if (scribed.Count == 0)
        {
            player.Session.Network.EnqueueSend(
                new GameMessageSystemChat("That scroll has no components scribed on it yet.", ChatMessageType.Craft)
            );
            player.SendUseDoneEvent();
            return;
        }

        if (!SpellFormulaMatcher.TryMatch(scribed, out var spellId, out var spell))
        {
            player.TryConsumeFromInventoryWithNetworking(blankScroll, 1);

            player.Session.Network.EnqueueSend(
                new GameMessageSystemChat(
                    "The components you have scribed do not match a known spell formula. The scroll crumbles to dust.",
                    ChatMessageType.Craft
                )
            );
            player.SendUseDoneEvent();
            return;
        }

        if (!SpellFormulaMatcher.TryGetScrollWcid(spellId, out var scrollWcid))
        {
            player.Session.Network.EnqueueSend(
                new GameMessageSystemChat(
                    "That spell does not have a corresponding scroll available.",
                    ChatMessageType.Craft
                )
            );
            player.SendUseDoneEvent();
            return;
        }

        var level = spell.Formula.Level;
        var dustCost = (int)(level * level);

        var bypassChecks = PropertyManager.GetBool("bypass_crafting_checks").Item;

        // Required Spellcrafting skill = SpellLevel x 20 - 20 (e.g. level 1 = 0, level 5 = 80, level 8 = 140)
        var requiredSkill = Math.Max(0, (int)level * 20 - 20);

        var dustAvailable = player.GetNumInventoryItemsOfWCID(dustSource.WeenieClassId);

        if (!bypassChecks && dustAvailable < dustCost)
        {
            player.Session.Network.EnqueueSend(
                new GameMessageSystemChat(
                    $"You need {dustCost} Spell Dust to finish this scroll (you have {dustAvailable}).",
                    ChatMessageType.Craft
                )
            );
            player.SendUseDoneEvent();
            return;
        }

        var spellcraftSkill = player.GetCreatureSkill(Skill.Spellcrafting);

        if (!bypassChecks && spellcraftSkill.Current < requiredSkill)
        {
            player.Session.Network.EnqueueSend(
                new GameMessageSystemChat(
                    "Your Spellcrafting is not developed enough to complete this scroll.",
                    ChatMessageType.Craft
                )
            );
            player.SendUseDoneEvent();
            return;
        }

        var spellName = spell._spellBase?.Name ?? "Unknown Spell";

        if (!confirmed)
        {
            var confirmationMessage =
                $"Finalize this scroll into a Scroll of {spellName}?\n\nThis will consume the Blank Scroll and {dustCost} Spell Dust.\n\n";

            if (
                !player.ConfirmationManager.EnqueueSend(
                    new Confirmation_CraftInteration(player.Guid, dustSource.Guid, blankScroll.Guid),
                    confirmationMessage
                )
            )
            {
                player.SendUseDoneEvent(WeenieError.ConfirmationInProgress);
                return;
            }

            player.SendUseDoneEvent();
            return;
        }

        var actionChain = new ActionChain();

        var animTime = 0.0f;

        player.IsBusy = true;

        if (player.CombatMode != CombatMode.NonCombat)
        {
            var stanceTime = player.SetCombatMode(CombatMode.NonCombat);
            actionChain.AddDelaySeconds(stanceTime);

            animTime += stanceTime;
        }

        animTime += player.EnqueueMotion(actionChain, MotionCommand.ClapHands);

        actionChain.AddAction(
            player,
            () =>
            {
                if (!RecipeManager.VerifyUse(player, dustSource, blankScroll, true))
                {
                    player.SendTransientError(
                        "Either you or one of the items involved does not pass the requirements for this craft interaction."
                    );
                    return;
                }

                var newScroll = WorldObjectFactory.CreateNewWorldObject(scrollWcid);
                if (newScroll == null)
                {
                    player.SendTransientError("Failed to create the finished scroll.");
                    return;
                }

                player.TryConsumeFromInventoryWithNetworking(blankScroll, 1);
                player.TryConsumeFromInventoryWithNetworking(dustSource.WeenieClassId, dustCost);

                if (!player.TryCreateInInventoryWithNetworking(newScroll))
                {
                    player.SendTransientError("Failed to add the finished scroll to your inventory.");
                    newScroll.Destroy();
                    return;
                }

                player.Session.Network.EnqueueSend(
                    new GameMessageSystemChat($"You successfully write a Scroll of {spellName}.", ChatMessageType.Craft)
                );

                Player.TryAwardCraftingXp(player, spellcraftSkill, Skill.Spellcrafting, requiredSkill);

                SpellTomeService.RecordDiscoveredFormula(player, spell);
            }
        );

        actionChain.AddAction(
            player,
            () =>
            {
                player.IsBusy = false;
            }
        );

        player.EnqueueMotion(actionChain, MotionCommand.Ready);

        actionChain.EnqueueChain();

        player.NextUseTime = DateTime.UtcNow.AddSeconds(animTime);
    }
}
