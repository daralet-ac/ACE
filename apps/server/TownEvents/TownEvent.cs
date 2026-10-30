using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using ACE.Common;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Entity;
using ACE.Server.Entity.Actions;
using ACE.Server.Factories;
using ACE.Server.Managers;
using ACE.Server.Network.GameEvent.Events;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.WorldObjects;
using ACE.Server.WorldObjects.Patrol;
using Serilog;

namespace ACE.Server.TownEvents;

public enum TownEventPhase
{
    /// <summary>
    /// Reading the town's creature pool and finding its roads in
    /// </summary>
    Preparing,

    /// <summary>
    /// The scouts' warning is out; the first wave comes when it runs down
    /// </summary>
    Warning,

    Wave,

    /// <summary>
    /// The pause between waves
    /// </summary>
    Break,

    Ended
}

/// <summary>
/// One attack on a town (the Raid archetype): a warning, waves of creatures from the town's region marching in along its
/// roads with a leader in the last one, and rewards for the defenders if the town holds. TownEventManager ticks it on the
/// world thread; the combat hooks reach it through the manager's queue.
/// </summary>
public sealed class TownEvent
{
    private static readonly ILogger _log = Log.ForContext<TownEvent>();

    private static int nextId;

    public readonly int Id = ++nextId;

    public readonly string TownName;

    /// <summary>
    /// The town's tier counted from 1 (Holtburg is 1), the archetype tier of a raider scaled to the town
    /// </summary>
    public readonly int TierFromOne;

    public readonly int MaxLevel;

    public TownEventPhase Phase { get; private set; } = TownEventPhase.Preparing;

    public int Wave { get; private set; }

    public readonly int WaveCount;

    public readonly double StartTime = Time.GetUnixTime();

    public bool Succeeded { get; private set; }

    public string Family { get; private set; }

    // --- for the town event log (TownEventRecorder) ---

    /// <summary>
    /// The attack's own id, unique across restarts
    /// </summary>
    public readonly string Uid = Guid.NewGuid().ToString("N");

    /// <summary>
    /// unrest or admin
    /// </summary>
    public readonly string StartedBy;

    public readonly int SupplyLevelStart;

    /// <summary>
    /// held, fell, looted, stopped or cancelled, once it's over
    /// </summary>
    public string Outcome { get; private set; }

    public double EndTime { get; private set; }

    public string MinorFamilyName => minorFamily == null ? null : FamilyName(minorFamily);

    /// <summary>
    /// The roads in, by compass direction
    /// </summary>
    public string Roads { get; private set; }

    public int AttackersSpawned { get; private set; }

    /// <summary>
    /// What each defender got when the Warlord fell, and how many got it
    /// </summary>
    public int WarlordPyreals { get; private set; }

    public int WarlordPaid { get; private set; }

    /// <summary>
    /// Who killed the Warlord (TownDefender), or null if it didn't fall
    /// </summary>
    public string WarlordKilledBy { get; private set; }

    /// <summary>
    /// The damage dealt to attackers and the attackers killed by the players (and their pets), the Captain and the other
    /// guards, for how much of the fight the guards do on their own. Players count as they do in their scores: members of
    /// the Town Defense at the town's level. OtherKills are by anything else, or nothing (an attacker dying with no
    /// damager known).
    /// </summary>
    public double PlayerDamage { get; private set; }

    public double CaptainDamage { get; private set; }
    public double GuardDamage { get; private set; }
    public int PlayerKills { get; private set; }
    public int CaptainKills { get; private set; }
    public int GuardKills { get; private set; }
    public int OtherKills { get; private set; }

    /// <summary>
    /// The swings and damage between the raiders and the players, guards and Captain
    /// </summary>
    public readonly TownCombatStats Combat = new();

    /// <summary>
    /// This attack is after the town's Supply Chest: its attackers march on to it, and the attack fails if they loot it
    /// </summary>
    public bool ChestRaid { get; private set; }

    /// <summary>
    /// How much of the chest has been looted, 0 to 1
    /// </summary>
    public double ChestLooted => ChestRaid ? Math.Min(1, lootProgress / ChestLootSeconds) : 0;

    /// <summary>
    /// The Town Defense fellowship, once someone has joined
    /// </summary>
    public Fellowship Fellowship { get; private set; }

    /// <summary>
    /// The town's Captain of the Guard while alive, shown in the Town Defense fellowship as its leader, with their health
    /// </summary>
    public Creature Captain { get; private set; }

    private double nextCaptainSearch;

    // where the Captain stands, and the attacker it's been sent after
    private Position captainPost;
    private Creature captainTarget;

    // what the members were last sent about the Captain: the fellowship and Captain shown, and the Captain's vitals
    private (Fellowship Fellowship, uint Captain) captainShown;
    private (uint Health, uint MaxHealth, uint Stamina, uint MaxStamina, uint Mana, uint MaxMana) captainVitalsSent;

    public readonly Dictionary<uint, TownDefenseScore> Scores = new();

    public readonly HashSet<Creature> Attackers = new();

    // the rallied guards and the Captain, for the attackers to go after (read from the landblock threads)
    private volatile Creature[] guardTargets = Array.Empty<Creature>();

    /// <summary>
    /// The guards and the Captain the attackers fight when they see them, ahead of marching on (Monster.GetAttackTargets)
    /// </summary>
    public IReadOnlyList<Creature> GuardTargets => guardTargets;

    private void RefreshGuardTargets() =>
        guardTargets = rallied.Keys.Append(Captain).Where(g => g != null).Distinct().ToArray();

    public int AttackersAlive => Attackers.Count(a => !a.IsDead && a.CurrentLandblock != null);

    /// <summary>
    /// Seconds until the warning or the break between waves runs out, 0 in the other phases
    /// </summary>
    public int PhaseSecondsLeft =>
        Phase is TownEventPhase.Warning or TownEventPhase.Break ? (int)System.Math.Max(0, phaseEndTime - Time.GetUnixTime()) : 0;

    // --- timing ---

    private const double PrepareTimeoutSeconds = 30;
    private const double CaptainSearchSeconds = 5;

    // the Captain goes after attackers this close to its post, and gives up on one that gets this far from it
    private const float CaptainEngageRange = 30f;
    private const float CaptainLeashRange = 45f;
    private const double WarningSeconds = 120;
    private const int WarningMinutes = (int)(WarningSeconds / 60);

    // reminders to those warned, this many seconds before the attackers arrive
    private static readonly int[] WarningReminderSeconds = { 60, 30 };
    private int warningRemindersSent;
    private const double BreakSeconds = 25;
    private const double WaveTimeoutSeconds = 6 * 60;
    private const double EventTimeoutSeconds = 30 * 60;

    // --- Town Defense membership ---

    private const double JoinGraceSeconds = 120;
    private const double InactiveWarnSeconds = 60;
    private const double RemovalWarningSeconds = 60;
    private const double AwayRemovalSeconds = 60;

    // members may go this far past the farthest point attackers spawn from, chasing them, and a member who hit or healed
    // in the fight this recently is never away, wherever the fight took them
    private const float BattleAreaMargin = 50f;
    private const double FightingKeepsPresentSeconds = 15;

    // --- waves ---

    private const int WaveBaseBudget = 10;

    // the Warlord's health grows by this share for each member of the Town Defense past the first, when it spawns
    public const double LeaderHealthPerDefender = 0.2;

    // a wave's share of casters, when its families have both casters and fighters, and of creatures from the main family
    private const double CasterShare = 0.35;

    // every wave has a war caster and a support caster (TownCasterKits), and beyond those this share of its attackers is made
    // one or the other
    private const double ExtraCasterShare = 0.15;
    private const double MainFamilyShare = 0.7;

    // --- the Supply Chest ---

    // the chance an attack on a town with a Supply Chest goes for it: every one, for now, as an attack without an objective
    // stops at the town centre once its road runs out
    private const double ChestRaidChance = 1.0;

    // one attacker this close to the chest loots it at a time, and it's gone once they have spent this long at it between
    // them
    private const float ChestLootRadius = 4f;
    private const double ChestLootSeconds = 60;

    // a member or a guard this close to the looter pulls it off the chest to fight; the looter is chosen and checked this
    // often, rummages in the chest this often, and the region is told it was driven off once no one has looted for this long
    private const float ChestDefendRange = 12f;
    private const double ChestLooterCheckSeconds = 0.5;
    private const double ChestLootMotionSeconds = 3;
    private const double ChestDrivenOffSeconds = 3;
    private const int MaxAlive = 40;
    private const float SpawnJitter = 4f;

    // --- guards ---

    private const double RallyAwarenessRange = 60;

    private double phaseEndTime;
    private double waveStartTime;
    private double lastTick = Time.GetUnixTime();

