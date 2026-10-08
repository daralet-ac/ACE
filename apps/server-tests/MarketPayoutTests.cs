using System.Collections.Generic;
using System.Linq;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Entity.Models;
using ACE.Server.Managers;
using ACE.Server.Market;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ACE.Server.Tests;

[TestClass]
public class MarketPayoutTests
{
    #region Payouts

    [DataTestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(99)]
    [DataRow(100)]
    [DataRow(101)]
    [DataRow(12_345)]
    [DataRow(166_600)]
    [DataRow(987_654_321)]
    [DataRow(int.MaxValue)]
    public void SplitPayout_PaysExactlyTheTotal(int total)
    {
        var (notes, pyreals) = MarketBroker.SplitPayout(total);

        Assert.AreEqual((long)total, NoteValue(notes) + pyreals);
        Assert.IsTrue(pyreals is >= 0 and < 100, $"{pyreals} pyreals left over");
    }

    [TestMethod]
    public void SplitPayout_UsesTheFewestNotes()
    {
        var (notes, pyreals) = MarketBroker.SplitPayout(166_642);

        // 100k + 50k + 10k + 5k + 1k + 500 + 100: one of each note, and the 42 left over in pyreals
        Assert.AreEqual(MarketBroker.TradeNoteDenominations.Length, notes.Count);
        Assert.IsTrue(notes.All(n => n.amount == 1));
        Assert.AreEqual(42, pyreals);

        (notes, _) = MarketBroker.SplitPayout(300_000);

        Assert.AreEqual(1, notes.Count);
        Assert.AreEqual(3, notes[0].amount);
    }

    [TestMethod]
    public void TradeNoteBreakdown_NothingForNothing()
    {
        Assert.AreEqual(0, MarketBroker.GetTradeNoteBreakdown(0).Count);
        Assert.AreEqual(0, MarketBroker.GetTradeNoteBreakdown(-500).Count);
        Assert.AreEqual(0, MarketBroker.GetTradeNoteBreakdown(99).Count);
    }

    [TestMethod]
    public void TradeNoteDenominations_AreLargestFirst()
    {
        var values = MarketBroker.TradeNoteDenominations.Select(d => d.value).ToList();

        CollectionAssert.AreEqual(values.OrderByDescending(v => v).ToList(), values);
        Assert.AreEqual(100, values.Last(), "SplitPayout pays anything under 100 in pyreals");
    }

    private static long NoteValue(List<(uint wcid, int amount)> notes)
    {
        var values = MarketBroker.TradeNoteDenominations.ToDictionary(d => d.wcid, d => d.value);

        return notes.Sum(n => (long)values[n.wcid] * n.amount);
    }

    #endregion

    #region Typed input

    [DataTestMethod]
    [DataRow("500", 500)]
    [DataRow("  500  ", 500)]
    [DataRow("price 1000", 1000)]
    [DataRow("PRICE   1000 ", 1000)]
    [DataRow("2147483647", int.MaxValue)]
    public void Price_IsReadFromWhatWasTyped(string input, int expected)
    {
        Assert.IsTrue(MarketBroker.TryParsePriceInput(input, out var price));
        Assert.AreEqual(expected, price);
    }

    [DataTestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow("0")]
    [DataRow("-5")]
    [DataRow("price")]
    [DataRow("price 0")]
    [DataRow("price -1")]
    [DataRow("2147483648")]
    [DataRow("lots")]
    [DataRow("cancel 5")]
    public void Price_RefusesAnythingElse(string input)
    {
        Assert.IsFalse(MarketBroker.TryParsePriceInput(input, out _));
    }

    [DataTestMethod]
    [DataRow("cancel 12", 12)]
    [DataRow("cancel #12", 12)]
    [DataRow("CANCEL # 7", 7)]
    [DataRow("  cancel   3  ", 3)]
    public void Cancel_ReadsTheListingNumber(string input, int expected)
    {
        Assert.IsTrue(MarketBroker.TryParseCancelInput(input, out var listingId));
        Assert.AreEqual(expected, listingId);
    }

    [DataTestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("cancel")]
    [DataRow("cancel #")]
    [DataRow("cancel 0")]
    [DataRow("cancel -3")]
    [DataRow("cancel all")]
    [DataRow("cancelled 5")]
    [DataRow("12")]
    public void Cancel_RefusesAnythingElse(string input)
    {
        Assert.IsFalse(MarketBroker.TryParseCancelInput(input, out _));
    }

    #endregion

    #region Wealth

    [TestMethod]
    public void Wealth_CoinsCountTheirCoinValue()
    {
        var coins = CreateBiota(WeenieType.Coin, new() { [PropertyInt.CoinValue] = 2500, [PropertyInt.Value] = 1 });

        Assert.AreEqual((2500L, 0L), PlayerWealthCalculator.GetBiotaWealth(coins));
    }

    [TestMethod]
    public void Wealth_CoinsWithoutACoinValueCountTheirValue()
    {
        var coins = CreateBiota(WeenieType.Coin, new() { [PropertyInt.Value] = 75 });

        Assert.AreEqual((75L, 0L), PlayerWealthCalculator.GetBiotaWealth(coins));
    }

    [TestMethod]
    public void Wealth_TradeNotesCountTheirValue()
    {
        var notes = CreateBiota(
            WeenieType.Stackable,
            new() { [PropertyInt.ItemType] = (int)ItemType.PromissoryNote, [PropertyInt.Value] = 200_000 }
        );

        Assert.AreEqual((200_000L, 0L), PlayerWealthCalculator.GetBiotaWealth(notes));
    }

    [TestMethod]
    public void Wealth_TrophiesCountByTheSquareOfTheirQuality()
    {
        var trophy = CreateBiota(
            WeenieType.Generic,
            new() { [PropertyInt.TrophyQuality] = 3, [PropertyInt.Value] = 5000 }
        );

        Assert.AreEqual((0L, 900L), PlayerWealthCalculator.GetBiotaWealth(trophy));
    }

    [TestMethod]
    public void Wealth_OtherItemsAreNotMoney()
    {
        var sword = CreateBiota(
            WeenieType.MeleeWeapon,
            new() { [PropertyInt.ItemType] = (int)ItemType.MeleeWeapon, [PropertyInt.Value] = 50_000 }
        );

        Assert.AreEqual((0L, 0L), PlayerWealthCalculator.GetBiotaWealth(sword));
        Assert.AreEqual((0L, 0L), PlayerWealthCalculator.GetBiotaWealth(CreateBiota(WeenieType.Generic, null)));
        Assert.AreEqual((0L, 0L), PlayerWealthCalculator.GetBiotaWealth(null));
    }

    private static Biota CreateBiota(WeenieType weenieType, Dictionary<PropertyInt, int> ints)
    {
        return new Biota { WeenieType = weenieType, PropertiesInt = ints };
    }

    #endregion
}
