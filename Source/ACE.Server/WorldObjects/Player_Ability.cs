using ACE.Common;
using ACE.Entity.Enum;
using ACE.Server.Entity;
using ACE.Server.Factories.Tables;
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

        public void TryUseExposePhysicalWeakness(WorldObject ability)
        {
            Creature target = LastAttackedCreature;

            if (this == target || target.IsDead)
                return;

            var targetAsPlayer = target as Player;

            var attackerPerceptionSkill = GetModdedPerceptionSkill();
            var targetDeceptionSkill = target.GetModdedDeceptionSkill();

            var avoidChance = 1.0f - SkillCheck.GetSkillChance(attackerPerceptionSkill, targetDeceptionSkill);

            var combatAbility = CombatAbility.None;
            var combatFocus = GetEquippedCombatFocus();
            if (combatFocus != null)
                combatAbility = combatFocus.GetCombatAbility();

            // COMBAT ABILITY - Iron Fist: 20% increased chance to expose enemy weaknesses
            if (combatAbility == CombatAbility.IronFist)
                avoidChance -= 0.2f;

            var roll = ThreadSafeRandom.Next(0.0f, 1.0f);
            if (avoidChance > roll)
            {
                Session.Network.EnqueueSend(new GameMessageSystemChat($"{target.Name}'s deception prevents you from exposing a weakness!", ChatMessageType.Broadcast));

                if (targetAsPlayer != null)
                {
                    Proficiency.OnSuccessUse(targetAsPlayer, target.GetCreatureSkill(Skill.Deception), attackerPerceptionSkill);
                    targetAsPlayer.Session.Network.EnqueueSend(new GameMessageSystemChat($"Your deception prevents {Name} from exposing a weakness!", ChatMessageType.Broadcast));
                }
                return;
            }
            else
                Proficiency.OnSuccessUse(this, GetCreatureSkill(Skill.AssessCreature), targetDeceptionSkill);

            var vulnerabilitySpellLevels = SpellLevelProgression.GetSpellLevels(SpellId.VulnerabilityOther1);
            var imperilSpellLevels = SpellLevelProgression.GetSpellLevels(SpellId.ImperilOther1);

            if (vulnerabilitySpellLevels.Count == 0 || imperilSpellLevels.Count == 0)
                return;

            var overRoll = roll - avoidChance;
            int maxSpellLevel = (int)Math.Clamp(Math.Floor((double)attackerPerceptionSkill / 50), 1, 7);
            int spellLevel = (int)Math.Clamp(Math.Floor(overRoll * 10), 1, maxSpellLevel);

            var vulnerabilitySpell = new Spell(vulnerabilitySpellLevels[spellLevel - 1]);
            var imperilSpellLevel = new Spell(imperilSpellLevels[spellLevel - 1]);

            string spellTypePrefix;
            switch (spellLevel)
            {
                case 1: spellTypePrefix = "a slight"; break;
                default:
                case 2: spellTypePrefix = "a minor"; break;
                case 3: spellTypePrefix = "a moderate"; break;
                case 4: spellTypePrefix = "a major"; break;
                case 5: spellTypePrefix = "a severe"; break;
                case 6: spellTypePrefix = "a crippling"; break;
                case 7: spellTypePrefix = "a tremendous"; break;
            }

            Session.Network.EnqueueSend(new GameMessageSystemChat($"Your perception allows you to expose {spellTypePrefix} physical weakness on {target.Name}!", ChatMessageType.Broadcast));
            if (targetAsPlayer != null)
                targetAsPlayer.Session.Network.EnqueueSend(new GameMessageSystemChat($"{Name}'s perception exposes {spellTypePrefix} physical weakness on you!", ChatMessageType.Broadcast));

            if (vulnerabilitySpell.NonComponentTargetType == ItemType.None)
                TryCastSpell(vulnerabilitySpell, null, this, null, false, false, false, false);
            else
                TryCastSpell(vulnerabilitySpell, target, this, null, false, false, false, false);

            if (GetCreatureSkill(Skill.AssessCreature).AdvancementClass == SkillAdvancementClass.Specialized)
            {
                if (imperilSpellLevel.NonComponentTargetType == ItemType.None)
                    TryCastSpell(imperilSpellLevel, null, this, null, false, false, false, false);
                else
                    TryCastSpell(imperilSpellLevel, target, this, null, false, false, false, false);
            }
        }

        public void TryUseExposeMagicalWeakness(WorldObject ability)
        {
            {
                Creature target = LastAttackedCreature;

                if (this == target || target.IsDead)
                    return;

                var targetAsPlayer = target as Player;

                var attackerPerceptionSkill = GetModdedPerceptionSkill();
                var targetDeceptionSkill = target.GetModdedDeceptionSkill();

                var avoidChance = 1.0f - SkillCheck.GetSkillChance(attackerPerceptionSkill, targetDeceptionSkill);

                var combatAbility = CombatAbility.None;
                var combatFocus = GetEquippedCombatFocus();
                if (combatFocus != null)
                    combatAbility = combatFocus.GetCombatAbility();

                // COMBAT ABILITY - Iron Fist: 20% increased chance to expose enemy weaknesses
                if (combatAbility == CombatAbility.IronFist)
                    avoidChance -= 0.2f;

                var roll = ThreadSafeRandom.Next(0.0f, 1.0f);
                if (avoidChance > roll)
                {
                    Session.Network.EnqueueSend(new GameMessageSystemChat($"{target.Name}'s deception prevents you from exposing a weakness!", ChatMessageType.Broadcast));

                    if (targetAsPlayer != null)
                    {
                        Proficiency.OnSuccessUse(targetAsPlayer, target.GetCreatureSkill(Skill.Deception), attackerPerceptionSkill);
                        targetAsPlayer.Session.Network.EnqueueSend(new GameMessageSystemChat($"Your deception prevents {Name} from exposing a weakness!", ChatMessageType.Broadcast));
                    }
                    return;
                }
                else
                    Proficiency.OnSuccessUse(this, GetCreatureSkill(Skill.AssessCreature), targetDeceptionSkill);

                var magicYieldSpellLevels = SpellLevelProgression.GetSpellLevels(SpellId.MagicYieldOther1);
                var succumbSpellLevels = SpellLevelProgression.GetSpellLevels(SpellId.ExposeWeakness1);  // Succumb

                if (magicYieldSpellLevels.Count == 0 || succumbSpellLevels.Count == 0)
                    return;

                var overRoll = roll - avoidChance;
                int maxSpellLevel = (int)Math.Clamp(Math.Floor((double)attackerPerceptionSkill / 50), 1, 7);
                int spellLevel = (int)Math.Clamp(Math.Floor(overRoll * 10), 1, maxSpellLevel);

                var magicYieldSpell = new Spell(magicYieldSpellLevels[spellLevel - 1]);
                var succumbSpellLevel = new Spell(succumbSpellLevels[spellLevel - 1]);

                string spellTypePrefix;
                switch (spellLevel)
                {
                    case 1: spellTypePrefix = "a slight"; break;
                    default:
                    case 2: spellTypePrefix = "a minor"; break;
                    case 3: spellTypePrefix = "a moderate"; break;
                    case 4: spellTypePrefix = "a major"; break;
                    case 5: spellTypePrefix = "a severe"; break;
                    case 6: spellTypePrefix = "a crippling"; break;
                    case 7: spellTypePrefix = "a tremendous"; break;
                }

                Session.Network.EnqueueSend(new GameMessageSystemChat($"Your perception allows you to expose {spellTypePrefix} magical weakness on {target.Name}!", ChatMessageType.Broadcast));
                if (targetAsPlayer != null)
                    targetAsPlayer.Session.Network.EnqueueSend(new GameMessageSystemChat($"{Name}'s perception exposes {spellTypePrefix} magical weakness on you!", ChatMessageType.Broadcast));

                if (magicYieldSpell.NonComponentTargetType == ItemType.None)
                    TryCastSpell(magicYieldSpell, null, this, null, false, false, false, false);
                else
                    TryCastSpell(magicYieldSpell, target, this, null, false, false, false, false);

                if (GetCreatureSkill(Skill.AssessCreature).AdvancementClass == SkillAdvancementClass.Specialized)
                {
                    if (succumbSpellLevel.NonComponentTargetType == ItemType.None)
                        TryCastSpell(succumbSpellLevel, null, this, null, false, false, false, false);
                    else
                        TryCastSpell(succumbSpellLevel, target, this, null, false, false, false, false);
                }
            }
        }
    }
}
