using System.Collections.Generic;
using ACE.Server.Factories.Entity;
using ACE.Server.Factories.Enum;
using WeenieClassName = ACE.Server.Factories.Enum.WeenieClassName;

namespace ACE.Server.Factories.Tables.Wcids.Weapons.Legacy;

public static class SpearWcids
{
    private static ChanceTable<WeenieClassName> SpearWcids_All = new ChanceTable<WeenieClassName>(
        ChanceTableType.Weight
    )
    {
        (WeenieClassName.spear, 4.0f),
        (WeenieClassName.spearacid, 1.0f),
        (WeenieClassName.spearelectric, 1.0f),
        (WeenieClassName.spearflame, 1.0f),
        (WeenieClassName.spearfrost, 1.0f),
        (WeenieClassName.trident, 4.0f),
        (WeenieClassName.tridentacid, 1.0f),
        (WeenieClassName.tridentelectric, 1.0f),
        (WeenieClassName.tridentfire, 1.0f),
        (WeenieClassName.tridentfrost, 1.0f),
        (WeenieClassName.budiaq, 4.0f),
        (WeenieClassName.budiaqacid, 1.0f),
        (WeenieClassName.budiaqelectric, 1.0f),
        (WeenieClassName.budiaqfire, 1.0f),
        (WeenieClassName.budiaqfrost, 1.0f),
        (WeenieClassName.yari, 4.0f),
        (WeenieClassName.yariacid, 1.0f),
        (WeenieClassName.yarielectric, 1.0f),
        (WeenieClassName.yarifire, 1.0f),
        (WeenieClassName.yarifrost, 1.0f),
        (WeenieClassName.ace41041_magariyari, 4.0f),
        (WeenieClassName.ace41042_acidmagariyari, 1.0f),
        (WeenieClassName.ace41043_lightningmagariyari, 1.0f),
        (WeenieClassName.ace41044_flamingmagariyari, 1.0f),
        (WeenieClassName.ace41045_frostmagariyari, 1.0f),
        (WeenieClassName.ace41036_assagai, 4.0f),
        (WeenieClassName.ace41037_acidassagai, 1.0f),
        (WeenieClassName.ace41038_lightningassagai, 1.0f),
        (WeenieClassName.ace41039_flamingassagai, 1.0f),
        (WeenieClassName.ace41040_frostassagai, 1.0f),
        (WeenieClassName.ace41046_pike, 4.0f),
        (WeenieClassName.ace41047_acidpike, 1.0f),
        (WeenieClassName.ace41048_lightningpike, 1.0f),
        (WeenieClassName.ace41049_flamingpike, 1.0f),
        (WeenieClassName.ace41050_frostpike, 1.0f),
    };

    private static ChanceTable<WeenieClassName> SpearWcids_Aluvian_T1 = new ChanceTable<WeenieClassName>(
        ChanceTableType.Weight
    )
    {
        (WeenieClassName.spear, 3.0f),
        (WeenieClassName.trident, 0.5f),
        (WeenieClassName.swordstaff, 0.5f),
        (WeenieClassName.ace41046_pike, 0.5f),
    };

    private static ChanceTable<WeenieClassName> SpearWcids_Aluvian = new ChanceTable<WeenieClassName>(
        ChanceTableType.Weight
    )
    {
        (WeenieClassName.spear, 4.0f),
        (WeenieClassName.spearacid, 1.0f),
        (WeenieClassName.spearelectric, 1.0f),
        (WeenieClassName.spearflame, 1.0f),
        (WeenieClassName.spearfrost, 1.0f),
        (WeenieClassName.trident, 4.0f),
        (WeenieClassName.tridentacid, 1.0f),
        (WeenieClassName.tridentelectric, 1.0f),
        (WeenieClassName.tridentfire, 1.0f),
        (WeenieClassName.tridentfrost, 1.0f),
        (WeenieClassName.ace41046_pike, 4.0f),
        (WeenieClassName.ace41047_acidpike, 1.0f),
        (WeenieClassName.ace41048_lightningpike, 1.0f),
        (WeenieClassName.ace41049_flamingpike, 1.0f),
        (WeenieClassName.ace41050_frostpike, 1.0f),
    };

    private static ChanceTable<WeenieClassName> SpearWcids_Gharundim_T1 = new ChanceTable<WeenieClassName>(
        ChanceTableType.Weight
    )
    {
        (WeenieClassName.budiaq, 3.0f),
        (WeenieClassName.trident, 0.5f),
        (WeenieClassName.swordstaff, 0.5f),
        (WeenieClassName.ace41036_assagai, 0.5f),
    };

    private static ChanceTable<WeenieClassName> SpearWcids_Gharundim = new ChanceTable<WeenieClassName>(
        ChanceTableType.Weight
    )
    {
        (WeenieClassName.budiaq, 4.0f),
        (WeenieClassName.budiaqacid, 1.0f),
        (WeenieClassName.budiaqelectric, 1.0f),
        (WeenieClassName.budiaqfire, 1.0f),
        (WeenieClassName.budiaqfrost, 1.0f),
        (WeenieClassName.trident, 4.0f),
        (WeenieClassName.tridentacid, 1.0f),
        (WeenieClassName.tridentelectric, 1.0f),
        (WeenieClassName.tridentfire, 1.0f),
        (WeenieClassName.tridentfrost, 1.0f),
        (WeenieClassName.ace41036_assagai, 4.0f),
        (WeenieClassName.ace41037_acidassagai, 1.0f),
        (WeenieClassName.ace41038_lightningassagai, 1.0f),
        (WeenieClassName.ace41039_flamingassagai, 1.0f),
        (WeenieClassName.ace41040_frostassagai, 1.0f),
    };

