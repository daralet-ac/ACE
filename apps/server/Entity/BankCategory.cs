using System;
using System.Collections.Generic;
using ACE.Entity.Enum;
using ACE.Server.Factories;
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
    Animal = 1 << 12,

    // Salvage by what uses it: the tinkering skill for its material (Salvage.TinkeringTarget), or imbuing.
    // A salvage bag is Salvage plus one of these, and a pack tagged for one of them beats a pack tagged "salvage".
    Blacksmithing = 1 << 13,
    Tailoring = 1 << 14,
    Spellcrafting = 1 << 15,
    Woodworking = 1 << 16,
    Jewelcrafting = 1 << 17,
    Imbue = 1 << 18,

    Gear = Weapons | Armor | Jewelry | Trinkets,
    SalvageKinds = Blacksmithing | Tailoring | Spellcrafting | Woodworking | Jewelcrafting | Imbue,
}

/// <summary>
/// What a side pack's inscription asks of the bank commands.
/// Categories: the pack collects those items. Keep: a pack you carry is left alone by /bank deposit and /sort.
/// Deposit: opening the bank offers to deposit everything in a pack you carry.
/// </summary>
public readonly record struct BankPackTags(BankCategory Categories, bool Keep, bool Deposit = false)
{
    public bool IsEmpty => Categories == BankCategory.None && !Keep && !Deposit;
}

public static class BankCategories
{
    /// <summary>
    /// The order the categories are listed in, and the order /bank sort groups items in.
    /// The salvage kinds come last: they are never an item's own category, only a narrower tag for salvage.
    /// </summary>
    public static readonly BankCategory[] Singles =
    [
        BankCategory.Weapons,
        BankCategory.Armor,
        BankCategory.Jewelry,
        BankCategory.Trinkets,
        BankCategory.Ammo,
        BankCategory.Salvage,
        BankCategory.Animal,
        BankCategory.Components,
        BankCategory.ManaStones,
        BankCategory.Gems,
        BankCategory.Consumables,
        BankCategory.Keys,
        BankCategory.Trophies,
        BankCategory.Blacksmithing,
        BankCategory.Tailoring,
        BankCategory.Spellcrafting,
        BankCategory.Woodworking,
        BankCategory.Jewelcrafting,
        BankCategory.Imbue,
    ];

