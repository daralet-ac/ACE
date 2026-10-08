namespace ACE.Server.Entity;

/// <summary>
/// The multipliers that reduce (or increase) an attack's damage after its damage modifiers (see DamageModifiers).
/// Each one is 1.0 when it has no effect.
/// Every field starts at 0 and must be set in DamageEvent.GetMitigation, or the attack deals no damage.
/// </summary>
internal struct MitigationModifiers
{
    public float Armor;
    public float Shield;
    public float Resistance;

    /// <summary>
    /// Damage resistance rating, combined with the critical and PK damage resistance ratings when they apply,
    /// and with RATING - Hardened Defense
    /// </summary>
    public float DamageResistanceRating;

    /// <summary>
    /// 0.5 for a glancing blow (partial evasion), otherwise 1.0
    /// </summary>
    public float Evasion;

    /// <summary>
    /// SPEC BONUS - Physical Defense
    /// </summary>
    public float SpecDefense;

    /// <summary>
    /// COMBAT ABILITY - Provoke
    /// </summary>
    public float Provoke;

    /// <summary>
    /// COMBAT ABILITY - Aegis
    /// </summary>
    public float Aegis;

    /// <summary>
    /// COMBAT ABILITY - Phalanx
    /// </summary>
    public float Phalanx;

    /// <summary>
    /// RATING - Physical or elemental ward (jewels)
    /// </summary>
    public float DamageTypeWard;

    /// <summary>
    /// RATING - Self harm (jewels)
    /// </summary>
    public float SelfHarm;

    /// <summary>
    /// RATING - Red Fury (jewels)
    /// </summary>
    public float RedFury;

    /// <summary>
    /// RATING - Yellow Fury (jewels)
    /// </summary>
    public float YellowFury;

    /// <summary>
    /// Damage reduction for a player fighting several nearby monsters
    /// </summary>
    public float Swarmed;

    public float ImbuedArmorPhysical;
    public float ImbuedArmorCritical;

    /// <summary>
    /// Returns every modifier multiplied together
    /// </summary>
    public readonly float Product()
    {
        return Armor
            * Shield
            * Resistance
            * DamageResistanceRating
            * Evasion
            * SpecDefense
            * Provoke
            * Aegis
            * Phalanx
            * DamageTypeWard
            * SelfHarm
            * RedFury
            * YellowFury
            * Swarmed
            * ImbuedArmorPhysical
            * ImbuedArmorCritical;
    }
}
