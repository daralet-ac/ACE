using System.Linq;
using ACE.Server.Commands.PlayerCommands;
using ACE.Server.Entity.PlayerUi;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ACE.Server.Tests;

[TestClass]
public class PlayerUiProtocolTests
{
    [TestMethod]
    public void BuildMessages_UsesThePlayerPrefix()
    {
        var messages = PlayerUiProtocol.BuildMessages("hello", "{\"protocol\":2}", 7);

        Assert.AreEqual(1, messages.Count);
        Assert.AreEqual("DAP2|hello|7|1|1|eyJwcm90b2NvbCI6Mn0=", messages[0]);
    }

    [TestMethod]
    public void EveryChunkIsAShortLine()
    {
        var json = new string('x', 5000);

        var messages = PlayerUiProtocol.BuildMessages("fellowship", json, 9);

        Assert.IsTrue(messages.All(m => m.Length < 260));
        Assert.IsTrue(messages.All(m => m.StartsWith("DAP2|fellowship|9|")));
        Assert.AreEqual(json, messages.JoinedJson());
    }

    [TestMethod]
    public void BuildMessages_LongReplyIsSplitLikeAdminUi()
    {
        var json = new string('x', PlayerUiProtocol.ChunkSize + 5);

        var messages = PlayerUiProtocol.BuildMessages("hello", json, 3);

        Assert.AreEqual(2, messages.Count);
        Assert.IsTrue(messages.All(m => m.StartsWith("DAP2|hello|3|")));
        Assert.AreEqual(json, messages.JoinedJson());
    }

    [TestMethod]
    public void BuildMessages_NoLineHasReadableText()
    {
        // Virindi Chat System posts its own copy of a chat line with coordinates in it, which the plugin can't hide
        var json = "{\"members\":[{\"name\":\"+Spellsword\",\"location\":\"Hebian-to (39.0S, 83.0E)\"}]}";

        var messages = PlayerUiProtocol.BuildMessages("fellowship", json, 1);

        Assert.IsFalse(messages.Any(m => m.Contains("39.0S") || m.Contains("Hebian") || m.Contains("members")));
        Assert.AreEqual(json, messages.JoinedJson());
    }

    /// <summary>
    /// The names the plugin's HelloInfo reads (Feed/Models.cs in daralet-player-plugin).
    /// </summary>
    [TestMethod]
    public void HelloJsonHasTheNamesThePluginReads()
    {
        var hello = new PlayerUiHello(1, "Bob Smith", 0x50000001, PlayerUi.Kinds, PlayerUi.Features);

        var json = PlayerUiProtocol.BuildMessages("hello", hello).JoinedJson();

        StringAssert.StartsWith(json, "{\"protocol\":1,\"name\":\"Bob Smith\",\"guid\":1342177281,\"kinds\":[\"hello\",");
        StringAssert.EndsWith(json, ",\"features\":[\"packid\"]}");
    }

    [TestMethod]
    public void ParseGuids_ReadsHexAndDecimalAndSkipsTheRest()
    {
        var guids = PlayerUiData.ParseGuids(["0x80001234", "1342177281", "bob", "0", "-5", "0xZZ"]);

        CollectionAssert.AreEqual(new uint[] { 0x80001234, 1342177281 }, guids);
    }

    [TestMethod]
    public void ParseGuids_StopsAtTheLimit()
    {
        var guids = PlayerUiData.ParseGuids(Enumerable.Range(1, 10).Select(i => i.ToString()));

        Assert.AreEqual(PlayerUiData.MaxTargets, guids.Count);
    }

    [TestMethod]
    public void SecondsLeft_CountsDownFromTheDuration()
    {
        Assert.AreEqual(1800 - 60, PlayerUiData.SecondsLeft(new() { Duration = 1800, StartTime = -60 }));
        Assert.AreEqual(0, PlayerUiData.SecondsLeft(new() { Duration = 30, StartTime = -45 }));
        Assert.AreEqual(-1, PlayerUiData.SecondsLeft(new() { Duration = -1, StartTime = -500 }));
    }

