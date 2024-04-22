using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Entity.Models;
using ACE.Server.Factories.Tables;
using ACE.Server.WorldObjects.Entity;
using System;

namespace ACE.Server.WorldObjects
{
    partial class Creature
    {
        private static bool DebugArchetypeSystem = false;

        // Stat ranges by tier
        private static readonly int[] enemyHealth = { 10, 50, 150, 350, 500, 800, 1200, 1600, 2000};
        private static readonly int[] enemyStaminaMana = { 20, 100, 150, 225, 325, 450, 650, 950, 1250 };
        private static readonly int[] enemyHealthRegen = { 1, 2, 5, 10, 15, 20, 25, 30, 50 };
        private static readonly int[] enemyStaminaManaRegen = { 1, 2, 5, 10, 15, 20, 25, 30, 50 };

        private static readonly int[] enemyArmorWard = { 10, 20, 45, 68, 101, 152, 228, 342, 513 };
        private static readonly int[] enemyAttack = { 10, 50, 150, 200, 250, 300, 400, 500, 1000};
        private static readonly int[] enemyDefense = { 10, 75, 200, 300, 400, 500, 600, 700, 1000 };
        private static readonly int[] enemyAssessDeception = { 10, 50, 100, 150, 200, 250, 300, 350, 400 };
        private static readonly int[] enemyRun = { 10, 100, 150, 200, 250, 300, 400, 500, 600 };

        private static readonly float[] enemyDamage = { 1.0f, 1.5f, 3.0f, 3.3f, 3.6f, 3.9f, 4.2f, 5.0f, 6.0f }; // percentage of player health to be taken per second after all stats (of player and enemy) are considered

        private static readonly int[] avgPlayerHealth = { 25, 45, 95, 125, 155, 185, 215, 245, 320 };
        private static readonly float[] avgPlayerArmorReduction = { 0.75f, 0.57f, 0.40f, 0.31f, 0.25f, 0.21f, 0.18f, 0.16f, 0.1f };
        private static readonly float[] avgPlayerLifeProtReduction = { 1.0f, 0.9f, 0.9f, 0.85f, 0.85f, 0.8f, 0.8f, 0.75f, 0.75f };
        private static readonly int[] avgPlayerMeleeDefense = { 10, 75, 150, 175, 200, 225, 275, 350, 500 };

