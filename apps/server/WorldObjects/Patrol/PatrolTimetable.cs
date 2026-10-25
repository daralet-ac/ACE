using System;
using System.Numerics;

namespace ACE.Server.WorldObjects.Patrol;

/// <summary>
/// Keeps several patrollers on one looping route evenly spaced. The lap is laid out as a timeline
/// (walk each leg, then turn and pause at the next waypoint), and slot k of N is scheduled k/N of a lap
/// ahead of slot 0, on a clock derived from server time - so every patroller on the route works out
/// where it should be on its own, with no coordination. PatrolComponent compares that with where the
/// patroller actually is, and runs to catch up or waits at a waypoint to fall back.
/// </summary>
public sealed class PatrolTimetable
{
    /// <summary>
    /// Walking speed the schedule assumes: a little under MotionInterp.WalkAnimSpeed (3.12), so an undisturbed
    /// patroller runs slightly ahead and waits a moment at its next waypoint instead of falling behind and running.
    /// </summary>
    private const float WalkSpeed = 3.12f * 0.95f;

    /// <summary>
    /// Turning toward the next leg at a waypoint, at a human's walking turn rate (90 degrees a second)
    /// </summary>
    private const float TurnRadiansPerSecond = MathF.PI / 2;

    /// <summary>
    /// Stopping and starting again at a waypoint, on top of the turn
    /// </summary>
    private const float CornerSettleSeconds = 0.3f;

    private readonly Vector2[] _waypoints;  // global XY, in path order
    private readonly float[] _legLength;    // waypoint i -> waypoint i+1
    private readonly double[] _depart;      // lap time at which the patroller leaves waypoint i
    private readonly double _slotOffset;

    public double LapSeconds { get; }

    public PatrolTimetable(PatrolPath path, Vector3 homeGlobal, int slot, int slotCount)
    {
        var count = path.Count;

        _waypoints = new Vector2[count];
        _legLength = new float[count];
        _depart = new double[count];

        for (var i = 0; i < count; i++)
        {
            _waypoints[i] = new Vector2(homeGlobal.X + path[i].Dx, homeGlobal.Y + path[i].Dy);
        }

        for (var i = 0; i < count; i++)
        {
            _legLength[i] = Vector2.Distance(_waypoints[i], _waypoints[(i + 1) % count]);
        }

        // lap time 0 = arriving at waypoint 0
        var time = 0.0;
        for (var i = 0; i < count; i++)
        {
            time += CornerSeconds(i) + (path[i].PauseSeconds ?? 0f);
            _depart[i] = time;

            time += _legLength[i] / WalkSpeed;
        }

        LapSeconds = Math.Max(time, 1.0);
        _slotOffset = LapSeconds * slot / slotCount;
    }

    /// <summary>
    /// Time spent at waypoint i: the turn from the leg arriving there onto the leg leaving it, and settling
    /// </summary>
    private float CornerSeconds(int i)
    {
        var count = _waypoints.Length;
        var arriving = _waypoints[i] - _waypoints[(i + count - 1) % count];
        var leaving = _waypoints[(i + 1) % count] - _waypoints[i];

        if (arriving.LengthSquared() < 0.0001f || leaving.LengthSquared() < 0.0001f)
        {
            return CornerSettleSeconds;
        }

        var cos = Math.Clamp(Vector2.Dot(Vector2.Normalize(arriving), Vector2.Normalize(leaving)), -1f, 1f);
        return MathF.Acos(cos) / TurnRadiansPerSecond + CornerSettleSeconds;
    }

    /// <summary>
    /// Seconds the patroller walking the leg from waypoint leg to the next one is behind its scheduled spot
    /// (negative = ahead), wrapped to within half a lap, so a patroller more than half a lap behind counts as
    /// ahead. Measured along that leg only: on a road walked out and back, the return leg runs a few units from
    /// the outbound one, and the nearest point on the route can be on the wrong one, half a lap away.
    /// </summary>
    public double SecondsBehindOnLeg(int leg, Vector3 globalPos, double currentUnixTime)
    {
        var fraction = FractionAlong(leg, new Vector2(globalPos.X, globalPos.Y), out _);
        return SecondsBehind(_depart[leg] + fraction * _legLength[leg] / WalkSpeed, currentUnixTime);
    }

