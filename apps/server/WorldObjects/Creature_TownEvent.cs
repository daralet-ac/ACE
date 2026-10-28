using System;
using System.Collections.Generic;
using System.Numerics;
using ACE.Entity;
using ACE.Server.Entity;
using ACE.Server.TownEvents;
using ACE.Server.WorldObjects.Patrol;

namespace ACE.Server.WorldObjects;

/// <summary>
/// Town events (TownEventManager): an attacker marches into town along a road and fights whatever it meets on the way.
/// Guards and attackers that can't reach what they're chasing blip to it.
/// </summary>
partial class Creature
{
    /// <summary>
    /// The town event this creature attacks in, or null. Runtime only: event creatures are never saved.
    /// </summary>
    public TownEvent TownEvent;

    /// <summary>
    /// The archetype health and lethality multipliers of a town event variant (a Raider, a leader), applied by
    /// ApplyArchetypeSystem when the creature enters the world
    /// </summary>
    public double TownEventHealthMod = 1.0;

    public double TownEventLethalityMod = 1.0;

    /// <summary>
    /// Multipliers on the archetype stats themselves, for a leader scaled to its town's supply level
    /// </summary>
    public TownArchetypeMods TownEventArchetypeMods = TownArchetypeMods.None;

    /// <summary>
    /// The least archetype magic a town event attacker has, for one made a caster (TownCasterKits). Runtime only.
    /// </summary>
    public double TownEventMinMagic;

    /// <summary>
    /// The caster role a town event attacker was given, kept for the attacker that rises again in its place. Runtime only.
    /// </summary>
    public TownCasterRole TownEventCasterRole;

    /// <summary>
    /// For a monster whose worn armor counts (CreatureArmorEffectsDamageReduction): each body part takes the higher of its
    /// archetype armor and its worn armor, instead of both added together. Town guards (TownGuardUpgrades). Runtime only.
    /// </summary>
    public bool ArmorTakesHigher;

    /// <summary>
    /// The road into town, from the entry point to the centre
    /// </summary>
    internal List<Position> TownEventMarch;

    private int townEventMarchIndex;
    private double townEventNextMoveTime;
    private float townEventBestMarchDist = float.MaxValue;
    private double townEventBestMarchTime;

    private const float TownEventArriveDistance = 3.0f;

    // no progress toward a march waypoint for this long blips the attacker onto it
    private const double TownEventMarchBlipSeconds = 8.0;

    // no progress toward the target being chased for this long blips a guard or attacker next to it
    private const double ChaseBlipSeconds = 7.0;

    private WorldObject chaseTarget;
    private float chaseBestDist;
    private double chaseBestTime;

    /// <summary>
    /// Walks (runs) the attacker on toward the town centre while it has no one to fight
    /// </summary>
    private void TownEventMarchUpdate(double currentUnixTime)
    {
        if (TownEventMarch == null || townEventMarchIndex >= TownEventMarch.Count)
        {
            return;
        }

        var dest = TownEventMarch[townEventMarchIndex];
        var dist = (float)Math.Sqrt(Location.SquaredDistanceTo(dest));

        if (dist <= TownEventArriveDistance)
        {
            townEventMarchIndex++;
            townEventNextMoveTime = 0;
            townEventBestMarchDist = float.MaxValue;
            return;
        }

        if (dist < townEventBestMarchDist - 0.5f)
        {
            townEventBestMarchDist = dist;
            townEventBestMarchTime = currentUnixTime;
        }
        else if (currentUnixTime - townEventBestMarchTime >= TownEventMarchBlipSeconds)
        {
            townEventBestMarchDist = float.MaxValue;
            townEventBestMarchTime = currentUnixTime;

            // FakeTeleport can't cross landblocks: skip a waypoint in another one, the next leg carries on from here
            if (dest.Landblock == Location.Landblock)
            {
                CancelMoveToForEmote();
                FakeTeleport(dest);
            }
            else
            {
                townEventMarchIndex++;
            }

            townEventNextMoveTime = 0;
            return;
        }

        if (currentUnixTime < townEventNextMoveTime && IsMoving)
        {
            return;
        }

        if (CombatMode != ACE.Entity.Enum.CombatMode.NonCombat)
        {
            SetCombatMode(ACE.Entity.Enum.CombatMode.NonCombat);
        }

        var moveTo = new Position(dest);
        var dir = dest.ToGlobal() - Location.ToGlobal();
        dir.Z = 0;
        if (dir.LengthSquared() > 0.01f)
        {
            moveTo.Rotate(dir);
        }

        MoveTo(moveTo, GetRunRate(), true, null, null);
        IsMoving = true;

        // reissued now and then, in case a fight or a collision dropped the move
        townEventNextMoveTime = currentUnixTime + 5.0;
    }

