using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using ACE.Entity;
using ACE.Entity.Models;
using ACE.Server.Commands.PlayerCommands;
using ACE.Server.Managers;
using ACE.Server.WorldObjects;
using ACE.Server.WorldObjects.Managers;

namespace ACE.Server.Entity.PlayerUi;

/// <summary>
/// What @playerui fellowship and @playerui targets send. Everything is looked up from the player who asked: their own
/// fellowship, and creatures near them.
/// </summary>
public static class PlayerUiData
{
    /// <summary>The most creatures one @playerui targets request may ask about (the plugin asks about two).</summary>
    public const int MaxTargets = 4;

    /// <summary>
    /// The player's fellowship, or null. With spells, each fellow's buffs and debuffs too (only when the plugin shows them:
    /// a buffed character has dozens, and this is asked for every second).
    /// </summary>
    public static PlayerUiFellowship GetFellowship(Player player, bool spells = false)
    {
        var fellowship = player.Fellowship;

        if (fellowship == null)
        {
            return null;
        }

        var members = fellowship
            .GetFellowshipMembers()
            .Values.OrderByDescending(m => m.Guid.Full == fellowship.FellowshipLeaderGuid)
            .ThenBy(m => m.Name)
            .Select(m => GetFellow(player, m, spells))
            .ToList();

        return new PlayerUiFellowship(
            fellowship.FellowshipName,
            fellowship.FellowshipLeaderGuid,
            fellowship.Open,
            fellowship.ShareXP,
            fellowship.EvenShare,
            fellowship.IsLocked,
            members,
            Fellowship.MaxFellows
        );
    }

    private static PlayerUiFellow GetFellow(Player player, Player fellow, bool spells)
    {
        var mapCoords = GetMapCoords(fellow, out var inBuilding);
        var focus = fellow.GetEquippedCombatFocus();
        List<int> buffs = null;
        List<int> debuffs = null;

        if (spells)
        {
            (buffs, debuffs) = GetSpellIds(fellow);
        }

        return new PlayerUiFellow(
            fellow.Guid.Full,
            fellow.Name,
            fellow.Level ?? 0,
            fellow.Health.Current,
            fellow.Health.MaxValue,
            fellow.Stamina.Current,
            fellow.Stamina.MaxValue,
            fellow.Mana.Current,
            fellow.Mana.MaxValue,
            fellow == player ? 0 : DistanceIfNear(player, fellow),
            PlayerLocationInfo.GetDisplayString(fellow),
            mapCoords?.X,
            mapCoords?.Y,
            inBuilding,
            focus == null ? null : new PlayerUiFocus(focus.Name, focus.IconId, focus.IconUnderlayId ?? 0, focus.IconOverlayId ?? 0),
            buffs,
            debuffs
        );
    }

    /// <summary>
    /// The spell in effect in each category on a fellow, as ids, split into helpful and harmful, the helpful ones only
    /// with a duration: as GetEnchantments, without the rest of what a target's spells say, since the plugin only draws
    /// their icons.
    /// </summary>
    private static (List<int> Buffs, List<int> Debuffs) GetSpellIds(Creature creature)
    {
        var buffs = new List<int>();
        var debuffs = new List<int>();

        var entries = creature.Biota.PropertiesEnchantmentRegistry.GetEnchantmentsTopLayer(
            creature.BiotaDatabaseLock,
            SpellSet.SetSpells
        );

        foreach (var entry in entries ?? [])
        {
            if (entry.SpellId > EnchantmentManager.SpellCategory_Cooldown)
            {
                continue;
            }

            var spell = new Spell(entry.SpellId);

            if (spell.NotFound)
            {
                continue;
            }

            // a buff that never runs out (an item's cantrip, an aura) is always there: only timed ones are worth showing
            if (spell.IsBeneficial && entry.Duration < 0)
            {
                continue;
            }

            (spell.IsBeneficial ? buffs : debuffs).Add(entry.SpellId);
        }

        return (buffs, debuffs);
    }

