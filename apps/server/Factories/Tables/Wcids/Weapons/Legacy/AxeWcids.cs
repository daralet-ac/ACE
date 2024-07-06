using System.Collections.Generic;
using ACE.Server.Factories.Entity;
using ACE.Server.Factories.Enum;
using WeenieClassName = ACE.Server.Factories.Enum.WeenieClassName;

namespace ACE.Server.Factories.Tables.Wcids.Weapons.Legacy;

public static class AxeWcids
{
    private static ChanceTable<WeenieClassName> AxeWcids_All = new ChanceTable<WeenieClassName>(ChanceTableType.Weight)
    {
        (WeenieClassName.axehand, 4.0f),
        (WeenieClassName.axehandacid, 1.0f),
        (WeenieClassName.axehandelectric, 1.0f),
        (WeenieClassName.axehandfire, 1.0f),
        (WeenieClassName.axehandfrost, 1.0f),
        (WeenieClassName.axebattle, 4.0f),
        (WeenieClassName.axebattleacid, 1.0f),
        (WeenieClassName.axebattleelectric, 1.0f),
        (WeenieClassName.axebattlefire, 1.0f),
        (WeenieClassName.axebattlefrost, 1.0f),
        (WeenieClassName.warhammer, 4.0f),
        (WeenieClassName.warhammeracid, 1.0f),
        (WeenieClassName.warhammerelectric, 1.0f),
        (WeenieClassName.warhammerfire, 1.0f),
        (WeenieClassName.warhammerfrost, 1.0f),
        (WeenieClassName.ace41052_greataxe, 4.0f),
        (WeenieClassName.ace41053_acidgreataxe, 1.0f),
        (WeenieClassName.ace41054_lightninggreataxe, 1.0f),
        (WeenieClassName.ace41055_flaminggreataxe, 1.0f),
        (WeenieClassName.ace41056_frostgreataxe, 1.0f),
        (WeenieClassName.tungi, 4.0f),
        (WeenieClassName.tungiacid, 1.0f),
        (WeenieClassName.tungielectric, 1.0f),
        (WeenieClassName.tungifire, 1.0f),
        (WeenieClassName.tungifrost, 1.0f),
        (WeenieClassName.silifi, 4.0f),
        (WeenieClassName.silifiacid, 1.0f),
        (WeenieClassName.silifielectric, 1.0f),
        (WeenieClassName.silififire, 1.0f),
        (WeenieClassName.silififrost, 1.0f),
        (WeenieClassName.shouono, 4.0f),
        (WeenieClassName.shouonoacid, 1.0f),
        (WeenieClassName.shouonoelectric, 1.0f),
        (WeenieClassName.shouonofire, 1.0f),
        (WeenieClassName.shouonofrost, 1.0f),
        (WeenieClassName.ono, 4.0f),
        (WeenieClassName.onoacid, 1.0f),
        (WeenieClassName.onoelectric, 1.0f),
        (WeenieClassName.onofire, 1.0f),
        (WeenieClassName.onofrost, 1.0f),
    };

    private static ChanceTable<WeenieClassName> AxeWcids_Aluvian_T1 = new ChanceTable<WeenieClassName>(
        ChanceTableType.Weight
    )
    {
        (WeenieClassName.axehand, 1.0f),
        (WeenieClassName.axebattle, 1.0f),
        (WeenieClassName.warhammer, 1.0f),
        (WeenieClassName.ace41052_greataxe, 1.0f),
    };

    private static ChanceTable<WeenieClassName> AxeWcids_Aluvian = new ChanceTable<WeenieClassName>(
        ChanceTableType.Weight
    )
    {
        (WeenieClassName.axehand, 4.0f),
        (WeenieClassName.axehandacid, 1.0f),
        (WeenieClassName.axehandelectric, 1.0f),
        (WeenieClassName.axehandfire, 1.0f),
        (WeenieClassName.axehandfrost, 1.0f),
        (WeenieClassName.axebattle, 4.0f),
        (WeenieClassName.axebattleacid, 1.0f),
        (WeenieClassName.axebattleelectric, 1.0f),
        (WeenieClassName.axebattlefire, 1.0f),
        (WeenieClassName.axebattlefrost, 1.0f),
        (WeenieClassName.warhammer, 4.0f),
        (WeenieClassName.warhammeracid, 1.0f),
        (WeenieClassName.warhammerelectric, 1.0f),
        (WeenieClassName.warhammerfire, 1.0f),
        (WeenieClassName.warhammerfrost, 1.0f),
        (WeenieClassName.ace41052_greataxe, 4.0f),
        (WeenieClassName.ace41053_acidgreataxe, 1.0f),
        (WeenieClassName.ace41054_lightninggreataxe, 1.0f),
        (WeenieClassName.ace41055_flaminggreataxe, 1.0f),
        (WeenieClassName.ace41056_frostgreataxe, 1.0f),
    };

    private static ChanceTable<WeenieClassName> AxeWcids_Gharundim_T1 = new ChanceTable<WeenieClassName>(
        ChanceTableType.Weight
    )
    {
        (WeenieClassName.tungi, 1.0f),
        (WeenieClassName.silifi, 1.0f),
        (WeenieClassName.warhammer, 1.0f),
        (WeenieClassName.ace41052_greataxe, 1.0f),
    };

