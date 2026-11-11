using ACE.Entity.Enum;
using ACE.Server.Entity;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ACE.Server.Tests;

[TestClass]
public class SpellDamageLogTests
{
    [TestMethod]
    public void LogLine_IsKeyValuePairs_InTheInvariantCulture()
    {
        var line = new SpellDamageLogLine()
            .Add("targetWcid", 12345u)
            .Add("wardMod", 0.07861f)
            .Add("archetypeMod", 19.123456)
            .Add("crit", false)
            .Add("resist", PartialEvasion.Some);

        Assert.AreEqual("targetWcid=12345 wardMod=0.0786 archetypeMod=19.1235 crit=false resist=Some", line.ToString());
    }

    [TestMethod]
    public void LogLine_QuotesTextWithSpaces()
    {
        var line = new SpellDamageLogLine()
            .Add("target", "Olthoi Soldier")
            .Add("spell", "Flame Bolt VII")
            .Add("weapon", null)
            .Add("caster", "Gromnie");

        Assert.AreEqual("target=\"Olthoi Soldier\" spell=\"Flame Bolt VII\" weapon=null caster=Gromnie", line.ToString());
    }

    [TestMethod]
    public void CoversPlayers_EmptyListCoversEveryHitWithAPlayer()
    {
        Assert.IsTrue(SpellDamageLog.CoversPlayers("", "Alice", null));
        Assert.IsTrue(SpellDamageLog.CoversPlayers(" ", null, "Bob"));
        Assert.IsFalse(SpellDamageLog.CoversPlayers("", null, null));
    }

    [TestMethod]
    public void CoversPlayers_ListedCasterOrTarget_AnyCase()
    {
        Assert.IsTrue(SpellDamageLog.CoversPlayers("alice, Bob", "Alice", null));
        Assert.IsTrue(SpellDamageLog.CoversPlayers("alice, Bob", null, "bob"));
        Assert.IsTrue(SpellDamageLog.CoversPlayers("Sir Carl", "Dana", "sir carl"));
        Assert.IsFalse(SpellDamageLog.CoversPlayers("alice, Bob", "Carol", null));
        Assert.IsFalse(SpellDamageLog.CoversPlayers("alice,,", null, null));
    }
}
