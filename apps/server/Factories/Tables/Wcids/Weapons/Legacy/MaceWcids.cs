using System.Collections.Generic;
using ACE.Server.Factories.Entity;
using ACE.Server.Factories.Enum;
using WeenieClassName = ACE.Server.Factories.Enum.WeenieClassName;

namespace ACE.Server.Factories.Tables.Wcids;

public static class MaceWcids
{
    private static ChanceTable<WeenieClassName> MaceWcids_All = new ChanceTable<WeenieClassName>(ChanceTableType.Weight)
    {
        (WeenieClassName.club, 4.0f),
        (WeenieClassName.clubacid, 1.0f),
        (WeenieClassName.clubelectric, 1.0f),
        (WeenieClassName.clubfire, 1.0f),
        (WeenieClassName.clubfrost, 1.0f),
        (WeenieClassName.mace, 4.0f),
        (WeenieClassName.maceacid, 1.0f),
        (WeenieClassName.maceelectric, 1.0f),
        (WeenieClassName.macefire, 1.0f),
        (WeenieClassName.macefrost, 1.0f),
        (WeenieClassName.morningstar, 4.0f),
        (WeenieClassName.morningstaracid, 1.0f),
        (WeenieClassName.morningstarelectric, 1.0f),
        (WeenieClassName.morningstarfire, 1.0f),
        (WeenieClassName.morningstarfrost, 1.0f),
        (WeenieClassName.clubspiked, 4.0f),
        (WeenieClassName.clubspikedacid, 1.0f),
        (WeenieClassName.clubspikedelectric, 1.0f),
        (WeenieClassName.clubspikedfire, 1.0f),
        (WeenieClassName.clubspikedfrost, 1.0f),
        (WeenieClassName.tofun, 4.0f),
        (WeenieClassName.tofunacid, 1.0f),
        (WeenieClassName.tofunelectric, 1.0f),
        (WeenieClassName.tofunfire, 1.0f),
        (WeenieClassName.tofunfrost, 1.0f),
        (WeenieClassName.kasrullah, 4.0f),
        (WeenieClassName.kasrullahacid, 1.0f),
        (WeenieClassName.kasrullahelectric, 1.0f),
        (WeenieClassName.kasrullahfire, 1.0f),
        (WeenieClassName.kasrullahfrost, 1.0f),
        (WeenieClassName.dabus, 4.0f),
        (WeenieClassName.dabusacid, 1.0f),
        (WeenieClassName.dabuselectric, 1.0f),
        (WeenieClassName.dabusfire, 1.0f),
        (WeenieClassName.dabusfrost, 1.0f),
        (WeenieClassName.ace41062_khandahandledmace, 4.0f),
        (WeenieClassName.ace41063_acidkhandahandledmace, 1.0f),
        (WeenieClassName.ace41064_lightningkhandahandledmace, 1.0f),
        (WeenieClassName.ace41065_flamingkhandahandledmace, 1.0f),
        (WeenieClassName.ace41066_frostkhandahandledmace, 1.0f),
        (WeenieClassName.ace40635_tetsubo, 4.0f),
        (WeenieClassName.ace40636_acidtetsubo, 1.0f),
        (WeenieClassName.ace40637_lightningtetsubo, 1.0f),
        (WeenieClassName.ace40638_flamingtetsubo, 1.0f),
        (WeenieClassName.ace40639_frosttetsubo, 1.0f),
        (WeenieClassName.ace41057_greatstarmace, 4.0f),
        (WeenieClassName.ace41058_acidgreatstarmace, 1.0f),
        (WeenieClassName.ace41059_lightninggreatstarmace, 1.0f),
        (WeenieClassName.ace41060_flaminggreatstarmace, 1.0f),
        (WeenieClassName.ace41061_frostgreatstarmace, 1.0f),
    };

    private static ChanceTable<WeenieClassName> MaceWcids_Aluvian_T1 = new ChanceTable<WeenieClassName>(
        ChanceTableType.Weight
    )
    {
        (WeenieClassName.clubspiked, 3.0f),
        (WeenieClassName.club, 0.50f),
        (WeenieClassName.mace, 0.50f),
        (WeenieClassName.morningstar, 0.50f),
        (WeenieClassName.ace41057_greatstarmace, 0.50f),
    };

