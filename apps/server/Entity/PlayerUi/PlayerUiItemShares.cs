using System;
using System.Collections.Generic;
using System.Linq;
using ACE.Server.WorldObjects;

namespace ACE.Server.Entity.PlayerUi;

/// <summary>
/// Item links in chat, for the plugin's chat windows. @playerui shareitem keeps a description of one of the player's own
/// items (carried, worn, or in the bank they have open), and the plugin puts "[Name]" in the chat line, which reads as just
/// that to anyone without the plugin. Any player's plugin then asks @playerui shared about each "[...]" in what a player
/// said, by who said it and the name, and shows the item when a link is hovered. Text in brackets that nobody linked finds
/// nothing, and stays text.
///
/// What's shared is what the item's owner chose to show, as it was when they linked it: the same description the plugin
/// shows them for their own things. Nothing else about them goes with it but their name.
/// </summary>
public static class PlayerUiItemShares
{
    /// <summary>The most links one @playerui shared may name.</summary>
    public const int MaxLookup = 10;

    private static readonly SharedItemStore Store = new(capacity: 5000, keep: TimeSpan.FromDays(1));

    public static PlayerUiShareReply Share(Player player, string word)
    {
        var guid = PlayerUiCorpseData.ParseGuid(word);

        if (guid == 0)
        {
            return new PlayerUiShareReply(0, "Usage: @playerui shareitem <item guid>", null);
        }

        var item = PlayerUiCorpseData.FindOwnItem(player, guid);

        if (item == null)
        {
            return new PlayerUiShareReply(
                guid,
                "You can only link an item you carry or wear, or one in the bank you have open.",
                null
            );
        }

        var described = PlayerUiCorpseData.Describe(item);
        var slots = (int)(item.ValidLocations ?? 0);
        var shared = Store.Add(player.Name, slots, described, DateTime.UtcNow);
        return new PlayerUiShareReply(guid, null, shared);
    }

    /// <summary>A link someone else made, kept under this player's name too, so they can say it themselves.</summary>
    public static PlayerUiShareReply ShareAgain(Player player, string key)
    {
        var shared = Store.PassOn(key, player.Name, DateTime.UtcNow);

        return shared == null
            ? new PlayerUiShareReply(0, "That link is too old to pass on: links last a day.", null)
            : new PlayerUiShareReply(0, null, shared with { Key = null });
    }

    public static PlayerUiSharedLookup Lookup(IEnumerable<string> words)
    {
        var now = DateTime.UtcNow;
        var items = new List<PlayerUiSharedItem>();
        var missing = new List<string>();

        foreach (var key in words.Select(w => (w ?? "").Trim()).Where(w => w.Length > 0).Distinct().Take(MaxLookup))
        {
            var shared = Store.Find(key, now);

            if (shared != null)
            {
                items.Add(shared);
            }
            else
            {
                missing.Add(key);
            }
        }

        return new PlayerUiSharedLookup(items, missing);
    }
}
