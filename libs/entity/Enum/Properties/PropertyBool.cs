using System.ComponentModel;

namespace ACE.Entity.Enum.Properties;

public enum PropertyBool : ushort
{
    // No properties are sent to the client unless they feature an attribute.
    // SendOnLogin gets sent to players in the PlayerDescription event
    // AssessmentProperty gets sent in successful appraisal
    // description attributes are used by the weenie editor for a cleaner display name

    Undef = 0,

    [Ephemeral]
    Stuck = 1,

    [AssessmentProperty]
    [Ephemeral]
    Open = 2,

    [AssessmentProperty]
    Locked = 3,
    RotProof = 4,
    AllegianceUpdateRequest = 5,
    AiUsesMana = 6,
    AiUseHumanMagicAnimations = 7,
    AllowGive = 8,
    CurrentlyAttacking = 9,
    AttackerAi = 10,
    IgnoreCollisions = 11,
    ReportCollisions = 12,
    Ethereal = 13,
    GravityStatus = 14,
    LightsStatus = 15,
    ScriptedCollision = 16,
    Inelastic = 17,

    [Ephemeral]
    Visibility = 18,
    Attackable = 19,
    SafeSpellComponents = 20,

    [SendOnLogin]
    AdvocateState = 21,
    Inscribable = 22,
    DestroyOnSell = 23,
    UiHidden = 24,
    IgnoreHouseBarriers = 25,
    HiddenAdmin = 26,
    PkWounder = 27,
    PkKiller = 28,
    NoCorpse = 29,
    UnderLifestoneProtection = 30,
    ItemManaUpdatePending = 31,

    [Ephemeral]
    GeneratorStatus = 32,

    [Ephemeral]
    ResetMessagePending = 33,
    DefaultOpen = 34,
    DefaultLocked = 35,
    DefaultOn = 36,
    OpenForBusiness = 37,
    IsFrozen = 38,
    DealMagicalItems = 39,
    LogoffImDead = 40,
    ReportCollisionsAsEnvironment = 41,
    AllowEdgeSlide = 42,
    AdvocateQuest = 43,

    [Ephemeral]
    [SendOnLogin]
    IsAdmin = 44,

    [Ephemeral]
    [SendOnLogin]
    IsArch = 45,

    [Ephemeral]
    [SendOnLogin]
    IsSentinel = 46,

    [SendOnLogin]
    IsAdvocate = 47,
    CurrentlyPoweringUp = 48,

    [Ephemeral]
    GeneratorEnteredWorld = 49,
    NeverFailCasting = 50,
    VendorService = 51,
    AiImmobile = 52,
    DamagedByCollisions = 53,
    IsDynamic = 54,
    IsHot = 55,
    IsAffecting = 56,
    AffectsAis = 57,
    SpellQueueActive = 58,

    [Ephemeral]
    GeneratorDisabled = 59,
    IsAcceptingTells = 60,
    LoggingChannel = 61,
    OpensAnyLock = 62,

    [AssessmentProperty]
    UnlimitedUse = 63,
    GeneratedTreasureItem = 64,
    IgnoreMagicResist = 65,
    IgnoreMagicArmor = 66,
    AiAllowTrade = 67,

    [SendOnLogin]
    SpellComponentsRequired = 68,

    [AssessmentProperty]
    IsSellable = 69,
    IgnoreShieldsBySkill = 70,
    NoDraw = 71,
    ActivationUntargeted = 72,
    HouseHasGottenPriorityBootPos = 73,

    [Ephemeral]
    GeneratorAutomaticDestruction = 74,
    HouseHooksVisible = 75,
    HouseRequiresMonarch = 76,
    HouseHooksEnabled = 77,
    HouseNotifiedHudOfHookCount = 78,
    AiAcceptEverything = 79,
    IgnorePortalRestrictions = 80,
    RequiresBackpackSlot = 81,
    DontTurnOrMoveWhenGiving = 82,
    NpcLooksLikeObject = 83,
    IgnoreCloIcons = 84,

