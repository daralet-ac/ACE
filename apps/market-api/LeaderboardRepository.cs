using MySqlConnector;

namespace ACE.MarketApi;

/// <summary>
/// Read-only leaderboard queries against ace_shard. PropertyInt/PropertyInt64/Skill "type"
/// values are traced directly from ACE.Entity.Enum.Properties and ACE.Entity.Enum.Skill —
/// property/skill rows only exist for non-default values, so e.g. a character with zero
/// deaths or an untrained trade skill simply has no row rather than a row containing 0.
///
/// Every query joins ace_auth.account and filters to accessLevel &lt;= 1 (Player or Advocate) —
/// Sentinel/Envoy/Developer/Admin accounts (2-5) have characters used for testing with
/// server-granted stats, and must never show up on a player leaderboard. Advocate (1) is a
/// player-facing volunteer role, not a testing account, so it stays included.
/// </summary>
public class LeaderboardRepository
{
    private const int PropertyIntLevel = 25;
    private const int PropertyInt64TotalExperience = 1;
    private const int PropertyIntNumDeaths = 43;
    private const int PropertyIntAllegianceRank = 30;

    // ACE.Entity.Enum.Skill values, confirmed against the real enum (not counted by hand).
    private static readonly (string Name, int SkillId)[] TradeSkills =
    {
        ("Blacksmithing", 28),
        ("Tailoring", 29),
        ("Spellcrafting", 30),
        ("Jewelcrafting", 18),
        ("Woodworking", 37),
        ("Alchemy", 38),
        ("Cooking", 39),
    };

    private readonly DatabaseOptions _dbOptions;

    public LeaderboardRepository(DatabaseOptions dbOptions)
    {
        _dbOptions = dbOptions;
    }

    // Every query below joins on this to exclude GM/admin/dev test characters.
    private string PlayerAccountJoin => $"JOIN {_dbOptions.AuthDatabase}.account a ON a.accountId = c.account_Id";
    private const string PlayerAccountFilter = "a.accessLevel <= 1";

    public async Task<List<LevelLeaderboardEntry>> GetTopByLevelAsync(int limit, CancellationToken ct)
    {
        limit = Math.Clamp(limit, 1, 100);

        await using var connection = new MySqlConnection(_dbOptions.ShardConnectionString);
        await connection.OpenAsync(ct);

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"""
            SELECT c.name, lvl.value AS level, xp.value AS total_xp
            FROM `character` c
            JOIN biota_properties_int lvl ON lvl.object_Id = c.id AND lvl.type = @levelType
            JOIN biota_properties_int64 xp ON xp.object_Id = c.id AND xp.type = @xpType
            {PlayerAccountJoin}
            WHERE c.is_Deleted = 0 AND {PlayerAccountFilter}
            ORDER BY xp.value DESC
            LIMIT @limit
            """;
        cmd.Parameters.AddWithValue("@levelType", PropertyIntLevel);
        cmd.Parameters.AddWithValue("@xpType", PropertyInt64TotalExperience);
        cmd.Parameters.AddWithValue("@limit", limit);

        var results = new List<LevelLeaderboardEntry>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var rank = 1;
        while (await reader.ReadAsync(ct))
        {
            results.Add(new LevelLeaderboardEntry
            {
                Rank = rank++,
                CharacterName = reader.GetString("name"),
                Level = reader.GetInt32("level"),
                TotalXp = reader.GetInt64("total_xp"),
            });
        }
        return results;
    }

    public async Task<List<DeathsLeaderboardEntry>> GetTopByDeathsAsync(int limit, CancellationToken ct)
    {
        limit = Math.Clamp(limit, 1, 100);

        await using var connection = new MySqlConnection(_dbOptions.ShardConnectionString);
        await connection.OpenAsync(ct);

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"""
            SELECT c.name, d.value AS num_deaths
            FROM `character` c
            JOIN biota_properties_int d ON d.object_Id = c.id AND d.type = @deathsType
            {PlayerAccountJoin}
            WHERE c.is_Deleted = 0 AND {PlayerAccountFilter}
            ORDER BY d.value DESC
            LIMIT @limit
            """;
        cmd.Parameters.AddWithValue("@deathsType", PropertyIntNumDeaths);
        cmd.Parameters.AddWithValue("@limit", limit);

        var results = new List<DeathsLeaderboardEntry>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var rank = 1;
        while (await reader.ReadAsync(ct))
        {
            results.Add(new DeathsLeaderboardEntry
            {
                Rank = rank++,
                CharacterName = reader.GetString("name"),
                NumDeaths = reader.GetInt32("num_deaths"),
            });
        }
        return results;
    }

