using ACE.Common;
using ACE.Database.Models.Auth;
using ACE.Database.Models.World;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Factories.Entity;
using ACE.Server.Factories.Enum;
using ACE.Server.Factories.Tables;
using ACE.Server.WorldObjects;
using System;
using System.Collections.Generic;

namespace ACE.Server.Factories
{
    public static partial class LootGenerationFactory
    {
        private static WorldObject CreateWeapon(TreasureDeath profile, bool isMagical)
        {
            int chance = ThreadSafeRandom.Next(1, 100);

            // Aligning drop ratio to better align with retail - HarliQ 11/11/19
            // Melee - 42%
            // Missile - 36%
            // Casters - 22%

            return chance switch
            {
                var rate when (rate < 43) => CreateMeleeWeapon(profile, isMagical),
                var rate when (rate > 42 && rate < 79) => CreateMissileWeapon(profile, isMagical),
                _ => CreateCaster(profile, isMagical),
            };
        }
        private static WorldObject CreateWeaponWarrior(TreasureDeath profile, bool isMagical)
        {
            int chance = ThreadSafeRandom.Next(1, 100);

            // Aligning drop ratio to better align with retail - HarliQ 11/11/19
            // Melee - 42%
            // Missile - 36%
            // Casters - 22%

            return chance switch
            {
                var rate when (rate < 43) => CreateMeleeWeapon(profile, isMagical),
                var rate when (rate > 42 && rate < 79) => CreateMissileWeapon(profile, isMagical),
                _ => CreateCaster(profile, isMagical),
            };
        }
        private static WorldObject CreateWeaponRogue(TreasureDeath profile, bool isMagical)
        {
            int chance = ThreadSafeRandom.Next(1, 100);

            // Aligning drop ratio to better align with retail - HarliQ 11/11/19
            // Melee - 42%
            // Missile - 36%
            // Casters - 22%

            return chance switch
            {
                var rate when (rate < 43) => CreateMeleeWeapon(profile, isMagical),
                var rate when (rate > 42 && rate < 79) => CreateMissileWeapon(profile, isMagical),
                _ => CreateCaster(profile, isMagical),
            };
        }
        private static WorldObject CreateWeaponCaster(TreasureDeath profile, bool isMagical)
        {
            int chance = ThreadSafeRandom.Next(1, 100);

            // Aligning drop ratio to better align with retail - HarliQ 11/11/19
            // Melee - 42%
            // Missile - 36%
            // Casters - 22%

            return chance switch
            {
                var rate when (rate < 43) => CreateMeleeWeapon(profile, isMagical),
                var rate when (rate > 42 && rate < 79) => CreateMissileWeapon(profile, isMagical),
                _ => CreateCaster(profile, isMagical),
            };
        }

        private static float RollWeaponSpeedMod(TreasureDeath treasureDeath)
        {
            var qualityLevel = QualityChance.Roll(treasureDeath);

            if (qualityLevel == 0)
                return 1.0f;    // no bonus

            var rng = (float)ThreadSafeRandom.Next(-0.025f, 0.025f);

            // min/max range: 97.5% - 102.5%
            var weaponSpeedMod = 1.0f - (qualityLevel * 0.005f + rng);

            //Console.WriteLine($"WeaponSpeedMod: {weaponSpeedMod}");

            return weaponSpeedMod;
        }

        private static float ApplyQualityModToExtraMutationChance(float chance, float lootQualityMod)
        {
            return chance * (1 + lootQualityMod);
        }

        private static bool RollSlayer(TreasureDeath treasureDeath, WorldObject wo)
        {
            var chance = SlayerTypeChance.GetSlayerChanceForTier(treasureDeath.Tier);
            chance = ApplyQualityModToExtraMutationChance(chance, treasureDeath.LootQualityMod);
            if (chance > ThreadSafeRandom.Next(0.0f, 1.0f))
            {
                wo.SlayerCreatureType = SlayerTypeChance.Roll(treasureDeath);
                wo.SlayerDamageBonus = 1.5f;
                wo.IconOverlayId = 0x06005EC0;
                return true;
            }
            return false;
        }

