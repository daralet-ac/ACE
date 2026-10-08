namespace ACE.Server.Entity;

/// <summary>
/// The multipliers that reduce (or increase) a spell projectile's damage after its damage modifiers (see SpellDamageModifiers).
/// Each one is 1.0 when it has no effect.
/// Every field starts at 0 and must be set in SpellProjectile.GetMitigationModifiers, or the spell deals no damage.
/// </summary>
internal struct SpellMitigationModifiers
{
    /// <summary>
    /// Magic absorption (shields, magic absorbing items), RATING - Nullification and Aegis in PvP
    /// </summary>
    public float Absorb;

    public float Ward;
    public float Resistance;

    /// <summary>
    /// 0.5 for a partial resist, otherwise 1.0
    /// </summary>
    public float Resisted;

    /// <summary>
    /// SPEC BONUS - Magic Defense
    /// </summary>
    public float SpecDefense;

    /// <summary>
    /// RATING - Physical / Elemental Ward (jewels)
    /// </summary>
    public float DamageTypeWard;

    /// <summary>
    /// Returns damage with every modifier applied
    /// </summary>
    public readonly float Apply(float damage)
    {
        return damage * Absorb * Ward * Resistance * Resisted * SpecDefense * DamageTypeWard;
    }
}
