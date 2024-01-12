using ACE.Common;
using ACE.Entity.Enum;
using ACE.Server.Network.GameMessages.Messages;
using System;

namespace ACE.Server.WorldObjects
{
    partial class Player
    {
        public double LastFocusedTaunt = 0;
        public double LastFeignWeakness = 0;

        public void TryUseFocusedTaunt(WorldObject ability)
        {
            Creature target = LastAttackedCreature;

            var skillCheck = SkillCheck.GetSkillChance(Strength.Current * 2, target.GetCreatureSkill(Skill.AssessCreature).Current);
            
            if (ThreadSafeRandom.Next(0.0f, 1.0f) > skillCheck)
            {
                Session.Network.EnqueueSend(new GameMessageSystemChat($"{target.Name} can see right through you. Your taunt failed.", ChatMessageType.Broadcast));
                return; 
            }

            var targetTier = target.Tier ?? 1;
            var staminaCost = -10 * Math.Clamp(targetTier, 1, 7);
            UpdateVitalDelta(Stamina, staminaCost);

            target.IncreaseTargetThreatLevel(this, 100);
            LastFocusedTaunt = Time.GetUnixTime();

            Session.Network.EnqueueSend(new GameMessageSystemChat($"You successfully taunt {target.Name}, increasingly your threat level substantially. ({staminaCost} stamina)", ChatMessageType.Broadcast));

            PlayParticleEffect(PlayScript.VisionUpWhite, Guid);
            target.PlayParticleEffect(PlayScript.VisionDownBlack, target.Guid);
        }

        public void TryUseAreaTaunt(WorldObject ability)
        {
            var nearbyMonsters = GetNearbyMonsters(5);

            foreach  (var target in nearbyMonsters)
            {
                var skillCheck = SkillCheck.GetSkillChance(Strength.Current * 2, target.GetCreatureSkill(Skill.AssessCreature).Current);

                if (ThreadSafeRandom.Next(0.0f, 1.0f) > skillCheck)
                {
                    Session.Network.EnqueueSend(new GameMessageSystemChat($"{target.Name} can see right through you. Your taunt failed.", ChatMessageType.Broadcast));
                    continue;
                }

                var targetTier = target.Tier ?? 1;
                var staminaCost = -2 * Math.Clamp(targetTier, 1, 7);
                UpdateVitalDelta(Stamina, staminaCost);

                target.IncreaseTargetThreatLevel(this, 100);
                LastFocusedTaunt = Time.GetUnixTime();

                Session.Network.EnqueueSend(new GameMessageSystemChat($"You successfully taunt {target.Name}, increasingly your threat level substantially. ({staminaCost} stamina)", ChatMessageType.Broadcast));

                PlayParticleEffect(PlayScript.VisionUpWhite, Guid);
                target.PlayParticleEffect(PlayScript.VisionDownBlack, target.Guid);
            }
        }

        public void TryUseFeignInjury(WorldObject ability)
        {
            var nearbyMonsters = GetNearbyMonsters(5);

            foreach (var target in nearbyMonsters)
            {
                var skillCheck = SkillCheck.GetSkillChance(GetModdedDeceptionSkill(), target.GetCreatureSkill(Skill.AssessCreature).Current);

                if (ThreadSafeRandom.Next(0.0f, 1.0f) > skillCheck)
                {
                    Session.Network.EnqueueSend(new GameMessageSystemChat($"{target.Name} can see right through you. Your feign weakness failed.", ChatMessageType.Broadcast));
                    continue;
                }

                var targetTier = target.Tier ?? 1;
                var staminaCost = -2 * Math.Clamp(targetTier, 1, 7);
                UpdateVitalDelta(Stamina, staminaCost);

                target.IncreaseTargetThreatLevel(this, -100);
                LastFocusedTaunt = Time.GetUnixTime();

                Session.Network.EnqueueSend(new GameMessageSystemChat($"You successfully feign a weakness, reducing your threat level substantially with all nearby enemies. ({staminaCost} stamina)", ChatMessageType.Broadcast));

                PlayParticleEffect(PlayScript.VisionUpWhite, Guid);
                target.PlayParticleEffect(PlayScript.VisionDownBlack, target.Guid);
            }
        }

        public void TryUseVanish(WorldObject ability)
        {

        }
    }
}
