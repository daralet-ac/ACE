#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace ACE.MarketApi;

/// <summary>
/// How an item is classified and described from its properties: the half of MarketRepository that doesn't touch the
/// database. The game server compiles this file too (linked from apps/server), with ItemFacts.cs and MarketListing.cs,
/// so a corpse's items are described the same way as market listings (the Daralet plugin's loot rules).
/// </summary>
public partial class MarketRepository
{
    // ItemType (PropertyInt.ItemType) flags — ACE.Entity.Enum.ItemType.
    private const uint ItemTypeMeleeWeapon = 0x00000001;
    private const uint ItemTypeArmor = 0x00000002;
    private const uint ItemTypeClothing = 0x00000004;
    private const uint ItemTypeJewelry = 0x00000008;
    private const uint ItemTypeMissileWeapon = 0x00000100;
    private const uint ItemTypeSpellComponents = 0x00001000;
    private const uint ItemTypeCaster = 0x00008000;
    private const uint ItemTypeFood = 0x00000020;
    private const uint ItemTypeMisc = 0x00000080;
    private const uint ItemTypeWeaponMask = ItemTypeMeleeWeapon | ItemTypeMissileWeapon | ItemTypeCaster;
    private const uint ItemTypeArmorMask = ItemTypeArmor | ItemTypeClothing;

    // Icon underlay DAT ids — the real client always renders one of these colored background
    // tiles behind an item's icon art, chosen by broad ItemType category (distinct from the
    // elemental/magical outline recolor above the icon art, computed in ComputeIconEffect).
    private static readonly (uint Mask, string Category)[] IconUnderlayCategories =
    {
        (ItemTypeWeaponMask, "Weapon"),
        (ItemTypeArmor, "Armor"),
        (ItemTypeClothing, "Clothing"),
        (ItemTypeJewelry, "Jewelry"),
        (ItemTypeSpellComponents, "SpellComponent"),
    };

    private static string ComputeIconUnderlay(uint? itemType)
    {
        if (itemType.HasValue)
        {
            foreach (var (mask, category) in IconUnderlayCategories)
            {
                if ((itemType.Value & mask) != 0)
                {
                    return category;
                }
            }
        }

        return "Misc";
    }

    // DamageType (PropertyInt.DamageType) flags — ACE.Entity.Enum.DamageType. Only the elemental
    // subset matters for icon coloring (matches the client's own icon-underlay convention).
    private const int DamageTypeCold = 0x8;
    private const int DamageTypeFire = 0x10;
    private const int DamageTypeAcid = 0x20;
    private const int DamageTypeElectric = 0x40;

    // WeaponType (PropertyInt.WeaponType) — ACE.Entity.Enum.WeaponType (plain int enum, not flags).
    private const int WeaponTypeUnarmed = 1;
    private const int WeaponTypeSword = 2;
    private const int WeaponTypeAxe = 3;
    private const int WeaponTypeMace = 4;
    private const int WeaponTypeSpear = 5;
    private const int WeaponTypeDagger = 6;
    private const int WeaponTypeStaff = 7;
    private const int WeaponTypeBow = 8;
    private const int WeaponTypeCrossbow = 9;
    private const int WeaponTypeTwoHanded = 11;

    // DefaultCombatStyle (PropertyInt.DefaultCombatStyle) flags — ACE.Entity.Enum.CombatStyle.
    private const uint CombatStyleThrownWeapon = 0x00000080;
    private const uint CombatStyleAtlatl = 0x00000400;

    // WeaponSkill (PropertyInt.WeaponSkill) — ACE.Entity.Enum.Skill ordinals, verified by counting
    // the real enum source (libs/entity/Enum/Skill.cs) line-by-line from None=0, then spot-checked
    // live against 7 items missing WeaponType (Yari=Spear/9, Swordstaff+Flaming Jo=Staff/10,
    // Cestus+Frost Katar=UnarmedCombat/13, Flaming Long Sword=Sword/11, Lightning Morning Star=
    // Mace/5) — all matched exactly. These per-weapon-type skills are marked "/* Retired */" as
    // the *trained* skill (replaced by consolidated MartialWeapons/etc.), but the item's own
    // WeaponSkill property still uses them to flavor-classify the weapon, and some weenies (see
    // AddWeaponClassFilter/ClassifyWeapon) have WeaponType missing entirely, so this is used as a
    // fallback there.
    private const int SkillAxe = 1;
    private const int SkillBow = 2;
    private const int SkillCrossbow = 3;
    private const int SkillDagger = 4;
    private const int SkillMace = 5;
    private const int SkillSpear = 9;
    private const int SkillStaff = 10;
    private const int SkillSword = 11;
    private const int SkillThrownWeapon = 12;
    private const int SkillUnarmedCombat = 13;
    private const int SkillTwoHandedCombat = 41;
    private const int SkillLifeMagic = 33;
    private const int SkillWarMagic = 34;

    // AttackType (PropertyInt.AttackType) flags — ACE.Entity.Enum.AttackType. A small, reliably
    // populated swing-style flag set (unlike WeaponType, which some weenies genuinely lack) — used
    // only as a DPS fallback (Punch/Kick/OffhandPunch -> Unarmed anim length) when WeaponType itself
    // is missing.
    private const uint AttackTypePunch = 0x0001;
    private const uint AttackTypeKick = 0x0008;
    private const uint AttackTypeOffhandPunch = 0x0010;
    private const uint AttackTypeUnarmedMask = AttackTypePunch | AttackTypeKick | AttackTypeOffhandPunch;

    // ArmorWeightClass (PropertyInt.ArmorWeightClass) — ACE.Entity.Enum.ArmorWeightClass.
    private const int ArmorWeightCloth = 1;
    private const int ArmorWeightLight = 2;
    private const int ArmorWeightHeavy = 4;

    // ValidLocations (PropertyInt.ValidLocations) flags — ACE.Entity.Enum.EquipMask, jewelry slots.
    private const uint EquipMaskNeckWear = 0x00008000;
    private const uint EquipMaskWristWear = 0x00010000 | 0x00020000;
    private const uint EquipMaskFingerWear = 0x00040000 | 0x00080000;

    // ClothingPriority (PropertyInt.ClothingPriority) flags — ACE.Entity.Enum.CoverageMask.
    // Underwear/outerwear layers are combined per body part for a simple "does this cover X" filter.
    private static readonly Dictionary<string, uint> CoverageMasks = new()
    {
        ["Head"] = 0x00004000,
        ["Hands"] = 0x00008000,
        ["Feet"] = 0x00010000,
        ["Chest"] = 0x00000008 | 0x00000400,
        ["Abdomen"] = 0x00000010 | 0x00000800,
        ["UpperArms"] = 0x00000020 | 0x00001000,
        ["LowerArms"] = 0x00000040 | 0x00002000,
        ["UpperLegs"] = 0x00000002 | 0x00000100,
        ["LowerLegs"] = 0x00000004 | 0x00000200,
    };

    // SigilTrinketType (PropertyInt.SigilTrinketType) — apps/server/WorldObjects/SigilTrinket.cs.
    private static readonly string[] SigilTrinketTypeNames = { "Compass", "PuzzleBox", "Scarab", "PocketWatch", "Top", "Goggles" };

    // Every NameSuffix (minus the leading " of ") across apps/server/Factories/SigilTrinketConfig.cs's
    // effect maps — the full set of effect names a Sigil Trinket's name can end with.
    private static readonly string[] SigilEffectNames =
    {
        "Protection", "Vulnerability", "Artifice", "Growth",
        "Duplication", "Detonation", "Crushing",
        "Intensity", "Shielding", "Reduction",
        "Might", "Aggression",
        "Assailment", "Swift Killer",
        "Treachery", "Evasion", "Absorption", "Exposure", "Avoidance",
    };

    // Base trophy species/material names (PropertyInt.TrophyQuality-bearing weenies - 65 trophy
    // types, each with its own WCID per quality tier, class_Ids 1055100-1055749 in ace_world;
    // legacy pre-refactor single-WCID-per-trophy items, class_Ids 1054100-1054164, may still
    // exist on unmigrated shard items) — the stored Name is always "<QualityWord> <one of these>"
    // (see LootGenerationFactory.GetTrophyQualityName), so this doubles as both the Type
    // dropdown's option list and the suffix-match pattern used to classify/filter by it.
    private static readonly string[] TrophyTypeNames =
    {
        "Armoredillo Hide", "Armoredillo Spine", "Auroch Meat", "Auroch Horn", "Banderling Scalp",
        "Banderling Blood", "Chittick Spine", "Chittick Head", "Drudge Guts", "Drudge Charm",
        "Ectoplasm", "Doll Mask", "Violet Energy", "Grievver Silk", "Grievver Tibia",
        "Gromnie Tooth", "Gromnie Wing", "Brown Lump", "K'nath Egg", "Lugian Blood",
        "Lugian Sinew", "Mattekar Hide", "Mattekar Horn", "Mite Fur", "Mite Heart",
        "Monougat", "Monouga Skull", "Mosswart Eggs", "Swamp Stone", "Mu-miyah Arm",
        "Tomb Dust", "Niffis Shell", "Niffis Pearl", "Olthoi Claw", "Olthoi Ichor",
        "Wasp Venom", "Wasp Wing", "Rat Tail", "Rat Saliva", "Reedshark Fang",
        "Reedshark Hide", "Sclavus Hide", "Sclavus Tongue", "Shreth Tooth", "Shreth Hide",
        "Old Bone", "Skull", "Tusker Pelt", "Tusker Tusk", "Undead Leg",
        "Mnemosyne", "Ursuin Fang", "Ursuin Hide", "Zefir Gossamer", "Zefir Wing",
        "Wisp Heart", "Wisp Essence", "Tumerok Insignia", "Tumerok Salted Meats", "Moarsmuck",
        "Moarsman Head", "Crystalized Fire", "Crystalized Frost", "Crystalized Acid", "Crystalized Lightning",
    };

