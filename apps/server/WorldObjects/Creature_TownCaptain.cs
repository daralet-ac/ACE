using System;
using ACE.Entity;
using ACE.Entity.Enum;

namespace ACE.Server.WorldObjects;

/// <summary>
/// A town's Captain of the Guard during an attack (TownEvent.TickCaptain): the event hands the Captain the nearest attacker
/// near its post, the Captain fights it with the usual monster AI, and walks back to its post once there's nothing left to
/// fight. The Captain isn't a monster (it can't be attacked), so it never looks for targets itself: it fights only what
/// the event gives it, and never a player.
/// </summary>
partial class Creature
{
    /// <summary>
    /// Fighting for its town, or walking back to its post afterwards. Runtime only.
    /// </summary>
    public bool TownCaptainOnDuty { get; private set; }

    private bool? isTownCaptain;

    /// <summary>
    /// A town's Captain of the Guard (TownCaptain.IsCaptain). Though a faction mob like the guards, it never hunts on its
    /// own: off duty it picks no targets and isn't woken by anything nearby (Creature.AlertMonster), so it doesn't go after
    /// the town's mice and hens.
    /// </summary>
    public bool IsTownCaptain => isTownCaptain ??= TownEvents.TownCaptain.IsCaptain(this);

    private Position townCaptainPost;
    private double townCaptainNextMoveTime;
    private double townCaptainReturnSince;

    // close enough to the post to count as back at it
    private const float TownCaptainPostArriveDistance = 1.5f;

    // a Captain not back at its post this long after it started back is put there
    private const double TownCaptainReturnSeconds = 30;

    /// <summary>
    /// Fights this attacker. Run on the Captain's landblock.
    /// </summary>
    /// <param name="post">Where the Captain stands, to go back to afterwards</param>
    public void TownCaptainEngage(Creature target, Position post)
    {
        if (IsDead || target == null || target.IsDead)
        {
            return;
        }

        if (!TownCaptainOnDuty)
        {
            townCaptainPost = new Position(post);
            TownCaptainOnDuty = true;
        }

        if (AttackTarget == target)
        {
            return;
        }

        if (AttackTarget == null)
        {
            firstUpdate = true;
        }

        AttackTarget = target;
        CurrentAttack = null;
        townCaptainReturnSince = 0;
        IsAwake = true;
        MonsterState = State.Awake;
    }

    /// <summary>
    /// Stops fighting and goes back to its post (nothing left in range, or the attack is over). Run on the Captain's
    /// landblock.
    /// </summary>
    public void TownCaptainStandDown()
    {
        if (!TownCaptainOnDuty || AttackTarget == null)
        {
            return;
        }

        TownCaptainDropTarget();
    }

    private void TownCaptainDropTarget()
    {
        AttackTarget = null;
        CurrentAttack = null;
        firstUpdate = true;
        townCaptainNextMoveTime = 0;
        townCaptainReturnSince = 0;

        if (IsMoving)
        {
            CancelMoveToForEmote();
        }

        if (CombatMode != CombatMode.NonCombat)
        {
            SetCombatMode(CombatMode.NonCombat);
        }
    }

    /// <summary>
    /// FindNextTarget for the Captain: keeps the target it was given while it lives and is still in the world, and never
    /// picks one of its own
    /// </summary>
    private bool TownCaptainKeepTarget()
    {
        if (AttackTarget is Creature && IsAttackTargetValid())
        {
            return true;
        }

        TownCaptainDropTarget();
        return false;
    }

    /// <summary>
    /// With nothing to fight: walks back to its post, and goes off duty there, facing the way it stood
    /// </summary>
    private void TownCaptainReturnToPost(double currentUnixTime)
    {
        if (townCaptainPost == null || Location == null)
        {
            TownCaptainOffDuty();
            return;
        }

        var dist = (float)Math.Sqrt(Location.SquaredDistanceTo(townCaptainPost));

        if (dist <= TownCaptainPostArriveDistance)
        {
            TownCaptainOffDuty();
            return;
        }

        if (townCaptainReturnSince == 0)
        {
            townCaptainReturnSince = currentUnixTime;
        }

        // a post it can't get back to (another landblock, or caught on something for long): put it back
        if (townCaptainPost.Landblock != Location.Landblock || currentUnixTime - townCaptainReturnSince > TownCaptainReturnSeconds)
        {
            CancelMoveToForEmote();
            FakeTeleport(townCaptainPost);
            TownCaptainOffDuty();
            return;
        }

        if (currentUnixTime < townCaptainNextMoveTime && IsMoving)
        {
            return;
        }

        if (CombatMode != CombatMode.NonCombat)
        {
            SetCombatMode(CombatMode.NonCombat);
        }

        MoveTo(new Position(townCaptainPost), GetRunRate(), true, null, null);
        IsMoving = true;

        // reissued now and then, in case a collision dropped the move
        townCaptainNextMoveTime = currentUnixTime + 5.0;
    }

    private void TownCaptainOffDuty()
    {
        if (IsMoving)
        {
            CancelMoveToForEmote();
        }

        TownCaptainOnDuty = false;
        townCaptainPost = null;
        townCaptainNextMoveTime = 0;
        townCaptainReturnSince = 0;
        IsAwake = false;
        MonsterState = State.Idle;
    }
}
