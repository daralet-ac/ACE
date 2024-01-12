using ACE.Common;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Entity;
using ACE.Server.Entity.Actions;
using ACE.Server.Network.GameEvent.Events;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.Network.Structure;
using System;
using System.Linq;

namespace ACE.Server.WorldObjects
{
    partial class Player
    {
        private static uint EnterStealthDifficulty = 50;

        public bool IsStealthed = false;
        public bool IsAttackFromStealth = false;

        public void BeginStealth()
        {
            if (IsStealthed)
                return;

            var result = TestStealthInternal(EnterStealthDifficulty);
            if (result == StealthTestResult.Success)
            {
                IsStealthed = true;
                Session.Network.EnqueueSend(new GameMessageSystemChat("You enter stealth.", ChatMessageType.Broadcast));
                EnqueueBroadcast(new GameMessageScript(Guid, PlayScript.StealthBegin));

                var spell = new Spell(SpellId.MireFoot);
                var addResult = EnchantmentManager.Add(spell, null, null, true);

                Session.Network.EnqueueSend(new GameEventMagicUpdateEnchantment(Session, new Enchantment(this, addResult.Enchantment)));
                HandleRunRateUpdate(spell);

                RadarColor = ACE.Entity.Enum.RadarColor.Creature;
                EnqueueBroadcast(true, new GameMessagePublicUpdatePropertyInt(this, PropertyInt.RadarBlipColor, (int)RadarColor));
            }
            else if(result == StealthTestResult.Failure)
                Session.Network.EnqueueSend(new GameMessageSystemChat("You fail on your attempt to enter stealth.", ChatMessageType.Broadcast));
            else
                Session.Network.EnqueueSend(new GameMessageSystemChat("You are not trained in thievery!", ChatMessageType.Broadcast));
        }

        public void EndStealth(string message = null, bool isAttackFromStealth = false)
        {
            if (!IsStealthed)
                return;

            IsStealthed = false;
            IsAttackFromStealth = isAttackFromStealth;

            Session.Network.EnqueueSend(new GameMessageSystemChat(message == null ? "You lose stealth." : message, ChatMessageType.Broadcast));
            var actionChain = new ActionChain();
            actionChain.AddDelaySeconds(0.25f);
            if (!Teleporting)
            {
                actionChain.AddAction(this, () =>
                {
                    EnqueueBroadcast(new GameMessageScript(Guid, PlayScript.StealthEnd));
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

            actionChain.EnqueueChain();
        }

        public bool TestStealth(Creature creature, double distanceSquared, string failureMessage)
        {
            if (!IsStealthed)
                return false;

            if (creature == null || creature.PlayerKillerStatus == PlayerKillerStatus.RubberGlue || creature.PlayerKillerStatus == PlayerKillerStatus.Protected || distanceSquared > creature.VisualAwarenessRangeSq || !creature.IsDirectVisible(this))
                return true;

            uint difficulty;

            var angle = Math.Abs(creature.GetAngle(this));
            if (angle < 90)
            {
                if (distanceSquared < 2)
                {
                    EndStealth(failureMessage);
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

            return TestStealth(difficulty, failureMessage);
        }

        public bool TestStealth(Creature creature, string failureMessage)
        {
            if (!IsStealthed)
                return false;

            if (creature == null)
                return true;

            return TestStealth(creature, PhysicsObj.get_distance_sq_to_object(creature.PhysicsObj, true), failureMessage);
        }

        public bool TestStealth(uint difficulty, string failureMessage)
        {
            if(TestStealthInternal(difficulty) != StealthTestResult.Success)
            {
                EndStealth(failureMessage);
                return false;
            }
            else
                return true;
        }

        private enum StealthTestResult
        {
            Untrained,
            Failure,
            Success
        }

        private StealthTestResult TestStealthInternal(uint difficulty)
        {
            var stealthSkill = GetCreatureSkill(Skill.Lockpick); // Thievery
            if (stealthSkill.AdvancementClass < SkillAdvancementClass.Trained)
                return StealthTestResult.Untrained;

            var moddedStealthSkill = GetModdedThieverySkill();

            var chance = SkillCheck.GetSkillChance(moddedStealthSkill, difficulty);
            if (chance > ThreadSafeRandom.Next(0.0f, 1.0f))
            {
                Proficiency.OnSuccessUse(this, stealthSkill, difficulty);
                return StealthTestResult.Success;
            }
            return StealthTestResult.Failure;
        }

        public bool IsAware(WorldObject wo)
        {
            return wo.IsAware(this);
        }

        public bool TestPerception(WorldObject wo)
        {
            var perceptionSkill = GetCreatureSkill(Skill.AssessCreature); // Perception
            var chance = SkillCheck.GetSkillChance(perceptionSkill.Current, (uint)(wo.ResistPerception ?? 0));
            if (chance > ThreadSafeRandom.Next(0.0f, 1.0f))
            {
                Proficiency.OnSuccessUse(this, perceptionSkill, (uint)(wo.ResistPerception ?? 0));
                return true;
            }
            return false;
        }

        private double NextThreatTableTime = 0.0;
        public void GetMonsterThreatTable(Creature monster)
        {

            if (monster != null && IsAttemptingToPerceiveThreats && NextThreatTableTime < Time.GetUnixTime())
            {
                var skillCheck = SkillCheck.GetSkillChance(GetModdedPerceptionSkill(), monster.GetCreatureSkill(Skill.Deception).Current);

                if (ThreadSafeRandom.Next(0.0f, 1.0f) > skillCheck)
                {
                    Session.Network.EnqueueSend(new GameMessageSystemChat($"You fail to perceive {monster.Name}'s current threats.", ChatMessageType.Broadcast));
                    return;
                }
                else
                {
                    if (monster.PositiveThreat.Count > 0)
                    {
                        Session.Network.EnqueueSend(new GameMessageSystemChat($"{monster.Name}'s Threat Table:", ChatMessageType.Broadcast));

                        var sortedDict = from entry in monster.PositiveThreat orderby entry.Value descending select entry;

                        foreach (var entry in sortedDict)
                        {
                            string threatLevel;

                            switch (entry.Value)
                            {
                                case > 0.5f: threatLevel = $"Major threat"; break;
                                case > 0.25f: threatLevel = $"Moderate threat"; break;
                                case > 0.0f:
                                default: threatLevel = $"Minor threat"; break;
                            }
                            Session.Network.EnqueueSend(new GameMessageSystemChat($"   {entry.Key.Name}: {threatLevel}", ChatMessageType.Broadcast));
                        }
                    }

                    if (monster.NegativeThreat.Count > 0)
                    {
                        //Session.Network.EnqueueSend(new GameMessageSystemChat($"  ----------------", ChatMessageType.Broadcast));

                        var sortedDict = from entry in monster.NegativeThreat orderby entry.Value descending select entry;

                        foreach (var entry in sortedDict)
                        {
                            string threatLevel;
                            switch (entry.Value)
                            {
                                case > -0.25f: threatLevel = $"Potential threat"; break;
                                case > -0.5f: threatLevel = $"Unthreatening"; break;
                                case > -1.0f:
                                default: threatLevel = $"Harmless"; break;
                            }
                            Session.Network.EnqueueSend(new GameMessageSystemChat($"   {entry.Key.Name}: {threatLevel}", ChatMessageType.Broadcast));
                        }
                    }

                    NextThreatTableTime = Time.GetUnixTime() + 10;
                }
            }
        }
    }
}
