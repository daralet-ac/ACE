using System.Collections.Generic;
using ACE.Common;
using ACE.Server.Factories.Enum;

namespace ACE.Server.Factories.Tables.Wcids;

public static class EmpoweredScarabWcids
{
    private static readonly List<WeenieClassName> scarabColors = new List<WeenieClassName>()
    {
        WeenieClassName.empoweredScarabBlue_Life, // blue
        WeenieClassName.empoweredScarabBlue_War, // blue
        WeenieClassName.empoweredScarabYellow_Life, // yellow
        WeenieClassName.empoweredScarabYellow_War, // yellow
        WeenieClassName.empoweredScarabRed_Life, // red
        WeenieClassName.empoweredScarabRed_War, // red
    };

    public static WeenieClassName Roll(int tier)
    {
        switch (tier)
        {
            // blue only
            case 3:
            case 4:
            case 5:
                var rng = ThreadSafeRandom.Next(0, 1);
                return scarabColors[rng];

            // even chance between blue / yellow
            case 6:
                rng = ThreadSafeRandom.Next(0, 3);
                return scarabColors[rng];

            // even chance between blue / yellow / red
            case 7:
            case 8:
                rng = ThreadSafeRandom.Next(0, 5);
                return scarabColors[rng];
        }
        return WeenieClassName.undef;
    }

    private static readonly HashSet<WeenieClassName> _combined = new HashSet<WeenieClassName>();

    static EmpoweredScarabWcids()
    {
        foreach (var color in scarabColors)
        {
            _combined.Add(color);
        }
    }

    public static bool Contains(WeenieClassName wcid)
    {
        return _combined.Contains(wcid);
    }
}