    private Task<List<TownPoolCreature>> poolTask;

    // the raid's theme (TownRaidThemes), for a town that has them; without one the raid comes from the town's own pool
    private TownRaidTheme theme;
    private Task<TownRaidRoster> rosterTask;

    private List<TownPoolCreature> mainFamily;
    private List<TownPoolCreature> minorFamily;
    private List<TownPoolCreature> leaders = new();

    // the levels the town's attackers fight at: a theme's creatures from other tiers are scaled into them
    private readonly int minAttackerLevel;
    private readonly int maxAttackerLevel;
    private List<TownEntryRoute> routes;
    private List<TownEntryRoute> activeRoutes;

    // how far from the centre members can be without being away: the town area, or out to the roads attackers come in on
    private float battleAreaRadius = TownGeography.TownAreaRadius;

    private readonly HashSet<uint> promptOpen = new();
    private readonly HashSet<uint> inTownLastTick = new();
    private readonly Dictionary<uint, double> joinedAt = new();
    private readonly Dictionary<uint, double> idleSeconds = new();
    private readonly Dictionary<uint, double> awaySince = new();
    private readonly HashSet<uint> idleWarned = new();
    private readonly HashSet<uint> leftTownDefense = new();

    private readonly Dictionary<Creature, (double? Awareness, bool? ForceWalk)> rallied = new();

    // the wave's Warlord, while it lives
    private Creature leader;

    private readonly bool? forceChestRaid;
    private Position chestPosition;
    private WorldObject chestObject;
    private double lootProgress;
    private bool lootStartWarned;
    private int lootStepsWarned;

    // the attacker looting the chest, whether it's being looted (for the region's messages), and when
    private Creature chestLooter;
    private bool lootingNow;
    private double lastLootTime;
    private double nextLooterCheck;
    private double nextLootMotion;

    /// <param name="startedBy">unrest or admin, for the log</param>
    /// <param name="chestRaid">true makes it a chest raid if the town has a chest, false never, null rolls (ChestRaidChance)</param>
    public TownEvent(string town, bool? chestRaid = null, string startedBy = "unrest")
    {
        TownName = town;
        forceChestRaid = chestRaid;
        StartedBy = startedBy;
        SupplyLevelStart = TownSupply.GetLevel(town);
        TierFromOne = (int)Town.GetTownTier(town) + 1;
        MaxLevel = Town.GetTownMaxLevel(town);
        WaveCount = ThreadSafeRandom.Next(2, 4);

        // the roads come from the town's guards, so its centre has to be loaded
        LandblockManager.GetLandblock(Town.GetTownCenterLandblock(town), true);

        minAttackerLevel = Math.Max(1, Town.GetTownMinLevel(town));
        maxAttackerLevel = Math.Max(minAttackerLevel, Math.Min(MaxLevel, minAttackerLevel + 24));

        var themes = TownRaidThemes.ForTown(town);
        if (themes.Count > 0)
        {
            theme = themes[ThreadSafeRandom.Next(0, themes.Count - 1)];
            rosterTask = TownCreaturePool.GetRoster(theme);
        }

        poolTask = TownCreaturePool.Get(town);
        phaseEndTime = StartTime + PrepareTimeoutSeconds;
    }

    // ---------------------------------------------------------------- who's who

    public static bool IsShrouded(Player player) => player.EnchantmentManager.HasSpell((int)SpellId.Shrouded);

    /// <summary>
    /// Over the town's level band and not Shrouded: no credit, no rewards, and their kills don't count
    /// </summary>
    public bool IsOverLevel(Player player) => (player.Level ?? 1) > MaxLevel && !IsShrouded(player);

    public bool InRegion(Player player) => TownGeography.GetRegion(player) == TownName;

    /// <summary>
    /// Whether a member is where the fight is: outdoors or in a building, out to the roads attackers come in on this attack
    /// </summary>
    public bool InBattleArea(Player player) =>
        player?.Location != null && Town.IsOnMap(player.Location.Cell) && TownGeography.DistanceToCenter(TownName, player.Location) <= battleAreaRadius;

    public bool IsMember(Player player) => Fellowship != null && player.Fellowship == Fellowship;

    private TownDefenseScore Score(Player player)
    {
        if (!Scores.TryGetValue(player.Guid.Full, out var score))
        {
            score = new TownDefenseScore { PlayerGuid = player.Guid.Full };
            Scores[player.Guid.Full] = score;
        }

        score.Name = player.Name;
        score.Level = player.Level ?? 1;
        return score;
    }

    /// <summary>
    /// The members of the Town Defense, which a wave and its Warlord are sized for when they spawn
    /// </summary>
    public int Defenders => Fellowship?.TotalMembers ?? 0;

    /// <summary>
    /// The Warlord's health multiplier, from its variant's and the Town Defense's size
    /// </summary>
    public static double GetLeaderHealthMod(double variantHealth, int defenders) =>
        variantHealth * (1 + LeaderHealthPerDefender * Math.Max(0, defenders - 1));

    private static void Tell(Player player, string text) =>
        player.Session.Network.EnqueueSend(new GameMessageSystemChat(text, ChatMessageType.Broadcast));

    /// <summary>
    /// Tells everyone in the region
    /// </summary>
    private void TellRegion(string text)
    {
        foreach (var player in PlayerManager.GetAllOnline().Where(InRegion))
        {
            Tell(player, text);
        }
    }

    // ---------------------------------------------------------------- the tick

    public void Tick(double now)
    {
        var dt = now - lastTick;
        lastTick = now;

        if (Phase == TownEventPhase.Ended)
        {
            return;
        }

        if (now - StartTime > EventTimeoutSeconds)
        {
            End(false, "The attackers withdraw, but they have done their damage.");
            return;
        }

        switch (Phase)
        {
            case TownEventPhase.Preparing:
                TickPreparing(now);
                break;

            case TownEventPhase.Warning:
                if (now >= phaseEndTime)
                {
                    BeginDefense(now);
                    break;
                }

                if (warningRemindersSent < WarningReminderSeconds.Length && phaseEndTime - now <= WarningReminderSeconds[warningRemindersSent])
                {
                    var seconds = WarningReminderSeconds[warningRemindersSent++];
                    TellWarned($"Attackers will arrive at {TownName} in about {(seconds >= 60 ? $"{seconds / 60} minute{(seconds >= 120 ? "s" : "")}" : $"{seconds} seconds")}!");
                }
                break;

            case TownEventPhase.Wave:
                TickWave(now);
                break;

            case TownEventPhase.Break:
                if (now >= phaseEndTime)
                {
                    StartWave(now);
                }
                break;
        }

        if (ChestRaid && Phase == TownEventPhase.Wave)
        {
            TickChest(dt);

            if (Phase == TownEventPhase.Ended)
            {
                return;
            }
        }

        // the prompt goes out with the scouts' warning (SendWarning), so arrivals are watched for from then on
        if (Phase is TownEventPhase.Warning or TownEventPhase.Wave or TownEventPhase.Break)
        {
            TickPlayers(now, dt);
            TickCaptain(now);
        }
    }

    /// <summary>
    /// Keeps the Captain of the Guard in the Town Defense fellowship's panel: the whole fellowship is sent again when the
    /// Captain appears or is gone (dead, or never placed) or the fellowship is founded again, and the Captain's vitals
    /// whenever they change. The Captain is only shown: the fellowship itself is still made of players.
    /// </summary>
    private void TickCaptain(double now)
    {
        if (Captain is not { IsDead: false, CurrentLandblock: not null })
        {
            // the Captain can be killed: its generator brings it back 5 minutes later, and the search below finds it again
            if (Captain is { IsDead: true })
            {
                TellRegion($"{TownName}'s Captain of the Guard has fallen!");
            }

            Captain = null;

            if (now >= nextCaptainSearch)
            {
                Captain = TownCaptain.Find(TownName);
                nextCaptainSearch = now + CaptainSearchSeconds;
                RefreshGuardTargets();

                // where it stands, to fight around and go back to: taken while it's still there, not mid-fight
                if (Captain is { TownCaptainOnDuty: false })
                {
                    captainPost = new Position(Captain.Location);
                }
            }
        }

        CaptainFight();

        if (Fellowship == null || Fellowship.TotalMembers == 0)
        {
            captainShown = default;
            return;
        }

        var members = Fellowship.GetFellowshipMembers().Values.ToList();
        var shown = (Fellowship, Captain?.Guid.Full ?? 0);

        if (shown != captainShown)
        {
            captainShown = shown;
            captainVitalsSent = CaptainVitals();

            foreach (var member in members)
            {
                member.Session.Network.EnqueueSend(new GameEventFellowshipFullUpdate(member.Session));
            }
            return;
        }

        if (Captain == null)
        {
            return;
        }

        var vitals = CaptainVitals();
        if (vitals == captainVitalsSent)
        {
            return;
        }

        captainVitalsSent = vitals;

        foreach (var member in members.Where(m => m.FellowshipPanelOpen))
        {
            member.Session.Network.EnqueueSend(
                new GameEventFellowshipUpdateFellow(member.Session, Captain, Fellowship.ShareLoot, FellowUpdateType.Vitals)
            );
        }
    }

