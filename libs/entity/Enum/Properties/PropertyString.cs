using System.ComponentModel;

namespace ACE.Entity.Enum.Properties;

public enum PropertyString : ushort
{
    // No properties are sent to the client unless they feature an attribute.
    // SendOnLogin gets sent to players in the PlayerDescription event
    // AssessmentProperty gets sent in successful appraisal
    // description attributes are used by the weenie editor for a cleaner display name
    Undef = 0,

    [SendOnLogin]
    Name = 1,

    /// <summary>
    /// default "Adventurer"
    /// </summary>
    Title = 2,
    Sex = 3,
    HeritageGroup = 4,

    [AssessmentProperty]
    [SendOnLogin]
    Template = 5,
    AttackersName = 6,

    [AssessmentProperty]
    Inscription = 7,

    [AssessmentProperty]
    [Description("Scribe Name")]
    ScribeName = 8,
    VendorsName = 9,

    [AssessmentProperty]
    Fellowship = 10,
    MonarchsName = 11,
    LockCode = 12,
    KeyCode = 13,

    [AssessmentProperty]
    Use = 14,

    [AssessmentProperty]
    ShortDesc = 15,

    [AssessmentProperty]
    LongDesc = 16,
    ActivationTalk = 17,
    UseMessage = 18,
    ItemHeritageGroupRestriction = 19,
    PluralName = 20,

    [AssessmentProperty]
    MonarchsTitle = 21,
    ActivationFailure = 22,

    [AssessmentProperty]
    ScribeAccount = 23,
    TownName = 24,

    [AssessmentProperty]
    CraftsmanName = 25,
    UsePkServerError = 26,
    ScoreCachedText = 27,
    ScoreDefaultEntryFormat = 28,
    ScoreFirstEntryFormat = 29,
    ScoreLastEntryFormat = 30,
    ScoreOnlyEntryFormat = 31,
    ScoreNoEntry = 32,
    Quest = 33,
    GeneratorEvent = 34,

    [AssessmentProperty]
    PatronsTitle = 35,
    HouseOwnerName = 36,
    QuestRestriction = 37,

    [AssessmentProperty]
    AppraisalPortalDestination = 38,

    [AssessmentProperty]
    TinkerName = 39,

    [AssessmentProperty]
    ImbuerName = 40,
    HouseOwnerAccount = 41,
    DisplayName = 42,

    [AssessmentProperty]
    DateOfBirth = 43,
    ThirdPartyApi = 44,
    KillQuest = 45,

    [Ephemeral]
    Afk = 46,

    [AssessmentProperty]
    AllegianceName = 47,
    AugmentationAddQuest = 48,
    KillQuest2 = 49,
    KillQuest3 = 50,
    UseSendsSignal = 51,

    [AssessmentProperty]
    [Description("Gear Plating Name")]
    GearPlatingName = 52,
    SigilTrinketAllowedSpecializedSkills = 53,
    PatrolPath = 54,
    ScrollWritingComponents = 55,
    SpellTomeDiscoveredFormulas = 56,
    PCAPRecordedCurrentMotionState = 8006,
    PCAPRecordedServerName = 8031,
    PCAPRecordedCharacterName = 8032,

    /* custom */
    AllegianceMotd = 9001,
    AllegianceMotdSetBy = 9002,
    AllegianceSpeakerTitle = 9003,
    AllegianceSeneschalTitle = 9004,
    AllegianceCastellanTitle = 9005,
    GodState = 9006,
    TinkerLog = 9007,
    VendorBroadcastPrepend = 9008,
    VendorBroadcastAppend = 9009,

    [AssessmentProperty]
    LegacyJewelSocketString1 = 9010,

    [AssessmentProperty]
    LegacyJewelSocketString2 = 9011,

    [AssessmentProperty]
    CacheLog = 9012,

    [AssessmentProperty]
    AllegianceLog = 9013,

    [AssessmentProperty]
    CorpseLog = 9014,

    /// <summary>
    /// The character's arena ratings and records on the team boards (2v2, 3v3 scaled, ...), as JSON keyed by the board's name.
    /// The 1v1 boards have properties of their own (PropertyInt.ArenaRating, ...).
    /// </summary>
    ArenaTeamBoards = 9015,

    /// <summary>
    /// What /arena reset took from the character, as JSON keyed by the board's name (as ArenaTeamBoards, but for every board),
    /// so that /arena restore can give it back.
    /// </summary>
    ArenaResetBackup = 9016,
    


}

public static class PropertyStringExtensions
{
    public static string GetDescription(this PropertyString prop)
    {
        var description = prop.GetAttributeOfType<DescriptionAttribute>();
        return description?.Description ?? prop.ToString();
    }
}
