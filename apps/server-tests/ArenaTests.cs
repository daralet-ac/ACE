using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ACE.Entity;
using ACE.Server.Arena;
using ACE.Server.Entity;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ACE.Server.Tests;

[TestClass]
public class ArenaTests
{
    #region Elo

    [TestMethod]
    public void Elo_EvenlyRatedPlayersTradeHalfOfK()
    {
        var (winner, loser) = ArenaElo.Rate(1400, 1400, 50);

        Assert.AreEqual(1425, winner);
        Assert.AreEqual(1375, loser);
    }

    [TestMethod]
    public void Elo_AnUpsetIsWorthMoreThanBeatingSomeoneWeaker()
    {
        var upset = ArenaElo.Rate(1200, 1600, 50);
        var expected = ArenaElo.Rate(1600, 1200, 50);

        Assert.IsTrue(upset.Winner - 1200 > expected.Winner - 1600);
        Assert.AreEqual(upset.Winner - 1200, 1600 - upset.Loser, "what the winner gains, the loser loses");
    }

    [TestMethod]
    public void Elo_AWinIsAlwaysWorthAPointAndNobodyGoesBelowZero()
    {
        var (winner, loser) = ArenaElo.Rate(3000, 0, 50);

        Assert.AreEqual(3001, winner);
        Assert.AreEqual(0, loser);
    }

    [TestMethod]
    public void Elo_ExpectedScoresOfTwoPlayersAddUpToOne()
    {
        Assert.AreEqual(1.0, ArenaElo.ExpectedScore(1500, 1300) + ArenaElo.ExpectedScore(1300, 1500), 1e-9);
        Assert.AreEqual(0.5, ArenaElo.ExpectedScore(1400, 1400), 1e-9);
    }

    [TestMethod]
    public void Elo_AKOfZeroChangesNothing()
    {
        Assert.AreEqual((1400, 1400), ArenaElo.Rate(1400, 1400, 0));
    }

    #endregion

    #region Queue

    private static readonly DateTime Start = new DateTime(2026, 10, 6, 12, 0, 0, DateTimeKind.Utc);

    private static ArenaQueueEntry Entry(
        uint guid,
        int level = 100,
        int band = 0,
        string address = null,
        int joinedSecondsAfterStart = 0
    )
    {
        return new ArenaQueueEntry
        {
            Guid = guid,
            Name = $"Player{guid}",
            Level = level,
            LevelBand = band,
            Address = address ?? $"10.0.0.{guid}",
            JoinedAt = Start.AddSeconds(joinedSecondsAfterStart)
        };
    }

    [TestMethod]
    public void Queue_PairsTheTwoWhoHaveWaitedLongest()
    {
        var queue = new ArenaQueue();
        queue.Add(Entry(1, joinedSecondsAfterStart: 0));
        queue.Add(Entry(2, joinedSecondsAfterStart: 1));
        queue.Add(Entry(3, joinedSecondsAfterStart: 2));

        Assert.IsTrue(queue.TryTakePair(true, out var first, out var second));
        Assert.AreEqual(1u, first.Guid);
        Assert.AreEqual(2u, second.Guid);
        Assert.AreEqual(1, queue.Count);
        Assert.IsFalse(queue.TryTakePair(true, out _, out _), "one player can't be paired");
    }

    [TestMethod]
    public void Queue_RespectsTheLevelBandOfEitherPlayer()
    {
        var queue = new ArenaQueue();
        queue.Add(Entry(1, level: 100, band: 10, joinedSecondsAfterStart: 0));
        queue.Add(Entry(2, level: 150, joinedSecondsAfterStart: 1));
        queue.Add(Entry(3, level: 105, joinedSecondsAfterStart: 2));

        Assert.IsTrue(queue.TryTakePair(true, out var first, out var second));
        Assert.AreEqual(1u, first.Guid);
        Assert.AreEqual(3u, second.Guid, "the level 150 is outside the band the first one asked for");

        // the band of the one who asked for it counts, whoever has waited longer
        Assert.IsFalse(ArenaQueue.CanMeet(Entry(4, level: 200), Entry(5, level: 100, band: 50), true));
        Assert.IsTrue(ArenaQueue.CanMeet(Entry(4, level: 140), Entry(5, level: 100, band: 50), true));
        Assert.IsTrue(ArenaQueue.CanMeet(Entry(4, level: 1), Entry(5, level: 275), true), "no band is any level");
    }

    [TestMethod]
    public void Queue_SkipsWhoeverCantMeetAnybody()
    {
        var queue = new ArenaQueue();
        queue.Add(Entry(1, level: 10, band: 5, joinedSecondsAfterStart: 0));
        queue.Add(Entry(2, level: 200, joinedSecondsAfterStart: 1));
        queue.Add(Entry(3, level: 210, joinedSecondsAfterStart: 2));

        Assert.IsTrue(queue.TryTakePair(true, out var first, out var second));
        Assert.AreEqual(2u, first.Guid);
        Assert.AreEqual(3u, second.Guid);
        Assert.AreEqual(1, queue.PositionOf(1), "the one nobody fits keeps waiting, first in line");
    }

