using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ACE.Database.Models.Shard;
using ACE.Entity.Enum;
using ACE.Server.Managers;
using ACE.Server.WorldObjects;

namespace ACE.Server.Entity.PlayerUi;

/// <summary>
/// What @playerui social sends, for the plugin's Social tab: the player's friends, squelches and allegiance, and the
/// fellowship and Appear Offline options, and the actions behind its buttons (@playerui friend, squelch, allegiance and
/// fellowcreate). The actions are the game's own handlers, the ones the client's panels reach through their own messages,
/// so every check they make still applies: the plugin sends words instead because the client's messages carrying a name
/// can't be sent from a plugin.
/// </summary>
public static class PlayerUiSocialData
{
    public static PlayerUiSocial GetSocial(Player player)
    {
        return new PlayerUiSocial(
            player.GetAppearOffline(),
            player.GetCharacterOption(CharacterOption.IgnoreFellowshipRequests),
            player.GetCharacterOption(CharacterOption.AutomaticallyAcceptFellowshipRequests),
            player.GetCharacterOption(CharacterOption.ShareFellowshipExpAndLuminance),
            player.GetCharacterOption(CharacterOption.ShareFellowshipLoot),
            GetFriends(player),
            GetSquelches(player),
            GetAllegiance(player)
        );
    }

    private static List<PlayerUiFriend> GetFriends(Player player)
    {
        var friends = new List<PlayerUiFriend>();

        foreach (var entry in player.Character.GetFriends(player.CharacterDatabaseLock))
        {
            var friend = PlayerManager.FindByGuid(entry.FriendId);

            if (friend == null)
            {
                continue;
            }

            // one who appears offline is offline to their friends, as the game's own friends list shows them
            var online = PlayerManager.GetOnlinePlayer(entry.FriendId);
            friends.Add(new PlayerUiFriend(entry.FriendId, friend.Name, online != null && !online.GetAppearOffline()));
        }

        return friends.OrderByDescending(f => f.Online).ThenBy(f => f.Name).ToList();
    }

    /// <summary>
    /// Character and account squelches, by the character's name: an account squelch is shown by the character it was made
    /// with (or the account's latest), never by the account's name.
    /// </summary>
    private static List<PlayerUiSquelch> GetSquelches(Player player)
    {
        var squelches = player.SquelchManager?.Squelches;

        if (squelches == null)
        {
            return [];
        }

        return squelches
            .CharactersPlus.Select(entry => new PlayerUiSquelch(entry.Key, entry.Value.PlayerName, entry.Value.Account))
            .OrderBy(s => s.Name)
            .ToList();
    }

    private static PlayerUiAllegiance GetAllegiance(Player player)
    {
        var node = player.AllegianceNode;

        if (!player.HasAllegiance || node == null)
        {
            return null;
        }

        var monarch = node.IsMonarch ? null : Member(node.Monarch, 0);
        var patron = node.Patron == null ? null : Member(node.Patron, player.AllegianceXPGenerated);

        var vassals = (node.Vassals?.Values ?? Enumerable.Empty<AllegianceNode>())
            .Select(vassal => Member(vassal, vassal.Player?.AllegianceXPGenerated ?? 0))
            .Where(vassal => vassal != null)
            .OrderBy(vassal => vassal.Name)
            .ToList();

        return new PlayerUiAllegiance(
            player.Allegiance?.AllegianceName,
            node.Rank,
            node.TotalFollowers,
            player.AllegianceXPGenerated,
            player.AllegianceXPReceived,
            monarch,
            patron,
            vassals
        );
    }

    private static PlayerUiAllegianceMember Member(AllegianceNode node, ulong xpGenerated)
    {
        var member = node?.Player;

        if (member == null)
        {
            return null;
        }

        var online = PlayerManager.GetOnlinePlayer(member.Guid.Full) != null;
        return new PlayerUiAllegianceMember(member.Guid.Full, member.Name, member.Level ?? 0, node.Rank, online, xpGenerated);
    }

    /// <summary>
    /// @playerui friend add &lt;name&gt; | remove &lt;guid&gt;. Returns what's wrong with the request, or null once it's handed on.
    /// </summary>
    public static string Friend(Player player, string[] words)
    {
        var action = Word(words, 0);

        switch (action)
        {
            case "add":
            {
                var name = Rest(words, 1);

                if (name == null)
                {
                    return "Whose name should be added to your friends?";
                }

                player.HandleActionAddFriend(name);
                return null;
            }
            case "remove":
            {
                var guid = ParseGuid(Word(words, 1));

                if (guid == 0)
                {
                    return "Which friend should be removed?";
                }

                player.HandleActionRemoveFriend(guid);
                return null;
            }
            default:
                return "Use @playerui friend add <name> or remove <guid>.";
        }
    }

