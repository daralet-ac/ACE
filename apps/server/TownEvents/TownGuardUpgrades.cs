using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Entity;
using ACE.Server.Entity.Actions;
using ACE.Server.Factories;
using WeenieClassName = ACE.Server.Factories.Enum.WeenieClassName;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.WorldObjects;

namespace ACE.Server.TownEvents;

/// <summary>
/// A town's guards are dressed to match its supply level (TownSupply). Each guard carries two armor sets (world-db's
/// town-guard create lists): chainmail, and the town's own kit. At level 0 it wears the chainmail; from level 1 the kit,
/// recoloured bronze, then bright silver, then gold as the levels climb. Its weapon goes from black to bronze, silver and
/// gold the same way. Only the kit's colour changes past level 1, so a guard never needs more than the two sets. From
/// level 7 its weapon gains Defender, then Heart Seeker, Blood Drinker and, at 10, Biting Strike.
///
/// Guards come from generators and aren't saved, so a respawned guard, or one on a landblock that just loaded, is dressed
/// by the regular sweep (TownEventManager), and a level change dresses the town's guards at once.
/// </summary>
public static class TownGuardUpgrades
{
    // the colours, as decided in the design doc for platemail, scalemail and yoroi. A clothing table without one of these
    // palettes falls back to its first, so an odd kit piece shows its own default colour rather than nothing.
    private static readonly (PaletteTemplate Palette, double Shade) Bronze = (PaletteTemplate.Copper, 0.49);
    private static readonly (PaletteTemplate Palette, double Shade) BrightSilver = (PaletteTemplate.Silver, 0.943);
    private static readonly (PaletteTemplate Palette, double Shade) Gold = (PaletteTemplate.Gold, 0.49);

    private const double WeaponShade = 0.5;

    private static readonly HashSet<uint> ChainmailWcids = new()
    {
        (uint)WeenieClassName.basinetchainmail,
        (uint)WeenieClassName.gauntletschainmail,
        (uint)WeenieClassName.hauberkchainmail,
        (uint)WeenieClassName.leggingschainmail,
        (uint)WeenieClassName.shirtchainmail,
        (uint)WeenieClassName.sleeveschainmail,
        (uint)WeenieClassName.tassetschainmail,
        (uint)WeenieClassName.bracerschainmail,
        (uint)WeenieClassName.breastplatechainmail,
        (uint)WeenieClassName.girthchainmail,
        (uint)WeenieClassName.pauldronschainmail,
        (uint)WeenieClassName.greaveschainmail,
    };

    /// <summary>
    /// The kit's colour at a supply level: none at 0 (the chainmail is worn), bronze from 1, bright silver from 3, gold from 5
    /// </summary>
    public static (PaletteTemplate Palette, double Shade)? GetArmorColor(int level) =>
        level switch
        {
            >= 5 => Gold,
            >= 3 => BrightSilver,
            >= 1 => Bronze,
            _ => null
        };

    /// <summary>
    /// What every worn armor piece is worth in chainmail, at supply level 0: 50 at tier 0, then the base Covenant armor
    /// level of the tier's loot (100 at tier 1, doubling each tier after). Gold reaches twice this, the top of the tier's
    /// loot range.
    /// </summary>
    public static int GetChainmailArmorLevel(uint tier) => tier == 0 ? 50 : 100 << (int)Math.Min(tier - 1, 7u);

    /// <summary>
    /// How far up the tier's range the guard's armor is: chainmail 1, then a third of the way to double for each colour,
    /// so bronze 1.33, bright silver 1.67 and gold 2. The guard's ward rises the same way.
    /// </summary>
    public static double GetArmorMultiplier(int level) =>
        level switch
        {
            >= 5 => 2.0,
            >= 3 => 5.0 / 3,
            >= 1 => 4.0 / 3,
            _ => 1.0
        };

    public static int GetArmorLevel(int level, uint tier) => (int)Math.Round(GetChainmailArmorLevel(tier) * GetArmorMultiplier(level));

    /// <summary>
    /// The weapon's damage multiplier for its colour: black 1, bronze 1.15, silver 1.3, gold 1.5
    /// </summary>
    public static double GetWeaponDamageMod(int level) =>
        level switch
        {
            >= 6 => 1.5,
            >= 4 => 1.3,
            >= 2 => 1.15,
            _ => 1.0
        };

