using System.Collections.Generic;

using ACE.Entity.Enum;
using ACE.Server.WorldObjects;

namespace ACE.Server.Factories.Tables
{
    public static class WeaponAnimationLength
    {
        private static readonly Dictionary<AttackType, float> MeleeAnimLength = new Dictionary<AttackType, float>()
        {
            { AttackType.DoubleSlash,           2.62f }, 
            { AttackType.DoubleStrike,          2.62f }, // use fastest, DoubleSlash
            { AttackType.DoubleThrust,          3.05f },
            { AttackType.Kick,                  1.10f },
            { AttackType.MultiStrike,           2.67f },
            { AttackType.Offhand,               1.0f },
            { AttackType.OffhandDoubleSlash,    2.1f },
            { AttackType.OffhandDoubleThrust,   2.1f },
            { AttackType.OffhandPunch,          0.87f }, // 0.87
            { AttackType.OffhandSlash,          1.20f }, // 1.20
            { AttackType.OffhandThrust,         1.20f }, // 1.20
            { AttackType.OffhandTripleSlash,    2.67f }, // 2.67
            { AttackType.OffhandTripleThrust,   2.67f },
            { AttackType.Punch,                 1.10f }, // thrust = 1.17 | slash = 1.10 | while dualwielding = 0.87
            { AttackType.Punches,               1.10f },
            { AttackType.Slash,                 1.33f }, // slash w/ shield = 1.61 | no shield backhand = 1.52 | no shield slash = 1.33)
            { AttackType.Slashes,               1.33f },
            { AttackType.Thrust,                1.38f }, // 1.38
            { AttackType.Thrusts,               1.38f },
            { AttackType.ThrustSlash,           1.33f },
            { AttackType.TripleSlash,           3.18f }, // just weapon = 3.18 | while dual wielding = 2.67
            { AttackType.TripleStrike,          3.18f }, // use fastest, TripleSlash
            { AttackType.TripleThrust,          4.64f },
            { AttackType.Unarmed,               1.10f },
        };

        private static readonly Dictionary<Skill, float> MissileAnimLength = new Dictionary<Skill, float>()
        {
            { Skill.Bow,                        1.057f },
            { Skill.Crossbow,                   1.59f },
            { Skill.ThrownWeapon,               1.85f } // Atlatl
        };

        public static float GetAnimLength(WorldObject weapon)
        {
            if (weapon == null)
                return 1.0f;

            float valueMod;

            if (weapon.IsTwoHanded)
                return 1.85f;
            else if (!weapon.IsAmmoLauncher && weapon.WeaponSkill == Skill.ThrownWeapon)
                return 2.33f;
            else if (!weapon.IsAmmoLauncher  && MeleeAnimLength.TryGetValue(weapon.W_AttackType, out valueMod))
                return valueMod;
            else if (weapon.IsAmmoLauncher && MissileAnimLength.TryGetValue(weapon.WeaponSkill, out valueMod))
                return valueMod;
            else
                return 1.0f;    // default?
        }
    }
}
