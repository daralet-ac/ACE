using System.Threading.RateLimiting;
using ACE.MarketApi;
using Microsoft.AspNetCore.RateLimiting;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, config) =>
    config.ReadFrom.Configuration(context.Configuration));

var dbOptions = builder.Configuration.GetSection("Database").Get<DatabaseOptions>() ?? new DatabaseOptions();
var corsOptions = builder.Configuration.GetSection("Cors").Get<CorsOptions>() ?? new CorsOptions();

if (string.IsNullOrEmpty(dbOptions.Username) || string.IsNullOrEmpty(dbOptions.Password))
{
    throw new InvalidOperationException(
        "Database:Username/Password are not set. Copy appsettings.Development.json (gitignored) " +
        "with the market_api_ro credentials before running locally.");
}

builder.Services.AddSingleton(dbOptions);
builder.Services.AddSingleton<MarketRepository>();
builder.Services.AddSingleton<LeaderboardRepository>();

const string CorsPolicy = "SitePolicy";
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy =>
    {
        if (corsOptions.AllowedOrigins.Length > 0)
        {
            policy.WithOrigins(corsOptions.AllowedOrigins).AllowAnyHeader().AllowAnyMethod();
        }
    });
});

// Per-IP rate limit, applied to every endpoint — this is a small read-only public API with no
// auth, so nothing else stops a script from hammering it in a loop. 60 req/min comfortably
// covers real browsing (a single Leaderboards page load fires ~6 requests) while still capping
// abuse. Bursts beyond the limit are rejected outright (QueueLimit 0) rather than queued.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var key = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            Window = TimeSpan.FromMinutes(1),
            PermitLimit = 60,
            QueueLimit = 0,
        });
    });
});

var app = builder.Build();