        private void SetSkills(int tier, float statWeight, double toughness, double physicality, double dexterity, double magic, double intelligence)
        {
            if (DebugArchetypeSystem)
                Console.WriteLine($"\n\n---- {Name} ----  \n\n-- SetSkills() for {Name} ({WeenieClassId}) (statWeight: {statWeight}) --");

            if ((OverrideArchetypeSkills ?? false) != true)
            {
                // Martial Weapons Skill
                {
                    var newSkill = GetNewMeleeAttackSkill(tier, statWeight, physicality, dexterity);

                    var skillType = ACE.Entity.Enum.Skill.HeavyWeapons;

                    var propertiesSkill = new PropertiesSkill() { InitLevel = newSkill, SAC = ACE.Entity.Enum.SkillAdvancementClass.Trained };

                    Skills[ACE.Entity.Enum.Skill.HeavyWeapons] = new CreatureSkill(this, skillType, propertiesSkill);
                }

                // Unarmed Attack Skill
                {
                    var newSkill = GetNewUnarmedCombatSkill(tier, statWeight, physicality, dexterity);

                    var skillType = ACE.Entity.Enum.Skill.UnarmedCombat;

                    var propertiesSkill = new PropertiesSkill() { InitLevel = newSkill, SAC = ACE.Entity.Enum.SkillAdvancementClass.Trained };

                    Skills[ACE.Entity.Enum.Skill.UnarmedCombat] = new CreatureSkill(this, skillType, propertiesSkill);
                }

                // Dagger Skill
                {
                    var newSkill = GetNewDaggerSkill(tier, statWeight, physicality, dexterity);

                    var skillType = ACE.Entity.Enum.Skill.Dagger;

                    var propertiesSkill = new PropertiesSkill() { InitLevel = newSkill, SAC = ACE.Entity.Enum.SkillAdvancementClass.Trained };

                    Skills[ACE.Entity.Enum.Skill.Dagger] = new CreatureSkill(this, skillType, propertiesSkill);
                }

                // Staff Skill
                {
                    var newSkill = GetNewStaffSkill(tier, statWeight, physicality, dexterity);

                    var skillType = ACE.Entity.Enum.Skill.Staff;

                    var propertiesSkill = new PropertiesSkill() { InitLevel = newSkill, SAC = ACE.Entity.Enum.SkillAdvancementClass.Trained };

                    Skills[ACE.Entity.Enum.Skill.Staff] = new CreatureSkill(this, skillType, propertiesSkill);
                }

                // Missile Attack Skill
                {
                    var newSkill = GetNewMissileAttackSkill(tier, statWeight, dexterity);

                    var skillType = ACE.Entity.Enum.Skill.Bow;

                    var propertiesSkill = new PropertiesSkill() { InitLevel = newSkill, SAC = ACE.Entity.Enum.SkillAdvancementClass.Trained };

                    Skills[ACE.Entity.Enum.Skill.Bow] = new CreatureSkill(this, skillType, propertiesSkill);
                }

                // Thrown Weapons Skill
                {
                    var newSkill = GetNewMissileAttackSkill(tier, statWeight, dexterity);

                    var skillType = ACE.Entity.Enum.Skill.ThrownWeapon;

                    var propertiesSkill = new PropertiesSkill() { InitLevel = newSkill, SAC = ACE.Entity.Enum.SkillAdvancementClass.Trained };

                    Skills[ACE.Entity.Enum.Skill.ThrownWeapon] = new CreatureSkill(this, skillType, propertiesSkill);
                }

                // War Magic Skill
                {
                    var newSkill = GetNewWarMagicSkill(tier, statWeight, magic);

                    var skillType = ACE.Entity.Enum.Skill.WarMagic;

                    var propertiesSkill = new PropertiesSkill() { InitLevel = newSkill, SAC = ACE.Entity.Enum.SkillAdvancementClass.Trained };

                    Skills[ACE.Entity.Enum.Skill.WarMagic] = new CreatureSkill(this, skillType, propertiesSkill);
                }

                // Life Magic Skill
                {
                    var newSkill = GetNewLifeMagicSkill(tier, statWeight, magic);

                    var skillType = ACE.Entity.Enum.Skill.LifeMagic;

                    var propertiesSkill = new PropertiesSkill() { InitLevel = newSkill, SAC = ACE.Entity.Enum.SkillAdvancementClass.Trained };

                    Skills[ACE.Entity.Enum.Skill.LifeMagic] = new CreatureSkill(this, skillType, propertiesSkill);
                }

                // Physical Defense
                {
                    var newSkill = GetNewPhysicalDefenseSkill(tier, statWeight, toughness, (physicality + dexterity) / 2);

                    var skillType = ACE.Entity.Enum.Skill.MeleeDefense;

                    var propertiesSkill = new PropertiesSkill() { InitLevel = newSkill, SAC = ACE.Entity.Enum.SkillAdvancementClass.Specialized };

                    Skills[ACE.Entity.Enum.Skill.MeleeDefense] = new CreatureSkill(this, skillType, propertiesSkill);
                }

                // Missile Defense
                //{
                //    var newSkill = GetNewMissileDefenseSkill(tier, statWeight, toughness, dexterity);

                //    var skillType = ACE.Entity.Enum.Skill.MissileDefense;

                //    var propertiesSkill = new PropertiesSkill() { InitLevel = newSkill, SAC = ACE.Entity.Enum.SkillAdvancementClass.Specialized };

                //    Skills[ACE.Entity.Enum.Skill.MissileDefense] = new CreatureSkill(this, skillType, propertiesSkill);
                //}

                // Magic Defense
                {
                    var newSkill = GetNewMagicDefenseSkill(tier, statWeight, toughness, magic);

                    var skillType = ACE.Entity.Enum.Skill.MagicDefense;

                    var propertiesSkill = new PropertiesSkill() { InitLevel = newSkill, SAC = ACE.Entity.Enum.SkillAdvancementClass.Specialized };

                    Skills[ACE.Entity.Enum.Skill.MagicDefense] = new CreatureSkill(this, skillType, propertiesSkill);
                }

                // Perception
                {
                    var newSkill = GetNewPerceptionSkill(tier, statWeight, intelligence);

                    var skillType = ACE.Entity.Enum.Skill.AssessCreature;

                    var propertiesSkill = new PropertiesSkill() { InitLevel = newSkill, SAC = ACE.Entity.Enum.SkillAdvancementClass.Trained };

                    Skills[ACE.Entity.Enum.Skill.AssessCreature] = new CreatureSkill(this, skillType, propertiesSkill);
                }

                // Deception
                {
                    var newSkill = GetNewDeceptionSkill(tier, statWeight, intelligence);

                    var skillType = ACE.Entity.Enum.Skill.Deception;

                    var propertiesSkill = new PropertiesSkill() { InitLevel = newSkill, SAC = ACE.Entity.Enum.SkillAdvancementClass.Trained };

                    Skills[ACE.Entity.Enum.Skill.Deception] = new CreatureSkill(this, skillType, propertiesSkill);
                }

                // Run
                {
                    var newSkill = GetNewRunSkill(tier, statWeight, dexterity);

                    var skillType = ACE.Entity.Enum.Skill.Run;

                    var propertiesSkill = new PropertiesSkill() { InitLevel = newSkill, SAC = ACE.Entity.Enum.SkillAdvancementClass.Trained };

                    Skills[ACE.Entity.Enum.Skill.Run] = new CreatureSkill(this, skillType, propertiesSkill);
                }
            }
        }