    /// <summary>
    /// Sends the Captain after the nearest attacker that comes within CaptainEngageRange of its post, and keeps it on that
    /// one while it lives and stays within CaptainLeashRange; with none, the Captain goes back to its post
    /// </summary>
    private void CaptainFight()
    {
        if (Captain == null || captainPost == null)
        {
            captainTarget = null;
            return;
        }

        bool Near(Creature attacker, float range) =>
            attacker is { IsDead: false, CurrentLandblock: not null, Location: not null }
            && attacker.Location.SquaredDistanceTo(captainPost) <= range * range;

        var target = Near(captainTarget, CaptainLeashRange)
            ? captainTarget
            : Attackers.Where(a => Near(a, CaptainEngageRange)).MinBy(a => a.Location.SquaredDistanceTo(Captain.Location));

        if (target == captainTarget)
        {
            return;
        }

        captainTarget = target;

        var captain = Captain;
        var post = captainPost;
        var chain = new ActionChain();
        chain.AddAction(captain, () =>
        {
            if (target != null)
            {
                captain.TownCaptainEngage(target, post);
            }
            else
            {
                captain.TownCaptainStandDown();
            }
        });
        chain.EnqueueChain();
    }

    private void CaptainStandDown()
    {
        captainTarget = null;

        var captain = Captain;
        if (captain == null)
        {
            return;
        }

        var chain = new ActionChain();
        chain.AddAction(captain, captain.TownCaptainStandDown);
        chain.EnqueueChain();
    }

    private (uint, uint, uint, uint, uint, uint) CaptainVitals() =>
        Captain == null
            ? default
            : (Captain.Health.Current, Captain.Health.MaxValue, Captain.Stamina.Current, Captain.Stamina.MaxValue, Captain.Mana.Current, Captain.Mana.MaxValue);

    private void TickPreparing(double now)
    {
        routes ??= FindRoutes();

        if (poolTask.IsCompleted && (rosterTask?.IsCompleted ?? true) && routes.Count > 0)
        {
            var pool = poolTask.Result;
            var useTheme = rosterTask != null && UseRoster(rosterTask.Result);

            // a chest raid is decided first, since only the humanoid races go for the chest (every theme's raiders do)
            var chest = TownSupplyChest.Find(TownName);
            if (chest?.Location != null && (forceChestRaid ?? ThreadSafeRandom.Next(0.0f, 1.0f) < ChestRaidChance))
            {
                if (useTheme || pool.Any(c => c.Humanoid))
                {
                    ChestRaid = true;
                    chestPosition = new Position(chest.Location);
                    chestObject = chest;
                }
                else
                {
                    _log.Information("[TOWN EVENT] {TownName}: no humanoid creatures in its pool to raid the Supply Chest", TownName);
                }
            }

            if (!useTheme && !PickFamilies(ChestRaid ? pool.Where(c => c.Humanoid).ToList() : pool))
            {
                Cancel($"{TownName} has no creatures in its pool");
                return;
            }

            activeRoutes = routes.OrderBy(_ => ThreadSafeRandom.Next(0, 1000)).Take(Math.Min(routes.Count, ThreadSafeRandom.Next(2, 3))).ToList();

            var farthestEntry = activeRoutes.Max(r => TownGeography.DistanceToCenter(TownName, r.Entry));
            battleAreaRadius = Math.Max(TownGeography.TownAreaRadius, farthestEntry + BattleAreaMargin);

            Phase = TownEventPhase.Warning;
            phaseEndTime = now + WarningSeconds;

            SendWarning();
            PromptTown();
            return;
        }

        // the guards may not have spawned yet: look again next tick
        if (routes.Count == 0)
        {
            routes = null;
        }

        if (now >= phaseEndTime)
        {
            Cancel(poolTask.IsCompleted ? $"no roads into {TownName} were found from its guards" : $"{TownName}'s creature pool took too long");
        }
    }

    /// <summary>
    /// The raid's families from its theme: the raiders, pets and leaders nearest the town's tier. False if none of its
    /// raiders could be read, so the raid comes from the town's pool instead.
    /// </summary>
    private bool UseRoster(TownRaidRoster roster)
    {
        var raiders = TownRaidThemes.Nearest(roster.Raiders, minAttackerLevel, maxAttackerLevel);
        if (raiders.Count == 0)
        {
            _log.Warning("[TOWN EVENT] {TownName}: raid theme {Theme} has no raiders; using the town's creature pool", TownName, roster.Theme.Name);
            return false;
        }

        var pets = TownRaidThemes.Nearest(roster.Pets, minAttackerLevel, maxAttackerLevel);

        mainFamily = raiders;
        minorFamily = pets.Count > 0 ? pets : null;
        leaders = TownRaidThemes.Nearest(roster.Leaders, minAttackerLevel, maxAttackerLevel);
        Family = roster.Theme.Name;
        return true;
    }

    private List<TownEntryRoute> FindRoutes() => TownGeography.FindEntryRoutes(TownName, TownGeography.FindGuards(TownName));

    /// <summary>
    /// One family of creature for the event (drudges, tuskers, undead...), and a second, smaller one when the first lacks
    /// either casters or fighters, so every attack brings both magic and weapons; otherwise sometimes a second anyway.
    /// A family with both is picked first when there is one.
    /// </summary>
    private bool PickFamilies(List<TownPoolCreature> pool)
    {
        var families = pool.GroupBy(c => c.CreatureType).Select(g => g.ToList()).ToList();
        if (families.Count == 0)
        {
            return false;
        }

        static bool Mixed(List<TownPoolCreature> family) => family.Any(c => c.Caster) && family.Any(c => !c.Caster);

        // beasts never lead a raid: only raider families can be the main one, and a beast family only comes as the second
        var raiderFamilies = families.Where(f => TownRaidThemes.RaiderTypes.Contains(f[0].CreatureType)).ToList();
        if (raiderFamilies.Count == 0)
        {
            return false;
        }

        var mixed = raiderFamilies.Where(Mixed).ToList();
        var choices = mixed.Count > 0 ? mixed : raiderFamilies;
        mainFamily = choices[ThreadSafeRandom.Next(0, choices.Count - 1)];

        var others = families.Where(f => f != mainFamily).ToList();

        if (!Mixed(mainFamily))
        {
            // the main family fights only one way: bring in a family that fights the other
            var lacksCasters = !mainFamily.Any(c => c.Caster);
            var fillers = others.Where(f => f.Any(c => c.Caster == lacksCasters)).ToList();
            if (fillers.Count > 0)
            {
                minorFamily = fillers[ThreadSafeRandom.Next(0, fillers.Count - 1)];
            }
        }
        else if (others.Count > 0 && ThreadSafeRandom.Next(0.0f, 1.0f) < 0.3f)
        {
            minorFamily = others[ThreadSafeRandom.Next(0, others.Count - 1)];
        }

        Family = FamilyName(mainFamily);
        return true;
    }

    /// <summary>
    /// The next creature of a wave: a caster CasterShare of the time when there are both, and from the main family most
    /// of the time
    /// </summary>
    private TownPoolCreature PickCreature()
    {
        var all = mainFamily.Concat(minorFamily ?? Enumerable.Empty<TownPoolCreature>()).ToList();

        var casters = all.Where(c => c.Caster).ToList();
        var fighters = all.Where(c => !c.Caster).ToList();
        var wantCaster = casters.Count > 0 && (fighters.Count == 0 || ThreadSafeRandom.Next(0.0f, 1.0f) < CasterShare);
        var kind = wantCaster ? casters : fighters;

        var fromMain = kind.Where(c => mainFamily.Contains(c)).ToList();
        var fromMinor = kind.Except(fromMain).ToList();
        var list = fromMain.Count > 0 && (fromMinor.Count == 0 || ThreadSafeRandom.Next(0.0f, 1.0f) < MainFamilyShare) ? fromMain : fromMinor;

        return list[ThreadSafeRandom.Next(0, list.Count - 1)];
    }

    private static string FamilyName(List<TownPoolCreature> family)
    {
        var type = family[0].CreatureType;
        return type == CreatureType.Invalid ? family[0].Name : $"{type.ToString().ToLowerInvariant()} raiders";
    }

    // ---------------------------------------------------------------- warning and start

