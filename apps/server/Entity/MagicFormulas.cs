using System;
using ACE.Entity.Enum;

namespace ACE.Server.Entity;

/// <summary>
/// The pure formulas behind spell casting and spell damage: numbers in, numbers out, no game objects or randomness.
/// </summary>
internal static class MagicFormulas
{
    /// <summary>
    /// The damage multiplier for a resist: a full hit 1.0, a partial resist 0.5, a full resist 0.0
    /// </summary>
    public static float GetResistedMod(PartialEvasion partialEvasion)
    {
        return partialEvasion switch
        {
            PartialEvasion.None => 1.0f,
            PartialEvasion.Some => 0.5f,
            _ => 0.0f,
        };
    }

    /// <summary>
    /// The maximum distance a caster with this magic skill can cast a spell, up to maxRange
    /// </summary>
    public static float GetMaxCastRange(float baseRangeConstant, float baseRangeMod, uint magicSkill, float maxRange)
    {
        return Math.Min(baseRangeConstant + magicSkill * baseRangeMod, maxRange);
    }

    /// <summary>
    /// The magic skill bonus from an item's spellcraft: 10% of the spellcraft, including the Arcane Lore spec bonus
    /// </summary>
    public static uint GetSpellcraftSkillBonus(int itemSpellcraft, uint arcaneLoreBonus)
    {
        return (uint)((itemSpellcraft + arcaneLoreBonus) * 0.1);
    }

    /// <summary>
    /// Proc spells and Enchanted Blade receive 1% of spellcraft as a damage multiplier (300 spellcraft = x3 damage)
    /// </summary>
    public static float GetProcSpellcraftDamageMod(long spellcraft)
    {
        return spellcraft * 0.01f;
    }

    /// <summary>
    /// A proc spell cast on yourself scales with your magic skill (plus 10% of the spellcraft) vs the spell's power,
    /// from x0.5 to x2
    /// </summary>
    public static float GetSelfTargetProcMod(uint magicSkill, uint spellcraft, uint spellPower)
    {
        var procSpellSkill = (int)(magicSkill + spellcraft * 0.1);

        if (spellPower == 0)
        {
            return 1.0f;
        }

        var mod = (float)procSpellSkill / spellPower;

        return Math.Clamp(mod, 0.5f, 2.0f);
    }

    /// <summary>
    /// A player's ward shortens a debuff on them halfway to the ward's full mitigation
    /// </summary>
    public static float GetWardDebuffDurationMod(float wardMod)
    {
        return wardMod + (1 - wardMod) * 0.5f;
    }

    /// <summary>
    /// Aegis (magic absorption) is 72% effective in PvP
    /// http://acpedia.org/wiki/Announcements_-_2014/01_-_Forces_of_Nature
    /// </summary>
    public static float GetPvpAbsorbMod(float absorbMod)
    {
        absorbMod = 1 - absorbMod;
        absorbMod *= 0.72f;
        return 1 - absorbMod;
    }

    /// <summary>
    /// Each target a volley has already struck through reduces its damage: 1/2, 1/3, 1/4...
    /// </summary>
    public static float GetStrikethroughPenalty(int strikethrough)
    {
        return 1.0f / (strikethrough + 1);
    }

    /// <summary>
    /// The damage multiplier from a shield's magic absorption.
    /// https://asheron.fandom.com/wiki/Shield
    /// Reduction Percent = (cap * specMod * baseSkill * 0.003f) - (cap * specMod * 0.3f)
    /// Cap = Maximum reduction, SpecMod = 1.0 for spec, 0.8 for trained,
    /// BaseSkill = 100 to 433 (above 433 base shield you always achieve the maximum %)
    /// </summary>
    public static float GetShieldMagicAbsorbMod(float cap, uint baseShieldSkill, bool specialized)
    {
        var baseSkill = Math.Min(baseShieldSkill, 433);
        var specMod = specialized ? 1.0f : 0.8f;

        // speced, 100 skill = 0%
        // trained, 100 skill = 0%
        // speced, 200 skill = 30%
        // trained, 200 skill = 24%
        // speced, 300 skill = 60%
        // trained, 300 skill = 48%
        // speced, 433 skill = 100%
        // trained, 433 skill = 80%

        var reduction = (cap * specMod * baseSkill * 0.003f) - (cap * specMod * 0.3f);

        return Math.Min(1.0f, 1.0f - reduction);
    }