        private void SetVitals(int tier, float statWeight, double toughness, double physicality, double dexterity, double magic)
        {

            if (DebugArchetypeSystem)
                Console.WriteLine($"\n-- SetVitals() for {Name} ({WeenieClassId}) (statWeight: {statWeight}) --");

            // Health
            {
                if ((OverrideArchetypeHealth ?? false) != true)
                {
                    var newVital = GetNewHealthLevel(tier, statWeight, toughness, physicality);

                    Vitals[PropertyAttribute2nd.MaxHealth].StartingValue = newVital;
                }
            }

            // Health Regen
            {
                if ((OverrideArchetypeHealth ?? false) != true)
                {
                    var newVital = GetNewHealthRegenLevel(tier, statWeight, toughness, physicality);

                    HealthRate = newVital;
                }
            }

            // Stamina
            {
                if ((OverrideArchetypeStamina ?? false) != true)
                {
                    var newVital = GetNewStaminaLevel(tier, statWeight, physicality, dexterity);

                    Vitals[PropertyAttribute2nd.MaxStamina].StartingValue = newVital;
                }
            }

            // Stamina Regen
            {
                if ((OverrideArchetypeStamina ?? false) != true)
                {
                    var newVital = GetNewStaminaRegenLevel(tier, statWeight, physicality, dexterity);

                    StaminaRate = newVital;
                }
            }

            // Mana
            {
                if ((OverrideArchetypeMana ?? false) != true)
                {
                    var newVital = GetNewManaLevel(tier, statWeight, magic);

                    Vitals[PropertyAttribute2nd.MaxMana].StartingValue = newVital;
                }
            }

            // Mana Regen
            {
                if ((OverrideArchetypeMana ?? false) != true)
                {
                    var newVital = GetNewManaRegenLevel(tier, statWeight, magic);

                    ManaRate = newVital;
                }
            }
        }

        private void SetDamageArmorWard(int tier, float statWeight, double toughness, double physicality, double magic, double lethality)
        {
            if (DebugArchetypeSystem)
                Console.WriteLine($"\n-- SetDamageArmorAegus() for {Name} ({WeenieClassId}) (statWeight: {statWeight}) --");

            // Damage + Armor
            {
                // Armor Level
                var newArmorLevel = GetNewArmorLevel(tier, statWeight, toughness, physicality);

                // Damage Calculation
                var baseAvgDamage = GetNewBaseDamage(tier, statWeight, lethality);

                var damageHigh = Math.Max((int)(baseAvgDamage / (1.0f - (0.75f / 2))), 1);
                var damageMed = Math.Max((int)(baseAvgDamage / (1.0f - (0.5f / 2))), 1);
                var damageLow = Math.Max((int)(baseAvgDamage / (1.0f - (0.3f / 2))), 1);

                if (DebugArchetypeSystem)
                    Console.WriteLine($"Final: Damage(0.75): {damageHigh}, Damage(0.5): {damageMed}, Damage(0.3): {damageLow}\n" +
                        $"-Update Armor/Damage Body Values");

                // Set Body Parts
                if (Weenie != null)
                {
                    var bodyParts = GetBodyParts(this);
                    if (bodyParts.Weenie.PropertiesBodyPart != null)
                    {
                        foreach (var kvp in bodyParts.Weenie.PropertiesBodyPart)
                        {
                            var bodyPart = kvp.Value;

                            // Armor
                            if (DebugArchetypeSystem)
                                Console.WriteLine($" Old/New Armor: {bodyPart.BaseArmor} -> {newArmorLevel}");

                            bodyPart.BaseArmor = newArmorLevel;

                            // Damage
                            if (bodyPart.DVal > 0)
                            {
                                var oldDamage = bodyPart.DVal;

                                if (bodyPart.DVar == 0.75 || bodyPart.DVar == 0.9)
                                    bodyPart.DVal = damageHigh;
                                else if (bodyPart.DVar == 0.5)
                                    bodyPart.DVal = damageMed;
                                else
                                    bodyPart.DVal = damageLow;

                                if (DebugArchetypeSystem)
                                    Console.WriteLine($" Old/New Damage: {oldDamage} -> {bodyPart.DVal}");
                            }
                        }
                    }
                    else
                        _log.Warning("Creature_ArchetypeSystem.SetDamageArmorWard() - Unable to set damage/armor on bodyparts of {Name}. Body parts table may be missing from this wcid.", Name);
                }
                else
                    _log.Warning("Creature.SetDamageArmorWard() - Weenie == null for {Name} ({WeenieClassId}). Cannot set Damage/Armor for this creature.", Name, WeenieClassId);
            }

            // Ward
            {
                var tweakedWard = GetNewWardLevel(tier, statWeight, toughness, magic);

                WardLevel = tweakedWard;
            }
        }

        private void SetXp(double difficltyMod)
        {
            if ((OverrideArchetypeXp ?? false) != true)
            { 
                var killXpMod = KillXpMod ?? 1.0f;

                KillXpMod = killXpMod * difficltyMod;

                if (DebugArchetypeSystem)
                    Console.Write($"\n-- SetXpMod() for {Name} ({WeenieClassId}) --\n" +
                    $" Old/New Xp mod: {killXpMod} -> {KillXpMod.Value}");
            }
        }


        // HELPER FUNCTIONS

        private uint GetNewMeleeAttackSkill(int tier, float statWeight, double physicality, double dexterity)
        {
            var target = enemyAttack[tier] + (enemyAttack[tier + 1] - enemyAttack[tier]) * statWeight;
            var multiplier = (physicality + dexterity) / 2;
            var tweakedSkill = (uint)(target * multiplier);

            var divisor = 2;
            var skillFromAttributes = Strength.Base / divisor;

            if (skillFromAttributes > tweakedSkill)
            {
                var diff = (int)tweakedSkill - skillFromAttributes;
                var attributeAdjustment = diff * divisor;
                _log.Warning("Creature.SetSkills() - Archetype system is attempting to set the base Martial Weapons skill to {TweakedSkill} for {Name} ({WeenieClassId}) (defaulting to 1). Strength attribute should be lowered by {AttributeAdjustment}.", diff, Name, WeenieClassId, attributeAdjustment);
            }

            var newSkill = tweakedSkill - (uint)skillFromAttributes;

            if (DebugArchetypeSystem)
                Console.Write($"-Melee Attack\n" +
                $" Target: {target}\n" +
                $" Multiplier: {multiplier}\n" +
                $" TweakedSkill: {tweakedSkill}\n" +
                $" SkillFromAttributes: {skillFromAttributes}\n" +
                $" NewSkill: {newSkill}");

            return newSkill;
        }