    /// <summary>
    /// The scouts' warning: to everyone in the town's region, and to every at-level player anywhere who is attuned to it
    /// </summary>
    private void SendWarning()
    {
        var directions = string.Join(" and ", activeRoutes.Select(r => Compass(r.Entry)).Distinct());
        Roads = directions;
        var text = $"Scouts report {Family} massing on the {directions} road{(activeRoutes.Count > 1 ? "s" : "")} outside {TownName}! "
            + (ChestRaid ? "They're after the guards' Supply Chest! " : "")
            + $"Attackers will arrive in about {WarningMinutes} minute{(WarningMinutes == 1 ? "" : "s")}.";

        TellWarned(text);

        _log.Information("[TOWN EVENT] {TownName}: {Family} attack warned, {Waves} waves from {Roads}{Chest}", TownName, Family, WaveCount, directions, ChestRaid ? ", a chest raid" : "");
    }

    /// <summary>
    /// Tells those the scouts warn: everyone in the town's region, and every at-level player anywhere attuned to it
    /// </summary>
    private void TellWarned(string text)
    {
        var attuned = $"Attuned{TownName}";

        foreach (var player in PlayerManager.GetAllOnline())
        {
            if (InRegion(player) || (player.Level ?? 1) <= MaxLevel && player.QuestManager.HasQuest(attuned))
            {
                Tell(player, text);
            }
        }
    }

    private string Compass(Position entry)
    {
        var center = Town.GetTownCenterGlobal(TownName) ?? Vector2.Zero;
        var global = entry.ToGlobal();
        var angle = Math.Atan2(global.Y - center.Y, global.X - center.X) * 180 / Math.PI;

        // 0 = east, counter-clockwise
        string[] names = { "eastern", "north-eastern", "northern", "north-western", "western", "south-western", "southern", "south-eastern" };
        var index = (int)Math.Round(((angle + 360) % 360) / 45) % 8;
        return names[index];
    }

    /// <summary>
    /// The Town Defense opens with the warning, so defenders can group up before the first wave
    /// </summary>
    private void PromptTown()
    {
        TellRegion($"Join the Town Defense of {TownName} to earn a share of the town's thanks.");

        foreach (var player in PlayerManager.GetAllOnline().Where(p => TownGeography.InTownArea(p, TownName)))
        {
            Prompt(player);
            inTownLastTick.Add(player.Guid.Full);
        }
    }

    private void BeginDefense(double now)
    {
        TellRegion($"{TownName} is under attack!");

        // whoever joined during the warning gets the new member's grace from now, to reach the fighting
        foreach (var guid in joinedAt.Keys.ToList())
        {
            joinedAt[guid] = now;
        }

        RallyGuards();
        StartWave(now);
    }

    // ---------------------------------------------------------------- waves

    private void StartWave(double now)
    {
        Wave++;
        Phase = TownEventPhase.Wave;
        waveStartTime = now;

        // a bigger Town Defense, and a better-supplied town, draw bigger attacks: both checked as each wave spawns, so a
        // level lost or a defender joining mid-attack counts at once
        var budget = (int)Math.Round((WaveBaseBudget + Defenders) * TownSupply.GetWaveBudgetMultiplier(TownName));
        var spawned = SpawnWave(budget, Wave == WaveCount);

        TellRegion(
            Wave == WaveCount
                ? $"The final wave reaches {TownName}, and its warlord with it!"
                : $"Wave {Wave} of {WaveCount} reaches {TownName}!"
        );

        _log.Information(
            "[TOWN EVENT] {TownName}: wave {Wave}/{WaveCount}, {Defenders} defenders, budget {Budget}, {Spawned} attackers",
            TownName,
            Wave,
            WaveCount,
            Defenders,
            budget,
            spawned
        );
    }

    private void TickWave(double now)
    {
        Attackers.RemoveWhere(a => a.IsDead || a.CurrentLandblock == null);

        if (Attackers.Count > 0 && now - waveStartTime < WaveTimeoutSeconds)
        {
            return;
        }

        // stragglers that never reached anyone
        foreach (var attacker in Attackers.ToList())
        {
            attacker.Destroy();
        }
        Attackers.Clear();

        if (Wave >= WaveCount)
        {
            End(true, null);
            return;
        }

        TellRegion($"The defenders of {TownName} hold! The next wave comes in {(int)BreakSeconds} seconds.");

        Phase = TownEventPhase.Break;
        phaseEndTime = now + BreakSeconds;
    }

    private enum Variant
    {
        Standard,
        Raider,
        Elite,
        Leader
    }

    private static (double Health, double Lethality, string Suffix) VariantMods(Variant variant) =>
        variant switch
        {
            Variant.Raider => (1.5, 1.25, "Raider"),
            Variant.Elite => (2.0, 1.5, "Elite"),
            Variant.Leader => (4.0, 1.5, "Warlord"),
            _ => (1.0, 1.0, null)
        };

    /// <summary>
    /// Spends the wave's budget on attackers: each costs its archetype toughness x lethality, times its variant's health
    /// and lethality multipliers, so tougher variants come in smaller numbers
    /// </summary>
    private int SpawnWave(double budget, bool withLeader)
    {
        var spawned = 0;
        var hasWarCaster = false;
        var hasSupportCaster = false;

        void CountCaster(Creature attacker)
        {
            var role = TownCasterKits.GetNaturalRole(attacker);
            hasWarCaster |= role == TownCasterRole.War || attacker.TownEventCasterRole == TownCasterRole.War;
            hasSupportCaster |= role == TownCasterRole.Support;
        }

        if (withLeader)
        {
            var leaderBase = leaders.Count > 0
                ? leaders[ThreadSafeRandom.Next(0, leaders.Count - 1)]
                : mainFamily.OrderByDescending(c => c.Level).ThenByDescending(c => c.BaseCost).First();
            var leaderSpawn = Spawn(leaderBase, Variant.Leader);
            if (leaderSpawn != null)
            {
                spawned++;
                CountCaster(leaderSpawn);
            }
        }

        var cheapest = mainFamily.Concat(minorFamily ?? Enumerable.Empty<TownPoolCreature>()).Min(c => c.BaseCost);

        while (budget >= cheapest * 0.5 && spawned < MaxAlive)
        {
            var creature = PickCreature();

            var roll = ThreadSafeRandom.Next(0.0f, 1.0f);
            var variant = roll < 0.1f && Wave > 1 ? Variant.Elite : roll < 0.4f ? Variant.Raider : Variant.Standard;

            var mods = VariantMods(variant);
            var cost = creature.BaseCost * mods.Health * mods.Lethality;

            // too dear for what's left: try a standard one instead
            if (cost > budget && variant != Variant.Standard)
            {
                variant = Variant.Standard;
                cost = creature.BaseCost;
            }

            if (cost > budget && spawned > 0)
            {
                break;
            }

            budget -= cost;

            var casterRole = PickCasterRole(hasWarCaster, hasSupportCaster);

            var attacker = Spawn(creature, variant, role: casterRole);
            if (attacker != null)
            {
                spawned++;
                CountCaster(attacker);
            }
        }

        return spawned;
    }

    /// <summary>
    /// The caster role for a wave's next attacker: a war caster and then a support caster until the wave has one of each,
    /// then ExtraCasterShare of the time either
    /// </summary>
    private static TownCasterRole PickCasterRole(bool hasWarCaster, bool hasSupportCaster)
    {
        if (!hasWarCaster)
        {
            return TownCasterRole.War;
        }

        if (!hasSupportCaster)
        {
            return TownCasterRole.Support;
        }

        if (ThreadSafeRandom.Next(0.0f, 1.0f) >= ExtraCasterShare)
        {
            return TownCasterRole.None;
        }

        return ThreadSafeRandom.Next(0.0f, 1.0f) < 0.5f ? TownCasterRole.War : TownCasterRole.Support;
    }

