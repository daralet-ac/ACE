using System.Collections.Generic;
using ACE.Server.Factories.Entity;
using ACE.Server.Factories.Enum;
using WeenieClassName = ACE.Server.Factories.Enum.WeenieClassName;

namespace ACE.Server.Factories.Tables.Wcids.Weapons;

public static class BowWcids
{
    private static ChanceTable<WeenieClassName> BowWcids_All_T1_T5 = new ChanceTable<WeenieClassName>(
        ChanceTableType.Weight
    )
    {
        (WeenieClassName.bowshort, 2.0f),
        (WeenieClassName.bowlong, 2.0f),
        (WeenieClassName.nayin, 2.0f),
        (WeenieClassName.yag, 2.0f),
        (WeenieClassName.yumi, 2.0f),
        (WeenieClassName.shouyumi, 2.0f),
    };

    private static ChanceTable<WeenieClassName> BowWcids_All_T6_T8 = new ChanceTable<WeenieClassName>(
        ChanceTableType.Weight
    )
    {
        (WeenieClassName.bowshort, 2.0f),
        (WeenieClassName.bowlong, 2.0f),
        (WeenieClassName.nayin, 2.0f),
        (WeenieClassName.yag, 2.0f),
        (WeenieClassName.yumi, 2.0f),
        (WeenieClassName.shouyumi, 2.0f),
        (WeenieClassName.bowslashing, 1.0f),
        (WeenieClassName.bowpiercing, 1.0f),
        (WeenieClassName.bowblunt, 1.0f),
        (WeenieClassName.bowacid, 1.0f),
        (WeenieClassName.bowfire, 1.0f),
        (WeenieClassName.bowfrost, 1.0f),
        (WeenieClassName.bowelectric, 1.0f),
        (WeenieClassName.ace31798_slashingcompoundbow, 1.0f),
        (WeenieClassName.ace31804_piercingcompoundbow, 1.0f),
        (WeenieClassName.ace31800_bluntcompoundbow, 1.0f),
        (WeenieClassName.ace31799_acidcompoundbow, 1.0f),
        (WeenieClassName.ace31802_firecompoundbow, 1.0f),
        (WeenieClassName.ace31803_frostcompoundbow, 1.0f),
        (WeenieClassName.ace31801_electriccompoundbow, 1.0f),
    };

    private static ChanceTable<WeenieClassName> BowWcids_Aluvian_T1_T5 = new ChanceTable<WeenieClassName>(
        ChanceTableType.Weight
    )
    {
        (WeenieClassName.bowshort, 1.0f),
        (WeenieClassName.bowlong, 1.0f),
    };

    private static ChanceTable<WeenieClassName> BowWcids_Aluvian_T6_T8 = new ChanceTable<WeenieClassName>(
        ChanceTableType.Weight
    )
    {
        (WeenieClassName.bowshort, 4.0f),
        (WeenieClassName.bowlong, 4.0f),
        (WeenieClassName.bowslashing, 1.0f),
        (WeenieClassName.bowpiercing, 1.0f),
        (WeenieClassName.bowblunt, 1.0f),
        (WeenieClassName.bowacid, 1.0f),
        (WeenieClassName.bowfire, 1.0f),
        (WeenieClassName.bowfrost, 1.0f),
        (WeenieClassName.bowelectric, 1.0f),
        (WeenieClassName.ace31798_slashingcompoundbow, 1.0f),
        (WeenieClassName.ace31804_piercingcompoundbow, 1.0f),
        (WeenieClassName.ace31800_bluntcompoundbow, 1.0f),
        (WeenieClassName.ace31799_acidcompoundbow, 1.0f),
        (WeenieClassName.ace31802_firecompoundbow, 1.0f),
        (WeenieClassName.ace31803_frostcompoundbow, 1.0f),
        (WeenieClassName.ace31801_electriccompoundbow, 1.0f),
    };

    private static ChanceTable<WeenieClassName> BowWcids_Gharundim_T1_T5 = new ChanceTable<WeenieClassName>(
        ChanceTableType.Weight
    )
    {
        (WeenieClassName.nayin, 1.0f),
        (WeenieClassName.yag, 1.0f),
    };

    private static ChanceTable<WeenieClassName> BowWcids_Gharundim_T6_T8 = new ChanceTable<WeenieClassName>(
        ChanceTableType.Weight
    )
    {
        (WeenieClassName.nayin, 4.0f),
        (WeenieClassName.yag, 4.0f),
        (WeenieClassName.bowslashing, 1.0f),
        (WeenieClassName.bowpiercing, 1.0f),
        (WeenieClassName.bowblunt, 1.0f),
        (WeenieClassName.bowacid, 1.0f),
        (WeenieClassName.bowfire, 1.0f),
        (WeenieClassName.bowfrost, 1.0f),
        (WeenieClassName.bowelectric, 1.0f),
        (WeenieClassName.ace31798_slashingcompoundbow, 1.0f),
        (WeenieClassName.ace31804_piercingcompoundbow, 1.0f),
        (WeenieClassName.ace31800_bluntcompoundbow, 1.0f),
        (WeenieClassName.ace31799_acidcompoundbow, 1.0f),
        (WeenieClassName.ace31802_firecompoundbow, 1.0f),
        (WeenieClassName.ace31803_frostcompoundbow, 1.0f),
        (WeenieClassName.ace31801_electriccompoundbow, 1.0f),
    };

