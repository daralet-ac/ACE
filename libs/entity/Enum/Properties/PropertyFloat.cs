using System.ComponentModel;

namespace ACE.Entity.Enum.Properties;

public enum PropertyFloat : ushort
{
    // No properties are sent to the client unless they feature an attribute.
    // SendOnLogin gets sent to players in the PlayerDescription event
    // AssessmentProperty gets sent in successful appraisal
    // description attributes are used by the weenie editor for a cleaner display name

    Undef = 0,
    HeartbeatInterval = 1,

    [Ephemeral]
    HeartbeatTimestamp = 2,
    HealthRate = 3,
    StaminaRate = 4,

    [AssessmentProperty]
    ManaRate = 5,
    HealthUponResurrection = 6,
    StaminaUponResurrection = 7,
    ManaUponResurrection = 8,
    StartTime = 9,
    StopTime = 10,
    ResetInterval = 11,
    Shade = 12,
    ArmorModVsSlash = 13,
    ArmorModVsPierce = 14,
    ArmorModVsBludgeon = 15,
    ArmorModVsCold = 16,
    ArmorModVsFire = 17,
    ArmorModVsAcid = 18,
    ArmorModVsElectric = 19,
    CombatSpeed = 20,
    WeaponLength = 21,
    DamageVariance = 22,
    CurrentPowerMod = 23,
    AccuracyMod = 24,
    StrengthMod = 25,
    MaximumVelocity = 26,
    RotationSpeed = 27,
    MotionTimestamp = 28,

    [AssessmentProperty]
    WeaponDefense = 29,
    WimpyLevel = 30,
    VisualAwarenessRange = 31,
    AuralAwarenessRange = 32,
    PerceptionLevel = 33,
    PowerupTime = 34,
    MaxChargeDistance = 35,
    ChargeSpeed = 36,
    BuyPrice = 37,
    SellPrice = 38,
    DefaultScale = 39,
    LockpickMod = 40,
    RegenerationInterval = 41,
    RegenerationTimestamp = 42,
    GeneratorRadius = 43,
    TimeToRot = 44,
    DeathTimestamp = 45,
    PkTimestamp = 46,
    VictimTimestamp = 47,
    LoginTimestamp = 48,
    CreationTimestamp = 49,
    MinimumTimeSincePk = 50,
    DeprecatedHousekeepingPriority = 51,
    AbuseLoggingTimestamp = 52,

    [Ephemeral]
    LastPortalTeleportTimestamp = 53,
    UseRadius = 54,
    HomeRadius = 55,
    ReleasedTimestamp = 56,
    MinHomeRadius = 57,
    Facing = 58,

    [Ephemeral]
    ResetTimestamp = 59,
    LogoffTimestamp = 60,
    EconRecoveryInterval = 61,

    // not sent in retail appraisals, but read by this server's custom appraisal text (AppraiseInfo)
    [AssessmentProperty]
    WeaponOffense = 62,
    DamageMod = 63,
    ResistSlash = 64,
    ResistPierce = 65,
    ResistBludgeon = 66,
    ResistFire = 67,
    ResistCold = 68,
    ResistAcid = 69,
    ResistElectric = 70,
    ResistHealthBoost = 71,
    ResistStaminaDrain = 72,
    ResistStaminaBoost = 73,
    ResistManaDrain = 74,
    ResistManaBoost = 75,
    Translucency = 76,
    PhysicsScriptIntensity = 77,
    Friction = 78,
    Elasticity = 79,
    AiUseMagicDelay = 80,
    ItemMinSpellcraftMod = 81,
    ItemMaxSpellcraftMod = 82,
    ItemRankProbability = 83,
    Shade2 = 84,
    Shade3 = 85,
    Shade4 = 86,

    [AssessmentProperty]
    ItemEfficiency = 87,
    ItemManaUpdateTimestamp = 88,
    SpellGestureSpeedMod = 89,
    SpellStanceSpeedMod = 90,
    AllegianceAppraisalTimestamp = 91,
    PowerLevel = 92,
    AccuracyLevel = 93,
    AttackAngle = 94,
    AttackTimestamp = 95,
    CheckpointTimestamp = 96,
    SoldTimestamp = 97,
    UseTimestamp = 98,

