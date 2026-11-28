using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ACE.Database;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Entity.Models;

namespace ACE.Server.WorldObjects;

/// <summary>
/// Recipe Books: a Book weenie with PropertyInt.RecipeBookSkill set lists every recipe for that skill, generated from the
/// recipe and cook_book tables each time it's opened, so it's never out of date and nothing is stored on the item.
///
/// A recipe is listed when it uses the book's skill and involves a Daralet item (its source, target or result is a
/// custom WCID, 1,000,000 or above), which leaves out the retail recipes for items Daralet doesn't use.
/// PropertyString.RecipeBookIncludeRecipes adds recipes regardless (retail dyes, or recipes with no skill such as hide
/// processing) and RecipeBookExcludeRecipes removes them. After the recipes come the book's tinkering pages (see
/// TinkeringGuide).
///
/// A book's stored pages (the Spellcrafter's Tome keeps its discovered formulas there) are shown first and the
/// generated pages follow, so the Tome carries RecipeBookSkill too and gains the Spellcrafting tinkering pages.
/// Players can't add, edit or delete pages in a recipe book (see Player_Book).
/// </summary>
public static class RecipeBookService
{
    public const int DefaultCharsPerPage = 1000;

    /// <summary>
    /// Daralet's own weenies start here; anything below is retail.
    /// </summary>
    public const uint DaraletWcidStart = 1_000_000;

    /// <summary>
    /// A source or target list longer than this is shortened to "any of N items (e.g. A, B, C)".
    /// </summary>
    private const int MaxNamesListed = 3;

    private const int MaxEffectLength = 200;

    private const string EntryGap = "\n\n";

    internal sealed record RecipeRow(uint SourceWcid, uint TargetWcid);

    internal sealed record RecipeInfo(
        uint Id,
        uint Skill,
        uint Difficulty,
        uint SuccessWcid,
        uint SuccessAmount,
        string SuccessMessage,
        List<RecipeRow> Rows,
        uint SourceAmount = 1,
        uint TargetAmount = 1
    );

    /// <summary>
    /// Formatted recipe entries per book setup (skill + include + exclude), built on first use. Cleared with the
    /// cookbook cache (/clearcache, content reimports).
    /// </summary>
    private static readonly Dictionary<string, List<string>> EntryCache = new();

    public static bool IsRecipeBook(WorldObject item)
    {
        return item?.GetProperty(PropertyInt.RecipeBookSkill) != null;
    }

    public static void ClearCache()
    {
        lock (EntryCache)
        {
            EntryCache.Clear();
        }
    }

    /// <summary>
    /// The pages to show for a book: its stored pages, followed by the generated recipe and tinkering pages when it's a
    /// recipe book.
    /// </summary>
    public static List<PropertiesBookPageData> GetPages(Book book)
    {
        var pages = book.Biota.PropertiesBookPageData.Clone(book.BiotaDatabaseLock);

        var skillValue = book.GetProperty(PropertyInt.RecipeBookSkill);
        if (skillValue == null)
        {
            return pages;
        }

        var skill = (Skill)skillValue.Value;

        var maxChars = book.Biota.PropertiesBook?.MaxNumCharsPerPage ?? 0;
        if (maxChars <= 0)
        {
            maxChars = DefaultCharsPerPage;
        }

        var entries = GetRecipeEntries(
            skill,
            book.GetProperty(PropertyString.RecipeBookIncludeRecipes),
            book.GetProperty(PropertyString.RecipeBookExcludeRecipes)
        );

        var texts = BuildPageTexts(skill, entries, TinkeringGuide.GetBlocks(skill), pages.Count > 0, maxChars);

        foreach (var text in texts)
        {
            pages.Add(
                new PropertiesBookPageData
                {
                    AuthorId = 0,
                    AuthorName = "",
                    AuthorAccount = "",
                    IgnoreAuthor = false,
                    PageText = text
                }
            );
        }

        return pages;
    }

    /// <summary>
    /// Lays out the generated part of a book: an introduction (unless the book already has stored pages), the recipes,
    /// then the tinkering pages starting on a page of their own.
    /// </summary>
    internal static List<string> BuildPageTexts(
        Skill skill,
        List<string> recipeEntries,
        List<string> tinkeringBlocks,
        bool hasStoredPages,
        int maxChars
    )
    {
        var recipeBlocks = new List<string>();

        if (!hasStoredPages)
        {
            recipeBlocks.Add(GetIntroduction(skill, recipeEntries.Count, tinkeringBlocks.Count > 0));
        }

        recipeBlocks.AddRange(recipeEntries);

        var texts = Paginate(recipeBlocks, maxChars);
        texts.AddRange(Paginate(tinkeringBlocks, maxChars));

        return texts;
    }

