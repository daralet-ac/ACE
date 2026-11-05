using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ACE.Database;
using ACE.Database.Models.Shard;
using ACE.Entity;
using ACE.Server.WorldObjects;
using Serilog;

namespace ACE.Server.Managers;

/// <summary>
/// Items changing hands between characters, kept in the shard database (item_transfer_log) for the Daralet Admin
/// plugin's Items tab: given, traded, dropped on the ground, and picked up off the ground or a player's corpse by
/// someone else. Only moves between players are logged (no loot, vendors or moves within your own packs), so it's a few
/// rows per trade, written off the world thread. A pack that changes hands is logged with each item in it.
/// </summary>
public static class ItemTransferLog
{
    private static readonly ILogger _log = Log.ForContext(typeof(ItemTransferLog));

    public const string Given = "given";
    public const string Traded = "traded";
    public const string Dropped = "dropped";
    public const string PickedUp = "picked up";
    public const string Looted = "looted";

    /// <summary>Items dropped longer ago than this are forgotten: the ground has long since cleaned them up.</summary>
    private static readonly TimeSpan DroppedMemory = TimeSpan.FromHours(2);

    private const int MaxNameLength = 100;
    private const int MaxDetailsLength = 200;

    /// <summary>
    /// Who dropped each item that's on the ground now (by guid), so whoever picks it up can be logged as getting it from
    /// them. Ground items aren't saved, so a restart forgets them along with this.
    /// </summary>
    private static readonly ConcurrentDictionary<uint, (uint CharacterId, string Name, uint? AccountId, DateTime At)> droppedBy = new();

    // A missing table (its update script not applied yet) would fail every write, so that is reported once.
    private static int _writeFailureReported;

    /// <summary>
    /// from gave item (and everything in it, for a pack) to to: amount of a stack.
    /// </summary>
    public static void RecordGive(Player from, Player to, WorldObject item, int amount)
    {
        Record(Given, item, amount, from, to, null);
    }

    /// <summary>
    /// A trade went through: what each side got.
    /// </summary>
    public static void RecordTrade(Player first, Player second, IEnumerable<WorldObject> toSecond, IEnumerable<WorldObject> toFirst)
    {
        var entries = new List<ItemTransfer>();

        foreach (var item in toSecond)
        {
            entries.AddRange(Build(Traded, item, item.StackSize ?? 1, first, second, null));
        }

        foreach (var item in toFirst)
        {
            entries.AddRange(Build(Traded, item, item.StackSize ?? 1, second, first, null));
        }

        Save(entries);
    }

    /// <summary>
    /// player dropped item on the ground. Whoever else picks it up next is logged too.
    /// </summary>
    public static void RecordDrop(Player player, WorldObject item)
    {
        if (player == null || item == null)
        {
            return;
        }

        ForgetOldDrops();
        droppedBy[item.Guid.Full] = (player.Guid.Full, player.Name, player.Account?.AccountId, DateTime.UtcNow);

        Record(Dropped, item, item.StackSize ?? 1, player, null, null);
    }

    /// <summary>
    /// player picked item up off the ground. Logged only when someone else dropped it.
    /// </summary>
    public static void RecordPickup(Player player, WorldObject item)
    {
        if (player == null || item == null || !droppedBy.TryRemove(item.Guid.Full, out var dropper))
        {
            return;
        }

        if (dropper.CharacterId == player.Guid.Full)
        {
            return;
        }

        var entries = Build(PickedUp, item, item.StackSize ?? 1, null, player, "off the ground");

        foreach (var entry in entries)
        {
            entry.FromCharacterId = dropper.CharacterId;
            entry.FromName = dropper.Name;
            entry.FromAccountId = dropper.AccountId;
        }

        Save(entries);
    }

