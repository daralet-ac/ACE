using MySqlConnector;

namespace ACE.MarketApi;

public class MarketSearchRequest
{
    public string? Keyword { get; set; }
    public string? SpellName { get; set; }
    public int? MinPrice { get; set; }
    public int? MaxPrice { get; set; }
    public int? MinTier { get; set; }
    public int? MaxTier { get; set; }
    public string? ItemCategory { get; set; }
    public string? WeaponClass { get; set; }
    public string? ArmorWeightClass { get; set; }
    public List<string>? Coverage { get; set; }
    public string? SigilTrinketType { get; set; }
    public string? SigilEffect { get; set; }
    public string? JewelrySlot { get; set; }
    public string? SalvageTargetType { get; set; }
    public string? SalvageMaterial { get; set; }
    public string? ConsumableType { get; set; }
    public string? MiscType { get; set; }
    public string? TrophyType { get; set; }
    public int? MinTrophyQuality { get; set; }
    public int? MaxTrophyQuality { get; set; }
    public string? GemMaterial { get; set; }
    public bool? CarvedJewelOnly { get; set; }
    public int? MinGemQuality { get; set; }
    public int? MaxGemQuality { get; set; }
    public string? ScrollSchool { get; set; }
    public string? ScrollSpellName { get; set; }
    public double? MinWorkmanship { get; set; }
    public double? MaxWorkmanship { get; set; }
    public int? MinUnits { get; set; }
    public int? MaxUnits { get; set; }
    public double? MinPricePerUnit { get; set; }
    public double? MaxPricePerUnit { get; set; }
    public int? SigilMinLevel { get; set; }
    public int? SigilMaxLevel { get; set; }
    public double? MinProcChance { get; set; }
    public double? MaxProcChance { get; set; }
    public double? MinCooldown { get; set; }
    public double? MaxCooldown { get; set; }
    public double? MinManaReserved { get; set; }
    public double? MaxManaReserved { get; set; }
    public double? MinStaminaReserved { get; set; }
    public double? MaxStaminaReserved { get; set; }
    public double? MinHealthReserved { get; set; }
    public double? MaxHealthReserved { get; set; }
    public double? MinDps { get; set; }
    public double? MaxDps { get; set; }
    public int? MinBaseDamage { get; set; }
    public int? MaxBaseDamage { get; set; }
    public double? MinDamageVariance { get; set; }
    public double? MaxDamageVariance { get; set; }
    public double? MinSpeed { get; set; }
    public double? MaxSpeed { get; set; }
    public double? MinAttackMod { get; set; }
    public double? MaxAttackMod { get; set; }
    public double? MinPhysDefMod { get; set; }
    public double? MaxPhysDefMod { get; set; }
    public double? MinMagicDefMod { get; set; }
    public double? MaxMagicDefMod { get; set; }
    public int? MinSpellcraft { get; set; }
    public int? MaxSpellcraft { get; set; }
    public int? MinDifficulty { get; set; }
    public int? MaxDifficulty { get; set; }
    public double? MinWarMagicMod { get; set; }
    public double? MaxWarMagicMod { get; set; }
    public double? MinLifeMagicMod { get; set; }
    public double? MaxLifeMagicMod { get; set; }
    public int? MinArmorLevel { get; set; }
    public int? MaxArmorLevel { get; set; }
    public int? MinWardLevel { get; set; }
    public int? MaxWardLevel { get; set; }
    public double? MinArmorModVsSlash { get; set; }
    public double? MaxArmorModVsSlash { get; set; }
    public double? MinArmorModVsPierce { get; set; }
    public double? MaxArmorModVsPierce { get; set; }
    public double? MinArmorModVsBludgeon { get; set; }
    public double? MaxArmorModVsBludgeon { get; set; }
    public double? MinArmorModVsCold { get; set; }
    public double? MaxArmorModVsCold { get; set; }
    public double? MinArmorModVsFire { get; set; }
    public double? MaxArmorModVsFire { get; set; }
    public double? MinArmorModVsAcid { get; set; }
    public double? MaxArmorModVsAcid { get; set; }
    public double? MinArmorModVsElectric { get; set; }
    public double? MaxArmorModVsElectric { get; set; }
    public double? MinArmorWarMagicMod { get; set; }
    public double? MaxArmorWarMagicMod { get; set; }
    public double? MinArmorLifeMagicMod { get; set; }
    public double? MaxArmorLifeMagicMod { get; set; }
    public double? MinArmorManaRegenMod { get; set; }
    public double? MaxArmorManaRegenMod { get; set; }
    public double? MinArmorManaConversionMod { get; set; }
    public double? MaxArmorManaConversionMod { get; set; }
    public double? MinArmorAttackMod { get; set; }
    public double? MaxArmorAttackMod { get; set; }
    public double? MinArmorShieldMod { get; set; }
    public double? MaxArmorShieldMod { get; set; }
    public double? MinArmorDualWieldMod { get; set; }
    public double? MaxArmorDualWieldMod { get; set; }
    public double? MinArmorThieveryMod { get; set; }
    public double? MaxArmorThieveryMod { get; set; }
    public double? MinArmorRunMod { get; set; }
    public double? MaxArmorRunMod { get; set; }
    public double? MinArmorStaminaRegenMod { get; set; }
    public double? MaxArmorStaminaRegenMod { get; set; }
    public double? MinArmorPhysicalDefMod { get; set; }
    public double? MaxArmorPhysicalDefMod { get; set; }
    public double? MinArmorMagicDefMod { get; set; }
    public double? MaxArmorMagicDefMod { get; set; }
    public double? MinArmorTwohandedCombatMod { get; set; }
    public double? MaxArmorTwohandedCombatMod { get; set; }
    public double? MinArmorHealthRegenMod { get; set; }
    public double? MaxArmorHealthRegenMod { get; set; }
    public double? MinArmorPerceptionMod { get; set; }
    public double? MaxArmorPerceptionMod { get; set; }
    public double? MinArmorDeceptionMod { get; set; }
    public double? MaxArmorDeceptionMod { get; set; }
    public double? MinArmorHealthMod { get; set; }
    public double? MaxArmorHealthMod { get; set; }
    public double? MinArmorManaMod { get; set; }
    public double? MaxArmorManaMod { get; set; }
    public double? MinArmorStaminaMod { get; set; }
    public double? MaxArmorStaminaMod { get; set; }
    public int? MinGearCritDamage { get; set; }
    public int? MaxGearCritDamage { get; set; }
    public int? MinGearCritDamageResist { get; set; }
    public int? MaxGearCritDamageResist { get; set; }
    public int? MinGearDamage { get; set; }
    public int? MaxGearDamage { get; set; }
    public int? MinGearDamageResist { get; set; }
    public int? MaxGearDamageResist { get; set; }
    public int? MinGearHealingBoost { get; set; }
    public int? MaxGearHealingBoost { get; set; }
    public int? MinGearMaxHealth { get; set; }
    public int? MaxGearMaxHealth { get; set; }
    public string? SortBy { get; set; }
    public string? SortDir { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}

public class MarketSearchResult
{
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public List<MarketListing> Listings { get; set; } = new();
}

/// <summary>
/// Read-only queries against player_market_listings. Every filter is bound as a parameter —
/// never string-concatenated into the SQL — since this connects with a live, if narrowly
/// scoped (SELECT-only), database credential.
///
/// Item classification and stats are read out of ItemSnapshotJson (a full per-instance
/// property dump captured at listing time — see MarketListingSnapshot in ACE.Server) via
/// JSON_EXTRACT against ACE.Entity.Enum property names. The numeric enum/flag values below
/// were all verified directly against the enum source in daralet-ACE (WeaponType.cs,
/// CombatStyle.cs, ItemType.cs, ArmorWeightClass.cs, EquipMask.cs, CoverageMask.cs,
/// ImbuedEffectType.cs, Skill.cs, PropertyInt.cs/PropertyFloat.cs) — not guessed — since a
/// wrong constant here fails silently (JSON_EXTRACT just returns no rows, no error).
/// </summary>
public partial class MarketRepository
{

