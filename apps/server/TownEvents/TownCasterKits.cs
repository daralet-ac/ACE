using System;
using System.Collections.Generic;
using System.Linq;
using ACE.Common;
using ACE.Entity.Enum;
using ACE.Entity.Models;
using ACE.Server.Entity;
using ACE.Server.Factories.Tables;
using ACE.Server.WorldObjects;

namespace ACE.Server.TownEvents;

/// <summary>
/// How a town event attacker fights: with its weapons and claws, as a war caster, or as a support caster that heals and
/// buffs the other attackers
/// </summary>
public enum TownCasterRole
{
    None,
    War,
    Support
}

/// <summary>
/// The spells a town event attacker is given for a caster role when its own spellbook lacks them, at the spell level of the
/// town's tier (TownGuardUpgrades.GetSpellLevel). Monster spellbook probabilities are 2 plus the chance, and are tried in
/// order: support spells first, so a support caster heals and buffs whenever an ally needs it and fights otherwise.
/// </summary>
public static class TownCasterKits
{
    // a caster role's archetype magic is at least this, so its spells land and it has the mana for them
    public const double MinCasterMagic = 1.0;

    private const float HealChance = 2.5f;
    private const float WeaponBuffChance = 2.3f;
    private const float HarmChance = 2.2f;
    private const float MartyrChance = 2.1f;
    private const float BoltChance = 2.35f;

    private static readonly List<SpellId>[] Bolts =
    {
        SpellLevelProgression.FlameBolt,
        SpellLevelProgression.FrostBolt,
        SpellLevelProgression.AcidStream,
        SpellLevelProgression.LightningBolt,
        SpellLevelProgression.ForceBolt,
        SpellLevelProgression.ShockWave,
        SpellLevelProgression.WhirlingBlade
    };

    // Martyr's Hecatomb, Tenacity and Blight, levels I to VII
    private static readonly int[] MartyrsHecatomb = { 2760, 2761, 2762, 2763, 2764, 2765, 2766 };
    private static readonly int[] MartyrsTenacity = { 2767, 2768, 2769, 2770, 2771, 2772, 2773 };
    private static readonly int[] MartyrsBlight = { 2774, 2775, 2776, 2777, 2778, 2779, 2780 };

    /// <summary>
    /// The spells for a role at a spell level (1 to 8), in the order they're tried, with their spellbook probabilities
    /// </summary>
    public static List<(int Spell, float Probability)> GetKit(TownCasterRole role, int spellLevel)
    {
        var level = Math.Clamp(spellLevel, 1, 8);
        var martyrLevel = Math.Min(level, 7) - 1;

        var harm = ((int)SpellLevelProgression.HarmOther[level - 1], HarmChance);

        switch (role)
        {
            case TownCasterRole.War:
                var bolts = Bolts[ThreadSafeRandom.Next(0, Bolts.Length - 1)];
                return new List<(int, float)> { ((int)bolts[level - 1], BoltChance) };

            case TownCasterRole.Support:
                return new List<(int, float)>
                {
                    ((int)SpellLevelProgression.HealOther[level - 1], HealChance),
                    ((int)SpellLevelProgression.BloodDrinkerOther[level - 1], WeaponBuffChance),
                    ((int)SpellLevelProgression.HeartSeekerOther[level - 1], WeaponBuffChance),
                    harm,
                    (MartyrsHecatomb[martyrLevel], MartyrChance),
                    (MartyrsTenacity[martyrLevel], MartyrChance),
                    (MartyrsBlight[martyrLevel], MartyrChance)
                };

            default:
                return new List<(int, float)>();
        }
    }

    /// <summary>
    /// The role a creature's own spellbook already gives it: Support with any support spell, War with any harmful one
    /// </summary>
    public static TownCasterRole GetNaturalRole(Creature creature)
    {
        var spells = creature.Biota.GetKnownSpellsIds(creature.BiotaDatabaseLock);

        if (spells.Any(Creature.IsSupportSpell))
        {
            return TownCasterRole.Support;
        }

        if (spells.Any(id => new Spell(id) is { NotFound: false, IsHarmful: true }))
        {
            return TownCasterRole.War;
        }

        return TownCasterRole.None;
    }

    /// <summary>
    /// Moves every spell in a creature's spellbook with levels (I to VIII) to the spell level given, keeping its place and
    /// chance: for a creature scaled to another tier. Spells without levels are kept as they are.
    /// </summary>
    public static void ScaleSpellbook(Creature creature, int spellLevel)
    {
        var own = creature.Biota.CloneSpells(creature.BiotaDatabaseLock);
        if (own.Count == 0)
        {
            return;
        }

        var spellbook = new Dictionary<int, float>();

        foreach (var (spell, probability) in own)
        {
            var level1 = SpellLevelProgression.GetLevel1SpellId((SpellId)spell);
            var scaled = level1 == SpellId.Undef ? SpellId.Undef : SpellLevelProgression.GetSpellAtLevel(level1, spellLevel, true);

            spellbook.TryAdd(scaled == SpellId.Undef ? spell : (int)scaled, probability);
        }

        creature.BiotaDatabaseLock.EnterWriteLock();
        try
        {
            creature.Biota.PropertiesSpellBook = spellbook;
        }
        finally
        {
            creature.BiotaDatabaseLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Makes an attacker a caster of a role: its own spellbook if that already fits, and otherwise the role's kit ahead of
    /// what it has. Either way its archetype magic is raised to MinCasterMagic. Call before it enters the world.
    /// </summary>
    public static void Apply(Creature creature, TownCasterRole role, int spellLevel)
    {
        if (role == TownCasterRole.None)
        {
            return;
        }

        creature.TownEventMinMagic = MinCasterMagic;

        if (GetNaturalRole(creature) == role)
        {
            return;
        }

        // the kit goes first, as spellbooks are tried in order: a new spellbook with it ahead of the creature's own
        var own = creature.Biota.CloneSpells(creature.BiotaDatabaseLock);
        var spellbook = new Dictionary<int, float>();

        foreach (var (spell, probability) in GetKit(role, spellLevel).Concat(own.Select(kv => (kv.Key, kv.Value))))
        {
            spellbook.TryAdd(spell, probability);
        }

        creature.BiotaDatabaseLock.EnterWriteLock();
        try
        {
            creature.Biota.PropertiesSpellBook = spellbook;
        }
        finally
        {
            creature.BiotaDatabaseLock.ExitWriteLock();
        }
    }
}