    public static PlayerUiBank GetBank(Player player)
    {
        var bank = player.GetOpenBank();
        var carried = GetCarriedPacks(player);

        if (bank == null)
        {
            return new PlayerUiBank(false, false, 0, 0, 0, 0, 0, 0, player.BankAutoSort, [], carried);
        }

        if (!bank.BankInventoryLoaded)
        {
            return new PlayerUiBank(true, false, bank.Guid.Full, 0, 0, 0, 0, 0, player.BankAutoSort, [], carried);
        }

        var (used, capacity, _) = player.GetBankSpace(bank);
        var (coins, notes, noteCount) = player.GetBankMoney(bank);

        var packs = player
            .GetBankPacks(bank)
            .Select((pack, index) => BankPack(index + 1, pack))
            .ToList();

        return new PlayerUiBank(true, true, bank.Guid.Full, used, capacity, coins, notes, noteCount, player.BankAutoSort, packs, carried);
    }

    /// <summary>The side packs the player carries, in the order they're in, with their inscriptions as the server has them.</summary>
    private static List<PlayerUiBankPack> GetCarriedPacks(Player player)
    {
        return player.Inventory.Values
            .OfType<Container>()
            .OrderBy(p => p.PlacementPosition ?? int.MaxValue)
            .Select(p => BankPack(0, p))
            .ToList();
    }

    private static PlayerUiBankPack BankPack(int number, Container pack)
    {
        return new PlayerUiBankPack(
            number,
            pack.Guid.Full,
            pack.Name,
            BankCommand.PackTags(pack),
            pack.Inscription,
            pack.Inventory.Values.Count(i => !i.UseBackpackSlot),
            pack.ItemCapacity ?? 0
        );
    }

    public static PlayerUiPosition GetPosition(Player player)
    {
        var mapCoords = GetMapCoords(player, out var inBuilding);
        return new PlayerUiPosition(mapCoords?.X, mapCoords?.Y, inBuilding);
    }

    /// <summary>
    /// Where a player is on the map, or null in a dungeon. The game's own coordinates (GetMapCoords) stop at any indoor cell,
    /// but a building's rooms are part of the landblock outside and share its coordinates, so someone in a shop is placed
    /// where the shop is. Only a true dungeon (Town.HasReliableNearestTown says so, as listplayers does) has no place on the map.
    /// </summary>
    private static Vector2? GetMapCoords(Player player, out bool inBuilding)
    {
        inBuilding = false;

        var outdoors = player.Location.GetMapCoords();

        if (outdoors != null)
        {
            return outdoors;
        }

        if (!Town.HasReliableNearestTown(player))
        {
            return null;
        }

        inBuilding = true;
        return BuildingMapCoords(player.Location.Cell, player.Location.PositionX, player.Location.PositionY);
    }

    /// <summary>
    /// The map coordinates of a spot in a landblock's building, from the landblock (the cell's top two bytes) and the
    /// position in it. The same sums as PositionExtensions.GetMapCoords: 192 meters a landblock, 240 meters a map unit.
    /// </summary>
    public static Vector2 BuildingMapCoords(uint cell, float x, float y)
    {
        var landblockX = (cell >> 24) & 0xFF;
        var landblockY = (cell >> 16) & 0xFF;

        return new Vector2((landblockX * 192 + x) / 240 - 102, (landblockY * 192 + y) / 240 - 102);
    }

    /// <summary>
    /// Reads the guids of a targets request: hex (0x80001234) or decimal, at most MaxTargets of them. Anything else is skipped.
    /// </summary>
    public static List<uint> ParseGuids(IEnumerable<string> words)
    {
        var guids = new List<uint>();

        foreach (var word in words)
        {
            if (guids.Count >= MaxTargets)
            {
                break;
            }

            var text = word.Trim();
            uint guid;

            var parsed = text.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                ? uint.TryParse(text[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out guid)
                : uint.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out guid);

            if (parsed && guid != 0)
            {
                guids.Add(guid);
            }
        }

        return guids;
    }

    public static List<PlayerUiTarget> GetTargets(Player player, List<uint> guids)
    {
        return guids.Select(guid => GetTarget(player, guid)).ToList();
    }

