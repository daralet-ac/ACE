using System;
using System.ComponentModel;
using System.Globalization;

namespace ACE.Entity.Enum.Properties;

public enum PropertyInt : ushort
{
    // No properties are sent to the client unless they feature an attribute.
    // SendOnLogin gets sent to players in the PlayerDescription event
    // AssessmentProperty gets sent in successful appraisal
    // description attributes are used by the weenie editor for a cleaner display name

    Undef = 0,
    ItemType = 1,

    [AssessmentProperty]
    CreatureType = 2,
    PaletteTemplate = 3,
    ClothingPriority = 4,

    [AssessmentProperty]
    [SendOnLogin]
    EncumbranceVal = 5, // ENCUMB_VAL_INT,
    ItemsCapacity = 6,

    [SendOnLogin]
    ContainersCapacity = 7,
    Mass = 8,
    ValidLocations = 9, // LOCATIONS_INT

    CurrentWieldedLocation = 10,
    MaxStackSize = 11,
    StackSize = 12,
    StackUnitEncumbrance = 13,
    StackUnitMass = 14,
    StackUnitValue = 15,
    ItemUseable = 16,

    [AssessmentProperty]
    RareId = 17,
    UiEffects = 18,

    [AssessmentProperty]
    Value = 19,

    [Ephemeral]
    [SendOnLogin]
    CoinValue = 20,
    TotalExperience = 21,
    AvailableCharacter = 22,
    TotalSkillCredits = 23,

    [SendOnLogin]
    AvailableSkillCredits = 24,

    [AssessmentProperty]
    [SendOnLogin]
    Level = 25,

    [AssessmentProperty]
    AccountRequirements = 26,
    ArmorType = 27,

    [AssessmentProperty]
    ArmorLevel = 28,
    AllegianceCpPool = 29,

    [AssessmentProperty]
    [SendOnLogin]
    AllegianceRank = 30,
    ChannelsAllowed = 31,
    ChannelsActive = 32,

    [AssessmentProperty]
    Bonded = 33,
    MonarchsRank = 34,

    [AssessmentProperty]
    AllegianceFollowers = 35,

    [AssessmentProperty]
    ResistMagic = 36,
    ResistItemAppraisal = 37,

    [AssessmentProperty]
    ResistLockpick = 38,
    DeprecatedResistRepair = 39,

    [SendOnLogin]
    CombatMode = 40,
    CurrentAttackHeight = 41,
    CombatCollisions = 42,

    [AssessmentProperty]
    [SendOnLogin]
    NumDeaths = 43,
    Damage = 44,

    [AssessmentProperty]
    DamageType = 45,
    DefaultCombatStyle = 46,

    [AssessmentProperty]
    [SendOnLogin]
    AttackType = 47,
    WeaponSkill = 48,
    WeaponTime = 49,
    AmmoType = 50,
    CombatUse = 51,
    ParentLocation = 52,

    /// <summary>
    /// TODO: Migrate inventory order away from this and instead use the new InventoryOrder property
    /// TODO: PlacementPosition is used (very sparingly) in cache.bin, so it has (or had) a meaning at one point before we hijacked it
    /// TODO: and used it for our own inventory order
    /// </summary>
    PlacementPosition = 53,
    WeaponEncumbrance = 54,
    WeaponMass = 55,
    ShieldValue = 56,
    ShieldEncumbrance = 57,
    MissileInventoryLocation = 58,
    FullDamageType = 59,
    WeaponRange = 60,
    AttackersSkill = 61,
    DefendersSkill = 62,
    AttackersSkillValue = 63,
    AttackersClass = 64,
    Placement = 65,
    CheckpointStatus = 66,
    Tolerance = 67,
    TargetingTactic = 68,
    CombatTactic = 69,
    HomesickTargetingTactic = 70,
    NumFollowFailures = 71,
    FriendType = 72,
    FoeType = 73,
    MerchandiseItemTypes = 74,
    MerchandiseMinValue = 75,
    MerchandiseMaxValue = 76,
    NumItemsSold = 77,
    NumItemsBought = 78,
    MoneyIncome = 79,
    MoneyOutflow = 80,

    [Ephemeral]
    MaxGeneratedObjects = 81,

    [Ephemeral]
    InitGeneratedObjects = 82,
    ActivationResponse = 83,
    OriginalValue = 84,
    NumMoveFailures = 85,

    [AssessmentProperty]
    MinLevel = 86,

    [AssessmentProperty]
    MaxLevel = 87,
    LockpickMod = 88,

    [AssessmentProperty]
    BoosterEnum = 89,

    [AssessmentProperty]
    BoostValue = 90,

    [AssessmentProperty]
    MaxStructure = 91,

    [AssessmentProperty]
    Structure = 92,
    PhysicsState = 93,
    TargetType = 94,
    RadarBlipColor = 95,
    EncumbranceCapacity = 96,
    LoginTimestamp = 97,

    [AssessmentProperty]
    [SendOnLogin]
    CreationTimestamp = 98,
    PkLevelModifier = 99,
    GeneratorType = 100,
    AiAllowedCombatStyle = 101,
    LogoffTimestamp = 102,
    GeneratorDestructionType = 103,
    ActivationCreateClass = 104,

    [AssessmentProperty]
    ItemWorkmanship = 105,