    [Ephemeral]
    UseLockTimestamp = 99,

    [AssessmentProperty]
    HealkitMod = 100,
    FrozenTimestamp = 101,
    HealthRateMod = 102,
    AllegianceSwearTimestamp = 103,
    ObviousRadarRange = 104,
    HotspotCycleTime = 105,
    HotspotCycleTimeVariance = 106,
    SpamTimestamp = 107,
    SpamRate = 108,
    BondWieldedTreasure = 109,
    BulkMod = 110,
    SizeMod = 111,
    GagTimestamp = 112,
    GeneratorUpdateTimestamp = 113,
    DeathSpamTimestamp = 114,
    DeathSpamRate = 115,
    WildAttackProbability = 116,
    FocusedProbability = 117,
    CrashAndTurnProbability = 118,
    CrashAndTurnRadius = 119,
    CrashAndTurnBias = 120,
    GeneratorInitialDelay = 121,
    AiAcquireHealth = 122,
    AiAcquireStamina = 123,
    AiAcquireMana = 124,

    /// <summary>
    /// this had a default of "1" - leaving comment to investigate potential options for defaulting these things (125)
    /// </summary>
    [SendOnLogin]
    ResistHealthDrain = 125,
    LifestoneProtectionTimestamp = 126,
    AiCounteractEnchantment = 127,
    AiDispelEnchantment = 128,
    TradeTimestamp = 129,
    AiTargetedDetectionRadius = 130,
    EmotePriority = 131,

    [Ephemeral]
    LastTeleportStartTimestamp = 132,
    EventSpamTimestamp = 133,
    EventSpamRate = 134,
    InventoryOffset = 135,

    [AssessmentProperty]
    CriticalMultiplier = 136,

    [AssessmentProperty]
    ManaStoneDestroyChance = 137,

    // not sent in retail appraisals, but read by this server's custom appraisal text (AppraiseInfo)
    [AssessmentProperty]
    SlayerDamageBonus = 138,
    AllegianceInfoSpamTimestamp = 139,
    AllegianceInfoSpamRate = 140,
    NextSpellcastTimestamp = 141,

    [Ephemeral]
    AppraisalRequestedTimestamp = 142,
    AppraisalHeartbeatDueTimestamp = 143,

    [AssessmentProperty]
    ManaConversionMod = 144,
    LastPkAttackTimestamp = 145,
    FellowshipUpdateTimestamp = 146,

    [AssessmentProperty]
    CriticalFrequency = 147,
    LimboStartTimestamp = 148,

    [AssessmentProperty]
    WeaponMissileDefense = 149,

    [AssessmentProperty]
    WeaponMagicDefense = 150,
    IgnoreShield = 151,

    [AssessmentProperty]
    ElementalDamageMod = 152,
    StartMissileAttackTimestamp = 153,
    LastRareUsedTimestamp = 154,

    [AssessmentProperty]
    IgnoreArmor = 155,

    // not sent in retail appraisals, but read by this server's custom appraisal text (AppraiseInfo)
    [AssessmentProperty]
    ProcSpellRate = 156,

    [AssessmentProperty]
    ResistanceModifier = 157,
    AllegianceGagTimestamp = 158,

    [AssessmentProperty]
    AbsorbMagicDamage = 159,
    CachedMaxAbsorbMagicDamage = 160,
    GagDuration = 161,
    AllegianceGagDuration = 162,

    [SendOnLogin]
    GlobalXpMod = 163,
    HealingModifier = 164,
    ArmorModVsNether = 165,
    ResistNether = 166,

    [AssessmentProperty]
    CooldownDuration = 167,

    [SendOnLogin]
    WeaponAuraOffense = 168,

    [SendOnLogin]
    WeaponAuraDefense = 169,

    [SendOnLogin]
    WeaponAuraElemental = 170,

    [SendOnLogin]
    WeaponAuraManaConv = 171,

    //Timeline

    [AssessmentProperty]
    LootQualityMod = 172,

    [AssessmentProperty]
    KillXpMod = 173,

