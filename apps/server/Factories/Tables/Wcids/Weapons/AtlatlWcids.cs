using System;
using System.Collections.Generic;
using ACE.Server.Factories.Entity;
using ACE.Server.Factories.Enum;

namespace ACE.Server.Factories.Tables.Wcids;

public static class AtlatlWcids
{
    private static ChanceTable<WeenieClassName> T1_T4_Chances = new ChanceTable<WeenieClassName>(ChanceTableType.Weight)
    {
        (WeenieClassName.atlatl, 4.0f),
        (WeenieClassName.atlatlroyal, 4.0f),
        (WeenieClassName.dart, 4.0f),
        (WeenieClassName.dartacid, 1.0f),
        (WeenieClassName.dartflame, 1.0f),
        (WeenieClassName.dartfrost, 1.0f),
        (WeenieClassName.dartelectric, 1.0f),
        (WeenieClassName.axethrowing, 4.0f),
        (WeenieClassName.axethrowingacid, 1.0f),
        (WeenieClassName.axethrowingfire, 1.0f),
        (WeenieClassName.axethrowingfrost, 1.0f),
        (WeenieClassName.axethrowingelectric, 1.0f),
        (WeenieClassName.clubthrowing, 4.0f),
        (WeenieClassName.clubthrowingacid, 1.0f),
        (WeenieClassName.clubthrowingfire, 1.0f),
        (WeenieClassName.clubthrowingfrost, 1.0f),
        (WeenieClassName.clubthrowingelectric, 1.0f),
        (WeenieClassName.daggerthrowing, 4.0f),
        (WeenieClassName.daggerthrowingacid, 1.0f),
        (WeenieClassName.daggerthrowingfire, 1.0f),
        (WeenieClassName.daggerthrowingfrost, 1.0f),
        (WeenieClassName.daggerthrowingelectric, 1.0f),
        (WeenieClassName.javelin, 4.0f),
        (WeenieClassName.javelinacid, 1.0f),
        (WeenieClassName.javelinfire, 1.0f),
        (WeenieClassName.javelinfrost, 1.0f),
        (WeenieClassName.javelinelectric, 1.0f),
        (WeenieClassName.shuriken, 4.0f),
        (WeenieClassName.shurikenacid, 1.0f),
        (WeenieClassName.shurikenfire, 1.0f),
        (WeenieClassName.shurikenfrost, 1.0f),
        (WeenieClassName.shurikenelectric, 1.0f),

        //( WeenieClassName.djarid,                    4.0f ),
        //( WeenieClassName.djaridacid,                1.0f ),
        //( WeenieClassName.djaridfire,                1.0f ),
        //( WeenieClassName.djaridfrost,               1.0f ),
        //( WeenieClassName.djaridelectric,            1.0f ),
    };

    private static ChanceTable<WeenieClassName> T5_Chances = new ChanceTable<WeenieClassName>(ChanceTableType.Weight)
    {
        (WeenieClassName.atlatl, 4.0f),
        (WeenieClassName.atlatlroyal, 4.0f),
        (WeenieClassName.atlatlslashing, 1.0f),
        (WeenieClassName.atlatlpiercing, 1.0f),
        (WeenieClassName.atlatlblunt, 1.0f),
        (WeenieClassName.atlatlacid, 1.0f),
        (WeenieClassName.atlatlfire, 1.0f),
        (WeenieClassName.atlatlfrost, 1.0f),
        (WeenieClassName.atlatlelectric, 1.0f),
        (WeenieClassName.dart, 4.0f),
        (WeenieClassName.dartacid, 1.0f),
        (WeenieClassName.dartflame, 1.0f),
        (WeenieClassName.dartfrost, 1.0f),
        (WeenieClassName.dartelectric, 1.0f),
        (WeenieClassName.axethrowing, 4.0f),
        (WeenieClassName.axethrowingacid, 1.0f),
        (WeenieClassName.axethrowingfire, 1.0f),
        (WeenieClassName.axethrowingfrost, 1.0f),
        (WeenieClassName.axethrowingelectric, 1.0f),
        (WeenieClassName.clubthrowing, 4.0f),
        (WeenieClassName.clubthrowingacid, 1.0f),
        (WeenieClassName.clubthrowingfire, 1.0f),
        (WeenieClassName.clubthrowingfrost, 1.0f),
        (WeenieClassName.clubthrowingelectric, 1.0f),
        (WeenieClassName.daggerthrowing, 4.0f),
        (WeenieClassName.daggerthrowingacid, 1.0f),
        (WeenieClassName.daggerthrowingfire, 1.0f),
        (WeenieClassName.daggerthrowingfrost, 1.0f),
        (WeenieClassName.daggerthrowingelectric, 1.0f),
        (WeenieClassName.javelin, 4.0f),
        (WeenieClassName.javelinacid, 1.0f),
        (WeenieClassName.javelinfire, 1.0f),
        (WeenieClassName.javelinfrost, 1.0f),
        (WeenieClassName.javelinelectric, 1.0f),
        (WeenieClassName.shuriken, 4.0f),
        (WeenieClassName.shurikenacid, 1.0f),
        (WeenieClassName.shurikenfire, 1.0f),
        (WeenieClassName.shurikenfrost, 1.0f),
        (WeenieClassName.shurikenelectric, 1.0f),

        //( WeenieClassName.djarid,                     4.0f ),
        //( WeenieClassName.djaridacid,                  1.0f ),
        //( WeenieClassName.djaridfire,                  1.0f ),
        //( WeenieClassName.djaridfrost,                 1.0f ),
        //( WeenieClassName.djaridelectric,              1.0f ),
    };

