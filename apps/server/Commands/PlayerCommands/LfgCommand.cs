using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Lfg;
using ACE.Server.Network;

namespace ACE.Server.Commands.PlayerCommands;

/// <summary>
/// /lfgroup and /lfmore: looking for group. The client keeps /lfg and /clfg for the LFG channel, and /party, /g, /group,
/// /fellow, /fellows and /fellowship for fellowship chat, so those never reach the server. See LfgManager and docs/lfg.md.
/// </summary>
public class LfgCommand
{
    [CommandHandler(
        "lfgroup",
        AccessLevel.Player,
        CommandHandlerFlag.RequiresWorld,
        0,
        "Look for a group to join.",
        LfgManager.Usage
    )]
    public static void HandleLfGroup(Session session, params string[] parameters)
    {
        LfgManager.HandleCommand(session.Player, lfm: false, parameters);
    }

    [CommandHandler(
        "lfmore",
        AccessLevel.Player,
        CommandHandlerFlag.RequiresWorld,
        0,
        "Look for more players for your fellowship.",
        LfgManager.Usage
    )]
    public static void HandleLfMore(Session session, params string[] parameters)
    {
        LfgManager.HandleCommand(session.Player, lfm: true, parameters);
    }
}
