namespace ACE.Database.Models.Shard;

/// <summary>
/// A quest stamped on an account rather than a character (account_quest_registry): every character on the account
/// shares it, including characters made later. Quests whose names start with ACCOUNT_ are kept here.
/// </summary>
public class AccountQuestRegistry : IQuestRegistryEntry
{
    /// <summary>
    /// Id of the account this quest belongs to
    /// </summary>
    public uint AccountId { get; set; }

    /// <summary>
    /// Unique Name of Quest
    /// </summary>
    public string QuestName { get; set; }

    /// <summary>
    /// Timestamp of last successful completion
    /// </summary>
    public uint LastTimeCompleted { get; set; }

    /// <summary>
    /// Number of successful completions
    /// </summary>
    public int NumTimesCompleted { get; set; }
}
