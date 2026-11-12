namespace ACE.Server.Entity;

/// <summary>
/// The share of a creature's level cost one kill gives (Creature.GetCreatureDeathXP), by the killer's level. Kept apart
/// so rested experience can build up by the same share (RestedXpRules.LevelsPerHour) and keep pace with kills, whatever
/// these become.
/// </summary>
public static class KillXp
{
    /// <summary>2% below level 10, then 1%, 0.5%, 0.4%, 0.3% by tens to 50, 0.2% to 75, 0.1% to 100, then 0.05%.</summary>
    public static float Share(int killerLevel)
    {
        return killerLevel switch
        {
            < 10 => 0.02f,
            < 20 => 0.01f,
            < 30 => 0.005f,
            < 40 => 0.004f,
            < 50 => 0.003f,
            < 75 => 0.002f,
            < 100 => 0.001f,
            _ => 0.0005f,
        };
    }
}
