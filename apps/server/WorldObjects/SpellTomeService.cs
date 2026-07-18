using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Entity;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.WorldObjects;

/// <summary>
/// Spell Tome (WCID 1053989): a plain Book, granted to players by an NPC (outside this file's
/// concern). Every time the player successfully finalizes a scroll (see ScrollFinalizer) for a
/// spell formula they haven't recorded before, that formula's core components (scarabs and tapers
/// excluded — scarabs are the only thing that changes with spell level, so entries are level-agnostic)
/// are added to the Tome, sorted by magic school then component order.
///
/// The discovery record lives on the PLAYER (a new PropertyString), not the item — so a lost/destroyed
/// Tome can simply be re-acquired and it will sync to full history immediately (see SyncOnAcquire,
/// called from Player_Inventory.TryCreateInInventoryWithNetworking whenever a Tome enters a player's
/// inventory via any normal game mechanic — vendor purchase, trade, NPC/quest give). This also means a
/// Tome traded to a different player is rebuilt for ITS new owner, matching per-player semantics rather
/// than carrying over the previous owner's content. Book is a sealed class, so this isn't a WorldObject
/// subclass; the Tome is identified purely by WCID.
/// </summary>
public static class SpellTomeService
{
    public const uint SpellTomeWcid = 1053989;

    private sealed record DiscoveredFormula(MagicSchool School, string BaseName, List<uint> Components);

    /// <summary>
    /// Matches a trailing " I", " VI", " VIII", etc. (a level numeral AC appends to a spell's base
    /// name) so it can be stripped — entries are level-agnostic (see class remarks), so only the base
    /// family name is worth recording. Spells with no such suffix are left unchanged.
    /// </summary>
    private static readonly Regex TrailingLevelNumeral = new(@"^(.*?)\s+[IVXLCDM]+$", RegexOptions.Compiled);

    /// <summary>
    /// Syncs a Tome to the player's current discovery record the moment it enters their inventory,
    /// so a newly-acquired Tome (bought, given, traded, or a replacement for a lost one) is never
    /// stale or blank while waiting for the player to discover something new. No-ops for anything
    /// that isn't a Spell Tome.
    /// </summary>
    public static void SyncOnAcquire(Player player, WorldObject item)
    {
        if (item.WeenieClassId != SpellTomeWcid || item is not Book tome)
        {
            return;
        }

        RebuildTome(player, tome, SortFormulas(GetDiscoveredFormulas(player)));
    }

    /// <summary>
    /// Records a newly-crafted spell's core formula (if not already known) against the player, and
    /// rebuilds every Spell Tome currently in their inventory to reflect the updated, sorted list.
    /// </summary>
    public static void RecordDiscoveredFormula(Player player, Spell spell)
    {
        if (spell?.Formula?.Components == null)
        {
            return;
        }

        var coreComponents = StripToCoreComponents(spell.Formula.Components);

        if (coreComponents.Count == 0)
        {
            return;
        }

        var school = spell.School;
        var discovered = GetDiscoveredFormulas(player);

        if (discovered.Any(f => f.School == school && f.Components.SequenceEqual(coreComponents)))
        {
            return;
        }

        discovered.Add(new DiscoveredFormula(school, GetBaseSpellName(spell.Name), coreComponents));
        SaveDiscoveredFormulas(player, discovered);

        var tomes = player.GetInventoryItemsOfWCID(SpellTomeWcid).OfType<Book>().ToList();
        if (tomes.Count == 0)
        {
            return;
        }

        var sorted = SortFormulas(discovered);

        foreach (var tome in tomes)
        {
            RebuildTome(player, tome, sorted);
        }
    }

    /// <summary>
    /// Strips scarabs and tapers, leaving only the level-invariant, account-invariant core components
    /// (herb, powder, potion, talisman, etc.) in their original relative order.
    /// </summary>
    private static List<uint> StripToCoreComponents(List<uint> components)
    {
        return components.Where(c => !SpellFormula.IsScarab(c) && !SpellFormula.IsTaper(c)).ToList();
    }

    /// <summary>
    /// Strips a trailing level numeral off a spell name, e.g. "Blood Loather VI" -> "Blood Loather".
    /// Leaves the name unchanged if it doesn't end in one.
    /// </summary>
    private static string GetBaseSpellName(string spellName)
    {
        if (string.IsNullOrWhiteSpace(spellName))
        {
            return spellName;
        }

        var match = TrailingLevelNumeral.Match(spellName.Trim());
        return match.Success ? match.Groups[1].Value : spellName;
    }

    private static List<DiscoveredFormula> SortFormulas(List<DiscoveredFormula> formulas)
    {
        return formulas
            .OrderBy(f => (int)f.School)
            .ThenBy(f => string.Join(",", f.Components.Select(c => c.ToString("D4"))))
            .ToList();
    }

