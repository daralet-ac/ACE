using System;
using System.Linq;
using ACE.Entity.Enum;
using ACE.Server.Factories;
using ACE.Server.WorldObjects;

namespace ACE.Server.Entity.PlayerUi;

/// <summary>
/// A weapon's type for the player plugin's loot rules, which show the filters for its special property: each type rolls
/// the one LootGenerationFactory.TryMutateWeaponSubtypeBonuses gives it, so this sorts weapons the same way.
/// </summary>
public static class PlayerUiWeaponType
{
    // GetThrownWeaponsSubType's order, LootTables.ThrownWeaponMatrix's rows
    private static readonly string[] ThrownTypes = { "ThrownAxe", "ThrownClub", "ThrownDagger", "Dart", "Javelin", "Shuriken" };

    // GetCasterSubType's numbering: Orb, Scepter, Wand (batons too), Staff
    private static readonly string[] CasterTypes = { "Orb", "Scepter", "Wand", "CasterStaff" };

    /// <summary>Sword, Axe, Atlatl, ThrownAxe, Orb... or null for anything else, or a weapon that can't be told.</summary>
    public static string Of(WorldObject item)
    {
        switch (item.WeaponSkill)
        {
            case Skill.Sword: return "Sword";
            case Skill.Axe: return "Axe";
            case Skill.Mace: return "Mace";
            case Skill.Spear: return "Spear";
            case Skill.Dagger: return "Dagger";
            case Skill.Staff: return "Staff";
            case Skill.UnarmedCombat: return "Unarmed";
            case Skill.TwoHandedCombat: return "TwoHanded";
            case Skill.Bow: return "Bow";
            case Skill.Crossbow: return "Crossbow";
            // the loot atlatls' skill
            case Skill.MissileWeapons: return "Atlatl";
            case Skill.ThrownWeapon:
            {
                var subtype = LootGenerationFactory.GetThrownWeaponsSubType(item);
                return subtype < ThrownTypes.Length ? ThrownTypes[subtype] : null;
            }
            case Skill.WarMagic:
            case Skill.LifeMagic:
                return CasterType(item);
        }

        return BySubtype((LootTables.WeaponSubtype?)item.WeaponSubtype);
    }

    /// <summary>For a weapon whose skill is a combined one (MartialWeapons...): its loot subtype says what it is.</summary>
    private static string BySubtype(LootTables.WeaponSubtype? subtype)
    {
        return subtype switch
        {
            LootTables.WeaponSubtype.AxeLarge or LootTables.WeaponSubtype.AxeMedium or LootTables.WeaponSubtype.AxeSmall => "Axe",
            LootTables.WeaponSubtype.DaggerLarge or LootTables.WeaponSubtype.DaggerSmall => "Dagger",
            LootTables.WeaponSubtype.MaceLarge or LootTables.WeaponSubtype.MaceMedium or LootTables.WeaponSubtype.MaceSmall => "Mace",
            LootTables.WeaponSubtype.StaffLarge or LootTables.WeaponSubtype.StaffMedium or LootTables.WeaponSubtype.StaffSmall => "Staff",
            LootTables.WeaponSubtype.SpearLarge or LootTables.WeaponSubtype.SpearMedium or LootTables.WeaponSubtype.SpearSmall => "Spear",
            LootTables.WeaponSubtype.SwordLarge or LootTables.WeaponSubtype.SwordMedium or LootTables.WeaponSubtype.SwordSmall => "Sword",
            LootTables.WeaponSubtype.Ua => "Unarmed",
            LootTables.WeaponSubtype.TwohandAxe or LootTables.WeaponSubtype.TwohandMace
                or LootTables.WeaponSubtype.TwohandSpear or LootTables.WeaponSubtype.TwohandSword => "TwoHanded",
            LootTables.WeaponSubtype.AtlatlLarge or LootTables.WeaponSubtype.AtlatlSmall => "Atlatl",
            LootTables.WeaponSubtype.BowLarge or LootTables.WeaponSubtype.BowSmall => "Bow",
            LootTables.WeaponSubtype.CrossbowLarge or LootTables.WeaponSubtype.CrossbowSmall => "Crossbow",
            LootTables.WeaponSubtype.ThrownAxe => "ThrownAxe",
            LootTables.WeaponSubtype.ThrownClub => "ThrownClub",
            LootTables.WeaponSubtype.ThrownDagger => "ThrownDagger",
            LootTables.WeaponSubtype.ThrownDart => "Dart",
            LootTables.WeaponSubtype.ThrownJavelin => "Javelin",
            LootTables.WeaponSubtype.ThrownShuriken => "Shuriken",
            _ => null,
        };
    }

    /// <summary>
    /// By the loot tables' caster rows, as GetCasterSubType numbers them. That one calls any caster it doesn't know an orb,
    /// so a quest caster is told by its name instead.
    /// </summary>
    private static string CasterType(WorldObject item)
    {
        var wcid = (int)item.WeenieClassId;

        if (LootTables.TimelineCasterWeaponsMatrix[0].Contains(wcid))
        {
            return CasterTypes[Array.IndexOf(LootTables.TimelineCasterWeaponsMatrix[0], wcid)];
        }

        for (var i = 1; i < LootTables.TimelineCasterWeaponsMatrix.Length; i++)
        {
            if (LootTables.TimelineCasterWeaponsMatrix[i].Contains(wcid))
            {
                return CasterTypes[i - 1];
            }
        }

        var name = item.Name ?? "";
        bool Has(string word) => name.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0;

        return Has("Orb") ? "Orb"
            : Has("Sceptre") || Has("Scepter") ? "Scepter"
            : Has("Wand") || Has("Baton") ? "Wand"
            : Has("Staff") ? "CasterStaff"
            : null;
    }
}
