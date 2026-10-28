using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ACE.Common;
using ACE.Server.Entity;
using ACE.Server.Entity.AdminUi;
using ACE.Server.Managers;
using ACE.Server.WorldObjects;
using Serilog;

namespace ACE.Server.TownEvents;

/// <summary>
/// Town events: players questing and hunting around a town stir up unrest there, and once a town's unrest passes its
/// threshold an attack on it may begin (TownEvent). Ticked on the world thread from WorldManager. The combat, death and
/// quest hooks can run on landblock threads, so they only queue their work here, and the tick does it.
/// </summary>
public static class TownEventManager
{
    private static readonly ILogger _log = Log.ForContext(typeof(TownEventManager));

    public static bool Enabled => PropertyManager.GetBool("town_events_enabled").Item;

    // --- unrest ---

    private const double KillUnrest = 1.0;
    private const double DungeonKillUnrest = 0.5;
    private const double QuestUnrest = 2.0;
    private const double AttunementUnrest = 25.0;
    private const double DecayPerMinute = 0.25;

    // each town's threshold is rolled between the base (town_events_base_threshold) and this many times it
    private const double MaxThresholdMultiplier = 2.0;

    public static double BaseThreshold => Math.Max(1, PropertyManager.GetDouble("town_events_base_threshold").Item);

    // --- triggering ---

    private const double TriggerCheckSeconds = 60;

    // the chance a minute of an attack: this at the threshold, climbing to MaxTriggerChance at double it
    private const double TriggerChance = 0.10;
    private const double MaxTriggerChance = 0.50;
    private const double CooldownSeconds = 45 * 60;
    private const int MaxConcurrentEvents = 2;

    private const double EventTickSeconds = 1.0;

    // how often guards are checked against their town's supply level: a respawned guard, or one on a landblock that just
    // loaded, is in plain chainmail until then
    private const double GuardDressSeconds = 10;

    private static readonly Dictionary<string, double> Unrest = new();
    private static readonly Dictionary<string, double> Thresholds = new();
    private static readonly Dictionary<string, double> CooldownUntil = new();

    // a town whose Supply Chest was looted: its next threshold is rolled this much higher, so the raiders, having what
    // they came for, leave it alone for longer
    private const double LootedThresholdMultiplier = 2.0;
    private static readonly HashSet<string> LootedTowns = new();

    private static readonly List<TownEvent> Events = new();

    // rewards waiting at a town's Captain of the Guard: town, then player guid. Kept in memory only, so a restart in the
    // half hour after a defense loses them.
    private record TownRewardClaim(double Mod, double ExpiresAt, string EventUid);

    private static readonly Dictionary<string, Dictionary<uint, TownRewardClaim>> Claims = new();

    private static readonly ConcurrentQueue<Action> Queue = new();

    private static double nextEventTick;
    private static double nextTriggerCheck;
    private static double nextGuardDress;
    private static double lastDecay = Time.GetUnixTime();

    // from server start, so the supply doesn't drain while the server is down
    private static double lastDrain = Time.GetUnixTime();

    public static IReadOnlyList<TownEvent> ActiveEvents => Events;

    public static void Tick()
    {
        while (Queue.TryDequeue(out var work))
        {
            try
            {
                work();
            }
            catch (Exception ex)
            {
                _log.Error(ex, "[TOWN EVENT] queued work failed");
            }
        }

        var now = Time.GetUnixTime();

        if (now >= nextEventTick)
        {
            nextEventTick = now + EventTickSeconds;

            foreach (var townEvent in Events.ToList())
            {
                try
                {
                    townEvent.Tick(now);
                }
                catch (Exception ex)
                {
                    _log.Error(ex, "[TOWN EVENT] {Town}: tick failed, stopping the event", townEvent.TownName);
                    townEvent.Stop();
                }

                if (townEvent.Phase == TownEventPhase.Ended)
                {
                    Events.Remove(townEvent);
                    CooldownUntil[townEvent.TownName] = now + CooldownSeconds;
                    Unrest[townEvent.TownName] = 0;
                    Thresholds.Remove(townEvent.TownName);
                }
            }
        }

        if (now >= nextGuardDress)
        {
            nextGuardDress = now + GuardDressSeconds;

            foreach (var town in Town.TownNames)
            {
                try
                {
                    TownGuardUpgrades.Refresh(town);
                }
                catch (Exception ex)
                {
                    _log.Error(ex, "[TOWN EVENT] {Town}: dressing the guards failed", town);
                }
            }
        }

        if (now >= nextTriggerCheck)
        {
            nextTriggerCheck = now + TriggerCheckSeconds;

            Decay(now);
            TownSupply.Drain(now - lastDrain);
            lastDrain = now;
            ExpireClaims(now);

            if (Enabled)
            {
                CheckTriggers(now);
            }
        }
    }

