using ACE.Entity.Enum;
using ACE.Server.Entity;

namespace ACE.Server.WorldObjects;

partial class WorldObject
{
    /// <summary>
    /// Starts a spell damage log line (SpellDamageLog) for a War or Life Magic hit, or null if this hit isn't logged
    /// </summary>
    internal static SpellDamageLogLine BeginSpellDamageLog(string kind, WorldObject caster, Creature target, Spell spell)
    {
        if (spell.School is not (MagicSchool.WarMagic or MagicSchool.LifeMagic))
        {
            return null;
        }

        return SpellDamageLog.Begin(kind, caster, target, spell);
    }

    /// <summary>
    /// The resist roll for the spell damage log: the result, and the caster's magic skill against the target's magic
    /// defense from the caster's last resist check (LastSpellResistCheck)
    /// </summary>
    internal static void LogSpellResist(
        SpellDamageLogLine line,
        WorldObject resistChecker,
        bool overpower,
        PartialEvasion partialEvasion
    )
    {
        if (line == null)
        {
            return;
        }

        line.Add("resist", partialEvasion);
        line.Add("overpower", overpower);

        if (overpower)
        {
            return;
        }

        var check = resistChecker?.LastSpellResistCheck;
        if (check == null)
        {
            line.Add("resistable", false);
            return;
        }

        line.Add("magicSkill", check.Value.MagicSkill);
        line.Add("magicDefense", check.Value.MagicDefense);
        line.Add("resistChance", check.Value.ResistChance);
    }

    /// <summary>
    /// The target's ward for the spell damage log: its base ward and buffs, the average ward for its level that ward
    /// penetration and monster spell damage are sized against, the caster's penetration, and the resulting ward mod
    /// </summary>
    internal static void LogSpellWard(
        SpellDamageLogLine line,
        Creature target,
        WardPenetration wardPenetration,
        float wardMod
    )
    {
        if (line == null || target == null)
        {
            return;
        }

        line.Add("targetWard", target.GetWardLevel());
        line.Add("targetWardBuffAdd", target.EnchantmentManager.GetWardAdditiveMod());
        line.Add("targetWardBuffMult", target.EnchantmentManager.GetWardMultiplicativeMod());
        line.Add("referenceWard", ArmorPenetration.GetReferenceWard(target));
        line.Add("wardPenetration", wardPenetration.Penetration);
        line.Add("wardPenRatingMod", wardPenetration.RatingMod);
        line.Add("wardMod", wardMod);
    }

    /// <summary>
    /// A Life Magic harm on another creature: a boost spell that only lowers the vital
    /// </summary>
    private static bool IsHarm(Spell spell)
    {
        return spell.MetaSpellType == SpellType.Boost && System.Math.Max(spell.Boost, spell.MaxBoost) <= 0;
    }

    /// <summary>
    /// Starts the spell damage log line for a harm (HandleCastSpell_Boost), after its damage roll
    /// </summary>
    private SpellDamageLogLine BeginHarmDamageLog(in BoostCast cast, int rolledBoost)
    {
        if (cast.Target == null || cast.Target == this || !IsHarm(cast.Spell))
        {
            return null;
        }

        var line = BeginSpellDamageLog("harm", cast.DamageSource ?? this, cast.Target, cast.Spell);
        if (line == null)
        {
            return null;
        }

        line.Add("vital", cast.Spell.VitalDamageType);
        line.Add("reflected", cast.ReflectedCaster != null);
        line.Add("fromProc", cast.FromProc);
        line.Add("weapon", cast.Weapon?.Name);
        line.Add("weaponWcid", cast.Weapon?.WeenieClassId ?? 0);

        LogSpellResist(line, this, false, cast.PartialEvasion);

        var minimum = System.Math.Min(-cast.Spell.Boost, -cast.Spell.MaxBoost);
        var maximum = System.Math.Max(-cast.Spell.Boost, -cast.Spell.MaxBoost);
        line.Add("baseDamageRange", $"{minimum}-{maximum}");
        line.Add("lifeDamageMod", GetLifeDamageMod(cast.Weapon, cast.DamageSource, cast.Target));
        line.Add("rolledDamage", -rolledBoost);

        return line;
    }

    /// <summary>
    /// Finishes and writes a harm's spell damage log line: the vital it took
    /// </summary>
    private static void LogHarmApplied(
        SpellDamageLogLine line,
        in BoostCast cast,
        int tryBoost,
        int boost,
        string vital,
        float sigilDamageReductionMod
    )
    {
        if (line == null)
        {
            return;
        }

        var target = cast.Target;
        var targetVital = cast.Spell.VitalDamageType switch
        {
            DamageType.Mana => target.Mana,
            DamageType.Stamina => target.Stamina,
            _ => target.Health,
        };

        line.Add("sigilAbsorptionMod", sigilDamageReductionMod);
        line.Add("finalDamage", -tryBoost);
        line.Add("vitalTaken", -boost);
        line.Add("vitalTakenFrom", vital);
        line.Add("percentOfMaxVital", targetVital.MaxValue > 0 ? (float)-boost / targetVital.MaxValue : 0.0f);
        line.Add("targetHealthAfter", $"{target.Health.Current}/{target.Health.MaxValue}");
        line.Add("outcome", target.IsAlive ? "hit" : "killed");

        SpellDamageLog.Write(line);
    }

    /// <summary>
    /// Logs a harm on another creature that was fully resisted (TryResistSpell), which never reaches HandleCastSpell_Boost
    /// </summary>
    private static void LogResistedHarm(
        WorldObject caster,
        WorldObject resistChecker,
        Creature target,
        Spell spell,
        PartialEvasion partialEvasion
    )
    {
        if (target == null || target == caster || !IsHarm(spell))
        {
            return;
        }

        var line = BeginSpellDamageLog("harm", caster, target, spell);
        if (line == null)
        {
            return;
        }

        LogSpellResist(line, resistChecker, false, partialEvasion);
        line.Add("outcome", "resisted");

        SpellDamageLog.Write(line);
    }
}
