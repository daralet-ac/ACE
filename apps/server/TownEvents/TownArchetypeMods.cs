namespace ACE.Server.TownEvents;

/// <summary>
/// Multipliers on a town event attacker's archetype stats, applied with its other archetype settings when it enters the
/// world (Creature.ApplyArchetypeSystem)
/// </summary>
public sealed record TownArchetypeMods(
    double Toughness,
    double Physicality,
    double Dexterity,
    double Magic,
    double Intelligence,
    double Lethality
)
{
    public static readonly TownArchetypeMods None = new(1, 1, 1, 1, 1, 1);

    // what each guard supply level adds to an attack's Warlord, so a richer town's raid is led by a stronger one. Elites and
    // Raiders get a share of it.
    public const double ToughnessPerLevel = 0.20;
    public const double PhysicalityPerLevel = 0.02;
    public const double DexterityPerLevel = 0.02;
    public const double MagicPerLevel = 0.02;
    public const double IntelligencePerLevel = 0.02;
    public const double LethalityPerLevel = 0.10;

    // the share of the Warlord's scaling the lesser variants get; a plain attacker gets none
    public const double EliteShare = 0.75;
    public const double RaiderShare = 0.5;

    /// <summary>
    /// The Warlord's multipliers at a town's supply level: none at 0, and at 10 three times the toughness, double the
    /// lethality and a fifth more of the rest
    /// </summary>
    public static TownArchetypeMods ForLeader(int supplyLevel) => Scaled(supplyLevel, 1.0);

    public static TownArchetypeMods ForElite(int supplyLevel) => Scaled(supplyLevel, EliteShare);

    public static TownArchetypeMods ForRaider(int supplyLevel) => Scaled(supplyLevel, RaiderShare);

    /// <summary>
    /// The Warlord's per-level gains, times a share of them
    /// </summary>
    private static TownArchetypeMods Scaled(int supplyLevel, double share)
    {
        var level = System.Math.Clamp(supplyLevel, 0, TownSupply.MaxLevel) * share;

        if (level == 0)
        {
            return None;
        }

        return new TownArchetypeMods(
            1 + ToughnessPerLevel * level,
            1 + PhysicalityPerLevel * level,
            1 + DexterityPerLevel * level,
            1 + MagicPerLevel * level,
            1 + IntelligencePerLevel * level,
            1 + LethalityPerLevel * level
        );
    }
}
