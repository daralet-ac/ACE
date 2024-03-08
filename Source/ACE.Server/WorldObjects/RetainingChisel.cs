using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Entity.Models;
using ACE.Server.Entity;
using ACE.Server.Entity.Actions;
using ACE.Server.Managers;
using ACE.Server.Network.GameMessages.Messages;
using Serilog;
using System;

namespace ACE.Server.WorldObjects
{
    public class RetainingChisel : WorldObject
    {
        private static readonly ILogger _log = Log.ForContext(typeof(RetainingChisel));

        public RetainingChisel(Weenie weenie, ObjectGuid guid) : base(weenie, guid)
        {
            SetEphemeralValues();
        }

        public RetainingChisel(Biota biota) : base(biota)
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

            if (!RecipeManager.VerifyUse(player, source, target, true) || target.Workmanship == null)
            {
                player.SendUseDoneEvent(WeenieError.YouDoNotPassCraftingRequirements);
                return;
            }

            if (target.Workmanship == null)
            {
                player.Session.Network.EnqueueSend(new GameMessageSystemChat($"The {target.Name} cannot be retained.", ChatMessageType.Craft));
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

            actionChain.AddAction(player, () =>
            {
                if (target.Retained == false)
                {
                    target.Retained = true;
                    player.Session.Network.EnqueueSend(new GameMessageSystemChat($"The {target.NameWithMaterial} is now retained.", ChatMessageType.Craft));
                    player.SendUseDoneEvent();
                }
                else
                {
                    target.Retained = false;
                    player.Session.Network.EnqueueSend(new GameMessageSystemChat($"The {target.NameWithMaterial} is no longer retained.", ChatMessageType.Craft));
                    player.SendUseDoneEvent();
                }
                player.EnqueueBroadcast(new GameMessageUpdateObject(target));

            });

            player.EnqueueMotion(actionChain, MotionCommand.Ready);

            actionChain.AddAction(player, () =>
            {
                player.IsBusy = false;
            });

            actionChain.EnqueueChain();

            player.NextUseTime = DateTime.UtcNow.AddSeconds(animTime);
        }
    }
}
