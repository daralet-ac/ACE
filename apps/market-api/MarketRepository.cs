using MySqlConnector;

namespace ACE.MarketApi;

public class MarketSearchRequest
{
    public string? Keyword { get; set; }
    public string? SpellName { get; set; }
    public int? MinPrice { get; set; }
    public int? MaxPrice { get; set; }
    public int? MinTier { get; set; }
    public int? MaxTier { get; set; }
    public string? ItemCategory { get; set; }
    public string? WeaponClass { get; set; }
    public string? ArmorWeightClass { get; set; }
    public List<string>? Coverage { get; set; }
    public string? SigilTrinketType { get; set; }
    public string? SigilEffect { get; set; }
    public string? JewelrySlot { get; set; }
    public string? SalvageTargetType { get; set; }
    public string? SalvageMaterial { get; set; }
    public string? ConsumableType { get; set; }
    public string? MiscType { get; set; }
    public string? TrophyType { get; set; }
    public int? MinTrophyQuality { get; set; }
    public int? MaxTrophyQuality { get; set; }
    public string? GemMaterial { get; set; }
    public bool? CarvedJewelOnly { get; set; }
    public int? MinGemQuality { get; set; }
    public int? MaxGemQuality { get; set; }
    public string? ScrollSchool { get; set; }
    public string? ScrollSpellName { get; set; }
    public double? MinWorkmanship { get; set; }
    public double? MaxWorkmanship { get; set; }
    public int? MinUnits { get; set; }
    public int? MaxUnits { get; set; }
    public double? MinPricePerUnit { get; set; }
    public double? MaxPricePerUnit { get; set; }
    public int? SigilMinLevel { get; set; }
    public int? SigilMaxLevel { get; set; }
    public double? MinProcChance { get; set; }
    public double? MaxProcChance { get; set; }
    public double? MinCooldown { get; set; }
    public double? MaxCooldown { get; set; }
    public double? MinManaReserved { get; set; }
    public double? MaxManaReserved { get; set; }
    public double? MinStaminaReserved { get; set; }
    public double? MaxStaminaReserved { get; set; }
    public double? MinHealthReserved { get; set; }
    public double? MaxHealthReserved { get; set; }
    public double? MinDps { get; set; }
    public double? MaxDps { get; set; }
    public int? MinBaseDamage { get; set; }
    public int? MaxBaseDamage { get; set; }
    public double? MinDamageVariance { get; set; }
    public double? MaxDamageVariance { get; set; }
    public double? MinSpeed { get; set; }
    public double? MaxSpeed { get; set; }
    public double? MinAttackMod { get; set; }
    public double? MaxAttackMod { get; set; }
    public double? MinPhysDefMod { get; set; }
    public double? MaxPhysDefMod { get; set; }
    public double? MinMagicDefMod { get; set; }
    public double? MaxMagicDefMod { get; set; }
    public int? MinSpellcraft { get; set; }
    public int? MaxSpellcraft { get; set; }
    public int? MinDifficulty { get; set; }
    public int? MaxDifficulty { get; set; }
    public double? MinWarMagicMod { get; set; }
    public double? MaxWarMagicMod { get; set; }
    public double? MinLifeMagicMod { get; set; }
    public double? MaxLifeMagicMod { get; set; }
    public int? MinArmorLevel { get; set; }
    public int? MaxArmorLevel { get; set; }
    public int? MinWardLevel { get; set; }
    public int? MaxWardLevel { get; set; }
    public double? MinArmorModVsSlash { get; set; }
    public double? MaxArmorModVsSlash { get; set; }
    public double? MinArmorModVsPierce { get; set; }
    public double? MaxArmorModVsPierce { get; set; }
    public double? MinArmorModVsBludgeon { get; set; }
    public double? MaxArmorModVsBludgeon { get; set; }
    public double? MinArmorModVsCold { get; set; }
    public double? MaxArmorModVsCold { get; set; }
    public double? MinArmorModVsFire { get; set; }
    public double? MaxArmorModVsFire { get; set; }
    public double? MinArmorModVsAcid { get; set; }
    public double? MaxArmorModVsAcid { get; set; }
    public double? MinArmorModVsElectric { get; set; }
    public double? MaxArmorModVsElectric { get; set; }
    public double? MinArmorWarMagicMod { get; set; }
    public double? MaxArmorWarMagicMod { get; set; }
    public double? MinArmorLifeMagicMod { get; set; }
    public double? MaxArmorLifeMagicMod { get; set; }
    public double? MinArmorManaRegenMod { get; set; }
    public double? MaxArmorManaRegenMod { get; set; }
    public double? MinArmorManaConversionMod { get; set; }
    public double? MaxArmorManaConversionMod { get; set; }
    public double? MinArmorAttackMod { get; set; }
    public double? MaxArmorAttackMod { get; set; }
    public double? MinArmorShieldMod { get; set; }
    public double? MaxArmorShieldMod { get; set; }
    public double? MinArmorDualWieldMod { get; set; }
    public double? MaxArmorDualWieldMod { get; set; }
    public double? MinArmorThieveryMod { get; set; }
    public double? MaxArmorThieveryMod { get; set; }
    public double? MinArmorRunMod { get; set; }
    public double? MaxArmorRunMod { get; set; }
    public double? MinArmorStaminaRegenMod { get; set; }
    public double? MaxArmorStaminaRegenMod { get; set; }
    public double? MinArmorPhysicalDefMod { get; set; }
    public double? MaxArmorPhysicalDefMod { get; set; }
    public double? MinArmorMagicDefMod { get; set; }
    public double? MaxArmorMagicDefMod { get; set; }
    public double? MinArmorTwohandedCombatMod { get; set; }
    public double? MaxArmorTwohandedCombatMod { get; set; }
    public double? MinArmorHealthRegenMod { get; set; }
    public double? MaxArmorHealthRegenMod { get; set; }
    public double? MinArmorPerceptionMod { get; set; }
    public double? MaxArmorPerceptionMod { get; set; }
    public double? MinArmorDeceptionMod { get; set; }
    public double? MaxArmorDeceptionMod { get; set; }
    public double? MinArmorHealthMod { get; set; }
    public double? MaxArmorHealthMod { get; set; }
    public double? MinArmorManaMod { get; set; }
    public double? MaxArmorManaMod { get; set; }
    public double? MinArmorStaminaMod { get; set; }
    public double? MaxArmorStaminaMod { get; set; }
    public int? MinGearCritDamage { get; set; }
    public int? MaxGearCritDamage { get; set; }
    public int? MinGearCritDamageResist { get; set; }
    public int? MaxGearCritDamageResist { get; set; }
    public int? MinGearDamage { get; set; }
    public int? MaxGearDamage { get; set; }
    public int? MinGearDamageResist { get; set; }
    public int? MaxGearDamageResist { get; set; }
    public int? MinGearHealingBoost { get; set; }
    public int? MaxGearHealingBoost { get; set; }
    public int? MinGearMaxHealth { get; set; }
    public int? MaxGearMaxHealth { get; set; }
    public string? SortBy { get; set; }
    public string? SortDir { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}

public class MarketSearchResult
{
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public List<MarketListing> Listings { get; set; } = new();
}

/// <summary>
/// Read-only queries against player_market_listings. Every filter is bound as a parameter —
/// never string-concatenated into the SQL — since this connects with a live, if narrowly
/// scoped (SELECT-only), database credential.
///
/// Item classification and stats are read out of ItemSnapshotJson (a full per-instance
/// property dump captured at listing time — see MarketListingSnapshot in ACE.Server) via
/// JSON_EXTRACT against ACE.Entity.Enum property names. The numeric enum/flag values below
/// were all verified directly against the enum source in daralet-ACE (WeaponType.cs,
/// CombatStyle.cs, ItemType.cs, ArmorWeightClass.cs, EquipMask.cs, CoverageMask.cs,
/// ImbuedEffectType.cs, Skill.cs, PropertyInt.cs/PropertyFloat.cs) — not guessed — since a
/// wrong constant here fails silently (JSON_EXTRACT just returns no rows, no error).
/// </summary>
public class MarketRepository
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

