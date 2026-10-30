namespace ACE.Server.TownEvents;

/// <summary>
/// One player's part in a town event
/// </summary>
public sealed class TownDefenseScore
{
    public uint PlayerGuid { get; init; }

    public string Name { get; set; }

    /// <summary>
    /// Damage dealt to event creatures, up to their remaining health
    /// </summary>
    public double Damage;

    /// <summary>
    /// Life magic healing on eligible participants, up to their missing health
    /// </summary>
    public double Healing;

    public int KillingBlows;

    /// <summary>
    /// Damage taken from event creatures before armor, resistances and shields
    /// </summary>
    public double DamageTaken;

    /// <summary>
    /// The last time they damaged an event creature, healed a participant or were hit by an event creature
    /// </summary>
    public double LastActive;

    /// <summary>
    /// They killed an event creature while over the town's level and unshrouded after leaving the Town Defense, which
    /// forfeits what they'd earned
    /// </summary>
    public bool CreditLost;

    // the over-level kill log
    public int OverLevelKills;
    public int PromptsRefused;
    public int KillsAfterRefusal;

    public bool HasContributed => Damage > 0 || Healing > 0 || KillingBlows > 0 || DamageTaken > 0;

    // for the town event log (TownEventRecorder)

    public int Level;

    /// <summary>
    /// none, paid (at the end) or claim (waiting at the Captain)
    /// </summary>
    public string Reward = "none";

    public double RewardMod;

    /// <summary>
    /// How much they did, 0 to 10: each category's share of its top score, times TownEvent.CategoryBonus, added up and
    /// capped (TownEvent.GetContributionMultiplier)
    /// </summary>
    public double Contribution;

    public int CommendationTokens;
    public long Xp;
    public int CategoriesTopped;
    public int WarlordPyreals;
}