    [AssessmentProperty]
    ArchetypeToughness = 174,

    [AssessmentProperty]
    ArchetypePhysicality = 175,

    [AssessmentProperty]
    ArchetypeDexterity = 176,

    [AssessmentProperty]
    ArchetypeMagic = 177,

    [AssessmentProperty]
    ArchetypeIntelligence = 178,

    [AssessmentProperty]
    ArchetypeLethality = 179,

    [AssessmentProperty]
    BossKillXpMonsterMax = 180,

    [AssessmentProperty]
    BossKillXpPlayerMax = 181,

    [AssessmentProperty]
    SigilTrinketTriggerChance = 182,

    [AssessmentProperty]
    SigilTrinketCooldown = 183,

    [AssessmentProperty]
    SigilTrinketManaReserved = 184,

    [AssessmentProperty]
    SigilTrinketIntensity = 185,

    [AssessmentProperty]
    SigilTrinketReductionAmount = 186,

    [AssessmentProperty]
    WeaponPhysicalDefense = 187,

    [AssessmentProperty]
    WeaponMagicalDefense = 188,

    [AssessmentProperty]
    Damage = 189,

    [AssessmentProperty]
    WeaponAuraDamage = 190,

    [AssessmentProperty]
    StaminaCostReductionMod = 191,

    [AssessmentProperty]
    RankContribution = 192,

    [AssessmentProperty]
    SworeAllegiance = 193,

    [AssessmentProperty]
    NearbyPlayerVitalsScalingPerExtraPlayer = 194,

    [AssessmentProperty]
    NearbyPlayerAttackScalingPerExtraPlayer = 195,

    [AssessmentProperty]
    NearbyPlayerDefenseScalingPerExtraPlayer = 196,

    [AssessmentProperty]
    SigilTrinketStaminaReserved = 197,

    [AssessmentProperty]
    SigilTrinketHealthReserved = 198,

    [AssessmentProperty]
    ResistBleed = 199,

    [AssessmentProperty]
    ArchetypeSpellDamageMultiplier = 200,
    PatrolScanInterval = 201,
    PatrolPauseMinSeconds = 202,
    PatrolPauseMaxSeconds = 203,
    SpecializedPackBurdenMod = 204,

    [AssessmentProperty]
    GearFrigidProtectionMod = 205,
    DestabVarPercent = 205,
    HomesickGracePeriod = 206,
    BonusHealthRegenPerTick = 207,

    /// <summary>
    /// Seconds to pause a monster's AI. Set it (e.g. with the SetMyFloatStat emote) and the monster stops
    /// targeting, moving and attacking for that long - e.g. a boss winding up a big hit. Read once, then cleared.
    /// </summary>
    AiPauseDuration = 208,
    PCAPRecordedWorkmanship = 8004,
    PCAPRecordedVelocityX = 8010,
    PCAPRecordedVelocityY = 8011,
    PCAPRecordedVelocityZ = 8012,
    PCAPRecordedAccelerationX = 8013,
    PCAPRecordedAccelerationY = 8014,
    PCAPRecordedAccelerationZ = 8015,
    PCAPRecordeOmegaX = 8016,
    PCAPRecordeOmegaY = 8017,
    PCAPRecordeOmegaZ = 8018,

    [AssessmentProperty]
    HotspotImmunityTimestamp = 10002,
    VendorRestockInterval = 10006,
    VendorStockTimeToRot = 10007,

    // Timeline

    [AssessmentProperty]
    IgnoreWard = 20000,

    [AssessmentProperty]
    ArmorWarMagicMod = 20001,

    [AssessmentProperty]
    ArmorLifeMagicMod = 20002,

    [AssessmentProperty]
    ArmorMagicDefMod = 20003,

    [AssessmentProperty]
    ArmorPhysicalDefMod = 20004,

    [AssessmentProperty]
    ArmorMissileDefMod = 20005,

    [AssessmentProperty]
    ArmorDualWieldMod = 20006,

    [AssessmentProperty]
    ArmorRunMod = 20007,

    [AssessmentProperty]
    ArmorAttackMod = 20008,

