namespace ACE.Entity.Enum;

/// <summary>
/// exported from the decompiled client.  actual usage of these is 100% speculative.
/// </summary>
public enum EmoteType
{
    Invalid = 0,
    InvalidVendor = 0,
    Act = 1,
    AwardXP = 2,
    Give = 3,
    MoveHome = 4,
    Motion = 5,
    Move = 6,
    PhysScript = 7,
    Say = 8,
    Sound = 9,
    Tell = 10,
    Turn = 11,
    TurnToTarget = 12,
    TextDirect = 13,
    CastSpell = 14,
    Activate = 15,
    WorldBroadcast = 16,
    LocalBroadcast = 17,
    DirectBroadcast = 18,
    CastSpellInstant = 19,
    UpdateQuest = 20,
    InqQuest = 21,
    StampQuest = 22,
    StartEvent = 23,
    StopEvent = 24,
    BLog = 25,
    AdminSpam = 26,
    TeachSpell = 27,
    AwardSkillXP = 28,
    AwardSkillPoints = 29,
    InqQuestSolves = 30,
    EraseQuest = 31,
    DecrementQuest = 32,
    IncrementQuest = 33,
    AddCharacterTitle = 34,
    InqBoolStat = 35,
    InqIntStat = 36,
    InqFloatStat = 37,
    InqStringStat = 38,
    InqAttributeStat = 39,
    InqRawAttributeStat = 40,
    InqSecondaryAttributeStat = 41,
    InqRawSecondaryAttributeStat = 42,
    InqSkillStat = 43,
    InqRawSkillStat = 44,
    InqSkillTrained = 45,
    InqSkillSpecialized = 46,
    AwardTrainingCredits = 47,
    InflictVitaePenalty = 48,
    AwardLevelProportionalXP = 49,
    AwardLevelProportionalSkillXP = 50,
    InqEvent = 51,
    ForceMotion = 52,
    SetIntStat = 53,
    IncrementIntStat = 54,
    DecrementIntStat = 55,
    CreateTreasure = 56,
    ResetHomePosition = 57,
    InqFellowQuest = 58,
    InqFellowNum = 59,
    UpdateFellowQuest = 60,
    StampFellowQuest = 61,
    AwardNoShareXP = 62,
    SetSanctuaryPosition = 63,
    TellFellow = 64,
    FellowBroadcast = 65,
    LockFellow = 66,
    Goto = 67,
    PopUp = 68,
    SetBoolStat = 69,
    SetQuestCompletions = 70,
    InqNumCharacterTitles = 71,
    Generate = 72,
    PetCastSpellOnOwner = 73,
    TakeItems = 74,
    InqYesNo = 75,
    InqOwnsItems = 76,
    DeleteSelf = 77,
    KillSelf = 78,
    UpdateMyQuest = 79,
    InqMyQuest = 80,
    StampMyQuest = 81,
    InqMyQuestSolves = 82,
    EraseMyQuest = 83,
    DecrementMyQuest = 84,
    IncrementMyQuest = 85,
    SetMyQuestCompletions = 86,
    MoveToPos = 87,
    LocalSignal = 88,
    InqPackSpace = 89,
    RemoveVitaePenalty = 90,
    SetEyeTexture = 91,
    SetEyePalette = 92,
    SetNoseTexture = 93,
    SetNosePalette = 94,
    SetMouthTexture = 95,
    SetMouthPalette = 96,
    SetHeadObject = 97,
    SetHeadPalette = 98,
    TeleportTarget = 99,
    TeleportSelf = 100,
    StartBarber = 101,
    InqQuestBitsOn = 102,
    InqQuestBitsOff = 103,
    InqMyQuestBitsOn = 104,
    InqMyQuestBitsOff = 105,
    SetQuestBitsOn = 106,
    SetQuestBitsOff = 107,
    SetMyQuestBitsOn = 108,
    SetMyQuestBitsOff = 109,
    UntrainSkill = 110,
    SetAltRacialSkills = 111,
    SpendLuminance = 112,
    AwardLuminance = 113,
    InqInt64Stat = 114,
    SetInt64Stat = 115,
    OpenMe = 116,
    CloseMe = 117,
    SetFloatStat = 118,
    AddContract = 119,
    RemoveContract = 120,
    InqContractsFull = 121,

    // Unknown Id Emotes & Custom Emotes
    Enlightenment = 9001,
    VendorBroadcastStockLocal = 9002,
    VendorBroadcastStockWorld = 9003,

    // Timeline
    TrainSkill = 10000,
    InqFellowQuestSolves = 10001,
    EraseFellowQuest = 10002,
    SetLBEnviron = 10003,
    RelieveVitaePenalty = 10004,
    SetMyIntStat = 10005,
    SetMyBoolStat = 10006,
    SetMyFloatStat = 10007,
    AwardSkillRanks = 10008,
    CapstoneCacheReward = 10009,
    AssignCapstoneDungeon = 10010,
    AwardNoContribSkillXP = 10011,
    BroadcastSpellStacks = 10012,
    RemoveEnchantment = 10013,
    StampQuestForAllFellows = 10014,
    AdjustServerPropertyLong = 10015,
    InqServerPropertyLong = 10016,
    ResetAttributeXp = 10017,
    ResetVitalXp = 10018,
    CreateSigilTrinket = 10019,

    /// <summary>
    /// Heals the emote's own WorldObject (must be a Creature) by an amount.
    /// Amount = fixed heal amount. Min/Max = inclusive random range (overrides Amount when both are set).
    /// </summary>
    HealSelf = 10020,