    [AssessmentProperty]
    AppraisalHasAllowedWielder = 85,
    ChestRegenOnClose = 86,
    LogoffInMinigame = 87,
    PortalShowDestination = 88,
    PortalIgnoresPkAttackTimer = 89,
    NpcInteractsSilently = 90,

    [AssessmentProperty]
    Retained = 91,
    IgnoreAuthor = 92,
    Limbo = 93,

    [AssessmentProperty]
    AppraisalHasAllowedActivator = 94,
    ExistedBeforeAllegianceXpChanges = 95,
    IsDeaf = 96,

    [Ephemeral]
    [SendOnLogin]
    IsPsr = 97,
    Invincible = 98,

    [AssessmentProperty]
    Ivoryable = 99,

    [AssessmentProperty]
    Dyable = 100,
    CanGenerateRare = 101,
    CorpseGeneratedRare = 102,
    NonProjectileMagicImmune = 103,

    [SendOnLogin]
    ActdReceivedItems = 104,
    Unknown105 = 105,

    [Ephemeral]
    FirstEnterWorldDone = 106,
    RecallsDisabled = 107,

    [AssessmentProperty]
    RareUsesTimer = 108,
    ActdPreorderReceivedItems = 109,

    [Ephemeral]
    Afk = 110,
    IsGagged = 111,
    ProcSpellSelfTargeted = 112,
    IsAllegianceGagged = 113,
    EquipmentSetTriggerPiece = 114,
    Uninscribe = 115,
    WieldOnUse = 116,
    ChestClearedWhenClosed = 117,
    NeverAttack = 118,
    SuppressGenerateEffect = 119,
    TreasureCorpse = 120,
    EquipmentSetAddLevel = 121,
    BarberActive = 122,
    TopLayerPriority = 123,

    [SendOnLogin]
    NoHeldItemShown = 124,

    [SendOnLogin]
    LoginAtLifestone = 125,
    OlthoiPk = 126,

    [SendOnLogin]
    Account15Days = 127,
    HadNoVitae = 128,
    NoOlthoiTalk = 129,

    [AssessmentProperty]
    AutowieldLeft = 130,

    // Timeline

    [AssessmentProperty]
    UseArchetypeSystem = 131,

    [AssessmentProperty]
    OverrideArchetypeXp = 132,

    [AssessmentProperty]
    OverrideArchetypeHealth = 133,

    [AssessmentProperty]
    OverrideArchetypeStamina = 134,

    [AssessmentProperty]
    OverrideArchetypeMana = 135,

    [AssessmentProperty]
    OverrideArchetypeSkills = 136,

    [AssessmentProperty]
    BossKillXpReward = 137,

    [AssessmentProperty]
    ArmorPatchApplied = 138,

    [AssessmentProperty]
    UseLegacyThreatSystem = 139,

    [AssessmentProperty]
    OverrideVisualRange = 140,

    [AssessmentProperty]
    AffectsOnlyAis = 141,

    [AssessmentProperty]
    ExamineItemsSilently = 142, // allows for no/custom message upon NPC Emote Refuse examination of items

    [AssessmentProperty]
    TakeItemsSilently = 143, // allows for no/custom messages for NPC TakeItems emote

    [AssessmentProperty]
    DungeonLockout = 144, // if object is on landblock, no new players will be added to permitted list

    [AssessmentProperty]
    CannotBreakStealth = 145,

    [AssessmentProperty]
    CampfireHotspot = 146,

    [AssessmentProperty]
    MutableQuestItem = 147,

    [AssessmentProperty]
    StruckByUnshrouded = 148,

    [AssessmentProperty]
    MenhirManaHotspot = 149,

    [AssessmentProperty]
    UseNearbyPlayerScaling = 150,

    [AssessmentProperty]
    IsBankContainer = 151,

    [AssessmentProperty]
    ShroudKillXpReward = 152,

    [AssessmentProperty]
    IsPlayerTierChest = 153,

    [AssessmentProperty]
    UpgradeableQuestItem = 154,