    /// <summary>
    /// @playerui squelch character|account &lt;name&gt; | unsquelch &lt;guid&gt;. Unsquelching lifts an account squelch too: the
    /// plugin shows the two together, as the client's own list does.
    /// </summary>
    public static string Squelch(Player player, string[] words)
    {
        var action = Word(words, 0);

        switch (action)
        {
            case "character":
            case "account":
            {
                var name = Rest(words, 1);

                if (name == null)
                {
                    return "Whose name should be squelched?";
                }

                if (action == "account")
                {
                    player.SquelchManager.HandleActionModifyAccountSquelch(true, name);
                }
                else
                {
                    player.SquelchManager.HandleActionModifyCharacterSquelch(true, 0, name, ChatMessageType.AllChannels);
                }

                return null;
            }
            case "remove":
            {
                var guid = ParseGuid(Word(words, 1));

                if (guid == 0)
                {
                    return "Which squelch should be lifted?";
                }

                var existing = player
                    .SquelchManager.Squelches.CharactersPlus.Where(entry => entry.Key == guid)
                    .Select(entry => entry.Value)
                    .FirstOrDefault();

                if (existing == null)
                {
                    return "That character isn't squelched.";
                }

                if (existing.Account)
                {
                    player.SquelchManager.HandleActionModifyAccountSquelch(false, existing.PlayerName);
                }

                if (player.SquelchManager.Squelches.Characters.ContainsKey(guid))
                {
                    player.SquelchManager.HandleActionModifyCharacterSquelch(false, guid, null, ChatMessageType.AllChannels);
                }

                return null;
            }
            default:
                return "Use @playerui squelch character|account <name> or remove <guid>.";
        }
    }

    /// <summary>
    /// @playerui allegiance swear &lt;guid&gt; | break | kick &lt;guid&gt;. Swearing walks to the patron and asks them, as the
    /// game's own Swear does; breaking is from your patron, kicking from one of your own vassals.
    /// </summary>
    public static string Allegiance(Player player, string[] words)
    {
        var action = Word(words, 0);

        switch (action)
        {
            case "swear":
            {
                var guid = ParseGuid(Word(words, 1));

                if (guid == 0)
                {
                    return "Select the player you'd like to swear allegiance to.";
                }

                player.HandleActionSwearAllegiance(guid);
                return null;
            }
            case "break":
            {
                if (player.PatronId == null)
                {
                    return "You have no patron.";
                }

                player.HandleActionBreakAllegiance(player.PatronId.Value);
                return null;
            }
            case "kick":
            {
                var guid = ParseGuid(Word(words, 1));
                var vassal = guid == 0 ? null : PlayerManager.FindByGuid(guid);

                if (vassal == null || vassal.PatronId != player.Guid.Full)
                {
                    return "That character isn't your vassal.";
                }

                player.HandleActionBreakAllegiance(guid);
                return null;
            }
            default:
                return "Use @playerui allegiance swear <guid>, break, or kick <guid>.";
        }
    }

    /// <summary>
    /// @playerui fellowcreate &lt;0|1&gt; &lt;name&gt;: the Create Fellowship button, 1 to share XP. The game's own handler trusts
    /// the client not to ask while already in a fellowship (its panel has no button then), so that's checked here.
    /// </summary>
    public static string CreateFellowship(Player player, string[] words)
    {
        var name = Rest(words, 1);

        if (name == null)
        {
            return "Give your fellowship a name.";
        }

        if (player.Fellowship != null)
        {
            return "You're already in a fellowship.";
        }

        player.FellowshipCreate(name, Word(words, 0) == "1");
        return null;
    }

    private static string Word(string[] words, int index)
    {
        return words.Length > index ? words[index].ToLowerInvariant() : null;
    }

    /// <summary>The words from index on, as one name (names can have spaces), or null when there are none.</summary>
    private static string Rest(string[] words, int index)
    {
        if (words.Length <= index)
        {
            return null;
        }

        var text = string.Join(" ", words.Skip(index)).Trim();
        return text.Length == 0 ? null : text;
    }

    private static uint ParseGuid(string text)
    {
        if (text == null)
        {
            return 0;
        }

        var parsed = text.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            ? uint.TryParse(text[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var guid)
            : uint.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out guid);

        return parsed ? guid : 0;
    }
}