    // WeenieType (top-level MarketListingSnapshot.WeenieType field, serialized as its numeric value —
    // no JsonStringEnumConverter is registered on the snapshot serializer) — ACE.Entity.Enum.WeenieType.
    private const int WeenieTypeSalvage = 76;
    private const int WeenieTypeFood = 18;
    private const int WeenieTypeHealer = 28;
    private const int WeenieTypeScroll = 34;
    private const int WeenieTypeGem = 38;
    private const int WeenieTypeJewel = 75;

    // MaterialType (PropertyInt.MaterialType) -> which item category the salvage can actually be
    // applied to, derived from Salvage.CheckTinkerType's real eligibility gates (not the tinkering
    // skill/discipline — a material can be eligible for more than one target, e.g. Blacksmithing
    // metals work on both weapons and Heavy armor per Salvage.cs's Blacksmithing branch):
    //  - Blacksmithing metals (Brass/Bronze/Copper/Gold/Iron/Pyreal/Silver/Steel): MeleeWeapon,
    //    MissileWeapon, or ArmorWeightClass==Heavy -> targets Weapon AND Armor.
    //  - Tailoring materials (Leather/ArmoredilloHide/GromnieHide/ReedSharkHide/Linen/Satin/Silk/
    //    Velvet/Wool): ArmorWeightClass Light or Cloth -> targets Armor only.
    //  - Woodworking woods: WeenieType.MissileLauncher -> targets Weapon only.
    //  - Spellcrafting gems (weapon-only-imbue set, general ImbueSalvage set, and the "wands only"
    //    caster-gated set are ALL restricted to MeleeWeapon/MissileWeapon/Caster) -> targets Weapon only.
    //  - Jewelcrafting gems: ItemType.Jewelry only -> targets Jewelry only.
    private static readonly HashSet<int> SalvageWeaponTargetMaterials = new()
    {
        // Blacksmithing
        57, 58, 59, 60, 61, 62, 63, 64,
        // Woodworking
        72, 73, 74, 75, 76, 77,
        // Spellcrafting (all gates on MeleeWeapon/MissileWeapon/Caster — see CheckTinkerType)
        12, 13, 15, 16, 21, 22, 23, 26, 27, 29, 30, 33, 35, 37, 41, 43, 47,
    };

    private static readonly HashSet<int> SalvageArmorTargetMaterials = new()
    {
        // Blacksmithing (Heavy armor only)
        57, 58, 59, 60, 61, 62, 63, 64,
        // Tailoring (Light/Cloth armor)
        52, 53, 54, 55, 4, 5, 6, 7, 8,
    };

    private static readonly HashSet<int> SalvageJewelryTargetMaterials = new()
    {
        // Jewelcrafting
        10, 11, 14, 17, 18, 19, 20, 24, 25, 28, 31, 32, 34, 36, 38, 39, 71, 40, 42, 44, 45, 46, 48, 49, 50,
    };

    private static List<string> GetSalvageTargetTypes(int materialId)
    {
        var targets = new List<string>();
        if (SalvageWeaponTargetMaterials.Contains(materialId))
        {
            targets.Add("Weapon");
        }
        if (SalvageArmorTargetMaterials.Contains(materialId))
        {
            targets.Add("Armor");
        }
        if (SalvageJewelryTargetMaterials.Contains(materialId))
        {
            targets.Add("Jewelry");
        }
        return targets;
    }

    private static readonly Dictionary<int, string> MaterialTypeNames = new()
    {
        [4] = "Linen", [5] = "Satin", [6] = "Silk", [7] = "Velvet", [8] = "Wool",
        [10] = "Agate", [11] = "Amber", [12] = "Amethyst", [13] = "Aquamarine", [14] = "Azurite",
        [15] = "BlackGarnet", [16] = "BlackOpal", [17] = "Bloodstone", [18] = "Carnelian", [19] = "Citrine",
        [20] = "Diamond", [21] = "Emerald", [22] = "FireOpal", [23] = "GreenGarnet", [24] = "GreenJade",
        [25] = "Hematite", [26] = "ImperialTopaz", [27] = "Jet", [28] = "LapisLazuli", [29] = "LavenderJade",
        [30] = "Malachite", [31] = "Moonstone", [32] = "Onyx", [33] = "Opal", [34] = "Peridot",
        [35] = "RedGarnet", [36] = "RedJade", [37] = "RoseQuartz", [38] = "Ruby", [39] = "Sapphire",
        [40] = "SmokeyQuartz", [41] = "Sunstone", [42] = "TigerEye", [43] = "Tourmaline", [44] = "Turquoise",
        [45] = "WhiteJade", [46] = "WhiteQuartz", [47] = "WhiteSapphire", [48] = "YellowGarnet",
        [49] = "YellowTopaz", [50] = "Zircon", [52] = "Leather", [53] = "ArmoredilloHide",
        [54] = "GromnieHide", [55] = "ReedSharkHide", [57] = "Brass", [58] = "Bronze", [59] = "Copper",
        [60] = "Gold", [61] = "Iron", [62] = "Pyreal", [63] = "Silver", [64] = "Steel", [71] = "Serpentine",
        [72] = "Wood", [73] = "Ebony", [74] = "Mahogany", [75] = "Oak", [76] = "Pine", [77] = "Teak",
    };

    // ImbuedEffect (PropertyInt.ImbuedEffect) flags — ACE.Entity.Enum.ImbuedEffectType (subset actually
    // relevant to buyers; render-only, so an incomplete list just omits a rare effect from the tooltip).
    private static readonly (uint Flag, string Name)[] ImbuedEffectFlags =
    {
        (0x1, "Critical Strike"), (0x2, "Crippling Blow"), (0x4, "Armor Rending"),
        (0x8, "Slash Rending"), (0x10, "Pierce Rending"), (0x20, "Bludgeon Rending"),
        (0x40, "Acid Rending"), (0x80, "Cold Rending"), (0x100, "Electric Rending"),
        (0x200, "Fire Rending"), (0x400, "Melee Defense"), (0x800, "Missile Defense"),
        (0x1000, "Magic Defense"), (0x2000, "Spellbook"), (0x4000, "Nether Rending"),
        (0x8000, "Ward Rending"), (0x40000000, "Always Critical"), (0x80000000, "Ignore All Armor"),
        (0x10000, "Armor Tempering"), (0x20000, "Ward Tempering"), (0x40000, "Critical Resistance"), (0x80000, "Critical Damage Resistance"),
    };

    // Gear* rating properties (PropertyInt) — ACE.Entity.Enum.PropertyInt, wired to display text by
    // AppraiseInfo.BuildProperties() (apps/server/Network/Structure/AppraiseInfo.cs, ~line 875-926).
    // Shown as "{Name} {RomanNumeral}" once the rating reaches 1. The real client also factors in
    // jewel-socket and other-equipped-item contributions to the total; here we only have this one
    // item's own value (no wielder/other-gear context), so the numeral reflects just this item.
    private static readonly (string Property, string Name)[] GearRatingProperties =
    {
        ("GearStrength", "Mighty Thews"), ("GearEndurance", "Perseverance"), ("GearCoordination", "Dexterous Hand"),
        ("GearQuickness", "Swift-footed"), ("GearFocus", "Focused Mind"), ("GearSelf", "Erudite Mind"),
        ("GearSelfHarm", "Blood Frenzy"), ("GearThreatGain", "Provocation"), ("GearThreatReduction", "Clouded Vision"),
        ("GearElementalWard", "Prismatic Ward"), ("GearPhysicalWard", "Black Bulwark"), ("GearMagicFind", "Seeker"),
        ("GearBlock", "Stalwart Defense"), ("GearItemManaUsage", "Thrifty Scholar"), ("GearThorns", "Swift Retribution"),
        ("GearVitalsTransfer", "Tilted Scales"), ("GearRedFury", "Red Fury"), ("GearYellowFury", "Yellow Fury"),
        ("GearBlueFury", "Blue Fury"), ("GearSelflessness", "Selfless Spirit"), ("GearFamiliarity", "Familiar Foe"),
        ("GearBravado", "Bravado"), ("GearHealthToStamina", "Masochist"), ("GearHealthToMana", "Austere Anchorite"),
        ("GearExperienceGain", "Illuminated Mind"), ("GearLifesteal", "Sanguine Thirst"), ("GearStaminasteal", "Vigor Siphon"),
        ("GearManasteal", "Ophidian"), ("GearBludgeon", "Skull-cracker"), ("GearPierce", "Precision Strikes"),
        ("GearSlash", "Falcon's Gyre"), ("GearFire", "Blazing Brand"), ("GearFrost", "Bone-chiller"),
        ("GearAcid", "Devouring Mist"), ("GearLightning", "Astyrrian's Rage"), ("GearHealBubble", "Purified Soul"),
        ("GearCompBurn", "Meticulous Magus"), ("GearPyrealFind", "Prosperity"), ("GearNullification", "Nullification"),
        ("GearWardPen", "Ruthless Discernment"), ("GearHardenedDefense", "Hardened Fortification"), ("GearReprisal", "Vicious Reprisal"),
        ("GearElementalist", "Elementalist"), ("GearToughness", "Toughness"), ("GearResistance", "Resistance"),
        ("GearSlashBane", "Swordsman's Bane"), ("GearBludgeonBane", "Tusker's Bane"), ("GearPierceBane", "Archer's Bane"),
        ("GearAcidBane", "Olthoi's Bane"), ("GearFireBane", "Inferno's Bane"), ("GearFrostBane", "Gelidite's Bane"),
        ("GearLightningBane", "Astyrrian's Bane"), ("GearFrigidProtection", "Frigid Resistance"),
    };

