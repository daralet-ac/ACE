using ACE.Server.Factories.Entity;
using ACE.Server.Factories.Enum;

namespace ACE.Server.Factories.Tables;

public static class WeaponTypeChance
{
    // from magloot corpse logs
    // it appears they might have gotten rid of the tier chances
    private static ChanceTable<TreasureWeaponType> RetailChances = new ChanceTable<TreasureWeaponType>()
    {
        // melee: 63%
        // missile: 20%
        // two-handed: 10%
        // caster: 7%
        (TreasureWeaponType.Sword, 0.09f),
        (TreasureWeaponType.Mace, 0.09f),
        (TreasureWeaponType.Axe, 0.09f),
        (TreasureWeaponType.Spear, 0.09f),
        (TreasureWeaponType.Unarmed, 0.09f),
        (TreasureWeaponType.Staff, 0.09f),
        (TreasureWeaponType.Dagger, 0.09f),
        (TreasureWeaponType.Bow, 0.07f),
        (TreasureWeaponType.Crossbow, 0.07f),
        (TreasureWeaponType.Atlatl, 0.06f),
        (TreasureWeaponType.Caster, 0.07f),
        (TreasureWeaponType.TwoHandedWeapon, 0.10f), // see TreasureWeaponType for an explanation of why this is here,
        // and not deeper in WeaponWcids.cs
    };

    private static ChanceTable<TreasureWeaponType> MeleeChances = new ChanceTable<TreasureWeaponType>(ChanceTableType.Weight)
    {
        (TreasureWeaponType.Sword, 1.0f),
        (TreasureWeaponType.Mace, 1.0f),
        (TreasureWeaponType.Axe, 1.0f),
        (TreasureWeaponType.Spear, 1.0f),
        (TreasureWeaponType.Unarmed, 1.0f),
        (TreasureWeaponType.Staff, 1.0f),
        (TreasureWeaponType.Dagger, 1.0f),
        (TreasureWeaponType.TwoHandedWeapon, 1.0f),
        (TreasureWeaponType.Dagger, 1.0f)
    };

    private static ChanceTable<TreasureWeaponType> MissileChances = new ChanceTable<TreasureWeaponType>()
    {
        (TreasureWeaponType.Bow, 0.34f),
        (TreasureWeaponType.Crossbow, 0.33f),
        (TreasureWeaponType.Atlatl, 0.33f),
    };

    private static ChanceTable<TreasureWeaponType> WarriorChances = new ChanceTable<TreasureWeaponType>(ChanceTableType.Weight)
    {
        (TreasureWeaponType.Sword, 1.0f),
        (TreasureWeaponType.Mace, 1.0f),
        (TreasureWeaponType.Axe, 1.0f),
        (TreasureWeaponType.Spear, 1.0f),
        (TreasureWeaponType.Thrown, 2.0f),
    };

    private static ChanceTable<TreasureWeaponType> RogueChances = new ChanceTable<TreasureWeaponType>(ChanceTableType.Weight)
    {
        (TreasureWeaponType.Unarmed, 3.0f),
        (TreasureWeaponType.Dagger, 3.0f),
        (TreasureWeaponType.Staff, 3.0f),
        (TreasureWeaponType.Atlatl, 1.0f),
        (TreasureWeaponType.Bow, 1.0f),
        (TreasureWeaponType.Crossbow, 1.0f),
    };

    public static TreasureWeaponType Roll(int tier, TreasureWeaponType filterToType = TreasureWeaponType.Undef)
    {
        // todo: add unique profiles for t7 / t8?
        //tier = Math.Clamp(tier, 1, 6);

        //return weaponTiers[tier - 1].Roll();

        switch (filterToType)
        {
            case TreasureWeaponType.MeleeWeapon:
                return MeleeChances.Roll();
            case TreasureWeaponType.MissileWeapon:
                return MissileChances.Roll();
            default:
                return RetailChances.Roll();
        }
    }

    public static TreasureWeaponType Roll(TreasureItemType_Orig filterToType = TreasureItemType_Orig.Undef)
    {
        switch (filterToType)
        {
            case TreasureItemType_Orig.WeaponWarrior:
                return WarriorChances.Roll();
            default:
                return RogueChances.Roll();
        }
    }
}