    [AssessmentProperty]
    ItemSpellcraft = 106,

    [AssessmentProperty]
    ItemCurMana = 107,

    [AssessmentProperty]
    ItemMaxMana = 108,

    [AssessmentProperty]
    ItemDifficulty = 109,

    [AssessmentProperty]
    ItemAllegianceRankLimit = 110,

    [AssessmentProperty]
    PortalBitmask = 111,
    AdvocateLevel = 112,

    [AssessmentProperty]
    [SendOnLogin]
    Gender = 113,

    [AssessmentProperty]
    Attuned = 114,

    [AssessmentProperty]
    ItemSkillLevelLimit = 115,
    GateLogic = 116,

    [AssessmentProperty]
    ItemManaCost = 117,
    Logoff = 118,
    Active = 119,
    AttackHeight = 120,
    NumAttackFailures = 121,
    AiCpThreshold = 122,
    AiAdvancementStrategy = 123,
    Version = 124,

    [AssessmentProperty]
    [SendOnLogin]
    Age = 125,
    VendorHappyMean = 126,
    VendorHappyVariance = 127,
    CloakStatus = 128,

    [SendOnLogin]
    VitaeCpPool = 129,
    NumServicesSold = 130,

    [AssessmentProperty]
    MaterialType = 131,

    [SendOnLogin]
    NumAllegianceBreaks = 132,

    [Ephemeral]
    ShowableOnRadar = 133,

    [AssessmentProperty]
    [SendOnLogin]
    PlayerKillerStatus = 134,
    VendorHappyMaxItems = 135,
    ScorePageNum = 136,
    ScoreConfigNum = 137,
    ScoreNumScores = 138,

    [SendOnLogin]
    DeathLevel = 139,
    AiOptions = 140,
    OpenToEveryone = 141,
    GeneratorTimeType = 142,
    GeneratorStartTime = 143,
    GeneratorEndTime = 144,
    GeneratorEndDestructionType = 145,
    XpOverride = 146,
    NumCrashAndTurns = 147,
    ComponentWarningThreshold = 148,
    HouseStatus = 149,
    HookPlacement = 150,
    HookType = 151,
    HookItemType = 152,
    AiPpThreshold = 153,
    GeneratorVersion = 154,
    HouseType = 155,
    PickupEmoteOffset = 156,
    WeenieIteration = 157,

    [AssessmentProperty]
    WieldRequirements = 158,

    [AssessmentProperty]
    WieldSkillType = 159,

    [AssessmentProperty]
    WieldDifficulty = 160,
    HouseMaxHooksUsable = 161,

    [Ephemeral]
    HouseCurrentHooksUsable = 162,
    AllegianceMinLevel = 163,
    AllegianceMaxLevel = 164,
    HouseRelinkHookCount = 165,

    [AssessmentProperty]
    SlayerCreatureType = 166,
    ConfirmationInProgress = 167,
    ConfirmationTypeInProgress = 168,
    TsysMutationData = 169,

    [AssessmentProperty]
    NumItemsInMaterial = 170,

    [AssessmentProperty]
    NumTimesTinkered = 171,

    [AssessmentProperty]
    AppraisalLongDescDecoration = 172,

    [AssessmentProperty]
    AppraisalLockpickSuccessPercent = 173,

    [AssessmentProperty]
    [Ephemeral]
    AppraisalPages = 174,

    [AssessmentProperty]
    [Ephemeral]
    AppraisalMaxPages = 175,

    [AssessmentProperty]
    AppraisalItemSkill = 176,

    [AssessmentProperty]
    GemCount = 177,

    [AssessmentProperty]
    GemType = 178,

    [AssessmentProperty]
    ImbuedEffect = 179,
    AttackersRawSkillValue = 180,

    [AssessmentProperty]
    [SendOnLogin]
    ChessRank = 181,
    ChessTotalGames = 182,
    ChessGamesWon = 183,
    ChessGamesLost = 184,
    TypeOfAlteration = 185,
    SkillToBeAltered = 186,
    SkillAlterationCount = 187,

    [AssessmentProperty]
    [SendOnLogin]
    HeritageGroup = 188,
    TransferFromAttribute = 189,
    TransferToAttribute = 190,
    AttributeTransferCount = 191,

    [AssessmentProperty]
    [SendOnLogin]
    FakeFishingSkill = 192,

    [AssessmentProperty]
    NumKeys = 193,
    DeathTimestamp = 194,
    PkTimestamp = 195,
    VictimTimestamp = 196,
    HookGroup = 197,
    AllegianceSwearTimestamp = 198,

    [SendOnLogin]
    HousePurchaseTimestamp = 199,
    RedirectableEquippedArmorCount = 200,
    MeleeDefenseImbuedEffectTypeCache = 201,
    MissileDefenseImbuedEffectTypeCache = 202,
    MagicDefenseImbuedEffectTypeCache = 203,

    [AssessmentProperty]
    ElementalDamageBonus = 204,
    ImbueAttempts = 205,
    ImbueSuccesses = 206,
    CreatureKills = 207,
    PlayerKillsPk = 208,
    PlayerKillsPkl = 209,
    RaresTierOne = 210,
    RaresTierTwo = 211,
    RaresTierThree = 212,
    RaresTierFour = 213,
    RaresTierFive = 214,
    AugmentationStat = 215,
    AugmentationFamilyStat = 216,
    AugmentationInnateFamily = 217,

