using System.Collections.Generic;
using ACE.Server.Factories.Entity;
using ACE.Server.Factories.Enum;

namespace ACE.Server.Factories.Tables;

public static class TreasureProfile_Mundane
{
    // indexed by TreasureDeath.MundaneItemTypeSelectionChances

    private static ChanceTable<TreasureItemType_Orig> mundaneProfile1 = new ChanceTable<TreasureItemType_Orig>()
    {
        (TreasureItemType_Orig.Consumable, 1.0f),
    };

    private static ChanceTable<TreasureItemType_Orig> mundaneProfile2 = new ChanceTable<TreasureItemType_Orig>()
    {
        (TreasureItemType_Orig.HealKit, 1.0f),
    };

    private static ChanceTable<TreasureItemType_Orig> mundaneProfile3 = new ChanceTable<TreasureItemType_Orig>()
    {
        (TreasureItemType_Orig.Lockpick, 1.0f),
    };

    private static ChanceTable<TreasureItemType_Orig> mundaneProfile4 = new ChanceTable<TreasureItemType_Orig>()
    {
        (TreasureItemType_Orig.SpellComponent, 1.0f),
    };

    private static ChanceTable<TreasureItemType_Orig> mundaneProfile5 = new ChanceTable<TreasureItemType_Orig>()
    {
        (TreasureItemType_Orig.ManaStone, 1.0f),
    };

    private static ChanceTable<TreasureItemType_Orig> mundaneProfile6 = new ChanceTable<TreasureItemType_Orig>()
    {
        (TreasureItemType_Orig.Pyreal, 1.0f),
    };

    /// <summary>
    /// The most common MundaneItem profile
    /// </summary>
    private static ChanceTable<TreasureItemType_Orig> mundaneProfile7 = new ChanceTable<TreasureItemType_Orig>(
        ChanceTableType.Weight
    )
    {
        (TreasureItemType_Orig.Pyreal, 1.0f),
        (TreasureItemType_Orig.Consumable, 1.0f),
        (TreasureItemType_Orig.HealKit, 1.0f),
        (TreasureItemType_Orig.Lockpick, 1.0f),
        (TreasureItemType_Orig.SpellComponent, 1.0f),
        (TreasureItemType_Orig.ManaStone, 1.0f),
    };

    /// <summary>
    /// The second most common MundaneItem profile
    /// </summary>
    private static ChanceTable<TreasureItemType_Orig> mundaneProfile8 = new ChanceTable<TreasureItemType_Orig>(
        ChanceTableType.Weight
    )
    {
        (TreasureItemType_Orig.Pyreal, 1.0f),
        (TreasureItemType_Orig.SpellComponent, 1.0f),
        (TreasureItemType_Orig.ManaStone, 1.0f),
    };

    /// <summary>
    /// Warrior Profile
    /// </summary>
    private static ChanceTable<TreasureItemType_Orig> mundaneProfile9 = new ChanceTable<TreasureItemType_Orig>()
    {
        (TreasureItemType_Orig.Pyreal, 0.8f),
        (TreasureItemType_Orig.HealKit, 0.1f),
        (TreasureItemType_Orig.Consumable, 0.1f),
    };

    /// <summary>
    /// Rogue Profile
    /// </summary>
    private static ChanceTable<TreasureItemType_Orig> mundaneProfile10 = new ChanceTable<TreasureItemType_Orig>()
    {
        (TreasureItemType_Orig.Pyreal, 0.79f),
        (TreasureItemType_Orig.Lockpick, 0.1f),
        (TreasureItemType_Orig.HealKit, 0.1f),
        (TreasureItemType_Orig.Gem, 0.01f),
    };

    /// <summary>
    /// Caster Profile
    /// </summary>
    private static ChanceTable<TreasureItemType_Orig> mundaneProfile11 = new ChanceTable<TreasureItemType_Orig>()
    {
        (TreasureItemType_Orig.Pyreal, 0.59f),
        (TreasureItemType_Orig.SpellComponent, 0.2f),
        (TreasureItemType_Orig.Gem, 0.01f),
        (TreasureItemType_Orig.ManaStone, 0.1f),
        (TreasureItemType_Orig.Scroll, 0.1f),
    };

