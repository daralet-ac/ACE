using System;
using ACE.Entity.Enum;
using ACE.Server.Factories;
using ACE.Server.WorldObjects;

namespace ACE.Server.Entity;

/// <summary>
/// The order /bank sort puts items in. First by bank category (weapons, armor, jewelry, ... see BankCategories.Singles),
/// then by rules for that kind of item, then by name:
/// - Weapons: skill, then weapon subtype, then name.
/// - Armor: weight class, then equip slot (head to toe), then armor style, then name.
/// - Salvage: material category, then material, then workmanship.
/// - Everything else: name.
/// Names compare without the material first, then with it, so the same item in different materials sits together.
/// </summary>
public static class ItemSortOrder
{
    /// <summary>
    /// Weapon skills in sort order: melee, then missile, then magic. Other skills follow, and no skill goes last.
    /// These are the skills combat uses on this server (WorldObject.ConvertToMoASkill of the weapon's WeaponSkill).
    /// </summary>
    private static readonly Skill[] SkillOrder =
    [
        Skill.MartialWeapons,
        Skill.Dagger,
        Skill.Staff,
        Skill.UnarmedCombat,
        Skill.TwoHandedCombat,
        Skill.Bow,
        Skill.ThrownWeapon,
        Skill.WarMagic,
        Skill.LifeMagic,
        Skill.VoidMagic,
    ];

    /// <summary>
    /// Equip slots head to toe: armor, then under-clothes (shirts, then pants), then shields and cloaks.
    /// An item is placed by the first of these it covers, so a coat sorts with chest pieces.
    /// </summary>
    private static readonly EquipMask[] SlotOrder =
    [
        EquipMask.HeadWear,
        EquipMask.ChestArmor,
        EquipMask.AbdomenArmor,
        EquipMask.UpperArmArmor,
        EquipMask.LowerArmArmor,
        EquipMask.HandWear,
        EquipMask.UpperLegArmor,
        EquipMask.LowerLegArmor,
        EquipMask.FootWear,
        EquipMask.ChestWear,
        EquipMask.UpperArmWear,
        EquipMask.LowerArmWear,
        EquipMask.AbdomenWear,
        EquipMask.UpperLegWear,
        EquipMask.LowerLegWear,
        EquipMask.Shield,
        EquipMask.Cloak,
    ];

    public static int Compare(WorldObject a, WorldObject b)
    {
        if (ReferenceEquals(a, b))
        {
            return 0;
        }

        var categoryA = BankCategories.Classify(a);
        var categoryB = BankCategories.Classify(b);

        var result = BankCategories.SortOrder(categoryA).CompareTo(BankCategories.SortOrder(categoryB));
        if (result != 0)
        {
            return result;
        }

        result = categoryA switch
        {
            BankCategory.Weapons => CompareWeapons(a, b),
            BankCategory.Armor => CompareArmor(a, b),
            BankCategory.Salvage => Salvage.GetSalvageBagSortKey(a).CompareTo(Salvage.GetSalvageBagSortKey(b)),
            _ => 0,
        };
        if (result != 0)
        {
            return result;
        }

        result = string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
        if (result != 0)
        {
            return result;
        }

        result = string.Compare(a.NameWithMaterial, b.NameWithMaterial, StringComparison.OrdinalIgnoreCase);
        if (result != 0)
        {
            return result;
        }

        result = (b.StackSize ?? 1).CompareTo(a.StackSize ?? 1);
        if (result != 0)
        {
            return result;
        }

        return a.Guid.Full.CompareTo(b.Guid.Full);
    }

    private static int CompareWeapons(WorldObject a, WorldObject b)
    {
        var subtypeA = (LootTables.WeaponSubtype?)a.WeaponSubtype;
        var subtypeB = (LootTables.WeaponSubtype?)b.WeaponSubtype;

        var result = SkillRank(WeaponSkillOf(a.ConvertToMoASkill(a.WeaponSkill), subtypeA))
            .CompareTo(SkillRank(WeaponSkillOf(b.ConvertToMoASkill(b.WeaponSkill), subtypeB)));
        if (result != 0)
        {
            return result;
        }

        return SubtypeRank(subtypeA).CompareTo(SubtypeRank(subtypeB));
    }

