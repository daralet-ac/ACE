using ACE.Common;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Entity;
using ACE.Server.Entity.Actions;
using ACE.Server.Network.GameEvent.Events;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.Network.Structure;
using System;

namespace ACE.Server.WorldObjects
{
    partial class Player
    {
        private static uint EnterSneakingDifficulty = 50;

        public bool IsSneaking = false;
        public bool IsAttackFromSneaking = false;

        public void BeginSneaking()
        {
            if (IsSneaking)
                return;

            var result = TestSneakingInternal(EnterSneakingDifficulty);
            if (result == SneakingTestResult.Success)
            {
                IsSneaking = true;
                Session.Network.EnqueueSend(new GameMessageSystemChat("You start sneaking.", ChatMessageType.Broadcast));
                EnqueueBroadcast(new GameMessageScript(Guid, PlayScript.SneakingBegin));

                var spell = new Spell(SpellId.MireFoot);
                var addResult = EnchantmentManager.Add(spell, null, null, true);

                Session.Network.EnqueueSend(new GameEventMagicUpdateEnchantment(Session, new Enchantment(this, addResult.Enchantment)));
                HandleRunRateUpdate(spell);

                RadarColor = ACE.Entity.Enum.RadarColor.Creature;
                EnqueueBroadcast(true, new GameMessagePublicUpdatePropertyInt(this, PropertyInt.RadarBlipColor, (int)RadarColor));
                //The following code would remove the player from radar but has the drawback of having to reload the player causing some twitchiness and other players to lose target, which would be fine when entering sneak state but it's rather annoying when leaving sneak state.
                //SetProperty(PropertyInt.ShowableOnRadar, (int)ACE.Entity.Enum.RadarBehavior.ShowNever);
                //EnqueueBroadcast(false, new GameMessageUpdateObject(this));
                //var actionChain = new ActionChain();
                //actionChain.AddDelaySeconds(0.25f);
                //actionChain.AddAction(this, () => EnqueueBroadcast(new GameMessageScript(Guid, PlayScript.SneakingBegin)));
                //actionChain.EnqueueChain();
            }
            else if(result == SneakingTestResult.Failure)
                Session.Network.EnqueueSend(new GameMessageSystemChat("You fail on your attempt to start sneaking.", ChatMessageType.Broadcast));
            else
                Session.Network.EnqueueSend(new GameMessageSystemChat("You are not trained in sneaking!", ChatMessageType.Broadcast));
        }

        public void EndSneaking(string message = null, bool isAttackFromSneaking = false)
        {
            if (!IsSneaking)
                return;

            IsSneaking = false;
            IsAttackFromSneaking = isAttackFromSneaking;

            Session.Network.EnqueueSend(new GameMessageSystemChat(message == null ? "You stop sneaking." : message, ChatMessageType.Broadcast));
            // Add delay here to avoid remaining translucent indefinitely when EndSneaking is called right after BeginSneaking(like when a creature detects the player instantly).
            var actionChain = new ActionChain();
            actionChain.AddDelaySeconds(0.25f);
            if (!Teleporting)
            {
                actionChain.AddAction(this, () =>
                {
                    EnqueueBroadcast(new GameMessageScript(Guid, PlayScript.SneakingEnd));
                });
            }
            actionChain.AddAction(this, () =>
            {
                var propertiesEnchantmentRegistry = EnchantmentManager.GetEnchantment((uint)SpellId.MireFoot, null);
                if (propertiesEnchantmentRegistry != null)
                {
                    EnchantmentManager.Dispel(propertiesEnchantmentRegistry);
                    if (!Teleporting)
                        HandleRunRateUpdate(new Spell(propertiesEnchantmentRegistry.SpellId));
                }
            });

            RadarColor = null;
            EnqueueBroadcast(true, new GameMessagePublicUpdatePropertyInt(this, PropertyInt.RadarBlipColor, 0));
            //The following code would remove the player from radar but has the drawback of having to reload the player causing some twitchiness and other players to lose target, which would be fine when entering sneak state but it's rather annoying when leaving sneak state.
            //actionChain.AddDelaySeconds(1.0f);
            //actionChain.AddAction(this, () =>
            //{
            //    SetProperty(PropertyInt.ShowableOnRadar, (int)ACE.Entity.Enum.RadarBehavior.ShowAlways);
            //    EnqueueBroadcast(false, new GameMessageUpdateObject(this));
            //});

            actionChain.EnqueueChain();
        }

        public bool TestSneaking(Creature creature, double distanceSquared, string failureMessage)
        {
            if (!IsSneaking)
                return false;

            if (creature == null || creature.PlayerKillerStatus == PlayerKillerStatus.RubberGlue || creature.PlayerKillerStatus == PlayerKillerStatus.Protected || distanceSquared > creature.VisualAwarenessRangeSq || !creature.IsDirectVisible(this))
                return true;

            uint difficulty;

            var angle = Math.Abs(creature.GetAngle(this));
            if (angle < 90)
            {
                if (distanceSquared < 2)
                {
                    EndSneaking(failureMessage);
                    return false;
                }
                else if (distanceSquared < creature.VisualAwarenessRangeSq / 10)
                    difficulty = (uint)((creature.Level ?? 1) * 3.0f);
                else if (distanceSquared < creature.VisualAwarenessRangeSq / 5)
                    difficulty = (uint)((creature.Level ?? 1) * 2.0f);
                else
                    difficulty = (uint)((creature.Level ?? 1) * 1.0f);
            }
            else
                difficulty = (uint)((creature.Level ?? 1) * 0.5f);

            return TestSneaking(difficulty, failureMessage);
        }

        public bool TestSneaking(Creature creature, string failureMessage)
        {
            if (!IsSneaking)
                return false;

            if (creature == null)
                return true;

            return TestSneaking(creature, PhysicsObj.get_distance_sq_to_object(creature.PhysicsObj, true), failureMessage);
        }

        public bool TestSneaking(uint difficulty, string failureMessage)
        {
            if(TestSneakingInternal(difficulty) != SneakingTestResult.Success)
            {
                EndSneaking(failureMessage);
                return false;
            }
            else
                return true;
        }

        private enum SneakingTestResult
        {
            Untrained,
            Failure,
            Success
        }

        private SneakingTestResult TestSneakingInternal(uint difficulty)
        {
            var sneakingSkill = GetCreatureSkill(Skill.Sneaking);
            if (sneakingSkill.AdvancementClass < SkillAdvancementClass.Trained)
                return SneakingTestResult.Untrained;

            var moddedSneakingSkill = GetModdedSneakingSkill();

            var chance = SkillCheck.GetSkillChance(moddedSneakingSkill, difficulty);
            if (chance > ThreadSafeRandom.Next(0.0f, 1.0f))
            {
                Proficiency.OnSuccessUse(this, sneakingSkill, difficulty);
                return SneakingTestResult.Success;
            }
            return SneakingTestResult.Failure;
        }

        public bool IsAware(WorldObject wo)
        {
            return wo.IsAware(this);
        }

        public bool TestAwareness(WorldObject wo)
        {
            var awarenessSkill = GetCreatureSkill(Skill.Awareness);
            var chance = SkillCheck.GetSkillChance(awarenessSkill.Current, (uint)(wo.ResistAwareness ?? 0));
            if (chance > ThreadSafeRandom.Next(0.0f, 1.0f))
            {
                Proficiency.OnSuccessUse(this, awarenessSkill, (uint)(wo.ResistAwareness ?? 0));
                return true;
            }
            return false;
        }
    }
}