    private static ChanceTable<WeenieClassName> SpearWcids_Sho_T1 = new ChanceTable<WeenieClassName>(
        ChanceTableType.Weight
    )
    {
        (WeenieClassName.yari, 3.0f),
        (WeenieClassName.trident, 0.5f),
        (WeenieClassName.swordstaff, 0.5f),
        (WeenieClassName.ace41041_magariyari, 0.5f),
    };

    private static ChanceTable<WeenieClassName> SpearWcids_Sho = new ChanceTable<WeenieClassName>(
        ChanceTableType.Weight
    )
    {
        (WeenieClassName.yari, 4.0f),
        (WeenieClassName.yariacid, 1.0f),
        (WeenieClassName.yarielectric, 1.0f),
        (WeenieClassName.yarifire, 1.0f),
        (WeenieClassName.yarifrost, 1.0f),
        (WeenieClassName.trident, 4.0f),
        (WeenieClassName.tridentacid, 1.0f),
        (WeenieClassName.tridentelectric, 1.0f),
        (WeenieClassName.tridentfire, 1.0f),
        (WeenieClassName.tridentfrost, 1.0f),
        (WeenieClassName.ace41041_magariyari, 4.0f),
        (WeenieClassName.ace41042_acidmagariyari, 1.0f),
        (WeenieClassName.ace41043_lightningmagariyari, 1.0f),
        (WeenieClassName.ace41044_flamingmagariyari, 1.0f),
        (WeenieClassName.ace41045_frostmagariyari, 1.0f),
    };

    public static WeenieClassName Roll(TreasureHeritageGroup heritage, int tier, out TreasureWeaponType weaponType)
    {
        WeenieClassName weapon;

        switch (heritage)
        {
            case TreasureHeritageGroup.Aluvian:
                if (tier > 1)
                {
                    weapon = SpearWcids_Aluvian.Roll();
                }
                else
                {
                    weapon = SpearWcids_Aluvian_T1.Roll();
                }

                break;
            case TreasureHeritageGroup.Gharundim:
                if (tier > 1)
                {
                    weapon = SpearWcids_Gharundim.Roll();
                }
                else
                {
                    weapon = SpearWcids_Gharundim_T1.Roll();
                }

                break;
            case TreasureHeritageGroup.Sho:
                if (tier > 1)
                {
                    weapon = SpearWcids_Sho.Roll();
                }
                else
                {
                    weapon = SpearWcids_Sho_T1.Roll();
                }

                break;
            default:
                weapon = SpearWcids_All.Roll();
                break;
        }

        switch (weapon)
        {
            case WeenieClassName.ace41046_pike:
            case WeenieClassName.ace41047_acidpike:
            case WeenieClassName.ace41048_lightningpike:
            case WeenieClassName.ace41049_flamingpike:
            case WeenieClassName.ace41050_frostpike:
            case WeenieClassName.ace41036_assagai:
            case WeenieClassName.ace41037_acidassagai:
            case WeenieClassName.ace41038_lightningassagai:
            case WeenieClassName.ace41039_flamingassagai:
            case WeenieClassName.ace41040_frostassagai:
            case WeenieClassName.ace41041_magariyari:
            case WeenieClassName.ace41042_acidmagariyari:
            case WeenieClassName.ace41043_lightningmagariyari:
            case WeenieClassName.ace41044_flamingmagariyari:
            case WeenieClassName.ace41045_frostmagariyari:
                weaponType = TreasureWeaponType.TwoHandedMace;
                break;
            default:
                weaponType = TreasureWeaponType.Spear;
                break;
        }

        return weapon;
    }

    private static readonly Dictionary<WeenieClassName, TreasureWeaponType> _combined =
        new Dictionary<WeenieClassName, TreasureWeaponType>();

    static SpearWcids()
    {
        foreach (var entry in SpearWcids_Aluvian_T1)
        {
            _combined.TryAdd(entry.result, TreasureWeaponType.Spear);
        }

        foreach (var entry in SpearWcids_Aluvian)
        {
            _combined.TryAdd(entry.result, TreasureWeaponType.Spear);
        }

        foreach (var entry in SpearWcids_Gharundim_T1)
        {
            _combined.TryAdd(entry.result, TreasureWeaponType.Spear);
        }

        foreach (var entry in SpearWcids_Gharundim)
        {
            _combined.TryAdd(entry.result, TreasureWeaponType.Spear);
        }

        foreach (var entry in SpearWcids_Sho_T1)
        {
            _combined.TryAdd(entry.result, TreasureWeaponType.Spear);
        }

        foreach (var entry in SpearWcids_Sho)
        {
            _combined.TryAdd(entry.result, TreasureWeaponType.Spear);
        }
    }

    public static bool TryGetValue(WeenieClassName wcid, out TreasureWeaponType weaponType)
    {
        return _combined.TryGetValue(wcid, out weaponType);
    }
}
