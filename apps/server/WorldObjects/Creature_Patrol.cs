using ACE.Common;
using ACE.Entity;
using ACE.Entity.Enum.Properties;
using ACE.Server.Entity;
using ACE.Server.WorldObjects.Patrol;
using System.Numerics;
using ACE.Entity.Enum;
namespace ACE.Server.WorldObjects;

partial class Creature
{
    private PatrolComponent _patrol;

    /// <summary>
    /// The waypoint a patroller placed partway round its route (PlaceAtPatrolSlot) heads for first.
    /// Read once by the PatrolComponent it starts with.
    /// </summary>
    public int? PatrolStartWaypoint { get; set; }

    /// <summary>
    /// For a patroller sharing a route (PatrolSlot / PatrolSlotCount), entering the world at its route start:
    /// moves its Location to where its slot is on the timetable right now, so a landblock that has just loaded
    /// has its patrols already spread round their routes instead of all starting at the route start and
    /// running or waiting into place. The slot's spot can be on a neighbouring landblock, which enters the
    /// world loaded the same way physics moving a monster over the edge would.
    /// Returns the route start, which stays the patroller's Home (the origin of its PatrolPath offsets),
    /// or null if it wasn't moved.
    /// </summary>
    private Position PlaceAtPatrolSlot()
    {
        if (!HasPatrol || Location == null || Location.Indoors)
        {
            return null;
        }

        var slot = GetProperty(PropertyInt.PatrolSlot) ?? 0;
        var slotCount = GetProperty(PropertyInt.PatrolSlotCount) ?? 0;

        if (slotCount < 2 || slot < 0 || slot >= slotCount)
        {
            return null;
        }

        var path = PatrolPath.Parse(GetProperty(PropertyString.PatrolPath));
        if (path == null)
        {
            return null;
        }

        var routeStart = new Position(Location);
        var startGlobal = routeStart.ToGlobal();

        var timetable = new PatrolTimetable(path, startGlobal, slot, slotCount);
        var point = timetable.ScheduledPoint(Time.GetUnixTime(), out var nextWaypoint, out var heading);

        var slotPosition = PatrolComponent.BuildOutdoorPosition(
            routeStart,
            point.X - startGlobal.X,
            point.Y - startGlobal.Y,
            InstanceId
        );

        if (heading != Vector2.Zero)
        {
            slotPosition.Rotate(new Vector3(heading, 0));
        }

        Location = slotPosition;
        PatrolStartWaypoint = nextWaypoint;

        return routeStart;
    }

    public bool HasPatrol => (GetProperty(PropertyBool.PatrolEnabled) ?? false);

    private PatrolComponent Patrol
    {
        get
        {
            if (!HasPatrol)
            {
                // Patrol disabled at runtime (e.g., emote toggle).
                // Clear component state so re-enable starts clean without carrying stale waypoint state.
                _patrol = null;
                return null;
            }

            _patrol ??= new PatrolComponent(this);
            return _patrol;
        }
    }
    
    public void PatrolUpdate(double currentUnixTime)
    {
        Patrol?.Update(currentUnixTime);
    }

    /// <summary>
    /// Clears any in-flight patrol destination so patrol can resume cleanly after combat.
    /// </summary>
    public void PatrolResetDestination()
    {
        Patrol?.ResetDestination();
    }
    public void CancelMoveToForEmote()
    {
        if (PhysicsObj?.MovementManager?.MoveToManager == null)
        {
            return;
        }

        PhysicsObj.MovementManager.MoveToManager.CancelMoveTo(WeenieError.ActionCancelled);
        PhysicsObj.MovementManager.MoveToManager.FailProgressCount = 0;
        PhysicsObj.StopCompletely(false);

        EnqueueBroadcastMotion(new ACE.Server.Entity.Motion(CurrentMotionState.Stance, MotionCommand.Ready));

        IsMoving = false;
        PhysicsObj.CachedVelocity = Vector3.Zero;
    }


}