    [TestMethod]
    public void Queue_NeverPairsTheSameAddressWhenThatIsBlocked()
    {
        var a = Entry(1, address: "192.168.1.5");
        var b = Entry(2, address: "192.168.1.5");

        Assert.IsFalse(ArenaQueue.CanMeet(a, b, blockSameAddress: true));
        Assert.IsTrue(ArenaQueue.CanMeet(a, b, blockSameAddress: false));

        // an address that is not known is nobody's address
        var unknownA = new ArenaQueueEntry
        {
            Guid = 3,
            Level = 100,
            JoinedAt = Start
        };
        var unknownB = new ArenaQueueEntry
        {
            Guid = 4,
            Level = 100,
            JoinedAt = Start
        };
        Assert.IsTrue(ArenaQueue.CanMeet(unknownA, unknownB, blockSameAddress: true));
    }

    [TestMethod]
    public void Queue_SomeoneWhoIsPutBackKeepsTheirPlace()
    {
        var queue = new ArenaQueue();
        var early = Entry(1, joinedSecondsAfterStart: 0);
        queue.Add(early);
        queue.Add(Entry(2, joinedSecondsAfterStart: 5));
        queue.Add(Entry(3, joinedSecondsAfterStart: 9));

        Assert.IsTrue(queue.Remove(1));
        Assert.AreEqual(0, queue.PositionOf(1));

        queue.Add(early);

        Assert.AreEqual(1, queue.PositionOf(1));
        Assert.AreEqual(3, queue.Count);
    }

    [TestMethod]
    public void Queue_JoiningAgainReplacesTheOldEntry()
    {
        var queue = new ArenaQueue();
        queue.Add(Entry(1, band: 0));
        queue.Add(Entry(1, band: 20));

        Assert.AreEqual(1, queue.Count);
        Assert.AreEqual(20, queue.Entries[0].LevelBand);
    }

    #endregion

    #region Maps

