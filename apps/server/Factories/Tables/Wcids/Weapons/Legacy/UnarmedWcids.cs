using System.Collections.Generic;
using ACE.Server.Factories.Entity;
using ACE.Server.Factories.Enum;
using WeenieClassName = ACE.Server.Factories.Enum.WeenieClassName;

namespace ACE.Server.Factories.Tables.Wcids;

public static class UnarmedWcids
{
    private static ChanceTable<WeenieClassName> UnarmedWcids_All = new ChanceTable<WeenieClassName>(
        ChanceTableType.Weight
    )
    {
        (WeenieClassName.cestus, 4.0f),
        (WeenieClassName.cestusacid, 1.0f),
        (WeenieClassName.cestuselectric, 1.0f),
        (WeenieClassName.cestusfire, 1.0f),
        (WeenieClassName.cestusfrost, 1.0f),
        (WeenieClassName.katar, 4.0f),
        (WeenieClassName.kataracid, 1.0f),
        (WeenieClassName.katarelectric, 1.0f),
        (WeenieClassName.katarfire, 1.0f),
        (WeenieClassName.katarfrost, 1.0f),
        (WeenieClassName.nekode, 4.0f),
        (WeenieClassName.nekodeacid, 1.0f),
        (WeenieClassName.nekodeelectric, 1.0f),
        (WeenieClassName.nekodefire, 1.0f),
        (WeenieClassName.nekodefrost, 1.0f),
    };

    private static ChanceTable<WeenieClassName> UnarmedWcids_Aluvian = new ChanceTable<WeenieClassName>(
        ChanceTableType.Weight
    )
    {
        (WeenieClassName.cestus, 0.40f),
        (WeenieClassName.cestusacid, 0.15f),
        (WeenieClassName.cestuselectric, 0.15f),
        (WeenieClassName.cestusfire, 0.15f),
        (WeenieClassName.cestusfrost, 0.15f),
    };

    private static ChanceTable<WeenieClassName> UnarmedWcids_Gharundim = new ChanceTable<WeenieClassName>(
        ChanceTableType.Weight
    )
    {
        (WeenieClassName.katar, 0.40f),
        (WeenieClassName.kataracid, 0.15f),
        (WeenieClassName.katarelectric, 0.15f),
        (WeenieClassName.katarfire, 0.15f),
        (WeenieClassName.katarfrost, 0.15f),
    };

    private static ChanceTable<WeenieClassName> UnarmedWcids_Sho = new ChanceTable<WeenieClassName>(
        ChanceTableType.Weight
    )
    {
        (WeenieClassName.nekode, 0.40f),
        (WeenieClassName.nekodeacid, 0.15f),
        (WeenieClassName.nekodeelectric, 0.15f),
        (WeenieClassName.nekodefire, 0.15f),
        (WeenieClassName.nekodefrost, 0.15f),
    };

    public static WeenieClassName Roll(TreasureHeritageGroup heritage, int tier)
    {
        switch (heritage)
        {
            case TreasureHeritageGroup.Aluvian:
                return UnarmedWcids_Aluvian.Roll();

            case TreasureHeritageGroup.Gharundim:
                return UnarmedWcids_Gharundim.Roll();

            case TreasureHeritageGroup.Sho:
                return UnarmedWcids_Sho.Roll();

            default:
                return UnarmedWcids_All.Roll();
        }
    }

    private static readonly Dictionary<WeenieClassName, TreasureWeaponType> _combined =
        new Dictionary<WeenieClassName, TreasureWeaponType>();

    public static bool TryGetValue(WeenieClassName wcid, out TreasureWeaponType weaponType)
    {
        return _combined.TryGetValue(wcid, out weaponType);
    }
}
