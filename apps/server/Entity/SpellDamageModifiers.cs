namespace ACE.Server.Entity;

/// <summary>
/// The multipliers applied to a spell projectile's base damage, before the target's mitigation (see SpellMitigationModifiers).
/// Each one is 1.0 when it has no effect.
/// Every field starts at 0 and must be set in SpellProjectile.GetDamageModifiers, or the spell deals no damage.
/// </summary>
internal struct SpellDamageModifiers
{
    /// <summary>
    /// The critical damage multiplier on a critical hit
    /// </summary>
    public float Critical;

    public float Attribute;
    public float Elemental;
    public float Slayer;

    /// <summary>
    /// COMBAT ABILITY - Overload
    /// </summary>
    public float Overload;

    /// <summary>
    /// COMBAT ABILITY - Battery
    /// </summary>
    public float Battery;

    /// <summary>
    /// RATING - Elementalist (jewels)
    /// </summary>
    public float JewelElementalist;

    /// <summary>
    /// RATING - Elemental damage bonus (jewels)
    /// </summary>
    public float JewelElemental;

    /// <summary>
    /// RATING - Self Harm (jewels)
    /// </summary>
    public float JewelSelfHarm;

    /// <summary>
    /// RATING - Red Fury (jewels)
    /// </summary>
    public float JewelRedFury;

    /// <summary>
    /// RATING - Blue Fury (jewels)
    /// </summary>
    public float JewelBlueFury;

    /// <summary>
    /// Each target a volley has already struck through reduces its damage
    /// </summary>
    public float StrikethroughPenalty;

    public float Archetype;
    public float LevelScaling;

    /// <summary>
    /// From emote CastSpell / CastSpellInstant (emote.Percent)
    /// </summary>
    public float DamageMultiplier;

    /// <summary>
    /// Proc spells and Enchanted Blade: 1% of the weapon's spellcraft
    /// </summary>
    public float Spellcraft;

    /// <summary>
    /// The dungeon mod damage bonus, for traps and the creatures the archetype system doesn't scale
    /// </summary>
    public float LandblockScaling;

    public float Backstab;

    /// <summary>
    /// Returns baseDamage with every modifier applied
    /// </summary>
    public readonly float Apply(float baseDamage)
    {
        return baseDamage
            * Critical
            * Attribute
            * Elemental
            * Slayer
            * Overload
            * Battery
            * JewelElementalist
            * JewelElemental
            * JewelSelfHarm
            * JewelRedFury
            * JewelBlueFury
            * StrikethroughPenalty
            * Archetype
            * LevelScaling
            * DamageMultiplier
            * Spellcraft
            * LandblockScaling
            * Backstab;
    }
}
