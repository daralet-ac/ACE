using System.Collections.Generic;
using System.Linq;
using ACE.Entity.Enum;
using ACE.Server.WorldObjects;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ACE.Server.Tests;

[TestClass]
public class RecipeBookTests
{
    private static readonly Dictionary<uint, string> Names = new()
    {
        [5795] = "Pepper",
        [1053940] = "Monster Steaks (Bundle)",
        [1053942] = "Pepper Steak",
        [1052552] = "Tanning Kit",
        [1052500] = "Tattered Beast Hide",
        [1052553] = "Rough Leather",
        [2037004] = "Frigid Shard",
        [35] = "Chainmail Basinet",
        [36] = "Leather Bracers",
        [37] = "Scalemail Bracers",
        [38] = "Studded Leather Bracers",
        [25637] = "Leather Bracers",
        [1654] = "Verdalim Plant",
        [1655] = "Crushed Verdalim Plant",
        [4751] = "Mortar and Pestle",
    };

    private static string Name(uint wcid)
    {
        return Names.TryGetValue(wcid, out var name) ? name : $"Item {wcid}";
    }

    private static RecipeBookService.RecipeInfo Recipe(
        uint id,
        uint skill,
        uint difficulty,
        uint result,
        params (uint Source, uint Target)[] rows
    )
    {
        return new RecipeBookService.RecipeInfo(
            id,
            skill,
            difficulty,
            result,
            1,
            "",
            rows.Select(r => new RecipeBookService.RecipeRow(r.Source, r.Target)).ToList()
        );
    }

    [TestMethod]
    public void ParseRecipeIds_ReadsIdsAndRanges()
    {
        var ids = RecipeBookService.ParseRecipeIds("1654-1656, 10012;10028 junk 5-3");

        CollectionAssert.AreEquivalent(new uint[] { 1654, 1655, 1656, 10012, 10028 }, ids.ToList());
        Assert.AreEqual(0, RecipeBookService.ParseRecipeIds(null).Count);
    }

    [TestMethod]
    public void IsListed_KeepsDaraletRecipesForTheBooksSkill()
    {
        var none = new HashSet<uint>();

        var steak = Recipe(1053942, (uint)Skill.Cooking, 30, 1053942, (5795, 1053940));
        var retailCooking = Recipe(500, (uint)Skill.Cooking, 50, 600, (700, 800));
        var otherSkill = Recipe(1053942, (uint)Skill.Alchemy, 30, 1053942, (5795, 1053940));

        Assert.IsTrue(RecipeBookService.IsListed(steak, Skill.Cooking, none, none));
        Assert.IsFalse(RecipeBookService.IsListed(retailCooking, Skill.Cooking, none, none));
        Assert.IsFalse(RecipeBookService.IsListed(otherSkill, Skill.Cooking, none, none));

        // a Daralet target alone is enough (trophy essence onto a draught, no result item)
        var noResult = Recipe(10404, (uint)Skill.Alchemy, 0, 0, (700, 1053957));
        Assert.IsTrue(RecipeBookService.IsListed(noResult, Skill.Alchemy, none, none));
    }

    [TestMethod]
    public void IsListed_IncludeAndExcludeListsWin()
    {
        var none = new HashSet<uint>();

        // a retail dye recipe, and a hide recipe with no skill
        var dye = Recipe(1654, (uint)Skill.Alchemy, 50, 1655, (4751, 1654));
        var hide = Recipe(10012, 0, 0, 1052553, (1052552, 1052500));

        Assert.IsTrue(RecipeBookService.IsListed(dye, Skill.Alchemy, new HashSet<uint> { 1654 }, none));
        Assert.IsTrue(RecipeBookService.IsListed(hide, Skill.Leatherworking, new HashSet<uint> { 10012 }, none));
        Assert.IsFalse(RecipeBookService.IsListed(hide, Skill.Leatherworking, none, none));

        var steak = Recipe(1053942, (uint)Skill.Cooking, 30, 1053942, (5795, 1053940));
        Assert.IsFalse(RecipeBookService.IsListed(steak, Skill.Cooking, none, new HashSet<uint> { 1053942 }));
    }