    [SendOnLogin]
    AugmentationInnateStrength = 218,

    [SendOnLogin]
    AugmentationInnateEndurance = 219,

    [SendOnLogin]
    AugmentationInnateCoordination = 220,

    [SendOnLogin]
    AugmentationInnateQuickness = 221,

    [SendOnLogin]
    AugmentationInnateFocus = 222,

    [SendOnLogin]
    AugmentationInnateSelf = 223,

    [SendOnLogin]
    AugmentationSpecializeSalvaging = 224,

    [SendOnLogin]
    AugmentationSpecializeItemTinkering = 225,

    [SendOnLogin]
    AugmentationSpecializeArmorTinkering = 226,

    [SendOnLogin]
    AugmentationSpecializeMagicItemTinkering = 227,

    [SendOnLogin]
    AugmentationSpecializeWeaponTinkering = 228,

    [SendOnLogin]
    AugmentationExtraPackSlot = 229,

    [SendOnLogin]
    AugmentationIncreasedCarryingCapacity = 230,

    [SendOnLogin]
    AugmentationLessDeathItemLoss = 231,

    [SendOnLogin]
    AugmentationSpellsRemainPastDeath = 232,

    [SendOnLogin]
    AugmentationCriticalDefense = 233,

    [SendOnLogin]
    AugmentationBonusXp = 234,

    [SendOnLogin]
    AugmentationBonusSalvage = 235,

    [SendOnLogin]
    AugmentationBonusImbueChance = 236,

    [SendOnLogin]
    AugmentationFasterRegen = 237,

    [SendOnLogin]
    AugmentationIncreasedSpellDuration = 238,
    AugmentationResistanceFamily = 239,

    [SendOnLogin]
    AugmentationResistanceSlash = 240,

    [SendOnLogin]
    AugmentationResistancePierce = 241,

    [SendOnLogin]
    AugmentationResistanceBlunt = 242,

    [SendOnLogin]
    AugmentationResistanceAcid = 243,

    [SendOnLogin]
    AugmentationResistanceFire = 244,

    [SendOnLogin]
    AugmentationResistanceFrost = 245,

    [SendOnLogin]
    AugmentationResistanceLightning = 246,
    RaresTierOneLogin = 247,
    RaresTierTwoLogin = 248,
    RaresTierThreeLogin = 249,
    RaresTierFourLogin = 250,
    RaresTierFiveLogin = 251,
    RaresLoginTimestamp = 252,
    RaresTierSix = 253,
    RaresTierSeven = 254,
    RaresTierSixLogin = 255,
    RaresTierSevenLogin = 256,

    [AssessmentProperty]
    ItemAttributeLimit = 257,

    [AssessmentProperty]
    ItemAttributeLevelLimit = 258,

    [AssessmentProperty]
    ItemAttribute2ndLimit = 259,

    [AssessmentProperty]
    ItemAttribute2ndLevelLimit = 260,

    [AssessmentProperty]
    CharacterTitleId = 261,

    [AssessmentProperty]
    NumCharacterTitles = 262,

    [AssessmentProperty]
    ResistanceModifierType = 263,
    FreeTinkersBitfield = 264,

    [AssessmentProperty]
    EquipmentSetId = 265,
    PetClass = 266,

    [AssessmentProperty]
    Lifespan = 267,

    [AssessmentProperty]
    [Ephemeral]
    RemainingLifespan = 268,
    UseCreateQuantity = 269,

    [AssessmentProperty]
    WieldRequirements2 = 270,

    [AssessmentProperty]
    WieldSkillType2 = 271,

    [AssessmentProperty]
    WieldDifficulty2 = 272,

    [AssessmentProperty]
    WieldRequirements3 = 273,

    [AssessmentProperty]
    WieldSkillType3 = 274,

    [AssessmentProperty]
    WieldDifficulty3 = 275,

    [AssessmentProperty]
    WieldRequirements4 = 276,

    [AssessmentProperty]
    WieldSkillType4 = 277,

    [AssessmentProperty]
    WieldDifficulty4 = 278,

    [AssessmentProperty]
    Unique = 279,

    [AssessmentProperty]
    SharedCooldown = 280,

    [AssessmentProperty]
    [SendOnLogin]
    Faction1Bits = 281,
    Faction2Bits = 282,
    Faction3Bits = 283,
    Hatred1Bits = 284,
    Hatred2Bits = 285,
    Hatred3Bits = 286,

    [AssessmentProperty]
    [SendOnLogin]
    SocietyRankCelhan = 287,

    [AssessmentProperty]
    [SendOnLogin]
    SocietyRankEldweb = 288,

    [AssessmentProperty]
    [SendOnLogin]
    SocietyRankRadblo = 289,
    HearLocalSignals = 290,
    HearLocalSignalsRadius = 291,

    [AssessmentProperty]
    Cleaving = 292,
    AugmentationSpecializeGearcraft = 293,

    [SendOnLogin]
    AugmentationInfusedCreatureMagic = 294,

    [SendOnLogin]
    AugmentationInfusedItemMagic = 295,

