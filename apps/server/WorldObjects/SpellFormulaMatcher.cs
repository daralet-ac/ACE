using System.Collections.Generic;
using System.Linq;
using ACE.Database;
using ACE.Entity.Enum;
using ACE.Server.Entity;
using ACE.Server.Factories.Tables;
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
/// Only spells with an existing Scroll weenie in the world DB, AND that are in the explicit
/// <see cref="AllowedSpellIds"/> allowlist, are considered candidates.
/// </summary>
public static class SpellFormulaMatcher
{
    private static readonly ILogger _log = Log.ForContext(typeof(SpellFormulaMatcher));

    /// <summary>
    /// The level-1 head of every spell family Scroll Writing can produce. Mirrors the War/Life/Void
    /// selections in <see cref="Factories.Tables.Spells.ScrollSpells"/> (the loot-scroll table) —
    /// the same families already judged fit to exist as a standalone scroll — but kept as its own
    /// list rather than a reference to that table, so retuning loot-scroll drops can't silently
    /// change what players are able to craft.
    ///
    /// Each family is expanded up to <see cref="MaxScribeLevel"/> via <see cref="SpellLevelProgression"/>;
    /// level 8 "Ultimate" spells are deliberately excluded and stay loot/quest-reward only.
    /// </summary>
    private static readonly List<SpellId> AllowedSpellFamilies = new()
    {
        // Life
        SpellId.HealSelf1,
        SpellId.HealOther1,
        SpellId.HarmOther1,
        SpellId.RevitalizeSelf1,
        SpellId.RevitalizeOther1,
        SpellId.EnfeebleOther1,
        SpellId.ManaBoostSelf1,
        SpellId.ManaBoostOther1,
        SpellId.ManaDrainOther1,
        SpellId.FellowshipHeal1,
        SpellId.FellowshipRevitalize1,
        SpellId.FellowshipManaBoost1,
        SpellId.HealthToStaminaSelf1,
        SpellId.HealthToManaSelf1,
        SpellId.StaminaToHealthSelf1,
        SpellId.StaminaToManaSelf1,
        SpellId.ManaToHealthSelf1,
        SpellId.ManaToStaminaSelf1,
        SpellId.InfuseHealth1,
        SpellId.InfuseStamina1,
        SpellId.InfuseMana1,
        SpellId.DrainHealth1,
        SpellId.DrainStamina1,
        SpellId.DrainMana1,
        SpellId.HealthBolt1,
        SpellId.StaminaBolt1,
        SpellId.ManaBolt1,
        SpellId.DispelLifeBadSelf1,
        SpellId.DispelLifeBadOther1,
        SpellId.VitalityMend1,
        SpellId.VigorMend1,
        SpellId.ClarityMend1,
        SpellId.VitalityMendOther1,
        SpellId.VigorMendOther1,
        SpellId.ClarityMendOther1,

        // War
        SpellId.FlameBolt1,
        SpellId.FrostBolt1,
        SpellId.AcidStream1,
        SpellId.ShockWave1,
        SpellId.LightningBolt1,
        SpellId.ForceBolt1,
        SpellId.WhirlingBlade1,
        SpellId.AcidStreak1,
        SpellId.FlameStreak1,
        SpellId.ForceStreak1,
        SpellId.FrostStreak1,
        SpellId.LightningStreak1,
        SpellId.ShockwaveStreak1,
        SpellId.WhirlingBladeStreak1,
        SpellId.AcidArc1,
        SpellId.ForceArc1,
        SpellId.FrostArc1,
        SpellId.LightningArc1,
        SpellId.FlameArc1,
        SpellId.ShockArc1,
        SpellId.BladeArc1,
        SpellId.AcidBlast1,
        SpellId.ShockBlast1,
        SpellId.FrostBlast1,
        SpellId.LightningBlast1,
        SpellId.FlameBlast1,
        SpellId.ForceBlast1,
        SpellId.BladeBlast1,
        SpellId.AcidVolley1,
        SpellId.BludgeoningVolley1,
        SpellId.FrostVolley1,
        SpellId.LightningVolley1,
        SpellId.FlameVolley1,
        SpellId.ForceVolley1,
        SpellId.BladeVolley1,

        // Void
        SpellId.Corrosion1,
        SpellId.CurseDestructionOther1,
        SpellId.CurseWeakness1,
    };

    /// <summary>
    /// Highest spell level Scroll Writing will produce for any family in <see cref="AllowedSpellFamilies"/>.
    /// Level 8 ("Ultimate") is excluded, matching the loot-scroll table's NumLevels cap.
    /// </summary>
    private const int MaxScribeLevel = 7;

    private static HashSet<uint> _allowedSpellIds;

    /// <summary>
    /// Every exact spellId Scroll Writing can produce, expanded from <see cref="AllowedSpellFamilies"/>.
    /// Built once from static content (spell enum + level progression table), not the world DB, so it
    /// never needs rebuilding at runtime.
    /// </summary>
    private static HashSet<uint> AllowedSpellIds => _allowedSpellIds ??= BuildAllowedSpellIds();

    private static HashSet<uint> BuildAllowedSpellIds()
    {
        var result = new HashSet<uint>();

        foreach (var family in AllowedSpellFamilies)
        {
            var levels = SpellLevelProgression.GetSpellLevels(family);

            if (levels == null)
            {
                _log.Warning("SpellFormulaMatcher: no level progression found for {Spell}", family);
                continue;
            }

            for (var i = 0; i < levels.Count && i < MaxScribeLevel; i++)
            {
                result.Add((uint)levels[i]);
            }
        }

        return result;
    }

    /// <summary>
    /// spellId -> scroll weenie classId, built from every Scroll-type weenie with a PropertyDataId.Spell
    /// that is in the Scroll Writing allowlist.
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
            if (!AllowedSpellIds.Contains(spellId))
            {
                continue;
            }

            var spell = new Spell(spellId, false);

            if (spell._spellBase != null)
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
