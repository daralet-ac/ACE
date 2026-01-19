using System;
using System.Collections.Generic;
using ACE.Entity.Enum;

namespace ACE.Server.Entity;

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

    public static float GetStaminaCost(
        int weaponTier,
        float animLength = 3.0f,
        float powerAccuracyLevel = 0.0f,
        float? weightClassPenalty = null
    )
    {
        // Weapon tier and base cost
        weaponTier = Math.Clamp(weaponTier - 1, 0, 7);
        int[] staminaCosts = [40, 60, 80, 90, 100, 105, 115, 120];
        var baseCost = staminaCosts[weaponTier];

        // PowerLevel mod reduces stamina cost exponentially: i.e.  100% = 100% Cost,  75% = 56% Cost,  50% = 25% Cost,  25% = 6.25% Cost,  0% = 0% Cost (min 1)
        var powerLevelMod = (float)Math.Pow(powerAccuracyLevel, 2);

        // WeaponAnimationLength mod
        var divisor = 5.0f;
        var animLengthMod = (animLength + powerAccuracyLevel) / (divisor + powerAccuracyLevel);
        //Console.WriteLine($"AnimLengthMod: ({animLength} + {powerAccuracyLevel}) / ({divisor} + {powerAccuracyLevel}) = {animLengthMod}");

        // Weight class resource penalty mod
        var weightClassPenaltyMod = weightClassPenalty ?? 1.0f;

        // Final calculation
        var finalCost = baseCost * powerLevelMod * animLengthMod * weightClassPenaltyMod;

        // Console.WriteLine($"GetStaminaCost()\n" +
        //     $" -Base Cost: {baseCost}\n" +
        //     $" -WeaponTier: {weaponTier}\n" +
        //     $" -WeightClassPenalty: {weightClassPenalty}\n" +
        //     $" -PowerLevel: {powerAccuracyLevel} PowerLevelMod: {powerLevelMod}\n" +
        //     $" -WeaponAnim: {animLength} AnimLengthMod: {animLengthMod}\n" +
        //     $" -Final Cost: {finalCost}\n" +
        //     $" -AttacksPerMin: {60 / (animLength + powerAccuracyLevel)}\n" +
        //     $" -Cost per min: {(60 / (animLength + powerAccuracyLevel)) * finalCost}");

        return finalCost;
    }
}