    private Creature Spawn(
        TownPoolCreature creature,
        Variant variant,
        TownEntryRoute route = null,
        TownCasterRole role = TownCasterRole.None
    )
    {
        if (WorldObjectFactory.CreateNewWorldObject(creature.Wcid) is not Creature attacker)
        {
            return null;
        }

        route ??= activeRoutes[ThreadSafeRandom.Next(0, activeRoutes.Count - 1)];

        var jitterX = (float)ThreadSafeRandom.Next(-SpawnJitter, SpawnJitter);
        var jitterY = (float)ThreadSafeRandom.Next(-SpawnJitter, SpawnJitter);
        var location = PatrolComponent.BuildOutdoorPosition(route.Entry, jitterX, jitterY, Landblock.PersistentInstance);

        var mods = VariantMods(variant);

        // a theme's creature from another tier fights at the town's: the archetype system sets its stats from these
        var level = TownRaidThemes.ScaleLevel(creature.Level, minAttackerLevel, maxAttackerLevel, variant == Variant.Leader, ThreadSafeRandom.Next);
        if (level != creature.Level)
        {
            attacker.Level = level;
            attacker.Tier = TierFromOne;

            // and casts its spells at the town's spell level
            TownCasterKits.ScaleSpellbook(attacker, TownGuardUpgrades.GetSpellLevel(Town.GetTownTier(TownName)));
        }

        attacker.TownEvent = this;
        attacker.TownEventHealthMod = variant == Variant.Leader ? GetLeaderHealthMod(mods.Health, Defenders) : mods.Health;
        attacker.TownEventLethalityMod = mods.Lethality;

        // a richer town's attackers are stronger: the Warlord most, Elites and Raiders a share of it
        var supplyLevel = TownSupply.GetLevel(TownName);
        attacker.TownEventArchetypeMods = variant switch
        {
            Variant.Leader => TownArchetypeMods.ForLeader(supplyLevel),
            Variant.Elite => TownArchetypeMods.ForElite(supplyLevel),
            Variant.Raider => TownArchetypeMods.ForRaider(supplyLevel),
            _ => TownArchetypeMods.None
        };
        attacker.TownEventMarch = route.Waypoints.Skip(1).ToList();

        attacker.TownEventCasterRole = role;
        TownCasterKits.Apply(attacker, role, TownGuardUpgrades.GetSpellLevel(Town.GetTownTier(TownName)));

        // a chest raid marches on from the centre to the chest
        if (ChestRaid)
        {
            attacker.TownEventMarch.Add(new Position(chestPosition));
        }

        // a theme may rename a creature it borrows (TownRaidTheme.Names)
        var name = mods.Suffix != null ? $"{creature.Name} {mods.Suffix}" : creature.Name;
        if (attacker.Name != name)
        {
            attacker.Name = name;
        }

        // a look-alike: none of the creature's own emotes, so a borrowed boss or quest creature stamps no quests, gives no
        // rewards and counts toward no kill tallies
        attacker.Biota.PropertiesEmote = new List<ACE.Entity.Models.PropertiesEmote>();

        // the rewards come from the defence, not from looting the waves
        attacker.DeathTreasureType = null;

        attacker.Location = location;
        attacker.Home = new Position(location);

        if (!attacker.EnterWorld())
        {
            attacker.Destroy();
            return null;
        }

        Attackers.Add(attacker);
        AttackersSpawned++;

        if (variant == Variant.Leader)
        {
            leader = attacker;
        }

        return attacker;
    }

    // ---------------------------------------------------------------- guards

    /// <summary>
    /// The guards see attackers from much farther away, and run everywhere, while the town is under attack
    /// </summary>
    private void RallyGuards()
    {
        foreach (var guard in TownGeography.FindGuards(TownName))
        {
            if (rallied.ContainsKey(guard))
            {
                continue;
            }

            rallied[guard] = (guard.VisualAwarenessRange, guard.GetProperty(PropertyBool.PatrolForceWalk));

            guard.SetVisualAwarenessRange(RallyAwarenessRange);
            guard.SetProperty(PropertyBool.PatrolForceWalk, false);
        }

        RefreshGuardTargets();
    }

    private void StandDownGuards()
    {
        foreach (var (guard, (awareness, forceWalk)) in rallied)
        {
            if (guard.IsDestroyed)
            {
                continue;
            }

            guard.SetVisualAwarenessRange(awareness);

            if (forceWalk.HasValue)
            {
                guard.SetProperty(PropertyBool.PatrolForceWalk, forceWalk.Value);
            }
            else
            {
                guard.RemoveProperty(PropertyBool.PatrolForceWalk);
            }
        }

        rallied.Clear();
        guardTargets = Array.Empty<Creature>();
    }

    // ---------------------------------------------------------------- the Town Defense

    /// <summary>
    /// Asks the player to join the Town Defense, unless they're in it or already being asked. The question can be given,
    /// for a player asked because of something they did (attacking a raider while outside it).
    /// </summary>
    public void Prompt(Player player, string question = null)
    {
        if (Phase == TownEventPhase.Ended || IsMember(player))
        {
            return;
        }

        // a fake player (@fakeplayer) says yes by itself
        if (player.IsFakePlayer)
        {
            Join(player, false);
            return;
        }

        if (!promptOpen.Add(player.Guid.Full))
        {
            return;
        }

        question ??= Phase is TownEventPhase.Preparing or TownEventPhase.Warning
            ? $"{TownName} is about to be attacked! Join the Town Defense?"
            : $"{TownName} is under attack! Join the Town Defense?";

        if (player.Fellowship != null)
        {
            question += "\n\nJoining takes you out of your current fellowship.";
        }

        if (IsOverLevel(player))
        {
            question += $"\n\nYou are above {TownName}'s level. Joining Shrouds you, so you fight at the town's level.";
        }

        var guid = player.Guid.Full;
        var confirmation = new Confirmation_TownDefense(player.Guid, yes => TownEventManager.EnqueuePromptAnswer(this, guid, yes));

        if (!player.ConfirmationManager.EnqueueSend(confirmation, question))
        {
            // something else is being asked: try again later
            promptOpen.Remove(guid);
        }
    }

    public void OnPromptAnswer(uint playerGuid, bool yes)
    {
        promptOpen.Remove(playerGuid);

        var player = PlayerManager.GetOnlinePlayer(playerGuid);
        if (player == null || Phase == TownEventPhase.Ended)
        {
            return;
        }

        if (yes)
        {
            Join(player, false);
            return;
        }

        if (IsOverLevel(player))
        {
            Score(player).PromptsRefused++;
        }
    }

    /// <summary>
    /// Puts the player in the Town Defense: from the prompt, or from /towndefense join (which needs them in the town area)
    /// </summary>
    public void Join(Player player, bool fromCommand)
    {
        if (Phase == TownEventPhase.Ended)
        {
            return;
        }

        if (IsMember(player))
        {
            if (fromCommand)
            {
                Tell(player, $"You are already defending {TownName}.");
            }
            return;
        }

        if (fromCommand ? !TownGeography.InTownArea(player, TownName) : !InRegion(player))
        {
            Tell(player, $"You have to be in {TownName} to join its defense.");
            return;
        }

        if (player.Fellowship != null)
        {
            player.FellowshipQuit(false);
        }

        if (Fellowship == null || Fellowship.TotalMembers == 0)
        {
            player.FellowshipCreate($"Defenders of {TownName}", true);
            Fellowship = player.Fellowship;

            if (Fellowship == null)
            {
                return;
            }

            Fellowship.TownDefense = this;
        }
        else
        {
            var inviter =
                PlayerManager.GetOnlinePlayer(Fellowship.FellowshipLeaderGuid)
                ?? Fellowship.GetFellowshipMembers().Values.FirstOrDefault();

            if (inviter == null)
            {
                return;
            }

            Fellowship.AddConfirmedMember(inviter, player, true);
        }

        if (!IsMember(player))
        {
            return;
        }

        var now = Time.GetUnixTime();
        joinedAt[player.Guid.Full] = now;
        idleSeconds.Remove(player.Guid.Full);
        idleWarned.Remove(player.Guid.Full);
        awaySince.Remove(player.Guid.Full);
        Score(player).LastActive = now;

        if (IsOverLevel(player))
        {
            player.ApplyShroud($"Joining the Town Defense of {TownName} Shrouds you, so you fight at the town's level.");
        }

        Tell(player, $"You join the defenders of {TownName}.");
    }

    /// <summary>
    /// Takes the player out of the Town Defense, and tells them how to get back in. Their scores are kept, for if they
    /// rejoin: only members are rewarded.
    /// </summary>
    private void Remove(Player player, string reason)
    {
        if (!IsMember(player))
        {
            return;
        }

        Fellowship.QuitFellowship(player, false);
        Forget(player.Guid.Full);

        Tell(
            player,
            $"{reason} You have been removed from the Town Defense of {TownName}, and won't be rewarded unless you rejoin with /towndefense join in {TownName}."
        );
    }

    private void Forget(uint guid)
    {
        leftTownDefense.Add(guid);
        joinedAt.Remove(guid);
        idleSeconds.Remove(guid);
        idleWarned.Remove(guid);
        awaySince.Remove(guid);
    }

