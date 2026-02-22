using System.Collections.Generic;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Database;
using ACE.Server.WorldObjects;

namespace ACE.Server.Managers;

public static class PlayerWealthCalculator
{
    private static readonly IReadOnlyDictionary<uint, int> TradeNoteValuesByWcid = new Dictionary<uint, int>
    {
        [(uint)WeenieClassName.W_TRADENOTE100000_CLASS] = 100000,
        [(uint)WeenieClassName.W_TRADENOTE50000_CLASS] = 50000,
        [(uint)WeenieClassName.W_TRADENOTE10000_CLASS] = 10000,
        [(uint)WeenieClassName.W_TRADENOTE5000_CLASS] = 5000,
        [(uint)WeenieClassName.W_TRADENOTE1000_CLASS] = 1000,
        [(uint)WeenieClassName.W_TRADENOTE500_CLASS] = 500,
        [(uint)WeenieClassName.W_TRADENOTE100_CLASS] = 100,
    };

    public static (long rawPyrealCurrency, long trophyValue) GetAccountWealth(Player player)
    {
        if (player == null)
        {
            return (0, 0);
        }

        long raw = 0;
        long trophies = 0;

        foreach (var item in EnumerateAccountOwnedItems(player))
        {
            if (item.WeenieType == WeenieType.Coin)
            {
                raw += item.Value ?? 0;
                continue;
            }

            if (TradeNoteValuesByWcid.TryGetValue(item.WeenieClassId, out var noteValue))
            {
                raw += (long)noteValue * (item.StackSize ?? 1);
            }

            var tq = item.GetProperty(PropertyInt.TrophyQuality);
            if (tq.HasValue && tq.Value > 0)
            {
                trophies += (long)tq.Value * tq.Value * 100;
            }
        }

        // Account bank storage (aggregate query)
        var accountId = player.Account?.AccountId ?? 0;
        if (accountId != 0)
        {
            var (bankPyreal, bankTrophy) = DatabaseManager.Shard.BaseDatabase.GetBankWealthAggregate(accountId);
            raw += bankPyreal;
            trophies += bankTrophy;
        }

        return (raw, trophies);
    }

    private static IEnumerable<WorldObject> EnumerateAccountOwnedItems(Player player)
    {
        // Character possessions (inventory, packs, house containers, etc.)
        foreach (var item in player.GetAllPossessions())
        {
            yield return item;
        }
    }
}
