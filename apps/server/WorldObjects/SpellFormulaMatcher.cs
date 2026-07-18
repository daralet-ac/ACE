using System.Collections.Generic;
using System.Linq;
using ACE.Database;
using ACE.Entity.Enum;
using ACE.Server.Entity;
using Serilog;

namespace ACE.Server.WorldObjects;

/// <summary>
/// Reverse lookup for Scroll Writing: given an ordered list of scribed spell component IDs,
/// finds the spell (if any) whose formula matches it.
///
/// Tapers are deliberately excluded from matching (and from scribing, see ScrollWritingService) —
/// taper choice/order is normally account-hash-randomized (SpellFormula.GetPlayerFormula), but
/// Scroll Writing ignores that entirely so every player scribes the same formula for a given spell.
///
/// Only spells with an existing Scroll weenie in the world DB, AND whose magic school is in
/// <see cref="AllowedSchools"/>, are considered candidates.
/// </summary>
public static class SpellFormulaMatcher
{
    private static readonly ILogger _log = Log.ForContext(typeof(SpellFormulaMatcher));

    /// <summary>
    /// The only magic schools Scroll Writing can currently produce scrolls for.
    /// Narrow this further (or add per-spell exclusions alongside it) as needed.
    /// </summary>
    private static readonly HashSet<MagicSchool> AllowedSchools = new() { MagicSchool.WarMagic, MagicSchool.LifeMagic };

    /// <summary>
    /// spellId -> scroll weenie classId, built from every Scroll-type weenie with a PropertyDataId.Spell
    /// whose spell is in an allowed magic school.
    /// </summary>
    private static Dictionary<uint, uint> _scrollWcidBySpellId;

    private static Dictionary<uint, uint> ScrollWcidBySpellId
    {
        get
        {
            if (_scrollWcidBySpellId == null)
            {
                RebuildScrollMap();
            }

            return _scrollWcidBySpellId;
        }
    }

    /// <summary>
    /// Rebuilds the spellId -> scroll wcid cache from the world DB. Called lazily on first use;
    /// call again after live-editing scroll weenie content to refresh without a server restart.
    /// </summary>
    public static void RebuildScrollMap()
    {
        var rawMap = DatabaseManager.World.GetScrollWeenieSpellMap();

        var filtered = new Dictionary<uint, uint>();

        foreach (var (spellId, scrollWcid) in rawMap)
        {
            var spell = new Spell(spellId, false);

            if (spell._spellBase != null && AllowedSchools.Contains(spell.School))
            {
                filtered.Add(spellId, scrollWcid);
            }
        }

        _scrollWcidBySpellId = filtered;
    }

    public static bool TryGetScrollWcid(uint spellId, out uint scrollWcid)
    {
        return ScrollWcidBySpellId.TryGetValue(spellId, out scrollWcid);
    }

    /// <summary>
    /// Strips taper components out of a formula, leaving the account-invariant components in
    /// their original relative order.
    /// </summary>
    private static List<uint> StripTapers(List<uint> components)
    {
        var result = new List<uint>(components.Count);

        foreach (var component in components)
        {
            if (!SpellFormula.IsTaper(component))
            {
                result.Add(component);
            }
        }

        return result;
    }

    /// <summary>
    /// Attempts to match an ordered list of scribed component IDs (tapers excluded) against a known
    /// spell's formula (also with tapers stripped). A match against either the standard formula or the
    /// foci (scarab-only, since tapers are stripped) formula succeeds.
    /// </summary>
    public static bool TryMatch(List<uint> scribed, out uint spellId, out Spell spell)
    {
        spellId = 0;
        spell = null;

        if (scribed == null || scribed.Count == 0)
        {
            return false;
        }

        foreach (var candidateSpellId in ScrollWcidBySpellId.Keys)
        {
            var candidate = new Spell(candidateSpellId, false);

            if (candidate._spellBase == null || candidate.Formula == null)
            {
                _log.Warning(
                    "SpellFormulaMatcher.TryMatch: scroll map contains spellId {SpellId} with no matching dat spell",
                    candidateSpellId
                );
                continue;
            }

            var canonicalFormula = StripTapers(candidate.Formula.Components);
            if (scribed.Count == canonicalFormula.Count && scribed.SequenceEqual(canonicalFormula))
            {
                spellId = candidateSpellId;
                spell = candidate;
                return true;
            }

            var fociFormula = StripTapers(candidate.Formula.GetFociFormula());
            if (fociFormula.Count > 0 && scribed.Count == fociFormula.Count && scribed.SequenceEqual(fociFormula))
            {
                spellId = candidateSpellId;
                spell = candidate;
                return true;
            }
        }

        return false;
    }
}
