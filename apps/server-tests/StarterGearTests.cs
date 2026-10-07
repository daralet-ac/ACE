using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using ACE.Common;
using ACE.Server.Entity;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ACE.Server.Tests;

[TestClass]
public class StarterGearTests
{
    [TestMethod]
    public void CanParseStarterGearJson()
    {
        // apps/server/starterGear.json is copied next to the tests. Read it the way StarterGearFactory does.
        var contents = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "starterGear.json"));

        var config = JsonSerializer.Deserialize<StarterGearConfiguration>(contents, ConfigManager.SerializerOptions);

        Assert.IsNotNull(config);
        Assert.IsTrue(config.Skills.Count > 0, "no skills");
        Assert.IsTrue(config.Skills.SelectMany(s => s.Gear).Any(), "no gear");

        foreach (var skill in config.Skills)
        {
            foreach (var item in skill.Gear)
            {
                Assert.AreNotEqual(
                    0u,
                    item.WeenieId,
                    $"skill {skill.SkillId} ({skill.Name}) has gear with no weenieId"
                );
                Assert.AreNotEqual(
                    (ushort)0,
                    item.StackSize,
                    $"skill {skill.SkillId} ({skill.Name}) has an empty stack"
                );
            }
        }
    }
}
