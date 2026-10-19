#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;

namespace ACE.MarketApi;

// Shared with the game server (apps/server links this file, MarketListing.cs and MarketRepository.Items.cs), so an item on
// a corpse is described exactly as a market listing is. Keep it free of MySQL and of anything only the API has.

/// <summary>
/// What MarketRepository.Describe reads an item from: the columns of the listings query (MarketRepository.SearchListingsAsync),
/// by name. The API reads them from the query's row; the game server reads the same values from an item snapshot.
/// </summary>
public interface IItemFacts
{
    int? Int(string column);
    uint? UInt(string column);
    long? Long(string column);
    double? Double(string column);
    string? Text(string column);
    DateTime? Date(string column);
}

/// <summary>
/// Where in an item snapshot (MarketListingSnapshot, as JSON) each column of the listings query comes from: the
/// JSON_EXTRACT path in its SELECT, without the "$.". A test checks the query and this table agree.
/// </summary>
public static class ItemColumns
{
    /// <summary>JSON_LENGTH of the spell book, not a value of its own.</summary>
    public const string SpellCount = "SpellCountRaw";

    public static readonly IReadOnlyDictionary<string, string> Paths = new Dictionary<string, string>
    {
        ["AllPropertiesIntRaw"] = "PropertiesInt",
        ["ArmorAttackModRaw"] = "PropertiesFloat.ArmorAttackMod",
        ["ArmorDeceptionModRaw"] = "PropertiesFloat.ArmorDeceptionMod",
        ["ArmorDualWieldModRaw"] = "PropertiesFloat.ArmorDualWieldMod",
        ["ArmorHealthModRaw"] = "PropertiesFloat.ArmorHealthMod",
        ["ArmorHealthRegenModRaw"] = "PropertiesFloat.ArmorHealthRegenMod",
        ["ArmorLevelRaw"] = "PropertiesInt.ArmorLevel",
        ["ArmorLifeMagicModRaw"] = "PropertiesFloat.ArmorLifeMagicMod",
        ["ArmorMagicDefModRaw"] = "PropertiesFloat.ArmorMagicDefMod",
        ["ArmorManaConversionModRaw"] = "PropertiesFloat.ManaConversionMod",
        ["ArmorManaModRaw"] = "PropertiesFloat.ArmorManaMod",
        ["ArmorManaRegenModRaw"] = "PropertiesFloat.ArmorManaRegenMod",
        ["ArmorModVsAcidRaw"] = "PropertiesFloat.ArmorModVsAcid",
        ["ArmorModVsBludgeonRaw"] = "PropertiesFloat.ArmorModVsBludgeon",
        ["ArmorModVsColdRaw"] = "PropertiesFloat.ArmorModVsCold",
        ["ArmorModVsElectricRaw"] = "PropertiesFloat.ArmorModVsElectric",
        ["ArmorModVsFireRaw"] = "PropertiesFloat.ArmorModVsFire",
        ["ArmorModVsNetherRaw"] = "PropertiesFloat.ArmorModVsNether",
        ["ArmorModVsPierceRaw"] = "PropertiesFloat.ArmorModVsPierce",
        ["ArmorModVsSlashRaw"] = "PropertiesFloat.ArmorModVsSlash",
        ["ArmorPerceptionModRaw"] = "PropertiesFloat.ArmorPerceptionMod",
        ["ArmorPhysicalDefModRaw"] = "PropertiesFloat.ArmorPhysicalDefMod",
        ["ArmorRunModRaw"] = "PropertiesFloat.ArmorRunMod",
        ["ArmorShieldModRaw"] = "PropertiesFloat.ArmorShieldMod",
        ["ArmorSlotsRaw"] = "PropertiesInt.ArmorSlots",
        ["ArmorStaminaModRaw"] = "PropertiesFloat.ArmorStaminaMod",
        ["ArmorStaminaRegenModRaw"] = "PropertiesFloat.ArmorStaminaRegenMod",
        ["ArmorThieveryModRaw"] = "PropertiesFloat.ArmorThieveryMod",
        ["ArmorTwohandedCombatModRaw"] = "PropertiesFloat.ArmorTwohandedCombatMod",
        ["ArmorWarMagicModRaw"] = "PropertiesFloat.ArmorWarMagicMod",
        ["ArmorWeightClassRaw"] = "PropertiesInt.ArmorWeightClass",
        ["AttackTypeRaw"] = "PropertiesInt.AttackType",
        ["ClothingPriorityRaw"] = "PropertiesInt.ClothingPriority",
        ["CombatStyleRaw"] = "PropertiesInt.DefaultCombatStyle",
        ["CriticalFrequencyRaw"] = "PropertiesFloat.CriticalFrequency",
        ["CriticalMultiplierRaw"] = "PropertiesFloat.CriticalMultiplier",
        ["DamageModRaw"] = "PropertiesFloat.DamageMod",
        ["DamageRaw"] = "PropertiesInt.Damage",
        ["DamageTypeRaw"] = "PropertiesInt.DamageType",
        ["DamageVarianceRaw"] = "PropertiesFloat.DamageVariance",
        ["DifficultyRaw"] = "PropertiesInt.ItemDifficulty",
        ["ElementalDamageBonusRaw"] = "PropertiesInt.ElementalDamageBonus",
        ["ElementalDamageModRaw"] = "PropertiesFloat.ElementalDamageMod",
        ["GearCritDamageRaw"] = "PropertiesInt.GearCritDamage",
        ["GearCritDamageResistRaw"] = "PropertiesInt.GearCritDamageResist",
        ["GearDamageRaw"] = "PropertiesInt.GearDamage",
        ["GearDamageResistRaw"] = "PropertiesInt.GearDamageResist",
        ["GearHealingBoostRaw"] = "PropertiesInt.GearHealingBoost",
        ["GearMaxHealthRaw"] = "PropertiesInt.GearMaxHealth",
        ["IconDidRaw"] = "PropertiesDID.Icon",
        ["IconOverlayDidRaw"] = "PropertiesDID.IconOverlay",
        ["IgnoreArmorRaw"] = "PropertiesFloat.IgnoreArmor",
        ["IgnoreWardRaw"] = "PropertiesFloat.IgnoreWard",
        ["ImbuedEffectRaw"] = "PropertiesInt.ImbuedEffect",
        ["ItemBaseXpRaw"] = "PropertiesInt64.ItemBaseXp",
        ["ItemDesc"] = "PropertiesString.LongDesc",
        ["ItemMaxLevelRaw"] = "PropertiesInt.ItemMaxLevel",
        ["ItemName"] = "PropertiesString.Name",
        ["ItemTotalXpRaw"] = "PropertiesInt64.ItemTotalXp",
        ["ItemTypeRaw"] = "PropertiesInt.ItemType",
        ["ItemWorkmanshipRaw"] = "PropertiesInt.ItemWorkmanship",
        ["ItemXpStyleRaw"] = "PropertiesInt.ItemXpStyle",
        ["JewelMaterialTypeRaw"] = "PropertiesInt.JewelMaterialType",
        ["JewelQualityRaw"] = "PropertiesInt.JewelQuality",
        ["ManaRaw"] = "PropertiesInt.ItemMaxMana",
        ["MaterialTypeRaw"] = "PropertiesInt.MaterialType",
        ["NoCompsRequiredForMagicSchoolRaw"] = "PropertiesInt.NoCompsRequiredForMagicSchool",
        ["NumItemsInMaterialRaw"] = "PropertiesInt.NumItemsInMaterial",
        ["NumTimesTinkeredRaw"] = "PropertiesInt.NumTimesTinkered",
        ["ResistanceModifierRaw"] = "PropertiesFloat.ResistanceModifier",
        ["ResistanceModifierTypeRaw"] = "PropertiesInt.ResistanceModifierType",
        ["RestorationModRaw"] = "PropertiesFloat.WeaponRestorationSpellsMod",
        ["SigilCooldownRaw"] = "PropertiesFloat.SigilTrinketCooldown",
        ["SigilHealthReservedRaw"] = "PropertiesFloat.SigilTrinketHealthReserved",
        ["SigilManaReservedRaw"] = "PropertiesFloat.SigilTrinketManaReserved",
        ["SigilStaminaReservedRaw"] = "PropertiesFloat.SigilTrinketStaminaReserved",
        ["SigilTriggerChanceRaw"] = "PropertiesFloat.SigilTrinketTriggerChance",
        ["SigilTrinketColorRaw"] = "PropertiesInt.SigilTrinketColor",
        ["SigilTrinketTypeRaw"] = "PropertiesInt.SigilTrinketType",
        ["SocketsRaw"] = "PropertiesInt.JewelSockets",
        ["SpellBookRaw"] = "PropertiesSpellBook",
        ["SpellCountRaw"] = "PropertiesSpellBook",
        ["SpellDidRaw"] = "PropertiesDID.Spell",
        ["SpellcraftRaw"] = "PropertiesInt.ItemSpellcraft",
        ["StaminaCostReductionModRaw"] = "PropertiesFloat.StaminaCostReductionMod",
        ["StructureRaw"] = "PropertiesInt.Structure",
        ["TrophyQualityRaw"] = "PropertiesInt.TrophyQuality",
        ["ValidLocationsRaw"] = "PropertiesInt.ValidLocations",
        ["WardLevelRaw"] = "PropertiesInt.WardLevel",
        ["WeaponLifeMagicModRaw"] = "PropertiesFloat.WeaponLifeMagicMod",
        ["WeaponMagicalDefenseRaw"] = "PropertiesFloat.WeaponMagicalDefense",
        ["WeaponOffenseRaw"] = "PropertiesFloat.WeaponOffense",
        ["WeaponPhysicalDefenseRaw"] = "PropertiesFloat.WeaponPhysicalDefense",
        ["WeaponSkillRaw"] = "PropertiesInt.WeaponSkill",
        ["WeaponTimeRaw"] = "PropertiesInt.WeaponTime",
        ["WeaponTypeRaw"] = "PropertiesInt.WeaponType",
        ["WeaponWarMagicModRaw"] = "PropertiesFloat.WeaponWarMagicMod",
        ["WeenieTypeRaw"] = "WeenieType",
        ["WieldDifficulty2Raw"] = "PropertiesInt.WieldDifficulty2",
        ["WieldDifficulty3Raw"] = "PropertiesInt.WieldDifficulty3",
        ["WieldDifficulty4Raw"] = "PropertiesInt.WieldDifficulty4",
        ["WieldDifficultyRaw"] = "PropertiesInt.WieldDifficulty",
        ["WieldRequirements2Raw"] = "PropertiesInt.WieldRequirements2",
        ["WieldRequirements3Raw"] = "PropertiesInt.WieldRequirements3",
        ["WieldRequirements4Raw"] = "PropertiesInt.WieldRequirements4",
        ["WieldRequirementsRaw"] = "PropertiesInt.WieldRequirements",
        ["WieldSkillType2Raw"] = "PropertiesInt.WieldSkillType2",
        ["WieldSkillType3Raw"] = "PropertiesInt.WieldSkillType3",
        ["WieldSkillType4Raw"] = "PropertiesInt.WieldSkillType4",
        ["WieldSkillTypeRaw"] = "PropertiesInt.WieldSkillType",
    };
}