    private static readonly (int Value, string Numeral)[] RomanNumerals =
    {
        (1000, "M"), (900, "CM"), (500, "D"), (400, "CD"), (100, "C"), (90, "XC"), (50, "L"), (40, "XL"),
        (10, "X"), (9, "IX"), (5, "V"), (4, "IV"), (1, "I"),
    };

    private static string ToRoman(int n)
    {
        if (n <= 0)
        {
            return n.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        var sb = new System.Text.StringBuilder();
        foreach (var (value, numeral) in RomanNumerals)
        {
            while (n >= value)
            {
                sb.Append(numeral);
                n -= value;
            }
        }

        return sb.ToString();
    }

    private static readonly Dictionary<int, string> DamageTypeElementNames = new()
    {
        [0x1] = "Slashing", [0x2] = "Piercing", [0x4] = "Bludgeoning", [0x8] = "Cold",
        [0x10] = "Fire", [0x20] = "Acid", [0x40] = "Electric", [0x400] = "Nether",
    };

    // Every damage type in a DamageType's flags, by name: "Slashing", "Slashing/Piercing". Null for none.
    private static string? DamageTypeNames(int? damageType)
    {
        if (damageType is not > 0)
        {
            return null;
        }

        var names = DamageTypeElementNames.Where(t => (damageType.Value & t.Key) != 0).Select(t => t.Value).ToList();
        return names.Count > 0 ? string.Join("/", names) : null;
    }

    // WieldRequirement enum (ACE.Entity.Enum.WieldRequirement) — this project has no reference to
    // the main ACE libs, so the ordinals are hardcoded here (verified against libs/entity/Enum/
    // WieldRequirement.cs). Only the types relevant to display (Level, primary Attrib, Skill) are named.
    private const int WieldReqSkill = 1;
    private const int WieldReqRawSkill = 2;
    private const int WieldReqAttrib = 3;
    private const int WieldReqRawAttrib = 4;
    private const int WieldReqLevel = 7;

    // PropertyAttribute enum ordinals (verified against libs/entity/Enum/Properties/PropertyAttribute.cs).
    private static readonly Dictionary<int, string> AttributeNames = new()
    {
        [1] = "Strength", [2] = "Endurance", [3] = "Quickness", [4] = "Coordination", [5] = "Focus", [6] = "Self",
    };

    // Skill enum ordinals (verified against libs/entity/Enum/Skill.cs via the same scratchpad
    // console-app technique used for the Weapon-classification WeaponSkill fallback), for
    // displaying "requires X <skill>" wield requirements. Names are hand-spaced from the enum's
    // PascalCase member names for readability.
    private static readonly Dictionary<int, string> SkillNames = new()
    {
        [1] = "Axe", [2] = "Bow", [3] = "Crossbow", [4] = "Dagger", [5] = "Mace",
        [6] = "Physical Defense", [7] = "Missile Defense", [8] = "Sling", [9] = "Spear", [10] = "Staff",
        [11] = "Sword", [12] = "Thrown Weapon", [13] = "Unarmed Combat", [14] = "Arcane Lore", [15] = "Magic Defense",
        [16] = "Mana Conversion", [17] = "Spellcraft", [18] = "Jewelcrafting", [19] = "Assess Person", [20] = "Deception",
        [21] = "Healing", [22] = "Jump", [23] = "Thievery", [24] = "Run", [25] = "Awareness",
        [26] = "Arms and Armor Repair", [27] = "Perception", [28] = "Blacksmithing", [29] = "Tailoring", [30] = "Spellcrafting",
        [31] = "Creature Enchantment", [32] = "Portal Magic", [33] = "Life Magic", [34] = "War Magic", [35] = "Leadership",
        [36] = "Loyalty", [37] = "Woodworking", [38] = "Alchemy", [39] = "Cooking", [40] = "Salvaging",
        [41] = "Two Handed Combat", [42] = "Gearcraft", [43] = "Void Magic", [44] = "Martial Weapons", [45] = "Light Weapons",
        [46] = "Finesse Weapons", [47] = "Missile Weapons", [48] = "Shield", [49] = "Dual Wield", [50] = "Recklessness",
        [51] = "Sneak Attack", [52] = "Dirty Fighting", [53] = "Challenge", [54] = "Summoning",
    };

    // Walks an item's (up to 4) wield-requirement slots and picks out: the first Level requirement
    // (LevelReq), the first primary-Attribute requirement (ReqAttribute/ReqAttributeAmount), and
    // formats any other Attribute/Skill requirement found in the remaining slots as display strings.
    private static (int? LevelReq, string? ReqAttribute, int? ReqAttributeAmount, List<string> Other) ComputeWieldRequirements(
        (int? Req, int? SkillType, int? Difficulty)[] slots)
    {
        int? levelReq = null;
        string? reqAttribute = null;
        int? reqAttributeAmount = null;
        var other = new List<string>();
        var primaryAttribSet = false;

        foreach (var (req, skillType, difficulty) in slots)
        {
            if (!req.HasValue || req.Value == 0 || !difficulty.HasValue)
            {
                continue;
            }

            if (req.Value == WieldReqLevel)
            {
                levelReq ??= difficulty;
            }
            else if (req.Value is WieldReqAttrib or WieldReqRawAttrib && skillType.HasValue
                     && AttributeNames.TryGetValue(skillType.Value, out var attribName))
            {
                if (!primaryAttribSet)
                {
                    reqAttribute = attribName;
                    reqAttributeAmount = difficulty;
                    primaryAttribSet = true;
                }
                else
                {
                    other.Add($"{difficulty} {attribName}");
                }
            }
            else if (req.Value is WieldReqSkill or WieldReqRawSkill && skillType.HasValue
                     && SkillNames.TryGetValue(skillType.Value, out var skillName))
            {
                other.Add($"{difficulty} {skillName}");
            }
        }

        return (levelReq, reqAttribute, reqAttributeAmount, other);
    }

    // WorldObject.CrushingBlowScale, which the server's tests check this matches: the Market API can't see WorldObject.
    public const double CrushingBlowScale = 1.3;

    // The amounts of the Properties loot generation rolls, as appraisal's long descriptions work them out
    // (AppraiseInfo: Biting Strike, Crushing Blow, Stamina Cost Reduction, Armor and Ward Cleaving), with the same
    // conditions ComputeProperties adds their names on.
    private static Dictionary<string, double>? ComputePropertyValues(
        double? staminaCostReductionMod, double? ignoreWard, double? ignoreArmor, double? criticalMultiplier, double? criticalFrequency)
    {
        var values = new Dictionary<string, double>();

        if (staminaCostReductionMod is > 0.001) values["Stamina Cost Reduction"] = Math.Round(staminaCostReductionMod.Value * 100);
        if (ignoreWard is { } ward and not 0) values["Ward Cleaving"] = 100 - Math.Round(ward * 100);
        if (ignoreArmor is { } armor and not 0) values["Armor Cleaving"] = 100 - Math.Round(armor * 100);
        if (criticalMultiplier is { } multiplier and > 1) values["Crushing Blow"] = Math.Round((multiplier - 1) * CrushingBlowScale * 100);
        if (criticalFrequency is { } frequency and > 0) values["Biting Strike"] = Math.Round((frequency - 0.1) * 100);

        return values.Count > 0 ? values : null;
    }

    // Named mods/bonuses shown in the client's "Properties"/"Additional Properties" tooltip
    // sections (AppraiseInfo.BuildProperties/AddPropertyEnchantments) — Gear* ratings, imbue
    // effects (reusing the already-computed ImbuedEffects list), and the handful of threshold-
    // gated float mods (Cleaving/Crushing/Biting/Primacy). Wielder-relative bonuses (Rending %,
    // Crippling/Critical Strike %, jewel-socket contributions to Gear ratings) aren't derivable
    // from a single unequipped listing, so only the mod's presence/name is shown, not its %.
    private static List<string> ComputeProperties(
        string? allPropertiesIntRaw, List<string> imbuedEffects, double? staminaCostReductionMod,
        double? ignoreWard, double? ignoreArmor, double? criticalMultiplier, double? criticalFrequency,
        double? resistanceModifier, int? resistanceModifierType, int? noCompsRequiredForMagicSchool)
    {
        var mods = new List<string>();

        if (!string.IsNullOrEmpty(allPropertiesIntRaw))
        {
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(allPropertiesIntRaw);
                foreach (var (property, name) in GearRatingProperties)
                {
                    if (doc.RootElement.TryGetProperty(property, out var el) && el.TryGetInt32(out var rating) && rating >= 1)
                    {
                        mods.Add($"{name} {ToRoman(rating)}");
                    }
                }
            }
            catch (System.Text.Json.JsonException)
            {
                // Malformed/unexpected JSON shape — skip Gear ratings for this listing rather than fail the whole request.
            }
        }

        mods.AddRange(imbuedEffects);

        if (staminaCostReductionMod is > 0.001) mods.Add("Stamina Cost Reduction");
        if (ignoreWard is not null and not 0) mods.Add("Ward Cleaving");
        if (ignoreArmor is not null and not 0) mods.Add("Armor Cleaving");
        if (criticalMultiplier > 1) mods.Add("Crushing Blow");
        if (criticalFrequency > 0) mods.Add("Biting Strike");
        if (resistanceModifier is not null and not 0)
        {
            var element = resistanceModifierType.HasValue && DamageTypeElementNames.TryGetValue(resistanceModifierType.Value, out var elName) ? elName : "Unknown";
            mods.Add($"Resistance Cleaving ({element})");
        }
        if (noCompsRequiredForMagicSchool is 1) mods.Add("War Primacy");
        else if (noCompsRequiredForMagicSchool is 2) mods.Add("Life Primacy");
        else if (noCompsRequiredForMagicSchool is 3) mods.Add("Portal Primacy");

        return mods;
    }

