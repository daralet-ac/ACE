using ACE.Server.WorldObjects;

namespace ACE.Server.Entity;

/// <summary>
/// What a spell's projectiles carry from the cast to their impact, where it affects the damage
/// </summary>
/// <param name="Weapon">The weapon or casting item the spell came from (the projectile's ProjectileLauncher)</param>
/// <param name="IsWeaponSpell">The spell is built into the casting item</param>
/// <param name="FromProc">The spell came from a proc, so it can't proc again on impact</param>
/// <param name="LifeProjectileDamage">The damage of a life magic projectile, worked out when it was cast</param>
/// <param name="WeaponSpellcraft">The spellcraft of a proc or Enchanted Blade spell</param>
/// <param name="DamageMultiplier">Multiplier for the spell's damage (emote CastSpell / CastSpellInstant Percent)</param>
/// <param name="ReflectedCaster">COMBAT ABILITY - Reflect: the original caster of a reflected spell</param>
public sealed record SpellProjectileLaunch(
    WorldObject Weapon,
    bool IsWeaponSpell = false,
    bool FromProc = false,
    uint LifeProjectileDamage = 0,
    int? WeaponSpellcraft = null,
    double DamageMultiplier = 1.0,
    Creature ReflectedCaster = null
);
