using System.Collections.Generic;
using ACE.Entity.Enum;

namespace ACE.Server.Physics;

public class PhysicsScriptTable
{
    public Dictionary<long, PhysicsScriptTableData> ScriptTable;

    public void Release() { }

    public uint GetScript(PlayScript? type, float? mod)
    {
        return 0;
    }
}