        private static bool TryMutateWeaponMods(WorldObject wo, TreasureDeath treasureDeath, out float highestModPercentile, bool caster = false)
        {
            highestModPercentile = 0.0f;

            var qualityMod = treasureDeath.LootQualityMod != 0.0f ? treasureDeath.LootQualityMod : 0.0f;

            var potentialTypes = new List<int>() { 1, 2};
            var rolledTypes = GetRolledTypes(potentialTypes, qualityMod);

            // Multiplier for weapons that rolled multiple mods. Currently limited to 2 mod rolls, but ready for more.
            float numRolledTypesMultiplier;
            switch (rolledTypes.Count)
            {
                default:
                case 1: numRolledTypesMultiplier = 1.0f; break; // 100% per mod, 100% total.
                case 2: numRolledTypesMultiplier = 0.75f; break; // 75% per mod, 150% total.
                case 3: numRolledTypesMultiplier = 0.5833f; break; // 58.33% per mod, 175% total.
                case 4: numRolledTypesMultiplier = 0.475f; break; // 47.5% per mod, 190% total.
                case 5: numRolledTypesMultiplier = 0.4f; break; // 40% per mod, 200% total.
            }

            foreach (var type in rolledTypes)
            {
                float modPercentile;
                    
                switch(type)
                {
                    case 1: // Offense Mods
                        if(!caster)
                            wo.WeaponOffense = GetWeaponModAmount(wo, treasureDeath, out modPercentile) * numRolledTypesMultiplier + 1;
                        else
                        {
                            if(wo.WieldSkillType2 == (int)Skill.WarMagic)
                                wo.WeaponWarMagicMod = GetWeaponModAmount(wo, treasureDeath, out modPercentile) * numRolledTypesMultiplier;
                            else
                                wo.WeaponLifeMagicMod = GetWeaponModAmount(wo, treasureDeath, out modPercentile) * numRolledTypesMultiplier;
                        }

                        if(modPercentile > highestModPercentile)
                            highestModPercentile = modPercentile;
                        break;
                    case 2: // Defense Mods
                        float defenseModPercetile;
                        wo.WeaponPhysicalDefense = GetWeaponModAmount(wo, treasureDeath, out modPercentile) * numRolledTypesMultiplier + 1;
                        defenseModPercetile = modPercentile;

                        wo.WeaponMagicalDefense = GetWeaponModAmount(wo, treasureDeath, out modPercentile) * numRolledTypesMultiplier + 1;
                        defenseModPercetile += modPercentile;

                        defenseModPercetile /= 2;
                        if (defenseModPercetile > highestModPercentile)
                            highestModPercentile = modPercentile;
                        break;
                }
            }

            var isStaff = caster ? GetCasterSubType(wo) == 3 : false;
            if (isStaff)
            {
                wo.WeaponPhysicalDefense += BonusDefenseMod(treasureDeath, wo);
                wo.WeaponMagicalDefense += BonusDefenseMod(treasureDeath, wo);
            }

            return true;
        }

        private static double GetWeaponModAmount(WorldObject wo, TreasureDeath treasureDeath, out float modPercentile)
        {
            var tier = Math.Clamp(treasureDeath.Tier - 1, 0, 7);
            float[] bonusModRollPerTier = { 0.0f, 0.01f, 0.02f, 0.03f, 0.04f, 0.05f, 0.075f, 0.1f };

            var minMod = 0.1f;
            var weaponMod = minMod + minMod * GetDiminishingRoll(treasureDeath) + bonusModRollPerTier[tier];

            var maxPossibleMod = minMod + minMod + bonusModRollPerTier[7];

            modPercentile = (weaponMod - minMod) / (maxPossibleMod - minMod);
            return weaponMod;
        }

        private static int GetWeaponWorkmanship(WorldObject wo, float damagePercentile, float skillModsPercentile)
        {
            var divisor = 3; // damage x2 + mods

            // Average Percentile
            var finalPercentile = (damagePercentile * 2 + skillModsPercentile) / divisor;
            //Console.WriteLine($"{wo.NameWithMaterialAndElement}\n -Mods %: {skillModsPercentile}\n" + $" -Damage %: {damagePercentile}\n" + $" -Divisor: {divisor}\n" +
            //    $" --FINAL: {finalPercentile}\n\n");

            // Workmanship Calculation
            //Console.WriteLine($"{wo.NameWithMaterialAndElement} - {Math.Max((int)(finalPercentile * 10), 1)}");
            return Math.Max((int)(finalPercentile * 10), 1);
        }