/// <summary>
/// An item's facts read from its snapshot JSON, the way MySQL reads them for the listings query: JSON_EXTRACT by path (a
/// missing property is null), JSON_UNQUOTE for text, JSON_LENGTH for the spell count, and an object (the spell book, all
/// the ints) as its JSON text. Columns that aren't in the snapshot (Id, ListedPrice, ItemTier...) come from row.
/// </summary>
public sealed class SnapshotItemFacts : IItemFacts
{
    private readonly JsonElement root;
    private readonly IReadOnlyDictionary<string, object?> row;

    public SnapshotItemFacts(string snapshotJson, IReadOnlyDictionary<string, object?>? row = null)
    {
        using var document = JsonDocument.Parse(snapshotJson);
        root = document.RootElement.Clone();
        this.row = row ?? new Dictionary<string, object?>();
    }

    public int? Int(string column) => Number(column, e => e.TryGetInt32(out var i) ? i : Convert.ToInt32(e.GetDouble()), Convert.ToInt32);

    public uint? UInt(string column) => Number(column, e => e.TryGetUInt32(out var u) ? u : Convert.ToUInt32(e.GetDouble()), Convert.ToUInt32);

    public long? Long(string column) => Number(column, e => e.TryGetInt64(out var l) ? l : Convert.ToInt64(e.GetDouble()), Convert.ToInt64);

