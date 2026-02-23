using System.Collections.Generic;
using System.Linq;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Database;
using ACE.Database.Adapter;
using ACE.Server.Entity;
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

        foreach (var biota in EnumerateAccountOwnedBiotas(player))
        {
            var (biotaRaw, biotaTrophies) = GetBiotaWealth(biota);
            raw += biotaRaw;
            trophies += biotaTrophies;
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
        var accountId = player.Account?.AccountId ?? 0;

        // Fallback: no account context, only count current character possessions.
        if (accountId == 0)
        {
            foreach (var item in player.GetAllPossessions())
            {
                yield return item;
            }

            yield break;
        }

        // Always aggregate across all characters on the account.
        // Prefer live in-memory possessions for online characters; otherwise load possessions from the DB.
        var accountPlayers = PlayerManager.GetAccountPlayers(accountId) ?? new Dictionary<uint, IPlayer>();

        var characters = DatabaseManager.Shard.BaseDatabase.GetCharacters(accountId, includeDeleted: false);
        foreach (var character in characters.Where(c => !c.IsDeleted))
        {
            if (accountPlayers.TryGetValue(character.Id, out var cached) && cached is Player online)
            {
                foreach (var item in online.GetAllPossessions())
                {
                    yield return item;
                }

                continue;
            }
        }
    }

    private static IEnumerable<ACE.Entity.Models.Biota> EnumerateAccountOwnedBiotas(Player player)
    {
        var accountId = player.Account?.AccountId ?? 0;
        if (accountId == 0)
        {
            yield break;
        }

        var accountPlayers = PlayerManager.GetAccountPlayers(accountId) ?? new Dictionary<uint, IPlayer>();

        var characters = DatabaseManager.Shard.BaseDatabase.GetCharacters(accountId, includeDeleted: false);
        foreach (var character in characters.Where(c => !c.IsDeleted))
        {
            // Online characters are already covered via WorldObjects.
            if (accountPlayers.TryGetValue(character.Id, out var cached) && cached is Player)
            {
                continue;
            }

            var possessed = DatabaseManager.Shard.BaseDatabase.GetPossessedBiotasInParallel(character.Id);
            foreach (var biota in possessed.Inventory)
            {
                yield return BiotaConverter.ConvertToEntityBiota(biota);
            }

            foreach (var biota in possessed.WieldedItems)
            {
                yield return BiotaConverter.ConvertToEntityBiota(biota);
            }
        }
    }

    private static (long rawPyrealCurrency, long trophyValue) GetBiotaWealth(ACE.Entity.Models.Biota biota)
    {
        if (biota == null)
        {
            return (0, 0);
        }

        long raw = 0;
        long trophies = 0;

        if (biota.WeenieType == WeenieType.Coin)
        {
            // For coins, use `CoinValue` (preferred) then fall back to standard `Value`.
            if (biota.PropertiesInt != null && biota.PropertiesInt.TryGetValue(PropertyInt.CoinValue, out var coinValue))
            {
                raw += coinValue;
            }
            else if (biota.PropertiesInt != null && biota.PropertiesInt.TryGetValue(PropertyInt.Value, out var value))
            {
                raw += value;
            }

            return (raw, trophies);
        }

        if (TradeNoteValuesByWcid.TryGetValue(biota.WeenieClassId, out var noteValue))
        {
            var stackSize = 1;
            if (biota.PropertiesInt != null && biota.PropertiesInt.TryGetValue(PropertyInt.StackSize, out var ss) && ss > 0)
            {
                stackSize = ss;
            }

            raw += (long)noteValue * stackSize;
        }

        if (biota.PropertiesInt != null && biota.PropertiesInt.TryGetValue(PropertyInt.TrophyQuality, out var tq) && tq > 0)
        {
            trophies += (long)tq * tq * 100;
        }

        return (raw, trophies);
    }
}