    private readonly DatabaseOptions _dbOptions;

    public MarketRepository(DatabaseOptions dbOptions)
    {
        _dbOptions = dbOptions;
    }

    public async Task<MarketSearchResult> SearchListingsAsync(MarketSearchRequest request, CancellationToken ct)
    {
        var page = Math.Max(request.Page, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var offset = (page - 1) * pageSize;

        var extraClauses = new List<string>();
        var extraParams = new List<(string Name, object? Value)>();
        BuildCategoryFilter(request, extraClauses, extraParams);

        if (!string.IsNullOrWhiteSpace(request.SpellName))
        {
            var spellIds = await GetSpellIdsByNameAsync(request.SpellName.Trim(), ct);
            extraClauses.Add(
                spellIds.Count > 0
                    ? "(" + string.Join(" OR ", spellIds.Select(id => $"JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesSpellBook.\"{id}\"') IS NOT NULL")) + ")"
                    : "1=0"
            );
        }

        // Scrolls carry a single direct-cast spell (PropertiesDID.Spell), not a PropertiesSpellBook
        // dictionary like gear does, so this needs its own IN(...) match rather than the block above.
        if (!string.IsNullOrWhiteSpace(request.ScrollSpellName))
        {
            var scrollSpellIds = await GetSpellIdsByNameAsync(request.ScrollSpellName.Trim(), ct);
            extraClauses.Add(
                scrollSpellIds.Count > 0
                    ? $"CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesDID.Spell') AS UNSIGNED) IN ({string.Join(",", scrollSpellIds)})"
                    : "1=0"
            );
        }

        var orderByExpr = "CreatedAtUtc";
        var orderByDir = "DESC";
        if (request.SortBy != null && SortExpressions.TryGetValue(request.SortBy, out var sortExpr))
        {
            orderByExpr = sortExpr;
            orderByDir = string.Equals(request.SortDir, "desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";
        }

        var whereClause = $"""
            WHERE IsSold = 0 AND IsCancelled = 0 AND ExpiresAtUtc > UTC_TIMESTAMP()
              AND (@keyword IS NULL OR JSON_UNQUOTE(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesString.Name')) LIKE @keywordLike)
              AND (@minPrice IS NULL OR ListedPrice >= @minPrice)
              AND (@maxPrice IS NULL OR ListedPrice <= @maxPrice)
              AND (@minTier IS NULL OR ItemTier >= @minTier)
              AND (@maxTier IS NULL OR ItemTier <= @maxTier)
              {string.Concat(extraClauses.Select(c => $"AND ({c})\n              "))}
            """;

        await using var connection = new MySqlConnection(_dbOptions.ShardConnectionString);
        await connection.OpenAsync(ct);

        var result = new MarketSearchResult { Page = page, PageSize = pageSize };

        await using (var countCmd = connection.CreateCommand())
        {
            countCmd.CommandText = $"SELECT COUNT(*) FROM player_market_listings {whereClause}";
            AddFilterParams(countCmd, request, extraParams);
            result.TotalCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync(ct));
        }

        await using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = $"""
                SELECT
                    Id, ItemWeenieClassId, ListedPrice, ItemTier,
                    MarketVendorTier, CreatedAtUtc, ExpiresAtUtc,
                    JSON_UNQUOTE(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesString.Name')) AS ItemName,
                    JSON_UNQUOTE(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesString.LongDesc')) AS ItemDesc,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesDID.Icon') AS IconDidRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesDID.IconOverlay') AS IconOverlayDidRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.ItemType') AS ItemTypeRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.WeaponType') AS WeaponTypeRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.DefaultCombatStyle') AS CombatStyleRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.WeaponSkill') AS WeaponSkillRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.ArmorWeightClass') AS ArmorWeightClassRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.SigilTrinketType') AS SigilTrinketTypeRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.SigilTrinketColor') AS SigilTrinketColorRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.ValidLocations') AS ValidLocationsRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.ClothingPriority') AS ClothingPriorityRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.ImbuedEffect') AS ImbuedEffectRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.Damage') AS DamageRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.DamageVariance') AS DamageVarianceRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.DamageMod') AS DamageModRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ElementalDamageMod') AS ElementalDamageModRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.WeaponRestorationSpellsMod') AS RestorationModRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.ItemSpellcraft') AS SpellcraftRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.ItemDifficulty') AS DifficultyRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.ItemMaxMana') AS ManaRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ManaRate') AS ManaRateRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ProcSpellRate') AS ProcSpellRateRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesDID.ProcSpell') AS ProcSpellRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.ElementalDamageBonus') AS ElementalDamageBonusRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.WeaponOffense') AS WeaponOffenseRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.WeaponWarMagicMod') AS WeaponWarMagicModRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.WeaponLifeMagicMod') AS WeaponLifeMagicModRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.WeaponPhysicalDefense') AS WeaponPhysicalDefenseRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.WeaponMagicalDefense') AS WeaponMagicalDefenseRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.ArmorLevel') AS ArmorLevelRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.WardLevel') AS WardLevelRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.ArmorSlots') AS ArmorSlotsRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorModVsSlash') AS ArmorModVsSlashRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorModVsPierce') AS ArmorModVsPierceRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorModVsBludgeon') AS ArmorModVsBludgeonRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorModVsCold') AS ArmorModVsColdRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorModVsFire') AS ArmorModVsFireRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorModVsAcid') AS ArmorModVsAcidRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorModVsElectric') AS ArmorModVsElectricRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorModVsNether') AS ArmorModVsNetherRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorWarMagicMod') AS ArmorWarMagicModRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorLifeMagicMod') AS ArmorLifeMagicModRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorManaRegenMod') AS ArmorManaRegenModRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ManaConversionMod') AS ArmorManaConversionModRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorAttackMod') AS ArmorAttackModRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorShieldMod') AS ArmorShieldModRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorDualWieldMod') AS ArmorDualWieldModRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorThieveryMod') AS ArmorThieveryModRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorRunMod') AS ArmorRunModRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorStaminaRegenMod') AS ArmorStaminaRegenModRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorPhysicalDefMod') AS ArmorPhysicalDefModRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorMagicDefMod') AS ArmorMagicDefModRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorTwohandedCombatMod') AS ArmorTwohandedCombatModRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorHealthRegenMod') AS ArmorHealthRegenModRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorPerceptionMod') AS ArmorPerceptionModRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorDeceptionMod') AS ArmorDeceptionModRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorHealthMod') AS ArmorHealthModRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorManaMod') AS ArmorManaModRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ArmorStaminaMod') AS ArmorStaminaModRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.GearCritDamage') AS GearCritDamageRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.GearCritDamageResist') AS GearCritDamageResistRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.GearDamage') AS GearDamageRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.GearDamageResist') AS GearDamageResistRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.GearHealingBoost') AS GearHealingBoostRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.GearMaxHealth') AS GearMaxHealthRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.SigilTrinketTriggerChance') AS SigilTriggerChanceRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.SigilTrinketCooldown') AS SigilCooldownRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.SigilTrinketManaReserved') AS SigilManaReservedRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.SigilTrinketStaminaReserved') AS SigilStaminaReservedRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.SigilTrinketHealthReserved') AS SigilHealthReservedRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.WeenieType') AS WeenieTypeRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.MaterialType') AS MaterialTypeRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.ItemWorkmanship') AS ItemWorkmanshipRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.NumItemsInMaterial') AS NumItemsInMaterialRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.NumTimesTinkered') AS NumTimesTinkeredRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.Structure') AS StructureRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt64.ItemTotalXp') AS ItemTotalXpRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt64.ItemBaseXp') AS ItemBaseXpRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.ItemXpStyle') AS ItemXpStyleRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.ItemMaxLevel') AS ItemMaxLevelRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.DamageType') AS DamageTypeRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.WeaponTime') AS WeaponTimeRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.AttackType') AS AttackTypeRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.WieldRequirements') AS WieldRequirementsRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.WieldSkillType') AS WieldSkillTypeRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.WieldDifficulty') AS WieldDifficultyRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.WieldRequirements2') AS WieldRequirements2Raw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.WieldSkillType2') AS WieldSkillType2Raw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.WieldDifficulty2') AS WieldDifficulty2Raw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.WieldRequirements3') AS WieldRequirements3Raw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.WieldSkillType3') AS WieldSkillType3Raw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.WieldDifficulty3') AS WieldDifficulty3Raw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.WieldRequirements4') AS WieldRequirements4Raw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.WieldSkillType4') AS WieldSkillType4Raw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.WieldDifficulty4') AS WieldDifficulty4Raw,
                    JSON_LENGTH(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesSpellBook')) AS SpellCountRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesSpellBook') AS SpellBookRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt') AS AllPropertiesIntRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.JewelSockets') AS SocketsRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.StaminaCostReductionMod') AS StaminaCostReductionModRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.IgnoreWard') AS IgnoreWardRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.IgnoreArmor') AS IgnoreArmorRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.CriticalMultiplier') AS CriticalMultiplierRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.CriticalFrequency') AS CriticalFrequencyRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesFloat.ResistanceModifier') AS ResistanceModifierRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.ResistanceModifierType') AS ResistanceModifierTypeRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.NoCompsRequiredForMagicSchool') AS NoCompsRequiredForMagicSchoolRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.TrophyQuality') AS TrophyQualityRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.JewelQuality') AS JewelQualityRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.JewelMaterialType') AS JewelMaterialTypeRaw,
                    JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesDID.Spell') AS SpellDidRaw
                FROM player_market_listings
                {whereClause}
                ORDER BY {orderByExpr} {orderByDir}
                LIMIT @limit OFFSET @offset
                """;
            AddFilterParams(cmd, request, extraParams);
            cmd.Parameters.AddWithValue("@limit", pageSize);
            cmd.Parameters.AddWithValue("@offset", offset);

            var pending = new List<(MarketListing Listing, List<int> SpellIds)>();
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                pending.Add(Describe(new RowFacts(reader)));
            }
            result.Listings.AddRange(pending.Select(p => p.Listing));

            var allSpellIds = pending.SelectMany(p => p.SpellIds).Distinct().ToList();
            if (allSpellIds.Count > 0)
            {
                var spellNames = await GetSpellNamesAsync(allSpellIds, ct);
                foreach (var (listing, spellIds) in pending)
                {
                    listing.Spells = spellIds.Select(id => spellNames.TryGetValue(id, out var n) ? n : $"Spell {id}").ToList();
                }
            }
        }