    // ---------------------------------------------------------------- unrest

    /// <summary>
    /// The unrest it takes for an attack: rolled once per town (and again after each attack) as a multiple of the base
    /// threshold, so changing the base counts at once, then lowered by the town's supply level, so a well-supplied town,
    /// with more to steal, is raided more often, and losing a level counts at once
    /// </summary>
    private static double Threshold(string town)
    {
        if (!Thresholds.TryGetValue(town, out var roll))
        {
            roll = ThreadSafeRandom.Next(1.0f, (float)MaxThresholdMultiplier);

            if (LootedTowns.Remove(town))
            {
                roll *= LootedThresholdMultiplier;
            }

            Thresholds[town] = roll;
        }

        return BaseThreshold * roll * TownSupply.GetThresholdMultiplier(town);
    }

    private static void AddUnrest(string town, double amount)
    {
        if (town == null || Events.Any(e => e.TownName == town))
        {
            return;
        }

        Unrest[town] = Unrest.GetValueOrDefault(town) + amount;
    }

    private static void Decay(double now)
    {
        var minutes = (now - lastDecay) / 60;
        lastDecay = now;

        foreach (var town in Unrest.Keys.ToList())
        {
            Unrest[town] = Math.Max(0, Unrest[town] - DecayPerMinute * minutes);
        }
    }

    private static void CheckTriggers(double now)
    {
        if (Events.Count >= MaxConcurrentEvents)
        {
            return;
        }

        foreach (var (town, unrest) in Unrest.ToList())
        {
            if (unrest < Threshold(town) || now < CooldownUntil.GetValueOrDefault(town) || Events.Any(e => e.TownName == town))
            {
                continue;
            }

            if (!HasEligiblePlayer(town) || ThreadSafeRandom.Next(0.0f, 1.0f) >= GetTriggerChance(unrest, Threshold(town)))
            {
                continue;
            }

            StartEvent(town);

            if (Events.Count >= MaxConcurrentEvents)
            {
                return;
            }
        }
    }

    /// <summary>
    /// The chance a minute of an attack on a town with this much unrest: none below the threshold, 10% at it, climbing in a
    /// straight line to 50% at double the threshold and staying there, so a town left to stir keeps getting likelier
    /// </summary>
    public static double GetTriggerChance(double unrest, double threshold)
    {
        if (threshold <= 0 || unrest < threshold)
        {
            return 0;
        }

        var over = (unrest - threshold) / threshold;
        return Math.Min(MaxTriggerChance, TriggerChance + (MaxTriggerChance - TriggerChance) * over);
    }

    /// <summary>
    /// Someone at the town's level (or Shrouded) is in its region, out on the map
    /// </summary>
    private static bool HasEligiblePlayer(string town)
    {
        var maxLevel = Town.GetTownMaxLevel(town);

        return PlayerManager.GetAllOnline().Any(p =>
            p.Location != null
            && Town.IsOnMap(p.Location.Cell)
            && TownGeography.GetRegion(p) == town
            && ((p.Level ?? 1) <= maxLevel || TownEvent.IsShrouded(p))
        );
    }

    private static TownEvent StartEvent(string town, bool? chestRaid = null, string startedBy = "unrest")
    {
        var townEvent = new TownEvent(town, chestRaid, startedBy);
        Events.Add(townEvent);

        _log.Information("[TOWN EVENT] {Town}: attack starting (unrest {Unrest:N0})", town, Unrest.GetValueOrDefault(town));
        return townEvent;
    }

    // ---------------------------------------------------------------- hooks (any thread)

    /// <summary>
    /// A creature died. An event attacker's killing blow is scored; any other kill by a player, of a creature at or
    /// above the nearest town's tier, stirs up unrest there (less from a dungeon)
    /// </summary>
    public static void OnCreatureDeath(Creature creature, Player killer)
    {
        var townEvent = creature.TownEvent;
        if (townEvent != null)
        {
            Queue.Enqueue(() => townEvent.OnAttackerDeath(creature, killer));
            return;
        }

        if (killer == null || creature.Location == null)
        {
            return;
        }

        var town = Town.GetNearestTown(creature);
        if (creature.GetCreatureTier() < Town.GetTownTier(town))
        {
            return;
        }

        var amount = Town.IsOnMap(creature.Location.Cell) ? KillUnrest : DungeonKillUnrest;
        Queue.Enqueue(() => AddUnrest(town, amount));
    }

