using ACE.Common.Performance;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ACE.Server.Tests.Libraries;

[TestClass]
public class TimedEventHistoryTests
{
    [TestMethod]
    public void NoEventsAverageZero()
    {
        // #1284: this divided by zero
        var history = new TimedEventHistory();

        Assert.AreEqual(0, history.AverageEventDuration);
        Assert.AreEqual(0, history.ShortestEvent);
    }

    [TestMethod]
    public void TheFirstEventIsTheShortestSoFar()
    {
        // #1284: the shortest stayed 0, which nothing is shorter than
        var history = new TimedEventHistory();

        history.RegisterEvent(5);

        Assert.AreEqual(5, history.ShortestEvent);
        Assert.AreEqual(5, history.LongestEvent);
    }

    [TestMethod]
    public void KeepsTheShortestLongestLastAndAverage()
    {
        var history = new TimedEventHistory();

        history.RegisterEvent(5);
        history.RegisterEvent(2);
        history.RegisterEvent(9);

        Assert.AreEqual(3, history.TotalEvents);
        Assert.AreEqual(2, history.ShortestEvent);
        Assert.AreEqual(9, history.LongestEvent);
        Assert.AreEqual(9, history.LastEvent);
        Assert.AreEqual(16.0 / 3, history.AverageEventDuration, 1e-9);
    }

    [TestMethod]
    public void ClearingStartsOver()
    {
        var history = new TimedEventHistory();
        history.RegisterEvent(1);
        history.RegisterEvent(10);

        history.ClearHistory();
        history.RegisterEvent(7);

        Assert.AreEqual(1, history.TotalEvents);
        Assert.AreEqual(7, history.ShortestEvent);
        Assert.AreEqual(7, history.LongestEvent);
        Assert.AreEqual(7, history.AverageEventDuration);
    }
}
