using System;
using System.Globalization;
using System.Text;
using ACE.Entity.Enum;
using ACE.Server.Managers;
using ACE.Server.WorldObjects;
using Serilog;

namespace ACE.Server.Entity;

/// <summary>
/// Info logs for spell damage that involves a player (server property log_spell_damage), to check reports of spell
/// damage against the numbers the server worked out. Each hit is one line of key=value pairs, starting "Spell damage:".
/// log_spell_damage_players narrows it to hits cast by or landing on the named characters.
/// </summary>
internal static class SpellDamageLog
{
    private static readonly ILogger _log = Log.ForContext(typeof(SpellDamageLog));

    /// <summary>
    /// Starts a log line for a hit if logging is on and the caster or the target is a player it covers, otherwise null.
    /// Monster-on-monster hits are never logged.
    /// </summary>
    public static SpellDamageLogLine Begin(string kind, WorldObject caster, Creature target, Spell spell)
    {
        if (!PropertyManager.GetBool("log_spell_damage").Item)
        {
            return null;
        }

        var casterPlayer = caster as Player;
        var targetPlayer = target as Player;

        if (casterPlayer == null && targetPlayer == null)
        {
            return null;
        }

        var players = PropertyManager.GetString("log_spell_damage_players").Item;

        if (!CoversPlayers(players, casterPlayer?.Name, targetPlayer?.Name))
        {
            return null;
        }

        var line = new SpellDamageLogLine();

        line.Add("kind", kind);
        AddSpell(line, spell);
        AddCreature(line, "caster", caster);
        AddCreature(line, "target", target);

        return line;
    }

    /// <summary>
    /// Writes the line, if there is one
    /// </summary>
    public static void Write(SpellDamageLogLine line)
    {
        if (line == null)
        {
            return;
        }

        _log.Information("Spell damage: {SpellDamage:l}", line.ToString());
    }

    /// <summary>
    /// Whether log_spell_damage_players (a comma separated list of character names, any case) covers a hit between
    /// these players: an empty list covers every player, otherwise one of them must be listed. Null for a monster.
    /// </summary>
    internal static bool CoversPlayers(string players, string casterPlayerName, string targetPlayerName)
    {
        if (string.IsNullOrWhiteSpace(players))
        {
            return casterPlayerName != null || targetPlayerName != null;
        }

        foreach (var name in players.Split(','))
        {
            var listed = name.Trim();

            if (listed.Length == 0)
            {
                continue;
            }

            if (
                string.Equals(listed, casterPlayerName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(listed, targetPlayerName, StringComparison.OrdinalIgnoreCase)
            )
            {
                return true;
            }
        }

        return false;
    }

    private static void AddSpell(SpellDamageLogLine line, Spell spell)
    {
        line.Add("spell", spell.Name);
        line.Add("spellId", spell.Id);
        line.Add("spellLevel", spell.Level);
        line.Add("school", spell.School);
        line.Add("damageType", spell.DamageType);
    }

    /// <summary>
    /// A creature's name, WCID, GUID, level and health, and the archetype values that set a monster's damage and ward
    /// </summary>
    private static void AddCreature(SpellDamageLogLine line, string prefix, WorldObject wo)
    {
        if (wo == null)
        {
            line.Add(prefix, "none");
            return;
        }

        line.Add(prefix, wo.Name);
        line.Add(prefix + "Wcid", wo.WeenieClassId);
        line.Add(prefix + "Guid", $"0x{wo.Guid.Full:X8}");
        line.Add(prefix + "IsPlayer", wo is Player);

        if (wo is not Creature creature)
        {
            return;
        }

        line.Add(prefix + "Level", creature.Level ?? 0);
        line.Add(prefix + "Health", $"{creature.Health.Current}/{creature.Health.MaxValue}");

        if (creature is Player)
        {
            // Shrouded players are scaled to the monster's level (LevelScaling)
            line.Add(prefix + "Shrouded", creature.EnchantmentManager.HasSpell((uint)SpellId.Shrouded));
        }
        else
        {
            line.Add(prefix + "ArchetypeSpellDamageMod", creature.ArchetypeSpellDamageMultiplier ?? 1.0);
            line.Add(prefix + "ArchetypeMagic", creature.ArchetypeMagic ?? 1.0);
            line.Add(prefix + "ArchetypeLethality", creature.ArchetypeLethality ?? 1.0);
        }
    }
}

/// <summary>
/// One spell hit's log line: key=value pairs, numbers in the invariant culture, text quoted when it has spaces
/// </summary>
internal sealed class SpellDamageLogLine
{
    private readonly StringBuilder _text = new();

    public SpellDamageLogLine Add(string key, object value)
    {
        if (_text.Length > 0)
        {
            _text.Append(' ');
        }

        _text.Append(key).Append('=').Append(Format(value));

        return this;
    }

    private static string Format(object value)
    {
        switch (value)
        {
            case null:
                return "null";
            case float f:
                return f.ToString("0.####", CultureInfo.InvariantCulture);
            case double d:
                return d.ToString("0.####", CultureInfo.InvariantCulture);
            case bool b:
                return b ? "true" : "false";
            case IFormattable formattable:
                return formattable.ToString(null, CultureInfo.InvariantCulture);
        }

        var text = value.ToString() ?? "";

        return text.Contains(' ') || text.Contains('"') || text.Length == 0 ? $"\"{text.Replace("\"", "'")}\"" : text;
    }

    public override string ToString() => _text.ToString();
}
