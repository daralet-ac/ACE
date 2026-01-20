using System.Collections.Generic;

namespace ACE.Server.WorldObjects;

partial class Creature
{
    /// <summary>
    /// Patrols should only acquire monster targets (not players).
    /// This is intentionally simple: visible + attackable + is monster.
    /// </summary>
    private bool PatrolFindNextTarget()
    {
        var monsters = new List<Creature>();

        foreach (var c in PhysicsObj.ObjMaint.GetVisibleTargetsValuesOfTypeCreature())
        {
            if (c == null || c == this)
            {
                continue;
            }

            if (c.IsDead || !c.Attackable || c.Visibility)
            {
                continue;
            }

            if (!c.IsMonster)
            {
                continue;
            }

            monsters.Add(c);
        }

        if (monsters.Count == 0)
        {
            return false;
        }

        var nearest = BuildTargetDistance(monsters, true);

        if (nearest.Count == 0)
        {
            return false;
        }

        // Nearest distance is squared; compare to VisualAwarenessRangeSq
        if (nearest[0].Distance > VisualAwarenessRangeSq)
        {
            return false;
        }

        AttackTarget = nearest[0].Target;
        return true;
    }
}