    private static string GetSchoolDisplayName(MagicSchool school)
    {
        return school switch
        {
            MagicSchool.WarMagic => "War Magic",
            MagicSchool.LifeMagic => "Life Magic",
            MagicSchool.PortalMagic => "Portal Magic",
            MagicSchool.CreatureEnchantment => "Creature Enchantment",
            MagicSchool.VoidMagic => "Void Magic",
            _ => school.ToString()
        };
    }

    private const string ScarabNoteText =
        "A list of spell formulas you have discovered. The formulas recorded in this tome exclude scarabs, listing only the core spell components (herb, powder, potion, "
        + "talisman). Select a scarab separately, based on the "
        + "level of scroll you wish to create, and use it as the first component in the spell formula. .";

    /// <summary>
    /// Blank-line gap between the scarab note and the first entry, and between each subsequent entry
    /// (2 newlines = 1 blank line between the last line of one block and the first line of the next).
    /// </summary>
    private const string EntryGap = "\n\n";

    /// <summary>
    /// Shortens component names that follow the game's own "Powdered X" / "X Talisman" naming
    /// conventions ("Powdered Amber" -> "P. Amber", "Yew Talisman" -> "Yew T.") so more formulas fit
    /// on one line. Names that don't match either pattern are left as-is.
    /// </summary>
    private static string AbbreviateComponentName(string name)
    {
        const string poweredPrefix = "Powdered ";
        const string talismanSuffix = " Talisman";

        if (name.StartsWith(poweredPrefix, StringComparison.Ordinal))
        {
            name = "P. " + name[poweredPrefix.Length..];
        }

        if (name.EndsWith(talismanSuffix, StringComparison.Ordinal))
        {
            name = name[..^talismanSuffix.Length] + " T.";
        }

        return name;
    }

    /// <summary>
    /// Rebuilds a Tome from the given sorted formula list as a single combined page: the scarab note,
    /// then every entry one line apart, rather than one entry per page. NOTE: this doesn't paginate —
    /// if the combined text ever exceeds PropertiesBook.MaxNumCharsPerPage, it's written as-is anyway
    /// (Book.AddPage doesn't enforce that limit server-side), so very large discovery lists may render
    /// truncated or oddly in the client's page-reading UI. Not a concern at current formula-pool sizes.
    /// </summary>
    private static void RebuildTome(Player player, Book tome, List<DiscoveredFormula> sortedFormulas)
    {
        tome.Biota.PropertiesBookPageData.Clear();

        var comps = SpellComponent.SpellComponentsTable.SpellComponents;

        var blocks = new List<string> { ScarabNoteText };

        foreach (var formula in sortedFormulas)
        {
            var componentNames = formula.Components.Select(c =>
                AbbreviateComponentName(comps.TryGetValue(c, out var comp) ? comp.Name : "Unknown Component")
            );

            // Two lines per entry: name/school header, then the whole formula on one line.
            blocks.Add(
                $"{formula.BaseName} ({GetSchoolDisplayName(formula.School)})\n" + string.Join(", ", componentNames)
            );
        }

        var pageText = string.Join(EntryGap, blocks);

        tome.AddPage(player.Guid.Full, player.Name, player.Account.AccountName, true, pageText, out _);

        if (tome.Biota.PropertiesBookPageData.Count == 0)
        {
            tome.SetProperty(PropertyInt.AppraisalPages, 0);
        }

        player.EnqueueBroadcast(new GameMessageUpdateObject(tome));
    }

    /// <summary>
    /// Format: "{schoolInt}:{baseName}:{comp1}-{comp2}-...", semicolon-separated entries. Entries saved
    /// before BaseName was added (2-part, no name) are silently skipped rather than crashing.
    /// </summary>
    private static List<DiscoveredFormula> GetDiscoveredFormulas(Player player)
    {
        var result = new List<DiscoveredFormula>();

        var raw = player.GetProperty(PropertyString.SpellTomeDiscoveredFormulas);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return result;
        }

        foreach (var entry in raw.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = entry.Split(':', 3);
            if (parts.Length != 3 || !int.TryParse(parts[0], out var schoolInt))
            {
                continue;
            }

            var baseName = parts[1];

            var components = new List<uint>();
            foreach (var compStr in parts[2].Split('-', StringSplitOptions.RemoveEmptyEntries))
            {
                if (uint.TryParse(compStr, out var compId))
                {
                    components.Add(compId);
                }
            }

            if (components.Count > 0)
            {
                result.Add(new DiscoveredFormula((MagicSchool)schoolInt, baseName, components));
            }
        }

        return result;
    }

    private static void SaveDiscoveredFormulas(Player player, List<DiscoveredFormula> formulas)
    {
        if (formulas.Count == 0)
        {
            player.RemoveProperty(PropertyString.SpellTomeDiscoveredFormulas);
            return;
        }

        var raw = string.Join(
            ';',
            formulas.Select(f => $"{(int)f.School}:{f.BaseName}:{string.Join('-', f.Components)}")
        );

        player.SetProperty(PropertyString.SpellTomeDiscoveredFormulas, raw);
    }
}
