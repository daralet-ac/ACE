using System;
using ACE.Entity;
using ACE.Entity.Enum.Properties;
using ACE.Server.Entity;

namespace ACE.Server.WorldObjects.Patrol;

public sealed class PatrolComponent
{
    private readonly Creature _creature;
    // Cache the original spawn/home location as the patrol center.
    // Do NOT rely on _creature.Home because other systems may update it.
    private readonly Position _patrolHome;

    private PatrolPath _path;
    private int _index;

    private double _nextMoveTime;
    private Position _currentDest;
    private Position _finalDest;
    private int _finalIndex;

    // Pause handling (applied only on real waypoints, not detours)
    private float _pauseOnArrivalSeconds;

    // Stuck handling: detect lack of movement progress and do small detours ("nudges")
    private double _nextProgressSampleTime;
    private Position _lastSamplePos;
    private double _stuckSinceTime;
    private int _stuckAttempts;
    private bool _detouring;
    // When patrol is interrupted (combat/emote), rejoin the patrol loop from current position.
    private bool _needsRejoin;
    private const float ArriveDistance = 1.5f;

    // No progress toward the current waypoint for this long (nudges included) blips the patroller onto it
    private const double BlipAfterSeconds = 10.0;
    private float _bestDistToFinal;
    private double _bestDistTime;

    // Shared-route timetable (PatrolSlot / PatrolSlotCount), null for a patroller walking a route alone
    private PatrolTimetable _timetable;
    private bool _running;
    private int _lastReachedWaypoint = -1;
    private double _nextTimetableCheck;
    private const double RunWhenBehindSeconds = 5.0;
    private const double WalkWhenBehindSeconds = 1.0;
    private const double HoldWhenAheadSeconds = 1.0;
    private const float RejoinSameLegDistance = 10.0f;

    public PatrolComponent(Creature creature)
    {
        if (creature == null)
        {
            throw new ArgumentNullException(nameof(creature));
        }
        _creature = creature;

        // Capture patrol center once (original spawn/home). If Home isn't set, fall back quietly.
        var homePos = _creature.Home ?? _creature.Location;
        _patrolHome = new Position(homePos);

        ReloadPath();

        // spawned straight into its timetable slot partway round the route (Creature.PlaceAtPatrolSlot):
        // head on along the leg it was put on, not back to the route start
        if (_creature.PatrolStartWaypoint is int start && _path != null && _path.Count > 0)
        {
            _index = start % _path.Count;
        }
        _creature.PatrolStartWaypoint = null;
    }


    public void ReloadPath()
    {
        var raw = _creature.GetProperty(PropertyString.PatrolPath);
        _path = PatrolPath.Parse(raw);
        _index = 0;

        _currentDest = null;
        _finalDest = null;

        _detouring = false;
        _stuckAttempts = 0;
        _stuckSinceTime = 0;
        _nextProgressSampleTime = 0;
        _lastSamplePos = null;

        _pauseOnArrivalSeconds = 0f;
        _nextMoveTime = 0;

        var slot = _creature.GetProperty(PropertyInt.PatrolSlot) ?? 0;
        var slotCount = _creature.GetProperty(PropertyInt.PatrolSlotCount) ?? 0;

        _timetable =
            _path != null && slotCount >= 2 && slot >= 0 && slot < slotCount
                ? new PatrolTimetable(_path, _patrolHome.ToGlobal(), slot, slotCount)
                : null;

        _running = false;
        _lastReachedWaypoint = -1;
    }

    /// <summary>
    /// Clears any in-flight patrol destination so patrol can't be wedged "waiting to arrive"
    /// after a combat interruption.
    /// </summary>
    public void ResetDestination(double currentUnixTime = 0)
    {
        _currentDest = null;
        _finalDest = null;

        _detouring = false;
        _stuckAttempts = 0;
        _stuckSinceTime = 0;
        _lastSamplePos = null;
        _nextProgressSampleTime = 0;
        _pauseOnArrivalSeconds = 0f;

        // After interruptions, recompute next waypoint from current position.
        _needsRejoin = true;

        // Allow immediate next waypoint after reset.
        _nextMoveTime = currentUnixTime;

    }