    /// <summary>
    /// The weapon's colour at a supply level: black at 0 and 1, bronze from 2, silver from 4, gold from 6
    /// </summary>
    public static PaletteTemplate GetWeaponPalette(int level) =>
        level switch
        {
            >= 6 => PaletteTemplate.Gold,
            >= 4 => PaletteTemplate.Silver,
            >= 2 => PaletteTemplate.Brown,
            _ => PaletteTemplate.Black
        };

    // --- weapon buffs, levels 7 to 10, each keeping the ones below. Swift Killer was left out: a monster's attack timing
    // comes from its animation, not its weapon's speed, so it would do nothing on a guard.

    public const int DefenderLevel = 7;
    public const int HeartSeekerLevel = 8;
    public const int BloodDrinkerLevel = 9;
    public const int BitingStrikeLevel = 10;

    // Biting Strike isn't a spell: it's the weapon's critical hit chance (the default is 10%)
    public const double BitingStrikeFrequency = 0.20;

    private static readonly SpellId[] DefenderSpells =
    {
        SpellId.DefenderSelf1, SpellId.DefenderSelf2, SpellId.DefenderSelf3, SpellId.DefenderSelf4,
        SpellId.DefenderSelf5, SpellId.DefenderSelf6, SpellId.DefenderSelf7, SpellId.DefenderSelf8,
    };

    private static readonly SpellId[] HeartSeekerSpells =
    {
        SpellId.HeartSeekerSelf1, SpellId.HeartSeekerSelf2, SpellId.HeartSeekerSelf3, SpellId.HeartSeekerSelf4,
        SpellId.HeartSeekerSelf5, SpellId.HeartSeekerSelf6, SpellId.HeartSeekerSelf7, SpellId.HeartSeekerSelf8,
    };

    private static readonly SpellId[] BloodDrinkerSpells =
    {
        SpellId.BloodDrinkerSelf1, SpellId.BloodDrinkerSelf2, SpellId.BloodDrinkerSelf3, SpellId.BloodDrinkerSelf4,
        SpellId.BloodDrinkerSelf5, SpellId.BloodDrinkerSelf6, SpellId.BloodDrinkerSelf7, SpellId.BloodDrinkerSelf8,
    };

    /// <summary>
    /// The level of the weapon spells (I to VIII) for a town's tier, so the buff keeps up with the town's own attackers:
    /// I at tier 0 (Holtburg), up to VII at tier 6
    /// </summary>
    public static int GetSpellLevel(uint tier) => (int)Math.Clamp(tier + 1, 1, 8);

    /// <summary>
    /// The weapon spells a guard's weapon carries at a supply level, for a town of this tier
    /// </summary>
    public static List<SpellId> GetWeaponSpells(int level, uint tier)
    {
        var index = GetSpellLevel(tier) - 1;
        var spells = new List<SpellId>();

        if (level >= DefenderLevel)
        {
            spells.Add(DefenderSpells[index]);
        }
        if (level >= HeartSeekerLevel)
        {
            spells.Add(HeartSeekerSpells[index]);
        }
        if (level >= BloodDrinkerLevel)
        {
            spells.Add(BloodDrinkerSpells[index]);
        }

        return spells;
    }

    private static readonly SpellId[] AllWeaponSpells = DefenderSpells.Concat(HeartSeekerSpells).Concat(BloodDrinkerSpells).ToArray();

    // a weapon's own critical chance from world-db, put back when the town drops below Biting Strike's level
    private static readonly ConditionalWeakTable<WorldObject, Tuple<double?>> OriginalCritical = new();

    // a guard's ward from the archetype system, which the armor multiplier scales
    private static readonly ConditionalWeakTable<Creature, Tuple<int?>> OriginalWard = new();

    // the level each guard was last dressed for, so the sweep only touches guards that need it. Held weakly, so a guard
    // that's gone takes its entry with it.
    private class Dressed
    {
        public int Level;
    }

    private static readonly ConditionalWeakTable<Creature, Dressed> DressedFor = new();

    // a kit piece's colour from world-db, for a guard with no chainmail to fall back on at level 0
    private static readonly ConditionalWeakTable<WorldObject, Tuple<int?, double?>> OriginalColor = new();

