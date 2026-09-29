using System;
using System.Collections.Generic;
using ACE.Entity.Enum;
using ACE.Server.WorldObjects;

namespace ACE.Server.Entity;

/// <summary>
/// The groups of items the /bank command deposits, sorts and searches by.
/// A side pack whose inscription names one of these collects those items.
/// </summary>
[Flags]
public enum BankCategory
{
    None = 0,
    Salvage = 1 << 0,
    Weapons = 1 << 1,
    Armor = 1 << 2,
    Jewelry = 1 << 3,
    Trinkets = 1 << 4,
    Ammo = 1 << 5,
    Components = 1 << 6,
    Consumables = 1 << 7,
    Gems = 1 << 8,
    Keys = 1 << 9,
    ManaStones = 1 << 10,
    Trophies = 1 << 11,

    Gear = Weapons | Armor | Jewelry | Trinkets,
}

/// <summary>
/// What a side pack's inscription asks of the bank commands.
/// Categories: the pack collects those items. Keep: a pack you carry is left alone by /bank deposit.
/// </summary>
public readonly record struct BankPackTags(BankCategory Categories, bool Keep)
{
    public bool IsEmpty => Categories == BankCategory.None && !Keep;
}

public static class BankCategories
{
    /// <summary>
    /// The order the categories are listed in, and the order /bank sort groups items in.
    /// </summary>
    public static readonly BankCategory[] Singles =
    [
        BankCategory.Weapons,
        BankCategory.Armor,
        BankCategory.Jewelry,
        BankCategory.Trinkets,
        BankCategory.Ammo,
        BankCategory.Salvage,
        BankCategory.Components,
        BankCategory.ManaStones,
        BankCategory.Gems,
        BankCategory.Consumables,
        BankCategory.Keys,
        BankCategory.Trophies,
    ];

    public const string KeepTag = "keep";

    private static readonly Dictionary<string, BankCategory> Words = new(StringComparer.OrdinalIgnoreCase)
    {
        { "salvage", BankCategory.Salvage },
        { "weapon", BankCategory.Weapons },
        { "weapons", BankCategory.Weapons },
        { "armor", BankCategory.Armor },
        { "armors", BankCategory.Armor },
        { "armour", BankCategory.Armor },
        { "armours", BankCategory.Armor },
        { "jewelry", BankCategory.Jewelry },
        { "jewellery", BankCategory.Jewelry },
        { "trinket", BankCategory.Trinkets },
        { "trinkets", BankCategory.Trinkets },
        { "gear", BankCategory.Gear },
        { "ammo", BankCategory.Ammo },
        { "ammunition", BankCategory.Ammo },
        { "arrow", BankCategory.Ammo },
        { "arrows", BankCategory.Ammo },
        { "bolt", BankCategory.Ammo },
        { "bolts", BankCategory.Ammo },
        { "component", BankCategory.Components },
        { "components", BankCategory.Components },
        { "comp", BankCategory.Components },
        { "comps", BankCategory.Components },
        { "consumable", BankCategory.Consumables },
        { "consumables", BankCategory.Consumables },
        { "food", BankCategory.Consumables },
        { "potion", BankCategory.Consumables },
        { "potions", BankCategory.Consumables },
        { "gem", BankCategory.Gems },
        { "gems", BankCategory.Gems },
        { "jewel", BankCategory.Gems },
        { "jewels", BankCategory.Gems },
        { "key", BankCategory.Keys },
        { "keys", BankCategory.Keys },
        { "mana", BankCategory.ManaStones },
        { "manastone", BankCategory.ManaStones },
        { "manastones", BankCategory.ManaStones },
        { "trophy", BankCategory.Trophies },
        { "trophies", BankCategory.Trophies },
    };

    public static BankCategory Classify(WorldObject item)
    {
        return Classify(
            item.WeenieType,
            item.ItemType,
            item.ValidLocations ?? EquipMask.None,
            (item.TrophyQuality ?? 0) > 0
        );
    }

    /// <summary>
    /// True if the item is one /bank should act on. A null filter means "all".
    /// </summary>
    public static bool Matches(WorldObject item, BankCategory? filter)
    {
        return filter == null || (Classify(item) & filter.Value) != 0;
    }