    private static ChanceTable<WeenieClassName> AxeWcids_Gharundim = new ChanceTable<WeenieClassName>(
        ChanceTableType.Weight
    )
    {
        (WeenieClassName.tungi, 4.0f),
        (WeenieClassName.tungiacid, 1.0f),
        (WeenieClassName.tungielectric, 1.0f),
        (WeenieClassName.tungifire, 1.0f),
        (WeenieClassName.tungifrost, 1.0f),
        (WeenieClassName.silifi, 4.0f),
        (WeenieClassName.silifiacid, 1.0f),
        (WeenieClassName.silifielectric, 1.0f),
        (WeenieClassName.silififire, 1.0f),
        (WeenieClassName.silififrost, 1.0f),
        (WeenieClassName.warhammer, 4.0f),
        (WeenieClassName.warhammeracid, 1.0f),
        (WeenieClassName.warhammerelectric, 1.0f),
        (WeenieClassName.warhammerfire, 1.0f),
        (WeenieClassName.warhammerfrost, 1.0f),
        (WeenieClassName.ace41052_greataxe, 4.0f),
        (WeenieClassName.ace41053_acidgreataxe, 1.0f),
        (WeenieClassName.ace41054_lightninggreataxe, 1.0f),
        (WeenieClassName.ace41055_flaminggreataxe, 1.0f),
        (WeenieClassName.ace41056_frostgreataxe, 1.0f),
    };

    private static ChanceTable<WeenieClassName> AxeWcids_Sho_T1 = new ChanceTable<WeenieClassName>(
        ChanceTableType.Weight
    )
    {
        (WeenieClassName.shouono, 1.0f),
        (WeenieClassName.ono, 1.0f),
        (WeenieClassName.warhammer, 1.0f),
        (WeenieClassName.ace41052_greataxe, 1.0f),
    };

    private static ChanceTable<WeenieClassName> AxeWcids_Sho = new ChanceTable<WeenieClassName>(ChanceTableType.Weight)
    {
        (WeenieClassName.shouono, 4.0f),
        (WeenieClassName.shouonoacid, 1.0f),
        (WeenieClassName.shouonoelectric, 1.0f),
        (WeenieClassName.shouonofire, 1.0f),
        (WeenieClassName.shouonofrost, 1.0f),
        (WeenieClassName.ono, 4.0f),
        (WeenieClassName.onoacid, 1.0f),
        (WeenieClassName.onoelectric, 1.0f),
        (WeenieClassName.onofire, 1.0f),
        (WeenieClassName.onofrost, 1.0f),
        (WeenieClassName.warhammer, 4.0f),
        (WeenieClassName.warhammeracid, 1.0f),
        (WeenieClassName.warhammerelectric, 1.0f),
        (WeenieClassName.warhammerfire, 1.0f),
        (WeenieClassName.warhammerfrost, 1.0f),
        (WeenieClassName.ace41052_greataxe, 4.0f),
        (WeenieClassName.ace41053_acidgreataxe, 1.0f),
        (WeenieClassName.ace41054_lightninggreataxe, 1.0f),
        (WeenieClassName.ace41055_flaminggreataxe, 1.0f),
        (WeenieClassName.ace41056_frostgreataxe, 1.0f),
    };

    public static WeenieClassName Roll(TreasureHeritageGroup heritage, int tier, out TreasureWeaponType weaponType)
    {
        WeenieClassName weapon;

        switch (heritage)
        {
            case TreasureHeritageGroup.Aluvian:
                if (tier > 1)
                {
                    weapon = AxeWcids_Aluvian.Roll();
                }
                else
                {
                    weapon = AxeWcids_Aluvian_T1.Roll();
                }

                break;
            case TreasureHeritageGroup.Gharundim:
                if (tier > 1)
                {
                    weapon = AxeWcids_Gharundim.Roll();
                }
                else
                {
                    weapon = AxeWcids_Gharundim_T1.Roll();
                }

                break;
            case TreasureHeritageGroup.Sho:
                if (tier > 1)
                {
                    weapon = AxeWcids_Sho.Roll();
                }
                else
                {
                    weapon = AxeWcids_Sho_T1.Roll();
                }

                break;
            default:
                weapon = AxeWcids_All.Roll();
                break;
        }

        switch (weapon)
        {
            case WeenieClassName.ace41052_greataxe:
            case WeenieClassName.ace41053_acidgreataxe:
            case WeenieClassName.ace41054_lightninggreataxe:
            case WeenieClassName.ace41055_flaminggreataxe:
            case WeenieClassName.ace41056_frostgreataxe:
                weaponType = TreasureWeaponType.TwoHandedAxe;
                break;
            default:
                weaponType = TreasureWeaponType.Axe;
                break;
        }

        return weapon;
    }

    private static readonly Dictionary<WeenieClassName, TreasureWeaponType> _combined =
        new Dictionary<WeenieClassName, TreasureWeaponType>();

    static AxeWcids()
    {
        foreach (var entry in AxeWcids_Aluvian_T1)
        {
            _combined.TryAdd(entry.result, TreasureWeaponType.Axe);
        }

        foreach (var entry in AxeWcids_Aluvian)
        {
            _combined.TryAdd(entry.result, TreasureWeaponType.Axe);
        }

        foreach (var entry in AxeWcids_Gharundim_T1)
        {
            _combined.TryAdd(entry.result, TreasureWeaponType.Axe);
        }

        foreach (var entry in AxeWcids_Gharundim)
        {
            _combined.TryAdd(entry.result, TreasureWeaponType.Axe);
        }

        foreach (var entry in AxeWcids_Sho_T1)
        {
            _combined.TryAdd(entry.result, TreasureWeaponType.Axe);
        }

        foreach (var entry in AxeWcids_Sho)
        {
            _combined.TryAdd(entry.result, TreasureWeaponType.Axe);
        }
    }

    public static bool TryGetValue(WeenieClassName wcid, out TreasureWeaponType weaponType)
    {
        return _combined.TryGetValue(wcid, out weaponType);
    }
}