    public const string KeepTag = "keep";
    public const string DepositTag = "deposit";

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
        { "animal", BankCategory.Animal },
        { "animals", BankCategory.Animal },
        { "blacksmithing", BankCategory.Blacksmithing },
        { "blacksmith", BankCategory.Blacksmithing },
        { "tailoring", BankCategory.Tailoring },
        { "tailor", BankCategory.Tailoring },
        { "spellcrafting", BankCategory.Spellcrafting },
        { "spellcraft", BankCategory.Spellcrafting },
        { "woodworking", BankCategory.Woodworking },
        { "woodwork", BankCategory.Woodworking },
        { "jewelcrafting", BankCategory.Jewelcrafting },
        { "jewelcraft", BankCategory.Jewelcrafting },
        { "imbue", BankCategory.Imbue },
        { "imbues", BankCategory.Imbue },
        { "imbuing", BankCategory.Imbue },
    };

    /// <summary>
    /// The one category an item belongs to: what /bank sort groups it under. See Tags for everything a pack tag can match.
    /// </summary>
    public static BankCategory Classify(WorldObject item)
    {
        return Classify(
            item.WeenieType,
            item.ItemType,
            item.ValidLocations ?? EquipMask.None,
            item.IsTrophy,
            IsAnimalPart(item.WeenieClassId)
        );
    }

    /// <summary>
    /// Every category an item answers to: its own (Classify), plus for salvage what uses it (blacksmithing, imbue, ...).
    /// Deposit and sort filters, searches and pack tags all go by these.
    /// </summary>
    public static BankCategory Tags(WorldObject item)
    {
        var category = Classify(item);

        return category == BankCategory.Salvage ? category | SalvageKindOf(item.MaterialType) : category;
    }

    /// <summary>
    /// True if the item is one /bank should act on. A null filter means "all".
    /// </summary>
    public static bool Matches(WorldObject item, BankCategory? filter)
    {
        return filter == null || (Tags(item) & filter.Value) != 0;
    }

    /// <summary>
    /// What uses salvage of this material: imbuing for the imbue gems (whose skill depends on what is imbued), or
    /// the tinkering skill Salvage.TinkeringTarget gives the material. None if the material has neither.
    /// </summary>
    public static BankCategory SalvageKindOf(MaterialType? material)
    {
        if (material is not { } materialType)
        {
            return BankCategory.None;
        }

        if (Salvage.ImbueSalvage.Contains(materialType))
        {
            return BankCategory.Imbue;
        }

        if (!Salvage.TinkeringTarget.TryGetValue(materialType, out var skill))
        {
            return BankCategory.None;
        }

        return skill switch
        {
            Skill.Blacksmithing => BankCategory.Blacksmithing,
            Skill.Tailoring => BankCategory.Tailoring,
            Skill.Spellcrafting => BankCategory.Spellcrafting,
            Skill.Woodworking => BankCategory.Woodworking,
            Skill.Jewelcrafting => BankCategory.Jewelcrafting,
            _ => BankCategory.None,
        };
    }

    /// <summary>
    /// Animal parts are the hides, bones and meat in LootTables.AnimalPartsLootMatrix.
    /// kind: 0 hide, 1 bone, 2 meat. quality: 0 (tattered, cracked, gristly) up to 3 (rugged, pristine, choice).
    /// </summary>
    public static bool TryGetAnimalPart(uint weenieClassId, out int kind, out int quality)
    {
        var matrix = LootTables.AnimalPartsLootMatrix;

        for (kind = 0; kind < matrix.Length; kind++)
        {
            quality = Array.IndexOf(matrix[kind], (int)weenieClassId);
            if (quality >= 0)
            {
                return true;
            }
        }

        kind = -1;
        quality = -1;
        return false;
    }

    public static bool IsAnimalPart(uint weenieClassId)
    {
        return TryGetAnimalPart(weenieClassId, out _, out _);
    }

    /// <summary>
    /// The one category an item belongs to, or None for everything else (pyreals, scrolls, quest items, ...).
    /// Earlier checks win: a trophy is only ever a trophy, animal parts are never food, a trinket is never jewelry,
    /// and arrows (ItemType.MissileWeapon) are ammo, not weapons.
    /// </summary>
    public static BankCategory Classify(
        WeenieType weenieType,
        ItemType itemType,
        EquipMask validLocations,
        bool isTrophy = false,
        bool isAnimalPart = false
    )
    {
        if (weenieType == WeenieType.Salvage)
        {
            return BankCategory.Salvage;
        }

        if (isTrophy)
        {
            return BankCategory.Trophies;
        }

        if (isAnimalPart)
        {
            return BankCategory.Animal;
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
        var deposit = false;

        foreach (var word in SplitWords(inscription))
        {
            if (word.Equals(KeepTag, StringComparison.OrdinalIgnoreCase))
            {
                keep = true;
            }
            else if (word.Equals(DepositTag, StringComparison.OrdinalIgnoreCase))
            {
                deposit = true;
            }
            else if (Words.TryGetValue(word, out var category))
            {
                categories |= category;
            }
        }

        return new BankPackTags(categories, keep, deposit);
    }

    /// <summary>
    /// How well a pack tagged with packCategories fits an item with these Tags. Higher is better, 0 is no fit.
    /// A tag for what uses a salvage bag (imbue, blacksmithing, ...) beats a plain "salvage" tag, and either way
    /// a pack tagged for exactly that beats a pack that also takes other things ("weapons" beats "gear").
    /// 4: exactly its salvage kind. 3: its salvage kind among others. 2: exactly its category. 1: its category among others.
    /// </summary>
    public static int PackFit(BankCategory packCategories, BankCategory itemTags)
    {
        var kind = itemTags & BankCategory.SalvageKinds;
        if (kind != BankCategory.None && (packCategories & kind) != 0)
        {
            return packCategories == kind ? 4 : 3;
        }

        var category = itemTags & ~BankCategory.SalvageKinds;
        if (category != BankCategory.None && (packCategories & category) != 0)
        {
            return packCategories == category ? 2 : 1;
        }

        return 0;
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

        if (tags.Deposit)
        {
            text = text.Length == 0 ? DepositTag : $"{text}, {DepositTag}";
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
