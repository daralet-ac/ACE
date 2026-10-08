namespace ACE.Server.Entity;

/// <summary>
/// The multipliers applied to an attack's base damage, before the defender's mitigation (see MitigationModifiers).
/// Each one is 1.0 when it has no effect.
/// </summary>
internal struct DamageModifiers
{
    public float Attribute;
    public float Power;
    public float Slayer;

    /// <summary>
    /// Damage rating, combined with the PK damage rating in PK battles and the critical damage rating on critical hits
    /// </summary>
    public float DamageRating;

    public float Recklessness;
    public float SneakAttack;
    public float Backstab;
    public float AttackHeight;

    /// <summary>
    /// RATING - Elemental damage bonus (jewels)
    /// </summary>
    public float ElementalRating;

    /// <summary>
    /// RATING - Pierce: piercing resistance penetration (JEWEL - Black Garnet)
    /// </summary>
    public float PierceRating;

    public float DualWield;
    public float TwoHandedCombat;

    /// <summary>
    /// COMBAT ABILITY - Fury stance and Enrage
    /// </summary>
    public float Fury;

    /// <summary>
    /// COMBAT ABILITY - Relentless stance penalty
    /// </summary>
    public float Relentless;

    /// <summary>
    /// COMBAT ABILITY - Steady Strike
    /// </summary>
    public float SteadyStrike;

    public float Ammo;
    public float LevelScaling;

    /// <summary>
    /// Returns baseDamage with every modifier applied
    /// </summary>
    public readonly float Apply(float baseDamage)
    {
        return baseDamage
               * Attribute
               * Power
               * Slayer
               * DamageRating
               * Recklessness
               * SneakAttack
               * Backstab
               * AttackHeight
               * ElementalRating
               * PierceRating
               * DualWield
               * TwoHandedCombat
               * Fury
               * Relentless
               * SteadyStrike
               * Ammo
               * LevelScaling;
    }
}
