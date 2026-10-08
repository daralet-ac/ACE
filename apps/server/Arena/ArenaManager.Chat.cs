using ACE.Server.Entity;
using ACE.Server.Managers;
using ACE.Server.WorldObjects;

namespace ACE.Server.Arena;

/// <summary>
/// Chat in the arena. Nobody in a duel's instance (fighters and spectators alike) can talk locally or send to the global channels
/// (general, trade, LFG, roleplay, society, Olthoi), so opponents can't talk to each other and spectators can't distract the fighters.
/// They still hear all of it. Tells are refused between opponents, and from spectators to fighters. Fellowship and allegiance chat are untouched.
/// </summary>
public static partial class ArenaManager
{
    /// <summary>
    /// The duel whose instance this player is in, if any
    /// </summary>
    private static ArenaMatch MatchAt(Player player)
    {
        if (player.InstanceId == Landblock.PersistentInstance)
        {
            return null;
        }

        return InstanceManager.Get(player.InstanceId)?.Owner as ArenaMatch;
    }

    /// <summary>
    /// Why this player can't talk locally or send to a global channel, or null if they can: nobody can from inside an arena
    /// </summary>
    public static string WhyCantChat(Player player)
    {
        return MatchAt(player) == null ? null : "You can't talk here or send to chat channels while you are in the arena.";
    }

    /// <summary>
    /// Why one player can't send a tell to another, or null if they can. Opponents can't send each other tells while either of them is in
    /// the arena, and nobody who is watching a duel can send one to a fighter in it. Teammates can, and so can anybody outside the arena.
    /// </summary>
    public static string WhyCantTell(Player sender, Player target)
    {
        if (sender == target)
        {
            return null;
        }

        var match = MatchAt(target) ?? MatchAt(sender);

        if (match == null)
        {
            return null;
        }

        lock (sync)
        {
            var to = match.Get(target.Guid.Full);
            var from = match.Get(sender.Guid.Full);

            if (to != null && from != null)
            {
                return to.Side == from.Side ? null : "You can't send tells to your opponents during a duel.";
            }

            // someone who has come to watch, telling a fighter (it doesn't matter where the fighter is: they are still in the duel)
            if (to != null && match.GetSpectator(sender.Guid.Full) != null)
            {
                return "You can't send tells to the fighters while you are watching their duel.";
            }

            return null;
        }
    }
}