    /// <summary>
    /// The damage multiplier from a 'Magic Absorbing' bow or caster.
    /// https://asheron.fandom.com/wiki/Category:Magic_Absorbing
    /// - For a 25% maximum item: (magic absorbing %) = 25 - (0.1 * (319 - base magic defense))
    /// - For a 10% maximum item: (magic absorbing %) = 10 - (0.04 * (319 - base magic defense))
    /// The wiki likely has a typo for the 10% formula (0.4 instead of 0.04): with 0.04,
    /// both formulas start to become effective at base magic defense 69.
    /// This is an equivalent formula that produces the correct results for any %.
    /// </summary>
    public static float GetMagicAbsorbingMod(double maxPercent, uint baseMagicDefense)
    {
        var baseCap = 319;
        var diff = Math.Max(0, baseCap - baseMagicDefense);

        var percent = maxPercent - maxPercent * diff * 0.004f;

        return Math.Min(1.0f, 1.0f - (float)percent);
    }

    /// <summary>
    /// The angle between the projectiles of a spread spell (ie. a wall or a ring)
    /// </summary>
    public static float GetSpreadAnglePerStep(float spreadAngle, int numProjectiles)
    {
        if (spreadAngle == 0.0f || numProjectiles == 1)
        {
            return 0.0f;
        }

        if (numProjectiles % 2 == 1)
        {
            numProjectiles--;
        }

        return spreadAngle / numProjectiles;
    }

    /// <summary>
    /// COMBAT ABILITY - Overload: the chance (0-1) a spell burns its caster, half the charge level
    /// </summary>
    public static float GetOverloadBacklashChance(float chargeLevel)
    {
        return chargeLevel * 0.5f;
    }

    /// <summary>
    /// COMBAT ABILITY - Overload: the burn's damage, 10% of the spell's base mana at full charge
    /// </summary>
    public static int GetOverloadBacklashDamage(float chargeLevel, uint spellBaseMana)
    {
        return Convert.ToInt32(0.1f * chargeLevel * spellBaseMana);
    }

    /// <summary>
    /// The mana Mana Conversion saves: up to half the cost, from a roll (0-1) within the caster's chance range
    /// </summary>
    /// <param name="reductionFraction">The fraction of the cost saved</param>
    public static uint GetManaConversionSavings(uint manaCost, double roll, out double reductionFraction)
    {
        const float maxManaReduction = 0.5f;

        reductionFraction = maxManaReduction * roll;

        return (uint)Math.Round(manaCost * reductionFraction);
    }

    /// <summary>
    /// SPEC BONUS - Mana Conversion: half of the saved mana is returned as health and stamina
    /// </summary>
    public static int GetSpecManaConversionRefund(uint savedMana)
    {
        return (int)Math.Round(savedMana * 0.5f);
    }

    /// <summary>
    /// A monster's chance to cast a spell from its spellbook. Spellbook probabilities have base 2.0
    /// (a 5% chance is 2.05); a value without it is a percent.
    /// </summary>
    public static float GetSpellbookCastChance(float probability)
    {
        return probability > 2.0f ? probability - 2.0f : probability / 100.0f;
    }

    /// <summary>
    /// A monster spellbook entry with probability exactly 2.0 (heals / drains) is cast more as the monster loses health,
    /// up to 33% at no health
    /// </summary>
    public static float GetSpellbookHealthCastChance(float maxHealth, float currentHealth)
    {
        var maxProbability = 0.33f;
        var reciprocal = 1 / maxProbability;

        return ((maxHealth - currentHealth) / maxHealth) / reciprocal;
    }
}