    // Melee/thrown/missile animation timing constants — client/DAT-driven, not a stored item
    // property, so these are hardcoded from the project's verified Weapon Damage/Power Tables
    // spreadsheet (the same source as weapontierdata.js on the site) plus a Base/Moddable Anim
    // Length (melee/thrown) or Anim/Reload Anim Length (missile) split. Every subtype/size within
    // a WeaponType shares one constant except Dagger, where Large (Dirk/Jitte) and Small
    // (Dagger/Jambiya/Khanjar) differ — that one case is keyed by item name.
    private const double AnimLenSwordAxeMaceStaff = 1.33;
    private const double AnimLenSpear = 1.38;
    private const double AnimLenDaggerLarge = 1.33;
    private const double AnimLenDaggerSmall = 1.31;
    private const double AnimLenUnarmed = 1.1;
    private const double AnimLenTwoHanded = 0.9375;
    private const double AnimLenThrownFixed = 1.352; // Base 2.33 - Moddable 0.978
    private const double AnimLenThrownModdable = 0.978;
    private const double AnimLenBow = 1.057;
    private const double ReloadLenBow = 0.32;
    private const double AnimLenCrossbow = 1.59;
    private const double ReloadLenCrossbow = 0.26;
    private const double AnimLenAtlatl = 1.85;
    private const double ReloadLenAtlatl = 0.73;

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

    // DPS — a "weapon-only" reference figure (0 Quickness, so only the listing's own weapon Speed
    // affects animation speed), since a market listing has no wielder to pull real Quickness from.
    // Mirrors Creature_Combat.GetAnimSpeed() and MotionTable.GetAnimationLength() exactly, with the
    // quickness term fixed at 0:
    //   animSpeed = clamp(1.0 + (1 - weaponSpeed/100), 1.0, 2.5)
    //   cycleTime = fixedAnimPortion + moddableAnimPortion / animSpeed   (melee/thrown)
    //   cycleTime = animLength + reloadAnimLength / animSpeed           (missile launchers)
    //   dps = avgDamage * damageMod * (1 / cycleTime)                  (melee/thrown)
    //   dps = ammoDam(itemTier) * damageMod * (1 / cycleTime)          (missile launchers)
    //   dps = boltAvgDamage(itemTier) * elementalDamageMod / boltCycleTime(itemTier)  (casters)
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

