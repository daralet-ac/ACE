using System;
using System.Collections.Generic;

using ACE.Server.Factories.Entity;
using ACE.Server.Factories.Enum;

namespace ACE.Server.Factories.Tables.Wcids
{
    public static class JewelryWcids
    {
        // original headwear:
        // - crown

        // added 09-2005 - under cover of night
        // - coronet
        // - circlet
        // - diadem
        // - signet crown
        // - teardrop crown

        // trinkets: drop rate 15% consistently per tier, 2.5% for each of the 6 trinkets
        // scaling the pre-t7 tables to 85% / 15%

        private static ChanceTable<WeenieClassName> T1_T2_Chances = new ChanceTable<WeenieClassName>()
        {
            ( WeenieClassName.amulet,        0.10f ),
            ( WeenieClassName.bracelet,      0.30f ),
            ( WeenieClassName.braceletheavy, 0.10f ),
            ( WeenieClassName.necklace,      0.20f ),
            ( WeenieClassName.ring,          0.25f ),
            ( WeenieClassName.ringjeweled,   0.05f )
        };

        private static ChanceTable<WeenieClassName> T3_T4_Chances = new ChanceTable<WeenieClassName>()
        {
            ( WeenieClassName.amulet,        0.10f ),
            ( WeenieClassName.bracelet,      0.15f ),
            ( WeenieClassName.braceletheavy, 0.15f ),
            ( WeenieClassName.gorget,        0.10f ),
            ( WeenieClassName.necklace,      0.15f ),
            ( WeenieClassName.necklaceheavy, 0.05f ),
            ( WeenieClassName.ring,          0.15f ),
            ( WeenieClassName.ringjeweled,   0.15f )
        };

        private static ChanceTable<WeenieClassName> T5_T8_Chances = new ChanceTable<WeenieClassName>()
        {
            ( WeenieClassName.amulet,        0.10f ),
            ( WeenieClassName.bracelet,      0.10f ),
            ( WeenieClassName.braceletheavy, 0.20f ),
            ( WeenieClassName.gorget,        0.10f ),
            ( WeenieClassName.necklace,      0.05f ),
            ( WeenieClassName.necklaceheavy, 0.15f ),
            ( WeenieClassName.ring,          0.10f ),
            ( WeenieClassName.ringjeweled,   0.20f ),
        };

        private static List<ChanceTable<WeenieClassName>> tierChances = new List<ChanceTable<WeenieClassName>>()
        {
            T1_T2_Chances,
            T1_T2_Chances,
            T3_T4_Chances,
            T3_T4_Chances,
            T5_T8_Chances,
            T5_T8_Chances,
            T5_T8_Chances,
            T5_T8_Chances,
        };

        public static WeenieClassName Roll(int tier)
        {
            // todo: add unique profiles for t7 / t8?
            tier = Math.Clamp(tier, 1, 8);

            return tierChances[tier - 1].Roll();
        }

        private static readonly HashSet<WeenieClassName> _combined = new HashSet<WeenieClassName>();

        static JewelryWcids()
        {
            foreach (var tierChance in tierChances)
            {
                foreach (var entry in tierChance)
                    _combined.Add(entry.result);
            }
        }

        public static bool Contains(WeenieClassName wcid)
        {
            return _combined.Contains(wcid);
        }
    }
}