    /// <summary>
    /// The names the plugin's TargetInfo, EnchantmentInfo and ThreatInfo read.
    /// </summary>
    [TestMethod]
    public void TargetJsonHasTheNamesThePluginReads()
    {
        var target = new PlayerUiTarget(
            0x80001234, "ok", "Drudge", 50, 100, 200, 10, 20, 5, 50, 12.5f, 0x50000001, "Bob",
            [new PlayerUiEnchantment(1, "Fester", 7, "LifeMagic", false, 30, 0x50000001, "Bob", true, 0x06001234)],
            [new PlayerUiThreat(0x50000001, "Bob", 400, true, true)]
        );

        var json = PlayerUiProtocol.BuildMessages("targets", new[] { target }).JoinedJson();

        Assert.AreEqual(
            "[{\"guid\":2147488308,\"status\":\"ok\",\"name\":\"Drudge\",\"level\":50,\"health\":100,\"maxHealth\":200,"
                + "\"stamina\":10,\"maxStamina\":20,\"mana\":5,\"maxMana\":50,\"distance\":12.5,\"attackTarget\":1342177281,"
                + "\"attackTargetName\":\"Bob\",\"enchantments\":[{\"spellId\":1,\"name\":\"Fester\",\"level\":7,"
                + "\"school\":\"LifeMagic\",\"beneficial\":false,\"secondsLeft\":30,\"casterGuid\":1342177281,"
                + "\"casterName\":\"Bob\",\"yours\":true,\"iconId\":100667956}],\"threat\":[{\"guid\":1342177281,"
                + "\"name\":\"Bob\",\"threat\":400,\"isYou\":true,\"isPlayer\":true}]}]",
            json
        );
    }

    [TestMethod]
    public void FellowshipJsonHasTheNamesThePluginReads()
    {
        var fellowship = new PlayerUiFellowship(
            "Raiders", 0x50000001, true, true, false, false,
            [
                new PlayerUiFellow(0x50000001, "Bob", 126, 1, 2, 3, 4, 5, 6, null, "Holtburg (42.1N, 33.6E)", 33.5f, 42.25f, true,
                    new PlayerUiFocus("Combat Focus", 0x06001234, 0, 1), [2, 3], [4]),
                new PlayerUiFellow(0x50000002, "Al", 50, 1, 2, 3, 4, 5, 6, 3, "Indoors", null, null, false),
            ]
        );

        var json = PlayerUiProtocol.BuildMessages("fellowship", fellowship).JoinedJson();

        Assert.AreEqual(
            "{\"name\":\"Raiders\",\"leader\":1342177281,\"open\":true,\"shareXp\":true,\"evenShare\":false,\"locked\":false,"
                + "\"members\":[{\"guid\":1342177281,\"name\":\"Bob\",\"level\":126,\"health\":1,\"maxHealth\":2,\"stamina\":3,"
                + "\"maxStamina\":4,\"mana\":5,\"maxMana\":6,\"distance\":null,\"location\":\"Holtburg (42.1N, 33.6E)\","
                + "\"mapX\":33.5,\"mapY\":42.25,\"inBuilding\":true,\"focus\":{\"name\":\"Combat Focus\",\"icon\":100667956,"
                + "\"underlay\":0,\"overlay\":1},\"buffs\":[2,3],\"debuffs\":[4]},"
                + "{\"guid\":1342177282,\"name\":\"Al\",\"level\":50,\"health\":1,\"maxHealth\":2,\"stamina\":3,"
                + "\"maxStamina\":4,\"mana\":5,\"maxMana\":6,\"distance\":3,\"location\":\"Indoors\",\"mapX\":null,\"mapY\":null,\"inBuilding\":false,"
                + "\"focus\":null,\"buffs\":null,\"debuffs\":null}],\"maxMembers\":0}",
            json
        );
    }

