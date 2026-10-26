using System.Collections.Generic;
using ACE.Entity.Enum;

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

            // CombatPets count as monsters, but they belong to players
            if (!c.IsMonster || c is CombatPet)
            {
                continue;
            }

            // Harmless animals (rabbits, chickens, cows, rats) never start a fight - leave them alone
            // unless one is fighting someone right now
            if (c.Tolerance.HasFlag(Tolerance.NoAttack)
                || c.Tolerance.HasFlag(Tolerance.Retaliate) && c.AttackTarget == null)
            {
                continue;
            }

            // Fellow defenders: creatures that only fight monsters (other guards, training recruits) or share
            // this patroller's faction
            if (c.Tolerance.HasFlag(Tolerance.Monster) || SameFaction(c))
            {
                continue;
            }

            monsters.Add(c);
        }

        if (monsters.Count == 0)
        {
            PatrolDropTarget();
            return false;
        }

        var nearest = BuildTargetDistance(monsters, true);

        if (nearest.Count == 0)
        {
            PatrolDropTarget();
            return false;
        }

        // Nearest distance is squared; compare to VisualAwarenessRangeSq
        if (nearest[0].Distance > VisualAwarenessRangeSq)
        {
            PatrolDropTarget();
            return false;
        }

        AttackTarget = nearest[0].Target;
        return true;
    }

    /// <summary>
    /// Gives up the current target once nothing valid is left in range, so the patrol resumes
    /// instead of holding a target that fled out of range (or past MaxChaseRange) forever.
    /// </summary>
    private void PatrolDropTarget()
    {
        if (AttackTarget == null)
        {
            return;
        }

        AttackTarget = null;
        CurrentAttack = null;
        firstUpdate = true;

        if (IsMoving)
        {
            CancelMoveToForEmote();
        }

        if (CombatMode != CombatMode.NonCombat)
        {
            SetCombatMode(CombatMode.NonCombat);
        }

        PatrolResetDestination();
    }
}
