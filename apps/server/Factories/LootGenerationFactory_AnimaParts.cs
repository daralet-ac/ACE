using ACE.Common;
using ACE.Database.Models.World;
using ACE.Server.WorldObjects;
using System;

namespace ACE.Server.Factories
{
    public static partial class LootGenerationFactory
    {
        private static WorldObject CreateAnimalParts(TreasureDeath profile)
        {
            WorldObject wo;

            var amount = ThreadSafeRandom.Next(1, 2);
            var tier = profile.Tier;

            var quality = tier / 2;

            int animalPartsMatrixIndex = tier - 1;
            int upperLimit = LootTables.AnimalPartsLootMatrix[animalPartsMatrixIndex].Length - 1;
            int chance = ThreadSafeRandom.Next(0, upperLimit);

            uint id = (uint)LootTables.AnimalPartsLootMatrix[animalPartsMatrixIndex][chance];

            wo = WorldObjectFactory.CreateNewWorldObject(id);
            wo.SetStackSize(amount);

            return wo;
        }
    }
}
