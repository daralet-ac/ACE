using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ACE.Database;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.WorldObjects;
using Serilog;

namespace ACE.Server.Managers;

/// <summary>
/// Tells quest gear and quest items from everything else, for the item history, which always logs them (ItemTransferLog).
///
/// Quest gear is marked: MutableQuestItem is on it from its weenie, and stays (set to false) once its stats have rolled
/// (LootGenerationFactory.MutateQuestItem); UpgradeableQuestItem and QuestItemTinks mark it too.
///
/// Quest items aren't, so they're known by their weenie: any item a quest NPC's emotes give out (Give), take in
/// (TakeItems) or look for (InqOwnsItems), read from the world database, and any item with a quest stamp on pickup (Quest).
/// </summary>
public static class QuestItems
{
    private static readonly ILogger _log = Log.ForContext(typeof(QuestItems));

    private static readonly uint[] QuestEmoteTypes =
    {
        (uint)EmoteType.Give,
        (uint)EmoteType.TakeItems,
        (uint)EmoteType.InqOwnsItems,
    };

    /// <summary>Content can change while the server runs (@reload, imports), so the list is read again this often.</summary>
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromHours(1);

    private static volatile HashSet<uint> _questWcids = new();
    private static DateTime _nextRefreshUtc = DateTime.MinValue;
    private static int _refreshing;

    public static bool IsQuestGearOrItem(WorldObject item)
    {
        return IsQuestGear(item) || IsQuestItem(item);
    }

    public static bool IsQuestGear(WorldObject item)
    {
        return IsQuestGear(
            item.GetProperty(PropertyBool.MutableQuestItem) != null,
            item.GetProperty(PropertyBool.UpgradeableQuestItem) != null,
            item.GetProperty(PropertyInt.QuestItemTinks) != null
        );
    }

    public static bool IsQuestGear(bool hasMutableQuestItem, bool hasUpgradeableQuestItem, bool hasQuestItemTinks)
    {
        return hasMutableQuestItem || hasUpgradeableQuestItem || hasQuestItemTinks;
    }

    public static bool IsQuestItem(WorldObject item)
    {
        RefreshIfDue();

        return IsQuestItem(item.WeenieClassId, item.GetProperty(PropertyString.Quest), _questWcids);
    }

    public static bool IsQuestItem(uint wcid, string pickupQuest, IReadOnlySet<uint> questWcids)
    {
        return !string.IsNullOrEmpty(pickupQuest) || questWcids.Contains(wcid);
    }

    /// <summary>
    /// Reads the quest items' weenie classes again, off the world thread, when they're due. Until the first read is done the
    /// list is empty, and only quest gear and stamped items count.
    /// </summary>
    private static void RefreshIfDue()
    {
        if (DateTime.UtcNow < _nextRefreshUtc || Interlocked.CompareExchange(ref _refreshing, 1, 0) != 0)
        {
            return;
        }

        _nextRefreshUtc = DateTime.UtcNow + RefreshInterval;

        _ = Task.Run(() =>
        {
            try
            {
                _questWcids = DatabaseManager.World.GetEmoteItemWcids(QuestEmoteTypes);
            }
            catch (Exception ex)
            {
                _log.Warning(ex, "[ITEMS] Couldn't read which items quest NPCs give and take; trying again in an hour.");
            }
            finally
            {
                Interlocked.Exchange(ref _refreshing, 0);
            }
        });
    }
}