    public void Update(double currentUnixTime)
    {
        // Patrol only acts when idle; combat AI handles combat.
        if (_creature.AttackTarget != null)
        {
            _needsRejoin = true;
            return;
        }

        if (_creature.EmoteManager.IsBusy)
        {
            _needsRejoin = true;
            return;
        }
        if (_path == null || _path.Count == 0)
        {
            return;
        }

        // If patrol was interrupted (combat/emote), rejoin the loop at a computed waypoint.
        if (_needsRejoin)
        {
            RejoinLoopFromCurrentPosition(currentUnixTime);
            _needsRejoin = false;
        }
        // If we have an in-flight destination, see if we've arrived or if we're stuck.
        if (_currentDest != null)
        {
            // SquaredDistanceTo accounts for the destination being in a neighbouring landblock
            var distSq = _creature.Location.SquaredDistanceTo(_currentDest);

            if (distSq > (ArriveDistance * ArriveDistance))
            {
                // Mid-leg on a shared route: start or stop running as the patroller falls behind or catches up.
                if (_timetable != null && !_detouring && currentUnixTime >= _nextTimetableCheck)
                {
                    _nextTimetableCheck = currentUnixTime + 1.0;

                    var leg = (_finalIndex + _path.Count - 1) % _path.Count;
                    var behind = _timetable.SecondsBehindOnLeg(leg, _creature.Location.ToGlobal(), currentUnixTime);
                    if (UpdateRunning(behind))
                    {
                        IssueMove(_currentDest, currentUnixTime);
                    }
                }

                if (currentUnixTime >= _nextProgressSampleTime)
                {
                    _nextProgressSampleTime = currentUnixTime + 0.5;

                    if (_finalDest != null)
                    {
                        var distToFinal = (float)Math.Sqrt(_creature.Location.SquaredDistanceTo(_finalDest));
                        if (distToFinal < _bestDistToFinal - 0.5f)
                        {
                            _bestDistToFinal = distToFinal;
                            _bestDistTime = currentUnixTime;
                        }
                        else if (currentUnixTime - _bestDistTime >= BlipAfterSeconds)
                        {
                            BlipToWaypoint(currentUnixTime);
                            return;
                        }
                    }

                    if (_lastSamplePos == null)
                    {
                        _lastSamplePos = new Position(_creature.Location);
                        _stuckSinceTime = currentUnixTime;
                    }
                    else
                    {
                        var movedSq = _creature.Location.SquaredDistanceTo(_lastSamplePos);

                        // ~10cm movement threshold
                        if (movedSq < 0.01f)
                        {
                            if ((currentUnixTime - _stuckSinceTime) >= 2.0)
                            {
                                TryDetour(currentUnixTime);
                            }
                        }
                        else
                        {
                            _stuckSinceTime = currentUnixTime;
                            _lastSamplePos = new Position(_creature.Location);
                        }
                    }
                }

                return;
            }

            // Arrived at _currentDest
            _currentDest = null;

            // If we were detouring, immediately resume the real waypoint.
            // Never pause on detours.
            if (_detouring && _finalDest != null)
            {
                _detouring = false;
                _currentDest = _finalDest;
                IssueMove(_finalDest, currentUnixTime);
                return;
            }

            // Arrived at a real waypoint.
            if (_finalDest != null)
            {
                _finalDest = null;
                _lastReachedWaypoint = _finalIndex;

                if (_pauseOnArrivalSeconds > 0f)
                {
                    // Pause, then give rotation/idle blend a moment to settle before next MoveTo()
                    _nextMoveTime = currentUnixTime + _pauseOnArrivalSeconds + 0.35;
                    _pauseOnArrivalSeconds = 0f;
                    return;
                }
            }
        }

        // Wait for pause/cooldown between legs.
        if (currentUnixTime < _nextMoveTime)
        {
            return;
        }

        // Shared route: wait here while ahead of schedule, and run the next leg while behind it.
        if (_timetable != null)
        {
            // not at a waypoint (just spawned, or back from combat): on the leg leading to the next one
            var behind =
                _lastReachedWaypoint >= 0
                    ? _timetable.SecondsBehindLeaving(_lastReachedWaypoint, currentUnixTime)
                    : _timetable.SecondsBehindOnLeg((_index + _path.Count - 1) % _path.Count, _creature.Location.ToGlobal(), currentUnixTime);

            if (behind < -HoldWhenAheadSeconds)
            {
                _nextMoveTime = currentUnixTime + Math.Min(-behind, 1.0);
                return;
            }

            UpdateRunning(behind);
        }

        // Select next offset (looping)
        _finalIndex = _index;
        var offset = _path[_index];
        _index = (_index + 1) % _path.Count;

        // Base position is the cached patrol center (original spawn/home).
        var basePos = new Position(_patrolHome);
        var nextPos = BuildWaypoint(basePos, offset, _creature.InstanceId);

        // Pause: fixed override on waypoint, otherwise weenie random default range.
        // A shared route's timetable owns the pacing, so it only takes fixed pauses.
        _pauseOnArrivalSeconds = offset.PauseSeconds ?? (_timetable != null ? 0f : GetDefaultPauseSeconds());

        _finalDest = nextPos;
        _currentDest = nextPos;
        _lastReachedWaypoint = -1;

        _detouring = false;
        _stuckAttempts = 0;
        _stuckSinceTime = currentUnixTime;
        _lastSamplePos = new Position(_creature.Location);
        _bestDistToFinal = float.MaxValue;
        _bestDistTime = currentUnixTime;

        IssueMove(nextPos, currentUnixTime);

        // Small pacing guard (pause is applied on arrival).
        _nextMoveTime = currentUnixTime + 0.75;
        _nextTimetableCheck = currentUnixTime + 1.0;
    }

