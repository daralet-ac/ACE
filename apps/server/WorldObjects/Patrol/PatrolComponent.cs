using System;
using ACE.Entity;
using ACE.Entity.Enum.Properties;
using ACE.Server.Entity;

namespace ACE.Server.WorldObjects.Patrol;

public sealed class PatrolComponent
{
    private readonly Creature _creature;

    private PatrolPath _path;
    private int _index;

    private double _nextMoveTime;
    private Position _currentDest;
    private Position _finalDest;

    // Pause handling (applied only on real waypoints, not detours)
    private float _pauseOnArrivalSeconds;

    // Stuck handling: detect lack of movement progress and do small detours ("nudges")
    private double _nextProgressSampleTime;
    private Position _lastSamplePos;
    private double _stuckSinceTime;
    private int _stuckAttempts;
    private bool _detouring;

    private const float ArriveDistance = 1.5f;

    public PatrolComponent(Creature creature)
    {
        _creature = creature;
        ReloadPath();
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

        // Allow immediate next waypoint after reset.
        _nextMoveTime = currentUnixTime;
    }

    public void Update(double currentUnixTime)
    {
        // Patrol only acts when idle; combat AI handles combat.
        if (_creature.AttackTarget != null)
        {
            return;
        }

        if (_creature.EmoteManager.IsBusy)
        {
            return;
        }

        if (_path == null || _path.Count == 0)
        {
            return;
        }

        // If we have an in-flight destination, see if we've arrived or if we're stuck.
        if (_currentDest != null)
        {
            var dx = _creature.Location.Pos.X - _currentDest.Pos.X;
            var dy = _creature.Location.Pos.Y - _currentDest.Pos.Y;
            var dz = _creature.Location.Pos.Z - _currentDest.Pos.Z;

            var distSq = (dx * dx) + (dy * dy) + (dz * dz);

            if (distSq > (ArriveDistance * ArriveDistance))
            {
                if (currentUnixTime >= _nextProgressSampleTime)
                {
                    _nextProgressSampleTime = currentUnixTime + 0.5;

                    if (_lastSamplePos == null)
                    {
                        _lastSamplePos = new Position(_creature.Location);
                        _stuckSinceTime = currentUnixTime;
                    }
                    else
                    {
                        var mx = _creature.Location.Pos.X - _lastSamplePos.Pos.X;
                        var my = _creature.Location.Pos.Y - _lastSamplePos.Pos.Y;
                        var mz = _creature.Location.Pos.Z - _lastSamplePos.Pos.Z;

                        var movedSq = (mx * mx) + (my * my) + (mz * mz);

                        // ~10cm movement threshold
                        if (movedSq < 0.01f)
                        {
                            if ((currentUnixTime - _stuckSinceTime) >= 2.0)
                            {
                                TryDetourOrSkip(currentUnixTime);
                            }
                        }
                        else
                        {
                            _stuckSinceTime = currentUnixTime;
                            _stuckAttempts = 0;
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

        // Select next offset (looping)
        var offset = _path[_index];
        _index = (_index + 1) % _path.Count;

        // Base position is Home.
        var basePos = new Position(_creature.Home);
        var nextPos = new Position(basePos);

        // 2D-only movement: compute XY from home + offsets, Z from terrain.
        nextPos.PositionX = basePos.Pos.X + offset.Dx;
        nextPos.PositionY = basePos.Pos.Y + offset.Dy;

        // Update cell before terrain lookup.
        nextPos.LandblockId = new LandblockId(nextPos.GetCell());

        // Terrain height accounts for hills.
        nextPos.PositionZ = nextPos.GetTerrainZ();

        // Update cell again after Z assignment.
        nextPos.LandblockId = new LandblockId(nextPos.GetCell());

        // Pause: fixed override on waypoint, otherwise weenie random default range.
        _pauseOnArrivalSeconds = offset.PauseSeconds ?? GetDefaultPauseSeconds();

        _finalDest = nextPos;
        _currentDest = nextPos;

        _detouring = false;
        _stuckAttempts = 0;
        _stuckSinceTime = currentUnixTime;
        _lastSamplePos = new Position(_creature.Location);

        IssueMove(nextPos, currentUnixTime);

        // Small pacing guard (pause is applied on arrival).
        _nextMoveTime = currentUnixTime + 0.75;
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

    private void IssueMove(Position dest, double currentUnixTime)
    {
        var patrolSpeed = (float)(_creature.GetProperty(PropertyFloat.PatrolSpeed) ?? 1.0);

        _creature.MoveTo(dest, _creature.GetRunRate(), true, null, patrolSpeed);

        // After issuing a move, sample soon for stuck detection.
        _nextProgressSampleTime = currentUnixTime + 0.5;
    }

    private void TryDetourOrSkip(double currentUnixTime)
    {
        // If we're already detouring and still stuck, abort this leg and continue patrol.
        if (_detouring)
        {
            _currentDest = null;
            _finalDest = null;

            _stuckAttempts = 0;
            _pauseOnArrivalSeconds = 0f;
            _nextMoveTime = currentUnixTime + 0.25;
            return;
        }

        _stuckAttempts++;

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

        if (_stuckAttempts <= offsets.Length)
        {
            var (ox, oy) = offsets[_stuckAttempts - 1];

            var detour = new Position(_creature.Location);
            detour.PositionX = detour.Pos.X + ox;
            detour.PositionY = detour.Pos.Y + oy;

            detour.LandblockId = new LandblockId(detour.GetCell());
            detour.PositionZ = detour.GetTerrainZ();
            detour.LandblockId = new LandblockId(detour.GetCell());

            _detouring = true;
            _currentDest = detour;

            // Never pause on detours.
            _pauseOnArrivalSeconds = 0f;

            IssueMove(detour, currentUnixTime);

            _stuckSinceTime = currentUnixTime;
            _lastSamplePos = new Position(_creature.Location);
            return;
        }

        // Too many attempts: abandon this waypoint and continue patrol.
        _currentDest = null;
        _finalDest = null;

        _detouring = false;
        _stuckAttempts = 0;
        _pauseOnArrivalSeconds = 0f;
        _nextMoveTime = currentUnixTime + 0.25;
    }
}
