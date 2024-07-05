using System;
using System.Collections.Generic;
using ACE.Server.Factories.Entity;
using ACE.Server.Factories.Enum;

namespace ACE.Server.Factories.Tables;

public static class ArmorTypeChance
{
    private static ChanceTable<TreasureArmorType> T1_Chances = new ChanceTable<TreasureArmorType>(
        ChanceTableType.Weight
    )
    {
        (TreasureArmorType.Leather, 1.0f),
        (TreasureArmorType.StuddedLeather, 1.0f),
        (TreasureArmorType.Chainmail, 1.0f),
    };

    private static ChanceTable<TreasureArmorType> T2_Chances = new ChanceTable<TreasureArmorType>(
        ChanceTableType.Weight
    )
    {
        (TreasureArmorType.Leather, 1.0f),
        (TreasureArmorType.StuddedLeather, 1.0f),
        (TreasureArmorType.Chainmail, 1.0f),
    };

    private static ChanceTable<TreasureArmorType> T3_Chances = new ChanceTable<TreasureArmorType>(
        ChanceTableType.Weight
    )
    {
        (TreasureArmorType.Leather, 1.0f),
        (TreasureArmorType.StuddedLeather, 1.0f),
        (TreasureArmorType.Chainmail, 1.0f),
        (TreasureArmorType.Platemail, 1.0f),
        (TreasureArmorType.Scalemail, 1.0f),
        (TreasureArmorType.Yoroi, 1.0f),
    };

    private static ChanceTable<TreasureArmorType> T4_Chances = new ChanceTable<TreasureArmorType>(
        ChanceTableType.Weight
    )
    {
        (TreasureArmorType.Leather, 1.0f),
        (TreasureArmorType.StuddedLeather, 1.0f),
        (TreasureArmorType.Chainmail, 1.0f),
        (TreasureArmorType.Platemail, 1.0f),
        (TreasureArmorType.Scalemail, 1.0f),
        (TreasureArmorType.Yoroi, 1.0f),
    };

    private static ChanceTable<TreasureArmorType> T5_Chances = new ChanceTable<TreasureArmorType>(
        ChanceTableType.Weight
    )
    {
        (TreasureArmorType.Leather, 1.0f),
        (TreasureArmorType.StuddedLeather, 1.0f),
        (TreasureArmorType.Chainmail, 1.0f),
        (TreasureArmorType.Platemail, 1.0f),
        (TreasureArmorType.Scalemail, 1.0f),
        (TreasureArmorType.Yoroi, 1.0f),
        (TreasureArmorType.HeritageLow, 1.0f),
        (TreasureArmorType.Covenant, 1.0f),
    };

    private static ChanceTable<TreasureArmorType> T6_Chances = new ChanceTable<TreasureArmorType>(
        ChanceTableType.Weight
    )
    {
        (TreasureArmorType.Leather, 1.0f),
        (TreasureArmorType.StuddedLeather, 1.0f),
        (TreasureArmorType.Chainmail, 1.0f),
        (TreasureArmorType.Platemail, 1.0f),
        (TreasureArmorType.Scalemail, 1.0f),
        (TreasureArmorType.Yoroi, 1.0f),
        (TreasureArmorType.HeritageLow, 1.0f),
        (TreasureArmorType.Covenant, 1.0f),
        (TreasureArmorType.HeritageHigh, 1.0f),
    };

    // added, from mag-loot logs
    private static ChanceTable<TreasureArmorType> T7_Chances = new ChanceTable<TreasureArmorType>(
        ChanceTableType.Weight
    )
    {
        (TreasureArmorType.Leather, 1.0f),
        (TreasureArmorType.StuddedLeather, 1.0f),
        (TreasureArmorType.Chainmail, 1.0f),
        (TreasureArmorType.Platemail, 1.0f),
        (TreasureArmorType.Scalemail, 1.0f),
        (TreasureArmorType.Yoroi, 1.0f),
        (TreasureArmorType.HeritageLow, 1.0f),
        (TreasureArmorType.Covenant, 1.0f),
        (TreasureArmorType.HeritageHigh, 1.0f),
        (TreasureArmorType.Olthoi, 1.0f),
        (TreasureArmorType.OlthoiHeritage, 1.0f),
    };

