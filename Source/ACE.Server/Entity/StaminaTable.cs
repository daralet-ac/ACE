using System;
using System.Collections.Generic;
using ACE.Entity.Enum;
using ACE.Server.WorldObjects;

namespace ACE.Server.Entity
{
    public class StaminaCost
    {
        public int Burden;
        public int WeaponTier;
        public float Stamina;

        public StaminaCost(int burden, int weaponTier, float stamina)
        {
            Burden = burden;
            WeaponTier = weaponTier;
            Stamina = stamina;
        }
    }

    public static class StaminaTable
    {
        public static Dictionary<PowerAccuracy, List<StaminaCost>> Costs;

        static StaminaTable()
        {
            BuildTable();
        }

        public static void BuildTable()
        {
            Costs = new Dictionary<PowerAccuracy, List<StaminaCost>>();

            // must be in descending order
            var lowCosts = new List<StaminaCost>();

            var midCosts = new List<StaminaCost>();

            var highCosts = new List<StaminaCost>();

            Costs.Add(PowerAccuracy.Low, lowCosts);
            Costs.Add(PowerAccuracy.Medium, midCosts);
            Costs.Add(PowerAccuracy.High, highCosts);
        }

        public static float GetStaminaCost(int weaponTier, bool dualWieldStaminaBonus, float animLength = 3.0f, float powerAccuracyLevel = 0.0f, int weaponSpeed = 0, float? weightClassPenalty = null)
        {
            // Weapon tier mod
            weaponTier = Math.Clamp(weaponTier - 1, 0, 7);

            // Tier Cost
            var baseCost = (weaponTier + 1) * 20;

            // PowerLevel mod reduces stamina cost exponentially: i.e.  100% = 100% Cost,  75% = 56% Cost,  50% = 25% Cost,  25% = 6.25% Cost,  0% = 0% Cost (min 1)
            var powerLevelMod = (float)Math.Pow(powerAccuracyLevel, 2);

            // WeaponAnimationLength mod
            var maxAnimLength = 3.0f;
            var animLengthMod = animLength / maxAnimLength;

            // WeaponSpeed mod can range from 66.66% to 100%, depending on weapon speed (0-100)
            var minSpeedMod = 200.0f / 3;  
            var speedModRange = 100.0f / 3;
            var weaponSpeedMod = minSpeedMod + (float)weaponSpeed / 100 * speedModRange;
            weaponSpeedMod *= 0.01f;

            // Weight class resource penalty mod
            var weightClassPenaltyMod = weightClassPenalty ?? 1.0f;

            // Dual wield spec mod
            var dualWieldSpecMod = dualWieldStaminaBonus ? 0.75f : 1.0f;

            // Final calculation
            var finalCost = baseCost * powerLevelMod * weaponSpeedMod * animLengthMod * weightClassPenaltyMod * dualWieldSpecMod;

            //Console.WriteLine($"GetStaminaCost - Final Cost: {finalCost}\n" +
            //    $" -WeaponTier: {weaponTier} WeightClassPenalty: {weightClassPenalty}\n" +
            //    $" -PowerLevel: {powerAccuracyLevel} PowerLevelMod: {powerLevelMod}\n" +
            //    $" -WeaponSpeed: {weaponSpeed} WeaponSpeedMod: {weaponSpeedMod}\n" +
            //    $" -WeaponAnim: {animLength} AnimLengthMod: {animLengthMod}\n" +
            //    $" -DualWield: {dualWieldSpecMod}");

            return finalCost;
        }
    }
}
