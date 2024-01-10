using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using ACE.Common;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Entity.Models;
using ACE.Server.Entity;
using ACE.Server.Entity.Actions;
using ACE.Server.Factories;
using ACE.Server.Managers;
using ACE.Server.Network.Enum;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.Network.Sequence;
using ACE.Server.Network.Structure;
using ACE.Server.Physics;
using ACE.Server.Physics.Common;

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

            Session.Network.EnqueueSend(new GameMessageSystemChat($"You taunt {target.Name}, increasingly your threat level substantially. ({staminaCost} stamina)", ChatMessageType.Broadcast));

            PlayParticleEffect(PlayScript.VisionUpWhite, Guid);
            target.PlayParticleEffect(PlayScript.VisionDownBlack, target.Guid);
        }

        public void TryUseAreaTaunt(WorldObject ability)
        {

        }
        public void TryUseFeignInjury(WorldObject ability)
        {

        }
        public void TryUseVanish(WorldObject ability)
        {

        }
    }
}