    private static ChanceTable<WeenieClassName> MaceWcids_Aluvian = new ChanceTable<WeenieClassName>(
        ChanceTableType.Weight
    )
    {
        (WeenieClassName.club, 4.0f),
        (WeenieClassName.clubacid, 1.0f),
        (WeenieClassName.clubelectric, 1.0f),
        (WeenieClassName.clubfire, 1.0f),
        (WeenieClassName.clubfrost, 1.0f),
        (WeenieClassName.mace, 4.0f),
        (WeenieClassName.maceacid, 1.0f),
        (WeenieClassName.maceelectric, 1.0f),
        (WeenieClassName.macefire, 1.0f),
        (WeenieClassName.macefrost, 1.0f),
        (WeenieClassName.morningstar, 4.0f),
        (WeenieClassName.morningstaracid, 1.0f),
        (WeenieClassName.morningstarelectric, 1.0f),
        (WeenieClassName.morningstarfire, 1.0f),
        (WeenieClassName.morningstarfrost, 1.0f),
        (WeenieClassName.clubspiked, 4.0f),
        (WeenieClassName.clubspikedacid, 1.0f),
        (WeenieClassName.clubspikedelectric, 1.0f),
        (WeenieClassName.clubspikedfire, 1.0f),
        (WeenieClassName.clubspikedfrost, 1.0f),
        (WeenieClassName.ace41057_greatstarmace, 4.0f),
        (WeenieClassName.ace41058_acidgreatstarmace, 1.0f),
        (WeenieClassName.ace41059_lightninggreatstarmace, 1.0f),
        (WeenieClassName.ace41060_flaminggreatstarmace, 1.0f),
        (WeenieClassName.ace41061_frostgreatstarmace, 1.0f),
    };

    private static ChanceTable<WeenieClassName> MaceWcids_Gharundim_T1 = new ChanceTable<WeenieClassName>(
        ChanceTableType.Weight
    )
    {
        (WeenieClassName.clubspiked, 3.0f),
        (WeenieClassName.kasrullah, 0.50f),
        (WeenieClassName.dabus, 0.50f),
        (WeenieClassName.morningstar, 0.50f),
        (WeenieClassName.ace41062_khandahandledmace, 0.50f),
    };

    private static ChanceTable<WeenieClassName> MaceWcids_Gharundim = new ChanceTable<WeenieClassName>(
        ChanceTableType.Weight
    )
    {
        (WeenieClassName.kasrullah, 4.0f),
        (WeenieClassName.kasrullahacid, 1.0f),
        (WeenieClassName.kasrullahelectric, 1.0f),
        (WeenieClassName.kasrullahfire, 1.0f),
        (WeenieClassName.kasrullahfrost, 1.0f),
        (WeenieClassName.dabus, 4.0f),
        (WeenieClassName.dabusacid, 1.0f),
        (WeenieClassName.dabuselectric, 1.0f),
        (WeenieClassName.dabusfire, 1.0f),
        (WeenieClassName.dabusfrost, 1.0f),
        (WeenieClassName.morningstar, 4.0f),
        (WeenieClassName.morningstaracid, 1.0f),
        (WeenieClassName.morningstarelectric, 1.0f),
        (WeenieClassName.morningstarfire, 1.0f),
        (WeenieClassName.morningstarfrost, 1.0f),
        (WeenieClassName.clubspiked, 4.0f),
        (WeenieClassName.clubspikedacid, 1.0f),
        (WeenieClassName.clubspikedelectric, 1.0f),
        (WeenieClassName.clubspikedfire, 1.0f),
        (WeenieClassName.clubspikedfrost, 1.0f),
        (WeenieClassName.ace41062_khandahandledmace, 4.0f),
        (WeenieClassName.ace41063_acidkhandahandledmace, 1.0f),
        (WeenieClassName.ace41064_lightningkhandahandledmace, 1.0f),
        (WeenieClassName.ace41065_flamingkhandahandledmace, 1.0f),
        (WeenieClassName.ace41066_frostkhandahandledmace, 1.0f),
    };

    private static ChanceTable<WeenieClassName> MaceWcids_Sho_T1 = new ChanceTable<WeenieClassName>(
        ChanceTableType.Weight
    )
    {
        (WeenieClassName.clubspiked, 3.0f),
        (WeenieClassName.tofun, 0.50f),
        (WeenieClassName.morningstar, 0.50f),
        (WeenieClassName.ace40635_tetsubo, 0.50f),
    };

