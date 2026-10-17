namespace ACE.MarketApi;

public class LevelLeaderboardEntry
{
    public int Rank { get; set; }
    public string CharacterName { get; set; } = "";
    public int Level { get; set; }
    public long TotalXp { get; set; }
}

public class DeathsLeaderboardEntry
{
    public int Rank { get; set; }
    public string CharacterName { get; set; } = "";
    public int NumDeaths { get; set; }
}

public class AllegianceRankLeaderboardEntry
{
    public int Rank { get; set; }
    public string CharacterName { get; set; } = "";
    public int AllegianceRank { get; set; }
}

public class TownAttunementLeaderboardEntry
{
    public int Rank { get; set; }
    public string CharacterName { get; set; } = "";
    public int TownsAttuned { get; set; }
}

public class DangerousCreatureLeaderboardEntry
{
    public int Rank { get; set; }
    public string CreatureName { get; set; } = "";
    public long PlayerDeathsCaused { get; set; }
}

public class TradeSkillLeaderEntry
{
    public string SkillName { get; set; } = "";
    public string? CharacterName { get; set; }
    public int? SkillLevel { get; set; }
}

public class CapstoneLeaderboardEntry
{
    public int Rank { get; set; }
    public List<string> CharacterNames { get; set; } = new();
    public int CompletionSeconds { get; set; }
    public DateTime CompletedAt { get; set; }
}

public class ArenaLeaderboardEntry
{
    public int Rank { get; set; }
    public string CharacterName { get; set; } = "";
    public int Rating { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int Draws { get; set; }
}
