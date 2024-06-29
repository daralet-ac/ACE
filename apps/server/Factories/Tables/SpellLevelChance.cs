using System.Collections.Generic;

using ACE.Server.Factories.Entity;

namespace ACE.Server.Factories.Tables
{
    public static class SpellLevelChance
    {
        /*
            1: 1-3
            2: 3-5
            3: 4-6
            4: 5-6
            5: 6-7 (should be 5-7)
            6: 6-7
            7: 7-8 (should be 6-8)
            8: 7-8 (should be 6-8)
        */

        private static ChanceTable<int> T1_SpellLevelChances = new ChanceTable<int>()
        {
            ( 1, 1.0f ),
        };

        private static ChanceTable<int> T2_SpellLevelChances = new ChanceTable<int>()
        {
            ( 1, 1.0f ),
        };

        private static ChanceTable<int> T3_SpellLevelChances = new ChanceTable<int>()
        {
            ( 1, 0.95f ),
            ( 2, 0.05f ),
        };

        private static ChanceTable<int> T4_SpellLevelChances = new ChanceTable<int>()
        {
            ( 2, 1.0f ),
        };

        private static ChanceTable<int> T5_SpellLevelChances = new ChanceTable<int>()
        {
            ( 2, 0.95f ),
            ( 3, 0.05f ),
        };

        private static ChanceTable<int> T6_SpellLevelChances = new ChanceTable<int>()
        {
            ( 3, 1.0f ),
        };

        private static ChanceTable<int> T7_SpellLevelChances = new ChanceTable<int>()
        {
            ( 3, 0.95f ),
            ( 4, 0.05f ),
        };

        private static ChanceTable<int> T8_SpellLevelChances = new ChanceTable<int>()
        {
            ( 3, 0.85f ),
            ( 4, 0.15f ),
        };

        private static readonly List<ChanceTable<int>> spellLevelChances = new List<ChanceTable<int>>()
        {
            T1_SpellLevelChances,
            T2_SpellLevelChances,
            T3_SpellLevelChances,
            T4_SpellLevelChances,
            T5_SpellLevelChances,
            T6_SpellLevelChances,
            T7_SpellLevelChances,
            T8_SpellLevelChances
        };

        static SpellLevelChance()
        {

        }

        /// <summary>
        /// Rolls for a spell level for a tier
        /// </summary>
        public static int Roll(int tier)
        {
            return spellLevelChances[tier - 1].Roll();
        }
    }
}