    /// <summary>
    /// The one category an item belongs to, or None for everything else (pyreals, scrolls, quest items, ...).
    /// Earlier checks win: a trophy is only ever a trophy, a trinket is never jewelry, and arrows
    /// (ItemType.MissileWeapon) are ammo, not weapons.
    /// </summary>
    public static BankCategory Classify(WeenieType weenieType, ItemType itemType, EquipMask validLocations, bool isTrophy = false)
    {
        if (weenieType == WeenieType.Salvage)
        {
            return BankCategory.Salvage;
        }

        if (isTrophy)
        {
            return BankCategory.Trophies;
        }

        if (weenieType == WeenieType.SigilTrinket || validLocations == EquipMask.TrinketOne)
        {
            return BankCategory.Trinkets;
        }

        if (weenieType == WeenieType.Ammunition)
        {
            return BankCategory.Ammo;
        }

        if ((itemType & ItemType.WeaponOrCaster) != 0)
        {
            return BankCategory.Weapons;
        }

        // Clothing and cloaks count as armor: they are worn and carry armor on this server.
        if ((itemType & ItemType.Vestements) != 0)
        {
            return BankCategory.Armor;
        }

        if ((itemType & ItemType.Jewelry) != 0)
        {
            return BankCategory.Jewelry;
        }

        if (weenieType == WeenieType.SpellComponent || (itemType & ItemType.SpellComponents) != 0)
        {
            return BankCategory.Components;
        }

        if (weenieType == WeenieType.ManaStone || (itemType & ItemType.ManaStone) != 0)
        {
            return BankCategory.ManaStones;
        }

        // Lockpicks open locks too, so they go with keys.
        if (weenieType == WeenieType.Key || weenieType == WeenieType.Lockpick || (itemType & ItemType.Key) != 0)
        {
            return BankCategory.Keys;
        }

        // Food covers potions and drinks as well; Healer is healing kits.
        if (weenieType == WeenieType.Food || weenieType == WeenieType.Healer)
        {
            return BankCategory.Consumables;
        }

        if (weenieType == WeenieType.Gem || weenieType == WeenieType.Jewel || (itemType & ItemType.Gem) != 0)
        {
            return BankCategory.Gems;
        }

        return BankCategory.None;
    }

    /// <summary>
    /// Reads a category word as typed in a command or an inscription: weapons, armor, gear, ...
    /// </summary>
    public static bool TryParse(string word, out BankCategory category)
    {
        category = BankCategory.None;

        if (string.IsNullOrWhiteSpace(word))
        {
            return false;
        }

        return Words.TryGetValue(word.Trim(), out category);
    }

    /// <summary>
    /// Reads an item type name for /bank search: gem, key, food, manastone, ... A trailing "s" is allowed.
    /// Only letters count, so a number is never taken as a type.
    /// </summary>
    public static bool TryParseItemType(string word, out ItemType itemType)
    {
        itemType = ItemType.None;

        if (string.IsNullOrWhiteSpace(word))
        {
            return false;
        }

        word = word.Trim();

        foreach (var c in word)
        {
            if (!char.IsLetter(c))
            {
                return false;
            }
        }

        if (Enum.TryParse(word, true, out itemType) && itemType != ItemType.None)
        {
            return true;
        }

        if (word.Length > 1 && word.EndsWith('s') && Enum.TryParse(word[..^1], true, out itemType) && itemType != ItemType.None)
        {
            return true;
        }

        itemType = ItemType.None;
        return false;
    }

    /// <summary>
    /// Reads a pack's inscription. Every category word in it is collected; other words are ignored,
    /// so "Weapons + Armor", "weapons, armor" and "my armor & weapons" all mean the same thing.
    /// </summary>
    public static BankPackTags ParseInscription(string inscription)
    {
        if (string.IsNullOrWhiteSpace(inscription))
        {
            return default;
        }

        var categories = BankCategory.None;
        var keep = false;

        foreach (var word in SplitWords(inscription))
        {
            if (word.Equals(KeepTag, StringComparison.OrdinalIgnoreCase))
            {
                keep = true;
            }
            else if (Words.TryGetValue(word, out var category))
            {
                categories |= category;
            }
        }

        return new BankPackTags(categories, keep);
    }

    /// <summary>
    /// How well a pack tagged with packCategories fits an item of itemCategory. Higher is better, 0 is no fit.
    /// A pack tagged for exactly that category beats a pack that also takes other things (weapons beats gear).
    /// </summary>
    public static int PackFit(BankCategory packCategories, BankCategory itemCategory)
    {
        if (itemCategory == BankCategory.None || (packCategories & itemCategory) == 0)
        {
            return 0;
        }

        return packCategories == itemCategory ? 2 : 1;
    }

    /// <summary>
    /// Where a category falls in /bank sort order. Items with no category go last.
    /// </summary>
    public static int SortOrder(BankCategory category)
    {
        var index = Array.IndexOf(Singles, category);
        return index < 0 ? Singles.Length : index;
    }

    public static string Describe(BankCategory category)
    {
        if (category == BankCategory.None)
        {
            return "none";
        }

        if (category == BankCategory.Gear)
        {
            return "gear";
        }

        var names = new List<string>();
        foreach (var single in Singles)
        {
            if ((category & single) != 0)
            {
                names.Add(single.ToString().ToLowerInvariant());
            }
        }

        return string.Join(", ", names);
    }

    public static string Describe(BankPackTags tags)
    {
        if (tags.IsEmpty)
        {
            return "untagged";
        }

        var text = tags.Categories == BankCategory.None ? "" : Describe(tags.Categories);

        if (tags.Keep)
        {
            text = text.Length == 0 ? KeepTag : $"{text}, {KeepTag}";
        }

        return text;
    }

    private static IEnumerable<string> SplitWords(string text)
    {
        var start = -1;

        for (var i = 0; i <= text.Length; i++)
        {
            var isLetter = i < text.Length && char.IsLetter(text[i]);

            if (isLetter && start < 0)
            {
                start = i;
            }
            else if (!isLetter && start >= 0)
            {
                yield return text[start..i];
                start = -1;
            }
        }
    }
}
