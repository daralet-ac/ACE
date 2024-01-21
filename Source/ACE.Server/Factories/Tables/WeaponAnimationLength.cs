using System.Collections.Generic;

using ACE.Entity.Enum;
using ACE.Server.WorldObjects;

namespace ACE.Server.Factories.Tables
{
    public static class WeaponAnimationLength
    {
        private static readonly Dictionary<AttackType, float> MeleeAnimLength = new Dictionary<AttackType, float>()
        {
            { AttackType.DoubleSlash,           1.0f }, 
            { AttackType.DoubleStrike,          1.0f },
            { AttackType.DoubleThrust,          1.0f },
            { AttackType.Kick,                  1.0f },
            { AttackType.MultiStrike,           2.67f },
            { AttackType.Offhand,               1.0f },
            { AttackType.OffhandDoubleSlash,    1.0f },
            { AttackType.OffhandDoubleThrust,   1.0f },
            { AttackType.OffhandPunch,          0.87f }, // 0.87
            { AttackType.OffhandSlash,          1.20f }, // 1.20
            { AttackType.OffhandThrust,         1.20f }, // 1.20
            { AttackType.OffhandTripleSlash,    2.67f }, // 2.67
            { AttackType.OffhandTripleThrust,   1.0f },
            { AttackType.Punch,                 1.10f }, // thrust = 1.17 | slash = 1.10 | while dualwielding = 0.87
            { AttackType.Punches,               1.0f },
            { AttackType.Slash,                 1.61f }, // slash w/ shield = 1.61 | no shield backhand = 1.52 | no shield slash = 1.33)
            { AttackType.Slashes,               1.0f },
            { AttackType.Thrust,                1.38f }, // 1.38
            { AttackType.Thrusts,               1.0f },
            { AttackType.TripleSlash,           2.67f }, // just weapon = 3.18 | while dual wielding = 2.67
            { AttackType.TripleStrike,          1.0f }, // average slash and thrust?
            { AttackType.TripleThrust,          4.64f },
            { AttackType.Unarmed,               1.0f },
        };

        public static float GetAnimLength(WorldObject weapon)
        {
            if (weapon != null && !weapon.IsAmmoLauncher && MeleeAnimLength.TryGetValue(weapon.W_AttackType, out var valueMod))
                return valueMod;
            else
                return 1.0f;    // default?
        }
    }
}
