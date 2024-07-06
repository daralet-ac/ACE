using System.Collections.Generic;
using ACE.Server.Factories.Entity;
using ACE.Server.Factories.Enum;
using WeenieClassName = ACE.Server.Factories.Enum.WeenieClassName;

namespace ACE.Server.Factories.Tables.Wcids.Weapons.Legacy;

public static class DaggerWcids
{
    private static ChanceTable<WeenieClassName> DaggerWcids_All = new ChanceTable<WeenieClassName>(
        ChanceTableType.Weight
    )
    {
        (WeenieClassName.knife, 4.0f),
        (WeenieClassName.knifeacid, 1.0f),
        (WeenieClassName.knifeelectric, 1.0f),
        (WeenieClassName.knifefire, 1.0f),
        (WeenieClassName.knifefrost, 1.0f),
        (WeenieClassName.dagger, 4.0f),
        (WeenieClassName.daggeracid, 1.0f),
        (WeenieClassName.daggerelectric, 1.0f),
        (WeenieClassName.daggerfire, 1.0f),
        (WeenieClassName.daggerfrost, 1.0f),
        (WeenieClassName.dirk, 4.0f),
        (WeenieClassName.dirkacid, 1.0f),
        (WeenieClassName.dirkelectric, 1.0f),
        (WeenieClassName.dirkfire, 1.0f),
        (WeenieClassName.dirkfrost, 1.0f),
        (WeenieClassName.jambiya, 4.0f),
        (WeenieClassName.jambiyaacid, 1.0f),
        (WeenieClassName.jambiyaelectric, 1.0f),
        (WeenieClassName.jambiyafire, 1.0f),
        (WeenieClassName.jambiyafrost, 1.0f),
        (WeenieClassName.khanjar, 4.0f),
        (WeenieClassName.khanjaracid, 1.0f),
        (WeenieClassName.khanjarelectric, 1.0f),
        (WeenieClassName.khanjarfire, 1.0f),
        (WeenieClassName.khanjarfrost, 1.0f),
        (WeenieClassName.jitte, 4.0f),
        (WeenieClassName.jitteacid, 1.0f),
        (WeenieClassName.jitteelectric, 1.0f),
        (WeenieClassName.jittefire, 1.0f),
        (WeenieClassName.jittefrost, 1.0f),
    };

    private static ChanceTable<WeenieClassName> DaggerWcids_Aluvian_T1 = new ChanceTable<WeenieClassName>(
        ChanceTableType.Weight
    )
    {
        (WeenieClassName.knife, 1.0f),
        (WeenieClassName.dagger, 1.0f),
        (WeenieClassName.dirk, 1.0f),
    };

    private static ChanceTable<WeenieClassName> DaggerWcids_Aluvian = new ChanceTable<WeenieClassName>(
        ChanceTableType.Weight
    )
    {
        (WeenieClassName.knife, 4.0f),
        (WeenieClassName.knifeacid, 1.0f),
        (WeenieClassName.knifeelectric, 1.0f),
        (WeenieClassName.knifefire, 1.0f),
        (WeenieClassName.knifefrost, 1.0f),
        (WeenieClassName.dagger, 4.0f),
        (WeenieClassName.daggeracid, 1.0f),
        (WeenieClassName.daggerelectric, 1.0f),
        (WeenieClassName.daggerfire, 1.0f),
        (WeenieClassName.daggerfrost, 1.0f),
        (WeenieClassName.dirk, 4.0f),
        (WeenieClassName.dirkacid, 1.0f),
        (WeenieClassName.dirkelectric, 1.0f),
        (WeenieClassName.dirkfire, 1.0f),
        (WeenieClassName.dirkfrost, 1.0f),
    };

    private static ChanceTable<WeenieClassName> DaggerWcids_Gharundim_T1 = new ChanceTable<WeenieClassName>(
        ChanceTableType.Weight
    )
    {
        (WeenieClassName.jambiya, 1.0f),
        (WeenieClassName.khanjar, 1.0f),
        (WeenieClassName.dirk, 1.0f),
    };

    private static ChanceTable<WeenieClassName> DaggerWcids_Gharundim = new ChanceTable<WeenieClassName>(
        ChanceTableType.Weight
    )
    {
        (WeenieClassName.jambiya, 4.0f),
        (WeenieClassName.jambiyaacid, 1.0f),
        (WeenieClassName.jambiyaelectric, 1.0f),
        (WeenieClassName.jambiyafire, 1.0f),
        (WeenieClassName.jambiyafrost, 1.0f),
        (WeenieClassName.khanjar, 4.0f),
        (WeenieClassName.khanjaracid, 1.0f),
        (WeenieClassName.khanjarelectric, 1.0f),
        (WeenieClassName.khanjarfire, 1.0f),
        (WeenieClassName.khanjarfrost, 1.0f),
        (WeenieClassName.dirk, 4.0f),
        (WeenieClassName.dirkacid, 1.0f),
        (WeenieClassName.dirkelectric, 1.0f),
        (WeenieClassName.dirkfire, 1.0f),
        (WeenieClassName.dirkfrost, 1.0f),
    };