    [TestMethod]
    public void FormatNameList_ShortensLongListsAndMergesSameNames()
    {
        Assert.AreEqual("Pepper", RecipeBookService.FormatNameList(new uint[] { 5795, 5795 }, Name));
        Assert.AreEqual("Pepper or Tanning Kit", RecipeBookService.FormatNameList(new uint[] { 5795, 1052552 }, Name));

        // 36 and 25637 are both "Leather Bracers"
        Assert.AreEqual(
            "Chainmail Basinet, Leather Bracers or Scalemail Bracers",
            RecipeBookService.FormatNameList(new uint[] { 35, 36, 25637, 37 }, Name)
        );

        Assert.AreEqual(
            "any of 4 items (e.g. Chainmail Basinet, Leather Bracers, Scalemail Bracers)",
            RecipeBookService.FormatNameList(new uint[] { 35, 36, 37, 38 }, Name)
        );
    }

    [TestMethod]
    public void FormatEntry_ShowsResultSkillAndMaterials()
    {
        var steak = Recipe(1053942, (uint)Skill.Cooking, 30, 1053942, (5795, 1053940));
        Assert.AreEqual(
            "Pepper Steak - Skill 30\nPepper on Monster Steaks (Bundle)",
            RecipeBookService.FormatEntry(steak, "Pepper Steak", Skill.Cooking, Name)
        );

        var hide = Recipe(10012, 0, 0, 1052553, (1052552, 1052500));
        Assert.AreEqual(
            "Rough Leather - no skill check\nTanning Kit on Tattered Beast Hide",
            RecipeBookService.FormatEntry(hide, "Rough Leather", Skill.Leatherworking, Name)
        );

        // a recipe that uses up several of the target says how many
        var patch = Recipe(10016, (uint)Skill.Leatherworking, 1, 1052540, (1052553, 1052552)) with { SourceAmount = 5 };
        Assert.AreEqual(
            "Poor Armor Reinforcement - Skill 1\nRough Leather x5 on Tanning Kit",
            RecipeBookService.FormatEntry(patch, "Poor Armor Reinforcement", Skill.Leatherworking, Name)
        );

        var tanning = Recipe(10012, (uint)Skill.Leatherworking, 1, 1052553, (1052552, 1052500)) with { TargetAmount = 3 };
        StringAssert.EndsWith(
            RecipeBookService.FormatEntry(tanning, "Rough Leather", Skill.Leatherworking, Name),
            "\nTanning Kit on Tattered Beast Hide x3"
        );

        // a recipe from another skill names that skill
        var weaving = Recipe(10020, 26, 50, 1052553, (1052552, 1052500));
        StringAssert.StartsWith(
            RecipeBookService.FormatEntry(weaving, "X", Skill.Leatherworking, Name),
            "X - Weaving 50\n"
        );
    }

    [TestMethod]
    public void FormatEntry_ExplainsRecipesWithNoResultItem()
    {
        var lining = new RecipeBookService.RecipeInfo(
            10011,
            (uint)Skill.Leatherworking,
            100,
            0,
            0,
            "You apply the Insulated Armor Lining to the armor.",
            new List<RecipeBookService.RecipeRow> { new(2037004, 35), new(2037004, 36) }
        );

        var entries = RecipeBookService.BuildEntries(
            new List<RecipeBookService.RecipeInfo> { lining },
            Skill.Leatherworking,
            new HashSet<uint>(),
            new HashSet<uint>(),
            Name
        );

        Assert.AreEqual(1, entries.Count);
        Assert.AreEqual(
            "Frigid Shard - Skill 100\nFrigid Shard on Chainmail Basinet or Leather Bracers\nYou apply the Insulated Armor Lining to the armor.",
            entries[0]
        );
    }

