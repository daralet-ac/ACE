using System;
using System.Collections.Generic;

using ACE.Server.Factories.Entity;
using ACE.Server.Factories.Enum;

namespace ACE.Server.Factories.Tables
{
    public static class EmpoweredScarabWcids
    {
        private static ChanceTable<WeenieClassName> T1_T2_T3_Chances = new ChanceTable<WeenieClassName>(ChanceTableType.Weight)
        {
            ( WeenieClassName.empoweredScarabBlue_Life,      1.0f),
            ( WeenieClassName.empoweredScarabBlue_War,       1.0f)
        };

        private static ChanceTable<WeenieClassName> T4_T5_Chances = new ChanceTable<WeenieClassName>(ChanceTableType.Weight)
        {
            ( WeenieClassName.empoweredScarabBlue_Life,      1.0f),
            ( WeenieClassName.empoweredScarabBlue_War,       1.0f),
            ( WeenieClassName.empoweredScarabYellow_Life,    1.0f),
            ( WeenieClassName.empoweredScarabYellow_War,     1.0f)
        };

        private static ChanceTable<WeenieClassName> T6_T7_T8_Chances = new ChanceTable<WeenieClassName>(ChanceTableType.Weight)
        {
            ( WeenieClassName.empoweredScarabBlue_Life,      1.0f),
            ( WeenieClassName.empoweredScarabBlue_War,       1.0f),
            ( WeenieClassName.empoweredScarabYellow_Life,    1.0f),
            ( WeenieClassName.empoweredScarabYellow_War,     1.0f),
            ( WeenieClassName.empoweredScarabRed_Life,       1.0f),
            ( WeenieClassName.empoweredScarabRed_War,        1.0f)
        };

        private static List<ChanceTable<WeenieClassName>> tierChances = new List<ChanceTable<WeenieClassName>>()
        {
            T1_T2_T3_Chances,
            T1_T2_T3_Chances,
            T1_T2_T3_Chances,
            T4_T5_Chances,
            T4_T5_Chances,
            T6_T7_T8_Chances,
            T6_T7_T8_Chances,
            T6_T7_T8_Chances
        };

        public static WeenieClassName Roll(int tier)
        {
            // todo: add unique profiles for t7 / t8?
            tier = Math.Clamp(tier, 1, 8);

            return tierChances[tier - 1].Roll();
        }

        private static readonly HashSet<WeenieClassName> _combined = new HashSet<WeenieClassName>();

        static EmpoweredScarabWcids()
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
