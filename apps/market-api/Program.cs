using System.Net;
using System.Threading.RateLimiting;
using ACE.MarketApi;
using Microsoft.AspNetCore.HttpOverrides;
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

// Behind a reverse proxy (Caddy in the Docker deployment) every connection comes from the proxy's own
// address, so without this the per-IP rate limit below would put the whole site in one bucket. Take the
// client address from X-Forwarded-For, but only when the request really came from a private-network
// peer (the proxy); a request from anywhere else can't set it, so it can't be used to dodge the limit.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 1;
    options.KnownProxies.Clear();
    options.KnownIPNetworks.Clear();
    options.KnownIPNetworks.Add(new System.Net.IPNetwork(IPAddress.Loopback, 8));
    options.KnownIPNetworks.Add(new System.Net.IPNetwork(IPAddress.Parse("10.0.0.0"), 8));
    options.KnownIPNetworks.Add(new System.Net.IPNetwork(IPAddress.Parse("172.16.0.0"), 12));
    options.KnownIPNetworks.Add(new System.Net.IPNetwork(IPAddress.Parse("192.168.0.0"), 16));
});

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

// Must come before the rate limiter, which reads the client address.
app.UseForwardedHeaders();

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
    string? sigilEffect,
    int? sigilMinLevel,
    int? sigilMaxLevel,
    double? minProcChance,
    double? maxProcChance,
    double? minCooldown,
    double? maxCooldown,
    double? minManaReserved,
    double? maxManaReserved,
    double? minStaminaReserved,
    double? maxStaminaReserved,
    double? minHealthReserved,
    double? maxHealthReserved,
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
    double? minArmorHealthMod,
    double? maxArmorHealthMod,
    double? minArmorManaMod,
    double? maxArmorManaMod,
    double? minArmorStaminaMod,
    double? maxArmorStaminaMod,
    int? minGearCritDamage,
    int? maxGearCritDamage,
    int? minGearCritDamageResist,
    int? maxGearCritDamageResist,
    int? minGearDamage,
    int? maxGearDamage,
    int? minGearDamageResist,
    int? maxGearDamageResist,
    int? minGearHealingBoost,
    int? maxGearHealingBoost,
    int? minGearMaxHealth,
    int? maxGearMaxHealth,
    string? jewelrySlot,
    string? salvageTargetType,
    string? salvageMaterial,
    string? consumableType,
    string? miscType,
    string? trophyType,
    int? minTrophyQuality,
    int? maxTrophyQuality,
    string? gemMaterial,
    bool? carvedJewelOnly,
    int? minGemQuality,
    int? maxGemQuality,
    string? scrollSchool,
    string? scrollSpellName,
    double? minWorkmanship,
    double? maxWorkmanship,
    int? minUnits,
    int? maxUnits,
    double? minPricePerUnit,
    double? maxPricePerUnit,
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
        SigilEffect = sigilEffect,
        SigilMinLevel = sigilMinLevel,
        SigilMaxLevel = sigilMaxLevel,
        MinProcChance = minProcChance,
        MaxProcChance = maxProcChance,
        MinCooldown = minCooldown,
        MaxCooldown = maxCooldown,
        MinManaReserved = minManaReserved,
        MaxManaReserved = maxManaReserved,
        MinStaminaReserved = minStaminaReserved,
        MaxStaminaReserved = maxStaminaReserved,
        MinHealthReserved = minHealthReserved,
        MaxHealthReserved = maxHealthReserved,
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
        MinArmorHealthMod = minArmorHealthMod,
        MaxArmorHealthMod = maxArmorHealthMod,
        MinArmorManaMod = minArmorManaMod,
        MaxArmorManaMod = maxArmorManaMod,
        MinArmorStaminaMod = minArmorStaminaMod,
        MaxArmorStaminaMod = maxArmorStaminaMod,
        MinGearCritDamage = minGearCritDamage,
        MaxGearCritDamage = maxGearCritDamage,
        MinGearCritDamageResist = minGearCritDamageResist,
        MaxGearCritDamageResist = maxGearCritDamageResist,
        MinGearDamage = minGearDamage,
        MaxGearDamage = maxGearDamage,
        MinGearDamageResist = minGearDamageResist,
        MaxGearDamageResist = maxGearDamageResist,
        MinGearHealingBoost = minGearHealingBoost,
        MaxGearHealingBoost = maxGearHealingBoost,
        MinGearMaxHealth = minGearMaxHealth,
        MaxGearMaxHealth = maxGearMaxHealth,
        JewelrySlot = jewelrySlot,
        SalvageTargetType = salvageTargetType,
        SalvageMaterial = salvageMaterial,
        ConsumableType = consumableType,
        MiscType = miscType,
        TrophyType = trophyType,
        MinTrophyQuality = minTrophyQuality,
        MaxTrophyQuality = maxTrophyQuality,
        GemMaterial = gemMaterial,
        CarvedJewelOnly = carvedJewelOnly,
        MinGemQuality = minGemQuality,
        MaxGemQuality = maxGemQuality,
        ScrollSchool = scrollSchool,
        ScrollSpellName = scrollSpellName,
        MinWorkmanship = minWorkmanship,
        MaxWorkmanship = maxWorkmanship,
        MinUnits = minUnits,
        MaxUnits = maxUnits,
        MinPricePerUnit = minPricePerUnit,
        MaxPricePerUnit = maxPricePerUnit,
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

// size = how many fight on each side (1 = 1v1, up to the game's 9-person fellowship); scaled picks
// the scaled-duel ladder instead of the raw one.
app.MapGet("/api/leaderboards/arena", async (int? size, bool? scaled, int? limit, LeaderboardRepository repository, CancellationToken ct) =>
{
    var teamSize = size ?? 1;
    if (teamSize < 1 || teamSize > 9)
    {
        return Results.BadRequest(new { error = "size must be between 1 and 9." });
    }

    return Results.Ok(await repository.GetArenaBoardAsync(teamSize, scaled ?? false, limit ?? 50, ct));
});

// The fastest completions of a capstone dungeon: dungeon is its website key (GlendenWoodDungeon), fellowshipSize
// is 1, 2, or 3 (3 or more) and difficulty is the Dungeon: Difficulty tier, 0 for none up to 20. 404 until the game
// server has created the tables its runs are recorded in, which the website shows as "not tracked yet".
app.MapGet("/api/leaderboards/capstone", async (string? dungeon, int? fellowshipSize, int? difficulty, int? limit, LeaderboardRepository repository, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(dungeon) || dungeon.Length > 40 || !dungeon.All(char.IsAsciiLetter))
    {
        return Results.BadRequest(new { error = "dungeon is required (for example GlendenWoodDungeon)." });
    }

    var size = fellowshipSize ?? 1;
    if (size < 1 || size > 3)
    {
        return Results.BadRequest(new { error = "fellowshipSize must be 1, 2, or 3 (3 or more)." });
    }

    var tier = difficulty ?? 0;
    if (tier < 0 || tier > 20)
    {
        return Results.BadRequest(new { error = "difficulty must be between 0 and 20." });
    }

    var rows = await repository.GetCapstoneBoardAsync(dungeon, size, tier, limit ?? 50, ct);
    return rows is null ? Results.NotFound() : Results.Ok(rows);
});

app.Run();