    /// <summary>
    /// Dresses every loaded guard of the town that isn't dressed for its supply level yet. Call on the world thread; the
    /// dressing itself runs on each guard's landblock.
    /// </summary>
    public static void Refresh(string town, bool force = false)
    {
        var level = TownSupply.GetLevel(town);
        var tier = Town.GetTownTier(town);

        foreach (var guard in TownGeography.FindGuards(town))
        {
            if (!force && DressedFor.TryGetValue(guard, out var dressed) && dressed.Level == level)
            {
                continue;
            }

            var chain = new ActionChain();
            chain.AddAction(guard, () => Dress(guard, level, tier));
            chain.EnqueueChain();
        }
    }

    private static bool IsArmor(WorldObject item) =>
        item.WeenieType == WeenieType.Clothing && ((uint)(item.ClothingPriority ?? 0) & (uint)CoverageMaskHelper.Outerwear) != 0;

    private static bool IsWeapon(WorldObject item) =>
        item is MeleeWeapon or MissileLauncher or Caster;

    /// <summary>
    /// Puts back the armor in the guard's create list that it isn't carrying. When a creature spawns it dresses in its
    /// best armor (Monster_Inventory.SelectWieldedArmor), and a piece whose slot is already taken is dropped rather than
    /// kept in its inventory, so a guard spawned in its kit has lost its chainmail, and the other way round.
    /// </summary>
    private static void RestoreSpareSet(Creature guard)
    {
        var createList = guard.Biota.PropertiesCreateList;
        if (createList == null)
        {
            return;
        }

        var carried = guard.EquippedObjects.Values.Concat(guard.Inventory.Values).Select(i => i.WeenieClassId).ToList();

        foreach (var entry in createList.Where(e => (e.DestinationType & DestinationType.Wield) != 0 && e.DestinationType != DestinationType.WieldTreasure))
        {
            // one of each piece: a wcid listed twice needs two
            if (carried.Remove(entry.WeenieClassId))
            {
                continue;
            }

            var piece = WorldObjectFactory.CreateNewWorldObject(entry, guard.Tier ?? 1);
            if (piece == null)
            {
                continue;
            }

            if (!IsArmor(piece) || !guard.TryAddToInventory(piece))
            {
                piece.Destroy();
            }
        }
    }

    /// <summary>
    /// Wears the right armor set for the level, recolours the kit and the weapons, and shows the change to everyone nearby
    /// </summary>
    private static void Dress(Creature guard, int level, uint tier)
    {
        if (guard.IsDestroyed || guard.IsDead)
        {
            return;
        }

        RestoreSpareSet(guard);

        var items = guard.EquippedObjects.Values.Concat(guard.Inventory.Values).ToList();

        var armor = items.Where(IsArmor).ToList();
        var chainmail = armor.Where(a => ChainmailWcids.Contains(a.WeenieClassId)).ToList();
        var kit = armor.Except(chainmail).ToList();

        // a guard whose create list hasn't got both sets yet keeps what it has
        var wearKit = kit.Count > 0 && (level >= 1 || chainmail.Count == 0);

        // in chainmail, only the kit pieces it replaces come off: chainmail has no boots, so the kit's stay on
        var chainSlots = chainmail.Aggregate(0, (slots, piece) => slots | (piece.ValidLocations is { } v ? (int)v : 0));
        var kitClashing = kit.Where(k => ((int)(k.ValidLocations ?? 0) & chainSlots) != 0).ToList();

        var wear = wearKit ? kit : chainmail.Concat(kit.Except(kitClashing)).ToList();
        var stow = wearKit ? chainmail : kitClashing;

        var changed = false;

        foreach (var item in stow.Where(i => guard.EquippedObjects.ContainsKey(i.Guid)))
        {
            if (guard.TryDequipObject(item.Guid, out var removed, out _))
            {
                guard.TryAddToInventory(removed);
                changed = true;
            }
        }

        foreach (var item in wear.Where(i => !guard.EquippedObjects.ContainsKey(i.Guid)))
        {
            if (!guard.TryRemoveFromInventory(item.Guid))
            {
                continue;
            }

            if (guard.TryWieldObject(item, (EquipMask)(item.ValidLocations ?? 0)))
            {
                changed = true;
            }
            else
            {
                guard.TryAddToInventory(item);
            }
        }

        var kitColor = GetArmorColor(level);
        foreach (var item in kit)
        {
            var original = OriginalColor.GetValue(item, i => Tuple.Create(i.PaletteTemplate, i.Shade));

            var palette = kitColor.HasValue ? (int)kitColor.Value.Palette : original.Item1;
            var shade = kitColor.HasValue ? kitColor.Value.Shade : original.Item2;

            changed |= Recolor(item, palette, shade);
        }

        ArmorUp(guard, level, tier);

        var weaponPalette = (int)GetWeaponPalette(level);
        foreach (var weapon in items.Where(IsWeapon))
        {
            if (Recolor(weapon, weaponPalette, WeaponShade) && guard.EquippedObjects.ContainsKey(weapon.Guid))
            {
                // a held weapon is its own object, so it's redrawn on its own
                guard.EnqueueBroadcast(new GameMessageUpdateObject(weapon));
            }

            if (guard.EquippedObjects.ContainsKey(weapon.Guid))
            {
                BuffWeapon(guard, weapon, level, tier);
            }
        }

        if (changed)
        {
            guard.EnqueueBroadcast(new GameMessageObjDescEvent(guard));
        }

        DressedFor.AddOrUpdate(guard, new Dressed { Level = level });
    }