    /// <summary>
    /// A player stamped a quest. Town attunement progress (Quest{Town}) stirs up a lot of unrest in that town, and other
    /// quests a little in the town whose region the player is in.
    /// </summary>
    public static void OnQuestStamped(Player player, string questName, bool townProgress)
    {
        if (player?.Location == null || questName.Contains(','))
        {
            return;
        }

        var town = Town.GetNearestTown(player);
        var amount = townProgress ? AttunementUnrest : QuestUnrest;

        Queue.Enqueue(() => AddUnrest(town, amount));
    }

    /// <summary>
    /// A player (or their pet) damaged a creature
    /// </summary>
    public static void OnDamage(Creature target, WorldObject attacker, uint amount)
    {
        var townEvent = target.TownEvent;
        if (townEvent == null || amount == 0)
        {
            return;
        }

        var player = attacker as Player ?? (attacker as CombatPet)?.P_PetOwner;
        if (player == null)
        {
            return;
        }

        Queue.Enqueue(() => townEvent.OnAttackerDamaged(target, player, amount));
    }

    /// <summary>
    /// An event attacker hit a player, for this much before armor, resistances and shields
    /// </summary>
    public static void OnPlayerHit(Player defender, Creature attacker, float unmitigated)
    {
        var townEvent = attacker.TownEvent;
        if (townEvent == null || unmitigated <= 0)
        {
            return;
        }

        Queue.Enqueue(() => townEvent.OnPlayerHit(defender, unmitigated));
    }

    /// <summary>
    /// A player's life magic restored another player's health (up to what they were missing)
    /// </summary>
    public static void OnLifeHeal(Player healer, Player target, int amount)
    {
        if (amount <= 0 || Events.Count == 0)
        {
            return;
        }

        Queue.Enqueue(() =>
        {
            foreach (var townEvent in Events)
            {
                if (townEvent.Phase is TownEventPhase.Wave or TownEventPhase.Break && townEvent.InRegion(target))
                {
                    townEvent.OnLifeHeal(healer, target, amount);
                }
            }
        });
    }

    // ---------------------------------------------------------------- reward claims (world thread)

    /// <summary>
    /// Holds a defender's reward at the town's Captain for TownCaptain.ClaimSeconds. A second defense before they claim
    /// adds to it.
    /// </summary>
    public static void AddClaim(string town, Player player, double mod, string eventUid)
    {
        if (!Claims.TryGetValue(town, out var claims))
        {
            claims = new Dictionary<uint, TownRewardClaim>();
            Claims[town] = claims;
        }

        var waiting = claims.GetValueOrDefault(player.Guid.Full)?.Mod ?? 0;
        claims[player.Guid.Full] = new TownRewardClaim(waiting + mod, Time.GetUnixTime() + TownCaptain.ClaimSeconds, eventUid);
    }

    /// <summary>
    /// Pays the player's reward waiting at this town's Captain, if they have one. Returns whether they did.
    /// </summary>
    public static bool TryClaim(Player player, string town, WorldObject captain)
    {
        if (!Claims.TryGetValue(town, out var claims)
            || !claims.TryGetValue(player.Guid.Full, out var claim)
            || claim.ExpiresAt < Time.GetUnixTime())
        {
            return false;
        }

        claims.Remove(player.Guid.Full);

        var xp = TownEvent.PayReward(player, town, claim.Mod, true, false);
        TownEventRecorder.RecordClaim(claim.EventUid, player.Guid.Full, "claimed", xp);
        TownCaptain.Tell(player, captain, $"{town} is in your debt. Go with the town's thanks.");

        _log.Information("[TOWN EVENT] {Town}: {Player} claimed their reward", town, player.Name);
        return true;
    }

    private static void ExpireClaims(double now)
    {
        foreach (var claims in Claims.Values)
        {
            foreach (var (guid, claim) in claims.ToList())
            {
                if (claim.ExpiresAt < now)
                {
                    claims.Remove(guid);
                    TownEventRecorder.RecordClaim(claim.EventUid, guid, "expired", 0);
                }
            }
        }
    }

    public static int CountClaims(string town) =>
        Claims.TryGetValue(town, out var claims) ? claims.Count(c => c.Value.ExpiresAt >= Time.GetUnixTime()) : 0;