    /// <summary>
    /// Where this slot is scheduled to be right now (global XY): at a waypoint during its turn/pause there, or
    /// partway along a leg. nextWaypoint is the waypoint it's heading for, and heading the direction of that leg.
    /// </summary>
    public Vector2 ScheduledPoint(double currentUnixTime, out int nextWaypoint, out Vector2 heading)
    {
        var count = _waypoints.Length;
        var lapTime = (currentUnixTime + _slotOffset) % LapSeconds;

        for (var i = 0; i < count; i++)
        {
            var next = (i + 1) % count;
            var leg = _waypoints[next] - _waypoints[i];
            heading = leg.LengthSquared() > 0.0001f ? Vector2.Normalize(leg) : Vector2.Zero;
            nextWaypoint = next;

            // turning / pausing at waypoint i
            if (lapTime < _depart[i])
            {
                return _waypoints[i];
            }

            // walking the leg from waypoint i
            var legSeconds = _legLength[i] / WalkSpeed;
            if (lapTime < _depart[i] + legSeconds)
            {
                var fraction = (float)((lapTime - _depart[i]) / legSeconds);
                return _waypoints[i] + leg * fraction;
            }
        }

        // rounding at the very end of the lap: arriving back at waypoint 0
        nextWaypoint = 1 % count;
        var first = _waypoints[nextWaypoint] - _waypoints[0];
        heading = first.LengthSquared() > 0.0001f ? Vector2.Normalize(first) : Vector2.Zero;
        return _waypoints[0];
    }

    /// <summary>
    /// Distance from globalPos to the leg from waypoint leg to the next one
    /// </summary>
    public float DistanceToLeg(int leg, Vector3 globalPos)
    {
        FractionAlong(leg, new Vector2(globalPos.X, globalPos.Y), out var distSq);
        return MathF.Sqrt(distSq);
    }

    /// <summary>
    /// SecondsBehind for a patroller about to leave a waypoint it has reached. Standing on the waypoint it
    /// projects onto its arrival time, which would count the scheduled turn/pause there as falling behind.
    /// </summary>
    public double SecondsBehindLeaving(int waypoint, double currentUnixTime)
    {
        return SecondsBehind(_depart[waypoint], currentUnixTime);
    }

    private double SecondsBehind(double actual, double currentUnixTime)
    {
        var scheduled = (currentUnixTime + _slotOffset) % LapSeconds;

        var behind = (scheduled - actual) % LapSeconds;
        if (behind < 0)
        {
            behind += LapSeconds;
        }

        if (behind > LapSeconds / 2)
        {
            behind -= LapSeconds;
        }

        return behind;
    }

    /// <summary>
    /// Index of the waypoint ending the leg nearest to globalPos - where a patroller pulled off its
    /// route (combat, emote) should head to rejoin it
    /// </summary>
    public int NextWaypointFrom(Vector3 globalPos)
    {
        var leg = NearestLeg(new Vector2(globalPos.X, globalPos.Y), out _);
        return (leg + 1) % _waypoints.Length;
    }

    private int NearestLeg(Vector2 pos, out float fraction)
    {
        var best = 0;
        var bestDistSq = float.MaxValue;
        fraction = 0f;

        for (var i = 0; i < _waypoints.Length; i++)
        {
            var f = FractionAlong(i, pos, out var distSq);

            if (distSq < bestDistSq)
            {
                bestDistSq = distSq;
                best = i;
                fraction = f;
            }
        }

        return best;
    }

    /// <summary>
    /// How far along leg (waypoint leg to the next) the point nearest pos is, 0 to 1, and how far pos is from it
    /// </summary>
    private float FractionAlong(int leg, Vector2 pos, out float distSq)
    {
        var a = _waypoints[leg];
        var ab = _waypoints[(leg + 1) % _waypoints.Length] - a;
        var lengthSq = ab.LengthSquared();

        var f = lengthSq > 0.0001f ? Math.Clamp(Vector2.Dot(pos - a, ab) / lengthSq, 0f, 1f) : 0f;
        distSq = Vector2.DistanceSquared(pos, a + ab * f);

        return f;
    }
}
