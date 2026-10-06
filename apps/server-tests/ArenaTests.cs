using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ACE.Entity;
using ACE.Server.Arena;
using ACE.Server.Entity;
using ACE.Server.Managers;
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

    [TestMethod]
    public void Queue_OnlyPairsPlayersWhoWantTheSameKindOfDuel()
    {
        var raw = Entry(1);
        var scaled = new ArenaQueueEntry
        {
            Guid = 2,
            Level = 100,
            Scaled = true,
            Address = "10.0.0.2",
            JoinedAt = Start
        };
        var unrated = new ArenaQueueEntry
        {
            Guid = 3,
            Level = 100,
            Rated = false,
            Address = "10.0.0.3",
            JoinedAt = Start
        };
        var scaledToo = new ArenaQueueEntry
        {
            Guid = 4,
            Level = 30,
            Scaled = true,
            Address = "10.0.0.4",
            JoinedAt = Start
        };

        Assert.IsTrue(raw.Rated, "rated unless asked otherwise");
        Assert.IsFalse(ArenaQueue.CanMeet(raw, scaled, true));
        Assert.IsFalse(ArenaQueue.CanMeet(raw, unrated, true));
        Assert.IsFalse(ArenaQueue.CanMeet(scaled, unrated, true));
        Assert.IsTrue(ArenaQueue.CanMeet(scaled, scaledToo, true));

        var queue = new ArenaQueue();
        queue.Add(raw);
        queue.Add(scaled);
        queue.Add(unrated);
        queue.Add(scaledToo);

        Assert.IsTrue(queue.TryTakePair(true, out var first, out var second));
        Assert.AreEqual(2u, first.Guid, "the raw and the unrated player have nobody to meet");
        Assert.AreEqual(4u, second.Guid);
        Assert.IsFalse(queue.TryTakePair(true, out _, out _));
    }

    [TestMethod]
    public void Queue_PairsFellowshipsOfTheSameSizeOnly()
    {
        ArenaQueueEntry Fellowship(uint leader, int size, int joined, string addressPrefix = "10.1")
        {
            var members = Enumerable
                .Range(0, size)
                .Select(i => new ArenaQueueMember
                {
                    Guid = leader + (uint)i,
                    Name = $"Player{leader + i}",
                    Level = 100,
                    Address = $"{addressPrefix}.{leader}.{i}"
                })
                .ToList();

            return new ArenaQueueEntry
            {
                Guid = leader,
                Name = $"Player{leader}",
                Level = 100,
                Address = members[0].Address,
                JoinedAt = Start.AddSeconds(joined),
                Members = members
            };
        }

        var three = Fellowship(100, 3, joined: 0);
        var two = Fellowship(200, 2, joined: 1);
        var alone = Entry(300, joinedSecondsAfterStart: 2);
        var otherThree = Fellowship(400, 3, joined: 3);

        Assert.AreEqual(1, alone.Size, "one player is an entry of one");
        Assert.IsTrue(three.Includes(102));
        Assert.AreEqual("Player100's fellowship of 3", three.Describe());

        var queue = new ArenaQueue();
        queue.Add(three);
        queue.Add(two);
        queue.Add(alone);
        queue.Add(otherThree);

        Assert.AreEqual(1, queue.PositionOf(101), "everyone in a fellowship waits in its place");
        Assert.AreSame(three, queue.Find(102));

        Assert.IsTrue(queue.TryTakePair(true, out var first, out var second));
        Assert.AreSame(three, first);
        Assert.AreSame(otherThree, second, "the fellowship of two and the lone player have nobody of their size");
        Assert.IsFalse(queue.TryTakePair(true, out _, out _));

        // one shared address between the sides is enough to keep them apart, when that is blocked
        var sharing = Fellowship(500, 3, joined: 4);
        var sharingToo = new ArenaQueueEntry
        {
            Guid = 600,
            Name = "Player600",
            Level = 100,
            JoinedAt = Start.AddSeconds(5),
            Members = new[]
            {
                new ArenaQueueMember
                {
                    Guid = 600,
                    Name = "Player600",
                    Level = 100,
                    Address = "1.2.3.4"
                },
                new ArenaQueueMember
                {
                    Guid = 601,
                    Name = "Player601",
                    Level = 100,
                    Address = sharing.Members[2].Address
                },
                new ArenaQueueMember
                {
                    Guid = 602,
                    Name = "Player602",
                    Level = 100,
                    Address = "1.2.3.5"
                }
            }
        };

        Assert.IsFalse(ArenaQueue.CanMeet(sharing, sharingToo, blockSameAddress: true));
        Assert.IsTrue(ArenaQueue.CanMeet(sharing, sharingToo, blockSameAddress: false));
    }

    [TestMethod]
    public void Queue_TakingOneMemberTakesTheWholeFellowship()
    {
        var queue = new ArenaQueue();
        var fellowship = new ArenaQueueEntry
        {
            Guid = 1,
            Name = "Leader",
            Level = 50,
            JoinedAt = Start,
            Members = new[]
            {
                new ArenaQueueMember
                {
                    Guid = 1,
                    Name = "Leader",
                    Level = 50
                },
                new ArenaQueueMember
                {
                    Guid = 2,
                    Name = "Member",
                    Level = 40
                }
            }
        };

        queue.Add(Entry(2, joinedSecondsAfterStart: 1));
        queue.Add(fellowship);

        Assert.AreEqual(1, queue.Count, "the member who waited alone now waits with the fellowship");
        Assert.AreSame(fellowship, queue.Take(2));
        Assert.AreEqual(0, queue.Count);
    }

    #endregion

    #region Commands

    [TestMethod]
    public void Commands_DuelOptionsAreTakenFromWhatWasTyped()
    {
        var name = ACE.Server.Commands.PlayerCommands.ArenaCommand.TakeDuelOptions(
            new[] { "Bob", "the", "Brave", "unrated", "Scaled", "fellowship" },
            onlyAtTheEnd: true,
            out var options
        );

        Assert.AreEqual("Bob the Brave", string.Join(" ", name));
        Assert.IsTrue(options.Scaled);
        Assert.IsTrue(options.Unrated);
        Assert.IsTrue(options.Fellowship);

        // in a name only the words at the end count
        name = ACE.Server.Commands.PlayerCommands.ArenaCommand.TakeDuelOptions(
            new[] { "Unrated", "Bob" },
            onlyAtTheEnd: true,
            out options
        );

        Assert.AreEqual("Unrated Bob", string.Join(" ", name));
        Assert.IsFalse(options.Scaled);
        Assert.IsFalse(options.Unrated);
        Assert.IsFalse(options.Fellowship);

        // for the queue they count anywhere
        var rest = ACE.Server.Commands.PlayerCommands.ArenaCommand.TakeDuelOptions(
            new[] { "scaled", "10", "fellow", "unrated" },
            onlyAtTheEnd: false,
            out options
        );

        CollectionAssert.AreEqual(new[] { "10" }, rest);
        Assert.IsTrue(options.Scaled);
        Assert.IsTrue(options.Unrated);
        Assert.IsTrue(options.Fellowship);
    }

    [TestMethod]
    public void Commands_BoardsAreNamedBySizeAndKind()
    {
        Assert.AreEqual(new ArenaBoard(1, false), ACE.Server.Commands.PlayerCommands.ArenaCommand.ParseBoard(""));
        Assert.AreEqual(new ArenaBoard(3, false), ACE.Server.Commands.PlayerCommands.ArenaCommand.ParseBoard("3v3"));
        Assert.AreEqual(
            new ArenaBoard(2, true),
            ACE.Server.Commands.PlayerCommands.ArenaCommand.ParseBoard("scaled 2v2")
        );
        Assert.AreEqual(new ArenaBoard(4, false), ACE.Server.Commands.PlayerCommands.ArenaCommand.ParseBoard("4 raw"));
        Assert.IsNull(
            ACE.Server.Commands.PlayerCommands.ArenaCommand.ParseBoard("2v3"),
            "the sides of a board are the same size"
        );
        Assert.IsNull(ACE.Server.Commands.PlayerCommands.ArenaCommand.ParseBoard("10v10"), "no fellowship is that big");
        Assert.IsNull(ACE.Server.Commands.PlayerCommands.ArenaCommand.ParseBoard("best"));

        Assert.AreEqual("1v1", ArenaBoard.OneOnOne.Name);
        Assert.AreEqual("3v3 scaled", new ArenaBoard(3, true).Name);
    }

    #endregion

    #region Team ratings

    [TestMethod]
    public void Elo_ATeamIsRatedAgainstTheOtherSidesAverage()
    {
        var (winners, losers) = ArenaElo.RateTeams(new[] { 1400, 1500 }, new[] { 1450, 1450 }, 50);

        // the averages are even, so each winner gains what beating a 1450 is worth to them
        Assert.AreEqual(ArenaElo.Rate(1400, 1450, 50).Winner, winners[0]);
        Assert.AreEqual(ArenaElo.Rate(1500, 1450, 50).Winner, winners[1]);
        Assert.AreEqual(ArenaElo.Rate(1450, 1450, 50).Loser, losers[0]);
        Assert.IsTrue(winners[0] - 1400 > winners[1] - 1500, "the weaker winner gains more");

        var single = ArenaElo.RateTeams(new[] { 1400 }, new[] { 1600 }, 50);
        Assert.AreEqual(
            ArenaElo.Rate(1400, 1600, 50),
            (single.Winners[0], single.Losers[0]),
            "one against one is plain Elo"
        );
    }

    [TestMethod]
    public void Boards_TeamBoardsAreKeptTogetherAndSurviveBadData()
    {
        var boards = ArenaBoards.ParseTeamBoards(null);
        Assert.AreEqual(0, boards.Count);

        boards["2v2"] = new ArenaStanding { Rating = 1425, Wins = 1 };
        boards["3v3 scaled"] = new ArenaStanding
        {
            Rating = 1380,
            Losses = 2,
            Draws = 1
        };

        var json = ArenaBoards.WriteTeamBoards(boards);
        var back = ArenaBoards.ParseTeamBoards(json);

        Assert.AreEqual(2, back.Count);
        Assert.AreEqual(1425, back["2v2"].Rating);
        Assert.AreEqual(3, back["3v3 scaled"].Duels);
        Assert.IsFalse(json.Contains("Duels"), "what can be worked out is not kept");

        Assert.AreEqual(
            0,
            ArenaBoards.ParseTeamBoards("{ not json").Count,
            "a duel is still recorded over something that can't be read"
        );
    }

    #endregion

    #region Scaling

    [TestMethod]
    public void Scaling_DamageInADuelFollowsTheFightersHealth()
    {
        var down = LevelScaling.GetDuelHealthRatio(attackerLevel: 100, defenderLevel: 30);
        var up = LevelScaling.GetDuelHealthRatio(attackerLevel: 30, defenderLevel: 100);

        Assert.IsTrue(down < 1.0f, "the higher fighter does less damage to the lower one");
        Assert.IsTrue(up > 1.0f, "and the lower one does more to the higher one");
        Assert.AreEqual(1.0f, down * up, 0.0001f);
        Assert.AreEqual(1.0f, LevelScaling.GetDuelHealthRatio(50, 50), 0.0001f);
    }

    #endregion

    #region Settings

    [TestMethod]
    public void Settings_AdminsCanTurnDuelingOffAndSetTheMinimumLevel()
    {
        // /modifybool and /modifylong only change properties that have a default
        Assert.IsTrue(
            DefaultPropertyManager.DefaultBooleanProperties.TryGetValue("arena_dueling_enabled", out var enabled)
        );
        Assert.IsTrue(enabled.Item, "dueling is on until an admin turns it off");

        Assert.IsTrue(
            DefaultPropertyManager.DefaultLongProperties.TryGetValue("arena_dueling_minimum_level", out var minimum)
        );
        Assert.AreEqual(1L, minimum.Item, "any level, until an admin sets one");
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