    private static string GetIntroduction(Skill skill, int recipeCount, bool hasTinkering)
    {
        var skillName = GetSkillName(skill);

        var sb = new StringBuilder();
        sb.Append($"{skillName} Recipes\n\n");

        if (recipeCount == 0)
        {
            sb.Append($"No {skillName} recipes are known yet.");
        }
        else
        {
            sb.Append(
                $"The {recipeCount} {skillName} recipe{(recipeCount == 1 ? "" : "s")}, from easiest to hardest. "
                    + $"Each lists what it makes, the {skillName} skill it needs, and what to use it on: use the first item on the second."
            );
        }

        if (hasTinkering)
        {
            sb.Append("\n\nTinkering with salvage is described at the back of this book.");
        }

        return sb.ToString();
    }

    public static string GetSkillName(Skill skill)
    {
        return skill.ToSentence();
    }

    private static List<string> GetRecipeEntries(Skill skill, string include, string exclude)
    {
        var key = $"{(int)skill}|{include}|{exclude}";

        lock (EntryCache)
        {
            if (EntryCache.TryGetValue(key, out var cached))
            {
                return cached;
            }
        }

        var includeIds = ParseRecipeIds(include);
        var excludeIds = ParseRecipeIds(exclude);

        var rows = DatabaseManager.World.GetCookbooksForRecipeBook((uint)skill, includeIds.ToList());

        var recipes = rows.Where(r => r.Recipe != null)
            .GroupBy(r => r.RecipeId)
            .Select(g =>
            {
                var recipe = g.First().Recipe;
                return new RecipeInfo(
                    recipe.Id,
                    recipe.Skill,
                    recipe.Difficulty,
                    recipe.SuccessWCID,
                    recipe.SuccessAmount,
                    recipe.SuccessMessage,
                    g.Select(r => new RecipeRow(r.SourceWCID, r.TargetWCID)).ToList(),
                    recipe.SuccessDestroySourceChance > 0 ? recipe.SuccessDestroySourceAmount : 1,
                    recipe.SuccessDestroyTargetChance > 0 ? recipe.SuccessDestroyTargetAmount : 1
                );
            })
            .ToList();

        var entries = BuildEntries(recipes, skill, includeIds, excludeIds, GetWeenieName);

        lock (EntryCache)
        {
            EntryCache[key] = entries;
        }

        return entries;
    }

    private static string GetWeenieName(uint wcid)
    {
        var weenie = DatabaseManager.World.GetCachedWeenie(wcid);

        return weenie?.GetName() ?? $"Unknown Item ({wcid})";
    }

    /// <summary>
    /// Picks the book's recipes and formats them, easiest first.
    /// </summary>
    internal static List<string> BuildEntries(
        List<RecipeInfo> recipes,
        Skill skill,
        HashSet<uint> includeIds,
        HashSet<uint> excludeIds,
        Func<uint, string> getName
    )
    {
        return recipes
            .Where(r => IsListed(r, skill, includeIds, excludeIds))
            .Select(r => (Recipe: r, Title: GetTitle(r, getName)))
            .OrderBy(r => GetRequiredSkill(r.Recipe))
            .ThenBy(r => r.Title, StringComparer.OrdinalIgnoreCase)
            .ThenBy(r => r.Recipe.Id)
            .Select(r => FormatEntry(r.Recipe, r.Title, skill, getName))
            .ToList();
    }

    internal static bool IsListed(RecipeInfo recipe, Skill skill, HashSet<uint> includeIds, HashSet<uint> excludeIds)
    {
        if (recipe.Rows.Count == 0 || excludeIds.Contains(recipe.Id))
        {
            return false;
        }

        if (includeIds.Contains(recipe.Id))
        {
            return true;
        }

        return recipe.Skill == (uint)skill && IsDaraletRecipe(recipe);
    }

    internal static bool IsDaraletRecipe(RecipeInfo recipe)
    {
        return recipe.SuccessWcid >= DaraletWcidStart
            || recipe.Rows.Any(r => r.SourceWcid >= DaraletWcidStart || r.TargetWcid >= DaraletWcidStart);
    }

    /// <summary>
    /// The skill a recipe checks, or 0 when there's no skill check.
    /// </summary>
    private static uint GetRequiredSkill(RecipeInfo recipe)
    {
        return recipe.Skill > 0 && recipe.Difficulty > 0 ? recipe.Difficulty : 0;
    }