    /// <summary>
    /// WarriorRogue Profile
    /// </summary>
    private static ChanceTable<TreasureItemType_Orig> mundaneProfile12 = new ChanceTable<TreasureItemType_Orig>()
    {
        (TreasureItemType_Orig.Pyreal, 0.79f),
        (TreasureItemType_Orig.HealKit, 0.1f),
        (TreasureItemType_Orig.Consumable, 0.05f),
        (TreasureItemType_Orig.Lockpick, 0.05f),
        (TreasureItemType_Orig.Gem, 0.01f),
    };

    /// <summary>
    /// WarriorCaster Profile
    /// </summary>
    private static ChanceTable<TreasureItemType_Orig> mundaneProfile13 = new ChanceTable<TreasureItemType_Orig>()
    {
        (TreasureItemType_Orig.Pyreal, 0.69f),
        (TreasureItemType_Orig.HealKit, 0.05f),
        (TreasureItemType_Orig.Consumable, 0.05f),
        (TreasureItemType_Orig.SpellComponent, 0.1f),
        (TreasureItemType_Orig.Gem, 0.01f),
        (TreasureItemType_Orig.ManaStone, 0.05f),
        (TreasureItemType_Orig.Scroll, 0.05f),
    };

    /// <summary>
    /// RogueCaster Profile
    /// </summary>
    private static ChanceTable<TreasureItemType_Orig> mundaneProfile14 = new ChanceTable<TreasureItemType_Orig>()
    {
        (TreasureItemType_Orig.Pyreal, 0.69f),
        (TreasureItemType_Orig.Lockpick, 0.05f),
        (TreasureItemType_Orig.HealKit, 0.05f),
        (TreasureItemType_Orig.SpellComponent, 0.125f),
        (TreasureItemType_Orig.Gem, 0.01f),
        (TreasureItemType_Orig.ManaStone, 0.025f),
        (TreasureItemType_Orig.Scroll, 0.05f),
    };

    /// <summary>
    /// Balanced Profile
    /// </summary>
    private static ChanceTable<TreasureItemType_Orig> mundaneProfile15 = new ChanceTable<TreasureItemType_Orig>()
    {
        (TreasureItemType_Orig.Pyreal, 0.59f),
        (TreasureItemType_Orig.HealKit, 0.1f),
        (TreasureItemType_Orig.Gem, 0.01f),
        (TreasureItemType_Orig.SpellComponent, 0.1f),
        (TreasureItemType_Orig.Consumable, 0.05f),
        (TreasureItemType_Orig.Lockpick, 0.05f),
        (TreasureItemType_Orig.ManaStone, 0.05f),
        (TreasureItemType_Orig.Scroll, 0.05f),
    };

    /// <summary>
    /// TreasureDeath.MundaneItemTypeSelectionChances indexes into these profiles
    /// </summary>
    public static List<ChanceTable<TreasureItemType_Orig>> mundaneProfiles = new List<
        ChanceTable<TreasureItemType_Orig>
    >()
    {
        mundaneProfile1,
        mundaneProfile2,
        mundaneProfile3,
        mundaneProfile4,
        mundaneProfile5,
        mundaneProfile6,
        mundaneProfile7,
        mundaneProfile8,
        mundaneProfile9,
        mundaneProfile10,
        mundaneProfile11,
        mundaneProfile12,
        mundaneProfile13,
        mundaneProfile14,
        mundaneProfile15,
    };

    /// <summary>
    /// Rolls for a TreasureItemType for a TreasureItemCategory.MundaneItem
    /// </summary>
    /// <param name="mundaneProfile">From TreasureDeath.MundaneItemTypeSelectionChances</param>
    public static TreasureItemType_Orig Roll(int mundaneProfile)
    {
        if (mundaneProfile < 1 || mundaneProfile > mundaneProfiles.Count)
        {
            return TreasureItemType_Orig.Undef;
        }

        return mundaneProfiles[mundaneProfile - 1].Roll();
    }
}