    /// <summary>
    /// Runs while well behind schedule and walks again once nearly caught up.
    /// Returns true if that changed, so an in-flight move needs reissuing at the new pace.
    /// </summary>
    private bool UpdateRunning(double secondsBehind)
    {
        var running = _running ? secondsBehind > WalkWhenBehindSeconds : secondsBehind > RunWhenBehindSeconds;

        if (running == _running)
        {
            return false;
        }

        _running = running;
        return true;
    }

    private float GetDefaultPauseSeconds()
    {
        // New weenie float properties:
        // 20055 = PatrolPauseMinSeconds
        // 20056 = PatrolPauseMaxSeconds
        var minObj = _creature.GetProperty(PropertyFloat.PatrolPauseMinSeconds);
        var maxObj = _creature.GetProperty(PropertyFloat.PatrolPauseMaxSeconds);

        if (minObj == null || maxObj == null)
        {
            return 0f;
        }

        var min = (float)minObj;
        var max = (float)maxObj;

        if (min < 0f)
        {
            min = 0f;
        }

        if (max < 0f)
        {
            max = 0f;
        }

        if (max < min)
        {
            var t = min;
            min = max;
            max = t;
        }

        if (max <= min)
        {
            return min;
        }

        return min + ((float)Random.Shared.NextDouble() * (max - min));
    }
    private void RejoinLoopFromCurrentPosition(double currentUnixTime)
    {
        // Clear any in-flight leg so we don't "arrive" at an old target after interruption.
        _currentDest = null;
        _finalDest = null;

        _detouring = false;
        _stuckAttempts = 0;
        _stuckSinceTime = 0;
        _lastSamplePos = null;
        _nextProgressSampleTime = 0;

        _pauseOnArrivalSeconds = 0f;
        _nextMoveTime = currentUnixTime;
        _lastReachedWaypoint = -1;

        // Shared route: pick the route back up on the leg it was walking if it's still close to it (on a road
        // walked out and back the nearest leg may be the one going the other way), else on the nearest leg.
        // The timetable restores the spacing.
        if (_timetable != null)
        {
            var pos = _creature.Location.ToGlobal();
            var interruptedLeg = (_finalIndex + _path.Count - 1) % _path.Count;

            _index =
                _timetable.DistanceToLeg(interruptedLeg, pos) <= RejoinSameLegDistance
                    ? _finalIndex
                    : _timetable.NextWaypointFrom(pos);
            return;
        }

        // Base position is the cached patrol center (original spawn/home).
        var basePos = new Position(_patrolHome);

        // Find the closest waypoint to our current location.
        var bestIndex = 0;
        var bestDistSq = double.MaxValue;

        for (var i = 0; i < _path.Count; i++)
        {
            var wp = BuildWaypoint(basePos, _path[i], _creature.InstanceId);

            var distSq = _creature.Location.SquaredDistanceTo(wp);

            if (distSq < bestDistSq)
            {
                bestDistSq = distSq;
                bestIndex = i;
            }
        }

        // Per-member deterministic offset, but keep it LOCAL (nearest / next / previous)
        // so we don't create diagonals that pass through Home.
        var count = _path.Count;
        var salt = (int)(_creature.Guid.Full % 3);

        var candidate = bestIndex;

        if (count >= 3)
        {
            if (salt == 1)
            {
                candidate = (bestIndex + 1) % count;
            }
            else if (salt == 2)
            {
                candidate = (bestIndex + count - 1) % count;
            }
        }
        else if (count == 2)
        {
            candidate = (bestIndex + (int)(_creature.Guid.Full % 2)) % 2;
        }

        _index = candidate;

    }

    private static Position BuildWaypoint(Position basePos, PatrolOffset offset, uint instance)
    {
        // 2D-only movement: compute XY from home + offsets, Z from terrain.
        return BuildOutdoorPosition(basePos, offset.Dx, offset.Dy, instance);
    }