    private static string GetTitle(RecipeInfo recipe, Func<uint, string> getName)
    {
        if (recipe.SuccessWcid != 0)
        {
            var name = getName(recipe.SuccessWcid);
            return recipe.SuccessAmount > 1 ? $"{name} x{recipe.SuccessAmount}" : name;
        }

        return FormatNameList(recipe.Rows.Select(r => r.SourceWcid), getName);
    }

    internal static string FormatEntry(RecipeInfo recipe, string title, Skill bookSkill, Func<uint, string> getName)
    {
        var sb = new StringBuilder();

        sb.Append(title);
        sb.Append(" - ");

        if (GetRequiredSkill(recipe) == 0)
        {
            sb.Append("no skill check");
        }
        else if (recipe.Skill == (uint)bookSkill)
        {
            sb.Append($"Skill {recipe.Difficulty}");
        }
        else
        {
            sb.Append($"{GetSkillName((Skill)recipe.Skill)} {recipe.Difficulty}");
        }

        var sources = FormatNameList(recipe.Rows.Select(r => r.SourceWcid), getName);
        var targets = FormatNameList(recipe.Rows.Select(r => r.TargetWcid), getName);

        // recipes that use up several of an item, like 10 Cured Leather for one Armor Reinforcement
        if (recipe.SourceAmount > 1)
        {
            sources += $" x{recipe.SourceAmount}";
        }

        if (recipe.TargetAmount > 1)
        {
            targets += $" x{recipe.TargetAmount}";
        }

        sb.Append($"\n{sources} on {targets}");

        // some messages are just "." placeholders
        if (recipe.SuccessWcid == 0 && recipe.SuccessMessage != null && recipe.SuccessMessage.Any(char.IsLetter))
        {
            var effect = recipe.SuccessMessage.Trim();
            if (effect.Length > MaxEffectLength)
            {
                effect = effect[..(MaxEffectLength - 3)].TrimEnd() + "...";
            }

            sb.Append($"\n{effect}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// "A", "A or B", "A, B or C", or "any of 12 items (e.g. A, B, C)", with duplicate names (items that share a name)
    /// listed once.
    /// </summary>
    internal static string FormatNameList(IEnumerable<uint> wcids, Func<uint, string> getName)
    {
        var names = wcids.Distinct().Select(getName).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        if (names.Count == 0)
        {
            return "";
        }

        if (names.Count > MaxNamesListed)
        {
            return $"any of {names.Count} items (e.g. {string.Join(", ", names.Take(MaxNamesListed))})";
        }

        if (names.Count == 1)
        {
            return names[0];
        }

        return string.Join(", ", names.Take(names.Count - 1)) + " or " + names[^1];
    }

    /// <summary>
    /// Parses "1654-1659, 10012" into a set of recipe IDs. Anything that isn't a number or a range is ignored.
    /// </summary>
    internal static HashSet<uint> ParseRecipeIds(string text)
    {
        var result = new HashSet<uint>();

        if (string.IsNullOrWhiteSpace(text))
        {
            return result;
        }

        foreach (var part in text.Split(new[] { ',', ';', ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var range = part.Split('-');

            if (range.Length == 1 && uint.TryParse(range[0], out var id))
            {
                result.Add(id);
            }
            else if (
                range.Length == 2
                && uint.TryParse(range[0], out var first)
                && uint.TryParse(range[1], out var last)
                && first <= last
                && last - first < 100_000
            )
            {
                for (var i = first; i <= last; i++)
                {
                    result.Add(i);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Packs blocks onto pages of at most maxChars, a blank line apart, never splitting a block across pages unless it
    /// can't fit on a page by itself (then it splits by line, and a line that's too long by characters).
    /// </summary>
    internal static List<string> Paginate(List<string> blocks, int maxChars)
    {
        var pages = new List<string>();
        var current = new StringBuilder();

        void Flush()
        {
            if (current.Length > 0)
            {
                pages.Add(current.ToString());
                current.Clear();
            }
        }

        void Add(string piece, string separator)
        {
            if (current.Length > 0 && current.Length + separator.Length + piece.Length > maxChars)
            {
                Flush();
            }

            if (current.Length > 0)
            {
                current.Append(separator);
            }

            current.Append(piece);
        }

        foreach (var block in blocks)
        {
            if (block.Length <= maxChars)
            {
                Add(block, EntryGap);
                continue;
            }

            var separator = EntryGap;

            foreach (var line in block.Split('\n'))
            {
                for (var start = 0; start < line.Length || start == 0; start += maxChars)
                {
                    var length = Math.Min(maxChars, line.Length - start);
                    Add(line.Substring(start, length), separator);
                    separator = "\n";

                    if (line.Length == 0)
                    {
                        break;
                    }
                }
            }
        }

        Flush();

        return pages;
    }
}