    [TestMethod]
    public void BuildEntries_SortsEasiestFirstWithNoSkillCheckAtTheTop()
    {
        var recipes = new List<RecipeBookService.RecipeInfo>
        {
            Recipe(3, (uint)Skill.Cooking, 100, 1053942, (5795, 1053940)),
            Recipe(1, (uint)Skill.Cooking, 30, 1053942, (5795, 1053940)),
            Recipe(2, 0, 0, 1052553, (1052552, 1052500)),
        };

        var entries = RecipeBookService.BuildEntries(
            recipes,
            Skill.Cooking,
            new HashSet<uint> { 2 },
            new HashSet<uint>(),
            Name
        );

        Assert.AreEqual(3, entries.Count);
        StringAssert.Contains(entries[0], "no skill check");
        StringAssert.Contains(entries[1], "Skill 30");
        StringAssert.Contains(entries[2], "Skill 100");
    }

    [TestMethod]
    public void Paginate_KeepsEntriesWholeAndPagesWithinTheLimit()
    {
        var blocks = Enumerable.Range(0, 40).Select(i => $"Recipe {i:D2}\n" + new string('x', 60)).ToList();

        var pages = RecipeBookService.Paginate(blocks, 300);

        Assert.IsTrue(pages.Count > 1);
        Assert.IsTrue(pages.All(p => p.Length <= 300));

        // every entry is on exactly one page, in order, in one piece
        var joined = string.Join("\n\n", pages);
        Assert.AreEqual(string.Join("\n\n", blocks), joined);
        Assert.IsTrue(pages.All(p => p.StartsWith("Recipe ")));
    }

    [TestMethod]
    public void Paginate_SplitsABlockTooLongForOnePage()
    {
        var block = string.Join("\n", Enumerable.Range(0, 30).Select(i => $"line {i:D2} " + new string('y', 20)));

        var pages = RecipeBookService.Paginate(new List<string> { "intro", block, new string('z', 250) }, 100);

        Assert.IsTrue(pages.All(p => p.Length <= 100));
        Assert.IsTrue(pages.Any(p => p.Contains("line 29")));
        Assert.AreEqual(250, pages.Sum(p => p.Count(c => c == 'z')));
    }

    [TestMethod]
    public void BuildPageTexts_IntroThenRecipesThenTinkeringOnItsOwnPage()
    {
        var texts = RecipeBookService.BuildPageTexts(
            Skill.Cooking,
            new List<string> { "Pepper Steak - Skill 30\nPepper on Monster Steaks (Bundle)" },
            new List<string> { "Tinkering\n\nApply salvage." },
            hasStoredPages: false,
            maxChars: 1000
        );

        Assert.AreEqual(2, texts.Count);
        StringAssert.StartsWith(texts[0], "Cooking Recipes");
        StringAssert.Contains(texts[0], "Pepper Steak - Skill 30");
        StringAssert.StartsWith(texts[1], "Tinkering");

        // the Spellcrafter's Tome already has its own pages: no introduction, and no empty recipe page
        var tome = RecipeBookService.BuildPageTexts(
            Skill.Spellcrafting,
            new List<string>(),
            new List<string> { "Tinkering\n\nApply salvage." },
            hasStoredPages: true,
            maxChars: 1000
        );

        Assert.AreEqual(1, tome.Count);
        StringAssert.StartsWith(tome[0], "Tinkering");
    }

    [TestMethod]
    public void SkillNames_UseDaraletNames()
    {
        Assert.AreEqual("Weaving", RecipeBookService.GetSkillName(Skill.Weaving));
        Assert.AreEqual("Cooking", RecipeBookService.GetSkillName(Skill.Cooking));
    }