    [TestMethod]
    public void BuildingMapCoords_PlacesARoomWhereItsLandblockIs()
    {
        // the middle of Holtburg's landblock (0xA9B4), from a building cell (0x0100 and up)
        var coords = PlayerUiData.BuildingMapCoords(0xA9B40105, 96, 96);

        Assert.AreEqual(33.6f, coords.X, 0.001f);
        Assert.AreEqual(42.4f, coords.Y, 0.001f);
    }

    [TestMethod]
    public void PositionJsonHasTheNamesThePluginReads()
    {
        var json = PlayerUiProtocol.BuildMessages("position", new PlayerUiPosition(33.5f, null, true)).JoinedJson();

        Assert.AreEqual("{\"mapX\":33.5,\"mapY\":null,\"inBuilding\":true}", json);
    }

    [TestMethod]
    public void RollRangesJsonHasTheNamesThePluginReads()
    {
        var json = PlayerUiProtocol.BuildMessages("rollranges", new PlayerUiRollRanges([new("Weapon", "AttackMod", 7, 13.13, 27.5)])).JoinedJson();

        Assert.AreEqual("{\"ranges\":[{\"category\":\"Weapon\",\"stat\":\"AttackMod\",\"tier\":7,\"min\":13.13,\"max\":27.5}]}", json);
    }

    [TestMethod]
    public void RollRangesFollowTheLootFormulas()
    {
        // without weenies the weapons' damage is left out, but the tier-only ranges are all there
        var ranges = ACE.Server.Factories.LootGenerationFactory.BuildLootFilterRanges(_ => null);
        PlayerUiRollRange Range(string category, string stat, int tier) => ranges.Single(r => r.Category == category && r.Stat == stat && r.Tier == tier);

        Assert.IsTrue(ranges.All(r => r.Min <= r.Max && r.Tier is >= 1 and <= 8));

        // tier 8: 10-20% plus 10%, down to 75% when the defense rolled too
        Assert.AreEqual(15.0, Range("Weapon", "AttackMod", 8).Min, 0.01);
        Assert.AreEqual(30.0, Range("Weapon", "AttackMod", 8).Max, 0.01);

        // a tier 7 ring or bracelet's ratings are 3 to 6; a necklace's healing boost 12 to 24
        Assert.AreEqual(3.0, Range("Jewelry", "GearDamage", 7).Min);
        Assert.AreEqual(6.0, Range("Jewelry", "GearDamage", 7).Max);
        Assert.AreEqual(24.0, Range("Jewelry", "GearHealingBoost", 7).Max);

        // tier 1 casters roll no elemental damage, and tier 1 jewelry no ratings
        Assert.IsFalse(ranges.Any(r => r.Stat == "ElementalDamageMod" && r.Tier == 1));
        Assert.IsFalse(ranges.Any(r => r.Stat == "GearDamage" && r.Tier == 1));
    }

    [TestMethod]
    public void BankJsonHasTheNamesThePluginReads()
    {
        var bank = new PlayerUiBank(
            true, true, 0x7A000001, 40, 120, 5000, 250000, 3, true,
            [new PlayerUiBankPack(1, 0x80000010, "Backpack", "weapons, swords t6", "weapons, swords t6", 12, 24)]
        );

        var json = PlayerUiProtocol.BuildMessages("bank", bank).JoinedJson();

        Assert.AreEqual(
            "{\"open\":true,\"loaded\":true,\"guid\":2046820353,\"used\":40,\"capacity\":120,\"coins\":5000,\"notes\":250000,"
                + "\"noteCount\":3,\"autoSort\":true,\"packs\":[{\"number\":1,\"guid\":2147483664,\"name\":\"Backpack\","
                + "\"tags\":\"weapons, swords t6\",\"inscription\":\"weapons, swords t6\",\"used\":12,\"capacity\":24}]}",
            json
        );
    }