        private uint GetNewUnarmedCombatSkill(int tier, float statWeight, double physicality, double dexterity)
        {
            var target = enemyAttack[tier] + (enemyAttack[tier + 1] - enemyAttack[tier]) * statWeight;
            var multiplier = (physicality + dexterity) / 2;
            var tweakedSkill = (uint)(target * multiplier);

            var divisor = 2;
            var skillFromAttributes = Coordination.Base / divisor;

            if (skillFromAttributes > tweakedSkill)
            {
                var diff = (int)tweakedSkill - skillFromAttributes;
                var attributeAdjustment = diff * divisor;
                _log.Warning("Creature.SetSkills() - Archetype system is attempting to set the base Unarmed Combat skill to {TweakedSkill} for {Name} ({WeenieClassId}) (defaulting to 1). Coordination attribute should be lowered by {AttributeAdjustment}.", diff, Name, WeenieClassId, attributeAdjustment);
            }

            var newSkill = tweakedSkill - (uint)skillFromAttributes;

            if (DebugArchetypeSystem)
                Console.Write($"-Unarmed Combat\n" +
                $" Target: {target}\n" +
                $" Multiplier: {multiplier}\n" +
                $" TweakedSkill: {tweakedSkill}\n" +
                $" SkillFromAttributes: {skillFromAttributes}\n" +
                $" NewSkill: {newSkill}");

            return newSkill;
        }

        private uint GetNewDaggerSkill(int tier, float statWeight, double physicality, double dexterity)
        {
            var target = enemyAttack[tier] + (enemyAttack[tier + 1] - enemyAttack[tier]) * statWeight;
            var multiplier = (physicality + dexterity) / 2;
            var tweakedSkill = (uint)(target * multiplier);

            var divisor = 2;
            var skillFromAttributes = Coordination.Base / divisor;

            if (skillFromAttributes > tweakedSkill)
            {
                var diff = (int)tweakedSkill - skillFromAttributes;
                var attributeAdjustment = diff * divisor;
                _log.Warning("Creature.SetSkills() - Archetype system is attempting to set the base Dagger skill to {TweakedSkill} for {Name} ({WeenieClassId}) (defaulting to 1). Coordination attribute should be lowered by {AttributeAdjustment}.", diff, Name, WeenieClassId, attributeAdjustment);
            }

            var newSkill = tweakedSkill - (uint)skillFromAttributes;

            if (DebugArchetypeSystem)
                Console.Write($"-Dagger\n" +
                $" Target: {target}\n" +
                $" Multiplier: {multiplier}\n" +
                $" TweakedSkill: {tweakedSkill}\n" +
                $" SkillFromAttributes: {skillFromAttributes}\n" +
                $" NewSkill: {newSkill}");

            return newSkill;
        }

        private uint GetNewStaffSkill(int tier, float statWeight, double physicality, double dexterity)
        {
            var target = enemyAttack[tier] + (enemyAttack[tier + 1] - enemyAttack[tier]) * statWeight;
            var multiplier = (physicality + dexterity) / 2;
            var tweakedSkill = (uint)(target * multiplier);

            var divisor = 2;
            var skillFromAttributes = Coordination.Base / divisor;

            if (skillFromAttributes > tweakedSkill)
            {
                var diff = (int)tweakedSkill - skillFromAttributes;
                var attributeAdjustment = diff * divisor;
                _log.Warning("Creature.SetSkills() - Archetype system is attempting to set the base Staff skill to {TweakedSkill} for {Name} ({WeenieClassId}) (defaulting to 1). Coordination attribute should be lowered by {AttributeAdjustment}.", diff, Name, WeenieClassId, attributeAdjustment);
            }

            var newSkill = tweakedSkill - (uint)skillFromAttributes;

            if (DebugArchetypeSystem)
                Console.Write($"-Staff\n" +
                $" Target: {target}\n" +
                $" Multiplier: {multiplier}\n" +
                $" TweakedSkill: {tweakedSkill}\n" +
                $" SkillFromAttributes: {skillFromAttributes}\n" +
                $" NewSkill: {newSkill}");

            return newSkill;
        }

        private uint GetNewMissileAttackSkill(int tier, float statWeight, double dexterity)
        {
            var target = enemyAttack[tier] + (enemyAttack[tier + 1] - enemyAttack[tier]) * statWeight;
            var multiplier = dexterity;
            var tweakedSkill = (uint)(target * multiplier);

            var divisor = 2;
            var skillFromAttributes = Coordination.Base / divisor;

            if (skillFromAttributes > tweakedSkill)
            {
                var diff = (int)tweakedSkill - skillFromAttributes;
                var attributeAdjustment = diff * divisor;
                _log.Warning("Creature.SetSkills() - Archetype system is attempting to set the base Bow skill to {TweakedSkill} for {Name} ({WeenieClassId}) (defaulting to 1). Coordination attribute should be lowered by {AttributeAdjustment}.", diff, Name, WeenieClassId, attributeAdjustment);
            }

            var newSkill = tweakedSkill - (uint)skillFromAttributes;

            if (DebugArchetypeSystem)
                Console.Write($"\n-Missile Attack\n" +
                $" Target: {target}\n" +
                $" Multiplier: {multiplier}\n" +
                $" TweakedSkill: {tweakedSkill}\n" +
                $" SkillFromAttributes: {skillFromAttributes}\n" +
                $" NewSkill: {newSkill}");

            return newSkill;
        }

