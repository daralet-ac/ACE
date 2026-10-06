namespace ACE.Database.Models.Shard;

/// <summary>
/// One quest in a quest registry: a character's (CharacterPropertiesQuestRegistry) or an account's (AccountQuestRegistry).
/// </summary>
public interface IQuestRegistryEntry
{
    /// <summary>
    /// Unique Name of Quest
    /// </summary>
    string QuestName { get; }

    /// <summary>
    /// Timestamp of last successful completion
    /// </summary>
    uint LastTimeCompleted { get; set; }

    /// <summary>
    /// Number of successful completions
    /// </summary>
    int NumTimesCompleted { get; set; }
}

public partial class CharacterPropertiesQuestRegistry : IQuestRegistryEntry { }