    [SendOnLogin]
    AugmentationInfusedLifeMagic = 296,

    [SendOnLogin]
    AugmentationInfusedWarMagic = 297,

    [SendOnLogin]
    AugmentationCriticalExpertise = 298,

    [SendOnLogin]
    AugmentationCriticalPower = 299,

    [SendOnLogin]
    AugmentationSkilledMelee = 300,

    [SendOnLogin]
    AugmentationSkilledMissile = 301,

    [SendOnLogin]
    AugmentationSkilledMagic = 302,

    [AssessmentProperty]
    ImbuedEffect2 = 303,

    [AssessmentProperty]
    ImbuedEffect3 = 304,

    [AssessmentProperty]
    ImbuedEffect4 = 305,

    [AssessmentProperty]
    ImbuedEffect5 = 306,

    [AssessmentProperty]
    [SendOnLogin]
    DamageRating = 307,

    [AssessmentProperty]
    [SendOnLogin]
    DamageResistRating = 308,

    [SendOnLogin]
    AugmentationDamageBonus = 309,

    [SendOnLogin]
    AugmentationDamageReduction = 310,
    ImbueStackingBits = 311,

    [SendOnLogin]
    HealOverTime = 312,

    [AssessmentProperty]
    [SendOnLogin]
    CritRating = 313,

    [AssessmentProperty]
    [SendOnLogin]
    CritDamageRating = 314,

    [AssessmentProperty]
    [SendOnLogin]
    CritResistRating = 315,

    [AssessmentProperty]
    [SendOnLogin]
    CritDamageResistRating = 316,

    [SendOnLogin]
    HealingResistRating = 317,

    [SendOnLogin]
    DamageOverTime = 318,

    [AssessmentProperty]
    ItemMaxLevel = 319,

    [AssessmentProperty]
    ItemXpStyle = 320,
    EquipmentSetExtra = 321,

    [SendOnLogin]
    AetheriaBitfield = 322,

    [AssessmentProperty]
    [SendOnLogin]
    HealingBoostRating = 323,

    [AssessmentProperty]
    HeritageSpecificArmor = 324,
    AlternateRacialSkills = 325,

    [SendOnLogin]
    AugmentationJackOfAllTrades = 326,
    AugmentationResistanceNether = 327,

    [SendOnLogin]
    AugmentationInfusedVoidMagic = 328,

    [SendOnLogin]
    WeaknessRating = 329,

    [SendOnLogin]
    NetherOverTime = 330,

    [SendOnLogin]
    NetherResistRating = 331,
    LuminanceAward = 332,

    [SendOnLogin]
    LumAugDamageRating = 333,

    [SendOnLogin]
    LumAugDamageReductionRating = 334,

    [SendOnLogin]
    LumAugCritDamageRating = 335,

    [SendOnLogin]
    LumAugCritReductionRating = 336,

    [SendOnLogin]
    LumAugSurgeEffectRating = 337,

    [SendOnLogin]
    LumAugSurgeChanceRating = 338,

    [SendOnLogin]
    LumAugItemManaUsage = 339,

    [SendOnLogin]
    LumAugItemManaGain = 340,

    [SendOnLogin]
    LumAugVitality = 341,

    [SendOnLogin]
    LumAugHealingRating = 342,

    [SendOnLogin]
    LumAugSkilledCraft = 343,

    [SendOnLogin]
    LumAugSkilledSpec = 344,
    LumAugNoDestroyCraft = 345,
    RestrictInteraction = 346,

    [SendOnLogin]
    OlthoiLootTimestamp = 347,
    OlthoiLootStep = 348,
    UseCreatesContractId = 349,

    [AssessmentProperty]
    [SendOnLogin]
    DotResistRating = 350,

    [AssessmentProperty]
    [SendOnLogin]
    LifeResistRating = 351,

    [AssessmentProperty]
    CloakWeaveProc = 352,

    [AssessmentProperty]
    WeaponType = 353,

    [SendOnLogin]
    MeleeMastery = 354,

    [SendOnLogin]
    RangedMastery = 355,
    SneakAttackRating = 356,
    RecklessnessRating = 357,
    DeceptionRating = 358,
    CombatPetRange = 359,

    [SendOnLogin]
    WeaponAuraDamage = 360,

    [SendOnLogin]
    WeaponAuraSpeed = 361,

    [SendOnLogin]
    SummoningMastery = 362,
    HeartbeatLifespan = 363,
    UseLevelRequirement = 364,

    [SendOnLogin]
    LumAugAllSkills = 365,

    [AssessmentProperty]
    UseRequiresSkill = 366,

    [AssessmentProperty]
    UseRequiresSkillLevel = 367,

    [AssessmentProperty]
    UseRequiresSkillSpec = 368,

    [AssessmentProperty]
    UseRequiresLevel = 369,

    [AssessmentProperty]
    [SendOnLogin]
    GearDamage = 370,

    [AssessmentProperty]
    [SendOnLogin]
    GearDamageResist = 371,

    [AssessmentProperty]
    [SendOnLogin]
    GearCrit = 372,

    [AssessmentProperty]
    [SendOnLogin]
    GearCritResist = 373,

    [AssessmentProperty]
    [SendOnLogin]
    GearCritDamage = 374,

    [AssessmentProperty]
    [SendOnLogin]
    GearCritDamageResist = 375,

