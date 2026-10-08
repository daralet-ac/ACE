using System;
using System.Collections.Generic;
using System.Linq;
using ACE.Entity;
using ACE.Server.Entity;
using ACE.Server.Managers;
using ACE.Server.WorldObjects;

namespace ACE.Server.Arena;

/// <summary>
/// Watching duels: /arena watch takes a player into a duel's instance, unseen, until it ends or they /arena leave.
/// A fighter who is defeated in a fellowship duel while their side fights on watches the rest of it the same way.
/// Spectators keep their own player killer status, and the arena refuses anything between them and anyone else (CheckPlayerVsPlayer).
/// </summary>
public static partial class ArenaManager
{
    /// <summary>
    /// Whether players can watch duels (arena_spectating_enabled)
    /// </summary>
    private static bool SpectatingEnabled => PropertyManager.GetBool("arena_spectating_enabled").Item;

    /// <summary>
    /// Why this player can't go to watch a duel now, or null if they can
    /// </summary>
    private static string WhyCantWatch(Player player)
    {
        if (!SpectatingEnabled)
        {
            return "Watching duels is turned off.";
        }

        if (player.RecallsDisabled)
        {
            return "You can't watch duels until you have left the training academy.";
        }

        if (player.InstanceId != Landblock.PersistentInstance)
        {
            return "You can't go to the arena from inside an instance.";
        }

        if (player.IsDead || player.IsInDeathProcess)
        {
            return "You can't watch a duel while you are dead.";
        }

        if (player.PKTimerActive)
        {
            return "You have been in a player killer battle too recently.";
        }

        if (player.Teleporting || player.IsLoggingOut || player.suicideInProgress)
        {
            return "You can't go to the arena right now.";
        }

        return null;
    }

    private static bool IsWatchable(ArenaMatch match)
    {
        return match.Instance != null
            && match.Map != null
            && match.State is ArenaMatchState.Arriving or ArenaMatchState.Countdown or ArenaMatchState.Fighting;
    }

    /// <summary>
    /// The duels that can be watched, a line each, for /arena watch
    /// </summary>
    public static List<string> DescribeWatchable()
    {
        lock (sync)
        {
            var now = UtcNow();

            return matches
                .Where(IsWatchable)
                .Select(m =>
                {
                    var sides = string.Join(
                        " vs ",
                        m.Fighters.GroupBy(f => f.Side).Select(side => string.Join(", ", side.Select(f => f.Name)))
                    );

                    var when =
                        m.State == ArenaMatchState.Fighting
                            ? $"fighting for {Math.Max(0, (now - m.FightStartedAt).TotalMinutes):N0} minute(s)"
                            : "about to begin";

                    var watching = m.Spectators.Count(s => !s.Returned);

                    return $"#{m.Id}: {sides} ({DescribeDuel(m.Scaled, m.Rated, m.SideSize)}) in {m.Map.Description}, {when}{(watching > 0 ? $", {watching} watching" : "")}";
                })
                .ToList();
        }
    }

    /// <summary>
    /// Takes a player to watch a duel, unseen: the one with this number, or the one this fighter is in
    /// </summary>
    public static void Watch(Player player, string which)
    {
        var why = WhyCantWatch(player);
        if (why != null)
        {
            player.SendMessage(why);
            return;
        }

        var guid = player.Guid.Full;

        lock (sync)
        {
            if (FindMatch(guid) != null)
            {
                player.SendMessage("You are in a duel yourself.");
                return;
            }

            if (queue.Find(guid) != null)
            {
                player.SendMessage(
                    "You can't watch a duel while you wait in the arena queue. /arena leave takes you out of it."
                );
                return;
            }

            if (FindWatched(guid) != null)
            {
                player.SendMessage("You are watching a duel already. /arena leave takes you back.");
                return;
            }

            var match = int.TryParse(which.TrimStart('#'), out var id)
                ? matches.FirstOrDefault(m => m.Id == id && IsWatchable(m))
                : matches.FirstOrDefault(m =>
                    IsWatchable(m) && m.Fighters.Any(f => f.Name.Equals(which, StringComparison.OrdinalIgnoreCase))
                );

            if (match == null)
            {
                player.SendMessage("There is no such duel going on. /arena watch lists them.");
                return;
            }

            var position = new Position(match.Map.Center ?? match.Map.Starts[0]);
            WorldObject.AdjustDungeon(position, match.Instance.Id);

            match.Spectators.Add(
                new ArenaSpectator
                {
                    Guid = guid,
                    Name = player.Name,
                    Home = new Position(player.Location),
                    SentAt = UtcNow()
                }
            );

            player.SendMessage(
                $"You are going to watch duel #{match.Id}, in {match.Map.Description}. Nobody can see you, and you can't do anything to anyone there. /arena leave takes you back."
            );

            Tell(match, $"{player.Name} has come to watch the duel.", spectators: false);

            player.EnterArenaAsSpectator(match.Instance, position);

            _log.Information("[ARENA] {Player} is watching {Match}", player.Name, match.Describe());
        }
    }