        return result;
    }

    // Spell names aren't in ItemSnapshotJson (only spell IDs are) — resolved via one batched
    // lookup per page against ace_world.spell, rather than a per-listing query.
    private async Task<Dictionary<int, string>> GetSpellNamesAsync(List<int> spellIds, CancellationToken ct)
    {
        var names = new Dictionary<int, string>();
        await using var connection = new MySqlConnection(_dbOptions.WorldConnectionString);
        await connection.OpenAsync(ct);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"SELECT id, name FROM spell WHERE id IN ({string.Join(",", spellIds)})";
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            names[reader.GetInt32(0)] = reader.GetString(1);
        }
        return names;
    }

    // Resolves a free-text spell name query (e.g. "Strength Self") to matching spell ids in
    // ace_world.spell, for the "Has Spell" listing filter — a substring match like Keyword's, so
    // "bow" finds every level/rank of every spell with "bow" in its name (Bow Aptitude, Bow Mastery,
    // Crossbow Mastery, ...). The candidate set is bounded (not unbounded) purely to protect against
    // a pathological 1-2 letter query matching a huge fraction of the ~4.8k-row spell table and
    // bloating the OR'd JSON_EXTRACT clause built in SearchListingsAsync — 500 comfortably covers
    // every realistic search term (e.g. "bow" only matches 64, "self" matches 865) without silently
    // dropping real matches the way the previous LIMIT 25 did.
    private async Task<List<int>> GetSpellIdsByNameAsync(string nameQuery, CancellationToken ct)
    {
        var ids = new List<int>();
        await using var connection = new MySqlConnection(_dbOptions.WorldConnectionString);
        await connection.OpenAsync(ct);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT id FROM spell WHERE name LIKE @nameLike LIMIT 500";
        cmd.Parameters.AddWithValue("@nameLike", "%" + nameQuery + "%");
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            ids.Add(reader.GetInt32(0));
        }
        return ids;
    }

    private static void BuildCategoryFilter(MarketSearchRequest request, List<string> clauses, List<(string Name, object? Value)> parameters)
    {
        var itemTypeCol = "CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.ItemType') AS UNSIGNED)";

        switch (request.ItemCategory)
        {
            case "Weapon":
                clauses.Add($"({itemTypeCol} & {ItemTypeWeaponMask}) != 0");
                AddWeaponClassFilter(request.WeaponClass, clauses);
                AddMinMaxFilter(DpsSqlExpr, request.MinDps, request.MaxDps, "dps", clauses, parameters);
                AddMinMaxFilter(SortExpressions["baseDamage"], request.MinBaseDamage, request.MaxBaseDamage, "baseDamage", clauses, parameters);
                AddMinMaxFilter(SortExpressions["damageVariance"], request.MinDamageVariance, request.MaxDamageVariance, "damageVariance", clauses, parameters);
                AddMinMaxFilter(SortExpressions["weaponSpeed"], request.MinSpeed, request.MaxSpeed, "weaponSpeed", clauses, parameters);
                AddMinMaxFilter(SortExpressions["attackMod"], request.MinAttackMod, request.MaxAttackMod, "attackMod", clauses, parameters);
                AddMinMaxFilter(SortExpressions["physDefMod"], request.MinPhysDefMod, request.MaxPhysDefMod, "physDefMod", clauses, parameters);
                AddMinMaxFilter(SortExpressions["magicDefMod"], request.MinMagicDefMod, request.MaxMagicDefMod, "magicDefMod", clauses, parameters);
                AddMinMaxFilter(SortExpressions["spellcraft"], request.MinSpellcraft, request.MaxSpellcraft, "spellcraft", clauses, parameters);
                AddMinMaxFilter(SortExpressions["difficulty"], request.MinDifficulty, request.MaxDifficulty, "difficulty", clauses, parameters);
                AddMinMaxFilter(SortExpressions["warMagicMod"], request.MinWarMagicMod, request.MaxWarMagicMod, "warMagicMod", clauses, parameters);
                AddMinMaxFilter(SortExpressions["lifeMagicMod"], request.MinLifeMagicMod, request.MaxLifeMagicMod, "lifeMagicMod", clauses, parameters);
                break;
            case "Armor":
                clauses.Add($"({itemTypeCol} & {ItemTypeArmor}) != 0");
                if (request.ArmorWeightClass is { Length: > 0 })
                {
                    var value = request.ArmorWeightClass switch
                    {
                        "Cloth" => ArmorWeightCloth,
                        "Light" => ArmorWeightLight,
                        "Heavy" => ArmorWeightHeavy,
                        _ => (int?)null,
                    };
                    if (value.HasValue)
                    {
                        clauses.Add("JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.ArmorWeightClass') = @armorWeightClass");
                        parameters.Add(("@armorWeightClass", value.Value));
                    }
                }
                if (request.Coverage is { Count: > 0 })
                {
                    uint mask = 0;
                    foreach (var part in request.Coverage)
                    {
                        if (CoverageMasks.TryGetValue(part, out var partMask))
                        {
                            mask |= partMask;
                        }
                    }
                    if (mask != 0)
                    {
                        clauses.Add($"(CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.ClothingPriority') AS UNSIGNED) & {mask}) != 0");
                    }
                }
                AddMinMaxFilter(SortExpressions["armorLevel"], request.MinArmorLevel, request.MaxArmorLevel, "armorLevel", clauses, parameters);
                AddMinMaxFilter(SortExpressions["wardPerSlot"], request.MinWardLevel, request.MaxWardLevel, "wardLevel", clauses, parameters);
                AddMinMaxFilter(SortExpressions["armorModVsSlash"], request.MinArmorModVsSlash, request.MaxArmorModVsSlash, "armorModVsSlash", clauses, parameters);
                AddMinMaxFilter(SortExpressions["armorModVsPierce"], request.MinArmorModVsPierce, request.MaxArmorModVsPierce, "armorModVsPierce", clauses, parameters);
                AddMinMaxFilter(SortExpressions["armorModVsBludgeon"], request.MinArmorModVsBludgeon, request.MaxArmorModVsBludgeon, "armorModVsBludgeon", clauses, parameters);
                AddMinMaxFilter(SortExpressions["armorModVsCold"], request.MinArmorModVsCold, request.MaxArmorModVsCold, "armorModVsCold", clauses, parameters);
                AddMinMaxFilter(SortExpressions["armorModVsFire"], request.MinArmorModVsFire, request.MaxArmorModVsFire, "armorModVsFire", clauses, parameters);
                AddMinMaxFilter(SortExpressions["armorModVsAcid"], request.MinArmorModVsAcid, request.MaxArmorModVsAcid, "armorModVsAcid", clauses, parameters);
                AddMinMaxFilter(SortExpressions["armorModVsElectric"], request.MinArmorModVsElectric, request.MaxArmorModVsElectric, "armorModVsElectric", clauses, parameters);
                AddMinMaxFilter(SortExpressions["armorWarMagicModPerSlot"], request.MinArmorWarMagicMod, request.MaxArmorWarMagicMod, "armorWarMagicMod", clauses, parameters);
                AddMinMaxFilter(SortExpressions["armorLifeMagicModPerSlot"], request.MinArmorLifeMagicMod, request.MaxArmorLifeMagicMod, "armorLifeMagicMod", clauses, parameters);
                AddMinMaxFilter(SortExpressions["armorManaRegenModPerSlot"], request.MinArmorManaRegenMod, request.MaxArmorManaRegenMod, "armorManaRegenMod", clauses, parameters);
                AddMinMaxFilter(SortExpressions["armorManaConversionModPerSlot"], request.MinArmorManaConversionMod, request.MaxArmorManaConversionMod, "armorManaConversionMod", clauses, parameters);
                AddMinMaxFilter(SortExpressions["armorAttackModPerSlot"], request.MinArmorAttackMod, request.MaxArmorAttackMod, "armorAttackMod", clauses, parameters);
                AddMinMaxFilter(SortExpressions["armorShieldModPerSlot"], request.MinArmorShieldMod, request.MaxArmorShieldMod, "armorShieldMod", clauses, parameters);
                AddMinMaxFilter(SortExpressions["armorDualWieldModPerSlot"], request.MinArmorDualWieldMod, request.MaxArmorDualWieldMod, "armorDualWieldMod", clauses, parameters);
                AddMinMaxFilter(SortExpressions["armorThieveryModPerSlot"], request.MinArmorThieveryMod, request.MaxArmorThieveryMod, "armorThieveryMod", clauses, parameters);
                AddMinMaxFilter(SortExpressions["armorRunModPerSlot"], request.MinArmorRunMod, request.MaxArmorRunMod, "armorRunMod", clauses, parameters);
                AddMinMaxFilter(SortExpressions["armorStaminaRegenModPerSlot"], request.MinArmorStaminaRegenMod, request.MaxArmorStaminaRegenMod, "armorStaminaRegenMod", clauses, parameters);
                AddMinMaxFilter(SortExpressions["armorPhysicalDefModPerSlot"], request.MinArmorPhysicalDefMod, request.MaxArmorPhysicalDefMod, "armorPhysicalDefMod", clauses, parameters);
                AddMinMaxFilter(SortExpressions["armorMagicDefModPerSlot"], request.MinArmorMagicDefMod, request.MaxArmorMagicDefMod, "armorMagicDefMod", clauses, parameters);
                AddMinMaxFilter(SortExpressions["armorTwohandedCombatModPerSlot"], request.MinArmorTwohandedCombatMod, request.MaxArmorTwohandedCombatMod, "armorTwohandedCombatMod", clauses, parameters);
                AddMinMaxFilter(SortExpressions["armorHealthRegenModPerSlot"], request.MinArmorHealthRegenMod, request.MaxArmorHealthRegenMod, "armorHealthRegenMod", clauses, parameters);
                AddMinMaxFilter(SortExpressions["armorPerceptionModPerSlot"], request.MinArmorPerceptionMod, request.MaxArmorPerceptionMod, "armorPerceptionMod", clauses, parameters);
                AddMinMaxFilter(SortExpressions["armorDeceptionModPerSlot"], request.MinArmorDeceptionMod, request.MaxArmorDeceptionMod, "armorDeceptionMod", clauses, parameters);
                AddMinMaxFilter(SortExpressions["difficulty"], request.MinDifficulty, request.MaxDifficulty, "difficulty", clauses, parameters);
                AddMinMaxFilter(SortExpressions["spellcraft"], request.MinSpellcraft, request.MaxSpellcraft, "spellcraft", clauses, parameters);
                break;
            case "Clothing":
                // Clothing (shirts, pants, etc.) shares ItemType's "wearable" family with Armor but
                // carries none of Armor's protection/weight-class stats — just Ward Level and its
                // own vital-mod trio (ArmorHealthMod/ArmorManaMod/ArmorStaminaMod), distinct
                // properties from Armor's *RegenMod family.
                clauses.Add($"({itemTypeCol} & {ItemTypeClothing}) != 0");
                if (request.Coverage is { Count: > 0 })
                {
                    uint clothingCoverageMask = 0;
                    foreach (var part in request.Coverage)
                    {
                        if (CoverageMasks.TryGetValue(part, out var partMask))
                        {
                            clothingCoverageMask |= partMask;
                        }
                    }
                    if (clothingCoverageMask != 0)
                    {
                        clauses.Add($"(CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.ClothingPriority') AS UNSIGNED) & {clothingCoverageMask}) != 0");
                    }
                }
                AddMinMaxFilter(SortExpressions["wardPerSlot"], request.MinWardLevel, request.MaxWardLevel, "wardLevel", clauses, parameters);
                AddMinMaxFilter(SortExpressions["armorHealthModPerSlot"], request.MinArmorHealthMod, request.MaxArmorHealthMod, "armorHealthMod", clauses, parameters);
                AddMinMaxFilter(SortExpressions["armorManaModPerSlot"], request.MinArmorManaMod, request.MaxArmorManaMod, "armorManaMod", clauses, parameters);
                AddMinMaxFilter(SortExpressions["armorStaminaModPerSlot"], request.MinArmorStaminaMod, request.MaxArmorStaminaMod, "armorStaminaMod", clauses, parameters);
                AddMinMaxFilter(SortExpressions["difficulty"], request.MinDifficulty, request.MaxDifficulty, "difficulty", clauses, parameters);
                AddMinMaxFilter(SortExpressions["spellcraft"], request.MinSpellcraft, request.MaxSpellcraft, "spellcraft", clauses, parameters);
                break;
            case "Salvage":
                clauses.Add("CAST(JSON_EXTRACT(ItemSnapshotJson, '$.WeenieType') AS UNSIGNED) = " + WeenieTypeSalvage);
                if (request.SalvageMaterial is { Length: > 0 })
                {
                    var materialId = MaterialTypeNames.FirstOrDefault(kv => kv.Value == request.SalvageMaterial).Key;
                    if (materialId != 0)
                    {
                        clauses.Add("JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.MaterialType') = @salvageMaterial");
                        parameters.Add(("@salvageMaterial", materialId));
                    }
                }
                else if (request.SalvageTargetType is { Length: > 0 })
                {
                    var targetSet = request.SalvageTargetType switch
                    {
                        "Weapon" => SalvageWeaponTargetMaterials,
                        "Armor" => SalvageArmorTargetMaterials,
                        "Jewelry" => SalvageJewelryTargetMaterials,
                        _ => null,
                    };
                    if (targetSet is { Count: > 0 })
                    {
                        clauses.Add($"JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.MaterialType') IN ({string.Join(",", targetSet)})");
                    }
                }
                AddMinMaxFilter(SortExpressions["workmanship"], request.MinWorkmanship, request.MaxWorkmanship, "workmanship", clauses, parameters);
                AddMinMaxFilter(SortExpressions["units"], request.MinUnits, request.MaxUnits, "units", clauses, parameters);
                AddMinMaxFilter(SortExpressions["pricePerUnit"], request.MinPricePerUnit, request.MaxPricePerUnit, "pricePerUnit", clauses, parameters);
                break;
            case "SigilTrinket":
                clauses.Add("JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.SigilTrinketType') IS NOT NULL");
                if (request.SigilTrinketType is { Length: > 0 })
                {
                    var index = Array.IndexOf(SigilTrinketTypeNames, request.SigilTrinketType);
                    if (index >= 0)
                    {
                        clauses.Add("JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.SigilTrinketType') = @sigilTrinketType");
                        parameters.Add(("@sigilTrinketType", index));
                    }
                }
                if (request.SigilEffect is { Length: > 0 } && Array.IndexOf(SigilEffectNames, request.SigilEffect) >= 0)
                {
                    // The effect name is baked into the item's name as a " of <Effect>" suffix (see
                    // SigilEffectName computation in Describe, MarketRepository.Items.cs) rather than stored as its own property.
                    clauses.Add($"{SortExpressions["name"]} LIKE @sigilEffectPattern");
                    parameters.Add(("@sigilEffectPattern", "% of " + request.SigilEffect));
                }
                if (request.SigilMinLevel.HasValue)
                {
                    clauses.Add($"{ItemLevelSqlExpr} >= @sigilMinLevel");
                    parameters.Add(("@sigilMinLevel", request.SigilMinLevel.Value));
                }
                if (request.SigilMaxLevel.HasValue)
                {
                    clauses.Add($"{ItemLevelSqlExpr} <= @sigilMaxLevel");
                    parameters.Add(("@sigilMaxLevel", request.SigilMaxLevel.Value));
                }
                AddMinMaxFilter(SortExpressions["procChance"], request.MinProcChance, request.MaxProcChance, "procChance", clauses, parameters);
                AddMinMaxFilter(SortExpressions["cooldown"], request.MinCooldown, request.MaxCooldown, "cooldown", clauses, parameters);
                AddMinMaxFilter(SortExpressions["manaReserved"], request.MinManaReserved, request.MaxManaReserved, "manaReserved", clauses, parameters);
                AddMinMaxFilter(SortExpressions["staminaReserved"], request.MinStaminaReserved, request.MaxStaminaReserved, "staminaReserved", clauses, parameters);
                AddMinMaxFilter(SortExpressions["healthReserved"], request.MinHealthReserved, request.MaxHealthReserved, "healthReserved", clauses, parameters);
                break;
            case "Jewelry":
                clauses.Add($"({itemTypeCol} & {ItemTypeJewelry}) != 0");
                // Sigil trinkets are also worn in a jewelry-like slot and can carry the Jewelry
                // ItemType bit, but they belong in the SigilTrinket category, not here.
                clauses.Add("JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.SigilTrinketType') IS NULL");
                if (request.JewelrySlot is { Length: > 0 })
                {
                    var mask = request.JewelrySlot switch
                    {
                        "Amulet" => EquipMaskNeckWear,
                        "Bracelet" => EquipMaskWristWear,
                        "Ring" => EquipMaskFingerWear,
                        _ => (uint?)null,
                    };
                    if (mask.HasValue)
                    {
                        clauses.Add($"(CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.ValidLocations') AS UNSIGNED) & {mask.Value}) != 0");
                    }
                }
                AddMinMaxFilter(SortExpressions["wardPerSlot"], request.MinWardLevel, request.MaxWardLevel, "wardLevel", clauses, parameters);
                AddMinMaxFilter(SortExpressions["gearCritDamage"], request.MinGearCritDamage, request.MaxGearCritDamage, "gearCritDamage", clauses, parameters);
                AddMinMaxFilter(SortExpressions["gearCritDamageResist"], request.MinGearCritDamageResist, request.MaxGearCritDamageResist, "gearCritDamageResist", clauses, parameters);
                AddMinMaxFilter(SortExpressions["gearDamage"], request.MinGearDamage, request.MaxGearDamage, "gearDamage", clauses, parameters);
                AddMinMaxFilter(SortExpressions["gearDamageResist"], request.MinGearDamageResist, request.MaxGearDamageResist, "gearDamageResist", clauses, parameters);
                AddMinMaxFilter(SortExpressions["gearHealingBoost"], request.MinGearHealingBoost, request.MaxGearHealingBoost, "gearHealingBoost", clauses, parameters);
                AddMinMaxFilter(SortExpressions["gearMaxHealth"], request.MinGearMaxHealth, request.MaxGearMaxHealth, "gearMaxHealth", clauses, parameters);
                AddMinMaxFilter(SortExpressions["difficulty"], request.MinDifficulty, request.MaxDifficulty, "difficulty", clauses, parameters);
                AddMinMaxFilter(SortExpressions["spellcraft"], request.MinSpellcraft, request.MaxSpellcraft, "spellcraft", clauses, parameters);
                break;
            case "Consumable":
                clauses.Add($"CAST(JSON_EXTRACT(ItemSnapshotJson, '$.WeenieType') AS UNSIGNED) IN ({WeenieTypeFood}, {WeenieTypeHealer})");
                switch (request.ConsumableType)
                {
                    case "HealingKit":
                        clauses.Add($"CAST(JSON_EXTRACT(ItemSnapshotJson, '$.WeenieType') AS UNSIGNED) = {WeenieTypeHealer}");
                        break;
                    case "Food":
                        clauses.Add($"CAST(JSON_EXTRACT(ItemSnapshotJson, '$.WeenieType') AS UNSIGNED) = {WeenieTypeFood}");
                        clauses.Add($"({itemTypeCol} & {ItemTypeFood}) != 0");
                        break;
                    case "Potion":
                        clauses.Add($"CAST(JSON_EXTRACT(ItemSnapshotJson, '$.WeenieType') AS UNSIGNED) = {WeenieTypeFood}");
                        clauses.Add($"({itemTypeCol} & {ItemTypeMisc}) != 0");
                        break;
                }
                break;
            case "Misc":
                // Catch-all: not Salvage, not a Sigil Trinket, not Jewelry/Armor/Clothing/Weapon by
                // ItemType, and not a Consumable — mirrors the else-chain in Describe exactly.
                clauses.Add($"""
                    NOT (
                        CAST(JSON_EXTRACT(ItemSnapshotJson, '$.WeenieType') AS UNSIGNED) = {WeenieTypeSalvage}
                        OR JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.SigilTrinketType') IS NOT NULL
                        OR ({itemTypeCol} & {ItemTypeJewelry}) != 0
                        OR ({itemTypeCol} & {ItemTypeArmorMask}) != 0
                        OR ({itemTypeCol} & {ItemTypeWeaponMask}) != 0
                        OR CAST(JSON_EXTRACT(ItemSnapshotJson, '$.WeenieType') AS UNSIGNED) IN ({WeenieTypeFood}, {WeenieTypeHealer})
                    )
                    """);
                switch (request.MiscType)
                {
                    case "Scroll":
                        clauses.Add($"CAST(JSON_EXTRACT(ItemSnapshotJson, '$.WeenieType') AS UNSIGNED) = {WeenieTypeScroll}");
                        if (request.ScrollSchool is { Length: > 0 } && MagicSchoolNameToValue.TryGetValue(request.ScrollSchool, out var schoolValue))
                        {
                            var schoolSpellIds = SpellSchoolById.Where(kv => kv.Value == schoolValue).Select(kv => kv.Key).ToList();
                            clauses.Add(
                                schoolSpellIds.Count > 0
                                    ? $"CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesDID.Spell') AS UNSIGNED) IN ({string.Join(",", schoolSpellIds)})"
                                    : "1=0"
                            );
                        }
                        break;
                    case "Gem":
                        // Loose gemstones (Gem) and carved jewels (Jewel) share one subtype — see
                        // Describe's Gem branch for why.
                        clauses.Add($"CAST(JSON_EXTRACT(ItemSnapshotJson, '$.WeenieType') AS UNSIGNED) IN ({WeenieTypeGem}, {WeenieTypeJewel})");
                        if (request.CarvedJewelOnly == true)
                        {
                            clauses.Add($"CAST(JSON_EXTRACT(ItemSnapshotJson, '$.WeenieType') AS UNSIGNED) = {WeenieTypeJewel}");
                        }
                        if (request.GemMaterial is { Length: > 0 })
                        {
                            var gemMaterialId = MaterialTypeNames.FirstOrDefault(kv => kv.Value == request.GemMaterial).Key;
                            if (gemMaterialId != 0)
                            {
                                clauses.Add($"""
                                    (
                                        (CAST(JSON_EXTRACT(ItemSnapshotJson, '$.WeenieType') AS UNSIGNED) = {WeenieTypeGem} AND JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.MaterialType') = @gemMaterial)
                                        OR (CAST(JSON_EXTRACT(ItemSnapshotJson, '$.WeenieType') AS UNSIGNED) = {WeenieTypeJewel} AND JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.JewelMaterialType') = @gemMaterial)
                                    )
                                    """);
                                parameters.Add(("@gemMaterial", gemMaterialId));
                            }
                        }
                        // Quality is ItemWorkmanship for a loose Gem, JewelQuality for a carved Jewel —
                        // both are the same 1-10 scale, just stored under different properties.
                        AddMinMaxFilter(
                            "COALESCE(CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.JewelQuality') AS SIGNED), CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.ItemWorkmanship') AS SIGNED))",
                            request.MinGemQuality, request.MaxGemQuality, "gemQuality", clauses, parameters
                        );
                        break;
                    case "Trophy":
                        clauses.Add("JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.TrophyQuality') IS NOT NULL");
                        if (request.TrophyType is { Length: > 0 } && Array.IndexOf(TrophyTypeNames, request.TrophyType) >= 0)
                        {
                            clauses.Add($"{SortExpressions["name"]} LIKE @trophyTypePattern");
                            parameters.Add(("@trophyTypePattern", "%" + request.TrophyType));
                        }
                        AddMinMaxFilter(
                            "CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.TrophyQuality') AS SIGNED)",
                            request.MinTrophyQuality, request.MaxTrophyQuality, "trophyQuality", clauses, parameters
                        );
                        break;
                    case "Other":
                        clauses.Add($"""
                            NOT (
                                CAST(JSON_EXTRACT(ItemSnapshotJson, '$.WeenieType') AS UNSIGNED) IN ({WeenieTypeScroll}, {WeenieTypeGem}, {WeenieTypeJewel})
                                OR JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.TrophyQuality') IS NOT NULL
                            )
                            """);
                        break;
                }
                break;
        }
    }

    private static void AddMinMaxFilter(string sqlExpr, double? min, double? max, string paramPrefix, List<string> clauses, List<(string Name, object? Value)> parameters)
    {
        if (min.HasValue)
        {
            clauses.Add($"{sqlExpr} >= @{paramPrefix}Min");
            parameters.Add(($"@{paramPrefix}Min", min.Value));
        }
        if (max.HasValue)
        {
            clauses.Add($"{sqlExpr} <= @{paramPrefix}Max");
            parameters.Add(($"@{paramPrefix}Max", max.Value));
        }
    }

    private static void AddWeaponClassFilter(string? weaponClass, List<string> clauses)
    {
        var weaponTypeCol = "JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.WeaponType')";
        var combatStyleCol = "CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.DefaultCombatStyle') AS UNSIGNED)";
        var weaponSkillCol = "JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.WeaponSkill')";
        var itemTypeCol = "CAST(JSON_EXTRACT(ItemSnapshotJson, '$.PropertiesInt.ItemType') AS UNSIGNED)";

        switch (weaponClass)
        {
            case "Martial":
                clauses.Add($"({weaponTypeCol} IN ({WeaponTypeSword},{WeaponTypeAxe},{WeaponTypeMace},{WeaponTypeSpear},{WeaponTypeTwoHanded}) OR {weaponSkillCol} IN ({SkillSword},{SkillAxe},{SkillMace},{SkillSpear},{SkillTwoHandedCombat}))");
                break;
            case "Missile":
                clauses.Add($"({weaponTypeCol} IN ({WeaponTypeBow},{WeaponTypeCrossbow}) OR ({combatStyleCol} & {CombatStyleAtlatl}) != 0 OR {weaponSkillCol} IN ({SkillBow},{SkillCrossbow}))");
                break;
            case "Thrown":
                clauses.Add($"(({combatStyleCol} & {CombatStyleThrownWeapon}) != 0 OR {weaponSkillCol} = {SkillThrownWeapon})");
                break;
            case "Dagger":
                clauses.Add($"({weaponTypeCol} = {WeaponTypeDagger} OR {weaponSkillCol} = {SkillDagger})");
                break;
            case "Staff":
                clauses.Add($"({weaponTypeCol} = {WeaponTypeStaff} OR {weaponSkillCol} = {SkillStaff})");
                break;
            case "Unarmed":
                clauses.Add($"({weaponTypeCol} = {WeaponTypeUnarmed} OR {weaponSkillCol} = {SkillUnarmedCombat})");
                break;
            case "WarMagic":
                clauses.Add($"({itemTypeCol} & {ItemTypeCaster}) != 0 AND {weaponSkillCol} = {SkillWarMagic}");
                break;
            case "LifeMagic":
                clauses.Add($"({itemTypeCol} & {ItemTypeCaster}) != 0 AND {weaponSkillCol} = {SkillLifeMagic}");
                break;
        }
    }

    private static void AddFilterParams(MySqlCommand cmd, MarketSearchRequest request, List<(string Name, object? Value)> extraParams)
    {
        cmd.Parameters.AddWithValue("@keyword", request.Keyword);
        cmd.Parameters.AddWithValue("@keywordLike", string.IsNullOrEmpty(request.Keyword) ? null : $"%{request.Keyword}%");
        cmd.Parameters.AddWithValue("@minPrice", request.MinPrice);
        cmd.Parameters.AddWithValue("@maxPrice", request.MaxPrice);
        cmd.Parameters.AddWithValue("@minTier", request.MinTier);
        cmd.Parameters.AddWithValue("@maxTier", request.MaxTier);
        foreach (var (name, value) in extraParams)
        {
            cmd.Parameters.AddWithValue(name, value);
        }
    }

    /// <summary>
    /// A row of the listings query as the facts Describe reads, through the same readers ReadListing used, so a listing comes
    /// out exactly as it did before Describe moved to MarketRepository.Items.cs.
    /// </summary>
    private sealed class RowFacts(MySqlDataReader reader) : IItemFacts
    {
        public int? Int(string column) => GetNullableInt(reader, column);
        public uint? UInt(string column) => GetNullableUInt(reader, column);
        public long? Long(string column) => GetNullableLong(reader, column);
        public double? Double(string column) => GetNullableDouble(reader, column);

        public string? Text(string column)
        {
            var ordinal = reader.GetOrdinal(column);
            return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
        }

        public DateTime? Date(string column)
        {
            var ordinal = reader.GetOrdinal(column);
            return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
        }
    }

    private static int? GetNullableInt(MySqlDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : Convert.ToInt32(reader.GetValue(ordinal));
    }

    private static uint? GetNullableUInt(MySqlDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : Convert.ToUInt32(reader.GetValue(ordinal));
    }

    private static long? GetNullableLong(MySqlDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : Convert.ToInt64(reader.GetValue(ordinal));
    }

    private static double? GetNullableDouble(MySqlDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : Convert.ToDouble(reader.GetValue(ordinal));
    }
}