        private uint GetNewThrownWeaponsSkill(int tier, float statWeight, double dexterity)
        {
            var target = enemyAttack[tier] + (enemyAttack[tier + 1] - enemyAttack[tier]) * statWeight;
            var multiplier = dexterity;
            var tweakedSkill = (uint)(target * multiplier);

            var divisor = 2;
            var skillFromAttributes = Strength.Base / divisor;

            if (skillFromAttributes > tweakedSkill)
            {
                var diff = (int)tweakedSkill - skillFromAttributes;
                var attributeAdjustment = diff * divisor;
                _log.Warning("Creature.SetSkills() - Archetype system is attempting to set the base Thrown Weapons skill to {TweakedSkill} for {Name} ({WeenieClassId}) (defaulting to 1). Coordination attribute should be lowered by {AttributeAdjustment}.", diff, Name, WeenieClassId, attributeAdjustment);
            }

            var newSkill = tweakedSkill - (uint)skillFromAttributes;

            if (DebugArchetypeSystem)
                Console.Write($"\n-Thrown Weapons\n" +
                $" Target: {target}\n" +
                $" Multiplier: {multiplier}\n" +
                $" TweakedSkill: {tweakedSkill}\n" +
                $" SkillFromAttributes: {skillFromAttributes}\n" +
                $" NewSkill: {newSkill}");

            return newSkill;
        }

        private uint GetNewWarMagicSkill(int tier, float statWeight, double magic)
        {
            var target = enemyAttack[tier] + (enemyAttack[tier + 1] - enemyAttack[tier]) * statWeight;
            var multiplier = magic;
            var tweakedSkill = (uint)(target * multiplier);

            var divisor = 2;
            var skillFromAttributes = Self.Base / divisor;

            if (skillFromAttributes > tweakedSkill)
            {
                var diff = (int)tweakedSkill - skillFromAttributes;
                var attributeAdjustment = diff * divisor;
                _log.Warning("Creature.SetSkills() - Archetype system is attempting to set the base War Magic skill to {TweakedSkill} for {Name} ({WeenieClassId}) (defaulting to 1). Self attribute should be lowered by {AttributeAdjustment}.", diff, Name, WeenieClassId, attributeAdjustment);
            }

            var newSkill = tweakedSkill - (uint)skillFromAttributes;

            if (DebugArchetypeSystem)
                Console.Write($"\n-War Magic\n" +
                $" Target: {target}\n" +
                $" Multiplier: {multiplier}\n" +
                $" TweakedSkill: {tweakedSkill}\n" +
                $" SkillFromAttributes: {skillFromAttributes}\n" +
                $" NewSkill: {newSkill}");

            return newSkill;
        }

        private uint GetNewLifeMagicSkill(int tier, float statWeight, double magic)
        {
            var target = enemyAttack[tier] + (enemyAttack[tier + 1] - enemyAttack[tier]) * statWeight;
            var multiplier = magic;
            var tweakedSkill = (uint)(target * multiplier);

            var divisor = 2;
            var skillFromAttributes = Self.Base / divisor;

            if (skillFromAttributes > tweakedSkill)
            {
                var diff = (int)tweakedSkill - skillFromAttributes;
                var attributeAdjustment = diff * divisor;
                _log.Warning("Creature.SetSkills() - Archetype system is attempting to set the base War Magic skill to {TweakedSkill} for {Name} ({WeenieClassId}) (defaulting to 1). Self attribute should be lowered by {AttributeAdjustment}.", diff, Name, WeenieClassId, attributeAdjustment);
            }

            var newSkill = tweakedSkill - (uint)skillFromAttributes;

            if (DebugArchetypeSystem)
                Console.Write($"\n-Life Magic\n" +
                $" Target: {target}\n" +
                $" Multiplier: {multiplier}\n" +
                $" TweakedSkill: {tweakedSkill}\n" +
                $" SkillFromAttributes: {skillFromAttributes}\n" +
                $" NewSkill: {newSkill}");

            return newSkill;
        }

        private uint GetNewPhysicalDefenseSkill(int tier, float statWeight, double toughness, double physicalityDexterity)
        {
            var target = enemyDefense[tier] + (enemyDefense[tier + 1] - enemyDefense[tier]) * statWeight;
            var multiplier = (toughness + physicalityDexterity) / 2;
            var tweakedSkill = (uint)(target * multiplier);

            var divisor = 4;
            var skillFromAttributes = (Coordination.Base + Quickness.Base) / divisor;

            if (skillFromAttributes > tweakedSkill)
            {
                var diff = (int)tweakedSkill - skillFromAttributes;
                var attributeAdjustment = diff * divisor;
                _log.Warning("Creature.SetSkills() - Archetype system is attempting to set the base Physical Defense skill to {TweakedSkill} for {Name} ({WeenieClassId}) (defaulting to 1). Coordination and/or Quickness attributes should be lowered by a total of {AttributeAdjustment}.", tweakedSkill, Name, WeenieClassId, attributeAdjustment);
            }

            var newSkill = tweakedSkill - (uint)skillFromAttributes;

            if (DebugArchetypeSystem)
                    Console.Write($"\n-Physical Defense\n" +
                    $" Target: {target}\n" +
                    $" Multiplier: {multiplier}\n" +
                    $" TweakedSkill: {tweakedSkill}\n" +
                    $" SkillFromAttributes: {skillFromAttributes}\n" +
                    $" NewSkill: {newSkill}");

            return newSkill;
        }