    public double? Double(string column) => Number(column, e => e.GetDouble(), Convert.ToDouble);

    public string? Text(string column)
    {
        if (row.TryGetValue(column, out var value))
        {
            return value?.ToString();
        }

        var element = Find(column);

        return element?.ValueKind switch
        {
            null or JsonValueKind.Null => null,
            JsonValueKind.String => element.Value.GetString(),
            _ => element.Value.GetRawText(),
        };
    }

    public DateTime? Date(string column)
    {
        return row.TryGetValue(column, out var value) && value is DateTime date ? date : null;
    }

    private T? Number<T>(string column, Func<JsonElement, T> read, Func<object, IFormatProvider, T> convert) where T : struct
    {
        if (row.TryGetValue(column, out var value))
        {
            return value == null ? null : convert(value, CultureInfo.InvariantCulture);
        }

        var element = Find(column);

        return element?.ValueKind switch
        {
            JsonValueKind.Number => read(element.Value),
            JsonValueKind.String => convert(element.Value.GetString()!, CultureInfo.InvariantCulture),
            JsonValueKind.True => convert(1, CultureInfo.InvariantCulture),
            JsonValueKind.False => convert(0, CultureInfo.InvariantCulture),
            _ => null,
        };
    }

    private JsonElement? Find(string column)
    {
        if (column == ItemColumns.SpellCount)
        {
            var book = Walk("PropertiesSpellBook");

            if (book?.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            var count = 0;
            foreach (var _ in book.Value.EnumerateObject())
            {
                count++;
            }

            using var number = JsonDocument.Parse(count.ToString(CultureInfo.InvariantCulture));
            return number.RootElement.Clone();
        }

        return ItemColumns.Paths.TryGetValue(column, out var path) ? Walk(path) : null;
    }

    private JsonElement? Walk(string path)
    {
        var element = root;

        foreach (var part in path.Split('.'))
        {
            if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(part, out element))
            {
                return null;
            }
        }

        return element;
    }
}