    [TestMethod]
    public void TinkeringGuide_ListsEachSkillsSalvage()
    {
        var blacksmithing = string.Join("\n", TinkeringGuide.GetBlocks(Skill.Blacksmithing));
        StringAssert.Contains(blacksmithing, "Iron\n");
        StringAssert.Contains(blacksmithing, "Steel\n");
        StringAssert.Contains(blacksmithing, "Aquamarine: Cold Rending (weapons only)");
        Assert.IsFalse(blacksmithing.Contains("Moonstone"));

        var spellcrafting = string.Join("\n", TinkeringGuide.GetBlocks(Skill.Spellcrafting));
        StringAssert.Contains(spellcrafting, "Green Garnet\n");
        StringAssert.Contains(spellcrafting, "wands");

        // only the Jewelcrafting gems that do something are listed
        var jewelcrafting = string.Join("\n", TinkeringGuide.GetBlocks(Skill.Jewelcrafting));
        StringAssert.Contains(jewelcrafting, "Moonstone\n");
        StringAssert.Contains(jewelcrafting, "White Jade\n");
        Assert.IsFalse(jewelcrafting.Contains("Agate"));
        Assert.IsFalse(jewelcrafting.Contains("Cold Rending"));

        // hides on light armor, fabrics on cloth armor; armor imbues only, no weapon-only gems
        var leatherworking = string.Join("\n", TinkeringGuide.GetBlocks(Skill.Leatherworking));
        StringAssert.Contains(leatherworking, "Gromnie Hide\n");
        StringAssert.Contains(leatherworking, "imbues light armor");
        StringAssert.Contains(leatherworking, "Sunstone: Armor Tempering");
        Assert.IsFalse(leatherworking.Contains("Wool"));
        Assert.IsFalse(leatherworking.Contains("Aquamarine"));

        var weaving = string.Join("\n", TinkeringGuide.GetBlocks(Skill.Weaving));
        StringAssert.Contains(weaving, "Wool\n");
        StringAssert.Contains(weaving, "imbues cloth armor");
        StringAssert.Contains(weaving, "Sunstone: Armor Tempering");
        Assert.IsFalse(weaving.Contains("Gromnie"));

        Assert.AreEqual(0, TinkeringGuide.GetBlocks(Skill.Cooking).Count);
        Assert.AreEqual(0, TinkeringGuide.GetBlocks(Skill.Alchemy).Count);
    }

    [TestMethod]
    public void TinkeringGuide_DescribesEverySalvageThatHasAnEffect()
    {
        // every material HandleTinkering does something with (all of TinkeringTarget except the inert jewel gems) has
        // a description in some book, so a new material can't be silently missing from the books
        var allText = string.Join(
            "\n",
            new[] { Skill.Blacksmithing, Skill.Leatherworking, Skill.Woodworking, Skill.Spellcrafting, Skill.Jewelcrafting, Skill.Weaving }
                .SelectMany(TinkeringGuide.GetBlocks)
        );

        var inertJewels = new[]
        {
            MaterialType.Agate, MaterialType.Amber, MaterialType.Azurite, MaterialType.Bloodstone, MaterialType.Carnelian,
            MaterialType.Citrine, MaterialType.Diamond, MaterialType.GreenJade, MaterialType.Hematite,
            MaterialType.LapisLazuli, MaterialType.Onyx, MaterialType.Peridot, MaterialType.RedJade, MaterialType.Ruby,
            MaterialType.Sapphire, MaterialType.Serpentine, MaterialType.SmokeyQuartz, MaterialType.TigerEye,
            MaterialType.Turquoise, MaterialType.WhiteQuartz, MaterialType.YellowGarnet, MaterialType.YellowTopaz,
            MaterialType.Zircon, MaterialType.Wood,
        };

        foreach (var material in Salvage.TinkeringTarget.Keys.Where(k => k.HasValue).Select(k => k.Value))
        {
            if (inertJewels.Contains(material))
            {
                continue;
            }

            Assert.IsTrue(
                allText.Replace(" ", "").Contains(material.ToString(), System.StringComparison.OrdinalIgnoreCase),
                $"{material} has no tinkering description"
            );
        }
    }
}