        private uint GetNewMagicDefenseSkill(int tier, float statWeight, double toughness, double magic)
        {
            var target = enemyDefense[tier] + (enemyDefense[tier + 1] - enemyDefense[tier]) * statWeight;
            var multiplier = (toughness + magic) / 2;
            var tweakedSkill = (uint)(target * multiplier);

            var divisor = 4;
            var skillFromAttributes = (Focus.Base + Self.Base) / divisor;

            if (skillFromAttributes > tweakedSkill)
            {
                var diff = (int)tweakedSkill - skillFromAttributes;
                var attributeAdjustment = diff * divisor;
                _log.Warning("Creature.SetSkills() - Archetype system is attempting to set the base Magic Defense skill to {TweakedSkill} for {Name} ({WeenieClassId}) (defaulting to 1). Coordination and/or Quickness attributes should be lowered by a total of {AttributeAdjustment}.", tweakedSkill, Name, WeenieClassId, attributeAdjustment);
            }

            var newSkill = tweakedSkill - (uint)skillFromAttributes;

            if (DebugArchetypeSystem)
                Console.Write($"\n-Magic Defense\n" +
                $" Target: {target}\n" +
                $" Multiplier: {multiplier}\n" +
                $" TweakedSkill: {tweakedSkill}\n" +
                $" SkillFromAttributes: {skillFromAttributes}\n" +
                $" NewSkill: {newSkill}");

            return newSkill;
        }

        private uint GetNewPerceptionSkill(int tier, float statWeight, double intelligence)
        {
            var target = enemyAssessDeception[tier] + (enemyAssessDeception[tier + 1] - enemyAssessDeception[tier]) * statWeight;
            var multiplier = intelligence;
            var tweakedSkill = (uint)(target * multiplier);

            var divisor = 2;
            var skillFromAttributes = Focus.Base / divisor;

            if (skillFromAttributes > tweakedSkill)
            {
                var diff = (int)tweakedSkill - skillFromAttributes;
                var attributeAdjustment = diff * divisor;
                _log.Warning("Creature.SetSkills() - Archetype system is attempting to set the base Perception skill to {TweakedSkill} for {Name} ({WeenieClassId}) (defaulting to 1). Focus attribute should be lowered by {AttributeAdjustment}.", diff, Name, WeenieClassId, attributeAdjustment);
            }

            var newSkill = tweakedSkill - (uint)skillFromAttributes;

            if (DebugArchetypeSystem)
                Console.Write($"\n-Perception\n" +
                $" Target: {target}\n" +
                $" Multiplier: {multiplier}\n" +
                $" TweakedSkill: {tweakedSkill}\n" +
                $" SkillFromAttributes: {skillFromAttributes}\n" +
                $" NewSkill: {newSkill}");

            return newSkill;
        }

        private uint GetNewDeceptionSkill(int tier, float statWeight, double intelligence)
        {
            var target = enemyAssessDeception[tier] + (enemyAssessDeception[tier + 1] - enemyAssessDeception[tier]) * statWeight;
            var multiplier = intelligence;
            var tweakedSkill = (uint)(target * multiplier);

            var divisor = 2;
            var skillFromAttributes = Focus.Base / divisor;

            if (skillFromAttributes > tweakedSkill)
            {
                var diff = (int)tweakedSkill - skillFromAttributes;
                var attributeAdjustment = diff * divisor;
                _log.Warning("Creature.SetSkills() - Archetype system is attempting to set the base Deception skill to {TweakedSkill} for {Name} ({WeenieClassId}) (defaulting to 1). Focus attribute should be lowered by {AttributeAdjustment}.", diff, Name, WeenieClassId, attributeAdjustment);
            }

            var newSkill = tweakedSkill - (uint)skillFromAttributes;

            if (DebugArchetypeSystem)
                Console.Write($"\n-Deception\n" +
                $" Target: {target}\n" +
                $" Multiplier: {multiplier}\n" +
                $" TweakedSkill: {tweakedSkill}\n" +
                $" SkillFromAttributes: {skillFromAttributes}\n" +
                $" NewSkill: {newSkill}");

            return newSkill;
        }

        private uint GetNewRunSkill(int tier, float statWeight, double dexterity)
        {
            var target = enemyRun[tier] + (enemyRun[tier + 1] - enemyRun[tier]) * statWeight;
            var multiplier = dexterity;
            var tweakedSkill = (uint)(target * multiplier);

            var divisor = 1;
            var skillFromAttributes = Quickness.Base / divisor;

            if (skillFromAttributes > tweakedSkill)
            {
                var diff = (int)tweakedSkill - skillFromAttributes;
                var attributeAdjustment = diff * divisor;
                _log.Warning("Creature.SetSkills() - Archetype system is attempting to set the base Run skill to {TweakedSkill} for {Name} ({WeenieClassId}) (defaulting to 1). Quickness attribute should be lowered by {AttributeAdjustment}.", diff, Name, WeenieClassId, attributeAdjustment);
            }

            var newSkill = tweakedSkill - (uint)skillFromAttributes;

            if (DebugArchetypeSystem)
                Console.Write($"\n-Run\n" +
                $" Target: {target}\n" +
                $" Multiplier: {multiplier}\n" +
                $" TweakedSkill: {tweakedSkill}\n" +
                $" SkillFromAttributes: {skillFromAttributes}\n" +
                $" NewSkill: {newSkill}");

            return newSkill;
        }


