using System.Collections.Generic;
using ACE.Database.Models.World;
using ACE.Server.Factories.Entity;
using ACE.Server.Factories.Enum;

namespace ACE.Server.Factories.Tables.Wcids;

public static class ManaStoneWcids
{
    private static ChanceTable<WeenieClassName> T1_Chances = new ChanceTable<WeenieClassName>()
    {
        (WeenieClassName.manastonelessernew, 1.0f),
    };

    private static ChanceTable<WeenieClassName> T2_Chances = new ChanceTable<WeenieClassName>()
    {
        (WeenieClassName.manastonelessernew, 0.9f),
        (WeenieClassName.manastonenew, 0.1f),
    };

    private static ChanceTable<WeenieClassName> T3_Chances = new ChanceTable<WeenieClassName>()
    {
        (WeenieClassName.manastonelessernew, 0.5f),
        (WeenieClassName.manastonenew, 0.5f),
    };

    private static ChanceTable<WeenieClassName> T4_Chances = new ChanceTable<WeenieClassName>()
    {
        (WeenieClassName.manastonenew, 0.9f),
        (WeenieClassName.manastonegreaternew, 0.1f),
    };

    private static ChanceTable<WeenieClassName> T5_Chances = new ChanceTable<WeenieClassName>()
    {
        (WeenieClassName.manastonenew, 0.5f),
        (WeenieClassName.manastonegreaternew, 0.5f),
    };

    private static ChanceTable<WeenieClassName> T6_Chances = new ChanceTable<WeenieClassName>()
    {
        (WeenieClassName.manastonegreaternew, 0.9f),
        (WeenieClassName.manastonetitannew, 0.1f),
    };

    private static ChanceTable<WeenieClassName> T7_Chances = new ChanceTable<WeenieClassName>()
    {
        (WeenieClassName.manastonegreaternew, 0.25f),
        (WeenieClassName.manastonetitannew, 0.75f),
    };

    private static ChanceTable<WeenieClassName> T8_Chances = new ChanceTable<WeenieClassName>()
    {
        (WeenieClassName.manastonegreaternew, 0.5f),
        (WeenieClassName.manastonetitannew, 0.5f),
    };

    private static readonly List<ChanceTable<WeenieClassName>> manaStoneTiers = new List<ChanceTable<WeenieClassName>>()
    {
        T1_Chances,
        T2_Chances,
        T3_Chances,
        T4_Chances,
        T5_Chances,
        T6_Chances,
        T7_Chances,
        T8_Chances,
    };

    public static WeenieClassName Roll(TreasureDeath profile)
    {
        // todo: verify t7 / t8 chances
        var table = manaStoneTiers[profile.Tier - 1];

        return table.Roll(profile.LootQualityMod);
    }
}
