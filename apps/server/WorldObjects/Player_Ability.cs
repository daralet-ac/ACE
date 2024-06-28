using ACE.Common;
using ACE.Database.Models.World;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Entity;
using ACE.Server.Factories;
using ACE.Server.Factories.Tables;
using ACE.Server.Network.GameAction.Actions;
using ACE.Server.Network.GameMessages.Messages;
using Org.BouncyCastle.Asn1.X509;
using System;
using Spell = ACE.Server.Entity.Spell;
using Time = ACE.Common.Time;

namespace ACE.Server.WorldObjects
{
    partial class Player
    {
        public double LastFocusedTaunt = 0;
        public double LastFeignWeakness = 0;
        public double LastProvokeActivated = 0;
        public double LastPhalanxActivated = 0;
        public double LastRecklessActivated = 0;
        public double LastParryActivated = 0;
        public double LastBackstabActivated = 0;
        public double LastSteadyShotActivated = 0;
        public double LastMultishotActivated = 0;
        public double LastSmokescreenActivated = 0;
        public double LastIronFistActivated = 0;
        public double LastOverloadActivated = 0;
        public double LastBatteryActivated = 0;
        public double LastReflectActivated = 0;
        public double LastEnchantedWeaponActivated = 0;

        public bool OverloadActivated = false;
        public bool OverloadDumped = false;
        public bool RecklessActivated = false;
        public bool RecklessDumped = false;

        public double MultishotActivatedDuration = 10;
        public double PhalanxActivatedDuration = 10;
        public double ParryActivatedDuration = 10;
        public double SteadyShotActivatedDuration = 10;
        public double SmokescreenActivatedDuration = 10;
        public double BackstabActivatedDuration = 10;
        public double IronFistActivatedDuration = 10;
        public double ProvokeActivatedDuration = 10;
        public double ReflectActivatedDuration = 10;
        public double BatteryActivatedDuration = 10;
        public double OverloadActivatedDuration = 10;
        public double EnchantedWeaponActivatedDuration = 10;
        public double RecklessActivatedDuration = 10;

        public CombatAbility EquippedCombatAbility
        {
            get
            {
                var combatFocus = GetEquippedCombatFocus();
                if (combatFocus != null)
                {
                    var combatAbility = combatFocus.GetCombatAbility();
                    return combatAbility;
                }
                else
                    return CombatAbility.None;

            }
        }
        
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
            var nearbyMonsters = GetNearbyMonsters(10);

            foreach (var target in nearbyMonsters)
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
            var nearbyMonsters = GetNearbyMonsters(15);

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

        public double LastVanishActivated = 0;

