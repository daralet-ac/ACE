using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ACE.Database.Models.World;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Entity;
using Serilog;

namespace ACE.Server.TownEvents;

/// <summary>
/// A creature a town event can send: native to the town's region and of its tier
/// </summary>
/// <param name="Caster">Has a spellbook, so it fights with magic; without one it fights with weapons and claws</param>
public sealed record TownPoolCreature(uint Wcid, string Name, CreatureType CreatureType, int Level, double Toughness, double Lethality, bool Caster)
{
    /// <summary>
    /// One of the races that would raid a town for its supplies (the thinking, tool-using ones and the undead), not a beast
    /// </summary>
    public bool Humanoid => TownCreaturePool.HumanoidTypes.Contains(CreatureType);

    /// <summary>
    /// What the creature costs from a wave's budget, against a standard creature of the tier (1.0): its archetype
    /// toughness and lethality. The creatures in a pool are all of the town's tier, so their level within it is left out.
    /// </summary>
    public double BaseCost => Math.Max(0.25, Toughness * Lethality);
}

/// <summary>
/// The creatures each town's events draw from, read from the world database once per town: every creature that the
/// landblock instances and encounters around the town spawn (through their generators), that uses the archetype
/// system, attacks on sight, and is of the town's tier
/// </summary>
public static class TownCreaturePool
{
    private static readonly ILogger _log = Log.ForContext(typeof(TownCreaturePool));

    // landblocks this many either side of the town centre's are its surroundings
    private const int LandblockRadius = 3;

    // generators of generators: how many levels down to follow
    private const int GeneratorDepth = 4;

    private static readonly ConcurrentDictionary<string, Task<List<TownPoolCreature>>> Pools = new();

    /// <summary>
    /// The races that raid a Supply Chest: people, the tool-using races and the undead, not beasts, insects or elementals
    /// </summary>
    public static readonly HashSet<CreatureType> HumanoidTypes = new()
    {
        CreatureType.Human,
        CreatureType.AlteredHuman,
        CreatureType.ViamontianKnight,
        CreatureType.Drudge,
        CreatureType.Banderling,
        CreatureType.Mosswart,
        CreatureType.Merwart,
        CreatureType.Lugian,
        CreatureType.GotrokLugian,
        CreatureType.Tumerok,
        CreatureType.AunTumerok,
        CreatureType.HeaTumerok,
        CreatureType.Sclavus,
        CreatureType.Ruschk,
        CreatureType.Burun,
        CreatureType.Gurog,
        CreatureType.Moarsman,
        CreatureType.BlightedMoarsman,
        CreatureType.Anekshay,
        CreatureType.Undead,
        CreatureType.Skeleton,
        CreatureType.Mumiyah,
    };

    /// <summary>
    /// The town's pool, read in the background the first time it's asked for
    /// </summary>
    public static Task<List<TownPoolCreature>> Get(string town) => Pools.GetOrAdd(town, t => Task.Run(() => Build(t)));

    /// <summary>
    /// Forgets the pools read so far, so the next event reads the world database again
    /// </summary>
    public static void Clear()
    {
        Pools.Clear();
        Rosters.Clear();
    }

    private static List<TownPoolCreature> Build(string town)
    {
        try
        {
            var center = Town.GetTownCenterLandblock(town);
            // a List, not a HashSet: EF translates List.Contains to SQL IN
            var landblocks = new List<int>();

            for (var dx = -LandblockRadius; dx <= LandblockRadius; dx++)
            {
                for (var dy = -LandblockRadius; dy <= LandblockRadius; dy++)
                {
                    var x = center.LandblockX + dx;
                    var y = center.LandblockY + dy;
                    if (x is >= 0 and <= 0xFE && y is >= 0 and <= 0xFE)
                    {
                        landblocks.Add(x << 8 | y);
                    }
                }
            }

            var minLevel = Math.Max(1, Town.GetTownMinLevel(town));
            var maxLevel = Town.GetTownMaxLevel(town);

            using var ctx = new WorldDbContext();

            var seen = new HashSet<uint>(
                ctx.LandblockInstance.Where(i => i.Landblock != null && landblocks.Contains(i.Landblock.Value))
                    .Select(i => i.WeenieClassId)
                    .ToList()
            );

            seen.UnionWith(ctx.Encounter.Where(e => landblocks.Contains(e.Landblock)).Select(e => e.WeenieClassId).ToList());

            var frontier = seen.ToList();
            for (var depth = 0; depth < GeneratorDepth && frontier.Count > 0; depth++)
            {
                var children = ctx.WeeniePropertiesGenerator.Where(g => frontier.Contains(g.ObjectId))
                    .Select(g => g.WeenieClassId)
                    .ToList();

                frontier = children.Where(seen.Add).ToList();
            }

            var ids = seen.ToList();

            var creatures = ctx.Weenie.Where(w => ids.Contains(w.ClassId) && w.Type == (int)WeenieType.Creature)
                .Select(w => w.ClassId)
                .ToList();

            var pool = Read(ctx, creatures, minLevel, maxLevel);

            _log.Information(
                "[TOWN EVENT] {Town}: {Count} creatures in its pool: {Families}",
                town,
                pool.Count,
                string.Join(", ", pool.GroupBy(c => c.CreatureType).Select(g => $"{g.Key} ({g.Count(c => c.Caster)} casters of {g.Count()})"))
            );
            return pool;
        }
        catch (Exception ex)
        {
            _log.Error(ex, "[TOWN EVENT] Couldn't read {Town}'s creature pool", town);
            return new List<TownPoolCreature>();
        }
    }