        private int GetNewArmorLevel(int tier, float statWeight, double toughness, double physicality)
        {
            var target = enemyArmorWard[tier] + (enemyArmorWard[tier + 1] - enemyArmorWard[tier]) * statWeight;
            var multiplier = (toughness + physicality) / 2;
            var newArmor = (int)(target * multiplier);

            if (DebugArchetypeSystem)
                Console.Write($"\n-Armor\n" +
                $" Target: {target}\n" +
                $" Multiplier: {multiplier}\n" +
                $" TweakedSkill: {newArmor}");

            return newArmor;
        }

        private int GetNewWardLevel(int tier, float statWeight, double toughness, double magic)
        {
            var target = enemyArmorWard[tier] + (enemyArmorWard[tier + 1] - enemyArmorWard[tier]) * statWeight;
            var multiplier = (toughness + magic) / 2;
            var newWard = (int)(target * multiplier);

            if (DebugArchetypeSystem)
                Console.Write($"\n-Ward\n" +
                $" Target: {target}\n" +
                $" Multiplier: {multiplier}\n" +
                $" TweakedSkill: {newWard}");

            return newWard;
        }

        private uint GetNewHealthLevel(int tier, float statWeight, double toughness, double physicality)
        {
            var target = enemyHealth[tier] + (enemyHealth[tier + 1] - enemyHealth[tier]) * statWeight;
            var multiplier = (toughness + physicality) / 2;
            var tweakedVital = (uint)(target * multiplier);

            var divisor = 2;
            var vitalFromAttributes = Endurance.Base / divisor;

            if (vitalFromAttributes > tweakedVital)
            {
                var diff = (int)tweakedVital - vitalFromAttributes;
                var attributeAdjustment = diff * divisor;
                _log.Warning("Creature.SetSkills() - Archetype system is attempting to set the base Health skill to {TweakedSkill} for {Name} ({WeenieClassId}) (defaulting to 1). Endurance attribute should be lowered by {AttributeAdjustment}.", diff, Name, WeenieClassId, attributeAdjustment);
            }

            var newVital = tweakedVital - (uint)vitalFromAttributes;

            if (DebugArchetypeSystem)
                Console.Write($"\n-Health\n" +
                $" Target: {target}\n" +
                $" Multiplier: {multiplier}\n" +
                $" TweakedSkill: {tweakedVital}\n" +
                $" SkillFromAttributes: {vitalFromAttributes}\n" +
                $" NewSkill: {newVital}");

            return newVital;
        }

        private double GetNewHealthRegenLevel(int tier, float statWeight, double toughness, double physicality)
        {
            var target = enemyHealthRegen[tier] + (enemyHealthRegen[tier + 1] - enemyHealthRegen[tier]) * statWeight;
            var multiplier = (toughness + physicality) / 2;
            var newVital = (target * multiplier);

            if (DebugArchetypeSystem)
                Console.Write($"\n-Health Regen\n" +
                $" Target: {target}\n" +
                $" Multiplier: {multiplier}\n" +
                $" TweakedSkill: {newVital}");

            return newVital;
        }

        private uint GetNewStaminaLevel(int tier, float statWeight, double physicality, double dexterity)
        {
            var target = enemyStaminaMana[tier] + (enemyStaminaMana[tier + 1] - enemyStaminaMana[tier]) * statWeight;
            var multiplier = (physicality + dexterity) / 2;
            var tweakedVital = (uint)(target * multiplier);

            var divisor = 1;
            var vitalFromAttributes = Endurance.Base / divisor;

            if (vitalFromAttributes > tweakedVital)
            {
                var diff = (int)tweakedVital - vitalFromAttributes;
                var attributeAdjustment = diff * divisor;
                _log.Warning("Creature.SetSkills() - Archetype system is attempting to set the base Stamina skill to {TweakedSkill} for {Name} ({WeenieClassId}) (defaulting to 1). Endurance attribute should be lowered by {AttributeAdjustment}.", diff, Name, WeenieClassId, attributeAdjustment);
            }

            var newVital = tweakedVital - (uint)vitalFromAttributes;

            if (DebugArchetypeSystem)
                Console.Write($"\n-Stamina\n" +
                $" Target: {target}\n" +
                $" Multiplier: {multiplier}\n" +
                $" TweakedSkill: {tweakedVital}\n" +
                $" SkillFromAttributes: {vitalFromAttributes}\n" +
                $" NewSkill: {newVital}");

            return newVital;
        }

        private double GetNewStaminaRegenLevel(int tier, float statWeight, double physicality, double dexterity)
        {
            var target = enemyStaminaManaRegen[tier] + (enemyStaminaManaRegen[tier + 1] - enemyStaminaManaRegen[tier]) * statWeight;
            var multiplier = (physicality + dexterity) / 2;
            var newVital = (target * multiplier);

            if (DebugArchetypeSystem)
                Console.Write($"\n-Stamina Regen\n" +
                $" Target: {target}\n" +
                $" Multiplier: {multiplier}\n" +
                $" TweakedSkill: {newVital}");

            return newVital;
        }