        public void TryUseVanish(WorldObject ability)
        {
            if (IsStealthed)
                return;
            var thieverySkill = GetCreatureSkill(Skill.Lockpick); // Thievery
            if (thieverySkill.AdvancementClass < SkillAdvancementClass.Trained)
                return;

            // the smoke is enough to fool monsters from far away?
            var nearbyMonsters = GetNearbyMonsters(15);

            var success = true;
            foreach (var target in nearbyMonsters)
            {
                // generous bonus to skill check to start 
                var skillCheck = SkillCheck.GetSkillChance(GetModdedThieverySkill() + 50, target.GetCreatureSkill(Skill.AssessCreature).Current);

                if (ThreadSafeRandom.Next(0.0f, 1.0f) > skillCheck)
                {
                    Session.Network.EnqueueSend(new GameMessageSystemChat($"Some thief you are! {target.Name} was not fooled by your attempt to vanish, and prevents you from entering stealth.", ChatMessageType.Broadcast));
                    success = false;
                    break;
                }

                var targetTier = target.Tier ?? 1;
                var staminaCost = -2 * Math.Clamp(targetTier, 1, 7);
                UpdateVitalDelta(Stamina, staminaCost);
            }
            if (success)
            {
                var smoke = WorldObjectFactory.CreateNewWorldObject(1051113);
                smoke.Location = Location;
                smoke.EnterWorld();

                this.LastVanishActivated = Time.GetUnixTime();
                this.BeginStealth();
            }
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

        public void TryUseActivated(WorldObject ability)
        {
           switch (EquippedCombatAbility)
            {
                case CombatAbility.Provoke:
                    LastProvokeActivated = Time.GetUnixTime();
                    PlayParticleEffect(PlayScript.SkillUpRed, this.Guid);
                    Session.Network.EnqueueSend(new GameMessageSystemChat($"Give them cause for provocation! For the next ten seconds, your damage is increased by an additional 20% and your threat generation by an additional 50%.", ChatMessageType.Broadcast));
                    break;

                case CombatAbility.Phalanx:
                        LastPhalanxActivated = Time.GetUnixTime();
                        PlayParticleEffect(PlayScript.ShieldUpGrey, this.Guid);
                        Session.Network.EnqueueSend(new GameMessageSystemChat($"Raise your shield! For the next ten seconds, your chance to block is increased, and applies to attacks from any angle.", ChatMessageType.Broadcast));
                    break;

                case CombatAbility.Fury:
                    RecklessActivated = true;
                    LastRecklessActivated = Time.GetUnixTime();
                    PlayParticleEffect(PlayScript.EnchantUpRed, this.Guid);
                    Session.Network.EnqueueSend(new GameMessageSystemChat($"You pour your rage into a mighty blow! The first attack you make within the next ten seconds will have increased damage and exhaust all of your Fury!", ChatMessageType.Broadcast));
                    break;

                case CombatAbility.Parry:
                    LastParryActivated = Time.GetUnixTime();
                    PlayParticleEffect(PlayScript.SkillUpYellow, this.Guid);
                    Session.Network.EnqueueSend(new GameMessageSystemChat($"En garde! For the next ten seconds, you will riposte any attack you parry, damaging your foe!", ChatMessageType.Broadcast));
                    break;

                case CombatAbility.Backstab:
                    LastBackstabActivated = Time.GetUnixTime();
                    PlayParticleEffect(PlayScript.EnchantDownGrey, this.Guid);
                    Session.Network.EnqueueSend(new GameMessageSystemChat($"You set aside what little mercy you possess. For the next ten seconds, your attacks from behind are utterly ruthless, and cannot be evaded!", ChatMessageType.Broadcast));
                    break;

                case CombatAbility.SteadyShot:
                    LastSteadyShotActivated = Time.GetUnixTime();
                    PlayParticleEffect(PlayScript.VisionUpWhite, this.Guid);
                    Session.Network.EnqueueSend(new GameMessageSystemChat($"For the next ten seconds, you summon all your powers of concentration, firing missiles of spectacular accuracy and damage!", ChatMessageType.Broadcast));
                    break;

                case CombatAbility.Multishot:
                    LastMultishotActivated = Time.GetUnixTime();
                    PlayParticleEffect(PlayScript.EnchantUpYellow, this.Guid);
                    Session.Network.EnqueueSend(new GameMessageSystemChat($"For the next ten seconds, you nock a third arrow, quarrel or dart, hitting an additional target!", ChatMessageType.Broadcast));
                    break;

                case CombatAbility.Smokescreen:
                    LastSmokescreenActivated = Time.GetUnixTime();
                    PlayParticleEffect(PlayScript.SkillDownBlack, this.Guid);
                    Session.Network.EnqueueSend(new GameMessageSystemChat($"For the next ten seconds, you shroud yourself in darkness, increasing your chance to evade by an additional 30%!", ChatMessageType.Broadcast));
                    break;

                case CombatAbility.IronFist:
                    LastIronFistActivated = Time.GetUnixTime();
                    Session.Network.EnqueueSend(new GameMessageSystemChat($"For the next ten seconds, you focus your strikes on your enemy's most vital points, increasing your critical chance by an additional 15%!", ChatMessageType.Broadcast));
                    break;

                case CombatAbility.Overload:
                    OverloadActivated = true;
                    LastOverloadActivated = Time.GetUnixTime();
                    PlayParticleEffect(PlayScript.EnchantUpBlue, this.Guid);
                    Session.Network.EnqueueSend(new GameMessageSystemChat($"You channel your accumulated energies! The first spell you cast within the next ten seconds will have increased potency and discharge all of your Overload!", ChatMessageType.Broadcast));
                    break;

                case CombatAbility.Battery:
                    LastBatteryActivated = Time.GetUnixTime();
                    PlayParticleEffect(PlayScript.RegenUpBlue, this.Guid);
                    Session.Network.EnqueueSend(new GameMessageSystemChat($"You summon an untapped reserve of willpower! For the next ten seconds, your spells are free to cast and suffer no effectiveness penalty!", ChatMessageType.Broadcast));
                    break;

                case CombatAbility.Reflect:
                    LastReflectActivated = Time.GetUnixTime();
                    PlayParticleEffect(PlayScript.SkillUpBlue, this.Guid);
                    Session.Network.EnqueueSend(new GameMessageSystemChat($"You harden your resolve to repel malign sorceries! For the next ten seconds, your chance to resist spells is increased by an additional 30%!", ChatMessageType.Broadcast));
                    break;

                case CombatAbility.EnchantedWeapon:
                    LastEnchantedWeaponActivated = Time.GetUnixTime();
                    PlayParticleEffect(PlayScript.EnchantUpPurple, this.Guid);
                    Session.Network.EnqueueSend(new GameMessageSystemChat($"You touch a rune carved into your weapon! For the next ten seconds, your proc spells are 25% more effective!", ChatMessageType.Broadcast));
                    break;

                default:
                    Session.Network.EnqueueSend(new GameMessageSystemChat($"You must equip an upgraded Combat Focus to activate an ability!", ChatMessageType.Broadcast));
                    break;
            }
            
        }

        public static int HandleRecklessStamps(Player playerAttacker)
        {
            var scaledStamps = Math.Round(playerAttacker.ScaleWithPowerAccuracyBar(20f));

            scaledStamps += 20f;

            var numStrikes = playerAttacker.GetNumStrikes(playerAttacker.AttackType);
            if (numStrikes == 2)
            {
                if (playerAttacker.GetEquippedWeapon().W_WeaponType == WeaponType.TwoHanded)
                    scaledStamps /= 2f;
                else
                    scaledStamps /= 1.5f;
            }

            if (playerAttacker.AttackType == AttackType.OffhandPunch || playerAttacker.AttackType == AttackType.Punch || playerAttacker.AttackType == AttackType.Punches)
                scaledStamps /= 1.25f;

            if (!playerAttacker.QuestManager.HasQuest($"{playerAttacker.Name},Reckless"))
            {
                playerAttacker.QuestManager.Stamp($"{playerAttacker.Name},Reckless");
                playerAttacker.QuestManager.Increment($"{playerAttacker.Name},Reckless", (int)scaledStamps);
            }
            else if (playerAttacker.QuestManager.GetCurrentSolves($"{playerAttacker.Name},Reckless") < 500)
                playerAttacker.QuestManager.Increment($"{playerAttacker.Name},Reckless", (int)scaledStamps);

            var stacks = playerAttacker.QuestManager.GetCurrentSolves($"{playerAttacker.Name},Reckless");

            return stacks;
        }

        public static int HandleOverloadStamps(Player sourcePlayer, int? spellTypeScaler, uint spellLevel)
        {
            var baseStamps = 50;
            var playerLevel = (int)(sourcePlayer.Level / 10);

            if (playerLevel > 7)
                playerLevel = 7;

            if (spellLevel == 1)
            {
                if (playerLevel > 2)
                    baseStamps /= playerLevel;
            }
            if (spellLevel == 2)
            {
                baseStamps = (int)(baseStamps * 1.5);

                if (playerLevel > 3)
                    baseStamps /= playerLevel;
            }
            if (spellLevel == 3)
            {
                baseStamps = (int)(baseStamps * 2);

                if (playerLevel > 4)
                    baseStamps /= playerLevel;
            }
            if (spellLevel == 4)
            {
                baseStamps = (int)(baseStamps * 2.25);

                if (playerLevel > 5)
                    baseStamps /= playerLevel;
            }
            if (spellLevel == 5)
            {
                baseStamps = (int)(baseStamps * 2.5);

                if (playerLevel > 6)
                    baseStamps /= playerLevel;
            }
            if (spellLevel == 6)
            {
                baseStamps = (int)(baseStamps * 2.5);

                if (playerLevel > 7)
                    baseStamps /= playerLevel;
            }
            if (spellLevel == 7)
                baseStamps = (int)(baseStamps * 2.5);

            if (spellTypeScaler != null)
                baseStamps = baseStamps/(int)spellTypeScaler;

            if (!sourcePlayer.QuestManager.HasQuest($"{sourcePlayer.Name},Overload"))
            {
                sourcePlayer.QuestManager.Stamp($"{sourcePlayer.Name},Overload");
                sourcePlayer.QuestManager.Increment($"{sourcePlayer.Name},Overload", (int)baseStamps);                    
            }
            else if (sourcePlayer.QuestManager.GetCurrentSolves($"{sourcePlayer.Name},Overload") < 500)
                sourcePlayer.QuestManager.Increment($"{sourcePlayer.Name},Overload", (int)baseStamps);

            var stacks = sourcePlayer.QuestManager.GetCurrentSolves($"{sourcePlayer.Name},Overload");
            if (stacks > 250)
            { 
                var overloadChance = 0.075f * (stacks - 250) / 250;
                if (overloadChance > ThreadSafeRandom.Next(0f, 1f))
                {
                    var damage = sourcePlayer.Health.MaxValue / 10;
                    sourcePlayer.UpdateVitalDelta(sourcePlayer.Health, -(int)damage);
                    sourcePlayer.Session.Network.EnqueueSend(new GameMessageSystemChat($"Overloaded! You lose control of the energies flowing through you, suffering {damage} points of damage!", ChatMessageType.Magic));
                    sourcePlayer.PlayParticleEffect(PlayScript.Fizzle, sourcePlayer.Guid);
                    sourcePlayer.DamageHistory.Add(sourcePlayer, DamageType.Health, (uint)-damage);
                    if (sourcePlayer.IsDead)
                    {
                        var lastDamager = sourcePlayer.DamageHistory.LastDamager;
                        sourcePlayer.OnDeath(lastDamager, DamageType.Health, false);
                        sourcePlayer.Die();
                    }

                }
            }

            // Max Stacks = 500, so a percentage is stacks / 5
            var overloadPercent = sourcePlayer.QuestManager.GetCurrentSolves($"{sourcePlayer.Name},Overload") / 5;
            if (overloadPercent > 100)
                overloadPercent = 100;
            if (overloadPercent < 0)
                overloadPercent = 0;

            return overloadPercent;
        }
    }
}
