using System.Collections.Concurrent;
using ACE.Database;
using ACE.Server.WorldObjects;

namespace ACE.Server.Managers;

public static class AccountWealthTracker
{
    private static readonly ConcurrentDictionary<uint, long> WealthByAccountId = new();

    public static void Update(Player player)
    {
        var accountId = player?.Session?.AccountId ?? 0;
        if (accountId == 0)
        {
            return;
        }

        var (rawPyrealCurrency, trophyValue) = PlayerWealthCalculator.GetAccountWealth(player);
        WealthByAccountId[accountId] = rawPyrealCurrency;

        // Persist a current snapshot (best-effort).
        DatabaseManager.Shard.BaseDatabase.UpsertAccountWealthSnapshot(
            accountId,
            player?.Character?.Id,
            rawPyrealCurrency,
            trophyValue,
            System.DateTime.UtcNow
        );
    }

    public static bool TryGet(uint accountId, out long wealth) => WealthByAccountId.TryGetValue(accountId, out wealth);

    public static long GetOrDefault(uint accountId) => WealthByAccountId.TryGetValue(accountId, out var value) ? value : 0;

    public static void Remove(uint accountId) => WealthByAccountId.TryRemove(accountId, out _);

    public static void Remove(Player player)
    {
        var accountId = player?.Session?.AccountId ?? 0;
        if (accountId != 0)
        {
            Remove(accountId);
        }
    }
}