        private static float[] GetWeaponMutationMultiplier(TreasureRoll roll)
        {
            var weaponType = roll.WeaponType;
            switch (weaponType)
            {
                case TreasureWeaponType.Axe: return LootTables.AxeMutationMultiplier;
                case TreasureWeaponType.Dagger: return LootTables.DaggerMutationMultiplier;
                case TreasureWeaponType.DaggerMS: return LootTables.DaggerMsMutationMultiplier;
                case TreasureWeaponType.Mace: return LootTables.MaceMutationMultiplier;
                case TreasureWeaponType.MaceJitte: return LootTables.MaceMutationMultiplier;
                case TreasureWeaponType.Spear: return LootTables.SpearMutationMultiplier;
                case TreasureWeaponType.Staff: return LootTables.StaffMutationMultiplier;
                case TreasureWeaponType.Sword: return LootTables.SwordMutationMultiplier;
                case TreasureWeaponType.SwordMS: return LootTables.SwordMSMutationMultiplier;
                case TreasureWeaponType.Unarmed: return LootTables.UnarmedMutationMultiplier;
                case TreasureWeaponType.Thrown: return LootTables.AtlatlDamageModMutationMultiplier;
                case TreasureWeaponType.TwoHandedAxe: return LootTables.TwohandAxeMutationMultiplier;
                case TreasureWeaponType.TwoHandedMace: return LootTables.TwohandMaceMutationMultiplier;
                case TreasureWeaponType.TwoHandedSpear: return LootTables.TwohandSpearMutationMultiplier;
                case TreasureWeaponType.TwoHandedSword: return LootTables.TwohandSwordMutationMultiplier;
            }
            return null;
        }

        private static int[] GetWeaponMutationAdder(TreasureRoll roll)
        {
            var weaponType = roll.WeaponType;
            switch (weaponType)
            {
                case TreasureWeaponType.Axe: return LootTables.AxeMutationAdder;
                case TreasureWeaponType.Dagger: return LootTables.DaggerMutationAdder;
                case TreasureWeaponType.DaggerMS: return LootTables.DaggerMsMutationAdder;
                case TreasureWeaponType.Mace: return LootTables.MaceMutationAdder;
                case TreasureWeaponType.MaceJitte: return LootTables.MaceMutationAdder;
                case TreasureWeaponType.Spear: return LootTables.SpearMutationAdder;
                case TreasureWeaponType.Staff: return LootTables.StaffMutationAdder;
                case TreasureWeaponType.Sword: return LootTables.SwordMutationAdder;
                case TreasureWeaponType.SwordMS: return LootTables.SwordMSMutationAdder;
                case TreasureWeaponType.Unarmed: return LootTables.UnarmedMutationAdder;
                case TreasureWeaponType.Thrown: return LootTables.AtlatlDamageModMutationAdder;
                case TreasureWeaponType.TwoHandedAxe: return LootTables.TwohandAxeMutationAdder;
                case TreasureWeaponType.TwoHandedMace: return LootTables.TwohandMaceMutationAdder;
                case TreasureWeaponType.TwoHandedSpear: return LootTables.TwohandSpearMutationAdder;
                case TreasureWeaponType.TwoHandedSword: return LootTables.TwohandSwordMutationAdder;
            }

            return null;
        }

        private static float[] GetMissileDamageModMutationMultiplier(TreasureRoll roll)
        {
            var weaponType = roll.WeaponType;
            switch (weaponType)
            {
                case TreasureWeaponType.Bow: return LootTables.BowDamageModMutationMultiplier;
                case TreasureWeaponType.BowShort: return LootTables.BowDamageModMutationMultiplier;
                case TreasureWeaponType.Crossbow: return LootTables.CrossbowDamageModMutationMultiplier;
                case TreasureWeaponType.CrossbowLight: return LootTables.CrossbowDamageModMutationMultiplier;
                case TreasureWeaponType.Atlatl: return LootTables.AtlatlDamageModMutationMultiplier;
                case TreasureWeaponType.AtlatlRegular: return LootTables.AtlatlDamageModMutationMultiplier;
                case TreasureWeaponType.Thrown: return LootTables.AtlatlDamageModMutationMultiplier;
            }
            return null;
        }

        private static int[] GetMissileDamageModMutationAdder(TreasureRoll roll)
        {
            var weaponType = roll.WeaponType;
            switch (weaponType)
            {
                case TreasureWeaponType.Bow: return LootTables.BowDamageModMutationAdder;
                case TreasureWeaponType.BowShort: return LootTables.BowDamageModMutationAdder;
                case TreasureWeaponType.Crossbow: return LootTables.CrossbowDamageModMutationAdder;
                case TreasureWeaponType.CrossbowLight: return LootTables.CrossbowDamageModMutationAdder;
                case TreasureWeaponType.Atlatl: return LootTables.AtlatlDamageModMutationAdder;
                case TreasureWeaponType.AtlatlRegular: return LootTables.AtlatlDamageModMutationAdder;
                case TreasureWeaponType.Thrown: return LootTables.AtlatlDamageModMutationAdder;
            }
            return null;
        }