    private static ChanceTable<WeenieClassName> DaggerWcids_Sho_T1 = new ChanceTable<WeenieClassName>(
        ChanceTableType.Weight
    )
    {
        (WeenieClassName.knife, 1.0f),
        (WeenieClassName.dagger, 1.0f),
        (WeenieClassName.jitte, 1.0f),
    };

    private static ChanceTable<WeenieClassName> DaggerWcids_Sho = new ChanceTable<WeenieClassName>(
        ChanceTableType.Weight
    )
    {
        (WeenieClassName.knife, 4.0f),
        (WeenieClassName.knifeacid, 1.0f),
        (WeenieClassName.knifeelectric, 1.0f),
        (WeenieClassName.knifefire, 1.0f),
        (WeenieClassName.knifefrost, 1.0f),
        (WeenieClassName.dagger, 4.0f),
        (WeenieClassName.daggeracid, 1.0f),
        (WeenieClassName.daggerelectric, 1.0f),
        (WeenieClassName.daggerfire, 1.0f),
        (WeenieClassName.daggerfrost, 1.0f),
        (WeenieClassName.dirk, 4.0f),
        (WeenieClassName.dirkacid, 1.0f),
        (WeenieClassName.dirkelectric, 1.0f),
        (WeenieClassName.dirkfire, 1.0f),
        (WeenieClassName.dirkfrost, 1.0f),
        (WeenieClassName.jitte, 4.0f),
        (WeenieClassName.jitteacid, 1.0f),
        (WeenieClassName.jitteelectric, 1.0f),
        (WeenieClassName.jittefire, 1.0f),
        (WeenieClassName.jittefrost, 1.0f),
    };

    public static WeenieClassName Roll(TreasureHeritageGroup heritage, int tier, out TreasureWeaponType weaponType)
    {
        WeenieClassName weapon;

        switch (heritage)
        {
            case TreasureHeritageGroup.Aluvian:
                if (tier > 1)
                {
                    weapon = DaggerWcids_Aluvian.Roll();
                }
                else
                {
                    weapon = DaggerWcids_Aluvian_T1.Roll();
                }

                break;
            case TreasureHeritageGroup.Gharundim:
                if (tier > 1)
                {
                    weapon = DaggerWcids_Gharundim.Roll();
                }
                else
                {
                    weapon = DaggerWcids_Gharundim_T1.Roll();
                }

                break;
            case TreasureHeritageGroup.Sho:
                if (tier > 1)
                {
                    weapon = DaggerWcids_Sho.Roll();
                }
                else
                {
                    weapon = DaggerWcids_Sho_T1.Roll();
                }

                break;
            default:
                weapon = DaggerWcids_All.Roll();
                break;
        }

        weaponType = TreasureWeaponType.Dagger;

        return weapon;
    }

    private static readonly Dictionary<WeenieClassName, TreasureWeaponType> _combined =
        new Dictionary<WeenieClassName, TreasureWeaponType>();

    static DaggerWcids()
    {
        foreach (var entry in DaggerWcids_Aluvian_T1)
        {
            _combined.TryAdd(entry.result, TreasureWeaponType.Dagger);
        }

        foreach (var entry in DaggerWcids_Aluvian)
        {
            _combined.TryAdd(entry.result, TreasureWeaponType.Dagger);
        }

        foreach (var entry in DaggerWcids_Gharundim_T1)
        {
            _combined.TryAdd(entry.result, TreasureWeaponType.Dagger);
        }

        foreach (var entry in DaggerWcids_Gharundim)
        {
            _combined.TryAdd(entry.result, TreasureWeaponType.Dagger);
        }

        foreach (var entry in DaggerWcids_Sho_T1)
        {
            _combined.TryAdd(entry.result, TreasureWeaponType.Dagger);
        }

        foreach (var entry in DaggerWcids_Sho)
        {
            _combined.TryAdd(entry.result, TreasureWeaponType.Dagger);
        }
    }

    public static bool TryGetValue(WeenieClassName wcid, out TreasureWeaponType weaponType)
    {
        return _combined.TryGetValue(wcid, out weaponType);
    }
}