    /// <summary>
    /// The town's Supply Chest was looted: the threshold rolled after this attack is doubled. Call on the world thread.
    /// </summary>
    public static void DelayNextAttack(string town) => LootedTowns.Add(town);

    public static void EnqueuePromptAnswer(TownEvent townEvent, uint playerGuid, bool yes) =>
        Queue.Enqueue(() => townEvent.OnPromptAnswer(playerGuid, yes));

    // ---------------------------------------------------------------- commands

    /// <summary>
    /// /towndefense join: the event in the town the player is in
    /// </summary>
    public static void HandleJoin(Player player)
    {
        Queue.Enqueue(() =>
        {
            var townEvent = Events.FirstOrDefault(e => e.Phase != TownEventPhase.Ended && TownGeography.InTownArea(player, e.TownName));

            if (townEvent == null)
            {
                player.Session.Network.EnqueueSend(
                    new Network.GameMessages.Messages.GameMessageSystemChat(
                        "There is no attack on the town you are in to defend.",
                        ACE.Entity.Enum.ChatMessageType.Broadcast
                    )
                );
                return;
            }

            townEvent.Join(player, true);
        });
    }

    /// <summary>
    /// Runs work on the world thread at the next tick (for the admin commands)
    /// </summary>
    public static void Enqueue(Action work) => Queue.Enqueue(work);

    public static string FindTown(string name) =>
        Town.TownNames.FirstOrDefault(t =>
            string.Equals(t, name, StringComparison.OrdinalIgnoreCase)
            || string.Equals(Town.GetSimplifiedTownString(t), name, StringComparison.OrdinalIgnoreCase)
        );

    /// <summary>
    /// @townevent start: begins an attack on the town now, whatever its unrest. Call on the world thread (Enqueue).
    /// </summary>
    public static string Start(string town, bool? chestRaid = null)
    {
        if (Events.Any(e => e.TownName == town))
        {
            return $"{town} is already under attack.";
        }

        if (chestRaid == true && TownSupplyChest.Find(town) == null)
        {
            return $"{town} has no Supply Chest loaded near its centre, so there's nothing for a chest raid to go for.";
        }

        StartEvent(town, chestRaid, "admin");
        return $"The attack on {town} is starting{(chestRaid == true ? ", a chest raid" : "")}. It warns the region once its creatures and roads are found.";
    }

    /// <summary>
    /// @townevent stop. Call on the world thread (Enqueue).
    /// </summary>
    public static string Stop(string town)
    {
        var townEvent = Events.FirstOrDefault(e => e.TownName == town);
        if (townEvent == null)
        {
            return $"{town} isn't under attack.";
        }

        townEvent.Stop();
        return $"The attack on {town} is called off.";
    }

    /// <summary>
    /// @townevent status. Call on the world thread (Enqueue).
    /// </summary>
    public static string Status()
    {
        var lines = new List<string> { $"Town events are {(Enabled ? "on" : "off")} (town_events_enabled). {Events.Count} under attack." };

        lines.AddRange(Events.Select(e => e.Describe()));

        foreach (var town in Claims.Keys.Where(t => CountClaims(t) > 0).OrderBy(t => t))
        {
            lines.Add($"{town}: {CountClaims(town)} rewards waiting at the Captain of the Guard.");
        }

        return string.Join("\n", lines);
    }

    /// <summary>
    /// @townevent unrest: every town's unrest against its threshold. Call on the world thread (Enqueue).
    /// </summary>
    public static string DescribeUnrest()
    {
        var now = Time.GetUnixTime();

        var lines = Unrest.OrderByDescending(kv => kv.Value / Threshold(kv.Key))
            .Select(kv =>
            {
                var cooldown = CooldownUntil.GetValueOrDefault(kv.Key) - now;
                return $"{kv.Key}: {kv.Value:N0} / {Threshold(kv.Key):N0}" + (cooldown > 0 ? $" (cooling down, {(int)(cooldown / 60)} min)" : "");
            })
            .ToList();

        return lines.Count == 0 ? "No town has any unrest yet." : string.Join("\n", lines);
    }