app.UseCors(CorsPolicy);
app.UseRateLimiter();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/api/market/listings", async (
    string? q,
    string? spellName,
    int? minPrice,
    int? maxPrice,
    int? minTier,
    int? maxTier,
    string? itemCategory,
    string? weaponClass,
    string? armorWeightClass,
    string? coverage,
    string? sigilTrinketType,
    int? sigilMinLevel,
    int? sigilMaxLevel,
    double? minDps,
    double? maxDps,
    int? minBaseDamage,
    int? maxBaseDamage,
    double? minDamageVariance,
    double? maxDamageVariance,
    double? minSpeed,
    double? maxSpeed,
    double? minAttackMod,
    double? maxAttackMod,
    double? minPhysDefMod,
    double? maxPhysDefMod,
    double? minMagicDefMod,
    double? maxMagicDefMod,
    int? minSpellcraft,
    int? maxSpellcraft,
    int? minDifficulty,
    int? maxDifficulty,
    double? minWarMagicMod,
    double? maxWarMagicMod,
    double? minLifeMagicMod,
    double? maxLifeMagicMod,
    int? minArmorLevel,
    int? maxArmorLevel,
    int? minWardLevel,
    int? maxWardLevel,
    double? minArmorModVsSlash,
    double? maxArmorModVsSlash,
    double? minArmorModVsPierce,
    double? maxArmorModVsPierce,
    double? minArmorModVsBludgeon,
    double? maxArmorModVsBludgeon,
    double? minArmorModVsCold,
    double? maxArmorModVsCold,
    double? minArmorModVsFire,
    double? maxArmorModVsFire,
    double? minArmorModVsAcid,
    double? maxArmorModVsAcid,
    double? minArmorModVsElectric,
    double? maxArmorModVsElectric,
    double? minArmorWarMagicMod,
    double? maxArmorWarMagicMod,
    double? minArmorLifeMagicMod,
    double? maxArmorLifeMagicMod,
    double? minArmorManaRegenMod,
    double? maxArmorManaRegenMod,
    double? minArmorManaConversionMod,
    double? maxArmorManaConversionMod,
    double? minArmorAttackMod,
    double? maxArmorAttackMod,
    double? minArmorShieldMod,
    double? maxArmorShieldMod,
    double? minArmorDualWieldMod,
    double? maxArmorDualWieldMod,
    double? minArmorThieveryMod,
    double? maxArmorThieveryMod,
    double? minArmorRunMod,
    double? maxArmorRunMod,
    double? minArmorStaminaRegenMod,
    double? maxArmorStaminaRegenMod,
    double? minArmorPhysicalDefMod,
    double? maxArmorPhysicalDefMod,
    double? minArmorMagicDefMod,
    double? maxArmorMagicDefMod,
    double? minArmorTwohandedCombatMod,
    double? maxArmorTwohandedCombatMod,
    double? minArmorHealthRegenMod,
    double? maxArmorHealthRegenMod,
    double? minArmorPerceptionMod,
    double? maxArmorPerceptionMod,
    double? minArmorDeceptionMod,
    double? maxArmorDeceptionMod,
    string? jewelrySlot,
    string? salvageTargetType,
    string? salvageMaterial,
    string? sortBy,
    string? sortDir,
    int? page,
    int? pageSize,
    MarketRepository repository,
    CancellationToken ct) =>
{
    var request = new MarketSearchRequest
    {
        Keyword = q,
        SpellName = spellName,
        MinPrice = minPrice,
        MaxPrice = maxPrice,
        MinTier = minTier,
        MaxTier = maxTier,
        ItemCategory = itemCategory,
        WeaponClass = weaponClass,
        ArmorWeightClass = armorWeightClass,
        Coverage = string.IsNullOrEmpty(coverage) ? null : coverage.Split(',').ToList(),
        SigilTrinketType = sigilTrinketType,
        SigilMinLevel = sigilMinLevel,
        SigilMaxLevel = sigilMaxLevel,
        MinDps = minDps,
        MaxDps = maxDps,
        MinBaseDamage = minBaseDamage,
        MaxBaseDamage = maxBaseDamage,
        MinDamageVariance = minDamageVariance,
        MaxDamageVariance = maxDamageVariance,
        MinSpeed = minSpeed,
        MaxSpeed = maxSpeed,
        MinAttackMod = minAttackMod,
        MaxAttackMod = maxAttackMod,
        MinPhysDefMod = minPhysDefMod,
        MaxPhysDefMod = maxPhysDefMod,
        MinMagicDefMod = minMagicDefMod,
        MaxMagicDefMod = maxMagicDefMod,
        MinSpellcraft = minSpellcraft,
        MaxSpellcraft = maxSpellcraft,
        MinDifficulty = minDifficulty,
        MaxDifficulty = maxDifficulty,
        MinWarMagicMod = minWarMagicMod,
        MaxWarMagicMod = maxWarMagicMod,
        MinLifeMagicMod = minLifeMagicMod,
        MaxLifeMagicMod = maxLifeMagicMod,
        MinArmorLevel = minArmorLevel,
        MaxArmorLevel = maxArmorLevel,
        MinWardLevel = minWardLevel,
        MaxWardLevel = maxWardLevel,
        MinArmorModVsSlash = minArmorModVsSlash,
        MaxArmorModVsSlash = maxArmorModVsSlash,
        MinArmorModVsPierce = minArmorModVsPierce,
        MaxArmorModVsPierce = maxArmorModVsPierce,
        MinArmorModVsBludgeon = minArmorModVsBludgeon,
        MaxArmorModVsBludgeon = maxArmorModVsBludgeon,
        MinArmorModVsCold = minArmorModVsCold,
        MaxArmorModVsCold = maxArmorModVsCold,
        MinArmorModVsFire = minArmorModVsFire,
        MaxArmorModVsFire = maxArmorModVsFire,
        MinArmorModVsAcid = minArmorModVsAcid,
        MaxArmorModVsAcid = maxArmorModVsAcid,
        MinArmorModVsElectric = minArmorModVsElectric,
        MaxArmorModVsElectric = maxArmorModVsElectric,
        MinArmorWarMagicMod = minArmorWarMagicMod,
        MaxArmorWarMagicMod = maxArmorWarMagicMod,
        MinArmorLifeMagicMod = minArmorLifeMagicMod,
        MaxArmorLifeMagicMod = maxArmorLifeMagicMod,
        MinArmorManaRegenMod = minArmorManaRegenMod,
        MaxArmorManaRegenMod = maxArmorManaRegenMod,
        MinArmorManaConversionMod = minArmorManaConversionMod,
        MaxArmorManaConversionMod = maxArmorManaConversionMod,
        MinArmorAttackMod = minArmorAttackMod,
        MaxArmorAttackMod = maxArmorAttackMod,
        MinArmorShieldMod = minArmorShieldMod,
        MaxArmorShieldMod = maxArmorShieldMod,
        MinArmorDualWieldMod = minArmorDualWieldMod,
        MaxArmorDualWieldMod = maxArmorDualWieldMod,
        MinArmorThieveryMod = minArmorThieveryMod,
        MaxArmorThieveryMod = maxArmorThieveryMod,
        MinArmorRunMod = minArmorRunMod,
        MaxArmorRunMod = maxArmorRunMod,
        MinArmorStaminaRegenMod = minArmorStaminaRegenMod,
        MaxArmorStaminaRegenMod = maxArmorStaminaRegenMod,
        MinArmorPhysicalDefMod = minArmorPhysicalDefMod,
        MaxArmorPhysicalDefMod = maxArmorPhysicalDefMod,
        MinArmorMagicDefMod = minArmorMagicDefMod,
        MaxArmorMagicDefMod = maxArmorMagicDefMod,
        MinArmorTwohandedCombatMod = minArmorTwohandedCombatMod,
        MaxArmorTwohandedCombatMod = maxArmorTwohandedCombatMod,
        MinArmorHealthRegenMod = minArmorHealthRegenMod,
        MaxArmorHealthRegenMod = maxArmorHealthRegenMod,
        MinArmorPerceptionMod = minArmorPerceptionMod,
        MaxArmorPerceptionMod = maxArmorPerceptionMod,
        MinArmorDeceptionMod = minArmorDeceptionMod,
        MaxArmorDeceptionMod = maxArmorDeceptionMod,
        JewelrySlot = jewelrySlot,
        SalvageTargetType = salvageTargetType,
        SalvageMaterial = salvageMaterial,
        SortBy = sortBy,
        SortDir = sortDir,
        Page = page ?? 1,
        PageSize = pageSize ?? 25,
    };

    var result = await repository.SearchListingsAsync(request, ct);
    return Results.Ok(result);
});

