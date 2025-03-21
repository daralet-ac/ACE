using System;
using ACE.Common;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Entity.Models;
using ACE.Server.Entity;
using ACE.Server.Factories;
using ACE.Server.Network.GameEvent.Events;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.Network.Structure;

namespace ACE.Server.WorldObjects;

public class Gem : Stackable
{
    /// <summary>
    /// A new biota be created taking all of its values from weenie.
    /// </summary>
    public Gem(Weenie weenie, ObjectGuid guid)
        : base(weenie, guid)
    {
        SetEphemeralValues();
    }

    /// <summary>
    /// Restore a WorldObject from the database.
    /// </summary>
    public Gem(Biota biota)
        : base(biota)
    {
        SetEphemeralValues();
    }

    private void SetEphemeralValues() { }

    /// <summary>
    /// This is raised by Player.HandleActionUseItem.<para />
    /// The item should be in the players possession.
    ///
    /// The OnUse method for this class is to use a contract to add a tracked quest to our quest panel.
    /// This gives the player access to information about the quest such as starting and ending NPC locations,
    /// and shows our progress for kill tasks as well as any timing information such as when we can repeat the
    /// quest or how much longer we have to complete it in the case of at timed quest.   Og II
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

        if (CombatAbilityId == null)
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

        if (!string.IsNullOrWhiteSpace(UseSendsSignal))
        {
            player.CurrentLandblock?.EmitSignal(player, UseSendsSignal);
            return;
        }

        // handle rare gems
        if (RareId != null && player.GetCharacterOption(CharacterOption.ConfirmUseOfRareGems) && !confirmed)
        {
            var msg = $"Are you sure you want to use {Name}?";
            var confirm = new Confirmation_Custom(player.Guid, () => ActOnUse(activator, true));
            if (!player.ConfirmationManager.EnqueueSend(confirm, msg))
            {
                player.SendWeenieError(WeenieError.ConfirmationInProgress);
            }

            return;
        }

        if (RareUsesTimer)
        {
            var currentTime = Time.GetUnixTime();

            var timeElapsed = currentTime - player.LastRareUsedTimestamp;

            if (timeElapsed < RareTimer)
            {
                // TODO: get retail message
                var remainTime = (int)Math.Ceiling(RareTimer - timeElapsed);
                player.Session.Network.EnqueueSend(
                    new GameMessageSystemChat(
                        $"You may use another timed rare in {remainTime}s",
                        ChatMessageType.Broadcast
                    )
                );
                return;
            }
        }

