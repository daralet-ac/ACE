using System.Collections.Generic;
using ACE.Server.Factories.Entity;
using ACE.Server.Factories.Enum;

namespace ACE.Server.Factories.Tables.Wcids.Weapons;

public static class AtlatlWcids
{
    private static ChanceTable<WeenieClassName> T1_T4_Chances = new ChanceTable<WeenieClassName>(ChanceTableType.Weight)
    {
        (WeenieClassName.atlatl, 1.0f),
        (WeenieClassName.atlatlroyal, 1.0f)
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
        (WeenieClassName.atlatlelectric, 1.0f)
    };

    private static ChanceTable<WeenieClassName> T6_T8_Chances = new ChanceTable<WeenieClassName>(ChanceTableType.Weight)
    {
        (WeenieClassName.atlatlslashing, 1.0f),
        (WeenieClassName.atlatlpiercing, 1.0f),
        (WeenieClassName.atlatlblunt, 1.0f),
        (WeenieClassName.atlatlacid, 1.0f),
        (WeenieClassName.atlatlfire, 1.0f),
        (WeenieClassName.atlatlfrost, 1.0f),
        (WeenieClassName.atlatlelectric, 1.0f)
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
            case WeenieClassName.atlatl:
                weaponType = TreasureWeaponType.Atlatl;
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