    [AssessmentProperty]
    [SendOnLogin]
    GearHealingBoost = 376,

    [AssessmentProperty]
    [SendOnLogin]
    GearNetherResist = 377,

    [AssessmentProperty]
    [SendOnLogin]
    GearLifeResist = 378,

    [AssessmentProperty]
    [SendOnLogin]
    GearMaxHealth = 379,
    Unknown380 = 380,

    [AssessmentProperty]
    [SendOnLogin]
    PKDamageRating = 381,

    [AssessmentProperty]
    [SendOnLogin]
    PKDamageResistRating = 382,

    [AssessmentProperty]
    [SendOnLogin]
    GearPKDamageRating = 383,

    [AssessmentProperty]
    [SendOnLogin]
    GearPKDamageResistRating = 384,
    Unknown385 = 385,

    /// <summary>
    /// Overpower chance % for endgame creatures.
    /// </summary>
    [AssessmentProperty]
    [SendOnLogin]
    Overpower = 386,

    [AssessmentProperty]
    [SendOnLogin]
    OverpowerResist = 387,

    // Client does not display accurately
    [AssessmentProperty]
    [SendOnLogin]
    GearOverpower = 388,

    // Client does not display accurately
    [AssessmentProperty]
    [SendOnLogin]
    GearOverpowerResist = 389,

    // Number of times a character has enlightened
    [AssessmentProperty]
    [SendOnLogin]
    Enlightenment = 390,

    // Daralet

    [AssessmentProperty]
    WardLevel = 391,

    [AssessmentProperty]
    ArmorSlots = 392,

    [AssessmentProperty]
    ArmorWeightClass = 393,

    [AssessmentProperty]
    GearMaxStamina = 394,

    [AssessmentProperty]
    GearMaxMana = 395,

    [AssessmentProperty]
    CombatFocusTypeId = 396,

    [AssessmentProperty]
    WeightClassReqAmount = 397,

    [AssessmentProperty]
    ArmorStyle = 398,

    [AssessmentProperty]
    WeaponSubtype = 399,

    [AssessmentProperty]
    ArmorPatchAmount = 400,

    [AssessmentProperty]
    SigilTrinketColor = 401,
    SigilTrinketSkill = 402,
    SigilTrinketEffectId = 403,

    [AssessmentProperty]
    SigilTrinketMaxTier = 404,

    [AssessmentProperty]
    SigilTrinketElement = 405,

    [AssessmentProperty]
    SigilTrinketBonusStat = 406,

    [AssessmentProperty]
    SigilTrinketBonusStatAmount = 407,

    [AssessmentProperty]
    JewelSockets = 408,

    [AssessmentProperty]
    GearStrength = 409,

    [AssessmentProperty]
    GearEndurance = 410,

    [AssessmentProperty]
    GearCoordination = 411,

    [AssessmentProperty]
    GearQuickness = 412,

    [AssessmentProperty]
    GearFocus = 413,

    [AssessmentProperty]
    GearSelf = 414,

    [AssessmentProperty]
    GearLifesteal = 415,

    [AssessmentProperty]
    GearSelfHarm = 416,

    [AssessmentProperty]
    GearThreatGain = 417,

    [AssessmentProperty]
    GearThreatReduction = 418,

    [AssessmentProperty]
    GearElementalWard = 419,

    [AssessmentProperty]
    GearPhysicalWard = 420,

    [AssessmentProperty]
    GearMagicFind = 421,

    [AssessmentProperty]
    GearBlock = 422,

    [AssessmentProperty]
    GearItemManaUsage = 423,

    [AssessmentProperty]
    GearThorns = 424,

    [AssessmentProperty]
    GearVitalsTransfer = 425,

    [AssessmentProperty]
    GearRedFury = 426,

    [AssessmentProperty]
    GearSelflessness = 427,

    [AssessmentProperty]
    GearVipersStrike = 428,

    [AssessmentProperty]
    GearFamiliarity = 429,

    [AssessmentProperty]
    GearBravado = 430,

    [AssessmentProperty]
    GearHealthToStamina = 431,

    [AssessmentProperty]
    GearHealthToMana = 432,

    [AssessmentProperty]
    GearExperienceGain = 433,

    [AssessmentProperty]
    GearManasteal = 434,

    [AssessmentProperty]
    GearBludgeon = 435,

    [AssessmentProperty]
    GearPierce = 436,

    [AssessmentProperty]
    GearSlash = 437,

    [AssessmentProperty]
    GearFire = 438,

    [AssessmentProperty]
    GearFrost = 439,

    [AssessmentProperty]
    GearAcid = 440,

    [AssessmentProperty]
    GearLightning = 441,

    [AssessmentProperty]
    GearHealBubble = 442,

    [AssessmentProperty]
    GearCompBurn = 443,

    [AssessmentProperty]
    GearPyrealFind = 444,

    [AssessmentProperty]
    GearNullification = 445,

    [AssessmentProperty]
    GearWardPen = 446,

    [AssessmentProperty]
    GearStaminasteal = 447,

    [AssessmentProperty]
    GearHardenedDefense = 448,

    [AssessmentProperty]
    GearReprisal = 449,

    [AssessmentProperty]
    GearElementalist = 450,

