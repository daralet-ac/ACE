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

        public static float GetStaminaCost(PowerAccuracy powerAccuracy, int weaponTier, int burden, float powerAccuracyLevel = 0.0f, int weaponSpeed = 0, float? weightClassPenalty = null)
        {
            //Console.WriteLine($"GetStaminaCost - Power: {powerAccuracy}, WeaponTier: {weaponTier}, Burden: {burden}");
            var baseCost = 0.0f;

            // Max stamina cost per tier, then reduced by these factors (weapon speed, power/accuracy level, 
            var maxCost = 20.0f;

            // WeaponSpeed mod can range from 66.66% to 100%, depending on weapon speed (0-100)
            var minSpeedMod = 200.0f / 3;  
            var speedModRange = 100.0f / 3;
            var weaponSpeedMod = minSpeedMod + (float)weaponSpeed / 100 * speedModRange;
            weaponSpeedMod *= 0.01f;

            // PowerLevel mod reduces stamina cost exponentially: i.e.  100% = 100% Cost,  75% = 56% Cost,  50% = 25% Cost,  25% = 6.25% Cost,  0% = 0% Cost (min 1)
            var powerLevelMod = (float)Math.Pow(powerAccuracyLevel, 2);

            var weightClassMod = weightClassPenalty ?? 1.0f;

            weaponTier = Math.Max(weaponTier - 1, 1);

            baseCost = maxCost * powerLevelMod * weaponTier * weaponSpeedMod * weightClassMod;

            //Console.WriteLine($"GetStaminaCost - Final Cost: {baseCost}\n" +
            //    $" -WeaponTier: {weaponTier} WeightClassPenalty: {weightClassPenalty}\n" +
            //    $" -PowerLevel: {powerAccuracyLevel} PowerLevelMod: {powerLevelMod}\n" +
            //    $" -WeaponSpeed: {weaponSpeed} WeaponSpeedMod: {weaponSpeedMod}");

            return baseCost;
        }
    }
}
