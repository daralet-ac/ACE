using System.Collections.Generic;
using ACE.Server.Physics.Combat;

namespace ACE.Server.Physics.Managers;

public class AttackManager
{
    public float AttackRadius;
    public int CurrentAttack;
    public HashSet<AttackInfo> PendingAttacks;

    public AttackInfo NewAttack(int partIdx)
    {
        return null;
    }

    public void AttackDone(AttackInfo attackInfo) { }
}
