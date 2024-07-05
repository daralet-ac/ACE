using System.IO;
using ACE.Server.Entity;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace ACE.Server.Tests;

[TestClass]
public class StarterGearTests
{
    [TestMethod]
    public void CanParseStarterGearJson()
    {
        var contents = File.ReadAllText("../../../../../ACE.Server/starterGear.json");

        var config = JsonConvert.DeserializeObject<StarterGearConfiguration>(contents);
    }
}