    private static ChanceTable<TreasureArmorType> T8_Chances = new ChanceTable<TreasureArmorType>(
        ChanceTableType.Weight
    )
    {
        (TreasureArmorType.Leather, 1.0f),
        (TreasureArmorType.StuddedLeather, 1.0f),
        (TreasureArmorType.Chainmail, 1.0f),
        (TreasureArmorType.Platemail, 1.0f),
        (TreasureArmorType.Scalemail, 1.0f),
        (TreasureArmorType.Yoroi, 1.0f),
        (TreasureArmorType.HeritageLow, 1.0f),
        (TreasureArmorType.Covenant, 1.0f),
        (TreasureArmorType.HeritageHigh, 1.0f),
        (TreasureArmorType.Olthoi, 1.0f),
        (TreasureArmorType.OlthoiHeritage, 1.0f),
    };

    private static readonly List<ChanceTable<TreasureArmorType>> armorTiers = new List<ChanceTable<TreasureArmorType>>()
    {
        T1_Chances,
        T2_Chances,
        T3_Chances,
        T4_Chances,
        T5_Chances,
        T6_Chances,
        T7_Chances,
        T8_Chances
    };

    private static ChanceTable<TreasureArmorType> T1_T2_Warrior_Chances = new ChanceTable<TreasureArmorType>(
        ChanceTableType.Weight
    )
    {
        (TreasureArmorType.Chainmail, 1.0f),
    };

    private static ChanceTable<TreasureArmorType> T3_T4_Warrior_Chances = new ChanceTable<TreasureArmorType>(
        ChanceTableType.Weight
    )
    {
        (TreasureArmorType.Chainmail, 1.0f),
        (TreasureArmorType.Platemail, 1.0f),
        (TreasureArmorType.Scalemail, 1.0f),
    };

    private static ChanceTable<TreasureArmorType> T5_Warrior_Chances = new ChanceTable<TreasureArmorType>(
        ChanceTableType.Weight
    )
    {
        (TreasureArmorType.Chainmail, 1.0f),
        (TreasureArmorType.Platemail, 1.0f),
        (TreasureArmorType.Scalemail, 1.0f),
        (TreasureArmorType.Celdon, 1.0f),
        (TreasureArmorType.Covenant, 1.0f),
    };

    private static ChanceTable<TreasureArmorType> T6_Warrior_Chances = new ChanceTable<TreasureArmorType>(
        ChanceTableType.Weight
    )
    {
        (TreasureArmorType.Chainmail, 1.0f),
        (TreasureArmorType.Platemail, 1.0f),
        (TreasureArmorType.Scalemail, 1.0f),
        (TreasureArmorType.Celdon, 1.0f),
        (TreasureArmorType.Covenant, 1.0f),
        (TreasureArmorType.Nariyid, 1.0f),
    };

    private static ChanceTable<TreasureArmorType> T7_T8_Warrior_Chances = new ChanceTable<TreasureArmorType>(
        ChanceTableType.Weight
    )
    {
        (TreasureArmorType.Chainmail, 1.0f),
        (TreasureArmorType.Platemail, 1.0f),
        (TreasureArmorType.Scalemail, 1.0f),
        (TreasureArmorType.Celdon, 1.0f),
        (TreasureArmorType.Covenant, 1.0f),
        (TreasureArmorType.Nariyid, 1.0f),
        (TreasureArmorType.OlthoiCeldon, 1.0f),
        (TreasureArmorType.Olthoi, 1.0f),
    };

    private static readonly List<ChanceTable<TreasureArmorType>> armorWarriorTiers = new List<
        ChanceTable<TreasureArmorType>
    >()
    {
        T1_T2_Warrior_Chances,
        T1_T2_Warrior_Chances,
        T3_T4_Warrior_Chances,
        T3_T4_Warrior_Chances,
        T5_Warrior_Chances,
        T6_Warrior_Chances,
        T7_T8_Warrior_Chances,
        T7_T8_Warrior_Chances
    };

    private static ChanceTable<TreasureArmorType> T1_T2_Rogue_Chances = new ChanceTable<TreasureArmorType>(
        ChanceTableType.Weight
    )
    {
        (TreasureArmorType.Leather, 1.0f),
        (TreasureArmorType.StuddedLeather, 1.0f),
    };

    private static ChanceTable<TreasureArmorType> T3_T4_Rogue_Chances = new ChanceTable<TreasureArmorType>(
        ChanceTableType.Weight
    )
    {
        (TreasureArmorType.Leather, 1.0f),
        (TreasureArmorType.StuddedLeather, 1.0f),
        (TreasureArmorType.Yoroi, 1.0f),
    };

