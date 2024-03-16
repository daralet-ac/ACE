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

            //if (!RecipeManager.VerifyUse(player, source, target, true) || target.Workmanship == null)
            //{
            //    player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
            //    return;
            //}

            if (!confirmed)
            {
                if (!player.ConfirmationManager.EnqueueSend(new Confirmation_CraftInteration(player.Guid, source.Guid, target.Guid), $"Use {source.Name} gem on {target.Name}?"))
                    player.SendUseDoneEvent(WeenieError.ConfirmationInProgress);
                else
                    player.SendUseDoneEvent();

                return;
            }
            
            var spellId = (SpellId)source.ItemSpellId;
            var isAttribute = IsAttribute(spellId);

            if (combatFocus.GetCurrentSpellList().Contains(spellId))
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
