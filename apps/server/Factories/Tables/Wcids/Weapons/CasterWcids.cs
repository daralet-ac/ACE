using System.Collections.Generic;
using ACE.Server.Factories.Entity;
using ACE.Server.Factories.Enum;

namespace ACE.Server.Factories.Tables.Wcids.Weapons;

public static class CasterWcids
{
    private static ChanceTable<WeenieClassName> T1_T2_T3_T4_T5_Chances = new ChanceTable<WeenieClassName>(
        ChanceTableType.Weight
    )
    {
        (WeenieClassName.orb, 1.0f),
        (WeenieClassName.sceptre, 1.0f),
        (WeenieClassName.staff, 1.0f),
        (WeenieClassName.wand, 1.0f),
    };

    private static ChanceTable<WeenieClassName> T6_T7_T8_Chances = new ChanceTable<WeenieClassName>(
        ChanceTableType.Weight
    )
    {
        (WeenieClassName.orbslash, 1.0f),
        (WeenieClassName.orbpierce, 1.0f),
        (WeenieClassName.orbblunt, 1.0f),
        (WeenieClassName.orbacid, 1.0f),
        (WeenieClassName.orbfire, 1.0f),
        (WeenieClassName.orbcold, 1.0f),
        (WeenieClassName.orbelectric, 1.0f),
        (WeenieClassName.wandslashing, 1.0f),
        (WeenieClassName.wandpiercing, 1.0f),
        (WeenieClassName.wandblunt, 1.0f),
        (WeenieClassName.wandacid, 1.0f),
        (WeenieClassName.wandfire, 1.0f),
        (WeenieClassName.wandfrost, 1.0f),
        (WeenieClassName.wandelectric, 1.0f),
        (WeenieClassName.ace31819_slashingbaton, 1.0f),
        (WeenieClassName.ace31825_piercingbaton, 1.0f),
        (WeenieClassName.ace31821_bluntbaton, 1.0f),
        (WeenieClassName.ace31820_acidbaton, 1.0f),
        (WeenieClassName.ace31823_firebaton, 1.0f),
        (WeenieClassName.ace31824_frostbaton, 1.0f),
        (WeenieClassName.ace31822_electricbaton, 1.0f),
        (WeenieClassName.ace37223_slashingstaff, 1.0f),
        (WeenieClassName.ace37222_piercingstaff, 1.0f),
        (WeenieClassName.ace37225_bluntstaff, 1.0f),
        (WeenieClassName.ace37224_acidstaff, 1.0f),
        (WeenieClassName.ace37220_firestaff, 1.0f),
        (WeenieClassName.ace37221_froststaff, 1.0f),
        (WeenieClassName.ace37219_electricstaff, 1.0f),
    };

    private static readonly List<ChanceTable<WeenieClassName>> casterTiers = new List<ChanceTable<WeenieClassName>>()
    {
        T1_T2_T3_T4_T5_Chances,
        T1_T2_T3_T4_T5_Chances,
        T1_T2_T3_T4_T5_Chances,
        T1_T2_T3_T4_T5_Chances,
        T1_T2_T3_T4_T5_Chances,
        T6_T7_T8_Chances,
        T6_T7_T8_Chances,
        T6_T7_T8_Chances
    };

    public static WeenieClassName Roll(int tier)
    {
        return casterTiers[tier - 1].Roll();
    }

    private static readonly HashSet<WeenieClassName> _combined = new HashSet<WeenieClassName>();

    static CasterWcids()
    {
        foreach (var casterTier in casterTiers)
        {
            foreach (var entry in casterTier)
            {
                _combined.Add(entry.result);
            }
        }
    }

    public static bool Contains(WeenieClassName wcid)
    {
        return _combined.Contains(wcid);
    }
}