    [TestMethod]
    public void MarketJsonHasTheNamesThePluginReads()
    {
        var market = new PlayerUiMarket(
            true, "Market Broker", 4.5f, 0.05, 0.1, 9500,
            [new PlayerUiListing(12, "Ono", 336, 6, 1000, 3600, new PlayerUiIcon("06001234", null, "Weapon", "Fire"))],
            [new PlayerUiSale(11, "Tachi", 337, 10000, 9500, false, 120, null)],
            [new PlayerUiExpired(10, "Yari", 338, 500, 60, null)]
        );

        var json = PlayerUiProtocol.BuildMessages("market", market).JoinedJson();

        Assert.AreEqual(
            "{\"nearBroker\":true,\"brokerName\":\"Market Broker\",\"brokerDistance\":4.5,\"payoutFee\":0.05,\"cancelFee\":0.1,"
                + "\"pendingPayout\":9500,\"listings\":[{\"id\":12,\"name\":\"Ono\",\"weenieClassId\":336,\"tier\":6,\"price\":1000,"
                + "\"expiresIn\":3600,\"icon\":{\"icon\":\"06001234\",\"overlay\":null,\"underlay\":\"Weapon\",\"effect\":\"Fire\"}}],"
                + "\"sales\":[{\"id\":11,\"name\":\"Tachi\",\"weenieClassId\":337,\"price\":10000,\"payout\":9500,"
                + "\"claimed\":false,\"soldAgo\":120,\"icon\":null}],\"expired\":[{\"id\":10,\"name\":\"Yari\",\"weenieClassId\":338,"
                + "\"price\":500,\"expiredAgo\":60,\"icon\":null}]}",
            json
        );
    }

    [TestMethod]
    public void MarketSecondsAreWholeAndNeverNegative()
    {
        Assert.AreEqual(90, PlayerUiMarketData.Seconds(System.TimeSpan.FromSeconds(90.7)));
        Assert.AreEqual(0, PlayerUiMarketData.Seconds(System.TimeSpan.FromSeconds(-5)));
    }

    [TestMethod]
    public void ArenaJsonHasTheNamesThePluginReads()
    {
        var arena = new PlayerUiArena(
            true, 20, 1500, "queued", "You are number 1 of 2 in the arena queue.", 1, 2,
            [new PlayerUiArenaBoard("1v1", 1, false, 1520, 3, 1, 0)],
            [new PlayerUiArenaDuel(7, "Alice vs Bob", 1, false, true, "Pit", "Fighting")],
            [new PlayerUiArenaResult(6, "Al vs Cy", 1, true, false, "Pit", "Al", false, 90)],
            ["The Pit"]
        );

        var json = PlayerUiProtocol.BuildMessages("arena", arena).JoinedJson();

        Assert.AreEqual(
            "{\"enabled\":true,\"minimumLevel\":20,\"startingRating\":1500,\"state\":\"queued\","
                + "\"status\":\"You are number 1 of 2 in the arena queue.\",\"queuePlace\":1,\"queueCount\":2,"
                + "\"boards\":[{\"name\":\"1v1\",\"size\":1,\"scaled\":false,\"rating\":1520,\"wins\":3,\"losses\":1,\"draws\":0}],"
                + "\"duels\":[{\"id\":7,\"sides\":\"Alice vs Bob\",\"size\":1,\"scaled\":false,\"rated\":true,\"map\":\"Pit\",\"state\":\"Fighting\"}],"
                + "\"recent\":[{\"id\":6,\"sides\":\"Al vs Cy\",\"size\":1,\"scaled\":true,\"rated\":false,\"map\":\"Pit\",\"winners\":\"Al\","
                + "\"calledOff\":false,\"endedAgo\":90}],\"maps\":[\"The Pit\"]}",
            json
        );
    }

