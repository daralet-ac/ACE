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

    // Stuck handling: patrol does not use pathfinding, so we detect lack of progress
    // and do small detours ("nudges") to get around world objects.
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
    }

    /// <summary>
    /// Clears any in-flight patrol destination. Used when combat starts/ends so patrol cannot
    /// get wedged "waiting to arrive" at a destination that was interrupted.
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

        // Allow an immediate waypoint issue after reset.
        _nextMoveTime = currentUnixTime;
    }

    public void Update(double currentUnixTime)
    {
        // Patrol only acts when idle; combat AI does combat.
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

        // If we already have a destination, just wait until we're close enough
        if (_currentDest != null)
        {
            var dx = _creature.Location.Pos.X - _currentDest.Pos.X;
            var dy = _creature.Location.Pos.Y - _currentDest.Pos.Y;
            var dz = _creature.Location.Pos.Z - _currentDest.Pos.Z;

            var distSq = (dx * dx) + (dy * dy) + (dz * dz);

            if (distSq > (ArriveDistance * ArriveDistance))
            {
                // Detect if we're making progress toward the destination.
                // If not, do a small detour move to try to get around collisions.
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

                        // If we've barely moved since the last sample, we may be stuck.
                        if (movedSq < (0.1f * 0.1f))
                        {
                            // Give it a short window before declaring stuck.
                            if ((currentUnixTime - _stuckSinceTime) >= 2.0)
                            {
                                TryDetourOrSkip(currentUnixTime);
                            }
                        }
                        else
                        {
                            // We moved. Reset stuck tracking.
                            _stuckSinceTime = currentUnixTime;
                            _stuckAttempts = 0;
                            _lastSamplePos = new Position(_creature.Location);
                        }
                    }
                }

                return;
            }

            // Arrived
            _currentDest = null;

            // If we were detouring, immediately resume the final destination.
            if (_detouring && _finalDest != null)
            {
                _detouring = false;
                _currentDest = _finalDest;
                IssueMove(_finalDest, currentUnixTime);
                return;
            }

            _finalDest = null;
        }

        // Optional: small cooldown between waypoints (prevents immediate snap-turn at corners)
        if (currentUnixTime < _nextMoveTime)
        {
            return;
        }

        var offset = _path[_index];
        _index = (_index + 1) % _path.Count;

        // Base: Home (which is set to spawn location automatically)
        var basePos = new Position(_creature.Home);
        var nextPos = new Position(basePos);

        // NOTE: Position.Pos returns by value in this codebase; do not mutate its components directly.
        nextPos.PositionX = basePos.Pos.X + offset.Dx;
        nextPos.PositionY = basePos.Pos.Y + offset.Dy;
        nextPos.PositionZ = basePos.Pos.Z + offset.Dz;

        // 1) update cell/landblock based on new XY FIRST
        nextPos.LandblockId = new LandblockId(nextPos.GetCell());

        // 2) now terrain height lookup will be in the correct landblock/cell context
        nextPos.PositionZ = nextPos.GetTerrainZ();

        // 3) update cell again in case Z changes indoor/outdoor cell selection
        nextPos.LandblockId = new LandblockId(nextPos.GetCell());

        // Patrol speed (falls back to 1.0 if not set)
        var patrolSpeed = (float)(_creature.GetProperty(PropertyFloat.PatrolSpeed) ?? 1.0);

        // Issue move and remember destination
        _finalDest = nextPos;
        _currentDest = nextPos;
        _detouring = false;
        _stuckAttempts = 0;
        _stuckSinceTime = currentUnixTime;
        _lastSamplePos = new Position(_creature.Location);

        IssueMove(nextPos, currentUnixTime);


        // Small cooldown so we don’t immediately issue the next leg
        _nextMoveTime = currentUnixTime + 0.75;

    }

    private void IssueMove(Position dest, double currentUnixTime)
    {
        // Patrol speed (falls back to 1.0 if not set)
        var patrolSpeed = (float)(_creature.GetProperty(PropertyFloat.PatrolSpeed) ?? 1.0);
        _creature.MoveTo(dest, _creature.GetRunRate(), true, null, patrolSpeed);

        // After issuing a move, schedule the next sample shortly.
        _nextProgressSampleTime = currentUnixTime + 0.5;
    }

    private void TryDetourOrSkip(double currentUnixTime)
    {
        // If we're already detouring, don't stack detours; just give up on this leg.
        if (_detouring)
        {
            _currentDest = null;
            _finalDest = null;
            _stuckAttempts = 0;
            _nextMoveTime = currentUnixTime + 0.25;
            return;
        }

        _stuckAttempts++;

        // A few deterministic nudges around the creature.
        // (No RNG to keep behavior stable and avoid threading concerns.)
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
            IssueMove(detour, currentUnixTime);

            // Give the detour a short window before we consider ourselves stuck again.
            _stuckSinceTime = currentUnixTime;
            _lastSamplePos = new Position(_creature.Location);
            return;
        }

        // Too many attempts: abandon this waypoint and continue the patrol.
        _currentDest = null;
        _finalDest = null;
        _detouring = false;
        _stuckAttempts = 0;
        _nextMoveTime = currentUnixTime + 0.25;
    }
}
