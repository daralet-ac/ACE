using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using ACE.Server.Entity;
using ACE.Server.Managers;
using Serilog;

namespace ACE.Server.TownEvents;

/// <summary>
/// A town's guard supply: pyreals donated to its Captain of the Guard, which drain away over time. The supply sets the
/// town's supply level (0 to 10), and each level lowers the unrest it takes to start an attack (a full coffer draws
/// raiders), and raises how hard attacks are and what they pay. Kept on the world thread (TownEventManager), and saved in town_events_supply.
/// </summary>
public static class TownSupply
{
    private static readonly ILogger _log = Log.ForContext(typeof(TownSupply));

    public const string PropertyName = "town_events_supply";

    public const int MaxLevel = 10;

    // each level costs this much more than the one before: 10% more, so the levels stay close to flat. 1.5 was tried first
    // and made the top levels far too dear.
    public const double LevelGrowth = 1.1;

    // level 1's cost: 1,000 × tier² from tier 1, and half of tier 1's for tier 0, which tier² would make free
    private const double LevelOneBase = 1000;
    private const double TierZeroLevelOne = 500;

    // every level's cost is rounded to this many pyreals, so the numbers players see are round
    public const double CostRounding = 50;

    // the share of the current level's threshold lost each hour, for levels 1 to 10. Supply below level 1 drains at level 1's
    // rate, so a part donation doesn't sit there forever.
    private static readonly double[] HourlyLossShare = { 0.02, 0.03, 0.04, 0.05, 0.06, 0.08, 0.10, 0.12, 0.15, 0.40 };

    // a newly reached level holds this long however fast it drains, so a donor sees what they paid for
    public const double MinimumHoldSeconds = 15 * 60;

    // what each level does. How much is still open, so each is one constant. The threshold falls this share per level,
    // so level 10 halves it; the wave budget and rewards rise this share per level.
    public const double ThresholdReductionPerLevel = 0.05;
    public const double WaveBudgetPerLevel = 0.10;
    public const double RewardPerLevel = 0.10;

    private class TownState
    {
        public double Supply { get; set; }

        // the level being held, and for how much longer. Counted down by the drain, so it pauses while the server is down.
        public int HeldLevel { get; set; }
        public double HoldSeconds { get; set; }
    }

    private static Dictionary<string, TownState> towns;

    // ---------------------------------------------------------------- the formula

    /// <summary>
    /// What level 1 costs in a town of this tier (from 0): 500 at tier 0, then 1,000 × tier²
    /// </summary>
    public static double GetLevelOneCost(int tier) => tier <= 0 ? TierZeroLevelOne : LevelOneBase * tier * tier;

    /// <summary>
    /// The supply it takes to reach a level in a town of this tier (from 0): GetLevelOneCost for level 1, and each level
    /// after costs LevelGrowth times the one before
    /// </summary>
    public static double GetThreshold(int level, int tier)
    {
        if (level <= 0)
        {
            return 0;
        }

        level = Math.Min(level, MaxLevel);
        // to the nearest 50 pyreals, which also means a donation of exactly the cost reaches the level (1.1^L isn't exact in
        // floating point)
        var cost = GetLevelOneCost(tier) * (Math.Pow(LevelGrowth, level) - 1) / (LevelGrowth - 1);
        return Math.Round(cost / CostRounding, MidpointRounding.AwayFromZero) * CostRounding;
    }

    public static int GetLevel(double supply, int tier)
    {
        var level = 0;
        while (level < MaxLevel && supply >= GetThreshold(level + 1, tier))
        {
            level++;
        }

        return level;
    }

    /// <summary>
    /// How much supply a town at this level loses an hour: a share of the level's threshold, steeper at the top
    /// </summary>
    public static double GetHourlyLoss(int level, int tier)
    {
        level = Math.Clamp(level, 1, MaxLevel);
        return HourlyLossShare[level - 1] * GetThreshold(level, tier);
    }

    private static int TierOf(string town) => (int)Town.GetTownTier(town);

    // ---------------------------------------------------------------- state (world thread)

    private static Dictionary<string, TownState> Towns
    {
        get
        {
            if (towns == null)
            {
                Load();
            }

            return towns;
        }
    }

    private static TownState State(string town)
    {
        if (!Towns.TryGetValue(town, out var state))
        {
            state = new TownState();
            Towns[town] = state;
        }

        return state;
    }

    public static double GetSupply(string town) => Towns.TryGetValue(town, out var state) ? state.Supply : 0;

    public static int GetLevel(string town) => GetLevel(GetSupply(town), TierOf(town));

    public static double GetThreshold(string town, int level) => GetThreshold(level, TierOf(town));

    public static double GetHourlyLoss(string town) => GetSupply(town) > 0 ? GetHourlyLoss(GetLevel(town), TierOf(town)) : 0;

    public static double GetHoldSecondsLeft(string town) => Towns.TryGetValue(town, out var state) ? state.HoldSeconds : 0;