    /// <summary>
    /// Arrivals get the prompt; members who stop fighting, or leave the town area, are warned and then removed
    /// </summary>
    private void TickPlayers(double now, double dt)
    {
        var inTown = new HashSet<uint>();

        foreach (var player in PlayerManager.GetAllOnline())
        {
            if (!TownGeography.InTownArea(player, TownName))
            {
                continue;
            }

            inTown.Add(player.Guid.Full);

            if (!inTownLastTick.Contains(player.Guid.Full))
            {
                Prompt(player);
            }
        }

        inTownLastTick.Clear();
        inTownLastTick.UnionWith(inTown);

        if (Fellowship == null)
        {
            return;
        }

        // anyone who left on their own since the last tick
        foreach (var guid in joinedAt.Keys.ToList())
        {
            var player = PlayerManager.GetOnlinePlayer(guid);
            if (player == null || !IsMember(player))
            {
                Forget(guid);
            }
        }

        foreach (var member in Fellowship.GetFellowshipMembers().Values.ToList())
        {
            var guid = member.Guid.Full;

            if (!joinedAt.ContainsKey(guid))
            {
                joinedAt[guid] = now;
            }

            // away from the fight (a lifestone elsewhere, a portal, a recall)
            if (!member.IsDead && !InBattleArea(member) && !(Scores.TryGetValue(guid, out var fought) && now - fought.LastActive <= FightingKeepsPresentSeconds))
            {
                if (!awaySince.ContainsKey(guid))
                {
                    awaySince[guid] = now;
                    Tell(member, $"Return to {TownName} within {(int)AwayRemovalSeconds} seconds or you will be removed from the Town Defense.");
                }
                else if (now - awaySince[guid] >= AwayRemovalSeconds)
                {
                    Remove(member, "You left the town.");
                    continue;
                }
            }
            else
            {
                awaySince.Remove(guid);
            }

            // not fighting: counted only while a wave is on, after the grace a new member gets to reach the fighting. A fake
            // player (@fakeplayer) never fights, so it isn't counted.
            if (Phase != TownEventPhase.Wave || member.IsDead || member.IsFakePlayer || now - joinedAt[guid] < JoinGraceSeconds)
            {
                continue;
            }

            var score = Score(member);
            var idle = now - score.LastActive <= dt + 0.5 ? 0 : idleSeconds.GetValueOrDefault(guid) + dt;
            idleSeconds[guid] = idle;

            if (idle < InactiveWarnSeconds)
            {
                idleWarned.Remove(guid);
                continue;
            }

            if (idleWarned.Add(guid))
            {
                Tell(member, $"Join the fight within {(int)RemovalWarningSeconds} seconds or you will be removed from the Town Defense.");
            }
            else if (idle >= InactiveWarnSeconds + RemovalWarningSeconds)
            {
                Remove(member, "You stopped fighting.");
            }
        }
    }

    // ---------------------------------------------------------------- combat (from the manager's queue)

    /// <summary>
    /// Who a defender is, for the guards' share of the fight: player, captain, guard or other
    /// </summary>
    private string TownDefender(WorldObject defender) =>
        defender switch
        {
            Player => "player",
            Creature c when c == Captain || TownCaptain.IsCaptain(c) => "captain",
            Creature c when rallied.ContainsKey(c) || c is { HasPatrol: true, TownEvent: null } => "guard",
            _ => "other"
        };

    /// <summary>
    /// Which side of the fight something is on: raider (this event's attacker), player (or their pet), captain, guard or
    /// other
    /// </summary>
    private string Side(WorldObject wo) =>
        wo switch
        {
            Creature c when c.TownEvent == this => "raider",
            CombatPet => "player",
            _ => TownDefender(wo)
        };

    /// <summary>
    /// A melee or missile attack between a raider and a player, guard or the Captain, either way
    /// </summary>
    public void OnSwing(WorldObject attacker, WorldObject defender, bool evaded, bool blocked, bool parried, double raw, double mitigated)
    {
        var key = $"{Side(attacker)}>{Side(defender)}";
        if (TownCombatStats.Tracked(key))
        {
            Combat.AddSwing(key, evaded, blocked, parried, raw, mitigated);
        }
    }

    /// <summary>
    /// Damage done between a raider and a player, guard or the Captain, either way, by any means
    /// </summary>
    public void OnDealt(WorldObject attacker, WorldObject target, double amount)
    {
        var key = $"{Side(attacker)}>{Side(target)}";
        if (TownCombatStats.Tracked(key))
        {
            Combat.AddDealt(key, amount);
        }
    }

    /// <summary>
    /// A creature that isn't a player's damaged an attacker: a guard, the Captain, or another attacker
    /// </summary>
    public void OnAttackerDamagedByCreature(Creature attacker, Creature damager, double amount)
    {
        switch (TownDefender(damager))
        {
            case "captain":
                CaptainDamage += amount;
                break;
            case "guard":
                GuardDamage += amount;
                break;
        }
    }

    public void OnAttackerDamaged(Creature attacker, Player player, double amount)
    {
        // only the Town Defense can harm attackers (Player.CheckPKStatusVsTarget), but a player may have left it since
        if (!IsMember(player) || IsOverLevel(player))
        {
            return;
        }

        PlayerDamage += amount;

        var score = Score(player);
        score.Damage += amount;
        score.LastActive = Time.GetUnixTime();
    }

    public void OnPlayerHit(Player player, double unmitigated)
    {
        if (!IsMember(player) || IsOverLevel(player))
        {
            return;
        }

        var score = Score(player);
        score.DamageTaken += unmitigated;
        score.LastActive = Time.GetUnixTime();
    }

    public void OnLifeHeal(Player healer, Player target, double amount)
    {
        // healing only counts by and on the Town Defense
        if (!IsMember(healer) || !IsMember(target) || IsOverLevel(healer) || IsOverLevel(target))
        {
            return;
        }

        var score = Score(healer);
        score.Healing += amount;
        score.LastActive = Time.GetUnixTime();
    }

    /// <param name="killer">The player who killed it, themselves or with their pet</param>
    /// <param name="killedBy">What killed it: the player, a guard, the Captain, another creature, or null</param>
    public void OnAttackerDeath(Creature attacker, Player killer, WorldObject killedBy)
    {
        // a kill by someone outside the Town Defense, or over-level, doesn't count: the attacker rises again (below)
        var defender = killer != null ? "player" : TownDefender(killedBy);
        var counts = killer == null || IsMember(killer) && !IsOverLevel(killer);

        if (counts)
        {
            switch (defender)
            {
                case "player":
                    PlayerKills++;
                    break;
                case "captain":
                    CaptainKills++;
                    break;
                case "guard":
                    GuardKills++;
                    break;
                default:
                    OtherKills++;
                    break;
            }
        }

        // the Warlord's spoils are shared out when the attack ends, by what each defender did; unless a kill that doesn't
        // count (below) raises it again
        if (attacker == leader && counts)
        {
            leader = null;
            WarlordKilledBy = defender;
            TellRegion($"The Warlord falls! Its spoils will be shared among the defenders of {TownName} when the attack ends.");
        }

        if (killer == null)
        {
            return;
        }

        if (counts)
        {
            Score(killer).KillingBlows++;
            return;
        }

        // a kill outside the Town Defense (a player who left it with the blow on its way), or an over-level, unshrouded
        // one, doesn't count: the attacker rises again at a road into town
        var overLevel = IsOverLevel(killer);
        if (overLevel)
        {
            var score = Score(killer);
            score.OverLevelKills++;

            if (score.PromptsRefused > 0)
            {
                score.KillsAfterRefusal++;
            }

            // one who earned credit in the Town Defense, left, and fights on unshrouded loses it
            if (leftTownDefense.Contains(killer.Guid.Full) && score.HasContributed)
            {
                score.CreditLost = true;
            }

            _log.Information(
                "[TOWN EVENT] {TownName}: over-level kill by {Player} (level {Level}) of {Creature}; refused {Refused}, kills after refusing {After}",
                TownName,
                killer.Name,
                killer.Level,
                attacker.Name,
                score.PromptsRefused,
                score.KillsAfterRefusal
            );
        }

        if (Phase == TownEventPhase.Wave)
        {
            var poolCreature = mainFamily.Concat(minorFamily ?? Enumerable.Empty<TownPoolCreature>()).Concat(leaders).FirstOrDefault(c => c.Wcid == attacker.WeenieClassId);
            if (poolCreature != null)
            {
                var variant = attacker.TownEventHealthMod switch
                {
                    >= 4.0 => Variant.Leader,
                    >= 2.0 => Variant.Elite,
                    >= 1.5 => Variant.Raider,
                    _ => Variant.Standard
                };

                Spawn(poolCreature, variant, role: attacker.TownEventCasterRole);
            }
        }

        Tell(
            killer,
            overLevel
                ? $"Your kill doesn't count: you are above {TownName}'s level and unshrouded. The {attacker.Name} rises again!"
                : $"Your kill doesn't count: you aren't defending {TownName}. The {attacker.Name} rises again!"
        );
        Prompt(killer);
    }