    private static int CompareArmor(WorldObject a, WorldObject b)
    {
        var result = WeightClassRank((ArmorWeightClass?)a.ArmorWeightClass)
            .CompareTo(WeightClassRank((ArmorWeightClass?)b.ArmorWeightClass));
        if (result != 0)
        {
            return result;
        }

        result = SlotRank(a.ValidLocations).CompareTo(SlotRank(b.ValidLocations));
        if (result != 0)
        {
            return result;
        }

        return StyleRank((ArmorStyle?)a.ArmorStyle).CompareTo(StyleRank((ArmorStyle?)b.ArmorStyle));
    }

    /// <summary>
    /// A weapon's skill as combat uses it. A weapon without one gets the skill its subtype belongs to.
    /// </summary>
    public static Skill WeaponSkillOf(Skill convertedWeaponSkill, LootTables.WeaponSubtype? subtype)
    {
        if (convertedWeaponSkill != Skill.None)
        {
            return convertedWeaponSkill;
        }

        return subtype switch
        {
            LootTables.WeaponSubtype.AxeLarge or LootTables.WeaponSubtype.AxeMedium or LootTables.WeaponSubtype.AxeSmall
                or LootTables.WeaponSubtype.MaceLarge or LootTables.WeaponSubtype.MaceMedium or LootTables.WeaponSubtype.MaceSmall
                or LootTables.WeaponSubtype.SpearLarge or LootTables.WeaponSubtype.SpearMedium or LootTables.WeaponSubtype.SpearSmall
                or LootTables.WeaponSubtype.SwordLarge or LootTables.WeaponSubtype.SwordMedium or LootTables.WeaponSubtype.SwordSmall
                => Skill.MartialWeapons,
            LootTables.WeaponSubtype.DaggerLarge or LootTables.WeaponSubtype.DaggerSmall => Skill.Dagger,
            LootTables.WeaponSubtype.StaffLarge or LootTables.WeaponSubtype.StaffMedium or LootTables.WeaponSubtype.StaffSmall
                => Skill.Staff,
            LootTables.WeaponSubtype.Ua => Skill.UnarmedCombat,
            LootTables.WeaponSubtype.TwohandAxe or LootTables.WeaponSubtype.TwohandMace
                or LootTables.WeaponSubtype.TwohandSpear or LootTables.WeaponSubtype.TwohandSword
                => Skill.TwoHandedCombat,
            LootTables.WeaponSubtype.BowLarge or LootTables.WeaponSubtype.BowSmall
                or LootTables.WeaponSubtype.CrossbowLarge or LootTables.WeaponSubtype.CrossbowSmall
                => Skill.Bow,
            LootTables.WeaponSubtype.AtlatlLarge or LootTables.WeaponSubtype.AtlatlSmall
                or LootTables.WeaponSubtype.ThrownAxe or LootTables.WeaponSubtype.ThrownJavelin
                or LootTables.WeaponSubtype.ThrownClub or LootTables.WeaponSubtype.ThrownDart
                or LootTables.WeaponSubtype.ThrownDagger or LootTables.WeaponSubtype.ThrownShuriken
                => Skill.ThrownWeapon,
            _ => Skill.None,
        };
    }

    public static int SkillRank(Skill skill)
    {
        var index = Array.IndexOf(SkillOrder, skill);
        if (index >= 0)
        {
            return index;
        }

        return skill == Skill.None ? SkillOrder.Length + 1 : SkillOrder.Length;
    }

    /// <summary>
    /// In WeaponSubtype order (each weapon large to small, then two-handed, missile, caster, thrown). None goes last.
    /// </summary>
    public static int SubtypeRank(LootTables.WeaponSubtype? subtype)
    {
        return subtype is null or LootTables.WeaponSubtype.Undef ? int.MaxValue : (int)subtype.Value;
    }

    /// <summary>
    /// Cloth, light, heavy, then armor with no weight class.
    /// </summary>
    public static int WeightClassRank(ArmorWeightClass? weightClass)
    {
        return weightClass switch
        {
            ArmorWeightClass.Cloth => 0,
            ArmorWeightClass.Light => 1,
            ArmorWeightClass.Heavy => 2,
            _ => 3,
        };
    }

    public static int SlotRank(EquipMask? slots)
    {
        if (slots is { } mask)
        {
            for (var i = 0; i < SlotOrder.Length; i++)
            {
                if ((mask & SlotOrder[i]) != 0)
                {
                    return i;
                }
            }
        }

        return SlotOrder.Length;
    }

    /// <summary>
    /// In ArmorStyle order (cloth, leather, studded, chain, plate, ... shields). None goes last.
    /// </summary>
    public static int StyleRank(ArmorStyle? style)
    {
        return style is null or ArmorStyle.None ? int.MaxValue : (int)style.Value;
    }
}