    [AssessmentProperty]
    BaseArmor = 451,

    [AssessmentProperty]
    BaseDamage = 452,

    [AssessmentProperty]
    BaseWard = 453,

    [AssessmentProperty]
    BaseWeaponTime = 454,

    [AssessmentProperty]
    BaseMaxMana = 455,

    [AssessmentProperty]
    ItemSpellId = 456,

    [AssessmentProperty]
    CombatFocusAttributeSpellRemoved = 457,

    [AssessmentProperty]
    CombatFocusAttributeSpellAdded = 458,

    [AssessmentProperty]
    CombatFocusSkillSpellRemoved = 459,

    [AssessmentProperty]
    CombatFocusSkillSpellAdded = 460,

    [AssessmentProperty]
    StackableSpellType = 461,

    [AssessmentProperty]
    NearbyPlayerScalingThreshold = 462,

    [AssessmentProperty]
    NearbyPlayerScalingExtraPlayersPerAdd = 463,

    [AssessmentProperty]
    NearbyPlayerScalingAddWcid = 464,

    [AssessmentProperty]
    RemainingConfirmations = 465,

    [AssessmentProperty]
    SigilTrinketType = 466,

    [AssessmentProperty]
    TrophyQuality = 467,

    [AssessmentProperty]
    AmmoEffect = 468,

    [AssessmentProperty]
    AmmoEffectUsesRemaining = 469,

    [AssessmentProperty]
    AltCurrencyValue = 470,

    [AssessmentProperty]
    GearYellowFury = 471,

    [AssessmentProperty]
    GearBlueFury = 472,

    [AssessmentProperty]
    NoCompsRequiredForMagicSchool = 473,

    [AssessmentProperty]
    JewelSocket1Material = 474,

    [AssessmentProperty]
    JewelSocket1Quality = 475,

    [AssessmentProperty]
    JewelSocket2Material = 476,

    [AssessmentProperty]
    JewelSocket2Quality = 477,

    [AssessmentProperty]
    JewelSocket3Material = 478,

    [AssessmentProperty]
    JewelSocket3Quality = 479,

    [AssessmentProperty]
    JewelSocket4Material = 480,

    [AssessmentProperty]
    JewelSocket4Quality = 481,

    [AssessmentProperty]
    JewelSocket5Material = 482,

    [AssessmentProperty]
    JewelSocket5Quality = 483,

    [AssessmentProperty]
    JewelSocket6Material = 484,

    [AssessmentProperty]
    JewelSocket6Quality = 485,

    [AssessmentProperty]
    JewelSocket7Material = 486,

    [AssessmentProperty]
    JewelSocket7Quality = 487,

    [AssessmentProperty]
    JewelSocket8Material = 488,

    [AssessmentProperty]
    JewelSocket8Quality = 489,

    [AssessmentProperty]
    JewelSocket9Material = 490,

    [AssessmentProperty]
    JewelSocket9Quality = 491,

    [AssessmentProperty]
    JewelSocket10Material = 492,

    [AssessmentProperty]
    JewelSocket10Quality = 493,

    [AssessmentProperty]
    JewelMaterialType = 494,

    [AssessmentProperty]
    JewelQuality = 495,

    [AssessmentProperty]
    GearToughness = 496,

    [AssessmentProperty]
    GearResistance = 497,

    [AssessmentProperty]
    GearSlashBane = 498,

    [AssessmentProperty]
    GearBludgeonBane = 499,

    [AssessmentProperty]
    GearPierceBane = 500,

    [AssessmentProperty]
    GearAcidBane = 501,

    [AssessmentProperty]
    GearFireBane = 502,

    [AssessmentProperty]
    GearFrostBane = 503,

    [AssessmentProperty]
    GearLightningBane = 504,

    [AssessmentProperty]
    CombatFocusSkill2SpellRemoved = 505,

    [AssessmentProperty]
    CombatFocusSkill2SpellAdded = 506,

    [AssessmentProperty]
    CombatFocusNumSkillsRemoved = 507,

    [AssessmentProperty]
    CombatFocusNumSkillsAdded = 508,

    [AssessmentProperty]
    StaminaOverTime = 509,

    [AssessmentProperty]
    ManaOverTime = 510,

    [AssessmentProperty]
    MonsterRank = 511,

    [AssessmentProperty]
    CombatFocusSkill3SpellRemoved = 512,

    [AssessmentProperty]
    CombatFocusSkill3SpellAdded = 513,

    [AssessmentProperty]
    CombatFocusPrestigeVersionId = 514,
    MarketListingId = 515,

    [AssessmentProperty]
    TrophyEssenceSpellId = 516,

    [AssessmentProperty]
    TrophyEssenceSkill = 517,

    [AssessmentProperty]
    TrophyEssenceEffectType = 518,

    [AssessmentProperty]
    GearFrigidProtection = 519,
    ForgePassCount = 520,
    PassiveThreatThreshold = 521,
    TargetSpecificWcid = 522,
    WeaponRelicApplyCount = 523,

    /// <summary>
    /// The wcid of the armor piece an Armor Style Template's style was copied from.
    /// </summary>
    ArmorStyleTemplateWcid = 524,

