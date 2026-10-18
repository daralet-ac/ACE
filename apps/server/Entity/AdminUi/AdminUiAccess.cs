using System.Collections.Generic;
using System.Linq;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Commands.PlayerCommands;

namespace ACE.Server.Entity.AdminUi;

/// <summary>
/// Who may use which part of the Daralet Admin plugin. This is the one place the levels are set: @adminui checks them before
/// sending any data, and sends them to the plugin in its hello reply so the plugin only shows the tabs, and enables the
/// buttons, that this account can use.
/// </summary>
public static class AdminUiAccess
{
    /// <summary>
    /// The lowest access level that sees each of the plugin's tabs (the plugin's tab ids)
    /// </summary>
    public static readonly IReadOnlyDictionary<string, AccessLevel> Tabs = new Dictionary<string, AccessLevel>
    {
        // moderation
        ["players"] = AccessLevel.Sentinel,
        ["fellowships"] = AccessLevel.Sentinel,
        ["arena"] = AccessLevel.Sentinel,

        // events and regions
        ["teleport"] = AccessLevel.Envoy,
        ["capstones"] = AccessLevel.Envoy,
        ["esper"] = AccessLevel.Envoy,

        // game systems
        ["instances"] = AccessLevel.Developer,

        // region controls
        ["olthoinorth"] = AccessLevel.Admin,
        ["tidemage"] = AccessLevel.Admin,
    };

    /// <summary>
    /// The lowest access level each @adminui request needs: the level of the lowest tab that shows its data
    /// </summary>
    public static readonly IReadOnlyDictionary<string, AccessLevel> Requests = new Dictionary<string, AccessLevel>
    {
        ["hello"] = AccessLevel.Sentinel,
        ["players"] = AccessLevel.Sentinel,
        ["fellowships"] = AccessLevel.Sentinel,
        ["arenas"] = Tabs["arena"],
        ["pois"] = Tabs["teleport"],
        ["capstones"] = Tabs["capstones"],
        ["olthoinorth"] = Tabs["olthoinorth"],
        ["esper"] = Tabs["esper"],
        ["instances"] = Tabs["instances"],
        ["tidemage"] = Tabs["tidemage"],
    };

    /// <summary>
    /// The commands the plugin's buttons send. Their levels are read from the commands themselves, so the plugin greys out
    /// exactly what the server would refuse.
    /// </summary>
    private static readonly string[] PluginCommands =
    {
        "boot",
        "ban",
        "gag",
        "ungag",
        "freeze",
        "unfreeze",
        "teleto",
        "teletome",
        "telereturn",
        "tele",
        "telepoi",
        "teledungeon",
        "teleloc",
        "home",
        "save",
        "instance",
        "olthoisupply",
        "modifylong",
    };

    /// <summary>
    /// The lowest access level of each command the plugin sends, plus "arena staff" for /arena watch and cancel, which
    /// /arena checks inside the command
    /// </summary>
    public static Dictionary<string, int> GetCommandLevels()
    {
        var levels = new Dictionary<string, int>();

        foreach (var name in PluginCommands)
        {
            var command = CommandManager.GetCommandByName(name).FirstOrDefault();

            if (command != null)
            {
                levels[name] = (int)command.Attribute.Access;
            }
        }

        levels["arena staff"] = (int)ArenaCommand.StaffLevel;

        return levels;
    }

    public static Dictionary<string, int> GetTabLevels()
    {
        return Tabs.ToDictionary(t => t.Key, t => (int)t.Value);
    }
}