    private static ChanceTable<WeenieClassName> BowWcids_Sho_T1_T5 = new ChanceTable<WeenieClassName>(
        ChanceTableType.Weight
    )
    {
        (WeenieClassName.yumi, 1.0f),
        (WeenieClassName.shouyumi, 1.0f),
    };

    private static ChanceTable<WeenieClassName> BowWcids_Sho_T6_T8 = new ChanceTable<WeenieClassName>(
        ChanceTableType.Weight
    )
    {
        (WeenieClassName.yumi, 4.0f),
        (WeenieClassName.shouyumi, 4.0f),
        (WeenieClassName.bowslashing, 1.0f),
        (WeenieClassName.bowpiercing, 1.0f),
        (WeenieClassName.bowblunt, 1.0f),
        (WeenieClassName.bowacid, 1.0f),
        (WeenieClassName.bowfire, 1.0f),
        (WeenieClassName.bowfrost, 1.0f),
        (WeenieClassName.bowelectric, 1.0f),
        (WeenieClassName.ace31798_slashingcompoundbow, 1.0f),
        (WeenieClassName.ace31804_piercingcompoundbow, 1.0f),
        (WeenieClassName.ace31800_bluntcompoundbow, 1.0f),
        (WeenieClassName.ace31799_acidcompoundbow, 1.0f),
        (WeenieClassName.ace31802_firecompoundbow, 1.0f),
        (WeenieClassName.ace31803_frostcompoundbow, 1.0f),
        (WeenieClassName.ace31801_electriccompoundbow, 1.0f),
    };

    public static WeenieClassName Roll(TreasureHeritageGroup heritage, int tier, out TreasureWeaponType weaponType)
    {
        WeenieClassName weapon;

        switch (heritage)
        {
            case TreasureHeritageGroup.Aluvian:
                if (tier > 5)
                {
                    weapon = BowWcids_Aluvian_T6_T8.Roll();
                }
                else
                {
                    weapon = BowWcids_Aluvian_T1_T5.Roll();
                }

                break;
            case TreasureHeritageGroup.Gharundim:
                if (tier > 5)
                {
                    weapon = BowWcids_Gharundim_T6_T8.Roll();
                }
                else
                {
                    weapon = BowWcids_Gharundim_T1_T5.Roll();
                }

                break;
            case TreasureHeritageGroup.Sho:
                if (tier > 5)
                {
                    weapon = BowWcids_Sho_T6_T8.Roll();
                }
                else
                {
                    weapon = BowWcids_Sho_T1_T5.Roll();
                }

                break;
            default:
                if (tier > 5)
                {
                    weapon = BowWcids_All_T6_T8.Roll();
                }
                else
                {
                    weapon = BowWcids_All_T1_T5.Roll();
                }

                break;
        }

        weaponType = TreasureWeaponType.Bow;

        return weapon;
    }

    private static readonly Dictionary<WeenieClassName, TreasureWeaponType> _combined =
        new Dictionary<WeenieClassName, TreasureWeaponType>();

    static BowWcids()
    {
        foreach (var entry in BowWcids_Aluvian_T1_T5)
        {
            _combined.TryAdd(entry.result, TreasureWeaponType.Bow);
        }

        foreach (var entry in BowWcids_Aluvian_T6_T8)
        {
            _combined.TryAdd(entry.result, TreasureWeaponType.Bow);
        }

        foreach (var entry in BowWcids_Gharundim_T1_T5)
        {
            _combined.TryAdd(entry.result, TreasureWeaponType.Bow);
        }

        foreach (var entry in BowWcids_Gharundim_T6_T8)
        {
            _combined.TryAdd(entry.result, TreasureWeaponType.Bow);
        }

        foreach (var entry in BowWcids_Sho_T1_T5)
        {
            _combined.TryAdd(entry.result, TreasureWeaponType.Bow);
        }

        foreach (var entry in BowWcids_Sho_T6_T8)
        {
            _combined.TryAdd(entry.result, TreasureWeaponType.Bow);
        }

        foreach (var entry in BowWcids_All_T1_T5)
        {
            _combined.TryAdd(entry.result, TreasureWeaponType.Bow);
        }

        foreach (var entry in BowWcids_All_T6_T8)
        {
            _combined.TryAdd(entry.result, TreasureWeaponType.Bow);
        }
    }

    public static bool TryGetValue(WeenieClassName wcid, out TreasureWeaponType weaponType)
    {
        return _combined.TryGetValue(wcid, out weaponType);
    }
}