    /// <summary>
    /// How many tinks were baked into a quest item when it mutated on pickup. Upgrade Kits strip and
    /// re-apply them when they retier the item.
    /// </summary>
    QuestItemTinks = 525,

    /// <summary>
    /// The character's arena rating (an Elo rating). A character that has none has the starting rating.
    /// </summary>
    ArenaRating = 526,
    ArenaWins = 527,
    ArenaLosses = 528,
    ArenaDraws = 529,

    /// <summary>
    /// The rating of the character's rated scaled duels, a board of its own (ArenaRating is for raw duels)
    /// </summary>
    ArenaScaledRating = 530,
    ArenaScaledWins = 531,
    ArenaScaledLosses = 532,
    ArenaScaledDraws = 533,
    PCAPRecordedAutonomousMovement = 8007,
    PCAPRecordedMaxVelocityEstimated = 8030,
    PCAPRecordedPlacement = 8041,
    PCAPRecordedAppraisalPages = 8042,
    PCAPRecordedAppraisalMaxPages = 8043,

    //[ServerOnly]
    //TotalLogins                              = 9001,
    //[ServerOnly]
    //DeletionTimestamp                        = 9002,
    //[ServerOnly]
    //CharacterOptions1                        = 9003,
    //[ServerOnly]
    //CharacterOptions2                        = 9004,
    //[ServerOnly]
    //LootTier                                 = 9005,
    //[ServerOnly]
    //GeneratorProbability                     = 9006,
    //[ServerOnly]
    //WeenieType                               = 9007 // I don't think this property type is needed anymore. We don't store the weenie type in the property bags, we store it as a separate field in the base objects.
    CurrentLoyaltyAtLastLogoff = 9008,
    CurrentLeadershipAtLastLogoff = 9009,
    AllegianceOfficerRank = 9010,
    HouseRentTimestamp = 9011,

    /// <summary>
    ///  Stores the player's selected hairstyle at creation or after a barber use. This is used only for Gear Knights and Olthoi characters who have more than a single part/texture for a "hairstyle" (BodyStyle)
    /// </summary>
    Hairstyle = 9012,

    /// <summary>
    /// Used to store the calculated Clothing Priority for use with armor reduced items and items like Over-Robes.
    /// </summary>
    [Ephemeral]
    VisualClothingPriority = 9013,
    SquelchGlobal = 9014,

    /// <summary>
    /// TODO: This is a place holder for future use. See PlacementPosition
    /// This is the sort order for items in a container
    /// </summary>
    InventoryOrder = 9015,
    CombatAbilityId = 10000,

    [AssessmentProperty]
    Tier = 10007,

    [AssessmentProperty]
    ResistPerception = 10008,
    EmptyId = 10009,
    VendorStockMaxAmount = 10010
}

public static class PropertyIntExtensions
{
    public static string GetDescription(this PropertyInt prop)
    {
        var description = prop.GetAttributeOfType<DescriptionAttribute>();
        return description?.Description ?? prop.ToString();
    }

