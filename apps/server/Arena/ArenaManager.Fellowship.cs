using System;
using System.Collections.Generic;
using System.Linq;
using ACE.Entity.Enum;
using ACE.Server.Managers;
using ACE.Server.WorldObjects;

namespace ACE.Server.Arena;

/// <summary>
/// Fellowship duels: a whole fellowship against another of the same size. The leader challenges another fellowship, or puts theirs in
/// the queue, and everyone on both sides has to say yes. In the arena each fellowship starts together, nobody can harm their own side,
/// and they can heal and buff each other. A side is beaten when everyone on it has been defeated, has given up or has left.
/// </summary>
public static partial class ArenaManager
{
    /// <summary>
    /// Whether this player can take their fellowship into the arena: they lead it, and it has someone else in it.
    /// Null if they can, with everyone who is in it (them first).
    /// </summary>
    private static string WhyCantBringFellowship(Player leader, out List<Player> members)
    {
        members = null;

        var fellowship = leader.Fellowship;

        if (fellowship == null)
        {
            return "You are not in a fellowship.";
        }

        if (fellowship.FellowshipLeaderGuid != leader.Guid.Full)
        {
            return "Only the leader of your fellowship can take it into the arena.";
        }

        members = fellowship
            .GetFellowshipMembers()
            .Values.OrderBy(p => p == leader ? 0 : 1)
            .ThenBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (members.Count < 2)
        {
            return "Your fellowship has nobody else in it.";
        }

        return null;
    }

    /// <summary>
    /// Why the players of a fellowship can't fight, or null if they all can
    /// </summary>
    private static string WhyCantFellowshipDuel(IEnumerable<Player> members, Player leader, bool scaled)
    {
        foreach (var member in members)
        {
            var self = member == leader;
            var why = WhyCantDuel(member, self) ?? (scaled ? WhyCantScale(member, self) : null);

            if (why != null)
            {
                return why;
            }
        }

        return null;
    }