    [TestMethod]
    public void LfgJsonHasTheNamesThePluginReads()
    {
        var lfg = new PlayerUiLfg(
            true, "seeking", "Green Mire Grave d10", 65, false, "Arwen", null, "You need a fellowship.", false,
            [new PlayerUiLfgListing("Arwen", "Green Mire Grave d10", 3, 9, 30, 35, false, 120, true, false)],
            [new PlayerUiLfgSeeker("Brin", 31, "any capstone up to level 30", 720, false, true)],
            [new PlayerUiLfgCapstone("Green Mire Grave", "gmg", 10)]
        );

        var json = PlayerUiProtocol.BuildMessages("lfg", lfg).JoinedJson();

        Assert.AreEqual(
            "{\"enabled\":true,\"mine\":\"seeking\",\"activity\":\"Green Mire Grave d10\",\"since\":65,\"autoJoin\":false,"
                + "\"askedBy\":\"Arwen\",\"cantSeek\":null,\"cantList\":\"You need a fellowship.\",\"leader\":false,"
                + "\"listings\":[{\"leader\":\"Arwen\",\"activity\":\"Green Mire Grave d10\",\"size\":3,\"maxSize\":9,\"minLevel\":30,"
                + "\"maxLevel\":35,\"autoJoin\":false,\"waited\":120,\"fits\":true,\"yours\":false}],"
                + "\"seekers\":[{\"name\":\"Brin\",\"level\":31,\"activity\":\"any capstone up to level 30\",\"waited\":720,"
                + "\"fits\":false,\"you\":true}],"
                + "\"capstones\":[{\"name\":\"Green Mire Grave\",\"short\":\"gmg\",\"level\":10}]}",
            json
        );
    }

    [TestMethod]
    public void SocialJsonHasTheNamesThePluginReads()
    {
        var social = new PlayerUiSocial(
            true, false, true, true, false,
            [new PlayerUiFriend(0x50000002, "Al", true)],
            [new PlayerUiSquelch(0x50000003, "Cy", true)],
            new PlayerUiAllegiance(
                "Order", 3, 7, 1000, 2000,
                new PlayerUiAllegianceMember(0x50000004, "Mo", 200, 6, false, 0),
                null,
                [new PlayerUiAllegianceMember(0x50000005, "Vi", 40, 1, true, 50)]
            )
        );

        var json = PlayerUiProtocol.BuildMessages("social", social).JoinedJson();

        Assert.AreEqual(
            "{\"appearOffline\":true,\"ignoreFellowshipRequests\":false,\"autoAcceptFellowshipRequests\":true,"
                + "\"shareFellowshipXp\":true,\"shareFellowshipLoot\":false,"
                + "\"friends\":[{\"guid\":1342177282,\"name\":\"Al\",\"online\":true}],"
                + "\"squelches\":[{\"guid\":1342177283,\"name\":\"Cy\",\"account\":true}],"
                + "\"allegiance\":{\"name\":\"Order\",\"rank\":3,\"followers\":7,\"xpGenerated\":1000,\"xpReceived\":2000,"
                + "\"monarch\":{\"guid\":1342177284,\"name\":\"Mo\",\"level\":200,\"rank\":6,\"online\":false,\"xpGenerated\":0},"
                + "\"patron\":null,"
                + "\"vassals\":[{\"guid\":1342177285,\"name\":\"Vi\",\"level\":40,\"rank\":1,\"online\":true,\"xpGenerated\":50}]}}",
            json
        );
    }