    // ---------------------------------------------------------------- the Supply Chest

    /// <summary>
    /// One attacker at a time loots the chest, one with no one to fight; once attackers have spent ChestLootSeconds at it
    /// between them, it's gone. A member or a guard who comes near the attackers at the chest pulls them off it to fight,
    /// and a free one takes over once they're gone, so the chest is saved by keeping the raiders at it busy.
    /// </summary>
    private void TickChest(double dt)
    {
        var now = Time.GetUnixTime();

        if (now >= nextLooterCheck)
        {
            nextLooterCheck = now + ChestLooterCheckSeconds;

            // the attackers idle at the chest: any with a member or a guard close by go and fight them, and the rest are
            // free to loot
            var free = new List<Creature>();

            foreach (var attacker in Attackers.Where(CanLoot))
            {
                var defender = NearestChestDefender(attacker);

                if (defender == null)
                {
                    free.Add(attacker);
                    continue;
                }

                var chain = new ActionChain();
                chain.AddAction(attacker, () => attacker.TownEventFight(defender));
                chain.EnqueueChain();
            }

            if (!free.Contains(chestLooter))
            {
                chestLooter = free.MinBy(a => a.Location.SquaredDistanceTo(chestPosition));
                nextLootMotion = now;
            }
        }

        if (chestLooter == null || !CanLoot(chestLooter))
        {
            if (lootingNow && now - lastLootTime >= ChestDrivenOffSeconds)
            {
                lootingNow = false;
                TellRegion($"The raiders are driven from {TownName}'s Supply Chest, {TownCombatStats.FormatPercent(ChestLooted)} emptied!");
            }

            return;
        }

        lootProgress += dt;
        lastLootTime = now;

        if (!lootingNow)
        {
            lootingNow = true;
            TellRegion(
                lootStartWarned
                    ? $"Raiders are back at {TownName}'s Supply Chest, {TownCombatStats.FormatPercent(ChestLooted)} emptied! Drive them off!"
                    : $"Raiders are at {TownName}'s Supply Chest! Drive them off before they empty it!"
            );
            lootStartWarned = true;
        }

        if (now >= nextLootMotion)
        {
            nextLootMotion = now + ChestLootMotionSeconds;

            var looter = chestLooter;
            var chest = chestObject;
            var chain = new ActionChain();
            chain.AddAction(looter, () => looter.TownEventLootMotion(chest));
            chain.EnqueueChain();
        }

        var step = GetChestLootStep(lootProgress, ChestLootSeconds);
        if (step > lootStepsWarned)
        {
            lootStepsWarned = step;
            TellRegion($"{TownName}'s Supply Chest is {step * 25}% emptied!");
        }

        if (lootProgress >= ChestLootSeconds)
        {
            LootChest();
        }
    }

    /// <summary>
    /// The quarters of the chest looted so far, 0 to 3 (the region is told at each), for the progress and the time it takes
    /// </summary>
    public static int GetChestLootStep(double progress, double lootSeconds) =>
        lootSeconds <= 0 ? 3 : Math.Clamp((int)Math.Floor(4 * progress / lootSeconds), 0, 3);

    /// <summary>
    /// An attacker at the chest with no one to fight
    /// </summary>
    private bool CanLoot(Creature attacker) =>
        attacker is { IsDead: false, CurrentLandblock: not null, Location: not null, AttackTarget: null }
        && Attackers.Contains(attacker)
        && attacker.Location.SquaredDistanceTo(chestPosition) <= ChestLootRadius * ChestLootRadius;

    /// <summary>
    /// The nearest member of the Town Defense, or guard or Captain, within ChestDefendRange of the attacker, or null
    /// </summary>
    private Creature NearestChestDefender(Creature attacker)
    {
        var rangeSquared = ChestDefendRange * ChestDefendRange;
        var members = Fellowship?.GetFellowshipMembers().Values ?? Enumerable.Empty<Player>();

        return members
            .Cast<Creature>()
            .Concat(guardTargets)
            .Where(d => d is { IsDead: false, Teleporting: false, CurrentLandblock: not null, Location: not null })
            .Select(d => (Defender: d, DistSq: d.Location.SquaredDistanceTo(attacker.Location)))
            .Where(d => d.DistSq <= rangeSquared)
            .OrderBy(d => d.DistSq)
            .Select(d => d.Defender)
            .FirstOrDefault();
    }

    /// <summary>
    /// The attack fails: the town's supply is gone, so every guard upgrade with it, and the raiders, having what they came
    /// for, leave the town alone for longer
    /// </summary>
    private void LootChest()
    {
        _log.Information("[TOWN EVENT] {TownName}: the Supply Chest was looted (supply {Supply:N0}, level {Level})", TownName, TownSupply.GetSupply(TownName), TownSupply.GetLevel(TownName));

        TownSupply.Empty(TownName);
        TownEventManager.DelayNextAttack(TownName);

        Outcome = "looted";

        End(false, $"The raiders make off with {TownName}'s Supply Chest! The guards' supplies are gone.");
    }

    // ---------------------------------------------------------------- the end

    private void Cancel(string why)
    {
        _log.Warning("[TOWN EVENT] {TownName}: cancelled, {Why}", TownName, why);
        Outcome = "cancelled";
        Finish();
    }

    private void End(bool success, string failureText)
    {
        Succeeded = success;
        Outcome ??= success ? "held" : "fell";

        var winners = Scores.Values.Where(s => s.HasContributed && !s.CreditLost).ToList();

        var topDamage = winners.Where(s => s.Damage > 0).MaxBy(s => s.Damage);
        var topHealing = winners.Where(s => s.Healing > 0).MaxBy(s => s.Healing);
        var topTank = winners.Where(s => s.DamageTaken > 0).MaxBy(s => s.DamageTaken);
        var topKills = winners.Where(s => s.KillingBlows > 0).MaxBy(s => s.KillingBlows);

        var tops = new[] { topDamage, topHealing, topTank, topKills };

        // a town with a Captain of the Guard hands a held town's rewards out from the Captain; without one (not placed
        // yet), and for a fallen town, they're paid now
        var claimFromCaptain = success && TownCaptain.HasCaptain(TownName);

        var tier = (int)Town.GetTownTier(TownName);
        var tokens = success ? GetCommendationTokens(tier) : 0;

        var rewarded = 0;
        foreach (var score in winners)
        {
            var player = PlayerManager.GetOnlinePlayer(score.PlayerGuid);
            if (player == null || IsOverLevel(player) || !IsMember(player))
            {
                continue;
            }

            score.CategoriesTopped = tops.Count(t => t == score);
            score.Contribution = GetContributionMultiplier(
                new[]
                {
                    (score.Damage, topDamage?.Damage ?? 0),
                    (score.Healing, topHealing?.Healing ?? 0),
                    (score.DamageTaken, topTank?.DamageTaken ?? 0),
                    ((double)score.KillingBlows, (double)(topKills?.KillingBlows ?? 0))
                }
            );

            var mod = GetRewardMod(TownName, success, score.Contribution);
            score.RewardMod = mod;
            score.CommendationTokens = tokens;

            if (claimFromCaptain)
            {
                score.Reward = "claim";
                TownEventManager.AddClaim(TownName, player, mod, tokens, Uid);
                Tell(
                    player,
                    $"The people of {TownName} thank you for your defense. See the Captain of the Guard within {TownCaptain.ClaimSeconds / 60:N0} minutes for your reward."
                );
            }
            else
            {
                score.Reward = "paid";
                score.Xp = PayReward(player, TownName, mod, success);
                GiveCommendationTokens(player, tokens);
            }

            // the Warlord's spoils, by what each did
            if (WarlordKilledBy != null)
            {
                PayWarlordSpoils(player, score, tier);
            }

            rewarded++;
        }

        var summary = success ? $"{TownName} holds!" : failureText ?? $"{TownName} falls.";
        var highlights = new List<string>();
        if (topDamage != null)
        {
            highlights.Add($"top damage: {topDamage.Name}");
        }
        if (topHealing != null)
        {
            highlights.Add($"top healer: {topHealing.Name}");
        }
        if (topTank != null)
        {
            highlights.Add($"top defender: {topTank.Name}");
        }
        if (topKills != null)
        {
            highlights.Add($"most killing blows: {topKills.Name}");
        }

        TellRegion(highlights.Count > 0 ? $"{summary} {Capitalize(string.Join(", ", highlights))}." : summary);

        _log.Information(
            "[TOWN EVENT] {TownName}: {Result} after {Waves} waves, {Rewarded} rewarded; {Defenders}",
            TownName,
            success ? "held" : "fell",
            Wave,
            rewarded,
            DescribeDefenders() + "; " + string.Join("; ", Combat.Describe())
        );

        Finish();
    }

