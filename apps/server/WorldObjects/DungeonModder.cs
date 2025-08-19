﻿using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Entity.Models;
using ACE.Server.Entity;
using ACE.Server.Network.GameEvent.Events;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.WorldObjects;

public class DungeonModder : Stackable
{
    /// <summary>
    /// A new biota be created taking all of its values from weenie.
    /// </summary>
    public DungeonModder(Weenie weenie, ObjectGuid guid)
        : base(weenie, guid)
    {
        SetEphemeralValues();
    }

    /// <summary>
    /// Restore a WorldObject from the database.
    /// </summary>
    public DungeonModder(Biota biota)
        : base(biota)
    {
        SetEphemeralValues();
    }

    private void SetEphemeralValues() { }

    /// <summary>
    /// This is raised by Player.HandleActionUseItem.<para />
    /// The item should be in the players possession.
    /// </summary>
    public override void ActOnUse(WorldObject activator)
    {
        ActOnUse(activator, false);
    }

    public void ActOnUse(WorldObject activator, bool confirmed)
    {
        if (!(activator is Player player))
        {
            return;
        }

        if (SpellDID == null)
        {
            if (player.IsBusy || player.Teleporting || player.suicideInProgress)
            {
                player.SendWeenieError(WeenieError.YoureTooBusy);
                return;
            }
        }
        if (player.IsJumping)
        {
            player.SendWeenieError(WeenieError.YouCantDoThatWhileInTheAir);
            return;
        }

        if (!player.IsShrouded())
        {
            player.SendTransientError($"You must be shrouded to use the {Name}.");
            return;
        }

        var fellowship = player.Fellowship;
        if (fellowship is null || fellowship.FellowshipLeaderGuid != player.Guid.Full)
        {
            player.SendTransientError($"You must be a fellowship leader to use {Name}.");
            return;
        }

        if (!confirmed)
        {
            var msg = $"Are you sure you want to use {Name}?\n\n" +
                      $"Using this may replace another active dungeon mod effect.\n\n" +
                      $"Once active, disbanding your fellow or passing leader to another player will disable the effect.";
            var confirm = new Confirmation_Custom(player.Guid, () => ActOnUse(activator, true));
            if (!player.ConfirmationManager.EnqueueSend(confirm, msg))
            {
                player.SendWeenieError(WeenieError.ConfirmationInProgress);
            }

            return;
        }

        if (UseUserAnimation != MotionCommand.Invalid)
        {
            var animMod =
                (UseUserAnimation == MotionCommand.MimeDrink || UseUserAnimation == MotionCommand.MimeEat)
                    ? 0.5f
                    : 1.0f;

            player.ApplyConsumable(UseUserAnimation, () => UseGem(player), animMod);
        }
        else
        {
            UseGem(player);
        }
    }

    public void UseGem(Player player)
    {
        if (player.IsDead)
        {
            return;
        }

        // verify item is still valid
        if (player.FindObject(Guid.Full, Player.SearchLocations.MyInventory) == null)
        {
            //player.SendWeenieError(WeenieError.ObjectGone);   // results in 'Unable to move object!' transient error
            player.SendTransientError($"Cannot find the {Name}"); // custom message
            return;
        }

        if (SpellDID.HasValue)
        {
            var spell = new Spell((uint)SpellDID);

            // should be 'You cast', instead of 'Item cast'
            // omitting the item caster here, so player is also used for enchantment registry caster,
            // which could prevent some scenarios with spamming enchantments from multiple gem sources to protect against dispels

            // TODO: figure this out better
            if (spell.MetaSpellType == SpellType.PortalSummon)
            {
                TryCastSpell(spell, player, this, tryResist: false);
            }
            else if (spell.IsImpenBaneType || spell.IsItemRedirectableType)
            {
                player.TryCastItemEnchantment_WithRedirects(spell, player, this);
            }
            else
            {
                player.TryCastSpell(spell, player, this, tryResist: false);
            }
        }

        if (UseSound > 0)
        {
            player.Session.Network.EnqueueSend(new GameMessageSound(player.Guid, UseSound));
        }

        if ((GetProperty(PropertyBool.UnlimitedUse) ?? false) == false)
        {
            player.TryConsumeFromInventoryWithNetworking(this, 1);
        }
    }

    public override void OnActivate(WorldObject activator)
    {
        if (ItemUseable == Usable.Contained && activator is Player player)
        {
            var containedItem = player.FindObject(
                Guid.Full,
                Player.SearchLocations.MyInventory | Player.SearchLocations.MyEquippedItems
            );
            if (containedItem != null) // item is contained by player
            {
                if (CombatAbilityId == null)
                {
                    if (player.IsBusy || player.Teleporting || player.suicideInProgress)
                    {
                        player.Session.Network.EnqueueSend(
                            new GameEventWeenieError(player.Session, WeenieError.YoureTooBusy)
                        );
                        player.EnchantmentManager.StartCooldown(this);
                        return;
                    }
                }

                if (player.IsDead)
                {
                    player.Session.Network.EnqueueSend(new GameEventWeenieError(player.Session, WeenieError.Dead));
                    player.EnchantmentManager.StartCooldown(this);
                    return;
                }
            }
            else
            {
                return;
            }
        }

        base.OnActivate(activator);
    }
}