    private static ChanceTable<WeenieClassName> MaceWcids_Sho = new ChanceTable<WeenieClassName>(ChanceTableType.Weight)
    {
        (WeenieClassName.tofun, 4.0f),
        (WeenieClassName.tofunacid, 1.0f),
        (WeenieClassName.tofunelectric, 1.0f),
        (WeenieClassName.tofunfire, 1.0f),
        (WeenieClassName.tofunfrost, 1.0f),
        (WeenieClassName.morningstar, 4.0f),
        (WeenieClassName.morningstaracid, 1.0f),
        (WeenieClassName.morningstarelectric, 1.0f),
        (WeenieClassName.morningstarfire, 1.0f),
        (WeenieClassName.morningstarfrost, 1.0f),
        (WeenieClassName.clubspiked, 4.0f),
        (WeenieClassName.clubspikedacid, 1.0f),
        (WeenieClassName.clubspikedelectric, 1.0f),
        (WeenieClassName.clubspikedfire, 1.0f),
        (WeenieClassName.clubspikedfrost, 1.0f),
        (WeenieClassName.ace40635_tetsubo, 4.0f),
        (WeenieClassName.ace40636_acidtetsubo, 1.0f),
        (WeenieClassName.ace40637_lightningtetsubo, 1.0f),
        (WeenieClassName.ace40638_flamingtetsubo, 1.0f),
        (WeenieClassName.ace40639_frosttetsubo, 1.0f),
    };

    public static WeenieClassName Roll(TreasureHeritageGroup heritage, int tier, out TreasureWeaponType weaponType)
    {
        WeenieClassName weapon;

        switch (heritage)
        {
            case TreasureHeritageGroup.Aluvian:
                if (tier > 1)
                {
                    weapon = MaceWcids_Aluvian.Roll();
                }
                else
                {
                    weapon = MaceWcids_Aluvian_T1.Roll();
                }

                break;
            case TreasureHeritageGroup.Gharundim:
                if (tier > 1)
                {
                    weapon = MaceWcids_Gharundim.Roll();
                }
                else
                {
                    weapon = MaceWcids_Gharundim_T1.Roll();
                }

                break;
            case TreasureHeritageGroup.Sho:
                if (tier > 1)
                {
                    weapon = MaceWcids_Sho.Roll();
                }
                else
                {
                    weapon = MaceWcids_Sho_T1.Roll();
                }

                break;
            default:
                weapon = MaceWcids_All.Roll();
                break;
        }

        switch (weapon)
        {
            case WeenieClassName.ace41057_greatstarmace:
            case WeenieClassName.ace41058_acidgreatstarmace:
            case WeenieClassName.ace41059_lightninggreatstarmace:
            case WeenieClassName.ace41060_flaminggreatstarmace:
            case WeenieClassName.ace41061_frostgreatstarmace:
            case WeenieClassName.ace41062_khandahandledmace:
            case WeenieClassName.ace41063_acidkhandahandledmace:
            case WeenieClassName.ace41064_lightningkhandahandledmace:
            case WeenieClassName.ace41065_flamingkhandahandledmace:
            case WeenieClassName.ace41066_frostkhandahandledmace:
            case WeenieClassName.ace40635_tetsubo:
            case WeenieClassName.ace40636_acidtetsubo:
            case WeenieClassName.ace40637_lightningtetsubo:
            case WeenieClassName.ace40638_flamingtetsubo:
            case WeenieClassName.ace40639_frosttetsubo:
                weaponType = TreasureWeaponType.TwoHandedMace;
                break;
            default:
                weaponType = TreasureWeaponType.Mace;
                break;
        }

        return weapon;
    }

    private static readonly Dictionary<WeenieClassName, TreasureWeaponType> _combined =
        new Dictionary<WeenieClassName, TreasureWeaponType>();

    static MaceWcids()
    {
        foreach (var entry in MaceWcids_Aluvian_T1)
        {
            _combined.TryAdd(entry.result, TreasureWeaponType.Mace);
        }

        foreach (var entry in MaceWcids_Aluvian)
        {
            _combined.TryAdd(entry.result, TreasureWeaponType.Mace);
        }

        foreach (var entry in MaceWcids_Gharundim_T1)
        {
            _combined.TryAdd(entry.result, TreasureWeaponType.Mace);
        }

        foreach (var entry in MaceWcids_Gharundim)
        {
            _combined.TryAdd(entry.result, TreasureWeaponType.Mace);
        }

        foreach (var entry in MaceWcids_Sho_T1)
        {
            _combined.TryAdd(entry.result, TreasureWeaponType.Mace);
        }

        foreach (var entry in MaceWcids_Sho)
        {
            _combined.TryAdd(entry.result, TreasureWeaponType.Mace);
        }
    }

    public static bool TryGetValue(WeenieClassName wcid, out TreasureWeaponType weaponType)
    {
        return _combined.TryGetValue(wcid, out weaponType);
    }
}