    [AssessmentProperty]
    FellowshipRequired = 155,

    [AssessmentProperty]
    SpecialPropertiesRequireMana = 156,

    [AssessmentProperty]
    RepeatConfirmation = 157,

    [AssessmentProperty]
    SilentCombat = 158,

    [AssessmentProperty]
    ReturnHomeWhenStuck = 159,

    [AssessmentProperty]
    ResetFromHotspot = 160,

    [AssessmentProperty]
    JewelAlternateEffect = 161,

    [AssessmentProperty]
    JewelSocket1AlternateEffect = 162,

    [AssessmentProperty]
    JewelSocket2AlternateEffect = 163,

    [AssessmentProperty]
    JewelSocket3AlternateEffect = 164,

    [AssessmentProperty]
    JewelSocket4AlternateEffect = 165,

    [AssessmentProperty]
    JewelSocket5AlternateEffect = 166,

    [AssessmentProperty]
    JewelSocket6AlternateEffect = 167,

    [AssessmentProperty]
    JewelSocket7AlternateEffect = 168,

    [AssessmentProperty]
    JewelSocket8AlternateEffect = 169,

    [AssessmentProperty]
    JewelSocket9AlternateEffect = 170,

    [AssessmentProperty]
    JewelSocket10AlternateEffect = 171,

    [AssessmentProperty]
    NoRotCorpse = 172,

    [AssessmentProperty]
    CreatureArmorEffectsDamageReduction = 173,
    PatrolEnabled = 174,
    PatrolForceWalk = 175,
    SignalCrossLB = 176,
    UnstableLoot = 177,
    IsUnstable = 178,
    TerminalDestabilizedLock = 179,
    GeneratesPassiveThreat = 180,
    AttuneOnEquip = 181,
    RequiresShrouded = 182,

    /// <summary>
    /// When true, this monster does NOT restore its vitals to max when it gives up on its target
    /// and returns home (homesick). Unset/false preserves the stock full-heal behavior.
    /// </summary>
    NoHomesickHeal = 183,

    /// <summary>
    /// When true, all damage this creature receives is reduced to 0. Unlike Invincible, attacks
    /// still land (and trigger on-hit effects / emotes) - they just deal no damage.
    /// </summary>
    Invulnerable = 184,

    /// <summary>
    /// The character has been taken off the arena rankings by an admin: /arena top does not list them. Their ratings and records are kept.
    /// </summary>
    ArenaRankingExcluded = 185,

    /// <summary>
    /// The character is watching a duel in the arena, unseen. Set while they are, so that a character saved that way
    /// (the server went down while they watched) is made visible again when they log in.
    /// </summary>
    ArenaSpectating = 186,

    /* custom */
    LinkedPortalOneSummon = 9001,
    LinkedPortalTwoSummon = 9002,
    HouseEvicted = 9003,
    UntrainedSkills = 9004,

    [Ephemeral]
    IsEnvoy = 9005,
    UnspecializedSkills = 9006,
    FreeSkillResetRenewed = 9007,
    FreeAttributeResetRenewed = 9008,
    SkillTemplesTimerReset = 9009,
    FreeMasteryResetRenewed = 9010,

    [Ephemeral]
    IsPseudoRandomGenerator = 9011,
    IsModified = 9012,
    VendorSellsSalvage = 9013,
    VendorSellsSpecialItems = 9014,

    /// <summary>
    /// Opt-in for Hotspot.OnCollideObject's custom PhysicsObj.is_touching() check: treats this
    /// object's placement position as the CENTER of its CylSphere collision height rather than
    /// the base, shifting the whole collision volume down by Height/2. Unset/false preserves the
    /// stock base-anchored behavior for every other object.
    /// </summary>
    HotspotCollidesFromCenter = 9015,
    AccountAttuned = 9016,
}

public static class PropertyBoolExtensions
{
    public static string GetDescription(this PropertyBool prop)
    {
        var description = prop.GetAttributeOfType<DescriptionAttribute>();
        return description?.Description ?? prop.ToString();
    }
}
