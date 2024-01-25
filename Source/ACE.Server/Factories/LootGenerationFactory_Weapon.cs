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

        private static bool TryMutateWeaponMods(WorldObject wo, TreasureDeath treasureDeath, out float totalModPercentile, bool caster = false)
        {
            var tier = treasureDeath.Tier;
            totalModPercentile = 0.0f;

            //if (tier < 2)
            //    return false;

            var qualityMod = treasureDeath.LootQualityMod != 0.0f ? treasureDeath.LootQualityMod : 0.0f;

            // Each weapon can 1 to 4 mod rolls
            var potentialTypes = new List<int>() { 1, 2, 3};
            var numTypes = potentialTypes.Count;
            var rolledTypes = GetRolledTypes(potentialTypes, qualityMod);

            // The more mods rolled, the lower value each one can roll (1 = 100%, 2 = 83.33%, 3 = 66.67%, 4 = 50%)
            var inverse = numTypes - rolledTypes.Count;
            var percentile = (float)inverse / (numTypes - 1);
            var multiplier = rolledTypes.Count > 0 ? (percentile + 1) / 2 : 1;

            if (caster)
            {
                var bonusAmount = GetCasterSubType(wo) == 0 || GetCasterSubType(wo) == 3 ? BonusDefenseMod(treasureDeath, wo) : 0;

                foreach (var type in rolledTypes)
                {
                    var amount = GetWeaponModAmount(wo, treasureDeath, multiplier, out var modPercentile, bonusAmount) * multiplier;

                    totalModPercentile += modPercentile;

                    switch(type)
                    {
                        case 1: wo.ManaConversionMod = amount; break;
                        case 2: wo.WeaponPhysicalDefense = amount + 1; break;
                        case 3: wo.WeaponMagicalDefense = amount + 1; break;
                    }
                }

                var subtype = GetCasterSubType(wo);
                var ORB = 0;
                var STAFF = 3;

                if (subtype == STAFF)
                {
                    if (wo.WeaponPhysicalDefense > 0)
                        wo.WeaponPhysicalDefense += bonusAmount;
                    else
                        wo.WeaponPhysicalDefense = 1 + bonusAmount;
                }
                else if (subtype == ORB)
                    if (wo.WeaponMagicalDefense > 0)
                        wo.WeaponMagicalDefense += bonusAmount;
                    else
                        wo.WeaponMagicalDefense = 1 + bonusAmount;
            }
            else
            {
                foreach (var type in rolledTypes)
                {
                    var amount = GetWeaponModAmount(wo, treasureDeath, multiplier, out var modPercentile) * multiplier;
                    totalModPercentile += modPercentile;

                    switch (type)
                    {
                        case 1: wo.WeaponOffense = amount + 1; break;
                        case 2: wo.WeaponPhysicalDefense = amount + 1; break;
                        case 3: wo.WeaponMagicalDefense = amount + 1; break;
                    }
                }
            }
            totalModPercentile /= rolledTypes.Count;

            return true;
        }

        private static double GetWeaponModAmount(WorldObject wo, TreasureDeath treasureDeath, float multiplier, out float modPercentile, float bonusAmount = 0.0f)
        {
            var tier = treasureDeath.Tier;
            var diminishingMultiplier = GetDiminishingRoll(treasureDeath);
            int[] maxModPerTier = { 5, 10, 15, 20, 22, 24, 26, 30 };

            var minMod = maxModPerTier[tier - 1] * multiplier / 2 / 100;
            var weaponMod = minMod + minMod * diminishingMultiplier;

            // If weapon is a caster staff or orb, the max possible defense mod is 20 more
            var bonusMax = (bonusAmount > 0 ? 20.0f : 0.0f) / 100;
            var maxPossibleModBeforeBonus = (float)maxModPerTier[6] / 100;

            modPercentile = (float)weaponMod / ((maxPossibleModBeforeBonus + bonusMax) * multiplier);

            return weaponMod;
        }

        private static int GetWeaponWorkmanship(WorldObject wo, float damagePercentile, float skillModsPercentile)
        {
            var divisor = 0;

            // Damage
            divisor++;

            // Weapon Mods
            divisor++;

            // Average Percentile
            var finalPercentile = (damagePercentile + skillModsPercentile) / divisor;
            //Console.WriteLine($"{wo.Name}\n -Mods %: {skillModsPercentile}\n" + $" -Damage %: {damagePercentile}\n" + $" -Divisor: {divisor}\n" +
            //    $" --FINAL: {finalPercentile}\n\n");

            // Workmanship Calculation
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

        private static int[] GetCasterMaxDamageMod(WorldObject wo)
        {
            return LootTables.CasterMaxDamageMod;
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
    }
}