    [AssessmentProperty]
    ArmorHealthRegenMod = 20009,

    [AssessmentProperty]
    ArmorStaminaRegenMod = 20010,

    [AssessmentProperty]
    ArmorManaRegenMod = 20011,

    [AssessmentProperty]
    ArmorShieldMod = 20012,

    [AssessmentProperty]
    ArmorPerceptionMod = 20013,

    [AssessmentProperty]
    ArmorThieveryMod = 20014,

    [AssessmentProperty]
    WeaponWarMagicMod = 20015,

    [AssessmentProperty]
    WeaponLifeMagicMod = 20016,

    [AssessmentProperty]
    WeaponRestorationSpellsMod = 20017,

    [AssessmentProperty]
    ArmorHealthMod = 20018,

    [AssessmentProperty]
    ArmorStaminaMod = 20019,

    [AssessmentProperty]
    ArmorManaMod = 20020,

    [AssessmentProperty]
    ArmorResourcePenalty = 20021,

    [AssessmentProperty]
    ArmorDeceptionMod = 20022,

    [AssessmentProperty]
    ArmorTwohandedCombatMod = 20023,

    [AssessmentProperty]
    BaseArmorWarMagicMod = 20024,

    [AssessmentProperty]
    BaseArmorLifeMagicMod = 20025,

    [AssessmentProperty]
    BaseArmorMagicDefMod = 20026,

    [AssessmentProperty]
    BaseArmorPhysicalDefMod = 20027,

    [AssessmentProperty]
    BaseArmorMissileDefMod = 20028,

    [AssessmentProperty]
    BaseArmorDualWieldMod = 20029,

    [AssessmentProperty]
    BaseArmorRunMod = 20030,

    [AssessmentProperty]
    BaseArmorAttackMod = 20031,

    [AssessmentProperty]
    BaseArmorHealthRegenMod = 20032,

    [AssessmentProperty]
    BaseArmorStaminaRegenMod = 20033,

    [AssessmentProperty]
    BaseArmorManaRegenMod = 20034,

    [AssessmentProperty]
    BaseArmorShieldMod = 20035,

    [AssessmentProperty]
    BaseArmorPerceptionMod = 20036,

    [AssessmentProperty]
    BaseArmorThieveryMod = 20037,

    [AssessmentProperty]
    BaseWeaponWarMagicMod = 20038,

    [AssessmentProperty]
    BaseWeaponLifeMagicMod = 20039,

    [AssessmentProperty]
    BaseWeaponRestorationSpellsMod = 20040,

    [AssessmentProperty]
    BaseArmorHealthMod = 20041,

    [AssessmentProperty]
    BaseArmorStaminaMod = 20042,

    [AssessmentProperty]
    BaseArmorManaMod = 20043,

    [AssessmentProperty]
    BaseArmorResourcePenalty = 20044,

    [AssessmentProperty]
    BaseArmorDeceptionMod = 20045,

    [AssessmentProperty]
    BaseArmorTwohandedCombatMod = 20046,

    [AssessmentProperty]
    BaseWeaponPhysicalDefense = 20047,

    [AssessmentProperty]
    BaseWeaponMagicalDefense = 20048,

    [AssessmentProperty]
    BaseWeaponOffense = 20049,

    [AssessmentProperty]
    BaseDamageMod = 20050,

    [AssessmentProperty]
    BaseElementalDamageMod = 20051,

    [AssessmentProperty]
    BaseManaConversionMod = 20052,

    /// <summary>
    /// Where a quest item's whole-number main stat (Damage or Armor Level) rolled within its tier's range, 0 to 1,
    /// so Upgrade Kits can roll it the same at the new tier instead of reading it back from a rounded value.
    /// </summary>
    QuestItemRollQuality = 20053,

    /// <summary>
    /// The same for a quest item's Ward Level, which rolls separately from its Armor Level.
    /// </summary>
    QuestItemWardRollQuality = 20054,
}

public static class PropertyFloatExtensions
{
    public static string GetDescription(this PropertyFloat prop)
    {
        var description = prop.GetAttributeOfType<DescriptionAttribute>();
        return description?.Description ?? prop.ToString();
    }
}