    /// <summary>
    /// The creatures of these WCIDs that a town event can send: archetype creatures that attack on sight, aren't patrolling,
    /// bosses or shroud creatures, and are within the levels given
    /// </summary>
    private static List<TownPoolCreature> Read(WorldDbContext ctx, List<uint> creatures, int minLevel, int maxLevel)
    {
        var ints = ctx.WeeniePropertiesInt.Where(p => creatures.Contains(p.ObjectId)).ToList().ToLookup(p => p.ObjectId);
        var bools = ctx.WeeniePropertiesBool.Where(p => creatures.Contains(p.ObjectId)).ToList().ToLookup(p => p.ObjectId);
        var floats = ctx.WeeniePropertiesFloat.Where(p => creatures.Contains(p.ObjectId)).ToList().ToLookup(p => p.ObjectId);
        var names = ctx.WeeniePropertiesString.Where(p => creatures.Contains(p.ObjectId) && p.Type == (ushort)PropertyString.Name)
            .ToList()
            .ToDictionary(p => p.ObjectId, p => p.Value);
        var casters = new HashSet<uint>(
            ctx.WeeniePropertiesSpellBook.Where(p => creatures.Contains(p.ObjectId)).Select(p => p.ObjectId).Distinct().ToList()
        );

        const Tolerance notOnSight = Tolerance.NoAttack | Tolerance.Appraise | Tolerance.Target | Tolerance.Retaliate | Tolerance.Monster;

        var pool = new List<TownPoolCreature>();

        foreach (var wcid in creatures)
        {
            int? Int(PropertyInt p) => ints[wcid].FirstOrDefault(i => i.Type == (ushort)p)?.Value;
            bool? Bool(PropertyBool p) => bools[wcid].FirstOrDefault(b => b.Type == (ushort)p)?.Value;
            double? Float(PropertyFloat p) => floats[wcid].FirstOrDefault(f => f.Type == (ushort)p)?.Value;

            var level = Int(PropertyInt.Level) ?? 0;
            var tolerance = (Tolerance)(Int(PropertyInt.Tolerance) ?? 0);
            var pkStatus = (PlayerKillerStatus)(Int(PropertyInt.PlayerKillerStatus) ?? 0);

            if (level < minLevel || level > maxLevel
                || Bool(PropertyBool.UseArchetypeSystem) != true
                || Bool(PropertyBool.Attackable) == false
                || Bool(PropertyBool.PatrolEnabled) == true
                || Bool(PropertyBool.BossKillXpReward) == true
                || Bool(PropertyBool.ShroudKillXpReward) == true
                || (tolerance & notOnSight) != 0
                || pkStatus == PlayerKillerStatus.RubberGlue
                || !names.TryGetValue(wcid, out var name))
            {
                continue;
            }

            pool.Add(
                new TownPoolCreature(
                    wcid,
                    name,
                    (CreatureType)(Int(PropertyInt.CreatureType) ?? 0),
                    level,
                    Float(PropertyFloat.ArchetypeToughness) ?? 1.0,
                    Float(PropertyFloat.ArchetypeLethality) ?? 1.0,
                    casters.Contains(wcid)
                )
            );
        }

        return pool;
    }

    private static readonly ConcurrentDictionary<string, Task<TownRaidRoster>> Rosters = new();

    /// <summary>
    /// A raid theme's creatures, read in the background the first time it's asked for. Their own levels are kept: the event
    /// picks the ones nearest the town's tier and scales them to it.
    /// </summary>
    public static Task<TownRaidRoster> GetRoster(TownRaidTheme theme) =>
        Rosters.GetOrAdd(theme.Name, _ => Task.Run(() => ReadRoster(theme)));

    private static TownRaidRoster ReadRoster(TownRaidTheme theme)
    {
        try
        {
            using var ctx = new WorldDbContext();

            List<TownPoolCreature> ReadAll(uint[] wcids)
            {
                // a creature the theme borrows under another name raids under it
                var found = Read(ctx, wcids.ToList(), 0, int.MaxValue)
                    .Select(c => theme.Names.TryGetValue(c.Wcid, out var name) ? c with { Name = name } : c)
                    .ToList();
                foreach (var missing in wcids.Except(found.Select(c => c.Wcid)))
                {
                    _log.Warning("[TOWN EVENT] Raid theme {Theme}: creature {Wcid} is missing or can't raid", theme.Name, missing);
                }
                return found;
            }

            return new TownRaidRoster(theme, ReadAll(theme.Raiders), ReadAll(theme.Pets), ReadAll(theme.Leaders));
        }
        catch (Exception ex)
        {
            _log.Error(ex, "[TOWN EVENT] Couldn't read raid theme {Theme}", theme.Name);
            return new TownRaidRoster(theme, new(), new(), new());
        }
    }
}
