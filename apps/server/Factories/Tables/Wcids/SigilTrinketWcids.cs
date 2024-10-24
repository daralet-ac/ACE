using System.Collections.Generic;
using ACE.Common;
using ACE.Server.Factories.Enum;
using ACE.Server.WorldObjects;

namespace ACE.Server.Factories.Tables.Wcids;

public static class SigilTrinketWcids
{
    private static readonly List<WeenieClassName> SigilScarabWcids =
    [
        WeenieClassName.empoweredScarabBlue_Life, // blue
        WeenieClassName.empoweredScarabBlue_War, // blue
        WeenieClassName.empoweredScarabYellow_Life, // yellow
        WeenieClassName.empoweredScarabYellow_War, // yellow
        WeenieClassName.empoweredScarabRed_Life, // red
        WeenieClassName.empoweredScarabRed_War // red
    ];

    private static readonly List<WeenieClassName> SigilCompassWcids =
    [
        WeenieClassName.empoweredScarabBlue_Life, // blue
        WeenieClassName.empoweredScarabBlue_War, // blue
        WeenieClassName.empoweredScarabYellow_Life, // yellow
        WeenieClassName.empoweredScarabYellow_War, // yellow
        WeenieClassName.empoweredScarabRed_Life, // red
        WeenieClassName.empoweredScarabRed_War // red
    ];

    private static readonly List<WeenieClassName> SigilPuzzleBoxWcids =
    [
        WeenieClassName.empoweredScarabBlue_Life, // blue
        WeenieClassName.empoweredScarabBlue_War, // blue
        WeenieClassName.empoweredScarabYellow_Life, // yellow
        WeenieClassName.empoweredScarabYellow_War, // yellow
        WeenieClassName.empoweredScarabRed_Life, // red
        WeenieClassName.empoweredScarabRed_War // red
    ];

    private static readonly List<WeenieClassName> SigilPocketWatchWcids =
    [
        WeenieClassName.empoweredScarabBlue_Life, // blue
        WeenieClassName.empoweredScarabBlue_War, // blue
        WeenieClassName.empoweredScarabYellow_Life, // yellow
        WeenieClassName.empoweredScarabYellow_War, // yellow
        WeenieClassName.empoweredScarabRed_Life, // red
        WeenieClassName.empoweredScarabRed_War // red
    ];

    private static readonly List<WeenieClassName> SigilTopWcids =
    [
        WeenieClassName.empoweredScarabBlue_Life, // blue
        WeenieClassName.empoweredScarabBlue_War, // blue
        WeenieClassName.empoweredScarabYellow_Life, // yellow
        WeenieClassName.empoweredScarabYellow_War, // yellow
        WeenieClassName.empoweredScarabRed_Life, // red
        WeenieClassName.empoweredScarabRed_War // red
    ];

    private static readonly List<WeenieClassName> SigilGoggleWcids =
    [
        WeenieClassName.empoweredScarabBlue_Life, // blue
        WeenieClassName.empoweredScarabBlue_War, // blue
        WeenieClassName.empoweredScarabYellow_Life, // yellow
        WeenieClassName.empoweredScarabYellow_War, // yellow
        WeenieClassName.empoweredScarabRed_Life, // red
        WeenieClassName.empoweredScarabRed_War // red
    ];

    public static WeenieClassName Roll(int tier, SigilTrinketType sigilTrinketType)
    {
        switch (sigilTrinketType)
        {
            case SigilTrinketType.Scarab:
                switch (tier)
                {
                    default: // blue only
                        var rng = ThreadSafeRandom.Next(0, 1);
                        return SigilScarabWcids[rng];
                    case 6: // even chance between blue / yellow
                    case 7:
                        rng = ThreadSafeRandom.Next(0, 3);
                        return SigilScarabWcids[rng];
                    case 8: // even chance between blue / yellow / red
                        rng = ThreadSafeRandom.Next(0, 5);
                        return SigilScarabWcids[rng];
                }
            case SigilTrinketType.Compass:
                switch (tier)
                {
                    default: // blue only
                        var rng = ThreadSafeRandom.Next(0, 1);
                        return SigilCompassWcids[rng];
                    case 6: // even chance between blue / yellow
                    case 7:
                        rng = ThreadSafeRandom.Next(0, 3);
                        return SigilCompassWcids[rng];
                    case 8: // even chance between blue / yellow / red
                        rng = ThreadSafeRandom.Next(0, 5);
                        return SigilCompassWcids[rng];
                }
            case SigilTrinketType.PuzzleBox:
                switch (tier)
                {
                    default: // blue only
                        var rng = ThreadSafeRandom.Next(0, 1);
                        return SigilPuzzleBoxWcids[rng];
                    case 6: // even chance between blue / yellow
                    case 7:
                        rng = ThreadSafeRandom.Next(0, 3);
                        return SigilPuzzleBoxWcids[rng];
                    case 8: // even chance between blue / yellow / red
                        rng = ThreadSafeRandom.Next(0, 5);
                        return SigilPuzzleBoxWcids[rng];
                }
            case SigilTrinketType.PocketWatch:
                switch (tier)
                {
                    default: // blue only
                        var rng = ThreadSafeRandom.Next(0, 1);
                        return SigilPocketWatchWcids[rng];
                    case 6: // even chance between blue / yellow
                    case 7:
                        rng = ThreadSafeRandom.Next(0, 3);
                        return SigilPocketWatchWcids[rng];
                    case 8: // even chance between blue / yellow / red
                        rng = ThreadSafeRandom.Next(0, 5);
                        return SigilPocketWatchWcids[rng];
                }
            case SigilTrinketType.Top:
                switch (tier)
                {
                    default: // blue only
                        var rng = ThreadSafeRandom.Next(0, 1);
                        return SigilTopWcids[rng];
                    case 6: // even chance between blue / yellow
                    case 7:
                        rng = ThreadSafeRandom.Next(0, 3);
                        return SigilTopWcids[rng];
                    case 8: // even chance between blue / yellow / red
                        rng = ThreadSafeRandom.Next(0, 5);
                        return SigilTopWcids[rng];
                }
            case SigilTrinketType.Goggles:
                switch (tier)
                {
                    default: // blue only
                        var rng = ThreadSafeRandom.Next(0, 1);
                        return SigilGoggleWcids[rng];
                    case 6: // even chance between blue / yellow
                    case 7:
                        rng = ThreadSafeRandom.Next(0, 3);
                        return SigilGoggleWcids[rng];
                    case 8: // even chance between blue / yellow / red
                        rng = ThreadSafeRandom.Next(0, 5);
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