        if (UseUserAnimation != MotionCommand.Invalid)
        {
            // some gems have UseUserAnimation and UseSound, similar to food
            // eg. 7559 - Condensed Dispel Potion

            // the animation is also weird, and differs from food, in that it is the full animation
            // instead of stopping at the 'eat/drink' point... so we pass 0.5 here?

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

        // trying to use a dispel potion while pk timer is active
        // send error message and cancel - do not consume item
        if (SpellDID != null)
        {
            var spell = new Spell(SpellDID.Value);

            if (spell.MetaSpellType == SpellType.Dispel && !VerifyDispelPkStatus(this, player))
            {
                return;
            }
        }

        if (RareUsesTimer)
        {
            var currentTime = Time.GetUnixTime();

            player.LastRareUsedTimestamp = currentTime;

            // local broadcast usage
            player.EnqueueBroadcast(
                new GameMessageSystemChat($"{player.Name} used the rare item {Name}", ChatMessageType.Broadcast)
            );
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

        if (UseCreateContractId > 0)
        {
            if (!player.ContractManager.Add(UseCreateContractId.Value))
            {
                return;
            }

            // this wasn't in retail, but the lack of feedback when using a contract gem just seems jarring so...
            player.Session.Network.EnqueueSend(
                new GameMessageSystemChat(
                    $"{Name} accepted. Click on the quill icon in the lower right corner to open your contract tab to view your active contracts.",
                    ChatMessageType.Broadcast
                )
            );
        }

        if (CombatAbilityId > 0)
        {
            switch ((CombatAbility)CombatAbilityId)
            {
                case CombatAbility.PerceiveThreats:
                    if (player.TogglePerceiveThreatsSetting())
                    {
                        player.Session.Network.EnqueueSend(
                            new GameMessageSystemChat(
                                $"You are now attempting to perceive enemy threats.",
                                ChatMessageType.Broadcast
                            )
                        );
                    }
                    else
                    {
                        player.Session.Network.EnqueueSend(
                            new GameMessageSystemChat(
                                $"You stop attempting to perceive enemy threats.",
                                ChatMessageType.Broadcast
                            )
                        );
                    }

                    break;
                case CombatAbility.Stealth:
                    if (!player.IsStealthed)
                    {
                        player.BeginStealth();
                    }
                    else
                    {
                        player.EndStealth();
                    }

                    break;
                case CombatAbility.Deceive:
                    if (player.ToggleDeceiveSetting())
                    {
                        player.Session.Network.EnqueueSend(
                            new GameMessageSystemChat($"You start deceiving nearby enemies.", ChatMessageType.Broadcast)
                        );
                    }
                    else
                    {
                        player.Session.Network.EnqueueSend(
                            new GameMessageSystemChat($"You stop deceiving nearby enemies.", ChatMessageType.Broadcast)
                        );
                    }

                    break;
                case CombatAbility.SlashThrustToggle:
                    if (player.ToggleSlashThrustSetting())
                    {
                        player.Session.Network.EnqueueSend(
                            new GameMessageSystemChat($"You switch to thrusting attacks.", ChatMessageType.Broadcast)
                        );
                    }
                    else
                    {
                        player.Session.Network.EnqueueSend(
                            new GameMessageSystemChat($"You switch to slashing attacks.", ChatMessageType.Broadcast)
                        );
                    }

                    break;
                case CombatAbility.FocusedTaunt:
                    player.TryUseFocusedTaunt(this);
                    break;
                case CombatAbility.AreaTaunt:
                    player.TryUseAreaTaunt(this);
                    break;
                case CombatAbility.FeignInjury:
                    player.TryUseFeignInjury(this);
                    break;
                case CombatAbility.Vanish:
                    player.TryUseVanish(this);
                    break;
                case CombatAbility.ExposePhysicalWeakness:
                    player.TryUseExposePhysicalWeakness(this);
                    break;
                case CombatAbility.ExposeMagicalWeakness:
                    player.TryUseExposeMagicalWeakness(this);
                    break;
                case CombatAbility.ActivatedCombatAbilities:
                    player.TryUseActivated(this);
                    break;
                case CombatAbility.ManaBarrier:
                    if (player.EvasiveStanceToggle)
                    {
                        player.Session.Network.EnqueueSend(
                            new GameMessageSystemChat($"You cannot use Mana Barrier while Evasive Stance is active.", ChatMessageType.Broadcast)
                        );
                        break;
                    }

                    if (player.ToggleManaBarrierSetting())
                    {
                        player.Session.Network.EnqueueSend(
                            new GameMessageSystemChat(
                                $"You draw on your stored mana to form an enchanted shield around yourself!",
                                ChatMessageType.Broadcast
                            )
                        );
                        player.PlayParticleEffect(PlayScript.ShieldUpBlue, player.Guid);
                    }
                    else
                    {
                        player.Session.Network.EnqueueSend(
                            new GameMessageSystemChat($"You dispel your mana barrier.", ChatMessageType.Broadcast)
                        );
                        player.PlayParticleEffect(PlayScript.DispelLife, player.Guid);
                    }
                    break;
                case CombatAbility.EvasiveStance:
                    if (player.ManaBarrierToggle)
                    {
                        player.Session.Network.EnqueueSend(
                            new GameMessageSystemChat($"You cannot use Evasive Stance while Mana Barrier is active.", ChatMessageType.Broadcast)
                        );
                        break;
                    }

                    if (player.ToggleEvasiveStanceSetting())
                    {
                        player.Session.Network.EnqueueSend(
                            new GameMessageSystemChat(
                                $"You move into an evasive stance!",
                                ChatMessageType.Broadcast
                            )
                        );
                        player.PlayParticleEffect(PlayScript.ShieldUpYellow, player.Guid);
                    }
                    else
                    {
                        player.Session.Network.EnqueueSend(
                            new GameMessageSystemChat($"You move out of your evasive stance.", ChatMessageType.Broadcast)
                        );
                        player.PlayParticleEffect(PlayScript.DispelLife, player.Guid);
                    }
                    break;
                case CombatAbility.PowerScaler:
                    if (player.EnchantmentManager.HasSpell(5379))
                    {
                        if (player.IsBusy)
                        {
                            player.Session.Network.EnqueueSend(
                                new GameMessageSystemChat(
                                    $"You cannot dispel the Shroud while performing other actions.",
                                    ChatMessageType.Broadcast
                                )
                            );
                            return;
                        }

                        if (player.Teleporting)
                        {
                            player.Session.Network.EnqueueSend(
                                new GameMessageSystemChat(
                                    $"You cannot dispel the Shroud while teleporting.",
                                    ChatMessageType.Broadcast
                                )
                            );
                            return;
                        }

                        if (player.LastSuccessCast_Time > Time.GetUnixTime() - 5.0)
                        {
                            player.Session.Network.EnqueueSend(
                                new GameMessageSystemChat(
                                    $"You cannot dispel the Shroud if you have recently cast a spell.",
                                    ChatMessageType.Broadcast
                                )
                            );
                            return;
                        }
                        if (player.CurrentLandblock != null && player.CurrentLandblock.IsDungeon)
                        {
                            player.Session.Network.EnqueueSend(
                                new GameMessageSystemChat(
                                    $"You cannot dispel the Shroud while inside a dungeon.",
                                    ChatMessageType.Broadcast
                                )
                            );
                            return;
                        }

                        if (player.Fellowship != null)
                        {
                            player.Session.Network.EnqueueSend(
                                new GameMessageSystemChat(
                                    $"You must leave your Fellowship before you can dispel the Shroud.",
                                    ChatMessageType.Broadcast
                                )
                            );
                            return;
                        }

                        var enchantment = player.EnchantmentManager.GetEnchantment(5379);
                        if (enchantment != null)
                        {
                            player.EnchantmentManager.Dispel(enchantment);
                            player.HandleSpellHooks(new Spell(5379));
                            player.PlayParticleEffect(PlayScript.DispelCreature, player.Guid);
                            player.Session.Network.EnqueueSend(
                                new GameMessageSystemChat(
                                    $"You dispel the Shroud, and your innate strength returns.",
                                    ChatMessageType.Broadcast
                                )
                            );
                        }
                    }
                    else
                    {
                        var spell = new Spell(5379);
                        var addResult = player.EnchantmentManager.Add(spell, null, null, true);
                        player.Session.Network.EnqueueSend(
                            new GameEventMagicUpdateEnchantment(
                                player.Session,
                                new Enchantment(player, addResult.Enchantment)
                            )
                        );
                        player.HandleSpellHooks(spell);
                        player.PlayParticleEffect(PlayScript.SkillDownVoid, player.Guid);
                        player.Session.Network.EnqueueSend(
                            new GameMessageSystemChat(
                                $"You activate the crystal, shrouding yourself and reducing your innate power.",
                                ChatMessageType.Broadcast
                            )
                        );
                    }
                    break;
            }
        }

        if (UseCreateItem > 0)
        {
            if (!HandleUseCreateItem(player))
            {
                return;
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

    public bool HandleUseCreateItem(Player player)
    {
        var amount = UseCreateQuantity ?? 1;

        var itemsToReceive = new ItemsToReceive(player);

        itemsToReceive.Add(UseCreateItem.Value, amount);

        if (itemsToReceive.PlayerExceedsLimits)
        {
            if (itemsToReceive.PlayerExceedsAvailableBurden)
            {
                player.Session.Network.EnqueueSend(
                    new GameEventCommunicationTransientString(player.Session, "You are too encumbered to use that!")
                );
            }
            else if (itemsToReceive.PlayerOutOfInventorySlots)
            {
                player.Session.Network.EnqueueSend(
                    new GameEventCommunicationTransientString(
                        player.Session,
                        "You do not have enough pack space to use that!"
                    )
                );
            }
            else if (itemsToReceive.PlayerOutOfContainerSlots)
            {
                player.Session.Network.EnqueueSend(
                    new GameEventCommunicationTransientString(
                        player.Session,
                        "You do not have enough container slots to use that!"
                    )
                );
            }

            return false;
        }

        if (itemsToReceive.RequiredSlots > 0)
        {
            var remaining = amount;

            while (remaining > 0)
            {
                var item = WorldObjectFactory.CreateNewWorldObject(UseCreateItem.Value);

                if (item is Stackable)
                {
                    var stackSize = Math.Min(remaining, item.MaxStackSize ?? 1);

                    item.SetStackSize(stackSize);
                    remaining -= stackSize;
                }
                else
                {
                    remaining--;
                }

                player.TryCreateInInventoryWithNetworking(item);
            }
        }
        else
        {
            player.SendTransientError($"Unable to use {Name} at this time!");
            return false;
        }
        return true;
    }

    public int? RareId
    {
        get => GetProperty(PropertyInt.RareId);
        set
        {
            if (!value.HasValue)
            {
                RemoveProperty(PropertyInt.RareId);
            }
            else
            {
                SetProperty(PropertyInt.RareId, value.Value);
            }
        }
    }

    public bool RareUsesTimer
    {
        get => GetProperty(PropertyBool.RareUsesTimer) ?? false;
        set
        {
            if (!value)
            {
                RemoveProperty(PropertyBool.RareUsesTimer);
            }
            else
            {
                SetProperty(PropertyBool.RareUsesTimer, value);
            }
        }
    }

    public override void HandleActionUseOnTarget(Player player, WorldObject target)
    {
        // should tailoring kit / aetheria be subtyped?
        if (Tailoring.IsTailoringKit(WeenieClassId))
        {
            Tailoring.UseObjectOnTarget(player, this, target);
            return;
        }

        if (target.WeenieType == WeenieType.CombatFocusAlterationGem)
        {
            CombatFocusAlterationGem.UseObjectOnTarget(player, this, target);
            return;
        }

        // fallback on recipe manager?
        base.HandleActionUseOnTarget(player, target);
    }

    /// <summary>
    /// For Rares that use cooldown timers (RareUsesTimer),
    /// any other rares with RareUsesTimer may not be used for 3 minutes
    /// Note that if the player logs out, this cooldown timer continues to tick/expire (unlike enchantments)
    /// </summary>
    public static int RareTimer = 180;

    public string UseSendsSignal
    {
        get => GetProperty(PropertyString.UseSendsSignal);
        set
        {
            if (value == null)
            {
                RemoveProperty(PropertyString.UseSendsSignal);
            }
            else
            {
                SetProperty(PropertyString.UseSendsSignal, value);
            }
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
