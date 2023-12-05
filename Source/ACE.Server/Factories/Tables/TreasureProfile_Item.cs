using System.Collections.Generic;

using ACE.Server.Factories.Entity;
using ACE.Server.Factories.Enum;

namespace ACE.Server.Factories.Tables
{
    public static class TreasureProfile_Item
    {
        // indexed by TreasureDeath.ItemTreasureTypeSelectionChances

        private static ChanceTable<TreasureItemType_Orig> itemProfile1 = new ChanceTable<TreasureItemType_Orig>()
        {
            ( TreasureItemType_Orig.Weapon,    1.0f ),
        };

        private static ChanceTable<TreasureItemType_Orig> itemProfile2 = new ChanceTable<TreasureItemType_Orig>()
        {
            ( TreasureItemType_Orig.Armor,     1.0f ),
        };

        private static ChanceTable<TreasureItemType_Orig> itemProfile3 = new ChanceTable<TreasureItemType_Orig>()
        {
            ( TreasureItemType_Orig.Scroll,    1.0f ),
        };

        private static ChanceTable<TreasureItemType_Orig> itemProfile4 = new ChanceTable<TreasureItemType_Orig>()
        {
            ( TreasureItemType_Orig.Clothing,  1.0f ),
        };

        private static ChanceTable<TreasureItemType_Orig> itemProfile5 = new ChanceTable<TreasureItemType_Orig>()
        {
            ( TreasureItemType_Orig.Jewelry,   1.0f ),
        };

        private static ChanceTable<TreasureItemType_Orig> itemProfile6 = new ChanceTable<TreasureItemType_Orig>()
        {
            ( TreasureItemType_Orig.Gem,       1.0f ),
        };

        private static ChanceTable<TreasureItemType_Orig> itemProfile7 = new ChanceTable<TreasureItemType_Orig>()
        {
            ( TreasureItemType_Orig.ArtObject, 1.0f ),
        };

        /// <summary>
        /// The second most common ItemProfile
        /// </summary>
        private static ChanceTable<TreasureItemType_Orig> itemProfile8 = new ChanceTable<TreasureItemType_Orig>(ChanceTableType.Weight)
        {
            ( TreasureItemType_Orig.Weapon,    2.0f ),
            ( TreasureItemType_Orig.Armor,     2.0f ),
            ( TreasureItemType_Orig.Clothing,  1.5f ),
            ( TreasureItemType_Orig.Jewelry,   1.5f ),
            ( TreasureItemType_Orig.Gem,       1.5f ),
            ( TreasureItemType_Orig.ArtObject, 1.5f ),
        };

        /// <summary>
        /// The most common ItemProfile
        /// </summary>
        private static ChanceTable<TreasureItemType_Orig> itemProfile9 = new ChanceTable<TreasureItemType_Orig>(ChanceTableType.Weight)
        {
            ( TreasureItemType_Orig.Weapon,    4.6f ),
            ( TreasureItemType_Orig.Armor,     4.0f ),
            ( TreasureItemType_Orig.Clothing,  0.4f ),
            ( TreasureItemType_Orig.Jewelry,   0.2f ),
            ( TreasureItemType_Orig.Gem,       0.4f ),
            ( TreasureItemType_Orig.ArtObject, 0.4f ),
        };

        private static ChanceTable<TreasureItemType_Orig> itemProfile10 = new ChanceTable<TreasureItemType_Orig>(ChanceTableType.Weight)
        {
            ( TreasureItemType_Orig.Weapon,    1.0f ),
            ( TreasureItemType_Orig.Armor,     1.0f ),
        };

        private static ChanceTable<TreasureItemType_Orig> itemProfile11 = new ChanceTable<TreasureItemType_Orig>()
        {
            ( TreasureItemType_Orig.Clothing,  0.25f ),
            ( TreasureItemType_Orig.Jewelry,   0.25f ),
            ( TreasureItemType_Orig.Gem,       0.25f ),
            ( TreasureItemType_Orig.ArtObject, 0.25f ),
        };

        // Warrior Profile
        private static ChanceTable<TreasureItemType_Orig> itemProfile12 = new ChanceTable<TreasureItemType_Orig>()
        {
            ( TreasureItemType_Orig.ArmorWarrior,   0.4f ),
            ( TreasureItemType_Orig.WeaponWarrior,  0.3f ),
            ( TreasureItemType_Orig.Clothing,       0.15f ),
            ( TreasureItemType_Orig.Jewelry,        0.15f ),
        };

        // Rogue Profile
        private static ChanceTable<TreasureItemType_Orig> itemProfile13 = new ChanceTable<TreasureItemType_Orig>()
        {
            ( TreasureItemType_Orig.ArmorRogue,         0.4f ),
            ( TreasureItemType_Orig.WeaponRogue,        0.3f ),
            ( TreasureItemType_Orig.Clothing,           0.15f ),
            ( TreasureItemType_Orig.Jewelry,            0.14f ),
            ( TreasureItemType_Orig.EmpoweredScarabs,   0.01f ),
        };