    // Current item level, computed from the item's XP fields — mirrors WorldObject.ItemLevel /
    // ExperienceSystem.ItemTotalXPToLevel (apps/server/Entity/ExperienceSystem.cs) exactly:
    // ItemXpStyle.Fixed(1): floor(xp/base); ScalesWithLevel(2, the default): each level doubles the
    // XP cost of the last (closed form: floor(log2(xp/base + 1))); FixedPlusBase(3, noted "unused?"
    // in source) approximated the same as Fixed here since it's not exercised by any live item.
    private const string ItemLevelSqlExpr = """
        LEAST(
          COALESCE(CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.ItemMaxLevel') AS SIGNED), 999999),
          CASE CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.ItemXpStyle') AS UNSIGNED)
            WHEN 2 THEN FLOOR(LOG2(CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt64.ItemTotalXp') AS DOUBLE) / NULLIF(CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt64.ItemBaseXp') AS DOUBLE), 0) + 1))
            ELSE FLOOR(CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt64.ItemTotalXp') AS DOUBLE) / NULLIF(CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt64.ItemBaseXp') AS DOUBLE), 0))
          END
        )
        """;

    // Launchers (Bow/Crossbow/Atlatl) carry no Damage/DamageVariance of their own — only a
    // DamageMod multiplier applied to whatever ammo is loaded, which isn't part of the item
    // snapshot. For a comparable DPS figure, we substitute the AmmoDam value from the matching
    // ItemTier (1-8) in the project's verified per-tier missile table (weapontierdata.js
    // MISSILE_WEAPON_TIERS) — AmmoDam only varies by launcher type, not by Large/Small.
    private static readonly int[] AmmoDamByTierBow = { 6, 8, 10, 12, 14, 16, 18, 20 };
    private static readonly int[] AmmoDamByTierCrossbow = { 9, 12, 15, 18, 21, 24, 27, 30 };
    private static readonly int[] AmmoDamByTierAtlatl = { 12, 16, 20, 24, 28, 32, 36, 40 };

    private static string AmmoDamSqlCase(int[] ammoDamByTier) =>
        "CASE ItemTier " + string.Join(" ", ammoDamByTier.Select((v, i) => $"WHEN {i + 1} THEN {v}")) + " ELSE NULL END";

    // Casters (both War and Life Magic) don't swing/reload at all — DPS is instead modeled on
    // casting a common Bolt spell (Force/Flame/Frost/Acid/Lightning Bolt) at spell level = the
    // item's own ItemTier. Cast time is fixed per tier (1.04s base + a per-tier windup, NOT
    // moddable by WeaponTime/Quickness the way melee/missile swing speed is) and damage is the
    // tier's Bolt damage range midpoint, scaled by the item's own ElementalDamageMod — which Life
    // Magic casters also roll (at half the War Magic rate; see LootGenerationFactory_Caster.cs),
    // since a Life-skill wand's wielder can still cast War spells through it.
    private const double BoltCastBaseTime = 1.04;
    private static readonly double[] BoltCastCycleTimeByTier =
        { BoltCastBaseTime + 0, BoltCastBaseTime + 0, BoltCastBaseTime + 0.54, BoltCastBaseTime + 1.001, BoltCastBaseTime + 1.4375, BoltCastBaseTime + 1.7763, BoltCastBaseTime + 1.8382, BoltCastBaseTime + 1.8382 };
    private static readonly double[] BoltAvgDamageByTier = { 9, 9, 18, 27, 36, 45, 54, 63 };

    private static string WarMagicBoltDpsSqlCase() =>
        "CASE ItemTier " + string.Join(" ", BoltAvgDamageByTier.Select((dmg, i) => $"WHEN {i + 1} THEN {(dmg / BoltCastCycleTimeByTier[i]).ToString(System.Globalization.CultureInfo.InvariantCulture)}")) + " ELSE NULL END";

    // DPS — a "weapon-only" reference figure, since a market listing has no wielder: one hit's damage times attacks a
    // second, 60 / WeaponTime (so it agrees with the atk/s the player plugin shows):
    //   dps = avgDamage * damageMod * 60 / weaponTime                  (melee/thrown)
    //   dps = ammoDam(itemTier) * damageMod * 60 / weaponTime          (missile launchers)
    //   dps = boltAvgDamage(itemTier) * elementalDamageMod / boltCycleTime(itemTier)  (casters, which have no WeaponTime)
    // AnimSpeedSqlExpr (the client's animation speed, clamp(2 - weaponTime/100, 1, 2.5)) is still the "speed" sort.
    private static readonly string AnimSpeedSqlExpr =
        $"LEAST(2.5, GREATEST(1.0, 2.0 - CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.WeaponTime') AS DOUBLE) / 100.0))";

    private static readonly string MeleeDamageSqlExpr =
        """
        (CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.Damage') AS DOUBLE)
          * (1 - CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.DamageVariance') AS DOUBLE) / 2)
          * COALESCE(CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.DamageMod') AS DOUBLE), 1.0))
        """;

    private static string MissileDamageSqlExpr(int[] ammoDamByTier) =>
        $"({AmmoDamSqlCase(ammoDamByTier)} * COALESCE(CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.DamageMod') AS DOUBLE), 1.0))";

    // Attacks a second for a weapon: 60 / WeaponTime, so a WeaponTime of 60 is one a second (Daralet's rule, which the
    // player plugin's tooltips show as atk/s). Null for a WeaponTime of 0 or none.
    private const string AttacksPerSecondSqlExpr =
        "(60.0 / NULLIF(CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.WeaponTime') AS DOUBLE), 0))";

    private static readonly string DpsSqlExpr = $"""
        (
          CASE
            WHEN (CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.ItemType') AS UNSIGNED) & {ItemTypeCaster}) != 0
              AND CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.WeaponSkill') AS SIGNED) IN ({SkillWarMagic},{SkillLifeMagic})
              THEN {WarMagicBoltDpsSqlCase()} * COALESCE(CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ElementalDamageMod') AS DOUBLE), 1.0)
            WHEN (CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.DefaultCombatStyle') AS UNSIGNED) & {CombatStyleThrownWeapon}) != 0
              THEN {MeleeDamageSqlExpr} * {AttacksPerSecondSqlExpr}
            WHEN CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.WeaponType') AS SIGNED) = {WeaponTypeBow}
              THEN {MissileDamageSqlExpr(AmmoDamByTierBow)} * {AttacksPerSecondSqlExpr}
            WHEN CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.WeaponType') AS SIGNED) = {WeaponTypeCrossbow}
              THEN {MissileDamageSqlExpr(AmmoDamByTierCrossbow)} * {AttacksPerSecondSqlExpr}
            WHEN (CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.DefaultCombatStyle') AS UNSIGNED) & {CombatStyleAtlatl}) != 0
              THEN {MissileDamageSqlExpr(AmmoDamByTierAtlatl)} * {AttacksPerSecondSqlExpr}
            WHEN CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.WeaponType') AS SIGNED)
                IN ({WeaponTypeDagger},{WeaponTypeSpear},{WeaponTypeUnarmed},{WeaponTypeTwoHanded},{WeaponTypeSword},{WeaponTypeAxe},{WeaponTypeMace},{WeaponTypeStaff})
              THEN {MeleeDamageSqlExpr} * {AttacksPerSecondSqlExpr}
            WHEN (CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.ItemType') AS UNSIGNED) & {ItemTypeCaster}) = 0
              AND CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.AttackType') AS UNSIGNED) > 0
              THEN {MeleeDamageSqlExpr} * {AttacksPerSecondSqlExpr}
            ELSE NULL
          END
        )
        """;

