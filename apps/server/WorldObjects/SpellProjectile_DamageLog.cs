using ACE.Server.Entity;
using PartialEvasion = ACE.Entity.Enum.PartialEvasion;

namespace ACE.Server.WorldObjects;

/// <summary>
/// The spell damage log (server property log_spell_damage) for War and Life Magic projectiles. CalculateDamage starts a
/// line for each hit, DamageTarget adds the ratings and the health taken, and OnCollideObject writes it.
/// </summary>
partial class SpellProjectile
{
    /// <summary>
    /// The log line of the hit being worked out, or null when it isn't logged
    /// </summary>
    private SpellDamageLogLine _damageLog;

    private SpellDamageLogLine BeginDamageLog(WorldObject source, Creature target)
    {
        var kind = Spell.MetaSpellType == ACE.Entity.Enum.SpellType.LifeProjectile ? "lifeProjectile" : "projectile";

        var line = BeginSpellDamageLog(kind, ReflectedCaster ?? source, target, Spell);

        line?.Add("projectileType", SpellType);
        line?.Add("reflected", ReflectedCaster != null);
        line?.Add("fromProc", FromProc);
        line?.Add("strikethrough", Strikethrough);
        line?.Add("weapon", ProjectileLauncher?.Name);
        line?.Add("weaponWcid", ProjectileLauncher?.WeenieClassId ?? 0);

        return line;
    }

    private void LogResist(in SpellHit hit, bool overpower, PartialEvasion partialEvasion)
    {
        LogSpellResist(_damageLog, hit.Source, overpower, partialEvasion);
    }

    private void LogDamage(
        in SpellHit hit,
        float criticalChance,
        bool criticalHit,
        bool critDefended,
        int baseDamage,
        in SpellDamageModifiers damageMods,
        in SpellMitigationModifiers mitigation,
        float damageBeforeMitigation,
        float damageAfterMitigation,
        float calculatedDamage
    )
    {
        var line = _damageLog;
        if (line == null)
        {
            return;
        }

        line.Add("attackSkill", hit.AttackSkill?.Current ?? 0);
        line.Add("critChance", criticalChance);
        line.Add("crit", criticalHit);
        line.Add("critDefended", critDefended);

        if (Spell.MetaSpellType == ACE.Entity.Enum.SpellType.LifeProjectile)
        {
            line.Add("lifeProjectileDamage", LifeProjectileDamage);
            line.Add("damageRatio", Spell.DamageRatio);
        }
        else
        {
            line.Add("baseDamageRange", $"{Spell.MinDamage}-{Spell.MaxDamage}");
        }

        line.Add("baseDamage", baseDamage);

        line.Add("critMod", damageMods.Critical);
        line.Add("attributeMod", damageMods.Attribute);
        line.Add("elementalMod", damageMods.Elemental);
        line.Add("slayerMod", damageMods.Slayer);
        line.Add("overloadMod", damageMods.Overload);
        line.Add("batteryMod", damageMods.Battery);
        line.Add("jewelElementalistMod", damageMods.JewelElementalist);
        line.Add("jewelElementalMod", damageMods.JewelElemental);
        line.Add("jewelSelfHarmMod", damageMods.JewelSelfHarm);
        line.Add("jewelRedFuryMod", damageMods.JewelRedFury);
        line.Add("jewelBlueFuryMod", damageMods.JewelBlueFury);
        line.Add("strikethroughMod", damageMods.StrikethroughPenalty);
        line.Add("archetypeMod", damageMods.Archetype);
        line.Add("levelScalingMod", damageMods.LevelScaling);
        line.Add("damageMultiplier", damageMods.DamageMultiplier);
        line.Add("spellcraftMod", damageMods.Spellcraft);
        line.Add("landblockMod", damageMods.LandblockScaling);
        line.Add("backstabMod", damageMods.Backstab);
        line.Add("damageBeforeMitigation", damageBeforeMitigation);

        line.Add("absorbMod", mitigation.Absorb);
        line.Add("resistanceMod", mitigation.Resistance);
        line.Add("resistedMod", mitigation.Resisted);
        line.Add("specDefenseMod", mitigation.SpecDefense);
        line.Add("damageTypeWardMod", mitigation.DamageTypeWard);
        line.Add("damageAfterMitigation", damageAfterMitigation);

        // the player/monster spell damage multipliers, armor imbues and Phalanx
        line.Add("postMitigationMod", damageAfterMitigation > 0 ? calculatedDamage / damageAfterMitigation : 1.0f);
        line.Add("calculatedDamage", calculatedDamage);
    }

    /// <summary>
    /// The ratings and the health the hit took, from DamageTarget
    /// </summary>
    private void LogAppliedDamage(Creature target, in SpellRatingModifiers ratings, float damage, uint amount, float percent)
    {
        var line = _damageLog;
        if (line == null)
        {
            return;
        }

        line.Add("damageRatingMod", ratings.DamageRating);
        line.Add("damageResistRatingMod", ratings.DamageResistRating);
        line.Add("damageAfterRatings", damage);
        line.Add("healthTaken", amount);
        line.Add("percentOfMaxHealth", percent);
        line.Add("targetHealthAfter", $"{target.Health.Current}/{target.Health.MaxValue}");
        line.Add("outcome", target.IsAlive ? "hit" : "killed");
    }

    /// <summary>
    /// Writes the hit's log line, if it has one
    /// </summary>
    private void WriteDamageLog()
    {
        SpellDamageLog.Write(_damageLog);
        _damageLog = null;
    }
}