    /// <summary>
    /// The admin plugin's Town Events tab: every town's unrest, and the attacks going on. Call on the world thread (Enqueue).
    /// </summary>
    public static AdminUiTownEvents GetAdminUiReport()
    {
        var now = Time.GetUnixTime();

        var towns = Town.TownNames.Select(town => new AdminUiTownUnrest(
                town,
                Town.GetTownTier(town),
                Math.Round(Unrest.GetValueOrDefault(town), 1),
                Math.Round(Threshold(town)),
                (int)Math.Max(0, CooldownUntil.GetValueOrDefault(town) - now),
                Events.Any(e => e.TownName == town),
                Math.Round(GetTriggerChance(Unrest.GetValueOrDefault(town), Threshold(town)), 3),
                HasEligiblePlayer(town),
                TownSupply.GetLevel(town),
                Math.Round(TownSupply.GetSupply(town)),
                Math.Round(TownSupply.GetThreshold(town, TownSupply.GetLevel(town))),
                Math.Round(TownSupply.GetThreshold(town, Math.Min(TownSupply.GetLevel(town) + 1, TownSupply.MaxLevel))),
                Math.Round(TownSupply.GetHourlyLoss(town)),
                (int)TownSupply.GetSecondsUntilLevelDrops(town),
                (int)TownSupply.GetHoldSecondsLeft(town)
            ))
            .OrderByDescending(t => t.Unrest / t.Threshold)
            .ThenBy(t => t.Town)
            .ToList();

        var attacks = Events.Select(e => new AdminUiTownAttack(
                e.Id,
                e.TownName,
                e.Phase.ToString(),
                e.Wave,
                e.WaveCount,
                e.Family,
                e.AttackersAlive,
                e.Fellowship?.TotalMembers ?? 0,
                (int)(now - e.StartTime),
                e.PhaseSecondsLeft,
                e.Scores.Values.OrderByDescending(s => s.Damage + s.Healing + s.DamageTaken)
                    .Select(s => new AdminUiTownDefender(
                        s.Name,
                        Math.Round(s.Damage),
                        Math.Round(s.Healing),
                        Math.Round(s.DamageTaken),
                        s.KillingBlows,
                        e.Fellowship != null && e.Fellowship.FellowshipMembers.ContainsKey(s.PlayerGuid),
                        s.CreditLost,
                        s.OverLevelKills,
                        s.PromptsRefused,
                        s.KillsAfterRefusal
                    ))
                    .ToList()
            ))
            .ToList();

        return new AdminUiTownEvents(Enabled, BaseThreshold, towns, attacks);
    }

    /// <summary>
    /// @townevent unrest town amount. Call on the world thread (Enqueue).
    /// </summary>
    public static string SetUnrest(string town, double amount)
    {
        Unrest[town] = Math.Max(0, amount);
        return $"{town}'s unrest is now {Unrest[town]:N0} of {Threshold(town):N0}.";
    }

    /// <summary>
    /// @townevent supply: one town's supply in full, or every supplied town's. Call on the world thread (Enqueue).
    /// </summary>
    public static string DescribeSupply(string town = null)
    {
        if (town != null)
        {
            return DescribeTownSupply(town);
        }

        var lines = Town.TownNames.Where(t => TownSupply.GetSupply(t) > 0).OrderBy(t => t).Select(DescribeTownSupply).ToList();

        return lines.Count == 0 ? "No town has any supply." : string.Join("\n", lines);
    }

    private static string DescribeTownSupply(string town)
    {
        var level = TownSupply.GetLevel(town);
        var text = $"{town}: level {level} of {TownSupply.MaxLevel}, supply {TownSupply.GetSupply(town):N0}";

        if (level < TownSupply.MaxLevel)
        {
            text += $", next level at {TownSupply.GetThreshold(town, level + 1):N0}";
        }

        var loss = TownSupply.GetHourlyLoss(town);
        if (loss > 0)
        {
            text += $", losing {loss:N0} an hour";
        }

        var secondsLeft = TownSupply.GetSecondsUntilLevelDrops(town);
        if (level > 0)
        {
            text += $", level {level} lasts {FormatDuration(secondsLeft)}";
        }

        var hold = TownSupply.GetHoldSecondsLeft(town);
        if (hold > 0)
        {
            text += $" (held for {FormatDuration(hold)})";
        }

        return text + $". Threshold {Threshold(town):N0} unrest.";
    }

    private static string FormatDuration(double seconds)
    {
        var span = TimeSpan.FromSeconds(Math.Max(0, seconds));
        return span.TotalHours >= 1 ? $"{(int)span.TotalHours}h {span.Minutes}m" : $"{span.Minutes}m";
    }

    /// <summary>
    /// @townevent supply town amount: sets the supply outright, with no minimum hold. Call on the world thread (Enqueue).
    /// </summary>
    public static string SetSupply(string town, double amount)
    {
        TownSupply.Set(town, amount);
        return DescribeTownSupply(town);
    }
}