    /// <summary>
    /// The duel this player has come to watch (not one they fought in), if any
    /// </summary>
    private static ArenaMatch FindWatched(uint guid)
    {
        return matches.FirstOrDefault(m => m.GetSpectator(guid) != null);
    }

    /// <summary>
    /// A fighter has been defeated in a fellowship duel, and their side fights on: once they have finished falling, they watch the rest of it,
    /// unseen, where they fell
    /// </summary>
    private static void Spectate(ArenaMatch match, ArenaFighter fighter, Player player, double delaySeconds)
    {
        fighter.Spectating = true;

        player.SpectateAfterArenaDefeat(
            match.Instance.Id,
            delaySeconds,
            () =>
            {
                lock (sync)
                {
                    return !fighter.Returned;
                }
            }
        );

        player.SendMessage(
            "Your side fights on. You will watch the rest of the duel, unseen. /arena leave takes you back now."
        );
    }

    /// <summary>
    /// Whoever has come to watch and has gone (logged out, recalled, gone through a portal) can be seen again.
    /// Whoever never got there is given up on.
    /// </summary>
    private static void UpdateSpectators(ArenaMatch match, DateTime now)
    {
        foreach (var spectator in match.Spectators.Where(s => !s.Returned))
        {
            var player = PlayerManager.GetOnlinePlayer(spectator.Guid);

            if (player == null)
            {
                spectator.Returned = true;
                continue;
            }

            if (!spectator.Arrived)
            {
                if (match.InInstance(player) && !player.Teleporting)
                {
                    spectator.Arrived = true;
                }
                else if (now >= spectator.SentAt + ArriveTime)
                {
                    spectator.Returned = true;
                    OnPlayer(player, () => player.StopArenaSpectating(reveal: !player.Teleporting));
                }

                continue;
            }

            if (!match.InInstance(player))
            {
                spectator.Returned = true;
                OnPlayer(
                    player,
                    () =>
                        player.StopArenaSpectating(
                            reveal: player.InstanceId == Landblock.PersistentInstance && !player.Teleporting
                        )
                );
            }
        }
    }

    /// <summary>
    /// Takes someone who came to watch back to where they were, after a delay, and makes them visible again
    /// </summary>
    private static void SendSpectatorHome(ArenaMatch match, ArenaSpectator spectator, Player player, double delaySeconds)
    {
        if (spectator.Returned)
        {
            return;
        }

        spectator.Returned = true;

        // they keep their own status: it is put back as it is
        player.ReturnFromArena(
            spectator.Home,
            player.PlayerKillerStatus,
            player.LastPkAttackTimestamp,
            match.Instance?.Id ?? Landblock.PersistentInstance,
            delaySeconds
        );
    }

    /// <summary>
    /// The duel is over, or off: everyone who came to watch it is told, and taken back
    /// </summary>
    private static void SendSpectatorsHome(ArenaMatch match, string message, double delaySeconds)
    {
        foreach (var spectator in match.Spectators.Where(s => !s.Returned).ToList())
        {
            var player = PlayerManager.GetOnlinePlayer(spectator.Guid);

            if (player == null)
            {
                spectator.Returned = true;
                continue;
            }

            player.SendMessage($"{message} You will be taken back in {delaySeconds:N0} seconds.");
            SendSpectatorHome(match, spectator, player, delaySeconds);
        }
    }
}
