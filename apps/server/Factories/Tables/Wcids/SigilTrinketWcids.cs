using System.Collections.Generic;
using ACE.Common;
using ACE.Server.Factories.Enum;
using ACE.Server.WorldObjects;

namespace ACE.Server.Factories.Tables.Wcids;

public static class SigilTrinketWcids
{

    private static readonly List<WeenieClassName> SigilCompassWcids =
    [
        WeenieClassName.sigilCompassBlue,
        WeenieClassName.sigilCompassYellow,
        WeenieClassName.sigilCompassRed
    ];

    private static readonly List<WeenieClassName> SigilPuzzleBoxWcids =
    [
        WeenieClassName.sigilPuzzleBoxBlue,
        WeenieClassName.sigilPuzzleBoxYellow,
        WeenieClassName.sigilPuzzleBoxRed
    ];

    private static readonly List<WeenieClassName> SigilScarabWcids =
    [
        WeenieClassName.sigilScarabBlue,
        WeenieClassName.sigilScarabYellow,
        WeenieClassName.sigilScarabRed
    ];

    private static readonly List<WeenieClassName> SigilPocketWatchWcids =
    [
        WeenieClassName.sigilPocketWatchBlue,
        WeenieClassName.sigilPocketWatchYellow,
        WeenieClassName.sigilPocketWatchRed
    ];

    private static readonly List<WeenieClassName> SigilTopWcids =
    [
        WeenieClassName.sigilTopBlue,
        WeenieClassName.sigilTopYellow,
        WeenieClassName.sigilTopRed
    ];

    private static readonly List<WeenieClassName> SigilGoggleWcids =
    [
        WeenieClassName.sigilGogglesBlue,
        WeenieClassName.sigilGogglesYellow,
        WeenieClassName.sigilGogglesRed
    ];

    public static WeenieClassName Roll(int tier, SigilTrinketType sigilTrinketType)
    {
        switch (sigilTrinketType)
        {
            case SigilTrinketType.Scarab:
                switch (tier)
                {
                    default: // blue only
                        return SigilScarabWcids[0];
                    case 6: // even chance between blue / yellow
                    case 7:
                        var rng = ThreadSafeRandom.Next(0, 1);
                        return SigilScarabWcids[rng];
                    case 8: // even chance between blue / yellow / red
                        rng = ThreadSafeRandom.Next(0, 2);
                        return SigilScarabWcids[rng];
                }
            case SigilTrinketType.Compass:
                switch (tier)
                {
                    default: // blue only
                        return SigilCompassWcids[0];
                    case 6: // even chance between blue / yellow
                    case 7:
                        var rng = ThreadSafeRandom.Next(0, 1);
                        return SigilCompassWcids[rng];
                    case 8: // even chance between blue / yellow / red
                        rng = ThreadSafeRandom.Next(0, 2);
                        return SigilCompassWcids[rng];
                }
            case SigilTrinketType.PuzzleBox:
                switch (tier)
                {
                    default: // blue only
                        return SigilPuzzleBoxWcids[0];
                    case 6: // even chance between blue / yellow
                    case 7:
                        var rng = ThreadSafeRandom.Next(0, 1);
                        return SigilPuzzleBoxWcids[rng];
                    case 8: // even chance between blue / yellow / red
                        rng = ThreadSafeRandom.Next(0, 2);
                        return SigilPuzzleBoxWcids[rng];
                }
            case SigilTrinketType.PocketWatch:
                switch (tier)
                {
                    default: // blue only
                        return SigilPocketWatchWcids[0];
                    case 6: // even chance between blue / yellow
                    case 7:
                        var rng = ThreadSafeRandom.Next(0, 1);
                        return SigilPocketWatchWcids[rng];
                    case 8: // even chance between blue / yellow / red
                        rng = ThreadSafeRandom.Next(0, 2);
                        return SigilPocketWatchWcids[rng];
                }
            case SigilTrinketType.Top:
                switch (tier)
                {
                    default: // blue only
                        return SigilTopWcids[0];
                    case 6: // even chance between blue / yellow
                    case 7:
                        var rng = ThreadSafeRandom.Next(0, 1);
                        return SigilTopWcids[rng];
                    case 8: // even chance between blue / yellow / red
                        rng = ThreadSafeRandom.Next(0, 2);
                        return SigilTopWcids[rng];
                }
            case SigilTrinketType.Goggles:
                switch (tier)
                {
                    default: // blue only
                        return SigilGoggleWcids[0];
                    case 6: // even chance between blue / yellow
                    case 7:
                        var rng = ThreadSafeRandom.Next(0, 1);
                        return SigilGoggleWcids[rng];
                    case 8: // even chance between blue / yellow / red
                        rng = ThreadSafeRandom.Next(0, 2);
                        return SigilGoggleWcids[rng];
                }
        }

        return WeenieClassName.undef;
    }

    private static readonly HashSet<WeenieClassName> Combined = new HashSet<WeenieClassName>();

    static SigilTrinketWcids()
    {
        foreach (var color in SigilScarabWcids)
        {
            Combined.Add(color);
        }
    }

    public static bool Contains(WeenieClassName wcid)
    {
        return Combined.Contains(wcid);
    }
}