    private static readonly string ValidMap =
        @"{ ""arenas"": [ {
            ""name"": ""room"",
            ""landblocks"": [ ""0067"" ],
            ""starts"": [
                { ""cell"": ""0x00670117"", ""x"": 30, ""y"": -50, ""z"": 0 },
                { ""cell"": ""0x00670106"", ""x"": 10, ""y"": 0, ""z"": 0 }
            ]
        } ] }";

    private static List<ArenaMap> Parse(string json, out List<string> errors)
    {
        errors = new List<string>();
        return ArenaMapConfig.Parse(json, errors);
    }

    [TestMethod]
    public void Maps_AnIndoorMapNeedsOnlyItsLandblockAndTwoStarts()
    {
        var maps = Parse(ValidMap, out var errors);

        Assert.AreEqual(0, errors.Count, string.Join(" | ", errors));
        Assert.AreEqual(1, maps.Count);

        var map = maps[0];
        Assert.IsTrue(map.Enabled, "enabled unless it says otherwise");
        Assert.AreEqual("arena:room", map.Template.Name);
        Assert.AreEqual(1, map.Template.Footprint.Count, "a dungeon has no ring");
        Assert.IsFalse(map.Template.InstanceOnly, "the persistent world keeps its own copy");
        Assert.IsFalse(map.HasRadius);
        Assert.AreEqual(2, map.Starts.Count);
    }

    [TestMethod]
    public void Maps_AnOutdoorMapIsLoadedWithItsRingAndDisqualifiesOutsideItsRadius()
    {
        var maps = Parse(
            @"{ ""arenas"": [ {
                ""name"": ""field"",
                ""landblocks"": [ ""A9B4"" ],
                ""bufferRing"": 1,
                ""center"": { ""cell"": ""0xA9B40021"", ""x"": 96, ""y"": 96, ""z"": 20 },
                ""radius"": 40,
                ""starts"": [
                    { ""cell"": ""0xA9B40021"", ""x"": 76, ""y"": 96, ""z"": 20 },
                    { ""cell"": ""0xA9B40021"", ""x"": 116, ""y"": 96, ""z"": 20 }
                ]
            } ] }",
            out var errors
        );

        Assert.AreEqual(0, errors.Count, string.Join(" | ", errors));

        var map = maps.Single();
        Assert.AreEqual(9, map.Template.Footprint.Count);
        Assert.AreEqual(8, map.Template.Boundary.Count);
        Assert.IsTrue(map.HasRadius);
        Assert.AreEqual(0f, map.DistanceOutside(new Position(0xA9B40021, 100, 100, 20, 0, 0, 0, 1)));
        Assert.AreEqual(10f, map.DistanceOutside(new Position(0xA9B40021, 146, 96, 20, 0, 0, 0, 1)), 0.01f);
    }

    [TestMethod]
    public void Maps_MistakesLeaveOnlyThatMapOut()
    {
        var maps = Parse(
            @"{ ""arenas"": [
                { ""name"": ""room"", ""landblocks"": [ ""0067"" ], ""starts"": [ { ""cell"": ""0x00670117"" }, { ""cell"": ""0x00670106"" } ] },
                { ""name"": ""room"", ""landblocks"": [ ""0067"" ], ""starts"": [ { ""cell"": ""0x00670117"" }, { ""cell"": ""0x00670106"" } ] },
                { ""name"": ""one-start"", ""landblocks"": [ ""0067"" ], ""starts"": [ { ""cell"": ""0x00670117"" } ] },
                { ""name"": ""elsewhere"", ""landblocks"": [ ""0067"" ], ""starts"": [ { ""cell"": ""0x00670117"" }, { ""cell"": ""0x00AB0118"" } ] },
                { ""name"": ""bad landblock"", ""landblocks"": [ ""zz"" ], ""starts"": [ ] },
                { ""landblocks"": [ ""0067"" ] },
                { ""name"": ""open-field"", ""landblocks"": [ ""A9B4"" ], ""starts"": [ { ""cell"": ""0xA9B40021"", ""x"": 10, ""y"": 10 }, { ""cell"": ""0xA9B40021"", ""x"": 20, ""y"": 20 } ] },
                { ""name"": ""no-center"", ""landblocks"": [ ""0067"" ], ""radius"": 10, ""starts"": [ { ""cell"": ""0x00670117"" }, { ""cell"": ""0x00670106"" } ] },
                { ""name"": ""wrong-kind"", ""enabled"": ""yes"" }
            ] }",
            out var errors
        );

        Assert.AreEqual(1, maps.Count, string.Join(" | ", errors));
        Assert.AreEqual("room", maps[0].Name);

        bool Reported(string label, string words) => errors.Any(e => e.StartsWith(label) && e.Contains(words));

        Assert.IsTrue(Reported("arena 'room'", "same name"));
        Assert.IsTrue(Reported("arena 'one-start'", "at least two starts"));
        Assert.IsTrue(Reported("arena 'elsewhere'", "start #2 is not in one of the arena's landblocks"));
        Assert.IsTrue(Reported("arena 'bad landblock'", "only have letters"));
        Assert.IsTrue(Reported("arena 'bad landblock'", "\"zz\" is not a landblock"));
        Assert.IsTrue(Reported("arena #6", "no name"));
        Assert.IsTrue(Reported("arena 'open-field'", "bufferRing of at least 1"));
        Assert.IsTrue(Reported("arena 'open-field'", "needs a radius"));
        Assert.IsTrue(Reported("arena 'no-center'", "needs a center"));
        Assert.IsTrue(Reported("arena #9", "enabled"));
    }

    [TestMethod]
    public void Maps_AStartOutsideTheRadiusIsAMistake()
    {
        Parse(
            @"{ ""arenas"": [ {
                ""name"": ""field"",
                ""landblocks"": [ ""A9B4"" ],
                ""bufferRing"": 1,
                ""center"": { ""cell"": ""0xA9B40021"", ""x"": 96, ""y"": 96, ""z"": 20 },
                ""radius"": 10,
                ""starts"": [
                    { ""cell"": ""0xA9B40021"", ""x"": 96, ""y"": 96, ""z"": 20 },
                    { ""cell"": ""0xA9B40021"", ""x"": 150, ""y"": 96, ""z"": 20 }
                ]
            } ] }",
            out var errors
        );

        Assert.IsTrue(errors.Any(e => e.Contains("start #2 is outside the radius")), string.Join(" | ", errors));
    }

    [TestMethod]
    public void Maps_AnIslandCantTakeAnArenaName()
    {
        var errors = new List<string>();

        InstanceTemplateConfig.Parse(
            @"{ ""islands"": [ { ""name"": ""arena:pkl-arena"", ""landblocks"": [ ""0067"" ], ""bufferRing"": 0, ""instanceOnly"": false,
                ""entry"": { ""cell"": ""0x00670117"", ""x"": 30, ""y"": -50, ""z"": 0 } } ] }",
            errors
        );

        Assert.IsTrue(errors.Any(e => e.Contains("arena maps")), string.Join(" | ", errors));
    }

    [TestMethod]
    public void Maps_TheFileThatComesWithTheServerHasNoMistakes()
    {
        // apps/server/arenas.json is copied next to the server, and from there next to the tests
        var path = Path.Combine(AppContext.BaseDirectory, "arenas.json");

        if (!File.Exists(path))
        {
            Assert.Inconclusive("arenas.json is not next to the tests");
        }

        var maps = Parse(File.ReadAllText(path), out var errors);

        Assert.AreEqual(0, errors.Count, string.Join(" | ", errors));
        Assert.IsTrue(maps.Any(m => m.Enabled), "at least one map is there to duel in");
        Assert.IsTrue(maps.Single(m => m.Name == "pkl-arena").Enabled);

        foreach (var map in maps)
        {
            Assert.IsFalse(map.Template.InstanceOnly, map.Name);
            Assert.IsTrue(map.Starts.Count >= 2, map.Name);
        }
    }

    #endregion
}