    private static ChanceTable<WeenieClassName> T6_T8_Chances = new ChanceTable<WeenieClassName>(ChanceTableType.Weight)
    {
        (WeenieClassName.atlatlslashing, 1.0f),
        (WeenieClassName.atlatlpiercing, 1.0f),
        (WeenieClassName.atlatlblunt, 1.0f),
        (WeenieClassName.atlatlacid, 1.0f),
        (WeenieClassName.atlatlfire, 1.0f),
        (WeenieClassName.atlatlfrost, 1.0f),
        (WeenieClassName.atlatlelectric, 1.0f),
        (WeenieClassName.dart, 1.0f),
        (WeenieClassName.dartacid, 1.0f),
        (WeenieClassName.dartflame, 1.0f),
        (WeenieClassName.dartfrost, 1.0f),
        (WeenieClassName.dartelectric, 1.0f),
        (WeenieClassName.axethrowing, 1.0f),
        (WeenieClassName.axethrowingacid, 1.0f),
        (WeenieClassName.axethrowingfire, 1.0f),
        (WeenieClassName.axethrowingfrost, 1.0f),
        (WeenieClassName.axethrowingelectric, 1.0f),
        (WeenieClassName.clubthrowing, 1.0f),
        (WeenieClassName.clubthrowingacid, 1.0f),
        (WeenieClassName.clubthrowingfire, 1.0f),
        (WeenieClassName.clubthrowingfrost, 1.0f),
        (WeenieClassName.clubthrowingelectric, 1.0f),
        (WeenieClassName.daggerthrowing, 1.0f),
        (WeenieClassName.daggerthrowingacid, 1.0f),
        (WeenieClassName.daggerthrowingfire, 1.0f),
        (WeenieClassName.daggerthrowingfrost, 1.0f),
        (WeenieClassName.daggerthrowingelectric, 1.0f),
        (WeenieClassName.javelin, 1.0f),
        (WeenieClassName.javelinacid, 1.0f),
        (WeenieClassName.javelinfire, 1.0f),
        (WeenieClassName.javelinfrost, 1.0f),
        (WeenieClassName.javelinelectric, 1.0f),
        (WeenieClassName.shuriken, 1.0f),
        (WeenieClassName.shurikenacid, 1.0f),
        (WeenieClassName.shurikenfire, 1.0f),
        (WeenieClassName.shurikenfrost, 1.0f),
        (WeenieClassName.shurikenelectric, 1.0f),

        //( WeenieClassName.djarid,                     1.0f ),
        //( WeenieClassName.djaridacid,                 1.0f ),
        //( WeenieClassName.djaridfire,                 1.0f ),
        //( WeenieClassName.djaridfrost,                1.0f ),
        //( WeenieClassName.djaridelectric,             1.0f ),
    };

    private static readonly List<ChanceTable<WeenieClassName>> atlatlTiers = new List<ChanceTable<WeenieClassName>>()
    {
        T1_T4_Chances,
        T1_T4_Chances,
        T1_T4_Chances,
        T1_T4_Chances,
        T5_Chances,
        T6_T8_Chances,
        T6_T8_Chances,
        T6_T8_Chances,
    };

    public static WeenieClassName Roll(int tier, out TreasureWeaponType weaponType)
    {
        var roll = atlatlTiers[tier - 1].Roll();

        switch (roll) // Modify weapon type so we get correct mutations.
        {
            case WeenieClassName.dart:
            case WeenieClassName.dartacid:
            case WeenieClassName.dartflame:
            case WeenieClassName.dartfrost:
            case WeenieClassName.dartelectric:
            case WeenieClassName.axethrowing:
            case WeenieClassName.axethrowingacid:
            case WeenieClassName.axethrowingfire:
            case WeenieClassName.axethrowingfrost:
            case WeenieClassName.axethrowingelectric:
            case WeenieClassName.clubthrowing:
            case WeenieClassName.clubthrowingacid:
            case WeenieClassName.clubthrowingfire:
            case WeenieClassName.clubthrowingfrost:
            case WeenieClassName.clubthrowingelectric:
            case WeenieClassName.daggerthrowing:
            case WeenieClassName.daggerthrowingacid:
            case WeenieClassName.daggerthrowingfire:
            case WeenieClassName.daggerthrowingfrost:
            case WeenieClassName.daggerthrowingelectric:
            case WeenieClassName.javelin:
            case WeenieClassName.javelinacid:
            case WeenieClassName.javelinfire:
            case WeenieClassName.javelinfrost:
            case WeenieClassName.javelinelectric:
            case WeenieClassName.shuriken:
            case WeenieClassName.shurikenacid:
            case WeenieClassName.shurikenfire:
            case WeenieClassName.shurikenfrost:
            case WeenieClassName.shurikenelectric:
            case WeenieClassName.djarid:
            case WeenieClassName.djaridacid:
            case WeenieClassName.djaridfire:
            case WeenieClassName.djaridfrost:
            case WeenieClassName.djaridelectric:
                weaponType = TreasureWeaponType.Thrown;
                break;
            case WeenieClassName.atlatl:
                weaponType = TreasureWeaponType.AtlatlRegular;
                break;
            default:
                weaponType = TreasureWeaponType.Atlatl;
                break;
        }

        return roll;
    }

    private static readonly Dictionary<WeenieClassName, TreasureWeaponType> _combined =
        new Dictionary<WeenieClassName, TreasureWeaponType>();

    static AtlatlWcids()
    {
        foreach (var atlatlTier in atlatlTiers)
        {
            foreach (var entry in atlatlTier)
            {
                _combined.TryAdd(entry.result, TreasureWeaponType.Atlatl);
            }
        }
    }

    public static bool TryGetValue(WeenieClassName wcid, out TreasureWeaponType weaponType)
    {
        return _combined.TryGetValue(wcid, out weaponType);
    }
}