    private static ChanceTable<TreasureArmorType> T5_Rogue_Chances = new ChanceTable<TreasureArmorType>(
        ChanceTableType.Weight
    )
    {
        (TreasureArmorType.Leather, 1.0f),
        (TreasureArmorType.StuddedLeather, 1.0f),
        (TreasureArmorType.Yoroi, 1.0f),
        (TreasureArmorType.Koujia, 1.0f),
    };

    private static ChanceTable<TreasureArmorType> T6_Rogue_Chances = new ChanceTable<TreasureArmorType>(
        ChanceTableType.Weight
    )
    {
        (TreasureArmorType.Leather, 1.0f),
        (TreasureArmorType.StuddedLeather, 1.0f),
        (TreasureArmorType.Yoroi, 1.0f),
        (TreasureArmorType.Koujia, 1.0f),
        (TreasureArmorType.Lorica, 1.0f),
    };

    private static ChanceTable<TreasureArmorType> T7_T8_Rogue_Chances = new ChanceTable<TreasureArmorType>(
        ChanceTableType.Weight
    )
    {
        (TreasureArmorType.Leather, 1.0f),
        (TreasureArmorType.StuddedLeather, 1.0f),
        (TreasureArmorType.Yoroi, 1.0f),
        (TreasureArmorType.Koujia, 1.0f),
        (TreasureArmorType.Lorica, 1.0f),
        (TreasureArmorType.OlthoiKoujia, 1.0f),
    };

    private static readonly List<ChanceTable<TreasureArmorType>> armorRogueTiers = new List<
        ChanceTable<TreasureArmorType>
    >()
    {
        T1_T2_Rogue_Chances,
        T1_T2_Rogue_Chances,
        T3_T4_Rogue_Chances,
        T3_T4_Rogue_Chances,
        T5_Rogue_Chances,
        T6_Rogue_Chances,
        T7_T8_Rogue_Chances,
        T7_T8_Rogue_Chances
    };

    private static ChanceTable<TreasureArmorType> T1_T2_Caster_Chances = new ChanceTable<TreasureArmorType>(
        ChanceTableType.Weight
    )
    {
        (TreasureArmorType.Cloth, 1.0f),
    };

    private static ChanceTable<TreasureArmorType> T3_T4_Caster_Chances = new ChanceTable<TreasureArmorType>(
        ChanceTableType.Weight
    )
    {
        (TreasureArmorType.Cloth, 1.0f),
    };

    private static ChanceTable<TreasureArmorType> T5_Caster_Chances = new ChanceTable<TreasureArmorType>(
        ChanceTableType.Weight
    )
    {
        (TreasureArmorType.Cloth, 1.0f),
        (TreasureArmorType.Amuli, 1.0f),
    };

    private static ChanceTable<TreasureArmorType> T6_Caster_Chances = new ChanceTable<TreasureArmorType>(
        ChanceTableType.Weight
    )
    {
        (TreasureArmorType.Cloth, 1.0f),
        (TreasureArmorType.Amuli, 1.0f),
        (TreasureArmorType.Chiran, 1.0f),
    };

    private static ChanceTable<TreasureArmorType> T7_T8_Caster_Chances = new ChanceTable<TreasureArmorType>(
        ChanceTableType.Weight
    )
    {
        (TreasureArmorType.Cloth, 1.0f),
        (TreasureArmorType.Amuli, 1.0f),
        (TreasureArmorType.Chiran, 1.0f),
        (TreasureArmorType.OlthoiAmuli, 1.0f),
    };

    private static readonly List<ChanceTable<TreasureArmorType>> armorCasterTiers = new List<
        ChanceTable<TreasureArmorType>
    >()
    {
        T1_T2_Caster_Chances,
        T1_T2_Caster_Chances,
        T3_T4_Caster_Chances,
        T3_T4_Caster_Chances,
        T5_Caster_Chances,
        T6_Caster_Chances,
        T7_T8_Caster_Chances,
        T7_T8_Caster_Chances
    };

    public static TreasureArmorType Roll(int tier)
    {
        return armorTiers[tier - 1].Roll();
    }

    public static TreasureArmorType RollWarrior(int tier)
    {
        return armorWarriorTiers[tier - 1].Roll();
    }

    public static TreasureArmorType RollRogue(int tier)
    {
        return armorRogueTiers[tier - 1].Roll();
    }

    public static TreasureArmorType RollCaster(int tier)
    {
        return armorCasterTiers[tier - 1].Roll();
    }
}
