using ACE.Common;
using ACE.Database.Models.World;
using ACE.Server.WorldObjects;

namespace ACE.Server.Factories;

public static partial class LootGenerationFactory
{
    private static WorldObject CreateFood()
    {
        var foodType = (uint)LootTables.food[ThreadSafeRandom.Next(0, LootTables.food.Length - 1)];
        return WorldObjectFactory.CreateNewWorldObject(foodType);
    }

    private static WorldObject CreateGenericObjects(TreasureDeath profile)
    {
        int chance;
        WorldObject wo;

        chance = ThreadSafeRandom.Next(1, 100);

        switch (chance)
        {
            case var rate when (rate < 3):
                wo = CreateRandomScroll(profile);
                break;
            default:
                var genericLootMatrixIndex = profile.Tier - 1;
                var upperLimit = LootTables.GenericLootMatrix[genericLootMatrixIndex].Length - 1;

                chance = ThreadSafeRandom.Next(0, upperLimit);
                var id = (uint)LootTables.GenericLootMatrix[genericLootMatrixIndex][chance];

                wo = WorldObjectFactory.CreateNewWorldObject(id);
                break;
        }

        return wo;
    }
}