    /// <summary>
    /// An attacker that has run out of targets goes back to marching, instead of home to where it spawned
    /// </summary>
    private void TownEventResumeMarch()
    {
        AttackTarget = null;
        CurrentAttack = null;
        firstUpdate = true;
        MonsterState = State.Awake;

        townEventNextMoveTime = 0;
        townEventBestMarchDist = float.MaxValue;
        townEventBestMarchTime = ACE.Common.Time.GetUnixTime();

        // carry on from the waypoint nearest the town centre that's still ahead, not the one behind
        if (TownEventMarch == null)
        {
            return;
        }

        var bestIndex = townEventMarchIndex;
        var bestDistSq = float.MaxValue;

        for (var i = townEventMarchIndex; i < TownEventMarch.Count; i++)
        {
            var distSq = Location.SquaredDistanceTo(TownEventMarch[i]);
            if (distSq < bestDistSq)
            {
                bestDistSq = distSq;
                bestIndex = i;
            }
        }

        townEventMarchIndex = bestIndex;
    }

    /// <summary>
    /// A guard or attacker chasing its target that hasn't got any closer for ChaseBlipSeconds (caught on scenery, a
    /// wall or a fence) blips next to it
    /// </summary>
    private void CheckChaseStuck(double currentUnixTime, float targetDist)
    {
        if (AttackTarget != chaseTarget)
        {
            chaseTarget = AttackTarget;
            chaseBestDist = targetDist;
            chaseBestTime = currentUnixTime;
            return;
        }

        if (targetDist < chaseBestDist - 0.5f)
        {
            chaseBestDist = targetDist;
            chaseBestTime = currentUnixTime;
            return;
        }

        if (currentUnixTime - chaseBestTime < ChaseBlipSeconds)
        {
            return;
        }

        chaseBestTime = currentUnixTime;

        var target = AttackTarget;
        if (target?.Location == null || target.Location.Indoors || Location.Indoors)
        {
            return;
        }

        // a couple of meters short of the target, on the side this creature is coming from
        var away = Location.ToGlobal() - target.Location.ToGlobal();
        var flat = new Vector2(away.X, away.Y);
        flat = flat.LengthSquared() > 0.01f ? Vector2.Normalize(flat) * 2.0f : new Vector2(2.0f, 0);

        var spot = PatrolComponent.BuildOutdoorPosition(target.Location, flat.X, flat.Y, InstanceId);

        if (spot.Landblock != Location.Landblock)
        {
            return;
        }

        CancelMoveToForEmote();
        FakeTeleport(spot);
        chaseBestDist = float.MaxValue;
    }

    /// <summary>
    /// Sets how far a guard sees (a town event rallies guards from farther away), or puts back its own with null
    /// </summary>
    internal void SetVisualAwarenessRange(double? range)
    {
        VisualAwarenessRange = range;
        _visualAwarenessRangeSq = null;
    }

    /// <summary>
    /// Forgets the chase being watched by CheckChaseStuck (the creature is attacking, or has no target)
    /// </summary>
    private void ResetChaseStuck()
    {
        chaseTarget = null;
    }
}