    /// <summary>
    /// player took item off another player's corpse.
    /// </summary>
    public static void RecordCorpseLoot(Player player, Corpse corpse, WorldObject item)
    {
        // monsters' corpses are most of them, and aren't anyone's
        if (player == null || corpse?.VictimId == null || corpse.VictimId == player.Guid.Full
            || !new ObjectGuid(corpse.VictimId.Value).IsPlayer())
        {
            return;
        }

        var victim = PlayerManager.FindByGuid(corpse.VictimId.Value);

        if (victim == null)
        {
            return;
        }

        var entries = Build(Looted, item, item.StackSize ?? 1, null, player, "from their corpse");

        foreach (var entry in entries)
        {
            entry.FromCharacterId = victim.Guid.Full;
            entry.FromName = victim.Name;
            entry.FromAccountId = victim.Account?.AccountId;
        }

        Save(entries);
    }

    private static void Record(string kind, WorldObject item, int amount, Player from, Player to, string details)
    {
        if (item == null)
        {
            return;
        }

        Save(Build(kind, item, amount, from, to, details));
    }

    /// <summary>
    /// The entries for item, and for everything in it if it's a pack ("in Backpack" in their details).
    /// </summary>
    internal static List<ItemTransfer> Build(string kind, WorldObject item, int amount, Player from, Player to, string details)
    {
        var now = DateTime.UtcNow;
        var entries = new List<ItemTransfer> { Entry(kind, item, amount, from, to, details, now) };

        if (item is Container pack)
        {
            foreach (var inside in pack.Inventory.Values)
            {
                entries.Add(Entry(kind, inside, inside.StackSize ?? 1, from, to, Join(details, $"in {pack.Name}"), now));

                if (inside is Container innerPack)
                {
                    foreach (var deeper in innerPack.Inventory.Values)
                    {
                        entries.Add(Entry(kind, deeper, deeper.StackSize ?? 1, from, to, Join(details, $"in {innerPack.Name}"), now));
                    }
                }
            }
        }

        return entries;
    }

    private static ItemTransfer Entry(string kind, WorldObject item, int amount, Player from, Player to, string details, DateTime at)
    {
        return new ItemTransfer
        {
            Kind = kind,
            ItemId = item.Guid.Full,
            ItemWcid = item.WeenieClassId,
            ItemName = Truncate(item.NameWithMaterial ?? item.Name ?? "", MaxNameLength),
            Amount = amount,
            FromCharacterId = from?.Guid.Full,
            FromName = from?.Name,
            FromAccountId = from?.Account?.AccountId,
            ToCharacterId = to?.Guid.Full,
            ToName = to?.Name,
            ToAccountId = to?.Account?.AccountId,
            Details = details == null ? null : Truncate(details, MaxDetailsLength),
            CreatedAtUtc = at,
        };
    }

    private static string Join(string first, string second)
    {
        return string.IsNullOrEmpty(first) ? second : $"{first}, {second}";
    }

    private static string Truncate(string text, int length)
    {
        return text.Length > length ? text[..length] : text;
    }

    private static void ForgetOldDrops()
    {
        // only now and then: this runs on every drop
        if (droppedBy.Count < 500)
        {
            return;
        }

        var cutoff = DateTime.UtcNow - DroppedMemory;

        foreach (var (guid, drop) in droppedBy.ToArray())
        {
            if (drop.At < cutoff)
            {
                droppedBy.TryRemove(guid, out _);
            }
        }
    }

    private static void Save(List<ItemTransfer> entries)
    {
        if (entries.Count == 0)
        {
            return;
        }

        _ = Task.Run(() =>
        {
            try
            {
                DatabaseManager.Shard.BaseDatabase.AddItemTransfers(entries);
            }
            catch (Exception ex)
            {
                if (Interlocked.Exchange(ref _writeFailureReported, 1) == 0)
                {
                    _log.Error(ex, "[ITEMS] Couldn't save to item_transfer_log; item history stays empty until it can. Has the 2026-11-05-00-Item-Transfer-Log.sql update run?");
                }
            }
        });
    }
}