    private static PlayerUiTarget GetTarget(Player player, uint guid)
    {
        var creature = FindNear(player, guid) as Creature;

        if (creature == null || creature.IsDead)
        {
            return Gone(guid);
        }

        // Another player's vitals and spells aren't anyone else's business: the plugin shows the health bar the client has.
        if (creature is Player)
        {
            return new PlayerUiTarget(guid, "player", creature.Name, 0, 0, 0, 0, 0, 0, 0, DistanceIfNear(player, creature),
                null, null, null, null);
        }

        var attackTarget = creature.AttackTarget;

        return new PlayerUiTarget(
            guid,
            "ok",
            creature.Name,
            creature.Level ?? 0,
            creature.Health.Current,
            creature.Health.MaxValue,
            creature.Stamina.Current,
            creature.Stamina.MaxValue,
            creature.Mana.Current,
            creature.Mana.MaxValue,
            DistanceIfNear(player, creature),
            attackTarget?.Guid.Full,
            attackTarget?.Name,
            GetEnchantments(player, creature),
            GetThreat(player, creature)
        );
    }

    private static PlayerUiTarget Gone(uint guid)
    {
        return new PlayerUiTarget(guid, "gone", null, 0, 0, 0, 0, 0, 0, 0, null, null, null, null, null);
    }

    /// <summary>
    /// The spell in effect in each category: the same ones the game applies, so a weaker copy underneath isn't listed.
    /// </summary>
    private static List<PlayerUiEnchantment> GetEnchantments(Player player, Creature creature)
    {
        var entries = creature.Biota.PropertiesEnchantmentRegistry.GetEnchantmentsTopLayer(
            creature.BiotaDatabaseLock,
            SpellSet.SetSpells
        );

        var results = new List<PlayerUiEnchantment>();

        if (entries == null)
        {
            return results;
        }

        foreach (var entry in entries)
        {
            // cooldowns are kept as enchantments too, but aren't spells
            if (entry.SpellId > EnchantmentManager.SpellCategory_Cooldown)
            {
                continue;
            }

            var spell = new Spell(entry.SpellId);

            if (spell.NotFound)
            {
                continue;
            }

            results.Add(
                new PlayerUiEnchantment(
                    entry.SpellId,
                    spell.Name,
                    spell.Level,
                    spell.School.ToString(),
                    spell.IsBeneficial,
                    SecondsLeft(entry),
                    entry.CasterObjectId,
                    CasterName(player, entry.CasterObjectId),
                    entry.CasterObjectId == player.Guid.Full,
                    spell.IconID
                )
            );
        }

        return results.OrderBy(e => e.Beneficial).ThenBy(e => e.Name).ToList();
    }

    /// <summary>
    /// How long an enchantment has left, or -1 for one that never runs out. StartTime counts down from 0 as it runs.
    /// </summary>
    public static double SecondsLeft(PropertiesEnchantmentRegistry entry)
    {
        if (entry.Duration < 0)
        {
            return -1;
        }

        return Math.Max(0, entry.Duration + entry.StartTime);
    }

    private static string CasterName(Player player, uint casterGuid)
    {
        if (casterGuid == 0)
        {
            return null;
        }

        if (casterGuid == player.Guid.Full)
        {
            return player.Name;
        }

        return PlayerManager.GetOnlinePlayer(casterGuid)?.Name ?? FindNear(player, casterGuid)?.Name;
    }

    private static List<PlayerUiThreat> GetThreat(Player player, Creature creature)
    {
        List<KeyValuePair<Creature, int>> table;

        try
        {
            table = creature.GetThreatTable();
        }
        catch (InvalidOperationException)
        {
            // the monster changed its table while it was being copied (it ticks on its own landblock's thread); the plugin
            // asks again in a second
            return null;
        }

        return table
            ?.Select(entry => new PlayerUiThreat(
                entry.Key.Guid.Full,
                entry.Key.Name,
                entry.Value,
                entry.Key == player,
                entry.Key is Player
            ))
            .ToList();
    }

    /// <summary>
    /// An object in the player's landblock or the ones around it, in the player's own instance (an instance is its own copy of
    /// the landblock, so looking it up there is enough).
    /// </summary>
    private static WorldObject FindNear(Player player, uint guid)
    {
        return player.CurrentLandblock?.GetObject(new ObjectGuid(guid));
    }

    private static float? DistanceIfNear(Player player, WorldObject target)
    {
        if (FindNear(player, target.Guid.Full) == null)
        {
            return null;
        }

        return player.GetDistance(target);
    }
}