    /// <summary>
    /// Randomly reschedules the emote's own WorldObject's next HeartBeat tick, by directly
    /// setting the in-memory NextHeartbeatTime field (WorldObject_Tick.cs) to now + a random
    /// offset. Min/Max = inclusive random offset range in seconds.
    ///
    /// Exists because HeartbeatTimestamp/HeartbeatInterval alone can't desync multiple
    /// instances of the same weenie that all get activated by the same broadcast signal at
    /// the same moment (e.g. several identical landblock fixtures reacting to one LocalSignal)
    /// - NextHeartbeatTime is a runtime-only field recomputed from "now" at load time (with
    /// only a small built-in 0-5s spread) and is never re-derived from the saved
    /// HeartbeatTimestamp property afterward, so writing that property via StampMyQuest-style
    /// emotes has no effect on actual tick scheduling.
    /// </summary>
    JitterNextHeartbeat = 10021,

    /// <summary>
    /// Sets the emote's own WorldObject's IsHot property (PropertyBool.IsHot) to true, if the
    /// WorldObject is a Hotspot.
    ///
    /// Exists because the built-in SetBoolStat/SetMyBoolStat emotes only operate on Player or
    /// Creature WorldObjects respectively (see EmoteManager.cs) - a Hotspot is neither, so
    /// there was no data-driven way to flip a Hotspot's damage on after it already exists.
    /// Hotspot.IsHot is read fresh from the property store on every collision tick (see
    /// Hotspot.cs Activate()), so a Hotspot can be spawned inert (no IsHot set, purely visual)
    /// and then have this fired after a delay once its spawn-in animation has finished, without
    /// needing to destroy/respawn the object.
    /// </summary>
    ActivateHotspot = 10022,

    /// <summary>
    /// Erases a quest from the emote's target (same target resolution as StampQuestForAllFellows -
    /// including the Hotspot P_HotspotOwner fallback) and, if that target is in a fellowship, from
    /// every other current fellow member's own individual QuestManager too.
    ///
    /// Exists as the erase-side counterpart to StampQuestForAllFellows: since that emote mirrors a
    /// stamp onto every fellow member's own personal QuestManager rather than a shared
    /// Fellowship.QuestManager, a plain EraseQuest/EraseFellowQuest pair only clears the triggering
    /// target's own copy - every other fellow member who received the mirrored stamp keeps their
    /// value, which for a spend-once resource (e.g. a contribution counter reset after successfully
    /// triggering an encounter) lets each fellow member independently re-spend the same underlying
    /// pooled total.
    /// </summary>
    EraseQuestForAllFellows = 10023,

    /// <summary>
    /// Increments a quest's solve count on the emote's target (same target resolution as
    /// StampQuestForAllFellows - the acting target regardless of fellowship, plus every OTHER
    /// current fellow member's own QuestManager) by the action's Amount column (default 1),
    /// instead of the flat +1 a plain StampQuestForAllFellows applies. Lets a single kill/event
    /// contribute a different amount depending on which weenie triggered it (e.g. a tougher
    /// monster type worth more toward a pooled contribution total than a weaker one), without
    /// needing the caller to fire the same emote multiple times.
    /// </summary>
    IncrementQuestForAllFellows = 10024,

    /// <summary>
    /// Restores mana to the emote's activating Player's currently-equipped items, rationed across
    /// every equipped item that still needs it - the same algorithm a native ManaStone uses on
    /// itself (see WorldObjects/ManaStone.cs and the extracted Player.TopOffEquippedItemsMana()),
    /// but driven by a fixed data-defined percentage of the player's own gear instead of a
    /// persisted ItemCurMana pool. The action's Percent column (0.0-1.0) is multiplied against the
    /// sum of ItemMaxMana across the player's currently-equipped items to compute the pool.
    ///
    /// Exists because this weenie (e.g. RIMS 1.0, WCID 2023139) needs the *effect* of a Mana Stone
    /// without becoming one - WeenieType.ManaStone's HandleActionUseOnTarget override intercepts
    /// all Use handling for that weenie type and never calls EmoteManager.OnUse(), which would
    /// silently kill this item's other, emote-driven Use functions. Requires peace mode - fails
    /// with WeenieError.YouMustBeInPeaceModeToTrade if the player is not in NonCombat mode.
    /// </summary>
    TopOffEquippedItemsMana = 10025,

    /// <summary>
    /// Casts a spell on the neediest member of the caster's own Cohort - every other Creature in
    /// the caster's landblock sharing the same Cohort value, excluding the caster itself. "Neediest"
    /// means lowest current Health percentage among those candidates; the cast is skipped entirely
    /// if even that lowest candidate is already at or above the action's max_Dbl threshold (nobody
    /// needs it). No candidates, or the caster has no Cohort set, is also a silent no-op.
    /// </summary>
    CastSpellOnCohort = 10026,

    /// <summary>
    /// Casts a spell on every present member of a nearby player's Fellowship. Finds the closest
    /// player within Math.Max(HomeRadius, 2 x the spell's own BaseRangeConstant) - searched across
    /// the caster's landblock and its Adjacents - takes their Fellowship (or just that one player,
    /// if unfellowshiped), re-filters the fellowship by that same radius (Fellowship membership
    /// itself carries no proximity guarantee), and casts on every survivor. No skip-threshold -
    /// unlike CastSpellOnCohort this isn't a beneficial "does anyone need this" cast. No nearby
    /// player found is a silent no-op.
    /// </summary>
    CastSpellOnFellowship = 10027,

    /// <summary>
    /// Sets the emote's own WorldObject (must be a Creature) to an absolute percentage of one
    /// vital's MaxValue - Stat holds the raw PropertyAttribute2nd value (Health/Stamina/Mana),
    /// Percent is a fraction (0.0-1.0) of that vital's MaxValue. Self only, no target resolution.
    /// </summary>
    SetVitalPercent = 10028
}