app.MapGet("/api/leaderboards/level", async (int? limit, LeaderboardRepository repository, CancellationToken ct) =>
    Results.Ok(await repository.GetTopByLevelAsync(limit ?? 50, ct)));

app.MapGet("/api/leaderboards/deaths", async (int? limit, LeaderboardRepository repository, CancellationToken ct) =>
    Results.Ok(await repository.GetTopByDeathsAsync(limit ?? 50, ct)));

app.MapGet("/api/leaderboards/allegiance-rank", async (int? limit, LeaderboardRepository repository, CancellationToken ct) =>
    Results.Ok(await repository.GetTopByAllegianceRankAsync(limit ?? 50, ct)));

app.MapGet("/api/leaderboards/town-attunements", async (int? limit, LeaderboardRepository repository, CancellationToken ct) =>
    Results.Ok(await repository.GetTopByTownAttunementsAsync(limit ?? 50, ct)));

app.MapGet("/api/leaderboards/dangerous-creatures", async (int? limit, LeaderboardRepository repository, CancellationToken ct) =>
    Results.Ok(await repository.GetMostDangerousCreaturesAsync(limit ?? 50, ct)));

app.MapGet("/api/leaderboards/trade-skills", async (LeaderboardRepository repository, CancellationToken ct) =>
    Results.Ok(await repository.GetTradeSkillLeadersAsync(ct)));

app.Run();