        private uint GetNewManaLevel(int tier, float statWeight, double magic)
        {
            var target = enemyStaminaMana[tier] + (enemyStaminaMana[tier + 1] - enemyStaminaMana[tier]) * statWeight;
            var multiplier = magic;
            var tweakedVital = (uint)(target * multiplier);

            var divisor = 1;
            var vitalFromAttributes = Self.Base / divisor;

            if (vitalFromAttributes > tweakedVital)
            {
                var diff = (int)tweakedVital - vitalFromAttributes;
                var attributeAdjustment = diff * divisor;
                _log.Warning("Creature.SetSkills() - Archetype system is attempting to set the base Mana skill to {TweakedSkill} for {Name} ({WeenieClassId}) (defaulting to 1). Self attribute should be lowered by {AttributeAdjustment}.", diff, Name, WeenieClassId, attributeAdjustment);
            }

            var newVital = tweakedVital - (uint)vitalFromAttributes;

            if (DebugArchetypeSystem)
                Console.Write($"\n-Mana\n" +
                $" Target: {target}\n" +
                $" Multiplier: {multiplier}\n" +
                $" TweakedSkill: {tweakedVital}\n" +
                $" SkillFromAttributes: {vitalFromAttributes}\n" +
                $" NewSkill: {newVital}");

            return newVital;
        }

        private double GetNewManaRegenLevel(int tier, float statWeight, double magic)
        {
            var target = enemyStaminaManaRegen[tier] + (enemyStaminaManaRegen[tier + 1] - enemyStaminaManaRegen[tier]) * statWeight;
            var multiplier = magic;
            var newVital = (target * multiplier);

            if (DebugArchetypeSystem)
                Console.Write($"\n-Mana Regen\n" +
                $" Target: {target}\n" +
                $" Multiplier: {multiplier}\n" +
                $" TweakedSkill: {newVital}");

            return newVital;
        }

        private double GetNewBaseDamage(int tier, float statWeight, double lethality)
        {
            // target enemy damage per second
            var weightedEnemyDamage = (enemyDamage[tier] + (enemyDamage[tier + 1] - enemyDamage[tier]) * statWeight);
            var weightedPlayerHealth = (avgPlayerHealth[tier] + (avgPlayerHealth[tier + 1] - avgPlayerHealth[tier]) * statWeight);
            var targetEnemyDamage = weightedEnemyDamage / 100 * weightedPlayerHealth;

            // player melee def
            var weightedPlayerMeleeDef = avgPlayerMeleeDefense[tier] + (avgPlayerMeleeDefense[tier + 1] - avgPlayerMeleeDefense[tier]) * statWeight;
            var enemyAttackSkill = Skills[Skill.HeavyWeapons].Current;

            // player evade
            var playerEvadeChance = 1 - SkillCheck.GetSkillChance((int)enemyAttackSkill, (int)weightedPlayerMeleeDef);
            var playerEvadeDamageReduction = 1 - (playerEvadeChance * 0.67f);

            // player armor
            var weightedPlayerArmorReduction = avgPlayerArmorReduction[tier] + (avgPlayerArmorReduction[tier + 1] - avgPlayerArmorReduction[tier]) * statWeight;

            //player life protection
            var playerLifeProtReduction = avgPlayerLifeProtReduction[tier];

            // enemy attack speed
            var enemyAvgAttackSpeed = 1 / (MonsterAverageAnimationLength.GetValueMod(CreatureType) / GetAnimSpeed() + ((PowerupTime ?? 1.0) / 2));

            // enemy attribute mod
            var strength = (int)Strength.Base;
            var enemyAttributeMod = SkillFormula.GetAttributeMod(strength, Skill.Sword);

            // final base damage
            var baseAvgDamage = targetEnemyDamage / weightedPlayerArmorReduction / playerLifeProtReduction / playerEvadeDamageReduction / enemyAvgAttackSpeed / enemyAttributeMod * lethality;

            if (DebugArchetypeSystem)
                Console.WriteLine($"\nDamage Calc for {Name} ({WeenieClassId})\n " +
                    $" BaseAvgDamge: {Math.Round(baseAvgDamage, 1)} = \n" +
                    $"  targetEnemyDamage ({Math.Round(targetEnemyDamage, 1)}) - weightedEnemyDamage ({Math.Round(weightedEnemyDamage, 1)}% per second), weightedPlayerHealth ({Math.Round(weightedPlayerHealth, 1)}) \n" +
                    $"  / armorReduction ({weightedPlayerArmorReduction})\n" +
                    $"  / protReduction ({playerLifeProtReduction})  \n" +
                    $"  / evadeReduction ({Math.Round(playerEvadeDamageReduction, 2)}) - weightedPlayerMeleeDef ({weightedPlayerMeleeDef}), enemyAttackSkill ({enemyAttackSkill}), playerEvadeChance ({Math.Round(playerEvadeChance, 2)})  \n" +
                    $"  / enemyAttacksPerSecond ({Math.Round(enemyAvgAttackSpeed, 2)})  \n" +
                    $"  / enemyAttributeMod ({enemyAttributeMod})\n");

            return baseAvgDamage;
        }
    }
}