    /// <summary>
    /// How long the town's current level lasts at the current drain, counting the hold: 0 at level 0 or with no drain
    /// </summary>
    public static double GetSecondsUntilLevelDrops(string town)
    {
        var level = GetLevel(town);
        var loss = GetHourlyLoss(town);
        if (level == 0 || loss <= 0)
        {
            return 0;
        }

        var drainSeconds = (GetSupply(town) - GetThreshold(town, level)) / loss * 3600;

        // the hold only keeps the level it was for: a later donation to a higher level isn't held by it
        var state = State(town);
        return state.HeldLevel == level ? Math.Max(drainSeconds, state.HoldSeconds) : drainSeconds;
    }

    /// <summary>
    /// Multipliers for the town's supply level: 1 at level 0
    /// </summary>
    public static double GetThresholdMultiplier(string town) => GetThresholdMultiplier(GetLevel(town));

    public static double GetThresholdMultiplier(int level) => 1 - ThresholdReductionPerLevel * Math.Clamp(level, 0, MaxLevel);

    public static double GetWaveBudgetMultiplier(string town) => 1 + WaveBudgetPerLevel * GetLevel(town);

    public static double GetRewardMultiplier(string town) => 1 + RewardPerLevel * GetLevel(town);

    /// <summary>
    /// Adds to a town's supply (a donation). A level reached this way holds for MinimumHoldSeconds. Returns the new level.
    /// </summary>
    public static int Add(string town, double amount)
    {
        var state = State(town);
        var before = GetLevel(state.Supply, TierOf(town));

        state.Supply = Math.Max(0, state.Supply + amount);
        var after = GetLevel(state.Supply, TierOf(town));

        if (after > before)
        {
            state.HeldLevel = after;
            state.HoldSeconds = MinimumHoldSeconds;
            _log.Information("[TOWN EVENT] {Town}: supply level {Before} -> {After}", town, before, after);
            TownGuardUpgrades.Refresh(town);
        }

        Save();
        return after;
    }

    /// <summary>
    /// Sets a town's supply outright (@townevent supply), with no hold, so the drain can be watched straight away
    /// </summary>
    public static void Set(string town, double amount)
    {
        var state = State(town);
        var before = GetLevel(state.Supply, TierOf(town));

        state.Supply = Math.Max(0, amount);
        state.HeldLevel = 0;
        state.HoldSeconds = 0;

        if (GetLevel(state.Supply, TierOf(town)) != before)
        {
            TownGuardUpgrades.Refresh(town);
        }

        Save();
    }

    /// <summary>
    /// Takes the town's whole supply (a lost Supply Chest)
    /// </summary>
    public static void Empty(string town) => Set(town, 0);

    /// <summary>
    /// Drains every town for this much time. Called by TownEventManager's minute tick, so time the server was down never
    /// counts.
    /// </summary>
    public static void Drain(double seconds)
    {
        if (seconds <= 0)
        {
            return;
        }

        foreach (var (town, state) in Towns)
        {
            if (state.Supply <= 0)
            {
                continue;
            }

            var tier = TierOf(town);
            var before = GetLevel(state.Supply, tier);

            var supply = Math.Max(0, state.Supply - GetHourlyLoss(before, tier) * seconds / 3600);

            if (state.HoldSeconds > 0)
            {
                state.HoldSeconds = Math.Max(0, state.HoldSeconds - seconds);
                supply = Math.Max(supply, Math.Min(state.Supply, GetThreshold(state.HeldLevel, tier)));
            }

            state.Supply = supply;

            var after = GetLevel(supply, tier);
            if (after < before)
            {
                _log.Information("[TOWN EVENT] {Town}: supply level {Before} -> {After}", town, before, after);
                TownGuardUpgrades.Refresh(town);
            }
        }

        Save();
    }

    // ---------------------------------------------------------------- saving

    private static void Load()
    {
        towns = new Dictionary<string, TownState>();

        var json = PropertyManager.GetString(PropertyName).Item;
        if (string.IsNullOrWhiteSpace(json))
        {
            return;
        }

        try
        {
            var saved = JsonSerializer.Deserialize<Dictionary<string, TownState>>(json);

            // towns renamed or removed since are dropped
            foreach (var (town, state) in saved.Where(kv => Town.TownNames.Contains(kv.Key)))
            {
                towns[town] = state;
            }
        }
        catch (JsonException ex)
        {
            _log.Error(ex, "[TOWN EVENT] {Property} couldn't be read, so every town's supply starts at 0", PropertyName);
        }
    }

    /// <summary>
    /// Writes to the property cache. PropertyManager saves it to the database every 5 minutes and at shutdown.
    /// </summary>
    private static void Save()
    {
        var json = JsonSerializer.Serialize(
            Towns.Where(kv => kv.Value.Supply > 0 || kv.Value.HoldSeconds > 0)
                .OrderBy(kv => kv.Key)
                .ToDictionary(kv => kv.Key, kv => kv.Value)
        );

        if (json != PropertyManager.GetString(PropertyName).Item)
        {
            PropertyManager.ModifyString(PropertyName, json);
        }
    }
}
