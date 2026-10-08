namespace ACE.Server.Entity;

/// <summary>
/// Health and stamina returned to a caster for the mana a spell costs
/// (Mana Conversion specialization, Evasive Stance).
/// Calculated with the mana cost, but only granted once that mana is actually spent.
/// </summary>
public readonly record struct ManaCastRefund(int Health, int Stamina)
{
    public static readonly ManaCastRefund None = default;
}
