using System;
using System.Collections.Generic;
using System.Text;

namespace ACE.Server.Entity.PlayerUi;

/// <summary>
/// The items players have linked in chat, in memory only. A link's line is only "[Name]", so everyone reads it, plugin or
/// not; what it means is found from who said it and the name, so each player has one item to a name, the latest they
/// linked. Only what that player linked can be found under their name, so nobody can pass another's item off as theirs,
/// or as anything else. A link is for the people reading chat at the time, so each is kept for a day (Keep), and only the
/// newest Capacity, so typing or scripting the command can't grow it without end.
/// </summary>
public sealed class SharedItemStore
{
    /// <summary>The longest name put in a link: chat lines are short.</summary>
    public const int MaxName = 60;

    /// <summary>The longest word a link's key can be, from the longest names and then some.</summary>
    public const int MaxKeyLength = 256;

    private sealed class Entry
    {
        public string Link;
        public PlayerUiSharedItem Shared;
        public DateTime At;
    }

    private readonly object gate = new();
    private readonly Dictionary<string, Entry> byLink = new();

    /// <summary>Links oldest first, to drop the oldest once there are Capacity. An entry replaced since is skipped.</summary>
    private readonly Queue<Entry> order = new();

    public SharedItemStore(int capacity, TimeSpan keep)
    {
        Capacity = Math.Max(1, capacity);
        Keep = keep;
    }

    public int Capacity { get; }
    public TimeSpan Keep { get; }

    public int Count
    {
        get
        {
            lock (gate)
            {
                return byLink.Count;
            }
        }
    }

    /// <summary>Keeps one of a player's items for a link, in place of any they linked with that name before.</summary>
    public PlayerUiSharedItem Add(string ownerName, int slots, PlayerUiCorpseItem item, DateTime now)
    {
        var name = LinkName(item?.Name);
        var shared = new PlayerUiSharedItem(null, name, ownerName, slots, item);
        Put(Link(ownerName, name), shared, now);
        return shared;
    }

    /// <summary>
    /// Keeps a link someone else made under a player's own name too, as a link they can pass on. Null when it's forgotten.
    /// </summary>
    public PlayerUiSharedItem PassOn(string key, string playerName, DateTime now)
    {
        var shared = Find(key, now);

        if (shared != null)
        {
            Put(Link(playerName, shared.Name), shared, now);
        }

        return shared;
    }

    /// <summary>The item a link's key names (Key: who said it and the name), or null when there's none.</summary>
    public PlayerUiSharedItem Find(string key, DateTime now)
    {
        if (!TryReadKey(key, out var link))
        {
            return null;
        }

        lock (gate)
        {
            if (!byLink.TryGetValue(link, out var entry))
            {
                return null;
            }

            if (now - entry.At >= Keep)
            {
                byLink.Remove(link);
                return null;
            }

            return entry.Shared with { Key = key };
        }
    }

    /// <summary>The name as a link has it: without what would break the brackets, and not too long.</summary>
    public static string LinkName(string name)
    {
        var clean = new StringBuilder();

        foreach (var letter in name ?? "")
        {
            if (letter != '[' && letter != ']' && !char.IsControl(letter))
            {
                clean.Append(letter);
            }
        }

        var text = clean.ToString().Trim();

        if (text.Length > MaxName)
        {
            text = text.Substring(0, MaxName).TrimEnd();
        }

        return text.Length > 0 ? text : "Item";
    }

    /// <summary>
    /// A link as one word for a command: who said it and the name, in base64 with - and _ (ChatItemLink.Key in the plugin
    /// makes these).
    /// </summary>
    public static string Key(string ownerName, string name)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(Link(ownerName, name)))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    /// <summary>A staff character's + isn't always in what the plugin reads, and case isn't a different link.</summary>
    private static string Link(string ownerName, string name)
    {
        return $"{(ownerName ?? "").TrimStart('+')}\n{name}".ToLowerInvariant();
    }

    private static bool TryReadKey(string key, out string link)
    {
        link = null;

        if (string.IsNullOrEmpty(key) || key.Length > MaxKeyLength)
        {
            return false;
        }

        var base64 = key.Replace('-', '+').Replace('_', '/');
        base64 = base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=');

        try
        {
            link = Encoding.UTF8.GetString(Convert.FromBase64String(base64)).ToLowerInvariant();
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private void Put(string link, PlayerUiSharedItem shared, DateTime now)
    {
        lock (gate)
        {
            Prune(now);
            var entry = new Entry { Link = link, Shared = shared, At = now };
            byLink[link] = entry;
            order.Enqueue(entry);

            while (byLink.Count > Capacity && order.Count > 0)
            {
                Remove(order.Dequeue());
            }
        }
    }

    /// <summary>Drops what's older than Keep, oldest first.</summary>
    private void Prune(DateTime now)
    {
        while (order.Count > 0 && now - order.Peek().At >= Keep)
        {
            Remove(order.Dequeue());
        }
    }

    private void Remove(Entry entry)
    {
        if (byLink.TryGetValue(entry.Link, out var current) && ReferenceEquals(current, entry))
        {
            byLink.Remove(entry.Link);
        }
    }
}
