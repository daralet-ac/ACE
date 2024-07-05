using System;
using System.Collections.Generic;
using ACE.Server.Factories.Entity;
using ACE.Server.Factories.Enum;

namespace ACE.Server.Factories.Tables.Wcids;

public static class AnimalPartsWcids
{
    private static ChanceTable<WeenieClassName> T1_T2_Chances = new ChanceTable<WeenieClassName>()
    {
        (WeenieClassName.beasthide_tattered, 0.33f),
        (WeenieClassName.meat_gristly, 0.33f),
        (WeenieClassName.animalbone_cracked, 0.34f),
    };

    private static ChanceTable<WeenieClassName> T3_T4_Chances = new ChanceTable<WeenieClassName>()
    {
        (WeenieClassName.beasthide_normal, 0.33f),
        (WeenieClassName.meat_normal, 0.33f),
        (WeenieClassName.animalbone_normal, 0.34f),
    };

    private static ChanceTable<WeenieClassName> T5_T6_Chances = new ChanceTable<WeenieClassName>()
    {
        (WeenieClassName.beasthide_sturdy, 0.33f),
        (WeenieClassName.meat_tender, 0.33f),
        (WeenieClassName.animalbone_solid, 0.34f),
    };

    private static ChanceTable<WeenieClassName> T7_T8_Chances = new ChanceTable<WeenieClassName>()
    {
        (WeenieClassName.beasthide_rugged, 0.33f),
        (WeenieClassName.meat_choice, 0.33f),
        (WeenieClassName.animalbone_pristine, 0.34f),
    };

    private static List<ChanceTable<WeenieClassName>> tierChances = new List<ChanceTable<WeenieClassName>>()
    {
        T1_T2_Chances,
        T1_T2_Chances,
        T3_T4_Chances,
        T3_T4_Chances,
        T5_T6_Chances,
        T5_T6_Chances,
        T7_T8_Chances,
        T7_T8_Chances,
    };

    public static WeenieClassName Roll(int tier)
    {
        // todo: add unique profiles for t7 / t8?
        tier = Math.Clamp(tier, 1, 8);

        return tierChances[tier - 1].Roll();
    }

    private static readonly HashSet<WeenieClassName> _combined = new HashSet<WeenieClassName>();

    public static bool Contains(WeenieClassName wcid)
    {
        return _combined.Contains(wcid);
    }
}