    [TestMethod]
    public void CorpseItemRollQualityKeepsAppraisalsStatNames()
    {
        var listing = new ACE.MarketApi.MarketListing { WeaponTime = 40, AttacksPerSecond = 0.8 };
        var item = new PlayerUiCorpseItem(
            5, "Axe", 1, 100, 50, true, false, "Iron", 6.5, listing,
            new() { ["Bonus to Attack Skill"] = 0.75 }
        );

        var json = PlayerUiProtocol.BuildMessages("describe", new[] { item }, omitNulls: true).JoinedJson();

        // the plugin finds each quality by the stat's name, so the names must not be camelCased like the fields
        StringAssert.Contains(json, "\"rollQuality\":{\"Bonus to Attack Skill\":0.75}");
        StringAssert.Contains(json, "\"weaponTime\":40");
        StringAssert.Contains(json, "\"attacksPerSecond\":0.8");

        // false isn't a null, so it's sent: the plugin reads a missing one as an older server's
        StringAssert.Contains(json, "\"sellable\":false");
    }

    [TestMethod]
    public void CharacterSendsEachStatsSheetSection()
    {
        var character = new PlayerUiCharacter(
            [
                new ACE.Server.WorldObjects.StatsSheetSection("Ward", ["Ward Level: 3"]),
                new ACE.Server.WorldObjects.StatsSheetSection("Jewel Effects", []),
            ]
        );

        var json = PlayerUiProtocol.BuildMessages("character", character).JoinedJson();

        // the plugin's CharacterInfo reads these names
        Assert.AreEqual(
            "{\"sections\":[{\"title\":\"Ward\",\"lines\":[\"Ward Level: 3\"]},{\"title\":\"Jewel Effects\",\"lines\":[]}]}",
            json
        );
    }

    [TestMethod]
    public void QuestsSendEachContractWithoutItsEmptyFields()
    {
        var quests = new PlayerUiQuests(
            [
                new PlayerUiQuest(
                    101, "Kill Tuskers", "Slay 10 tuskers.", "Tuskers slain", "started", 3, 10, null, null,
                    "Guard Tobin", null, new PlayerUiPlace(42.1f, -33.5f), null, null, true
                ),
            ]
        );

        var json = PlayerUiProtocol.BuildMessages("quests", quests, omitNulls: true).JoinedJson();

        // the plugin's QuestInfo reads these names
        Assert.AreEqual(
            "{\"quests\":[{\"id\":101,\"name\":\"Kill Tuskers\",\"description\":\"Slay 10 tuskers.\",\"progress\":\"Tuskers slain\","
                + "\"stage\":\"started\",\"solves\":3,\"maxSolves\":10,\"startNpc\":\"Guard Tobin\","
                + "\"startAt\":{\"mapX\":42.1,\"mapY\":-33.5},\"canAbandon\":true}]}",
            json
        );
    }

    [TestMethod]
    public void AContractsStageIsOneWord()
    {
        Assert.AreEqual("available", PlayerUiQuestData.StageWord(ACE.Server.Network.Structure.ContractStage.Available));
        Assert.AreEqual("started", PlayerUiQuestData.StageWord(ACE.Server.Network.Structure.ContractStage.InProgress));
        // a progress counter is the stage plus the count
        Assert.AreEqual("started", PlayerUiQuestData.StageWord(ACE.Server.Network.Structure.ContractStage.ProgressCounter + 3));
        Assert.AreEqual("done", PlayerUiQuestData.StageWord(ACE.Server.Network.Structure.ContractStage.DoneOrPendingRepeat));
    }

    [TestMethod]
    public void NoFellowshipIsNull()
    {
        Assert.AreEqual("DAP2|fellowship|1|1|1|bnVsbA==", PlayerUiProtocol.BuildMessages("fellowship", "null", 1)[0]);
        Assert.AreEqual("null", PlayerUiProtocol.BuildMessages("fellowship", (object)null).JoinedJson());
    }
}

internal static class PluginReplyMessages
{
    /// <summary>
    /// A reply's JSON, its chunks joined and decoded again the way the plugins' ReplyAssembler does. For @adminui and @playerui.
    /// </summary>
    public static string JoinedJson(this System.Collections.Generic.List<string> messages)
    {
        return System.Text.Encoding.UTF8.GetString(
            System.Convert.FromBase64String(string.Concat(messages.Select(m => m.Split('|', 6)[5])))
        );
    }
}