        // Caster Profile
        private static ChanceTable<TreasureItemType_Orig> itemProfile14 = new ChanceTable<TreasureItemType_Orig>()
        {
            ( TreasureItemType_Orig.ArmorCaster,        0.4f ),
            ( TreasureItemType_Orig.WeaponCaster,       0.3f ),
            ( TreasureItemType_Orig.Clothing,           0.15f ),
            ( TreasureItemType_Orig.Jewelry,            0.1f ),
            ( TreasureItemType_Orig.EmpoweredScarabs,   0.05f ),
        };

        // WarriorRogue Profile
        private static ChanceTable<TreasureItemType_Orig> itemProfile15 = new ChanceTable<TreasureItemType_Orig>()
        {
            ( TreasureItemType_Orig.ArmorWarrior,   0.2f ),
            ( TreasureItemType_Orig.WeaponWarrior,  0.15f ),
            ( TreasureItemType_Orig.ArmorRogue,     0.2f ),
            ( TreasureItemType_Orig.WeaponRogue,    0.15f ),
            ( TreasureItemType_Orig.Clothing,       0.15f ),
            ( TreasureItemType_Orig.Jewelry,        0.15f ),
        };

        // WarriorCaster Profile
        private static ChanceTable<TreasureItemType_Orig> itemProfile16 = new ChanceTable<TreasureItemType_Orig>()
        {
            ( TreasureItemType_Orig.ArmorWarrior,       0.2f ),
            ( TreasureItemType_Orig.WeaponWarrior,      0.15f ),
            ( TreasureItemType_Orig.ArmorCaster,        0.2f ),
            ( TreasureItemType_Orig.WeaponCaster,       0.15f ),
            ( TreasureItemType_Orig.Clothing,           0.15f ),
            ( TreasureItemType_Orig.Jewelry,            0.12f ),
            ( TreasureItemType_Orig.EmpoweredScarabs,   0.03f ),
        };

        // RogueCaster Profile
        private static ChanceTable<TreasureItemType_Orig> itemProfile17 = new ChanceTable<TreasureItemType_Orig>()
        {
            ( TreasureItemType_Orig.ArmorRogue,         0.2f ),
            ( TreasureItemType_Orig.WeaponRogue,        0.15f ),
            ( TreasureItemType_Orig.ArmorCaster,        0.2f ),
            ( TreasureItemType_Orig.WeaponCaster,       0.15f ),
            ( TreasureItemType_Orig.Clothing,           0.15f ),
            ( TreasureItemType_Orig.Jewelry,            0.11f ),
            ( TreasureItemType_Orig.EmpoweredScarabs,   0.04f ),
        };

        // Balanced Profile
        private static ChanceTable<TreasureItemType_Orig> itemProfile18 = new ChanceTable<TreasureItemType_Orig>()
        {
            ( TreasureItemType_Orig.ArmorWarrior,       0.15f ),
            ( TreasureItemType_Orig.WeaponWarrior,      0.11f ),
            ( TreasureItemType_Orig.ArmorRogue,         0.15f ),
            ( TreasureItemType_Orig.WeaponRogue,        0.11f ),
            ( TreasureItemType_Orig.ArmorCaster,        0.15f ),
            ( TreasureItemType_Orig.WeaponCaster,       0.11f ),
            ( TreasureItemType_Orig.Clothing,           0.15f ),
            ( TreasureItemType_Orig.Jewelry,            0.08f ),
            ( TreasureItemType_Orig.EmpoweredScarabs,   0.02f ),
        };

        // Animal Parts Profile
        private static ChanceTable<TreasureItemType_Orig> itemProfile19 = new ChanceTable<TreasureItemType_Orig>()
        {
            ( TreasureItemType_Orig.AnimalParts,   1.0f ),
        };

        // Empowered Scarabs Profile
        private static ChanceTable<TreasureItemType_Orig> itemProfile20 = new ChanceTable<TreasureItemType_Orig>()
        {
            ( TreasureItemType_Orig.EmpoweredScarabs,   1.0f ),
        };

        /// <summary>
        /// TreasureDeath.ItemTreasureTypeSelectionChances indexes into these profiles
        /// </summary>
        private static readonly List<ChanceTable<TreasureItemType_Orig>> itemProfiles = new List<ChanceTable<TreasureItemType_Orig>>()
        {
            itemProfile1,
            itemProfile2,
            itemProfile3,
            itemProfile4,
            itemProfile5,
            itemProfile6,
            itemProfile7,
            itemProfile8,
            itemProfile9,
            itemProfile10,
            itemProfile11,
            itemProfile12,
            itemProfile13,
            itemProfile14,
            itemProfile15,
            itemProfile16,
            itemProfile17,
            itemProfile18,
            itemProfile19,
            itemProfile20
        };

        /// <summary>
        /// Rolls for a TreasureItemType for a non-magical TreasureItemCategory.Item
        /// </summary>
        /// <param name="itemProfile">From TreasureDeath.ItemTreasureTypeSelectionChances</param>
        public static TreasureItemType_Orig Roll(int itemProfile)
        {
            if (itemProfile < 1 || itemProfile > itemProfiles.Count)
                return TreasureItemType_Orig.Undef;

            return itemProfiles[itemProfile - 1].Roll();
        }
    }
}
