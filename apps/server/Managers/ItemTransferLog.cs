using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ACE.Database;
using ACE.Database.Models.Shard;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.WorldObjects;
using Serilog;

namespace ACE.Server.Managers;

/// <summary>
/// Items changing hands, kept in the shard database (item_transfer_log) for the Daralet Admin plugin's Items tab: given,
/// traded, dropped on the ground, picked up off the ground or a player's corpse by someone else, sold to and bought back
/// from vendors, salvaged, and copied or restored by an admin. Written off the world thread. A pack that changes hands is
/// logged with each item in it.
///
/// Loot and moves within your own packs aren't logged, and selling, buying back, salvaging and dropping only for loot of
/// good workmanship for its tier (item_history_min_workmanship), which keeps vendor trash out. Pyreal value would be no
/// good for that: it changes too much from tier to tier. Those come with a snapshot of the whole item, so
/// @itemrestore can make it again. Old entries are deleted once a day (item_history_days, item_history_destroyed_days).
/// </summary>
public static class ItemTransferLog
{
    private static readonly ILogger _log = Log.ForContext(typeof(ItemTransferLog));

    public const string Given = "given";
    public const string Traded = "traded";
    public const string Dropped = "dropped";
    public const string PickedUp = "picked up";
    public const string Looted = "looted";
    public const string Sold = "sold to vendor";
    public const string Bought = "bought";
    public const string Salvaged = "salvaged";
    public const string Copied = "copied";
    public const string Restored = "restored";

    /// <summary>The kinds kept item_history_destroyed_days, not item_history_days: there are many more of them.</summary>
    public static readonly string[] ShortLivedKinds = { Sold, Bought, Salvaged };

    private static readonly TimeSpan PruneInterval = TimeSpan.FromDays(1);
    private static DateTime _nextPruneUtc = DateTime.MinValue;
    private static int _pruning;

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

        // remembered either way, so whoever picks it up is logged; the drop itself only when it's worth something
        if (!WorthLogging(item))
        {
            return;
        }

