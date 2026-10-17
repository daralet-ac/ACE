using System.Text.Json;
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

    // Arena boards (ACE.Entity.Enum.Properties). The 1v1 boards have properties of their own; every
    // team board (2v2, 3v3 scaled, ...) is a JSON object in ArenaTeamBoards keyed by the board's name.
    private const int PropertyIntArenaRating = 526;
    private const int PropertyIntArenaWins = 527;
    private const int PropertyIntArenaLosses = 528;
    private const int PropertyIntArenaDraws = 529;
    private const int PropertyIntArenaScaledRating = 530;
    private const int PropertyIntArenaScaledWins = 531;
    private const int PropertyIntArenaScaledLosses = 532;
    private const int PropertyIntArenaScaledDraws = 533;
    private const int PropertyStringArenaTeamBoards = 9015;
    private const int PropertyBoolArenaRankingExcluded = 185;

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

    /// <summary>
    /// The rated-duel ladder for one board (team size, scaled or not), best rating first. Characters an
    /// admin has taken off the rankings (ArenaRankingExcluded) and characters with no rated duel on the
    /// board (including ones reset to the starting rating) are left out, as in the game's /arena top.
    /// </summary>
    public Task<List<ArenaLeaderboardEntry>> GetArenaBoardAsync(int size, bool scaled, int limit, CancellationToken ct)
    {
        limit = Math.Clamp(limit, 1, 100);
        return size == 1 ? GetArenaOneOnOneAsync(scaled, limit, ct) : GetArenaTeamBoardAsync(size, scaled, limit, ct);
    }

    private async Task<List<ArenaLeaderboardEntry>> GetArenaOneOnOneAsync(bool scaled, int limit, CancellationToken ct)
    {
        await using var connection = new MySqlConnection(_dbOptions.ShardConnectionString);
        await connection.OpenAsync(ct);

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"""
            SELECT c.name,
                   r.value AS rating,
                   COALESCE(w.value, 0) AS wins,
                   COALESCE(l.value, 0) AS losses,
                   COALESCE(d.value, 0) AS draws
            FROM `character` c
            JOIN biota_properties_int r ON r.object_Id = c.id AND r.type = @ratingType
            LEFT JOIN biota_properties_int w ON w.object_Id = c.id AND w.type = @winsType
            LEFT JOIN biota_properties_int l ON l.object_Id = c.id AND l.type = @lossesType
            LEFT JOIN biota_properties_int d ON d.object_Id = c.id AND d.type = @drawsType
            LEFT JOIN biota_properties_bool ex ON ex.object_Id = c.id AND ex.type = @excludedType
            {PlayerAccountJoin}
            WHERE c.is_Deleted = 0 AND {PlayerAccountFilter}
              AND COALESCE(ex.value, 0) = 0
              AND COALESCE(w.value, 0) + COALESCE(l.value, 0) + COALESCE(d.value, 0) > 0
            ORDER BY r.value DESC, COALESCE(w.value, 0) DESC, c.name
            LIMIT @limit
            """;
        cmd.Parameters.AddWithValue("@ratingType", scaled ? PropertyIntArenaScaledRating : PropertyIntArenaRating);
        cmd.Parameters.AddWithValue("@winsType", scaled ? PropertyIntArenaScaledWins : PropertyIntArenaWins);
        cmd.Parameters.AddWithValue("@lossesType", scaled ? PropertyIntArenaScaledLosses : PropertyIntArenaLosses);
        cmd.Parameters.AddWithValue("@drawsType", scaled ? PropertyIntArenaScaledDraws : PropertyIntArenaDraws);
        cmd.Parameters.AddWithValue("@excludedType", PropertyBoolArenaRankingExcluded);
        cmd.Parameters.AddWithValue("@limit", limit);

        var results = new List<ArenaLeaderboardEntry>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var rank = 1;
        while (await reader.ReadAsync(ct))
        {
            results.Add(new ArenaLeaderboardEntry
            {
                Rank = rank++,
                CharacterName = reader.GetString("name"),
                Rating = reader.GetInt32("rating"),
                Wins = reader.GetInt32("wins"),
                Losses = reader.GetInt32("losses"),
                Draws = reader.GetInt32("draws"),
            });
        }
        return results;
    }

    // One board's standing inside the ArenaTeamBoards JSON (System.Text.Json's default names).
    private sealed class ArenaTeamStanding
    {
        public int Rating { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int Draws { get; set; }
    }

    private static readonly JsonSerializerOptions TeamBoardJson = new() { PropertyNameCaseInsensitive = true };

    private async Task<List<ArenaLeaderboardEntry>> GetArenaTeamBoardAsync(int size, bool scaled, int limit, CancellationToken ct)
    {
        // The game names a board "2v2" or "3v3 scaled".
        var boardName = $"{size}v{size}{(scaled ? " scaled" : "")}";

        await using var connection = new MySqlConnection(_dbOptions.ShardConnectionString);
        await connection.OpenAsync(ct);

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"""
            SELECT c.name, s.value AS boards
            FROM `character` c
            JOIN biota_properties_string s ON s.object_Id = c.id AND s.type = @boardsType
            LEFT JOIN biota_properties_bool ex ON ex.object_Id = c.id AND ex.type = @excludedType
            {PlayerAccountJoin}
            WHERE c.is_Deleted = 0 AND {PlayerAccountFilter} AND COALESCE(ex.value, 0) = 0
            """;
        cmd.Parameters.AddWithValue("@boardsType", PropertyStringArenaTeamBoards);
        cmd.Parameters.AddWithValue("@excludedType", PropertyBoolArenaRankingExcluded);

        // Only characters that have fought a team duel have this property, so the set stays small;
        // the JSON is filtered and ranked here rather than in SQL.
        var standings = new List<(string Name, ArenaTeamStanding Standing)>();
        await using (var reader = await cmd.ExecuteReaderAsync(ct))
        {
            while (await reader.ReadAsync(ct))
            {
                Dictionary<string, ArenaTeamStanding>? boards;
                try
                {
                    boards = JsonSerializer.Deserialize<Dictionary<string, ArenaTeamStanding>>(
                        reader.GetString("boards"), TeamBoardJson);
                }
                catch (JsonException)
                {
                    continue; // unreadable, as the game itself treats it: no boards
                }

                if (boards is not null
                    && boards.TryGetValue(boardName, out var standing)
                    && standing.Wins + standing.Losses + standing.Draws > 0)
                {
                    standings.Add((reader.GetString("name"), standing));
                }
            }
        }

        var rank = 1;
        return standings
            .OrderByDescending(s => s.Standing.Rating)
            .ThenByDescending(s => s.Standing.Wins)
            .ThenBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .Select(s => new ArenaLeaderboardEntry
            {
                Rank = rank++,
                CharacterName = s.Name,
                Rating = s.Standing.Rating,
                Wins = s.Standing.Wins,
                Losses = s.Standing.Losses,
                Draws = s.Standing.Draws,
            })
            .ToList();
    }

    /// <summary>
    /// The fastest capstone completions of one dungeon at one difficulty and fellowship size (1, 2, or 3 meaning 3 or
    /// more), fastest first, with only the best time of each distinct group of characters. Runs with a character on
    /// a staff account (accessLevel &gt; 1) are left out, as elsewhere. Returns null while the game server has not
    /// created the capstone_run tables yet (they come with the server update that records runs).
    /// </summary>
    public async Task<List<CapstoneLeaderboardEntry>?> GetCapstoneBoardAsync(
        string dungeon, int fellowshipSize, int difficultyTier, int limit, CancellationToken ct)
    {
        limit = Math.Clamp(limit, 1, 100);
        var sizeFilter = fellowshipSize >= 3 ? "r.fellowship_size >= 3" : "r.fellowship_size = @size";

        try
        {
            await using var connection = new MySqlConnection(_dbOptions.ShardConnectionString);
            await connection.OpenAsync(ct);

            var runs = new List<CapstoneLeaderboardEntry>();
            var runIds = new List<long>();

            await using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = $"""
                    SELECT best.id, best.completion_seconds, best.completed_at_utc
                    FROM (
                        SELECT r.id, r.completion_seconds, r.completed_at_utc,
                               ROW_NUMBER() OVER (PARTITION BY r.roster_key ORDER BY r.completion_seconds, r.id) AS rn
                        FROM capstone_run r
                        WHERE r.dungeon = @dungeon AND r.difficulty_tier = @tier AND {sizeFilter}
                          AND NOT EXISTS (
                              SELECT 1
                              FROM capstone_run_member m
                              JOIN `character` c ON c.id = m.character_id
                              JOIN {_dbOptions.AuthDatabase}.account a ON a.accountId = c.account_Id
                              WHERE m.run_id = r.id AND a.accessLevel > 1)
                    ) best
                    WHERE best.rn = 1
                    ORDER BY best.completion_seconds, best.id
                    LIMIT @limit
                    """;
                cmd.Parameters.AddWithValue("@dungeon", dungeon);
                cmd.Parameters.AddWithValue("@tier", difficultyTier);
                cmd.Parameters.AddWithValue("@size", fellowshipSize);
                cmd.Parameters.AddWithValue("@limit", limit);

                await using var reader = await cmd.ExecuteReaderAsync(ct);
                var rank = 1;
                while (await reader.ReadAsync(ct))
                {
                    runIds.Add(reader.GetInt64("id"));
                    runs.Add(new CapstoneLeaderboardEntry
                    {
                        Rank = rank++,
                        CompletionSeconds = reader.GetInt32("completion_seconds"),
                        // stored in UTC; mark it so it is sent with a Z and the browser shows the right local time
                        CompletedAt = DateTime.SpecifyKind(reader.GetDateTime("completed_at_utc"), DateTimeKind.Utc),
                    });
                }
            }

            if (runs.Count == 0)
            {
                return runs;
            }

            await using (var cmd = connection.CreateCommand())
            {
                var idParams = runIds.Select((_, i) => $"@id{i}").ToList();
                cmd.CommandText = $"""
                    SELECT run_id, character_name
                    FROM capstone_run_member
                    WHERE run_id IN ({string.Join(", ", idParams)})
                    ORDER BY run_id, character_name
                    """;
                for (var i = 0; i < runIds.Count; i++)
                {
                    cmd.Parameters.AddWithValue(idParams[i], runIds[i]);
                }

                var namesByRun = new Dictionary<long, List<string>>();
                await using var reader = await cmd.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                {
                    var runId = reader.GetInt64("run_id");
                    if (!namesByRun.TryGetValue(runId, out var names))
                    {
                        namesByRun[runId] = names = new List<string>();
                    }
                    names.Add(reader.GetString("character_name"));
                }

                for (var i = 0; i < runs.Count; i++)
                {
                    runs[i].CharacterNames = namesByRun.GetValueOrDefault(runIds[i]) ?? new List<string>();
                }
            }

            return runs;
        }
        catch (MySqlException ex) when (ex.ErrorCode == MySqlErrorCode.NoSuchTable)
        {
            return null;
        }
    }
}