    // Wraps a raw stat SQL expression so it's divided by the item's ArmorSlots (min 1) — Ward Level
    // and the weight-specific Skill Mods filter/compare on a per-slot basis, since a piece covering
    // more body slots can otherwise carry a proportionally larger total for the same per-slot value.
    private static string PerSlotExpr(string baseExpr) =>
        $"(({baseExpr}) / GREATEST(CAST(COALESCE(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.ArmorSlots'), 1) AS UNSIGNED), 1))";

    // Sort keys accepted from the client, mapped to a SQL expression. This is an explicit allow-list —
    // sortBy is never used to build SQL directly — so an unrecognized key just falls back to the default.
    private static readonly Dictionary<string, string> SortExpressions = new()
    {
        ["sigilLevel"] = ItemLevelSqlExpr,
        ["name"] = "JSON_UNQUOTE(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesString.Name'))",
        ["price"] = "ListedPrice",
        ["tier"] = "ItemTier",
        ["expires"] = "ExpiresAtUtc",
        ["avgDamage"] = "(CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.Damage') AS DOUBLE) * (1 - CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.DamageVariance') AS DOUBLE) / 2))",
        ["baseDamage"] = "CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.Damage') AS SIGNED)",
        ["damageVariance"] = "CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.DamageVariance') AS DOUBLE)",
        ["weaponSpeed"] = AnimSpeedSqlExpr,
        ["dps"] = DpsSqlExpr,
        ["attackMod"] = "CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.WeaponOffense') AS DOUBLE)",
        ["warMagicMod"] = "CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.WeaponWarMagicMod') AS DOUBLE)",
        ["lifeMagicMod"] = "CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.WeaponLifeMagicMod') AS DOUBLE)",
        ["damageMod"] = "CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.DamageMod') AS DOUBLE)",
        ["elementalDamageMod"] = "CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ElementalDamageMod') AS DOUBLE)",
        ["restorationMod"] = "CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.WeaponRestorationSpellsMod') AS DOUBLE)",
        ["physDefMod"] = "CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.WeaponPhysicalDefense') AS DOUBLE)",
        ["magicDefMod"] = "CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.WeaponMagicalDefense') AS DOUBLE)",
        ["spellcraft"] = "CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.ItemSpellcraft') AS SIGNED)",
        ["difficulty"] = "CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.ItemDifficulty') AS SIGNED)",
        ["mana"] = "CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.ItemMaxMana') AS SIGNED)",
        ["armorLevel"] = "CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.ArmorLevel') AS SIGNED)",
        ["ward"] = "CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.WardLevel') AS SIGNED)",
        ["procChance"] = "CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.SigilTrinketTriggerChance') AS DOUBLE)",
        ["cooldown"] = "CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.SigilTrinketCooldown') AS DOUBLE)",
        ["manaReserved"] = "CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.SigilTrinketManaReserved') AS DOUBLE)",
        ["staminaReserved"] = "CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.SigilTrinketStaminaReserved') AS DOUBLE)",
        ["healthReserved"] = "CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.SigilTrinketHealthReserved') AS DOUBLE)",
        ["workmanship"] = "(CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.ItemWorkmanship') AS DOUBLE) / CAST(COALESCE(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.NumItemsInMaterial'), 1) AS DOUBLE))",
        ["units"] = "CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.Structure') AS SIGNED)",
        ["pricePerUnit"] = "(ListedPrice / NULLIF(CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.Structure') AS SIGNED), 0))",
        ["armorModVsSlash"] = "CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorModVsSlash') AS DOUBLE)",
        ["armorModVsPierce"] = "CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorModVsPierce') AS DOUBLE)",
        ["armorModVsBludgeon"] = "CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorModVsBludgeon') AS DOUBLE)",
        ["armorModVsCold"] = "CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorModVsCold') AS DOUBLE)",
        ["armorModVsFire"] = "CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorModVsFire') AS DOUBLE)",
        ["armorModVsAcid"] = "CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorModVsAcid') AS DOUBLE)",
        ["armorModVsElectric"] = "CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorModVsElectric') AS DOUBLE)",
        ["armorWarMagicMod"] = "CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorWarMagicMod') AS DOUBLE)",
        ["armorLifeMagicMod"] = "CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorLifeMagicMod') AS DOUBLE)",
        ["armorManaRegenMod"] = "CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorManaRegenMod') AS DOUBLE)",
        ["armorManaConversionMod"] = "CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ManaConversionMod') AS DOUBLE)",
        ["armorAttackMod"] = "CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorAttackMod') AS DOUBLE)",
        ["armorShieldMod"] = "CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorShieldMod') AS DOUBLE)",
        ["armorDualWieldMod"] = "CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorDualWieldMod') AS DOUBLE)",
        ["armorThieveryMod"] = "CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorThieveryMod') AS DOUBLE)",
        ["armorRunMod"] = "CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorRunMod') AS DOUBLE)",
        ["armorStaminaRegenMod"] = "CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorStaminaRegenMod') AS DOUBLE)",
        ["armorPhysicalDefMod"] = "CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorPhysicalDefMod') AS DOUBLE)",
        ["armorMagicDefMod"] = "CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorMagicDefMod') AS DOUBLE)",
        ["armorTwohandedCombatMod"] = "CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorTwohandedCombatMod') AS DOUBLE)",
        ["armorHealthRegenMod"] = "CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorHealthRegenMod') AS DOUBLE)",
        ["armorPerceptionMod"] = "CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorPerceptionMod') AS DOUBLE)",
        ["armorDeceptionMod"] = "CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorDeceptionMod') AS DOUBLE)",
        ["armorHealthMod"] = "CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorHealthMod') AS DOUBLE)",
        ["armorManaMod"] = "CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorManaMod') AS DOUBLE)",
        ["armorStaminaMod"] = "CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorStaminaMod') AS DOUBLE)",
        ["gearCritDamage"] = "CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.GearCritDamage') AS SIGNED)",
        ["gearCritDamageResist"] = "CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.GearCritDamageResist') AS SIGNED)",
        ["gearDamage"] = "CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.GearDamage') AS SIGNED)",
        ["gearDamageResist"] = "CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.GearDamageResist') AS SIGNED)",
        ["gearHealingBoost"] = "CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.GearHealingBoost') AS SIGNED)",
        ["gearMaxHealth"] = "CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.GearMaxHealth') AS SIGNED)",
        // Per-slot variants — used for the Ward Level / Skill Mods filters instead of the raw totals above.
        ["wardPerSlot"] = PerSlotExpr("CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.WardLevel') AS SIGNED)"),
        ["armorWarMagicModPerSlot"] = PerSlotExpr("CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorWarMagicMod') AS DOUBLE)"),
        ["armorLifeMagicModPerSlot"] = PerSlotExpr("CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorLifeMagicMod') AS DOUBLE)"),
        ["armorManaRegenModPerSlot"] = PerSlotExpr("CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorManaRegenMod') AS DOUBLE)"),
        ["armorManaConversionModPerSlot"] = PerSlotExpr("CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ManaConversionMod') AS DOUBLE)"),
        ["armorAttackModPerSlot"] = PerSlotExpr("CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorAttackMod') AS DOUBLE)"),
        ["armorShieldModPerSlot"] = PerSlotExpr("CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorShieldMod') AS DOUBLE)"),
        ["armorDualWieldModPerSlot"] = PerSlotExpr("CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorDualWieldMod') AS DOUBLE)"),
        ["armorThieveryModPerSlot"] = PerSlotExpr("CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorThieveryMod') AS DOUBLE)"),
        ["armorRunModPerSlot"] = PerSlotExpr("CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorRunMod') AS DOUBLE)"),
        ["armorStaminaRegenModPerSlot"] = PerSlotExpr("CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorStaminaRegenMod') AS DOUBLE)"),
        ["armorPhysicalDefModPerSlot"] = PerSlotExpr("CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorPhysicalDefMod') AS DOUBLE)"),
        ["armorMagicDefModPerSlot"] = PerSlotExpr("CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorMagicDefMod') AS DOUBLE)"),
        ["armorTwohandedCombatModPerSlot"] = PerSlotExpr("CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorTwohandedCombatMod') AS DOUBLE)"),
        ["armorHealthRegenModPerSlot"] = PerSlotExpr("CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorHealthRegenMod') AS DOUBLE)"),
        ["armorPerceptionModPerSlot"] = PerSlotExpr("CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorPerceptionMod') AS DOUBLE)"),
        ["armorDeceptionModPerSlot"] = PerSlotExpr("CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorDeceptionMod') AS DOUBLE)"),
        ["armorHealthModPerSlot"] = PerSlotExpr("CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorHealthMod') AS DOUBLE)"),
        ["armorManaModPerSlot"] = PerSlotExpr("CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorManaMod') AS DOUBLE)"),
        ["armorStaminaModPerSlot"] = PerSlotExpr("CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorStaminaMod') AS DOUBLE)"),
    };

    // spellId -> MagicSchool (ACE.Entity.Enum.MagicSchool numeric value), extracted once from the
    // client's portal.dat SpellTable via a throwaway ACE.DatLoader console tool — there's no
    // "school" column in ace_world.spell (the closest thing, dispel_School, is the school a Dispel
    // spell purges from a target, not the spell's own casting school), so this is the only source.
    private static readonly Dictionary<int, int> SpellSchoolById = LoadSpellSchools();

