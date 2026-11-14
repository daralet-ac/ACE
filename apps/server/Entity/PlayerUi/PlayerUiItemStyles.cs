using System.Linq;
using ACE.Entity;
using ACE.Server.Commands.PlayerCommands;
using ACE.Server.WorldObjects;

namespace ACE.Server.Entity.PlayerUi;

/// <summary>
/// /style for the plugin's item menus. /style works on the item last examined, which the plugin can't count on (anything
/// appraised in between would take the change), so @playerui style names the item: "style &lt;guid&gt;" lists the looks
/// it has that the player can choose, and "style &lt;guid&gt; &lt;number or name&gt;" switches it, as /style &lt;number or
/// name&gt; does (StyleCommand.Switch, which says in chat what happened), then lists them again. Only an item the player
/// carries or has equipped, as /style.
/// </summary>
public static class PlayerUiItemStyles
{
    public static PlayerUiItemStyleReply Get(Player player, string[] words)
    {
        var guid = PlayerUiCorpseData.ParseGuid(words.Length > 0 ? words[0] : null);

        if (guid == 0)
        {
            return new PlayerUiItemStyleReply(0, null, "Usage: @playerui style <item guid> [number or name]", null);
        }

        var objectGuid = new ObjectGuid(guid);
        var equippedItem = player.GetEquippedItem(objectGuid);
        var item = equippedItem ?? player.GetInventoryItem(objectGuid);

        if (item == null)
        {
            return new PlayerUiItemStyleReply(
                guid,
                null,
                "You can only switch the look of an item you carry or have equipped.",
                null
            );
        }

        if (words.Length > 1)
        {
            StyleCommand.Switch(player, item, equippedItem != null, string.Join(" ", words.Skip(1)));
        }

        var current = ItemLook.Of(item);
        var styles = ItemStyles
            .GetAvailableStyles(item, player)
            .Select(s => new PlayerUiItemStyle(s.Name, s.Look == current, s.IsLocked))
            .ToList();

        // one look is no choice: the plugin says so rather than list it
        return new PlayerUiItemStyleReply(guid, item.Name, null, styles.Count > 1 ? styles : null);
    }
}
