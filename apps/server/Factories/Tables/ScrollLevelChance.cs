using System.Collections.Generic;

using ACE.Server.Factories.Entity;
using ACE.Database.Models.World;

namespace ACE.Server.Factories.Tables
{
    public static class ScrollLevelChance
    {
        private static ChanceTable<int> T1_ScrollLevelChances = new ChanceTable<int>()
        {
            ( 1, 1.0f ),
        };

        private static ChanceTable<int> T2_ScrollLevelChances = new ChanceTable<int>()
        {
            ( 1, 0.8f ),
            ( 2, 0.2f )
        };

        private static ChanceTable<int> T3_ScrollLevelChances = new ChanceTable<int>()
        {
            ( 1, 0.1f ),
            ( 2, 0.8f ),
            ( 3, 0.1f )
        };

        private static ChanceTable<int> T4_ScrollLevelChances = new ChanceTable<int>()
        {
            ( 2, 0.1f ),
            ( 3, 0.8f ),
            ( 4, 0.1f )
        };

        private static ChanceTable<int> T5_ScrollLevelChances = new ChanceTable<int>()
        {
            ( 3, 0.1f ),
            ( 4, 0.8f ),
            ( 5, 0.1f )
        };

        private static ChanceTable<int> T6_ScrollLevelChances = new ChanceTable<int>()
        {
            ( 4, 0.1f ),
            ( 5, 0.8f ),
            ( 6, 0.1f )
        };

        private static ChanceTable<int> T7_ScrollLevelChances = new ChanceTable<int>()
        {
            ( 5, 0.25f ),
            ( 6, 0.75f )
        };

        private static ChanceTable<int> T8_ScrollLevelChances = new ChanceTable<int>()
        {
            ( 6, 0.75f ),
            ( 7, 0.25f )
        };

        private static readonly List<ChanceTable<int>> scrollLevelChances = new List<ChanceTable<int>>()
        {
            T1_ScrollLevelChances,
            T2_ScrollLevelChances,
            T3_ScrollLevelChances,
            T4_ScrollLevelChances,
            T5_ScrollLevelChances,
            T6_ScrollLevelChances,
            T7_ScrollLevelChances,
            T8_ScrollLevelChances,
        };

        public static int Roll(TreasureDeath profile)
        {
            //if (profile.TreasureType == 338) // Steel Chest
            //    return 7;

            var table = scrollLevelChances[profile.Tier - 1];

            return table.Roll(profile.LootQualityMod);
        }

        static ScrollLevelChance()
        {

        }
    }
}