    /// <summary>
    /// Gives the held weapon the spells for the level and takes off any it shouldn't have (a lower level, or a spell level
    /// from another tier), and sets Biting Strike's critical chance or puts the weapon's own back
    /// </summary>
    private static void BuffWeapon(Creature guard, WorldObject weapon, int level, uint tier)
    {
        var wanted = GetWeaponSpells(level, tier);

        foreach (var spell in AllWeaponSpells)
        {
            var has = weapon.EnchantmentManager.GetEnchantment((uint)spell, weapon.Guid.Full) != null;

            if (wanted.Contains(spell) && !has)
            {
                guard.CreateItemSpell(weapon, (uint)spell);
            }
            else if (!wanted.Contains(spell) && has)
            {
                guard.RemoveItemSpell(weapon, (uint)spell, true);
            }
        }

        var original = OriginalCritical.GetValue(weapon, w => Tuple.Create(w.CriticalFrequency));
        weapon.CriticalFrequency = level >= BitingStrikeLevel ? BitingStrikeFrequency : original.Item1;

        // the archetype system only sets the weapon's base damage, so the colour's bonus rides on its damage multiplier
        var damageMod = GetWeaponDamageMod(level);
        if (damageMod == 1.0)
        {
            weapon.RemoveProperty(PropertyFloat.DamageMod);
        }
        else
        {
            weapon.SetProperty(PropertyFloat.DamageMod, damageMod);
        }
    }

    /// <summary>
    /// Makes the guard's worn armor count, at the armor level for its town's tier and supply level, and raises its ward to
    /// match. A monster's armor normally comes only from its archetype body armor; CreatureArmorEffectsDamageReduction
    /// counts its worn pieces too, and ArmorTakesHigher makes each body part take the higher of the two rather than both.
    /// Its ward is set by the archetype system and ignores worn pieces, so it's scaled instead.
    /// </summary>
    private static void ArmorUp(Creature guard, int level, uint tier)
    {
        guard.CreatureArmorEffectsDamageReduction = true;
        guard.ArmorTakesHigher = true;

        var armorLevel = GetArmorLevel(level, tier);
        foreach (var piece in guard.EquippedObjects.Values.Where(IsArmor))
        {
            piece.SetProperty(PropertyInt.ArmorLevel, armorLevel);
        }

        var originalWard = OriginalWard.GetValue(guard, g => Tuple.Create(g.WardLevel));
        if (originalWard.Item1.HasValue)
        {
            guard.WardLevel = (int)Math.Round(originalWard.Item1.Value * GetArmorMultiplier(level));
        }
    }

    private static bool Recolor(WorldObject item, int? palette, double? shade)
    {
        if (item.PaletteTemplate == palette && item.Shade == shade)
        {
            return false;
        }

        item.PaletteTemplate = palette;
        item.Shade = shade;
        return true;
    }
}