    public async Task<List<AllegianceRankLeaderboardEntry>> GetTopByAllegianceRankAsync(int limit, CancellationToken ct)
    {
        limit = Math.Clamp(limit, 1, 100);

        await using var connection = new MySqlConnection(_dbOptions.ShardConnectionString);
        await connection.OpenAsync(ct);

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"""
            SELECT c.name, r.value AS allegiance_rank
            FROM `character` c
            JOIN biota_properties_int r ON r.object_Id = c.id AND r.type = @rankType
            {PlayerAccountJoin}
            WHERE c.is_Deleted = 0 AND {PlayerAccountFilter}
            ORDER BY r.value DESC
            LIMIT @limit
            """;
        cmd.Parameters.AddWithValue("@rankType", PropertyIntAllegianceRank);
        cmd.Parameters.AddWithValue("@limit", limit);

        var results = new List<AllegianceRankLeaderboardEntry>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var rank = 1;
        while (await reader.ReadAsync(ct))
        {
            results.Add(new AllegianceRankLeaderboardEntry
            {
                Rank = rank++,
                CharacterName = reader.GetString("name"),
                AllegianceRank = reader.GetInt32("allegiance_rank"),
            });
        }
        return results;
    }

    /// <summary>
    /// Town attunement quest names are stamped server-side as $"Attuned{townName}"
    /// (QuestManager.cs) — counting rows matching that prefix per character gives the
    /// number of distinct towns that character has attuned to.
    /// </summary>
    public async Task<List<TownAttunementLeaderboardEntry>> GetTopByTownAttunementsAsync(int limit, CancellationToken ct)
    {
        limit = Math.Clamp(limit, 1, 100);

        await using var connection = new MySqlConnection(_dbOptions.ShardConnectionString);
        await connection.OpenAsync(ct);

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"""
            SELECT c.name, COUNT(*) AS towns_attuned
            FROM character_properties_quest_registry q
            JOIN `character` c ON c.id = q.character_Id
            {PlayerAccountJoin}
            WHERE q.quest_Name LIKE 'Attuned%' AND c.is_Deleted = 0 AND {PlayerAccountFilter}
            GROUP BY c.id, c.name
            ORDER BY towns_attuned DESC
            LIMIT @limit
            """;
        cmd.Parameters.AddWithValue("@limit", limit);

        var results = new List<TownAttunementLeaderboardEntry>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var rank = 1;
        while (await reader.ReadAsync(ct))
        {
            results.Add(new TownAttunementLeaderboardEntry
            {
                Rank = rank++,
                CharacterName = reader.GetString("name"),
                TownsAttuned = reader.GetInt32("towns_attuned"),
            });
        }
        return results;
    }

    /// <summary>
    /// Per-creature-type death tracking is stamped as $"KilledByTracking-{creatureName}"
    /// with num_Times_Completed incrementing per kill — summed across all (non-admin)
    /// characters and grouped by creature to rank "most dangerous" by total player deaths caused.
    /// </summary>
    public async Task<List<DangerousCreatureLeaderboardEntry>> GetMostDangerousCreaturesAsync(int limit, CancellationToken ct)
    {
        limit = Math.Clamp(limit, 1, 100);

        await using var connection = new MySqlConnection(_dbOptions.ShardConnectionString);
        await connection.OpenAsync(ct);

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"""
            SELECT SUBSTRING(q.quest_Name, 18) AS creature_name, SUM(q.num_Times_Completed) AS deaths_caused
            FROM character_properties_quest_registry q
            JOIN `character` c ON c.id = q.character_Id
            {PlayerAccountJoin}
            WHERE q.quest_Name LIKE 'KilledByTracking-%' AND c.is_Deleted = 0 AND {PlayerAccountFilter}
            GROUP BY creature_name
            ORDER BY deaths_caused DESC
            LIMIT @limit
            """;
        cmd.Parameters.AddWithValue("@limit", limit);

        var results = new List<DangerousCreatureLeaderboardEntry>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var rank = 1;
        while (await reader.ReadAsync(ct))
        {
            results.Add(new DangerousCreatureLeaderboardEntry
            {
                Rank = rank++,
                CreatureName = reader.GetString("creature_name"),
                PlayerDeathsCaused = reader.GetInt64("deaths_caused"),
            });
        }
        return results;
    }

    /// <summary>
    /// One row per trade skill, showing whoever currently holds the top trained/specialized
    /// value in that skill. Not a ranked top-N per skill — just the current leader.
    /// </summary>
    public async Task<List<TradeSkillLeaderEntry>> GetTradeSkillLeadersAsync(CancellationToken ct)
    {
        await using var connection = new MySqlConnection(_dbOptions.ShardConnectionString);
        await connection.OpenAsync(ct);

        var results = new List<TradeSkillLeaderEntry>();
        foreach (var (skillName, skillId) in TradeSkills)
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = $"""
                SELECT c.name, s.level_From_P_P AS skill_level
                FROM `character` c
                JOIN biota_properties_skill s ON s.object_Id = c.id AND s.type = @skillId
                {PlayerAccountJoin}
                WHERE s.level_From_P_P > 0 AND c.is_Deleted = 0 AND {PlayerAccountFilter}
                ORDER BY s.level_From_P_P DESC
                LIMIT 1
                """;
            cmd.Parameters.AddWithValue("@skillId", skillId);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            if (await reader.ReadAsync(ct))
            {
                results.Add(new TradeSkillLeaderEntry
                {
                    SkillName = skillName,
                    CharacterName = reader.GetString("name"),
                    SkillLevel = reader.GetInt32("skill_level"),
                });
            }
            else
            {
                results.Add(new TradeSkillLeaderEntry { SkillName = skillName, CharacterName = null, SkillLevel = null });
            }
        }
        return results;
    }
}
