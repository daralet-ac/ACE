using System.Collections.Generic;
using ACE.Entity.Enum;

namespace ACE.Server.Factories.Tables;

public static class MonsterAverageAnimationLength
{
    private static readonly Dictionary<CreatureType, float> AnimLength = new Dictionary<CreatureType, float>()
    {
        { CreatureType.Apparition, 1.0f },
        { CreatureType.Armoredillo, 1.32f },
        { CreatureType.AunTumerok, 1.0f },
        { CreatureType.Auroch, 1.23f },
        { CreatureType.Banderling, 1.61f },
        { CreatureType.BlightedMoarsman, 1.0f },
        { CreatureType.Bunny, 1.0f },
        { CreatureType.Burun, 1.0f },
        { CreatureType.Carenzi, 0.73f },
        { CreatureType.Chicken, 1.0f },
        { CreatureType.Chittick, 1.33f },
        { CreatureType.Cow, 1.0f },
        { CreatureType.Crystal, 2.66f },
        { CreatureType.Doll, 2.13f },
        { CreatureType.Drudge, 1.06f },
        { CreatureType.Eater, 1.0f },
        { CreatureType.Elemental, 1.37f },
        { CreatureType.Empyrean, 1.0f },
        { CreatureType.Fiun, 1.0f },
        { CreatureType.GearKnight, 1.0f },
        { CreatureType.Ghost, 1.78f },
        { CreatureType.Golem, 1.67f },
        { CreatureType.GotrokLugian, 1.0f },
        { CreatureType.Grievver, 1.88f },
        { CreatureType.Gromnie, 1.42f },
        { CreatureType.Gurog, 1.0f },
        { CreatureType.HeaTumerok, 1.0f },
        { CreatureType.HollowMinion, 0.93f },
        { CreatureType.Human, 1.45f },
        { CreatureType.Idol, 1.67f },
        { CreatureType.Knathtead, 2.43f },
        { CreatureType.Lugian, 1.65f },
        { CreatureType.Margul, 2.79f },
        { CreatureType.Marionette, 1.38f },
        { CreatureType.Mattekar, 1.23f },
        { CreatureType.Mite, 1.09f },
        { CreatureType.Moar, 1.0f },
        { CreatureType.Moarsman, 1.0f },
        { CreatureType.Monouga, 1.6f },
        { CreatureType.Mosswart, 1.46f },
        { CreatureType.Mukkir, 1.0f },
        { CreatureType.Mumiyah, 1.0f },
        { CreatureType.Niffis, 2.2f },
        { CreatureType.Olthoi, 1.44f },
        { CreatureType.OlthoiLarvae, 1.0f },
        { CreatureType.ParadoxOlthoi, 1.0f },
        { CreatureType.Penguin, 1.0f },
        { CreatureType.PhyntosWasp, 0.5f },
        { CreatureType.Rabbit, 1.0f },
        { CreatureType.Rat, 1.83f },
        { CreatureType.Reedshark, 0.8f },
        { CreatureType.Remoran, 1.0f },
        { CreatureType.Ruschk, 1.0f },
        { CreatureType.Scarecrow, 1.0f },
        { CreatureType.Sclavus, 1.73f },
        { CreatureType.Shadow, 1.45f },
        { CreatureType.ShallowsShark, 0.8f },
        { CreatureType.Shreth, 1.43f },
        { CreatureType.Simulacrum, 1.45f },
        { CreatureType.Siraluun, 1.39f },
        { CreatureType.Skeleton, 1.88f },
        { CreatureType.Sleech, 1.0f },
        { CreatureType.Slithis, 1.0f },
        { CreatureType.Snowman, 1.0f },
        { CreatureType.Statue, 1.0f },
        { CreatureType.Swarm, 1.0f },
        { CreatureType.Thrungus, 1.0f },
        { CreatureType.Tumerok, 1.33f },
        { CreatureType.Tusker, 1.66f },
        { CreatureType.Undead, 1.79f },
        { CreatureType.Ursuin, 1.35f },
        { CreatureType.ViamontianKnight, 1.0f },
        { CreatureType.Virindi, 1.96f },
        { CreatureType.Wisp, 1.44f },
        { CreatureType.Zefir, 1.0f },
    };

    public static float GetValueMod(CreatureType? creatureType)
    {
        if (creatureType != null && AnimLength.TryGetValue(creatureType.Value, out var valueMod))
        {
            return valueMod;
        }
        else
        {
            return 1.0f; // default?
        }
    }
}