    /// <summary>
    /// The leader of a fellowship challenges another fellowship of the same size, by naming anybody in it
    /// </summary>
    public static void ChallengeFellowship(Player challenger, string targetName, bool scaled = false, bool rated = true)
    {
        var why = WhyCantBringFellowship(challenger, out var ours);
        if (why != null)
        {
            challenger.SendMessage(why);
            return;
        }

        var target = PlayerManager.GetOnlinePlayer(targetName);

        if (target == null)
        {
            challenger.SendMessage($"There is nobody called {targetName} online.");
            return;
        }

        if (target.Fellowship == null)
        {
            challenger.SendMessage($"{target.Name} is not in a fellowship.");
            return;
        }

        if (target.Fellowship == challenger.Fellowship)
        {
            challenger.SendMessage($"{target.Name} is in your own fellowship.");
            return;
        }

        var theirs = target.Fellowship.GetFellowshipMembers().Values.OrderBy(p => p.Name).ToList();
        var theirLeader = PlayerManager.GetOnlinePlayer(target.Fellowship.FellowshipLeaderGuid) ?? target;

        if (theirs.Count != ours.Count)
        {
            challenger.SendMessage(
                $"Your fellowship has {ours.Count} in it and {theirLeader.Name}'s has {theirs.Count}. Fellowships can only duel fellowships of the same size."
            );
            return;
        }

        why = WhyCantFellowshipDuel(ours, challenger, scaled) ?? WhyCantFellowshipDuel(theirs, leader: null, scaled);
        if (why != null)
        {
            challenger.SendMessage(why);
            return;
        }

        if (theirs.Any(p => p.SquelchManager.Squelches.Contains(challenger, ChatMessageType.Tell)))
        {
            challenger.SendMessage($"{theirLeader.Name}'s fellowship is not accepting challenges from you.");
            return;
        }

        lock (sync)
        {
            var now = UtcNow();

            var busy = ours.Concat(theirs).FirstOrDefault(p => FindMatch(p.Guid.Full) != null);
            if (busy != null)
            {
                challenger.SendMessage($"{busy.Name} is already in a duel.");
                return;
            }

            var waitingWithFellowship = WhoWaitsWithAFellowship(ours.Concat(theirs));
            if (waitingWithFellowship != null)
            {
                challenger.SendMessage(waitingWithFellowship);
                return;
            }

            var turnedDown = theirs.FirstOrDefault(p =>
                challengeCooldowns.TryGetValue((challenger.Guid.Full, p.Guid.Full), out var until) && now < until
            );
            if (turnedDown != null)
            {
                challenger.SendMessage(
                    $"{turnedDown.Name} turned down your last challenge. You can challenge their fellowship again in a minute."
                );
                return;
            }

            var sameAddress = ArenaQueue.SharesAnAddress(ours.Select(AddressOf), theirs.Select(AddressOf));

            var fighters = ours.Select(p => NewFighter(p, side: 0, accepted: p == challenger))
                .Concat(theirs.Select(p => NewFighter(p, side: 1, accepted: false)))
                .ToArray();

            var match = NewMatch(
                ArenaMatchKind.Challenge,
                rated: rated && RatedChallenges && !(BlockSameAddress && sameAddress),
                scaled,
                now,
                fighters
            );

            var kind = DescribeDuel(match.Scaled, match.Rated, match.SideSize);

            foreach (var fighter in match.Fighters.Where(f => !f.Accepted))
            {
                var player = ours.Concat(theirs).First(p => p.Guid.Full == fighter.Guid);

                var question =
                    fighter.Side == 0
                        ? $"{challenger.Name} is taking your fellowship into the arena for {kind}, against {IntroduceFellowship(theirLeader.Name, theirs, match.Scaled)}.{ExplainScaling(match.Scaled)} Nobody loses anything by being defeated. Will you fight?"
                        : $"{IntroduceFellowship(challenger.Name, ours, match.Scaled)} challenges your fellowship to {kind} in the arena.{ExplainScaling(match.Scaled)} Nobody loses anything by being defeated. Will you fight?";

                if (!Ask(match, fighter, player, question))
                {
                    CallOff(
                        match,
                        $"{player.Name} has another question open, and can't be asked right now.",
                        new[] { fighter },
                        culpritsKeepTheirPlace: true
                    );
                    return;
                }
            }

            challenger.SendMessage(
                $"You challenge {theirLeader.Name}'s fellowship to {kind}{(match.Rated || !rated ? "" : " (it can't be rated)")}. Everyone has {AcceptTime.TotalSeconds:N0} seconds to answer."
            );
        }
    }

    /// <summary>
    /// The leader of a fellowship puts it in the queue, to be paired with a fellowship of the same size that wants the same kind of duel.
    /// Everyone in it is asked when one is found. Doing it again changes what it waits for, and keeps its place.
    /// </summary>
    /// <param name="levelBand">How many levels apart the two fellowships' highest levels may be at most, 0 for any. Null for the server's default.</param>
    public static void JoinQueueWithFellowship(Player leader, int? levelBand, bool scaled = false, bool rated = true)
    {
        var why = WhyCantBringFellowship(leader, out var members) ?? WhyCantFellowshipDuel(members, leader, scaled);
        if (why != null)
        {
            leader.SendMessage(why);
            return;
        }

        lock (sync)
        {
            var busy = members.FirstOrDefault(p => FindMatch(p.Guid.Full) != null);
            if (busy != null)
            {
                leader.SendMessage(
                    busy == leader ? "You are already in a duel." : $"{busy.Name} is already in a duel."
                );
                return;
            }

            // whoever was waiting on their own now waits with the fellowship; a fellowship that was waiting keeps its place
            var waiting = members.Select(p => queue.Find(p.Guid.Full)).Where(e => e != null).Distinct().ToList();
            var already = waiting.FirstOrDefault(e => e.Guid == leader.Guid.Full && e.Size > 1);

            foreach (var entry in waiting.Where(e => e != already))
            {
                queue.Remove(entry.Guid);
                TellQueued(
                    entry,
                    $"{leader.Name} has put your fellowship in the arena queue, so you wait with it now."
                );
            }

            var band = Math.Max(0, levelBand ?? DefaultLevelBand);

            var entryForFellowship = new ArenaQueueEntry
            {
                Guid = leader.Guid.Full,
                Name = leader.Name,
                Level = members.Max(p => p.Level ?? 1),
                LevelBand = band,
                Scaled = scaled,
                Rated = rated,
                Address = AddressOf(leader),
                JoinedAt = already?.JoinedAt ?? UtcNow(),
                Members = members
                    .Select(p => new ArenaQueueMember
                    {
                        Guid = p.Guid.Full,
                        Name = p.Name,
                        Level = p.Level ?? 1,
                        Address = AddressOf(p)
                    })
                    .ToList()
            };

            queue.Add(entryForFellowship);

            var bandText = band > 0 ? $", with the highest levels on each side within {band} of each other" : "";
            var kind = DescribeDuel(scaled, rated, members.Count);

            leader.SendMessage(
                already != null
                    ? $"Your fellowship is still in the arena queue, for {kind}{bandText}, and kept its place."
                    : $"Your fellowship is in the arena queue, for {kind}{bandText}. Everyone will be asked when opponents are found. /arena leave takes it out of the queue."
            );

            if (already == null)
            {
                TellQueued(
                    entryForFellowship,
                    $"{leader.Name} has put your fellowship in the arena queue, for {kind}. You will be asked when opponents are found. /arena leave takes it out of the queue.",
                    except: leader.Guid.Full
                );
            }
        }
    }
}