    private static string Capitalize(string text) => text.Length == 0 ? text : char.ToUpperInvariant(text[0]) + text[1..];

    // --- rewards ---

    // a category's top scorer earns this many times the town's base reward, and everyone else their share of it, in each
    // category; added up, a player's contribution is capped at the same, so one category won is the most there is
    public const double CategoryBonus = 10.0;
    public const double MaxContribution = 10.0;

    // the Warlord's spoils: this many pyreals per tier (tier 0 counting as 1) for each point of contribution
    public const int WarlordPyrealsPerContribution = 50;

    // what the Captain gives with a held town's reward, spent at the capitals' commendation vendors
    public const uint CommendationTokenWcid = 1057552;

    /// <summary>
    /// The XP reward, as a share of a level, for a contribution of 1: 1% at tier 0 (Holtburg, Shoushi, Yaraq), 0.1% less for
    /// each tier above. A full contribution (10) is ten times this.
    /// </summary>
    public static double GetBaseLevelShare(int tier) => Math.Max(0, 0.01 * (1 - 0.1 * tier));

    /// <summary>
    /// How much a player did, 0 to MaxContribution: in each category (damage, healing, damage taken, killing blows) their
    /// share of the top score, times CategoryBonus, added up. A late joiner or a light hitter gets a small share.
    /// </summary>
    public static double GetContributionMultiplier(IEnumerable<(double Mine, double Top)> categories) =>
        Math.Min(MaxContribution, categories.Where(c => c.Top > 0).Sum(c => CategoryBonus * Math.Clamp(c.Mine / c.Top, 0, 1)));

    /// <summary>
    /// How many base rewards a player earned: their contribution, times the town's supply bonus (as it stands when the attack
    /// ends), and a quarter for a failed defence
    /// </summary>
    public static double GetRewardMod(string town, bool success, double contribution) =>
        (success ? 1.0 : 0.25) * contribution * TownSupply.GetRewardMultiplier(town);

    /// <summary>
    /// The Warlord's spoils for one defender: 50 per tier (tier 0 counting as 1) for each point of contribution, and 10% more
    /// for each supply level
    /// </summary>
    public static int GetWarlordPyreals(int tier, double contribution, double supplyMultiplier) =>
        (int)Math.Round(WarlordPyrealsPerContribution * Math.Max(1, tier) * contribution * supplyMultiplier);

    /// <summary>
    /// The commendation tokens for a held town: one per tier, so none at tier 0
    /// </summary>
    public static int GetCommendationTokens(int tier) => Math.Max(0, tier);

    public static void GiveCommendationTokens(Player player, int tokens)
    {
        if (tokens <= 0)
        {
            return;
        }

        player.TryCreateOrMergeStackInInventoryWithNetworking(CommendationTokenWcid, tokens);
        Tell(player, $"You receive {tokens} Defense Commendation Token{(tokens == 1 ? string.Empty : "s")}.");
    }

    /// <summary>
    /// XP: the town's base share of a level times the reward mod, capped at what a player at the town's top level would get
    /// </summary>
    /// <returns>The XP granted, before the player's own XP bonuses (worked out as GrantLevelProportionalXp does)</returns>
    public static long PayReward(Player player, string town, double mod, bool success, bool tell = true)
    {
        var levelShare = GetBaseLevelShare((int)Town.GetTownTier(town));

        var capLevel = Math.Min(player.Level ?? 1, Town.GetTownMaxLevel(town));
        var maxXp = (long)(player.GetXPBetweenLevels(capLevel, capLevel + 1) * levelShare * mod);

        var level = player.Level ?? 1;
        var xp = maxXp == 0 ? 0 : Math.Min((long)Math.Round(player.GetXPBetweenLevels(level, level + 1) * levelShare * mod), maxXp);

        player.GrantLevelProportionalXp(levelShare * mod, 0, maxXp, null);

        if (tell)
        {
            Tell(player, success ? $"The people of {town} thank you for your defense." : $"The people of {town} thank you for trying.");
        }

        return xp;
    }

    /// <summary>
    /// The Warlord's spoils for one defender, once the attack is over and every score is in (the attack's pyreals come only
    /// from here)
    /// </summary>
    private void PayWarlordSpoils(Player player, TownDefenseScore score, int tier)
    {
        var pyreals = GetWarlordPyreals(tier, score.Contribution, TownSupply.GetRewardMultiplier(TownName));
        if (pyreals <= 0)
        {
            return;
        }

        player.TryCreateOrMergeStackInInventoryWithNetworking((uint)ACE.Server.Factories.Enum.WeenieClassName.coinstack, pyreals);
        Tell(player, $"Your share of the Warlord's spoils: {pyreals:N0} pyreals found on its body.");

        score.WarlordPyreals = pyreals;
        WarlordPyreals = Math.Max(WarlordPyreals, pyreals);
        WarlordPaid++;
    }

    /// <summary>
    /// Clears the event away: the remaining attackers, the guards' rally, and the Town Defense fellowship
    /// </summary>
    private void Finish()
    {
        Phase = TownEventPhase.Ended;
        EndTime = Time.GetUnixTime();

        // before the Town Defense disbands, so the log can tell who was still in it
        TownEventRecorder.Record(this);

        foreach (var attacker in Attackers.ToList())
        {
            if (!attacker.IsDestroyed)
            {
                attacker.Destroy();
            }
        }
        Attackers.Clear();

        StandDownGuards();
        CaptainStandDown();

        if (Fellowship != null && Fellowship.TotalMembers > 0)
        {
            var leader =
                PlayerManager.GetOnlinePlayer(Fellowship.FellowshipLeaderGuid)
                ?? Fellowship.GetFellowshipMembers().Values.FirstOrDefault();

            if (leader != null)
            {
                Fellowship.QuitFellowship(leader, true);
            }
        }

        if (Fellowship != null)
        {
            Fellowship.TownDefense = null;
        }
    }

    /// <summary>
    /// An admin stopped it (@townevent stop)
    /// </summary>
    public void Stop()
    {
        TellRegion($"The attack on {TownName} is called off.");
        Outcome = "stopped";
        Finish();
    }

    /// <summary>
    /// For @townevent status
    /// </summary>
    public string Describe()
    {
        var lines = new List<string>
        {
            $"{TownName} (#{Id}): {Phase}, wave {Wave}/{WaveCount}, {Family ?? "family not picked yet"}, {AttackersAlive} attackers alive, "
                + $"{Fellowship?.TotalMembers ?? 0} defenders, {(int)(Time.GetUnixTime() - StartTime)}s in"
                + (ChestRaid ? $", a chest raid ({TownCombatStats.FormatPercent(ChestLooted)} looted)" : ""),
            "  " + DescribeDefenders()
        };
        lines.AddRange(Combat.Describe().Select(l => "  " + l));

        foreach (var score in Scores.Values.OrderByDescending(s => s.Damage))
        {
            lines.Add(
                $"  {score.Name}: damage {score.Damage:N0}, healing {score.Healing:N0}, taken {score.DamageTaken:N0}, "
                    + $"killing blows {score.KillingBlows}"
                    + (score.OverLevelKills > 0 ? $", OVER-LEVEL kills {score.OverLevelKills} (refused {score.PromptsRefused}, after refusing {score.KillsAfterRefusal})" : "")
                    + (score.CreditLost ? ", credit lost" : "")
            );
        }

        return string.Join("\n", lines);
    }

    /// <summary>
    /// How the fight's split between the guards and the players: damage dealt to attackers and kills
    /// </summary>
    public string DescribeDefenders() =>
        DescribeDefenders(PlayerDamage, GuardDamage, CaptainDamage, PlayerKills, GuardKills, CaptainKills, OtherKills, WarlordKilledBy);

    public static string DescribeDefenders(
        double playerDamage,
        double guardDamage,
        double captainDamage,
        int playerKills,
        int guardKills,
        int captainKills,
        int otherKills,
        string warlordKilledBy
    )
    {
        var total = playerDamage + guardDamage + captainDamage;
        string Share(double damage) => total > 0 ? TownCombatStats.FormatPercent(damage / total) : "0%";

        return $"damage dealt: players {playerDamage:N0} ({Share(playerDamage)}), guards {guardDamage:N0} ({Share(guardDamage)}), "
            + $"Captain {captainDamage:N0} ({Share(captainDamage)}); kills: players {playerKills}, guards {guardKills}, Captain {captainKills}"
            + (otherKills > 0 ? $", other {otherKills}" : "")
            + (warlordKilledBy != null ? $"; Warlord killed by {(warlordKilledBy == "captain" ? "the Captain" : warlordKilledBy)}" : "");
    }
}