    private static Dictionary<int, int> LoadSpellSchools()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Data", "SpellSchools.json");
        var json = File.ReadAllText(path);
        return System.Text.Json.JsonSerializer.Deserialize<Dictionary<int, int>>(json) ?? new();
    }

    // ACE.Entity.Enum.MagicSchool values, keyed by the name used in the Market UI's School dropdown.
    private static readonly Dictionary<string, int> MagicSchoolNameToValue = new()
    {
        ["WarMagic"] = 1,
        ["LifeMagic"] = 2,
        ["PortalMagic"] = 3,
        ["CreatureEnchantment"] = 4,
        ["VoidMagic"] = 5,
    };

    internal static (MarketListing Listing, List<int> SpellIds) Describe(IItemFacts facts)
    {
        long? iconDid = facts.Long("IconDidRaw");
        long? iconOverlayDid = facts.Long("IconOverlayDidRaw");
        var itemType = facts.UInt("ItemTypeRaw");
        var weaponType = facts.Int("WeaponTypeRaw");
        var combatStyle = facts.UInt("CombatStyleRaw");
        var weaponSkill = facts.Int("WeaponSkillRaw");
        var armorWeightClass = facts.Int("ArmorWeightClassRaw");
        var sigilTrinketType = facts.Int("SigilTrinketTypeRaw");
        var sigilTrinketColor = facts.Int("SigilTrinketColorRaw");
        var validLocations = facts.UInt("ValidLocationsRaw");
        var clothingPriority = facts.UInt("ClothingPriorityRaw");
        var imbuedEffect = facts.UInt("ImbuedEffectRaw");
        var baseDamage = facts.Int("DamageRaw");
        var damageVariance = facts.Double("DamageVarianceRaw");
        var damageMod = facts.Double("DamageModRaw");
        var damageType = facts.Int("DamageTypeRaw");
        var weaponTime = facts.Int("WeaponTimeRaw");
        var attackType = facts.UInt("AttackTypeRaw");
        var wieldRequirements = ComputeWieldRequirements(new (int?, int?, int?)[]
        {
            (facts.Int("WieldRequirementsRaw"), facts.Int("WieldSkillTypeRaw"), facts.Int("WieldDifficultyRaw")),
            (facts.Int("WieldRequirements2Raw"), facts.Int("WieldSkillType2Raw"), facts.Int("WieldDifficulty2Raw")),
            (facts.Int("WieldRequirements3Raw"), facts.Int("WieldSkillType3Raw"), facts.Int("WieldDifficulty3Raw")),
            (facts.Int("WieldRequirements4Raw"), facts.Int("WieldSkillType4Raw"), facts.Int("WieldDifficulty4Raw")),
        });
        var elementalDamageMod = facts.Double("ElementalDamageModRaw");
        var spellCount = facts.Int("SpellCountRaw");
        var itemName = facts.Text("ItemName");
        var itemTier = facts.Int("ItemTier");
        var weenieType = facts.Int("WeenieTypeRaw");
        var materialType = facts.Int("MaterialTypeRaw");
        var itemWorkmanship = facts.Int("ItemWorkmanshipRaw");
        var numItemsInMaterial = facts.Int("NumItemsInMaterialRaw");
        var structure = facts.Int("StructureRaw");
        var armorSlots = facts.Int("ArmorSlotsRaw");
        var itemTotalXp = facts.Long("ItemTotalXpRaw");
        var itemBaseXp = facts.Long("ItemBaseXpRaw");
        var itemXpStyle = facts.Int("ItemXpStyleRaw");
        var itemMaxLevel = facts.Int("ItemMaxLevelRaw");
        var allPropertiesIntRaw = facts.Text("AllPropertiesIntRaw");
        var sockets = facts.Int("SocketsRaw");
        var staminaCostReductionMod = facts.Double("StaminaCostReductionModRaw");
        var ignoreWard = facts.Double("IgnoreWardRaw");
        var ignoreArmor = facts.Double("IgnoreArmorRaw");
        var criticalMultiplier = facts.Double("CriticalMultiplierRaw");
        var criticalFrequency = facts.Double("CriticalFrequencyRaw");
        var resistanceModifier = facts.Double("ResistanceModifierRaw");
        var resistanceModifierType = facts.Int("ResistanceModifierTypeRaw");
        var noCompsRequiredForMagicSchool = facts.Int("NoCompsRequiredForMagicSchoolRaw");
        var trophyQuality = facts.Int("TrophyQualityRaw");
        var jewelQuality = facts.Int("JewelQualityRaw");
        var jewelMaterialType = facts.Int("JewelMaterialTypeRaw");
        var spellDid = facts.Int("SpellDidRaw");
        var spellBookRaw = facts.Text("SpellBookRaw");
        var spellIds = new List<int>();
        if (!string.IsNullOrEmpty(spellBookRaw))
        {
            try
            {
                using var spellDoc = System.Text.Json.JsonDocument.Parse(spellBookRaw);
                foreach (var prop in spellDoc.RootElement.EnumerateObject())
                {
                    if (int.TryParse(prop.Name, out var spellId))
                    {
                        spellIds.Add(spellId);
                    }
                }
            }
            catch (System.Text.Json.JsonException)
            {
                // Malformed/unexpected JSON shape — skip spells for this listing rather than fail the whole request.
            }
        }

        var attack = ComputeAttack(weaponType, combatStyle, weaponTime, itemTier, baseDamage, damageVariance, damageMod, itemType, attackType, weaponSkill, elementalDamageMod);

        var listing = new MarketListing
        {
            Id = (facts.Int("Id") ?? 0),
            ItemWeenieClassId = (facts.UInt("ItemWeenieClassId") ?? 0),
            ListedPrice = (facts.Int("ListedPrice") ?? 0),
            ItemTier = itemTier,
            MarketVendorTier = (facts.Int("MarketVendorTier") ?? 0),
            CreatedAtUtc = (facts.Date("CreatedAtUtc") ?? default),
            ExpiresAtUtc = (facts.Date("ExpiresAtUtc") ?? default),
            ItemName = itemName,
            ItemDesc = facts.Text("ItemDesc"),
            IconHex = iconDid.HasValue ? ((uint)iconDid.Value).ToString("X8") : null,
            IconOverlayHex = iconOverlayDid.HasValue ? ((uint)iconOverlayDid.Value).ToString("X8") : null,
            IconEffect = ComputeIconEffect(damageType, spellCount),
            IconUnderlay = ComputeIconUnderlay(itemType),
            LevelReq = wieldRequirements.LevelReq,
            ReqAttribute = wieldRequirements.ReqAttribute,
            ReqAttributeAmount = wieldRequirements.ReqAttributeAmount,
            OtherWieldRequirements = wieldRequirements.Other,
            BaseDamage = baseDamage,
            DamageVariance = damageVariance,
            AvgDamage = baseDamage.HasValue && damageVariance.HasValue ? baseDamage.Value * (1 - damageVariance.Value / 2) : null,
            DamageType = DamageTypeNames(damageType),
            Speed = weaponTime.HasValue ? Math.Clamp(2.0 - weaponTime.Value / 100.0, 1.0, 2.5) : null,
            WeaponTime = weaponTime,
            DamageMod = damageMod,
            ElementalDamageMod = elementalDamageMod,
            RestorationMod = facts.Double("RestorationModRaw"),
            Spellcraft = facts.Int("SpellcraftRaw"),
            Difficulty = facts.Int("DifficultyRaw"),
            Mana = facts.Int("ManaRaw"),
            ManaRate = facts.Double("ManaRateRaw") is { } manaRate && manaRate != 0 ? manaRate : null,
            ProcChance = facts.UInt("ProcSpellRaw") is > 0 && facts.Double("ProcSpellRateRaw") is { } procRate && procRate > 0 ? procRate : null,
            Workmanship = itemWorkmanship,
            NumTimesTinkered = facts.Int("NumTimesTinkeredRaw"),
            Sockets = sockets,
            ElementalDamageBonus = facts.Int("ElementalDamageBonusRaw"),
            WeaponOffense = facts.Double("WeaponOffenseRaw"),
            WeaponWarMagicMod = facts.Double("WeaponWarMagicModRaw"),
            WeaponLifeMagicMod = facts.Double("WeaponLifeMagicModRaw"),
            WeaponPhysicalDefense = facts.Double("WeaponPhysicalDefenseRaw"),
            WeaponMagicalDefense = facts.Double("WeaponMagicalDefenseRaw"),
            Dps = ComputeDps(attack),
            AttacksPerSecond = attack is { CycleTime: > 0 } ? 1 / attack.Value.CycleTime : null,
            ArmorSlots = armorSlots,
            ArmorLevel = facts.Int("ArmorLevelRaw"),
            WardLevel = facts.Int("WardLevelRaw"),
            WardPerSlot = facts.Int("WardLevelRaw").HasValue
                ? (double)facts.Int("WardLevelRaw")!.Value / Math.Max(armorSlots ?? 1, 1)
                : null,
            ArmorModVsSlash = facts.Double("ArmorModVsSlashRaw"),
            ArmorModVsPierce = facts.Double("ArmorModVsPierceRaw"),
            ArmorModVsBludgeon = facts.Double("ArmorModVsBludgeonRaw"),
            ArmorModVsCold = facts.Double("ArmorModVsColdRaw"),
            ArmorModVsFire = facts.Double("ArmorModVsFireRaw"),
            ArmorModVsAcid = facts.Double("ArmorModVsAcidRaw"),
            ArmorModVsElectric = facts.Double("ArmorModVsElectricRaw"),
            ArmorModVsNether = facts.Double("ArmorModVsNetherRaw"),
            ArmorWarMagicMod = facts.Double("ArmorWarMagicModRaw"),
            ArmorLifeMagicMod = facts.Double("ArmorLifeMagicModRaw"),
            ArmorManaRegenMod = facts.Double("ArmorManaRegenModRaw"),
            ArmorManaConversionMod = facts.Double("ArmorManaConversionModRaw"),
            ArmorAttackMod = facts.Double("ArmorAttackModRaw"),
            ArmorShieldMod = facts.Double("ArmorShieldModRaw"),
            ArmorDualWieldMod = facts.Double("ArmorDualWieldModRaw"),
            ArmorThieveryMod = facts.Double("ArmorThieveryModRaw"),
            ArmorRunMod = facts.Double("ArmorRunModRaw"),
            ArmorStaminaRegenMod = facts.Double("ArmorStaminaRegenModRaw"),
            ArmorPhysicalDefMod = facts.Double("ArmorPhysicalDefModRaw"),
            ArmorMagicDefMod = facts.Double("ArmorMagicDefModRaw"),
            ArmorTwohandedCombatMod = facts.Double("ArmorTwohandedCombatModRaw"),
            ArmorHealthRegenMod = facts.Double("ArmorHealthRegenModRaw"),
            ArmorPerceptionMod = facts.Double("ArmorPerceptionModRaw"),
            ArmorDeceptionMod = facts.Double("ArmorDeceptionModRaw"),
            ArmorHealthMod = facts.Double("ArmorHealthModRaw"),
            ArmorManaMod = facts.Double("ArmorManaModRaw"),
            ArmorStaminaMod = facts.Double("ArmorStaminaModRaw"),
            GearCritDamage = facts.Int("GearCritDamageRaw"),
            GearCritDamageResist = facts.Int("GearCritDamageResistRaw"),
            GearDamage = facts.Int("GearDamageRaw"),
            GearDamageResist = facts.Int("GearDamageResistRaw"),
            GearHealingBoost = facts.Int("GearHealingBoostRaw"),
            GearMaxHealth = facts.Int("GearMaxHealthRaw"),
            SigilTriggerChance = facts.Double("SigilTriggerChanceRaw"),
            SigilCooldown = facts.Double("SigilCooldownRaw"),
            SigilManaReserved = facts.Double("SigilManaReservedRaw"),
            SigilStaminaReserved = facts.Double("SigilStaminaReservedRaw"),
            SigilHealthReserved = facts.Double("SigilHealthReservedRaw"),
            SigilCurrentLevel = ComputeItemLevel(itemTotalXp, itemBaseXp, itemXpStyle, itemMaxLevel),
            SigilMaxLevel = itemMaxLevel,
            SalvageUnits = structure,
        };

        // Classification, derived from the raw properties above (see class doc comment for sourcing).
        if (weenieType == WeenieTypeSalvage)
        {
            listing.ItemCategory = "Salvage";
            if (materialType.HasValue && MaterialTypeNames.TryGetValue(materialType.Value, out var materialName))
            {
                listing.SalvageMaterial = materialName;
                listing.SalvageTargetTypes = GetSalvageTargetTypes(materialType.Value);
            }
            // Mirrors WorldObject.Workmanship (WorldObject_Properties.cs) — workmanship = ItemWorkmanship /
            // NumItemsInMaterial, with the same legacy-data recovery/clamp for out-of-range values.
            if (itemWorkmanship.HasValue)
            {
                var divisor = numItemsInMaterial ?? 1;
                var workmanship = divisor != 0 ? (double)itemWorkmanship.Value / divisor : 0;
                if (workmanship < 1.0 || workmanship > 10.0)
                {
                    var structureForRecovery = structure ?? 1;
                    workmanship = structureForRecovery != 0 ? itemWorkmanship.Value / 10000.0 / structureForRecovery : 0;
                    workmanship = Math.Clamp(workmanship, 1.0, 10.0);
                }
                listing.SalvageWorkmanship = Math.Round(workmanship, 2);
            }
        }
        else if (sigilTrinketType.HasValue && sigilTrinketType.Value >= 0 && sigilTrinketType.Value < SigilTrinketTypeNames.Length)
        {
            listing.ItemCategory = "SigilTrinket";
            listing.SigilTrinketType = SigilTrinketTypeNames[sigilTrinketType.Value];

            // The effect name isn't a distinct biota property — the loot factory bakes it directly
            // into the item's name as a " of <Effect>" suffix (LootGenerationFactory_SigilTrinket.cs:
            // sigilTrinket.Name += cfg.NameSuffix, e.g. "Sigil Compass of Might"), so recover it the
            // same way rather than re-deriving it from SigilTrinketEffectId/AllowedSpecializedSkills
            // (whose effect-enum mapping doesn't cover every skill combination the loot tables use).
            if (itemName != null)
            {
                var ofIndex = itemName.LastIndexOf(" of ", StringComparison.Ordinal);
                if (ofIndex >= 0)
                {
                    listing.SigilEffectName = itemName[(ofIndex + 4)..];
                }
            }

            // Sigil Trinkets override the normal icon underlay/uieffect with one of three colors
            // (SigilTrinketColor enum: Blue=0, Yellow=1, Red=2 — apps/server/WorldObjects/SigilTrinket.cs),
            // instead of the usual ItemType-based underlay or DamageType/spell-based uieffect.
            if (sigilTrinketColor is 0)
            {
                listing.IconUnderlay = "SigilBlue";
                listing.IconEffect = "SigilBlue";
            }
            else if (sigilTrinketColor is 1)
            {
                listing.IconUnderlay = "SigilYellow";
                listing.IconEffect = "SigilYellow";
            }
            else if (sigilTrinketColor is 2)
            {
                listing.IconUnderlay = "SigilRed";
                listing.IconEffect = "SigilRed";
            }

            // The IconOverlay read straight from PropertiesDID is a numeral badge showing the
            // trinket's max level, but only Blue trinkets consistently carry the intended custom
            // badge (client data for Yellow/Red points at unrelated stock icons instead) — so
            // override it uniformly here to the matching digit from the same custom badge set
            // (0600900{maxLevel}.png) regardless of color.
            if (itemMaxLevel is >= 0 and <= 9)
            {
                listing.IconOverlayHex = $"0600900{itemMaxLevel}";
            }
        }
        else if (itemType.HasValue && (itemType.Value & ItemTypeJewelry) != 0)
        {
            listing.ItemCategory = "Jewelry";
            if (validLocations.HasValue)
            {
                listing.JewelrySlot = (validLocations.Value & EquipMaskNeckWear) != 0 ? "Amulet"
                    : (validLocations.Value & EquipMaskWristWear) != 0 ? "Bracelet"
                    : (validLocations.Value & EquipMaskFingerWear) != 0 ? "Ring"
                    : null;
            }
        }
        else if (itemType.HasValue && (itemType.Value & ItemTypeArmor) != 0)
        {
            listing.ItemCategory = "Armor";
            listing.ArmorWeightClass = armorWeightClass switch
            {
                ArmorWeightCloth => "Cloth",
                ArmorWeightLight => "Light",
                ArmorWeightHeavy => "Heavy",
                _ => null,
            };
        }
        else if (itemType.HasValue && (itemType.Value & ItemTypeClothing) != 0)
        {
            listing.ItemCategory = "Clothing";
        }
        else if (itemType.HasValue && (itemType.Value & ItemTypeWeaponMask) != 0)
        {
            listing.ItemCategory = "Weapon";
            listing.WeaponClass = ClassifyWeapon(itemType.Value, weaponType, combatStyle, weaponSkill);
        }
        else if (weenieType == WeenieTypeFood || weenieType == WeenieTypeHealer)
        {
            // Healer (health/stamina/mana kits) and Food (both eaten dishes and drinkable
            // potions — the client uses one WorldObject class for both, split only by ItemType:
            // Food-flag for meals/drinks, Misc-flag for buff potions/draughts/elixirs) share no
            // wield requirements or gear stats, so they're grouped as one Consumable category.
            listing.ItemCategory = "Consumable";
            listing.ConsumableType = weenieType == WeenieTypeHealer ? "HealingKit"
                : itemType.HasValue && (itemType.Value & ItemTypeFood) != 0 ? "Food"
                : "Potion";
        }
        else
        {
            // Catch-all for anything not otherwise classified above.
            listing.ItemCategory = "Misc";
            if (weenieType == WeenieTypeScroll)
            {
                listing.MiscType = "Scroll";
                if (spellDid.HasValue && SpellSchoolById.TryGetValue(spellDid.Value, out var schoolValue))
                {
                    listing.SpellSchool = MagicSchoolNameToValue.FirstOrDefault(kv => kv.Value == schoolValue).Key;
                }
            }
            else if (weenieType == WeenieTypeGem || weenieType == WeenieTypeJewel)
            {
                // Loose gemstones (Gem) and carved jewels (Jewel, made from a socketed item via
                // the Jewelcarving skill — apps/server/WorldObjects/Jewel.cs) are the same "gem"
                // concept to a buyer, just different craft stages, so they share one subtype.
                listing.MiscType = "Gem";
                listing.IsCarvedJewel = weenieType == WeenieTypeJewel;
                if (weenieType == WeenieTypeJewel)
                {
                    listing.GemQuality = jewelQuality;
                    if (jewelMaterialType.HasValue && MaterialTypeNames.TryGetValue(jewelMaterialType.Value, out var jewelMatName))
                    {
                        listing.GemMaterialType = jewelMatName;
                    }
                }
                else
                {
                    listing.GemQuality = itemWorkmanship;
                    if (materialType.HasValue && MaterialTypeNames.TryGetValue(materialType.Value, out var gemMatName))
                    {
                        listing.GemMaterialType = gemMatName;
                    }
                }
            }
            else if (trophyQuality.HasValue)
            {
                listing.MiscType = "Trophy";
                listing.TrophyQuality = trophyQuality;
                // Stored Name is always "<QualityWord> <BaseName>" (baked into the per-quality
                // weenie template - see LootGenerationFactory.GetTrophyQualityName), so the base
                // name is recoverable as a suffix match against the known type list.
                if (itemName != null)
                {
                    listing.TrophyType = TrophyTypeNames.FirstOrDefault(t => itemName.EndsWith(t, StringComparison.Ordinal));
                }
            }
            else
            {
                listing.MiscType = "Other";
            }
        }

        if (clothingPriority.HasValue)
        {
            listing.Coverage = CoverageMasks
                .Where(kv => (clothingPriority.Value & kv.Value) != 0)
                .Select(kv => kv.Key)
                .ToList();
        }

        if (imbuedEffect.HasValue)
        {
            listing.ImbuedEffects = ImbuedEffectFlags
                .Where(f => (imbuedEffect.Value & f.Flag) != 0)
                .Select(f => f.Name)
                .ToList();
        }

        listing.Properties = ComputeProperties(
            allPropertiesIntRaw, listing.ImbuedEffects, staminaCostReductionMod, ignoreWard, ignoreArmor,
            criticalMultiplier, criticalFrequency, resistanceModifier, resistanceModifierType, noCompsRequiredForMagicSchool);
        listing.PropertyValues = ComputePropertyValues(staminaCostReductionMod, ignoreWard, ignoreArmor, criticalMultiplier, criticalFrequency);

        return (listing, spellIds);
    }

    private static string? ClassifyWeapon(uint itemType, int? weaponType, uint? combatStyle, int? weaponSkill)
    {
        if ((itemType & ItemTypeCaster) != 0)
        {
            return weaponSkill switch
            {
                SkillWarMagic => "WarMagic",
                SkillLifeMagic => "LifeMagic",
                _ => "Caster",
            };
        }

        if (combatStyle.HasValue && (combatStyle.Value & CombatStyleThrownWeapon) != 0)
        {
            return "Thrown";
        }
        if (combatStyle.HasValue && (combatStyle.Value & CombatStyleAtlatl) != 0)
        {
            return "Missile";
        }

        var byWeaponType = weaponType switch
        {
            WeaponTypeSword or WeaponTypeAxe or WeaponTypeMace or WeaponTypeSpear or WeaponTypeTwoHanded => "Martial",
            WeaponTypeBow or WeaponTypeCrossbow => "Missile",
            WeaponTypeDagger => "Dagger",
            WeaponTypeStaff => "Staff",
            WeaponTypeUnarmed => "Unarmed",
            _ => null,
        };
        if (byWeaponType != null)
        {
            return byWeaponType;
        }

        // Some weenies are missing WeaponType entirely — WeaponSkill still flavor-classifies the
        // weapon even though these per-type skills are "Retired" as the trained skill (see the
        // SkillSword/SkillAxe/... doc comment above for how these ordinals were verified).
        return weaponSkill switch
        {
            SkillSword or SkillAxe or SkillMace or SkillSpear or SkillTwoHandedCombat => "Martial",
            SkillBow or SkillCrossbow => "Missile",
            SkillThrownWeapon => "Thrown",
            SkillDagger => "Dagger",
            SkillStaff => "Staff",
            SkillUnarmedCombat => "Unarmed",
            _ => null,
        };
    }

    // Mirrors the client's own icon-coloring convention: weapons with an elemental damage type get
    // that element's color; anything else (weapon or not) with at least one spell gets a "magical"
    // glow; anything with neither gets no special treatment.
    // Mirrors DpsSqlExpr exactly (see its doc comment above for the formula/sourcing) — computed
    // here in C# for the response payload, and separately in SQL for server-side sort/filter
    // (which must run before LIMIT/OFFSET, so it can't just reuse this method's result).
    private static double? ComputeDps((double? Damage, double CycleTime)? attack)
    {
        return attack is { Damage: { } damage, CycleTime: var cycleTime } ? damage / cycleTime : null;
    }

    // One attack's damage (null when it can't be worked out) and how long each attack takes, in seconds, for Dps and
    // AttacksPerSecond: WeaponTime / 60 for a weapon (60 is one a second), the bolt's cast time for a caster. Null when
    // the item isn't a weapon this can model.
    private static (double? Damage, double CycleTime)? ComputeAttack(int? weaponType, uint? combatStyle, int? weaponTime, int? itemTier, int? baseDamage, double? damageVariance, double? damageMod, uint? itemType, uint? attackType, int? weaponSkill, double? elementalDamageMod)
    {
        var isCaster = itemType.HasValue && (itemType.Value & ItemTypeCaster) != 0;

        if (isCaster && weaponSkill is SkillWarMagic or SkillLifeMagic)
        {
            if (!itemTier.HasValue || itemTier.Value < 1 || itemTier.Value > BoltAvgDamageByTier.Length)
            {
                return null;
            }

            var i = itemTier.Value - 1;
            return (BoltAvgDamageByTier[i] * (elementalDamageMod ?? 1.0), BoltCastCycleTimeByTier[i]);
        }

        if (weaponTime is not > 0)
        {
            return null;
        }

        var cycleTime = weaponTime.Value / 60.0;
        var meleeDamage = baseDamage.HasValue && damageVariance.HasValue
            ? baseDamage.Value * (1 - damageVariance.Value / 2) * (damageMod ?? 1.0)
            : (double?)null;

        // the same order as DpsSqlExpr: what deals the damage, the weapon itself or a launcher's ammo
        if (combatStyle.HasValue && (combatStyle.Value & CombatStyleThrownWeapon) != 0)
        {
            return (meleeDamage, cycleTime);
        }

        if (weaponType == WeaponTypeBow)
        {
            return (AmmoDamage(AmmoDamByTierBow, itemTier, damageMod), cycleTime);
        }

        if (weaponType == WeaponTypeCrossbow)
        {
            return (AmmoDamage(AmmoDamByTierCrossbow, itemTier, damageMod), cycleTime);
        }

        if (combatStyle.HasValue && (combatStyle.Value & CombatStyleAtlatl) != 0)
        {
            return (AmmoDamage(AmmoDamByTierAtlatl, itemTier, damageMod), cycleTime);
        }

        if (weaponType is WeaponTypeDagger or WeaponTypeSpear or WeaponTypeUnarmed or WeaponTypeTwoHanded
            or WeaponTypeSword or WeaponTypeAxe or WeaponTypeMace or WeaponTypeStaff)
        {
            return (meleeDamage, cycleTime);
        }

        // WeaponType fallback: some weenies genuinely lack it, but the small, reliably populated AttackType flag set
        // (Punch/Thrust/Slash/Kick) is still present.
        return !isCaster && attackType is > 0 ? (meleeDamage, cycleTime) : null;
    }

    private static double? AmmoDamage(int[] ammoDamByTier, int? itemTier, double? damageMod)
    {
        if (!itemTier.HasValue || itemTier.Value < 1 || itemTier.Value > ammoDamByTier.Length)
        {
            return null;
        }

        return ammoDamByTier[itemTier.Value - 1] * (damageMod ?? 1.0);
    }

    private static string? ComputeIconEffect(int? damageType, int? spellCount)
    {
        if (damageType.HasValue)
        {
            if ((damageType.Value & DamageTypeFire) != 0)
            {
                return "Fire";
            }
            if ((damageType.Value & DamageTypeCold) != 0)
            {
                return "Cold";
            }
            if ((damageType.Value & DamageTypeAcid) != 0)
            {
                return "Acid";
            }
            if ((damageType.Value & DamageTypeElectric) != 0)
            {
                return "Electric";
            }
        }

        return spellCount.GetValueOrDefault() > 0 ? "Magical" : "Undef";
    }

    // Mirrors ExperienceSystem.ItemTotalXPToLevel (apps/server/Entity/ExperienceSystem.cs) — see
    // ItemLevelSqlExpr's doc comment above for the derivation of the ScalesWithLevel closed form.
    private static int? ComputeItemLevel(long? totalXp, long? baseXp, int? xpStyle, int? maxLevel)
    {
        if (!totalXp.HasValue || !baseXp.HasValue || baseXp.Value <= 0 || !maxLevel.HasValue)
        {
            return null;
        }

        var level = xpStyle == 2
            ? (int)Math.Floor(Math.Log2((double)totalXp.Value / baseXp.Value + 1))
            : (int)Math.Floor((double)totalXp.Value / baseXp.Value);

        return Math.Min(level, maxLevel.Value);
    }

}
