using ACE.Entity.Enum.Properties;
using ACE.Server.WorldObjects.Patrol;

namespace ACE.Server.WorldObjects;

partial class Creature
{
    private PatrolComponent _patrol;

    public bool HasPatrol => (GetProperty(PropertyBool.PatrolEnabled) ?? false);

    private PatrolComponent Patrol
    {
        get
        {
            if (!HasPatrol)
            {
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
}