        var entries = Build(Dropped, item, item.StackSize ?? 1, player, null, null);
        entries[0].Snapshot = ItemSnapshots.FromLive(item);
        Save(entries);
    }

    /// <summary>
    /// player sold these to vendor. Only the ones worth logging, each with a snapshot, as most are destroyed.
    /// </summary>
    public static void RecordSale(Player player, WorldObject vendor, IEnumerable<WorldObject> items)
    {
        var entries = new List<ItemTransfer>();

        foreach (var item in items.Where(WorthLogging))
        {
            var entry = Entry(Sold, item, item.StackSize ?? 1, player, null, $"to {vendor?.Name}, worth {item.Value ?? 0:N0}", DateTime.UtcNow);
            entry.Snapshot = ItemSnapshots.FromLive(item);
            entries.Add(entry);
        }

        Save(entries);
    }

    /// <summary>
    /// player bought item from vendor: one a player sold it, as vendors sell those on (Vendor.UniqueItemsForSale).
    /// </summary>
    public static void RecordVendorBuy(Player player, WorldObject vendor, WorldObject item)
    {
        if (item == null || !WorthLogging(item))
        {
            return;
        }

        Save(new List<ItemTransfer> { Entry(Bought, item, item.StackSize ?? 1, null, player, $"from {vendor?.Name}", DateTime.UtcNow) });
    }

    /// <summary>
    /// player is about to salvage item. Called before it's destroyed, for the snapshot.
    /// </summary>
    public static void RecordSalvage(Player player, WorldObject item)
    {
        if (item == null || !WorthLogging(item))
        {
            return;
        }

        var material = item.MaterialType != null ? $"into {item.MaterialType} salvage" : "into salvage";
        var entry = Entry(Salvaged, item, item.StackSize ?? 1, player, null, $"{material}, workmanship {item.Workmanship ?? 0:0.#}", DateTime.UtcNow);
        entry.Snapshot = ItemSnapshots.FromLive(item);
        Save(new List<ItemTransfer> { entry });
    }

    /// <summary>
    /// admin made a copy of an item (@itemcopy, kind Copied) or made a gone one again (@itemrestore, Restored) for to.
    /// from is who had the original, if anyone. Always logged, whatever it's worth.
    /// </summary>
    public static void RecordCopy(string kind, Player admin, WorldObject copy, Player to, uint? fromCharacterId, string fromName, uint? fromAccountId, string original)
    {
        var entry = Entry(kind, copy, copy.StackSize ?? 1, null, to, Truncate($"by {admin?.Name}, {original}", MaxDetailsLength), DateTime.UtcNow);
        entry.FromCharacterId = fromCharacterId;
        entry.FromName = fromName;
        entry.FromAccountId = fromAccountId;
        Save(new List<ItemTransfer> { entry });
    }

    /// <summary>
    /// The lowest workmanship worth logging at each tier (1 to 8), when item_history_min_workmanship isn't set: one more
    /// than each tier's usual (WorkmanshipChance.MinWorkmanshipPerTier), about the best third of what drops.
    /// </summary>
    public const string DefaultMinWorkmanship = "2,2,3,4,5,6,7,8";

    private const int Tiers = 8;

    private static string _thresholdsText;
    private static int[] _thresholds;

    /// <summary>
    /// Whether an item is worth logging when it's sold, bought back, salvaged or dropped: a rare, or loot whose workmanship
    /// is at least item_history_min_workmanship for its tier. Money never is.
    /// </summary>
    public static bool WorthLogging(WorldObject item)
    {
        if (item == null || item.WeenieType == WeenieType.Coin || item.ItemType == ItemType.PromissoryNote)
        {
            return false;
        }

        var text = PropertyManager.GetString("item_history_min_workmanship", DefaultMinWorkmanship).Item;

        // read once each time the setting changes, not on every item sold
        var thresholds = _thresholds;
        if (thresholds == null || text != _thresholdsText)
        {
            thresholds = ParseMinWorkmanship(text);
            _thresholds = thresholds;
            _thresholdsText = text;
        }

        return WorthLogging(item.Tier, item.Workmanship, item.GetProperty(PropertyInt.RareId) != null, thresholds);
    }

    public static bool WorthLogging(int? tier, float? workmanship, bool rare, int[] thresholds)
    {
        if (rare)
        {
            return true;
        }

        // not loot: bought from a vendor, a quest's, a component
        if (workmanship == null || workmanship <= 0)
        {
            return false;
        }

        // loot with no tier on it is held to the highest bar
        var index = Math.Clamp(tier ?? Tiers, 1, Tiers) - 1;

        return workmanship.Value >= thresholds[index];
    }

    /// <summary>
    /// "2,2,3,4,5,6,7,8": the lowest workmanship worth logging at tiers 1 to 8. A missing or unreadable number is the
    /// default's for that tier, so a typo can't turn the log off or flood it.
    /// </summary>
    public static int[] ParseMinWorkmanship(string text)
    {
        var defaults = DefaultMinWorkmanship.Split(',').Select(int.Parse).ToArray();
        var parts = (text ?? "").Split(',');
        var thresholds = new int[Tiers];

        for (var i = 0; i < Tiers; i++)
        {
            thresholds[i] = i < parts.Length && int.TryParse(parts[i].Trim(), out var value) && value >= 0 ? value : defaults[i];
        }

        return thresholds;
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

    /// <summary>
    /// Deletes old entries. Runs with the first write after the server starts, then once a day.
    /// </summary>
    private static void Prune(int shortDays, int allDays)
    {
        try
        {
            var deleted = DatabaseManager.Shard.BaseDatabase.PruneItemTransfers(ShortLivedKinds, shortDays, allDays);

            if (deleted > 0)
            {
                _log.Information("[ITEMS] Deleted {Count} old item history entries.", deleted);
            }
        }
        catch (Exception ex)
        {
            _log.Warning(ex, "[ITEMS] Couldn't delete old item history entries.");
        }
        finally
        {
            Interlocked.Exchange(ref _pruning, 0);
        }
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

        var prune = DateTime.UtcNow >= _nextPruneUtc && Interlocked.CompareExchange(ref _pruning, 1, 0) == 0;

        if (prune)
        {
            _nextPruneUtc = DateTime.UtcNow + PruneInterval;
        }

        var shortDays = (int)Math.Max(0, PropertyManager.GetLong("item_history_destroyed_days", 30).Item);
        var allDays = (int)Math.Max(0, PropertyManager.GetLong("item_history_days", 0).Item);

        _ = Task.Run(() =>
        {
            if (prune)
            {
                Prune(shortDays, allDays);
            }

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