    /// <summary>
    /// Offsets a position in XY and snaps it to the terrain. Offsets may run past the landblock edge
    /// (routes that follow a road out of town), so the position is carried into the neighbouring landblock.
    /// </summary>
    internal static Position BuildOutdoorPosition(Position from, float dx, float dy, uint instance)
    {
        var nextPos = new Position(from);

        nextPos.PositionX = from.Pos.X + dx;
        nextPos.PositionY = from.Pos.Y + dy;

        nextPos.SetLandblock();

        // The outdoor cell first, then the terrain height: looking the cell up at the height this was built from (the
        // route start, or wherever the creature stood) can find the point below the ground and put it in a cellar or cave
        // cell under it, which then kept that cell at the terrain height, outside the cell's walls. A client loading in
        // walks that cell's geometry for an object it doesn't fit, and crashes.
        nextPos.LandblockId = new LandblockId(nextPos.GetOutdoorCell());

        // Terrain height accounts for hills.
        nextPos.PositionZ = nextPos.GetTerrainZ(instance);

        // At the terrain height, the cell it's really in: the landscape, or a building it stands in.
        nextPos.LandblockId = new LandblockId(nextPos.GetCell(instance));

        return nextPos;
    }

    private void IssueMove(Position dest, double currentUnixTime)
    {
        var forceWalk = _creature.GetProperty(PropertyBool.PatrolForceWalk) == true;

        // In Creature_Navigation.GetMoveToPosition():
        // runRate <= 0 => clears MovementParams.CanRun (forces walk)
        // Catching up to a shared route's timetable overrides PatrolForceWalk.
        var runRate = forceWalk && !_running ? 0.0f : _creature.GetRunRate();

        // MoveTo always turns to the destination's heading on arrival. Face along the leg being
        // walked, so arriving doesn't add a turn toward whatever rotation the waypoint copied from home.
        var dir = dest.ToGlobal() - _creature.Location.ToGlobal();
        dir.Z = 0;
        if (dir.LengthSquared() > 0.01f)
        {
            dest.Rotate(dir);
        }

        // last parameter is optional "speed" override; keep null to use default weenie locomotion
        _creature.MoveTo(dest, runRate, true, null, null);

        // After issuing a move, sample soon for stuck detection.
        _nextProgressSampleTime = currentUnixTime + 0.5;
    }

    /// <summary>
    /// Nudges the patroller off whatever it's caught on, cycling through small detours. If none of that
    /// gets it closer to its waypoint, the progress check in Update blips it there after BlipAfterSeconds.
    /// </summary>
    private void TryDetour(double currentUnixTime)
    {
        (float x, float y)[] offsets =
        {
            ( 2.0f,  0.0f),
            (-2.0f,  0.0f),
            ( 0.0f,  2.0f),
            ( 0.0f, -2.0f),
            ( 1.5f,  1.5f),
            (-1.5f,  1.5f),
            ( 1.5f, -1.5f),
            (-1.5f, -1.5f),
        };

        var (ox, oy) = offsets[_stuckAttempts % offsets.Length];
        _stuckAttempts++;

        var detour = BuildOutdoorPosition(_creature.Location, ox, oy, _creature.InstanceId);

        _detouring = true;
        _currentDest = detour;

        // If we're stuck on top of/against an object, detach before trying the detour move.
        _creature.PhysicsObj?.unstick_from_object();

        IssueMove(detour, currentUnixTime);

        _stuckSinceTime = currentUnixTime;
        _lastSamplePos = new Position(_creature.Location);
    }

    /// <summary>
    /// The patroller is stuck short of its waypoint: blip onto it. FakeTeleport can't cross landblocks,
    /// so a waypoint in another landblock is skipped instead (a shared route's timetable then catches up).
    /// </summary>
    private void BlipToWaypoint(double currentUnixTime)
    {
        _detouring = false;
        _stuckAttempts = 0;
        _lastSamplePos = null;
        _bestDistToFinal = float.MaxValue;
        _bestDistTime = currentUnixTime;

        if (_finalDest != null && _finalDest.Landblock == _creature.Location.Landblock)
        {
            _creature.CancelMoveToForEmote();
            _creature.FakeTeleport(_finalDest);

            // arrives on the next update
            _currentDest = _finalDest;
            return;
        }

        _currentDest = null;
        _finalDest = null;
        _pauseOnArrivalSeconds = 0f;
        _nextMoveTime = currentUnixTime + 0.25;
    }
}
