using System;
using ACE.Common;
using ACE.Database.Models.World;
using ACE.Server.WorldObjects;

namespace ACE.Server.Factories;

public static partial class LootGenerationFactory
{
    private static WorldObject CreateAnimalParts(TreasureDeath profile)
    {
        WorldObject wo;

        var amount = ThreadSafeRandom.Next(1, 2);
        var tier = profile.Tier;

        var quality = tier / 2;

        var animalPartsMatrixIndex = tier - 1;
        var upperLimit = LootTables.AnimalPartsLootMatrix[animalPartsMatrixIndex].Length - 1;
        var chance = ThreadSafeRandom.Next(0, upperLimit);

        var id = (uint)LootTables.AnimalPartsLootMatrix[animalPartsMatrixIndex][chance];

        wo = WorldObjectFactory.CreateNewWorldObject(id);
        wo.SetStackSize(amount);

        return wo;
    }
}