    public static string GetValueEnumName(this PropertyInt property, int value)
    {
        switch (property)
        {
            case PropertyInt.ActivationResponse:
                return System.Enum.GetName(typeof(ActivationResponse), value);
            case PropertyInt.AetheriaBitfield:
                return System.Enum.GetName(typeof(AetheriaBitfield), value);
            case PropertyInt.AttackHeight:
                return System.Enum.GetName(typeof(AttackHeight), value);
            case PropertyInt.AttackType:
                return System.Enum.GetName(typeof(AttackType), value);
            case PropertyInt.Attuned:
                return System.Enum.GetName(typeof(AttunedStatus), value);
            case PropertyInt.AmmoType:
                return System.Enum.GetName(typeof(AmmoType), value);
            case PropertyInt.Bonded:
                return System.Enum.GetName(typeof(BondedStatus), value);
            case PropertyInt.ChannelsActive:
            case PropertyInt.ChannelsAllowed:
                return System.Enum.GetName(typeof(Channel), value);
            case PropertyInt.CombatMode:
                return System.Enum.GetName(typeof(CombatMode), value);
            case PropertyInt.DefaultCombatStyle:
            case PropertyInt.AiAllowedCombatStyle:
                return System.Enum.GetName(typeof(CombatStyle), value);
            case PropertyInt.CombatUse:
                return System.Enum.GetName(typeof(CombatUse), value);
            case PropertyInt.ClothingPriority:
                return System.Enum.GetName(typeof(CoverageMask), value);
            case PropertyInt.CreatureType:
            case PropertyInt.SlayerCreatureType:
            case PropertyInt.FoeType:
            case PropertyInt.FriendType:
                return System.Enum.GetName(typeof(CreatureType), value);
            case PropertyInt.DamageType:
            case PropertyInt.ResistanceModifierType:
                return System.Enum.GetName(typeof(DamageType), value);
            case PropertyInt.CurrentWieldedLocation:
            case PropertyInt.ValidLocations:
                return System.Enum.GetName(typeof(EquipMask), value);
            case PropertyInt.EquipmentSetId:
                return System.Enum.GetName(typeof(EquipmentSet), value);
            case PropertyInt.Gender:
                return System.Enum.GetName(typeof(Gender), value);
            case PropertyInt.GeneratorDestructionType:
            case PropertyInt.GeneratorEndDestructionType:
                return System.Enum.GetName(typeof(GeneratorDestruct), value);
            case PropertyInt.GeneratorTimeType:
                return System.Enum.GetName(typeof(GeneratorTimeType), value);
            case PropertyInt.GeneratorType:
                return System.Enum.GetName(typeof(GeneratorType), value);
            case PropertyInt.HeritageGroup:
            case PropertyInt.HeritageSpecificArmor:
                return System.Enum.GetName(typeof(HeritageGroup), value);
            case PropertyInt.HookType:
                return System.Enum.GetName(typeof(HookType), value);
            case PropertyInt.HouseType:
                return System.Enum.GetName(typeof(HouseType), value);
            case PropertyInt.ImbuedEffect:
            case PropertyInt.ImbuedEffect2:
            case PropertyInt.ImbuedEffect3:
            case PropertyInt.ImbuedEffect4:
            case PropertyInt.ImbuedEffect5:
                return System.Enum.GetName(typeof(ImbuedEffectType), value);
            case PropertyInt.HookItemType:
            case PropertyInt.ItemType:
            case PropertyInt.MerchandiseItemTypes:
            case PropertyInt.TargetType:
                return System.Enum.GetName(typeof(ItemType), value);
            case PropertyInt.ItemXpStyle:
                return System.Enum.GetName(typeof(ItemXpStyle), value);
            case PropertyInt.MaterialType:
                return System.Enum.GetName(typeof(MaterialType), value);
            case PropertyInt.PaletteTemplate:
                return System.Enum.GetName(typeof(PaletteTemplate), value);
            case PropertyInt.PhysicsState:
                return System.Enum.GetName(typeof(PhysicsState), value);
            case PropertyInt.HookPlacement:
            case PropertyInt.Placement:
            case PropertyInt.PCAPRecordedPlacement:
                return System.Enum.GetName(typeof(Placement), value);
            case PropertyInt.PortalBitmask:
                return System.Enum.GetName(typeof(PortalBitmask), value);
            case PropertyInt.PlayerKillerStatus:
                return System.Enum.GetName(typeof(PlayerKillerStatus), value);
            case PropertyInt.BoosterEnum:
                return System.Enum.GetName(typeof(PropertyAttribute2nd), value);
            case PropertyInt.ShowableOnRadar:
                return System.Enum.GetName(typeof(RadarBehavior), value);
            case PropertyInt.RadarBlipColor:
                return System.Enum.GetName(typeof(RadarColor), value);
            case PropertyInt.WeaponSkill:
            case PropertyInt.WieldSkillType:
            case PropertyInt.WieldSkillType2:
            case PropertyInt.WieldSkillType3:
            case PropertyInt.WieldSkillType4:
            case PropertyInt.AppraisalItemSkill:
                return System.Enum.GetName(typeof(Skill), value);
            case PropertyInt.AccountRequirements:
                return System.Enum.GetName(typeof(SubscriptionStatus), value);
            case PropertyInt.SummoningMastery:
                return System.Enum.GetName(typeof(SummoningMastery), value);
            case PropertyInt.UiEffects:
                return System.Enum.GetName(typeof(UiEffects), value);
            case PropertyInt.ItemUseable:
                return System.Enum.GetName(typeof(Usable), value);
            case PropertyInt.WeaponType:
                return System.Enum.GetName(typeof(WeaponType), value);
            case PropertyInt.WieldRequirements:
            case PropertyInt.WieldRequirements2:
            case PropertyInt.WieldRequirements3:
            case PropertyInt.WieldRequirements4:
                return System.Enum.GetName(typeof(WieldRequirement), value);

            case PropertyInt.GeneratorStartTime:
            case PropertyInt.GeneratorEndTime:
                return DateTimeOffset.FromUnixTimeSeconds(value).DateTime.ToString(CultureInfo.InvariantCulture);

            case PropertyInt.ArmorType:
                return System.Enum.GetName(typeof(ArmorType), value);
            case PropertyInt.ParentLocation:
                return System.Enum.GetName(typeof(ParentLocation), value);
            case PropertyInt.PlacementPosition:
                return System.Enum.GetName(typeof(Placement), value);
            case PropertyInt.HouseStatus:
                return System.Enum.GetName(typeof(HouseStatus), value);

            case PropertyInt.UseCreatesContractId:
                return System.Enum.GetName(typeof(ContractId), value);

            case PropertyInt.Faction1Bits:
            case PropertyInt.Faction2Bits:
            case PropertyInt.Faction3Bits:
            case PropertyInt.Hatred1Bits:
            case PropertyInt.Hatred2Bits:
            case PropertyInt.Hatred3Bits:
                return System.Enum.GetName(typeof(FactionBits), value);

            case PropertyInt.UseRequiresSkill:
            case PropertyInt.UseRequiresSkillSpec:
            case PropertyInt.SkillToBeAltered:
                return System.Enum.GetName(typeof(Skill), value);

            case PropertyInt.HookGroup:
                return System.Enum.GetName(typeof(HookGroupType), value);

            case PropertyInt.JewelMaterialType:
                return System.Enum.GetName(typeof(MaterialType), value);

            //case PropertyInt.TypeOfAlteration:
            //    return System.Enum.GetName(typeof(SkillAlterationType), value);
        }

        return null;
    }
}