        private static float[] GetMissileDamageBonusMutationMultiplier(TreasureRoll roll)
        {
            var weaponType = roll.WeaponType;
            switch (weaponType)
            {
                case TreasureWeaponType.Bow: return LootTables.BowDamageBonusMutationMultiplier;
                case TreasureWeaponType.BowShort: return LootTables.BowDamageBonusMutationMultiplier;
                case TreasureWeaponType.Crossbow: return LootTables.CrossbowDamageBonusMutationMultiplier;
                case TreasureWeaponType.CrossbowLight: return LootTables.CrossbowDamageBonusMutationMultiplier;
                case TreasureWeaponType.Atlatl: return LootTables.AtlatlDamageBonusMutationMultiplier;
                case TreasureWeaponType.AtlatlRegular: return LootTables.AtlatlDamageBonusMutationMultiplier;
                case TreasureWeaponType.Thrown: return LootTables.AtlatlDamageBonusMutationMultiplier;
            }
            return null;
        }

        private static int[] GetMissileDamageBonusMutationAdder(TreasureRoll roll)
        {
            var weaponType = roll.WeaponType;
            switch (weaponType)
            {
                case TreasureWeaponType.Bow: return LootTables.BowDamageBonusMutationAdder;
                case TreasureWeaponType.BowShort: return LootTables.BowDamageBonusMutationAdder;
                case TreasureWeaponType.Crossbow: return LootTables.CrossbowDamageBonusMutationAdder;
                case TreasureWeaponType.CrossbowLight: return LootTables.CrossbowDamageBonusMutationAdder;
                case TreasureWeaponType.Atlatl: return LootTables.AtlatlDamageBonusMutationAdder;
                case TreasureWeaponType.AtlatlRegular: return LootTables.AtlatlDamageBonusMutationAdder;
                case TreasureWeaponType.Thrown: return LootTables.AtlatlDamageBonusMutationAdder;
            }
            return null;
        }

        private static float[] GetCasterMaxDamageMod()
        {
            return LootTables.CasterMaxDamageMod;
        }

        private static float[] GetCasterMinDamageMod()
        {
            return LootTables.CasterMinDamageMod;
        }

        private static float GetWeaponBaseDps(int tier)
        {
            var dps = 10.0f;
            switch(tier)
            {
                default:
                case 1: return 5.0f;
                case 2: return 10.0f;
                case 3: return 15.0f;
                case 4: return 22.0f;
                case 5: return 33.0f;
                case 6: return 50.0f;
                case 7: return 75.0f;
                case 8: return 110.0f;
            }
        }

        private static int GetWeaponPrimaryAttribute(Skill weaponSkill)
        {
            switch(weaponSkill)
            {
                default:
                case Skill.Sword:
                case Skill.Axe:
                case Skill.Mace:
                case Skill.Spear:
                case Skill.TwoHandedCombat:
                case Skill.ThrownWeapon:
                    return 1;
                case Skill.Bow:
                case Skill.Crossbow:
                case Skill.MissileWeapons:
                case Skill.Dagger:
                case Skill.Staff:
                case Skill.UnarmedCombat:
                    return 4;
                case Skill.WarMagic:
                case Skill.LifeMagic:
                    return 6;
            }
        }

        private static int GetWeaponWieldSkill(Skill weaponSkill)
        {
            switch (weaponSkill)
            {
                default:
                case Skill.Sword:
                case Skill.Axe:
                case Skill.Mace:
                case Skill.Spear:
                    return (int)Skill.HeavyWeapons;
                case Skill.ThrownWeapon:
                    return (int)Skill.ThrownWeapon;
                case Skill.Bow:
                case Skill.Crossbow:
                case Skill.MissileWeapons:
                    return (int)Skill.Bow;
                case Skill.Dagger:
                    return (int)Skill.Dagger;
                case Skill.Staff:
                    return (int)Skill.Staff;
                case Skill.UnarmedCombat:
                    return (int)Skill.UnarmedCombat;
                case Skill.WarMagic:
                    return (int)Skill.WarMagic;
                case Skill.LifeMagic:
                    return (int)Skill.LifeMagic;
            }
        }
    }
}