    private static readonly string DpsSqlExpr = $"""
        (
          CASE
            WHEN (CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.ItemType') AS UNSIGNED) & {ItemTypeCaster}) != 0
              AND CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.WeaponSkill') AS SIGNED) IN ({SkillWarMagic},{SkillLifeMagic})
              THEN {WarMagicBoltDpsSqlCase()} * COALESCE(CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ElementalDamageMod') AS DOUBLE), 1.0)
            WHEN (CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.DefaultCombatStyle') AS UNSIGNED) & {CombatStyleThrownWeapon}) != 0
              THEN {MeleeDamageSqlExpr} / ({AnimLenThrownFixed} + {AnimLenThrownModdable} / {AnimSpeedSqlExpr})
            WHEN CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.WeaponType') AS SIGNED) = {WeaponTypeBow}
              THEN {MissileDamageSqlExpr(AmmoDamByTierBow)} / ({AnimLenBow} + {ReloadLenBow} / {AnimSpeedSqlExpr})
            WHEN CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.WeaponType') AS SIGNED) = {WeaponTypeCrossbow}
              THEN {MissileDamageSqlExpr(AmmoDamByTierCrossbow)} / ({AnimLenCrossbow} + {ReloadLenCrossbow} / {AnimSpeedSqlExpr})
            WHEN (CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.DefaultCombatStyle') AS UNSIGNED) & {CombatStyleAtlatl}) != 0
              THEN {MissileDamageSqlExpr(AmmoDamByTierAtlatl)} / ({AnimLenAtlatl} + {ReloadLenAtlatl} / {AnimSpeedSqlExpr})
            WHEN CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.WeaponType') AS SIGNED) = {WeaponTypeDagger}
              THEN {MeleeDamageSqlExpr} / ((CASE WHEN JSON_UNQUOTE(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesString.Name')) IN ('Dirk','Jitte') THEN {AnimLenDaggerLarge} ELSE {AnimLenDaggerSmall} END) / {AnimSpeedSqlExpr})
            WHEN CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.WeaponType') AS SIGNED) = {WeaponTypeSpear}
              THEN {MeleeDamageSqlExpr} / ({AnimLenSpear} / {AnimSpeedSqlExpr})
            WHEN CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.WeaponType') AS SIGNED) = {WeaponTypeUnarmed}
              THEN {MeleeDamageSqlExpr} / ({AnimLenUnarmed} / {AnimSpeedSqlExpr})
            WHEN CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.WeaponType') AS SIGNED) = {WeaponTypeTwoHanded}
              THEN {MeleeDamageSqlExpr} / ({AnimLenTwoHanded} / {AnimSpeedSqlExpr})
            WHEN CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.WeaponType') AS SIGNED) IN ({WeaponTypeSword},{WeaponTypeAxe},{WeaponTypeMace},{WeaponTypeStaff})
              THEN {MeleeDamageSqlExpr} / ({AnimLenSwordAxeMaceStaff} / {AnimSpeedSqlExpr})
            WHEN (CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.ItemType') AS UNSIGNED) & {ItemTypeCaster}) = 0
              AND (CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.AttackType') AS UNSIGNED) & {AttackTypeUnarmedMask}) != 0
              THEN {MeleeDamageSqlExpr} / ({AnimLenUnarmed} / {AnimSpeedSqlExpr})
            WHEN (CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.ItemType') AS UNSIGNED) & {ItemTypeCaster}) = 0
              AND CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.AttackType') AS UNSIGNED) > 0
              THEN {MeleeDamageSqlExpr} / ({AnimLenSwordAxeMaceStaff} / {AnimSpeedSqlExpr})
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

    private readonly DatabaseOptions _dbOptions;

    public MarketRepository(DatabaseOptions dbOptions)
    {
        _dbOptions = dbOptions;
    }

    public async Task<MarketSearchResult> SearchListingsAsync(MarketSearchRequest request, CancellationToken ct)
    {
        var page = Math.Max(request.Page, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var offset = (page - 1) * pageSize;

        var extraClauses = new List<string>();
        var extraParams = new List<(string Name, object? Value)>();
        BuildCategoryFilter(request, extraClauses, extraParams);

        if (!string.IsNullOrWhiteSpace(request.SpellName))
        {
            var spellIds = await GetSpellIdsByNameAsync(request.SpellName.Trim(), ct);
            extraClauses.Add(
                spellIds.Count > 0
                    ? "(" + string.Join(" OR ", spellIds.Select(id => $"JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesSpellBook.\"{id}\"') IS NOT NULL")) + ")"
                    : "1=0"
            );
        }

        // Scrolls carry a single direct-cast spell (PropertiesDID.Spell), not a PropertiesSpellBook
        // dictionary like gear does, so this needs its own IN(...) match rather than the block above.
        if (!string.IsNullOrWhiteSpace(request.ScrollSpellName))
        {
            var scrollSpellIds = await GetSpellIdsByNameAsync(request.ScrollSpellName.Trim(), ct);
            extraClauses.Add(
                scrollSpellIds.Count > 0
                    ? $"CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesDID.Spell') AS UNSIGNED) IN ({string.Join(",", scrollSpellIds)})"
                    : "1=0"
            );
        }

        var orderByExpr = "CreatedAtUtc";
        var orderByDir = "DESC";
        if (request.SortBy != null && SortExpressions.TryGetValue(request.SortBy, out var sortExpr))
        {
            orderByExpr = sortExpr;
            orderByDir = string.Equals(request.SortDir, "desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";
        }

        var whereClause = $"""
            WHERE IsSold = 0 AND IsCancelled = 0 AND ExpiresAtUtc > UTC_TIMESTAMP()
              AND (@keyword IS NULL OR JSON_UNQUOTE(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesString.Name')) LIKE @keywordLike)
              AND (@minPrice IS NULL OR ListedPrice >= @minPrice)
              AND (@maxPrice IS NULL OR ListedPrice <= @maxPrice)
              AND (@minTier IS NULL OR ItemTier >= @minTier)
              AND (@maxTier IS NULL OR ItemTier <= @maxTier)
              {string.Concat(extraClauses.Select(c => $"AND ({c})\n              "))}
            """;

        await using var connection = new MySqlConnection(_dbOptions.ShardConnectionString);
        await connection.OpenAsync(ct);

        var result = new MarketSearchResult { Page = page, PageSize = pageSize };

        await using (var countCmd = connection.CreateCommand())
        {
            countCmd.CommandText = $"SELECT COUNT(*) FROM player_market_listings {whereClause}";
            AddFilterParams(countCmd, request, extraParams);
            result.TotalCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync(ct));
        }

        await using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = $"""
                SELECT
                    Id, ItemWeenieClassId, ListedPrice, ItemTier,
                    MarketVendorTier, CreatedAtUtc, ExpiresAtUtc,
                    JSON_UNQUOTE(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesString.Name')) AS ItemName,
                    JSON_UNQUOTE(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesString.LongDesc')) AS ItemDesc,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesDID.Icon') AS IconDidRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesDID.IconOverlay') AS IconOverlayDidRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.ItemType') AS ItemTypeRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.WeaponType') AS WeaponTypeRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.DefaultCombatStyle') AS CombatStyleRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.WeaponSkill') AS WeaponSkillRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.ArmorWeightClass') AS ArmorWeightClassRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.SigilTrinketType') AS SigilTrinketTypeRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.SigilTrinketColor') AS SigilTrinketColorRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.ValidLocations') AS ValidLocationsRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.ClothingPriority') AS ClothingPriorityRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.ImbuedEffect') AS ImbuedEffectRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.Damage') AS DamageRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.DamageVariance') AS DamageVarianceRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.DamageMod') AS DamageModRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ElementalDamageMod') AS ElementalDamageModRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.WeaponRestorationSpellsMod') AS RestorationModRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.ItemSpellcraft') AS SpellcraftRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.ItemDifficulty') AS DifficultyRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.ItemMaxMana') AS ManaRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.ElementalDamageBonus') AS ElementalDamageBonusRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.WeaponOffense') AS WeaponOffenseRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.WeaponWarMagicMod') AS WeaponWarMagicModRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.WeaponLifeMagicMod') AS WeaponLifeMagicModRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.WeaponPhysicalDefense') AS WeaponPhysicalDefenseRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.WeaponMagicalDefense') AS WeaponMagicalDefenseRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.ArmorLevel') AS ArmorLevelRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.WardLevel') AS WardLevelRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.ArmorSlots') AS ArmorSlotsRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorModVsSlash') AS ArmorModVsSlashRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorModVsPierce') AS ArmorModVsPierceRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorModVsBludgeon') AS ArmorModVsBludgeonRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorModVsCold') AS ArmorModVsColdRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorModVsFire') AS ArmorModVsFireRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorModVsAcid') AS ArmorModVsAcidRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorModVsElectric') AS ArmorModVsElectricRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorModVsNether') AS ArmorModVsNetherRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorWarMagicMod') AS ArmorWarMagicModRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorLifeMagicMod') AS ArmorLifeMagicModRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorManaRegenMod') AS ArmorManaRegenModRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ManaConversionMod') AS ArmorManaConversionModRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorAttackMod') AS ArmorAttackModRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorShieldMod') AS ArmorShieldModRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorDualWieldMod') AS ArmorDualWieldModRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorThieveryMod') AS ArmorThieveryModRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorRunMod') AS ArmorRunModRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorStaminaRegenMod') AS ArmorStaminaRegenModRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorPhysicalDefMod') AS ArmorPhysicalDefModRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorMagicDefMod') AS ArmorMagicDefModRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorTwohandedCombatMod') AS ArmorTwohandedCombatModRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorHealthRegenMod') AS ArmorHealthRegenModRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorPerceptionMod') AS ArmorPerceptionModRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorDeceptionMod') AS ArmorDeceptionModRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorHealthMod') AS ArmorHealthModRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorManaMod') AS ArmorManaModRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorStaminaMod') AS ArmorStaminaModRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.GearCritDamage') AS GearCritDamageRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.GearCritDamageResist') AS GearCritDamageResistRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.GearDamage') AS GearDamageRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.GearDamageResist') AS GearDamageResistRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.GearHealingBoost') AS GearHealingBoostRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.GearMaxHealth') AS GearMaxHealthRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.SigilTrinketTriggerChance') AS SigilTriggerChanceRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.SigilTrinketCooldown') AS SigilCooldownRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.SigilTrinketManaReserved') AS SigilManaReservedRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.SigilTrinketStaminaReserved') AS SigilStaminaReservedRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.SigilTrinketHealthReserved') AS SigilHealthReservedRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.WeenieType') AS WeenieTypeRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.MaterialType') AS MaterialTypeRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.ItemWorkmanship') AS ItemWorkmanshipRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.NumItemsInMaterial') AS NumItemsInMaterialRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.NumTimesTinkered') AS NumTimesTinkeredRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.Structure') AS StructureRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt64.ItemTotalXp') AS ItemTotalXpRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt64.ItemBaseXp') AS ItemBaseXpRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.ItemXpStyle') AS ItemXpStyleRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.ItemMaxLevel') AS ItemMaxLevelRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.DamageType') AS DamageTypeRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.WeaponTime') AS WeaponTimeRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.AttackType') AS AttackTypeRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.WieldRequirements') AS WieldRequirementsRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.WieldSkillType') AS WieldSkillTypeRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.WieldDifficulty') AS WieldDifficultyRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.WieldRequirements2') AS WieldRequirements2Raw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.WieldSkillType2') AS WieldSkillType2Raw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.WieldDifficulty2') AS WieldDifficulty2Raw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.WieldRequirements3') AS WieldRequirements3Raw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.WieldSkillType3') AS WieldSkillType3Raw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.WieldDifficulty3') AS WieldDifficulty3Raw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.WieldRequirements4') AS WieldRequirements4Raw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.WieldSkillType4') AS WieldSkillType4Raw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.WieldDifficulty4') AS WieldDifficulty4Raw,
                    JSON_LENGTH(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesSpellBook')) AS SpellCountRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesSpellBook') AS SpellBookRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt') AS AllPropertiesIntRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.JewelSockets') AS SocketsRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.StaminaCostReductionMod') AS StaminaCostReductionModRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.IgnoreWard') AS IgnoreWardRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.IgnoreArmor') AS IgnoreArmorRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.CriticalMultiplier') AS CriticalMultiplierRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.CriticalFrequency') AS CriticalFrequencyRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ResistanceModifier') AS ResistanceModifierRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.ResistanceModifierType') AS ResistanceModifierTypeRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.NoCompsRequiredForMagicSchool') AS NoCompsRequiredForMagicSchoolRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.TrophyQuality') AS TrophyQualityRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.JewelQuality') AS JewelQualityRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.JewelMaterialType') AS JewelMaterialTypeRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesDID.Spell') AS SpellDidRaw
                FROM player_market_listings
                {whereClause}
                ORDER BY {orderByExpr} {orderByDir}
                LIMIT @limit OFFSET @offset
                """;
            AddFilterParams(cmd, request, extraParams);
            cmd.Parameters.AddWithValue("@limit", pageSize);
            cmd.Parameters.AddWithValue("@offset", offset);

            var pending = new List<(MarketListing Listing, List<int> SpellIds)>();
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                pending.Add(ReadListing(reader));
            }
            result.Listings.AddRange(pending.Select(p => p.Listing));

            var allSpellIds = pending.SelectMany(p => p.SpellIds).Distinct().ToList();
            if (allSpellIds.Count > 0)
            {
                var spellNames = await GetSpellNamesAsync(allSpellIds, ct);
                foreach (var (listing, spellIds) in pending)
                {
                    listing.Spells = spellIds.Select(id => spellNames.TryGetValue(id, out var n) ? n : $"Spell {id}").ToList();
                }
            }
        }

        return result;
    }

    // Spell names aren't in ItemSnapshotJson (only spell IDs are) — resolved via one batched
    // lookup per page against ace_world.spell, rather than a per-listing query.
    private async Task<Dictionary<int, string>> GetSpellNamesAsync(List<int> spellIds, CancellationToken ct)
    {
        var names = new Dictionary<int, string>();
        await using var connection = new MySqlConnection(_dbOptions.WorldConnectionString);
        await connection.OpenAsync(ct);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"SELECT id, name FROM spell WHERE id IN ({string.Join(",", spellIds)})";
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            names[reader.GetInt32(0)] = reader.GetString(1);
        }
        return names;
    }

    // Resolves a free-text spell name query (e.g. "Strength Self") to matching spell ids in
    // ace_world.spell, for the "Has Spell" listing filter — a substring match like Keyword's, so
    // "bow" finds every level/rank of every spell with "bow" in its name (Bow Aptitude, Bow Mastery,
    // Crossbow Mastery, ...). The candidate set is bounded (not unbounded) purely to protect against
    // a pathological 1-2 letter query matching a huge fraction of the ~4.8k-row spell table and
    // bloating the OR'd JSON_EXTRACT clause built in SearchListingsAsync — 500 comfortably covers
    // every realistic search term (e.g. "bow" only matches 64, "self" matches 865) without silently
    // dropping real matches the way the previous LIMIT 25 did.
    private async Task<List<int>> GetSpellIdsByNameAsync(string nameQuery, CancellationToken ct)
    {
        var ids = new List<int>();
        await using var connection = new MySqlConnection(_dbOptions.WorldConnectionString);
        await connection.OpenAsync(ct);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT id FROM spell WHERE name LIKE @nameLike LIMIT 500";
        cmd.Parameters.AddWithValue("@nameLike", "%" + nameQuery + "%");
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            ids.Add(reader.GetInt32(0));
        }
        return ids;
    }

    private static void BuildCategoryFilter(MarketSearchRequest request, List<string> clauses, List<(string Name, object? Value)> parameters)
    {
        var itemTypeCol = "CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.ItemType') AS UNSIGNED)";

        switch (request.ItemCategory)
        {
            case "Weapon":
                clauses.Add($"({itemTypeCol} & {ItemTypeWeaponMask}) != 0");
                AddWeaponClassFilter(request.WeaponClass, clauses);
                AddMinMaxFilter(DpsSqlExpr, request.MinDps, request.MaxDps, "dps", clauses, parameters);
                AddMinMaxFilter(SortExpressions["baseDamage"], request.MinBaseDamage, request.MaxBaseDamage, "baseDamage", clauses, parameters);
                AddMinMaxFilter(SortExpressions["damageVariance"], request.MinDamageVariance, request.MaxDamageVariance, "damageVariance", clauses, parameters);
                AddMinMaxFilter(SortExpressions["weaponSpeed"], request.MinSpeed, request.MaxSpeed, "weaponSpeed", clauses, parameters);
                AddMinMaxFilter(SortExpressions["attackMod"], request.MinAttackMod, request.MaxAttackMod, "attackMod", clauses, parameters);
                AddMinMaxFilter(SortExpressions["physDefMod"], request.MinPhysDefMod, request.MaxPhysDefMod, "physDefMod", clauses, parameters);
                AddMinMaxFilter(SortExpressions["magicDefMod"], request.MinMagicDefMod, request.MaxMagicDefMod, "magicDefMod", clauses, parameters);
                AddMinMaxFilter(SortExpressions["spellcraft"], request.MinSpellcraft, request.MaxSpellcraft, "spellcraft", clauses, parameters);
                AddMinMaxFilter(SortExpressions["difficulty"], request.MinDifficulty, request.MaxDifficulty, "difficulty", clauses, parameters);
                AddMinMaxFilter(SortExpressions["warMagicMod"], request.MinWarMagicMod, request.MaxWarMagicMod, "warMagicMod", clauses, parameters);
                AddMinMaxFilter(SortExpressions["lifeMagicMod"], request.MinLifeMagicMod, request.MaxLifeMagicMod, "lifeMagicMod", clauses, parameters);
                break;
            case "Armor":
                clauses.Add($"({itemTypeCol} & {ItemTypeArmor}) != 0");
                if (request.ArmorWeightClass is { Length: > 0 })
                {
                    var value = request.ArmorWeightClass switch
                    {
                        "Cloth" => ArmorWeightCloth,
                        "Light" => ArmorWeightLight,
                        "Heavy" => ArmorWeightHeavy,
                        _ => (int?)null,
                    };
                    if (value.HasValue)
                    {
                        clauses.Add("JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.ArmorWeightClass') = @armorWeightClass");
                        parameters.Add(("@armorWeightClass", value.Value));
                    }
                }
                if (request.Coverage is { Count: > 0 })
                {
                    uint mask = 0;
                    foreach (var part in request.Coverage)
                    {
                        if (CoverageMasks.TryGetValue(part, out var partMask))
                        {
                            mask |= partMask;
                        }
                    }
                    if (mask != 0)
                    {
                        clauses.Add($"(CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.ClothingPriority') AS UNSIGNED) & {mask}) != 0");
                    }
                }
                AddMinMaxFilter(SortExpressions["armorLevel"], request.MinArmorLevel, request.MaxArmorLevel, "armorLevel", clauses, parameters);
                AddMinMaxFilter(SortExpressions["wardPerSlot"], request.MinWardLevel, request.MaxWardLevel, "wardLevel", clauses, parameters);
                AddMinMaxFilter(SortExpressions["armorModVsSlash"], request.MinArmorModVsSlash, request.MaxArmorModVsSlash, "armorModVsSlash", clauses, parameters);
                AddMinMaxFilter(SortExpressions["armorModVsPierce"], request.MinArmorModVsPierce, request.MaxArmorModVsPierce, "armorModVsPierce", clauses, parameters);
                AddMinMaxFilter(SortExpressions["armorModVsBludgeon"], request.MinArmorModVsBludgeon, request.MaxArmorModVsBludgeon, "armorModVsBludgeon", clauses, parameters);
                AddMinMaxFilter(SortExpressions["armorModVsCold"], request.MinArmorModVsCold, request.MaxArmorModVsCold, "armorModVsCold", clauses, parameters);
                AddMinMaxFilter(SortExpressions["armorModVsFire"], request.MinArmorModVsFire, request.MaxArmorModVsFire, "armorModVsFire", clauses, parameters);
                AddMinMaxFilter(SortExpressions["armorModVsAcid"], request.MinArmorModVsAcid, request.MaxArmorModVsAcid, "armorModVsAcid", clauses, parameters);
                AddMinMaxFilter(SortExpressions["armorModVsElectric"], request.MinArmorModVsElectric, request.MaxArmorModVsElectric, "armorModVsElectric", clauses, parameters);
                AddMinMaxFilter(SortExpressions["armorWarMagicModPerSlot"], request.MinArmorWarMagicMod, request.MaxArmorWarMagicMod, "armorWarMagicMod", clauses, parameters);
                AddMinMaxFilter(SortExpressions["armorLifeMagicModPerSlot"], request.MinArmorLifeMagicMod, request.MaxArmorLifeMagicMod, "armorLifeMagicMod", clauses, parameters);
                AddMinMaxFilter(SortExpressions["armorManaRegenModPerSlot"], request.MinArmorManaRegenMod, request.MaxArmorManaRegenMod, "armorManaRegenMod", clauses, parameters);
                AddMinMaxFilter(SortExpressions["armorManaConversionModPerSlot"], request.MinArmorManaConversionMod, request.MaxArmorManaConversionMod, "armorManaConversionMod", clauses, parameters);
                AddMinMaxFilter(SortExpressions["armorAttackModPerSlot"], request.MinArmorAttackMod, request.MaxArmorAttackMod, "armorAttackMod", clauses, parameters);
                AddMinMaxFilter(SortExpressions["armorShieldModPerSlot"], request.MinArmorShieldMod, request.MaxArmorShieldMod, "armorShieldMod", clauses, parameters);
                AddMinMaxFilter(SortExpressions["armorDualWieldModPerSlot"], request.MinArmorDualWieldMod, request.MaxArmorDualWieldMod, "armorDualWieldMod", clauses, parameters);
                AddMinMaxFilter(SortExpressions["armorThieveryModPerSlot"], request.MinArmorThieveryMod, request.MaxArmorThieveryMod, "armorThieveryMod", clauses, parameters);
                AddMinMaxFilter(SortExpressions["armorRunModPerSlot"], request.MinArmorRunMod, request.MaxArmorRunMod, "armorRunMod", clauses, parameters);
                AddMinMaxFilter(SortExpressions["armorStaminaRegenModPerSlot"], request.MinArmorStaminaRegenMod, request.MaxArmorStaminaRegenMod, "armorStaminaRegenMod", clauses, parameters);
                AddMinMaxFilter(SortExpressions["armorPhysicalDefModPerSlot"], request.MinArmorPhysicalDefMod, request.MaxArmorPhysicalDefMod, "armorPhysicalDefMod", clauses, parameters);
                AddMinMaxFilter(SortExpressions["armorMagicDefModPerSlot"], request.MinArmorMagicDefMod, request.MaxArmorMagicDefMod, "armorMagicDefMod", clauses, parameters);
                AddMinMaxFilter(SortExpressions["armorTwohandedCombatModPerSlot"], request.MinArmorTwohandedCombatMod, request.MaxArmorTwohandedCombatMod, "armorTwohandedCombatMod", clauses, parameters);
                AddMinMaxFilter(SortExpressions["armorHealthRegenModPerSlot"], request.MinArmorHealthRegenMod, request.MaxArmorHealthRegenMod, "armorHealthRegenMod", clauses, parameters);
                AddMinMaxFilter(SortExpressions["armorPerceptionModPerSlot"], request.MinArmorPerceptionMod, request.MaxArmorPerceptionMod, "armorPerceptionMod", clauses, parameters);
                AddMinMaxFilter(SortExpressions["armorDeceptionModPerSlot"], request.MinArmorDeceptionMod, request.MaxArmorDeceptionMod, "armorDeceptionMod", clauses, parameters);
                AddMinMaxFilter(SortExpressions["difficulty"], request.MinDifficulty, request.MaxDifficulty, "difficulty", clauses, parameters);
                AddMinMaxFilter(SortExpressions["spellcraft"], request.MinSpellcraft, request.MaxSpellcraft, "spellcraft", clauses, parameters);
                break;
            case "Clothing":
                // Clothing (shirts, pants, etc.) shares ItemType's "wearable" family with Armor but
                // carries none of Armor's protection/weight-class stats — just Ward Level and its
                // own vital-mod trio (ArmorHealthMod/ArmorManaMod/ArmorStaminaMod), distinct
                // properties from Armor's *RegenMod family.
                clauses.Add($"({itemTypeCol} & {ItemTypeClothing}) != 0");
                if (request.Coverage is { Count: > 0 })
                {
                    uint clothingCoverageMask = 0;
                    foreach (var part in request.Coverage)
                    {
                        if (CoverageMasks.TryGetValue(part, out var partMask))
                        {
                            clothingCoverageMask |= partMask;
                        }
                    }
                    if (clothingCoverageMask != 0)
                    {
                        clauses.Add($"(CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.ClothingPriority') AS UNSIGNED) & {clothingCoverageMask}) != 0");
                    }
                }
                AddMinMaxFilter(SortExpressions["wardPerSlot"], request.MinWardLevel, request.MaxWardLevel, "wardLevel", clauses, parameters);
                AddMinMaxFilter(SortExpressions["armorHealthModPerSlot"], request.MinArmorHealthMod, request.MaxArmorHealthMod, "armorHealthMod", clauses, parameters);
                AddMinMaxFilter(SortExpressions["armorManaModPerSlot"], request.MinArmorManaMod, request.MaxArmorManaMod, "armorManaMod", clauses, parameters);
                AddMinMaxFilter(SortExpressions["armorStaminaModPerSlot"], request.MinArmorStaminaMod, request.MaxArmorStaminaMod, "armorStaminaMod", clauses, parameters);
                AddMinMaxFilter(SortExpressions["difficulty"], request.MinDifficulty, request.MaxDifficulty, "difficulty", clauses, parameters);
                AddMinMaxFilter(SortExpressions["spellcraft"], request.MinSpellcraft, request.MaxSpellcraft, "spellcraft", clauses, parameters);
                break;
            case "Salvage":
                clauses.Add("CAST(JSON_EXTRACT(ItemSnapshotJson, '$.WeenieType') AS UNSIGNED) = " + WeenieTypeSalvage);
                if (request.SalvageMaterial is { Length: > 0 })
                {
                    var materialId = MaterialTypeNames.FirstOrDefault(kv => kv.Value == request.SalvageMaterial).Key;
                    if (materialId != 0)
                    {
                        clauses.Add("JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.MaterialType') = @salvageMaterial");
                        parameters.Add(("@salvageMaterial", materialId));
                    }
                }
                else if (request.SalvageTargetType is { Length: > 0 })
                {
                    var targetSet = request.SalvageTargetType switch
                    {
                        "Weapon" => SalvageWeaponTargetMaterials,
                        "Armor" => SalvageArmorTargetMaterials,
                        "Jewelry" => SalvageJewelryTargetMaterials,
                        _ => null,
                    };
                    if (targetSet is { Count: > 0 })
                    {
                        clauses.Add($"JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.MaterialType') IN ({string.Join(",", targetSet)})");
                    }
                }
                AddMinMaxFilter(SortExpressions["workmanship"], request.MinWorkmanship, request.MaxWorkmanship, "workmanship", clauses, parameters);
                AddMinMaxFilter(SortExpressions["units"], request.MinUnits, request.MaxUnits, "units", clauses, parameters);
                AddMinMaxFilter(SortExpressions["pricePerUnit"], request.MinPricePerUnit, request.MaxPricePerUnit, "pricePerUnit", clauses, parameters);
                break;
            case "SigilTrinket":
                clauses.Add("JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.SigilTrinketType') IS NOT NULL");
                if (request.SigilTrinketType is { Length: > 0 })
                {
                    var index = Array.IndexOf(SigilTrinketTypeNames, request.SigilTrinketType);
                    if (index >= 0)
                    {
                        clauses.Add("JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.SigilTrinketType') = @sigilTrinketType");
                        parameters.Add(("@sigilTrinketType", index));
                    }
                }
                if (request.SigilEffect is { Length: > 0 } && Array.IndexOf(SigilEffectNames, request.SigilEffect) >= 0)
                {
                    // The effect name is baked into the item's name as a " of <Effect>" suffix (see
                    // SigilEffectName computation in ReadListing) rather than stored as its own property.
                    clauses.Add($"{SortExpressions["name"]} LIKE @sigilEffectPattern");
                    parameters.Add(("@sigilEffectPattern", "% of " + request.SigilEffect));
                }
                if (request.SigilMinLevel.HasValue)
                {
                    clauses.Add($"{ItemLevelSqlExpr} >= @sigilMinLevel");
                    parameters.Add(("@sigilMinLevel", request.SigilMinLevel.Value));
                }
                if (request.SigilMaxLevel.HasValue)
                {
                    clauses.Add($"{ItemLevelSqlExpr} <= @sigilMaxLevel");
                    parameters.Add(("@sigilMaxLevel", request.SigilMaxLevel.Value));
                }
                AddMinMaxFilter(SortExpressions["procChance"], request.MinProcChance, request.MaxProcChance, "procChance", clauses, parameters);
                AddMinMaxFilter(SortExpressions["cooldown"], request.MinCooldown, request.MaxCooldown, "cooldown", clauses, parameters);
                AddMinMaxFilter(SortExpressions["manaReserved"], request.MinManaReserved, request.MaxManaReserved, "manaReserved", clauses, parameters);
                AddMinMaxFilter(SortExpressions["staminaReserved"], request.MinStaminaReserved, request.MaxStaminaReserved, "staminaReserved", clauses, parameters);
                AddMinMaxFilter(SortExpressions["healthReserved"], request.MinHealthReserved, request.MaxHealthReserved, "healthReserved", clauses, parameters);
                break;
            case "Jewelry":
                clauses.Add($"({itemTypeCol} & {ItemTypeJewelry}) != 0");
                // Sigil trinkets are also worn in a jewelry-like slot and can carry the Jewelry
                // ItemType bit, but they belong in the SigilTrinket category, not here.
                clauses.Add("JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.SigilTrinketType') IS NULL");
                if (request.JewelrySlot is { Length: > 0 })
                {
                    var mask = request.JewelrySlot switch
                    {
                        "Amulet" => EquipMaskNeckWear,
                        "Bracelet" => EquipMaskWristWear,
                        "Ring" => EquipMaskFingerWear,
                        _ => (uint?)null,
                    };
                    if (mask.HasValue)
                    {
                        clauses.Add($"(CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.ValidLocations') AS UNSIGNED) & {mask.Value}) != 0");
                    }
                }
                AddMinMaxFilter(SortExpressions["wardPerSlot"], request.MinWardLevel, request.MaxWardLevel, "wardLevel", clauses, parameters);
                AddMinMaxFilter(SortExpressions["gearCritDamage"], request.MinGearCritDamage, request.MaxGearCritDamage, "gearCritDamage", clauses, parameters);
                AddMinMaxFilter(SortExpressions["gearCritDamageResist"], request.MinGearCritDamageResist, request.MaxGearCritDamageResist, "gearCritDamageResist", clauses, parameters);
                AddMinMaxFilter(SortExpressions["gearDamage"], request.MinGearDamage, request.MaxGearDamage, "gearDamage", clauses, parameters);
                AddMinMaxFilter(SortExpressions["gearDamageResist"], request.MinGearDamageResist, request.MaxGearDamageResist, "gearDamageResist", clauses, parameters);
                AddMinMaxFilter(SortExpressions["gearHealingBoost"], request.MinGearHealingBoost, request.MaxGearHealingBoost, "gearHealingBoost", clauses, parameters);
                AddMinMaxFilter(SortExpressions["gearMaxHealth"], request.MinGearMaxHealth, request.MaxGearMaxHealth, "gearMaxHealth", clauses, parameters);
                AddMinMaxFilter(SortExpressions["difficulty"], request.MinDifficulty, request.MaxDifficulty, "difficulty", clauses, parameters);
                AddMinMaxFilter(SortExpressions["spellcraft"], request.MinSpellcraft, request.MaxSpellcraft, "spellcraft", clauses, parameters);
                break;
            case "Consumable":
                clauses.Add($"CAST(JSON_EXTRACT(ItemSnapshotJson, '$.WeenieType') AS UNSIGNED) IN ({WeenieTypeFood}, {WeenieTypeHealer})");
                switch (request.ConsumableType)
                {
                    case "HealingKit":
                        clauses.Add($"CAST(JSON_EXTRACT(ItemSnapshotJson, '$.WeenieType') AS UNSIGNED) = {WeenieTypeHealer}");
                        break;
                    case "Food":
                        clauses.Add($"CAST(JSON_EXTRACT(ItemSnapshotJson, '$.WeenieType') AS UNSIGNED) = {WeenieTypeFood}");
                        clauses.Add($"({itemTypeCol} & {ItemTypeFood}) != 0");
                        break;
                    case "Potion":
                        clauses.Add($"CAST(JSON_EXTRACT(ItemSnapshotJson, '$.WeenieType') AS UNSIGNED) = {WeenieTypeFood}");
                        clauses.Add($"({itemTypeCol} & {ItemTypeMisc}) != 0");
                        break;
                }
                break;
            case "Misc":
                // Catch-all: not Salvage, not a Sigil Trinket, not Jewelry/Armor/Clothing/Weapon by
                // ItemType, and not a Consumable — mirrors the else-chain in ReadListing exactly.
                clauses.Add($"""
                    NOT (
                        CAST(JSON_EXTRACT(ItemSnapshotJson, '$.WeenieType') AS UNSIGNED) = {WeenieTypeSalvage}
                        OR JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.SigilTrinketType') IS NOT NULL
                        OR ({itemTypeCol} & {ItemTypeJewelry}) != 0
                        OR ({itemTypeCol} & {ItemTypeArmorMask}) != 0
                        OR ({itemTypeCol} & {ItemTypeWeaponMask}) != 0
                        OR CAST(JSON_EXTRACT(ItemSnapshotJson, '$.WeenieType') AS UNSIGNED) IN ({WeenieTypeFood}, {WeenieTypeHealer})
                    )
                    """);
                switch (request.MiscType)
                {
                    case "Scroll":
                        clauses.Add($"CAST(JSON_EXTRACT(ItemSnapshotJson, '$.WeenieType') AS UNSIGNED) = {WeenieTypeScroll}");
                        if (request.ScrollSchool is { Length: > 0 } && MagicSchoolNameToValue.TryGetValue(request.ScrollSchool, out var schoolValue))
                        {
                            var schoolSpellIds = SpellSchoolById.Where(kv => kv.Value == schoolValue).Select(kv => kv.Key).ToList();
                            clauses.Add(
                                schoolSpellIds.Count > 0
                                    ? $"CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesDID.Spell') AS UNSIGNED) IN ({string.Join(",", schoolSpellIds)})"
                                    : "1=0"
                            );
                        }
                        break;
                    case "Gem":
                        // Loose gemstones (Gem) and carved jewels (Jewel) share one subtype — see
                        // ReadListing's Gem branch for why.
                        clauses.Add($"CAST(JSON_EXTRACT(ItemSnapshotJson, '$.WeenieType') AS UNSIGNED) IN ({WeenieTypeGem}, {WeenieTypeJewel})");
                        if (request.CarvedJewelOnly == true)
                        {
                            clauses.Add($"CAST(JSON_EXTRACT(ItemSnapshotJson, '$.WeenieType') AS UNSIGNED) = {WeenieTypeJewel}");
                        }
                        if (request.GemMaterial is { Length: > 0 })
                        {
                            var gemMaterialId = MaterialTypeNames.FirstOrDefault(kv => kv.Value == request.GemMaterial).Key;
                            if (gemMaterialId != 0)
                            {
                                clauses.Add($"""
                                    (
                                        (CAST(JSON_EXTRACT(ItemSnapshotJson, '$.WeenieType') AS UNSIGNED) = {WeenieTypeGem} AND JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.MaterialType') = @gemMaterial)
                                        OR (CAST(JSON_EXTRACT(ItemSnapshotJson, '$.WeenieType') AS UNSIGNED) = {WeenieTypeJewel} AND JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.JewelMaterialType') = @gemMaterial)
                                    )
                                    """);
                                parameters.Add(("@gemMaterial", gemMaterialId));
                            }
                        }
                        // Quality is ItemWorkmanship for a loose Gem, JewelQuality for a carved Jewel —
                        // both are the same 1-10 scale, just stored under different properties.
                        AddMinMaxFilter(
                            "COALESCE(CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.JewelQuality') AS SIGNED), CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.ItemWorkmanship') AS SIGNED))",
                            request.MinGemQuality, request.MaxGemQuality, "gemQuality", clauses, parameters
                        );
                        break;
                    case "Trophy":
                        clauses.Add("JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.TrophyQuality') IS NOT NULL");
                        if (request.TrophyType is { Length: > 0 } && Array.IndexOf(TrophyTypeNames, request.TrophyType) >= 0)
                        {
                            clauses.Add($"{SortExpressions["name"]} LIKE @trophyTypePattern");
                            parameters.Add(("@trophyTypePattern", "%" + request.TrophyType));
                        }
                        AddMinMaxFilter(
                            "CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.TrophyQuality') AS SIGNED)",
                            request.MinTrophyQuality, request.MaxTrophyQuality, "trophyQuality", clauses, parameters
                        );
                        break;
                    case "Other":
                        clauses.Add($"""
                            NOT (
                                CAST(JSON_EXTRACT(ItemSnapshotJson, '$.WeenieType') AS UNSIGNED) IN ({WeenieTypeScroll}, {WeenieTypeGem}, {WeenieTypeJewel})
                                OR JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.TrophyQuality') IS NOT NULL
                            )
                            """);
                        break;
                }
                break;
        }
    }

    private static void AddMinMaxFilter(string sqlExpr, double? min, double? max, string paramPrefix, List<string> clauses, List<(string Name, object? Value)> parameters)
    {
        if (min.HasValue)
        {
            clauses.Add($"{sqlExpr} >= @{paramPrefix}Min");
            parameters.Add(($"@{paramPrefix}Min", min.Value));
        }
        if (max.HasValue)
        {
            clauses.Add($"{sqlExpr} <= @{paramPrefix}Max");
            parameters.Add(($"@{paramPrefix}Max", max.Value));
        }
    }

    private static void AddWeaponClassFilter(string? weaponClass, List<string> clauses)
    {
        var weaponTypeCol = "JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.WeaponType')";
        var combatStyleCol = "CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.DefaultCombatStyle') AS UNSIGNED)";
        var weaponSkillCol = "JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.WeaponSkill')";
        var itemTypeCol = "CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.ItemType') AS UNSIGNED)";

        switch (weaponClass)
        {
            case "Martial":
                clauses.Add($"({weaponTypeCol} IN ({WeaponTypeSword},{WeaponTypeAxe},{WeaponTypeMace},{WeaponTypeSpear},{WeaponTypeTwoHanded}) OR {weaponSkillCol} IN ({SkillSword},{SkillAxe},{SkillMace},{SkillSpear},{SkillTwoHandedCombat}))");
                break;
            case "Missile":
                clauses.Add($"({weaponTypeCol} IN ({WeaponTypeBow},{WeaponTypeCrossbow}) OR ({combatStyleCol} & {CombatStyleAtlatl}) != 0 OR {weaponSkillCol} IN ({SkillBow},{SkillCrossbow}))");
                break;
            case "Thrown":
                clauses.Add($"(({combatStyleCol} & {CombatStyleThrownWeapon}) != 0 OR {weaponSkillCol} = {SkillThrownWeapon})");
                break;
            case "Dagger":
                clauses.Add($"({weaponTypeCol} = {WeaponTypeDagger} OR {weaponSkillCol} = {SkillDagger})");
                break;
            case "Staff":
                clauses.Add($"({weaponTypeCol} = {WeaponTypeStaff} OR {weaponSkillCol} = {SkillStaff})");
                break;
            case "Unarmed":
                clauses.Add($"({weaponTypeCol} = {WeaponTypeUnarmed} OR {weaponSkillCol} = {SkillUnarmedCombat})");
                break;
            case "WarMagic":
                clauses.Add($"({itemTypeCol} & {ItemTypeCaster}) != 0 AND {weaponSkillCol} = {SkillWarMagic}");
                break;
            case "LifeMagic":
                clauses.Add($"({itemTypeCol} & {ItemTypeCaster}) != 0 AND {weaponSkillCol} = {SkillLifeMagic}");
                break;
        }
    }

    private static void AddFilterParams(MySqlCommand cmd, MarketSearchRequest request, List<(string Name, object? Value)> extraParams)
    {
        cmd.Parameters.AddWithValue("@keyword", request.Keyword);
        cmd.Parameters.AddWithValue("@keywordLike", string.IsNullOrEmpty(request.Keyword) ? null : $"%{request.Keyword}%");
        cmd.Parameters.AddWithValue("@minPrice", request.MinPrice);
        cmd.Parameters.AddWithValue("@maxPrice", request.MaxPrice);
        cmd.Parameters.AddWithValue("@minTier", request.MinTier);
        cmd.Parameters.AddWithValue("@maxTier", request.MaxTier);
        foreach (var (name, value) in extraParams)
        {
            cmd.Parameters.AddWithValue(name, value);
        }
    }

    private (MarketListing Listing, List<int> SpellIds) ReadListing(MySqlDataReader reader)
    {
        long? iconDid = GetNullableLong(reader, "IconDidRaw");
        long? iconOverlayDid = GetNullableLong(reader, "IconOverlayDidRaw");
        var itemType = GetNullableUInt(reader, "ItemTypeRaw");
        var weaponType = GetNullableInt(reader, "WeaponTypeRaw");
        var combatStyle = GetNullableUInt(reader, "CombatStyleRaw");
        var weaponSkill = GetNullableInt(reader, "WeaponSkillRaw");
        var armorWeightClass = GetNullableInt(reader, "ArmorWeightClassRaw");
        var sigilTrinketType = GetNullableInt(reader, "SigilTrinketTypeRaw");
        var sigilTrinketColor = GetNullableInt(reader, "SigilTrinketColorRaw");
        var validLocations = GetNullableUInt(reader, "ValidLocationsRaw");
        var clothingPriority = GetNullableUInt(reader, "ClothingPriorityRaw");
        var imbuedEffect = GetNullableUInt(reader, "ImbuedEffectRaw");
        var baseDamage = GetNullableInt(reader, "DamageRaw");
        var damageVariance = GetNullableDouble(reader, "DamageVarianceRaw");
        var damageMod = GetNullableDouble(reader, "DamageModRaw");
        var damageType = GetNullableInt(reader, "DamageTypeRaw");
        var weaponTime = GetNullableInt(reader, "WeaponTimeRaw");
        var attackType = GetNullableUInt(reader, "AttackTypeRaw");
        var wieldRequirements = ComputeWieldRequirements(new (int?, int?, int?)[]
        {
            (GetNullableInt(reader, "WieldRequirementsRaw"), GetNullableInt(reader, "WieldSkillTypeRaw"), GetNullableInt(reader, "WieldDifficultyRaw")),
            (GetNullableInt(reader, "WieldRequirements2Raw"), GetNullableInt(reader, "WieldSkillType2Raw"), GetNullableInt(reader, "WieldDifficulty2Raw")),
            (GetNullableInt(reader, "WieldRequirements3Raw"), GetNullableInt(reader, "WieldSkillType3Raw"), GetNullableInt(reader, "WieldDifficulty3Raw")),
            (GetNullableInt(reader, "WieldRequirements4Raw"), GetNullableInt(reader, "WieldSkillType4Raw"), GetNullableInt(reader, "WieldDifficulty4Raw")),
        });
        var elementalDamageMod = GetNullableDouble(reader, "ElementalDamageModRaw");
        var spellCount = GetNullableInt(reader, "SpellCountRaw");
        var itemName = reader.IsDBNull(reader.GetOrdinal("ItemName")) ? null : reader.GetString("ItemName");
        var itemTier = reader.IsDBNull(reader.GetOrdinal("ItemTier")) ? null : (int?)reader.GetInt32("ItemTier");
        var weenieType = GetNullableInt(reader, "WeenieTypeRaw");
        var materialType = GetNullableInt(reader, "MaterialTypeRaw");
        var itemWorkmanship = GetNullableInt(reader, "ItemWorkmanshipRaw");
        var numItemsInMaterial = GetNullableInt(reader, "NumItemsInMaterialRaw");
        var structure = GetNullableInt(reader, "StructureRaw");
        var armorSlots = GetNullableInt(reader, "ArmorSlotsRaw");
        var itemTotalXp = GetNullableLong(reader, "ItemTotalXpRaw");
        var itemBaseXp = GetNullableLong(reader, "ItemBaseXpRaw");
        var itemXpStyle = GetNullableInt(reader, "ItemXpStyleRaw");
        var itemMaxLevel = GetNullableInt(reader, "ItemMaxLevelRaw");
        var allPropertiesIntRaw = reader.IsDBNull(reader.GetOrdinal("AllPropertiesIntRaw")) ? null : reader.GetString("AllPropertiesIntRaw");
        var sockets = GetNullableInt(reader, "SocketsRaw");
        var staminaCostReductionMod = GetNullableDouble(reader, "StaminaCostReductionModRaw");
        var ignoreWard = GetNullableDouble(reader, "IgnoreWardRaw");
        var ignoreArmor = GetNullableDouble(reader, "IgnoreArmorRaw");
        var criticalMultiplier = GetNullableDouble(reader, "CriticalMultiplierRaw");
        var criticalFrequency = GetNullableDouble(reader, "CriticalFrequencyRaw");
        var resistanceModifier = GetNullableDouble(reader, "ResistanceModifierRaw");
        var resistanceModifierType = GetNullableInt(reader, "ResistanceModifierTypeRaw");
        var noCompsRequiredForMagicSchool = GetNullableInt(reader, "NoCompsRequiredForMagicSchoolRaw");
        var trophyQuality = GetNullableInt(reader, "TrophyQualityRaw");
        var jewelQuality = GetNullableInt(reader, "JewelQualityRaw");
        var jewelMaterialType = GetNullableInt(reader, "JewelMaterialTypeRaw");
        var spellDid = GetNullableInt(reader, "SpellDidRaw");
        var spellBookRaw = reader.IsDBNull(reader.GetOrdinal("SpellBookRaw")) ? null : reader.GetString("SpellBookRaw");
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

        var listing = new MarketListing
        {
            Id = reader.GetInt32("Id"),
            ItemWeenieClassId = reader.GetUInt32("ItemWeenieClassId"),
            ListedPrice = reader.GetInt32("ListedPrice"),
            ItemTier = itemTier,
            MarketVendorTier = reader.GetInt32("MarketVendorTier"),
            CreatedAtUtc = reader.GetDateTime("CreatedAtUtc"),
            ExpiresAtUtc = reader.GetDateTime("ExpiresAtUtc"),
            ItemName = itemName,
            ItemDesc = reader.IsDBNull(reader.GetOrdinal("ItemDesc")) ? null : reader.GetString("ItemDesc"),
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
            Speed = weaponTime.HasValue ? Math.Clamp(2.0 - weaponTime.Value / 100.0, 1.0, 2.5) : null,
            DamageMod = damageMod,
            ElementalDamageMod = elementalDamageMod,
            RestorationMod = GetNullableDouble(reader, "RestorationModRaw"),
            Spellcraft = GetNullableInt(reader, "SpellcraftRaw"),
            Difficulty = GetNullableInt(reader, "DifficultyRaw"),
            Mana = GetNullableInt(reader, "ManaRaw"),
            Workmanship = itemWorkmanship,
            NumTimesTinkered = GetNullableInt(reader, "NumTimesTinkeredRaw"),
            Sockets = sockets,
            ElementalDamageBonus = GetNullableInt(reader, "ElementalDamageBonusRaw"),
            WeaponOffense = GetNullableDouble(reader, "WeaponOffenseRaw"),
            WeaponWarMagicMod = GetNullableDouble(reader, "WeaponWarMagicModRaw"),
            WeaponLifeMagicMod = GetNullableDouble(reader, "WeaponLifeMagicModRaw"),
            WeaponPhysicalDefense = GetNullableDouble(reader, "WeaponPhysicalDefenseRaw"),
            WeaponMagicalDefense = GetNullableDouble(reader, "WeaponMagicalDefenseRaw"),
            Dps = ComputeDps(itemName, weaponType, combatStyle, weaponTime, itemTier, baseDamage, damageVariance, damageMod, itemType, attackType, weaponSkill, elementalDamageMod),
            ArmorSlots = armorSlots,
            ArmorLevel = GetNullableInt(reader, "ArmorLevelRaw"),
            WardLevel = GetNullableInt(reader, "WardLevelRaw"),
            WardPerSlot = GetNullableInt(reader, "WardLevelRaw").HasValue
                ? (double)GetNullableInt(reader, "WardLevelRaw")!.Value / Math.Max(armorSlots ?? 1, 1)
                : null,
            ArmorModVsSlash = GetNullableDouble(reader, "ArmorModVsSlashRaw"),
            ArmorModVsPierce = GetNullableDouble(reader, "ArmorModVsPierceRaw"),
            ArmorModVsBludgeon = GetNullableDouble(reader, "ArmorModVsBludgeonRaw"),
            ArmorModVsCold = GetNullableDouble(reader, "ArmorModVsColdRaw"),
            ArmorModVsFire = GetNullableDouble(reader, "ArmorModVsFireRaw"),
            ArmorModVsAcid = GetNullableDouble(reader, "ArmorModVsAcidRaw"),
            ArmorModVsElectric = GetNullableDouble(reader, "ArmorModVsElectricRaw"),
            ArmorModVsNether = GetNullableDouble(reader, "ArmorModVsNetherRaw"),
            ArmorWarMagicMod = GetNullableDouble(reader, "ArmorWarMagicModRaw"),
            ArmorLifeMagicMod = GetNullableDouble(reader, "ArmorLifeMagicModRaw"),
            ArmorManaRegenMod = GetNullableDouble(reader, "ArmorManaRegenModRaw"),
            ArmorManaConversionMod = GetNullableDouble(reader, "ArmorManaConversionModRaw"),
            ArmorAttackMod = GetNullableDouble(reader, "ArmorAttackModRaw"),
            ArmorShieldMod = GetNullableDouble(reader, "ArmorShieldModRaw"),
            ArmorDualWieldMod = GetNullableDouble(reader, "ArmorDualWieldModRaw"),
            ArmorThieveryMod = GetNullableDouble(reader, "ArmorThieveryModRaw"),
            ArmorRunMod = GetNullableDouble(reader, "ArmorRunModRaw"),
            ArmorStaminaRegenMod = GetNullableDouble(reader, "ArmorStaminaRegenModRaw"),
            ArmorPhysicalDefMod = GetNullableDouble(reader, "ArmorPhysicalDefModRaw"),
            ArmorMagicDefMod = GetNullableDouble(reader, "ArmorMagicDefModRaw"),
            ArmorTwohandedCombatMod = GetNullableDouble(reader, "ArmorTwohandedCombatModRaw"),
            ArmorHealthRegenMod = GetNullableDouble(reader, "ArmorHealthRegenModRaw"),
            ArmorPerceptionMod = GetNullableDouble(reader, "ArmorPerceptionModRaw"),
            ArmorDeceptionMod = GetNullableDouble(reader, "ArmorDeceptionModRaw"),
            ArmorHealthMod = GetNullableDouble(reader, "ArmorHealthModRaw"),
            ArmorManaMod = GetNullableDouble(reader, "ArmorManaModRaw"),
            ArmorStaminaMod = GetNullableDouble(reader, "ArmorStaminaModRaw"),
            GearCritDamage = GetNullableInt(reader, "GearCritDamageRaw"),
            GearCritDamageResist = GetNullableInt(reader, "GearCritDamageResistRaw"),
            GearDamage = GetNullableInt(reader, "GearDamageRaw"),
            GearDamageResist = GetNullableInt(reader, "GearDamageResistRaw"),
            GearHealingBoost = GetNullableInt(reader, "GearHealingBoostRaw"),
            GearMaxHealth = GetNullableInt(reader, "GearMaxHealthRaw"),
            SigilTriggerChance = GetNullableDouble(reader, "SigilTriggerChanceRaw"),
            SigilCooldown = GetNullableDouble(reader, "SigilCooldownRaw"),
            SigilManaReserved = GetNullableDouble(reader, "SigilManaReservedRaw"),
            SigilStaminaReserved = GetNullableDouble(reader, "SigilStaminaReservedRaw"),
            SigilHealthReserved = GetNullableDouble(reader, "SigilHealthReservedRaw"),
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
    private static double? ComputeDps(string? itemName, int? weaponType, uint? combatStyle, int? weaponTime, int? itemTier, int? baseDamage, double? damageVariance, double? damageMod, uint? itemType, uint? attackType, int? weaponSkill, double? elementalDamageMod)
    {
        if (itemType.HasValue && (itemType.Value & ItemTypeCaster) != 0 && weaponSkill is SkillWarMagic or SkillLifeMagic)
        {
            if (!itemTier.HasValue || itemTier.Value < 1 || itemTier.Value > BoltAvgDamageByTier.Length)
            {
                return null;
            }

            var i = itemTier.Value - 1;
            return BoltAvgDamageByTier[i] / BoltCastCycleTimeByTier[i] * (elementalDamageMod ?? 1.0);
        }

        if (!weaponTime.HasValue)
        {
            return null;
        }

        var animSpeed = Math.Clamp(2.0 - weaponTime.Value / 100.0, 1.0, 2.5);
        var meleeDamage = baseDamage.HasValue && damageVariance.HasValue
            ? baseDamage.Value * (1 - damageVariance.Value / 2) * (damageMod ?? 1.0)
            : (double?)null;

        double? damage;
        double cycleTime;
        if (combatStyle.HasValue && (combatStyle.Value & CombatStyleThrownWeapon) != 0)
        {
            damage = meleeDamage;
            cycleTime = AnimLenThrownFixed + AnimLenThrownModdable / animSpeed;
        }
        else if (weaponType == WeaponTypeBow)
        {
            damage = AmmoDamage(AmmoDamByTierBow, itemTier, damageMod);
            cycleTime = AnimLenBow + ReloadLenBow / animSpeed;
        }
        else if (weaponType == WeaponTypeCrossbow)
        {
            damage = AmmoDamage(AmmoDamByTierCrossbow, itemTier, damageMod);
            cycleTime = AnimLenCrossbow + ReloadLenCrossbow / animSpeed;
        }
        else if (combatStyle.HasValue && (combatStyle.Value & CombatStyleAtlatl) != 0)
        {
            damage = AmmoDamage(AmmoDamByTierAtlatl, itemTier, damageMod);
            cycleTime = AnimLenAtlatl + ReloadLenAtlatl / animSpeed;
        }
        else if (weaponType == WeaponTypeDagger)
        {
            damage = meleeDamage;
            var animLen = itemName is "Dirk" or "Jitte" ? AnimLenDaggerLarge : AnimLenDaggerSmall;
            cycleTime = animLen / animSpeed;
        }
        else if (weaponType == WeaponTypeSpear)
        {
            damage = meleeDamage;
            cycleTime = AnimLenSpear / animSpeed;
        }
        else if (weaponType == WeaponTypeUnarmed)
        {
            damage = meleeDamage;
            cycleTime = AnimLenUnarmed / animSpeed;
        }
        else if (weaponType == WeaponTypeTwoHanded)
        {
            damage = meleeDamage;
            cycleTime = AnimLenTwoHanded / animSpeed;
        }
        else if (weaponType is WeaponTypeSword or WeaponTypeAxe or WeaponTypeMace or WeaponTypeStaff)
        {
            damage = meleeDamage;
            cycleTime = AnimLenSwordAxeMaceStaff / animSpeed;
        }
        else if (!itemType.HasValue || (itemType.Value & ItemTypeCaster) == 0)
        {
            // WeaponType fallback: some weenies genuinely lack it, but the small, reliably
            // populated AttackType flag set (Punch/Thrust/Slash/Kick) is still present.
            if (attackType.HasValue && (attackType.Value & AttackTypeUnarmedMask) != 0)
            {
                damage = meleeDamage;
                cycleTime = AnimLenUnarmed / animSpeed;
            }
            else if (attackType is > 0)
            {
                damage = meleeDamage;
                cycleTime = AnimLenSwordAxeMaceStaff / animSpeed;
            }
            else
            {
                return null;
            }
        }
        else
        {
            return null;
        }

        return damage.HasValue ? damage.Value / cycleTime : null;
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

    private static int? GetNullableInt(MySqlDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : Convert.ToInt32(reader.GetValue(ordinal));
    }

    private static uint? GetNullableUInt(MySqlDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : Convert.ToUInt32(reader.GetValue(ordinal));
    }

    private static long? GetNullableLong(MySqlDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : Convert.ToInt64(reader.GetValue(ordinal));
    }

    private static double? GetNullableDouble(MySqlDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : Convert.ToDouble(reader.GetValue(ordinal));
    }
}
