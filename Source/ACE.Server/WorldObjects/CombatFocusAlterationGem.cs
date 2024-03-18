using System;
using System.Linq;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Entity.Models;
using ACE.Server.Entity;
using ACE.Server.Network.GameMessages.Messages;
using Serilog;

namespace ACE.Server.WorldObjects
{
    public class CombatFocusAlterationGem : WorldObject
    {
        private readonly ILogger _log = Log.ForContext<CombatFocus>();

        /// <summary>
        /// A new biota be created taking all of its values from weenie.
        /// </summary>
        public CombatFocusAlterationGem(Weenie weenie, ObjectGuid guid) : base(weenie, guid)
        {
            SetEphemeralValues();
        }

        /// <summary>
        /// Restore a WorldObject from the database.
        /// </summary>
        public CombatFocusAlterationGem(Biota biota) : base(biota)
        {
            SetEphemeralValues();
        }

        private void SetEphemeralValues()
        {
        }

        public override void HandleActionUseOnTarget(Player player, WorldObject target)
        {
            UseObjectOnTarget(player, this, target);
        }

        public static void UseObjectOnTarget(Player player, WorldObject source, WorldObject target, bool confirmed = false)
        {
            if (player.IsBusy)
            {
                player.SendUseDoneEvent(WeenieError.YoureTooBusy);
                return;
            }

            var combatFocus = target as CombatFocus;
            if (combatFocus == null)
            {
                player.Session.Network.EnqueueSend(new GameMessageSystemChat($"You cannot use the {source.Name} with the {target.Name}.", ChatMessageType.Craft));
                player.SendUseDoneEvent();
                return;
            }

            if (combatFocus.Wielder != null)
            {
                player.Session.Network.EnqueueSend(new GameMessageSystemChat($"You must first unwield the Combat Focus before altering it.", ChatMessageType.Craft));
                player.SendUseDoneEvent();
                return;
            }

            if (source.ItemSpellId == null)
            {
                if (!confirmed)
                {
                    if (!player.ConfirmationManager.EnqueueSend(new Confirmation_CraftInteration(player.Guid, source.Guid, target.Guid), $"Use {source.Name} gem on {target.Name} to reset it to its base state?"))
                        player.SendUseDoneEvent(WeenieError.ConfirmationInProgress);
                    else
                        player.SendUseDoneEvent();

                    return;
                }

                var newFocus = Factories.WorldObjectFactory.CreateNewWorldObject(target.WeenieClassId);
                if (newFocus != null)
                {
                    player.Session.Network.EnqueueSend(new GameMessageSystemChat($"You reset the {target.Name} to its base state.", ChatMessageType.Craft));
                    player.TryConsumeFromInventoryWithNetworking(source);
                    player.TryConsumeFromInventoryWithNetworking(target);
                    player.TryCreateInInventoryWithNetworking(newFocus);
                }
            }

            var spellId = (SpellId)source.ItemSpellId;
            var spellMatch = combatFocus.GetCurrentSpellList().Contains(spellId) ? true : false;
            var isAttribute = IsAttribute(spellId);

            if (isAttribute)
            {
                if (!spellMatch && combatFocus.CombatFocusAttributeSpellRemoved == null)
                {
                    player.Session.Network.EnqueueSend(new GameMessageSystemChat($"You must remove an attribute spell from {combatFocus.Name} before a new attribute spell can be added.", ChatMessageType.Craft));
                    player.SendUseDoneEvent();
                    return;
                }
                if (!spellMatch && combatFocus.CombatFocusAttributeSpellAdded != null)
                {
                    player.Session.Network.EnqueueSend(new GameMessageSystemChat($"You have already added an attribute spell once to {combatFocus.Name}.", ChatMessageType.Craft));
                    player.SendUseDoneEvent();
                    return;
                }
                if (spellMatch && combatFocus.CombatFocusAttributeSpellRemoved != null)
                {
                    player.Session.Network.EnqueueSend(new GameMessageSystemChat($"You have already removed an attribute spell once from {combatFocus.Name}.", ChatMessageType.Craft));
                    player.SendUseDoneEvent();
                    return;
                }
            }

            if (!isAttribute)
            {
                if (!spellMatch && combatFocus.CombatFocusSkillSpellRemoved == null)
                {
                    player.Session.Network.EnqueueSend(new GameMessageSystemChat($"You must remove a skill spell from {combatFocus.Name} before a new skill spell can be added.", ChatMessageType.Craft));
                    player.SendUseDoneEvent();
                    return;
                }
                if (!spellMatch && combatFocus.CombatFocusSkillSpellAdded != null)
                {
                    player.Session.Network.EnqueueSend(new GameMessageSystemChat($"You have already added a skill spell once to {combatFocus.Name}.", ChatMessageType.Craft));
                    player.SendUseDoneEvent();
                    return;
                }
                if (spellMatch && combatFocus.CombatFocusSkillSpellRemoved != null)
                {
                    player.Session.Network.EnqueueSend(new GameMessageSystemChat($"You have already removed a skill spell once from {combatFocus.Name}.", ChatMessageType.Craft));
                    player.SendUseDoneEvent();
                    return;
                }
            }

            var change = spellMatch ? "remove" : "add";
            var property = isAttribute ? "attribute" : "skill";

            if (!confirmed)
            {
                if (!player.ConfirmationManager.EnqueueSend(new Confirmation_CraftInteration(player.Guid, source.Guid, target.Guid), $"Use {source.Name} gem on {target.Name} to {change} this {property}?"))
                    player.SendUseDoneEvent(WeenieError.ConfirmationInProgress);
                else
                    player.SendUseDoneEvent();

                return;
            }

            if (spellMatch)
                combatFocus.RemoveSpell(player, source, spellId, isAttribute);
            else
                combatFocus.AddSpell(player, source, spellId, isAttribute);

            combatFocus.UpdateDescriptionText();

            UpdateObj(player, target);
        }
        

        private static bool IsAttribute(SpellId spellId)
        {
            SpellId[] attributeSpellIds = { SpellId.StrengthSelf1, SpellId.EnduranceSelf1, SpellId.CoordinationSelf1,
                                            SpellId.QuicknessSelf1, SpellId.FocusSelf1, SpellId.WillpowerSelf1};

            if (attributeSpellIds.Contains(spellId))
                return true;

            return false;
        }

        private static void UpdateObj(Player player, WorldObject obj)
        {
            player.EnqueueBroadcast(new GameMessageUpdateObject(obj));

            if (obj.CurrentWieldedLocation != null)
            {
                player.EnqueueBroadcast(new GameMessageObjDescEvent(player));
                return;
            }

            var invObj = player.FindObject(obj.Guid.Full, Player.SearchLocations.MyInventory);

            if (invObj != null)
                player.MoveItemToFirstContainerSlot(obj);
        }
    }
}
