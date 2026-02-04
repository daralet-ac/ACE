using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACE.Database;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Entity;
using ACE.Server.Factories;
using ACE.Server.Market;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Serilog;
using Timer = System.Timers.Timer;

namespace ACE.Server.Discord;

public static class MarketListingsPublisher
{
    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(MarketListingsPublisher));

    private const string SnapshotMarker = "[market-snapshot]";

    private static DiscordSocketClient? _client;

    private static readonly ConcurrentQueue<int> NewListingQueue = new();

    private static readonly ConcurrentDictionary<ulong, List<ulong>> SnapshotMessageIdsByThreadId = new();

    private sealed record MarketChannelTarget(string Name, ulong ChannelId, double UpdateIntervalSeconds);

    private sealed record TierTarget(
        MarketChannelTarget Target,
        int MarketTier,
        string Title);

    private static readonly IReadOnlyList<TierTarget> TierTargets =
    [
        new(new("MiscThread", 0, 90), 0, "Miscellaneous Wares"),
        new(new("Tier1Thread", 0, 90), 2, "Crude Wares"),
        new(new("Tier2Thread", 0, 90), 3, "Common Wares"),
        new(new("Tier3Thread", 0, 90), 4, "Fine Wares"),
        new(new("Tier4Thread", 0, 90), 5, "Superior Wares"),
        new(new("Tier5Thread", 0, 90), 6, "Exceptional Wares"),
        new(new("Tier6Thread", 0, 90), 7, "Exquisite Wares"),
        new(new("Tier7Thread", 0, 90), 8, "Peerless Wares"),
    ];

    private static MarketChannelTarget LiveTargetDefaults = new("MarketLiveListings", 0, 20);
    private static MarketChannelTarget? _liveTarget;

    public static void Initialize(DiscordSocketClient client, IConfiguration marketChannelsSection)
    {
        _client = client;

        HookNewListings();

        _liveTarget = TryReadTarget(LiveTargetDefaults, marketChannelsSection);
        if (_liveTarget != null)
        {
            var liveTimer = new Timer { AutoReset = true, Interval = Math.Max(1, _liveTarget.UpdateIntervalSeconds) * 1000 };
            liveTimer.Elapsed += async (_, _) => await FlushLiveListings();
            liveTimer.Start();
        }

        foreach (var tier in TierTargets)
        {
            var target = TryReadTarget(tier.Target, marketChannelsSection);
            if (target == null || target.ChannelId == 0)
            {
                continue;
            }

            var resolved = tier with { Target = target };

            var timer = new Timer { AutoReset = true, Interval = Math.Max(1, resolved.Target.UpdateIntervalSeconds) * 1000 };
            timer.Elapsed += async (_, _) => await UpdateSnapshot(resolved);
            timer.Start();

            // fire soon after startup
            _ = Task.Run(async () =>
            {
                await Task.Delay(2000);
                await UpdateSnapshot(resolved);
            });
        }
    }

    private static void HookNewListings()
    {
        // We hook into MarketServiceLocator's repository by wrapping CreateListingFromWorldObject
        // at the source (DbPlayerMarketRepository) using a lightweight global event.
        MarketListingEvents.ListingCreated += id => NewListingQueue.Enqueue(id);
    }

    private static MarketChannelTarget? TryReadTarget(MarketChannelTarget defaults, IConfiguration section)
    {
        var s = section.GetSection(defaults.Name);
        if (!s.Exists())
        {
            return null;
        }

        var channelId = s.GetValue<ulong>("ChannelId");
        var interval = s.GetValue<double>("UpdateInterval", defaults.UpdateIntervalSeconds);
        return new MarketChannelTarget(defaults.Name, channelId, interval);
    }

    private static async Task FlushLiveListings()
    {
        try
        {
            var client = _client;
            var live = _liveTarget;
            if (client == null || live == null || live.ChannelId == 0)
            {
                return;
            }

            var listingIds = new List<int>();
            while (NewListingQueue.TryDequeue(out var id))
            {
                listingIds.Add(id);
                if (listingIds.Count >= 50)
                {
                    break;
                }
            }

            if (listingIds.Count == 0)
            {
                return;
            }

            var channel = await client.GetChannelAsync(live.ChannelId) as IMessageChannel;
            if (channel == null)
            {
                return;
            }

            var now = DateTime.UtcNow;

            var lines = new List<string>();
            foreach (var id in listingIds.Distinct())
            {
                var listing = MarketServiceLocator.PlayerMarketRepository.GetListingById(id);
                if (listing == null)
                {
                    continue;
                }

                if (listing.IsSold || listing.IsCancelled || listing.ExpiresAtUtc <= now)
                {
                    continue;
                }

                lines.Add(FormatListingLine(listing, now));
            }

            if (lines.Count == 0)
            {
                return;
            }

            foreach (var page in BuildPages(lines, header: "**New Market Listings**"))
            {
                await channel.SendMessageAsync(page);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Market live listing flush failed");
        }
    }

    private static async Task UpdateSnapshot(TierTarget tier)
    {
        try
        {
            var client = _client;
            if (client == null || tier.Target.ChannelId == 0)
            {
                return;
            }

            var threadChannel = await client.GetChannelAsync(tier.Target.ChannelId) as IMessageChannel;
            if (threadChannel == null)
            {
                return;
            }

            var now = DateTime.UtcNow;
            var unixNow = new DateTimeOffset(now).ToUnixTimeSeconds();
            var listings = MarketServiceLocator.PlayerMarketRepository
                .GetListingsForVendorTier(tier.MarketTier, now)
                .ToList();
            var totalActive = listings.Count;

            var sorted = listings
                .Select(l => new { Listing = l, Sort = GetSortKey(l) })
                .OrderBy(x => x.Sort.WeenieType)
                .ThenBy(x => x.Sort.ItemType)
                .ThenBy(x => x.Sort.SubType)
                .ThenBy(x => x.Listing.ListedPrice)
                .ThenBy(x => x.Listing.Id)
                .ToList();

            var lines = new List<string>(sorted.Count);
            int? lastItemType = null;
            var haveListingInSection = false;
            foreach (var e in sorted)
            {
                if (lastItemType != e.Sort.ItemType)
                {
                    lastItemType = e.Sort.ItemType;
                    haveListingInSection = false;
                    var itemTypeValue = unchecked((uint)e.Sort.ItemType);
                    var label = Enum.IsDefined(typeof(ItemType), itemTypeValue)
                        ? ((ItemType)itemTypeValue).ToString()
                        : $"ItemType {e.Sort.ItemType}";
                    lines.Add($"### {label}");
                }

                if (haveListingInSection)
                {
                    lines.Add(string.Empty);
                }

                lines.Add(FormatListingLine(e.Listing, now));
                haveListingInSection = true;
            }
            if (lines.Count == 0)
            {
                lines.Add("(no active listings)");
            }

            var pages = BuildPages(lines, header: $"**{tier.Title} ({totalActive})**   *(updated <t:{unixNow}:f> | <t:{unixNow}:R>)*");

            var existing = await GetOrDiscoverSnapshotMessageIds(tier.Target.ChannelId, threadChannel);

            // edit existing pages
            var keepIds = new List<ulong>();
            for (var i = 0; i < pages.Count; i++)
            {
                var content = pages[i] + $"\n\n{SnapshotMarker}";
                if (i < existing.Count)
                {
                    var msg = await threadChannel.GetMessageAsync(existing[i]);
                    if (msg is IUserMessage um)
                    {
                        await um.ModifyAsync(p => p.Content = content);
                        keepIds.Add(existing[i]);
                        continue;
                    }
                }

                var created = await threadChannel.SendMessageAsync(content);
                keepIds.Add(created.Id);
            }

            // delete extra pages
            for (var i = keepIds.Count; i < existing.Count; i++)
            {
                try
                {
                    await threadChannel.DeleteMessageAsync(existing[i]);
                }
                catch
                {
                    // ignore
                }
            }

            SnapshotMessageIdsByThreadId[tier.Target.ChannelId] = keepIds;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Market snapshot update failed for {Tier}", tier.Title);
        }
    }

    private static async Task<List<ulong>> GetOrDiscoverSnapshotMessageIds(ulong threadId, IMessageChannel channel)
    {
        if (SnapshotMessageIdsByThreadId.TryGetValue(threadId, out var cached) && cached.Count > 0)
        {
            return cached;
        }

        var client = _client;
        if (client == null)
        {
            return [];
        }

        // Reuse existing: find most recent bot-authored messages, oldest-first, that look like our snapshot pages.
        // We only search a bounded recent window.
        var found = new List<IMessage>();
        var before = (ulong?)null;

        for (var batch = 0; batch < 5; batch++)
        {
            var msgs = before.HasValue
                ? await channel.GetMessagesAsync(before.Value, Direction.Before, 100).FlattenAsync()
                : await channel.GetMessagesAsync(100).FlattenAsync();
            var list = msgs.ToList();
            if (list.Count == 0)
            {
                break;
            }

            found.AddRange(list);
            before = list.Last().Id;
        }

        var botId = client.CurrentUser?.Id;
        if (botId == null)
        {
            return [];
        }

        var snapshotMsgs = found
            .Where(m => m.Author.Id == botId)
            .Where(m => m.Content.Contains(SnapshotMarker, StringComparison.Ordinal))
            .OrderBy(m => m.Timestamp)
            .Select(m => m.Id)
            .ToList();

        SnapshotMessageIdsByThreadId[threadId] = snapshotMsgs;
        return snapshotMsgs;
    }

    private static string FormatListingLine(ACE.Database.Models.Shard.PlayerMarketListing listing, DateTime now)
    {
        var name = ResolveItemName(listing);
        var expiresIn = listing.ExpiresAtUtc - now;
        if (expiresIn < TimeSpan.Zero)
        {
            expiresIn = TimeSpan.Zero;
        }

        var stackSize = ResolveStackSize(listing);
        var reqLabel = ResolveReqLabelFromWeenie(listing.ItemWeenieClassId);
        var wieldReq = listing.WieldReq.HasValue ? $"{reqLabel} {listing.WieldReq.Value}" : $"{reqLabel} -";
        var stackText = stackSize > 1 ? $"x{stackSize}" : "";

        var details = TryBuildItemDetails(listing, name, stackText, listing.ListedPrice, wieldReq, listing.SellerName, FormatRemaining(expiresIn));
        return details ?? $"• {name} {stackText} | {listing.ListedPrice:N0} py | {wieldReq} | {listing.SellerName} | expires in {FormatRemaining(expiresIn)}";
    }

    private static string? TryBuildItemDetails(
        ACE.Database.Models.Shard.PlayerMarketListing listing,
        string name,
        string stackText,
        int listedPrice,
        string wieldReq,
        string sellerName,
        string expiresIn)
    {
        var obj = TryRecreateListingWorldObject(listing);
        if (obj == null)
        {
            return null;
        }

        try
        {
            var header = $"- {name} {stackText} | {listedPrice:N0} py";

            var commonParts = new List<string>(8) { wieldReq };
            AppendCommonItemParts(obj, commonParts);

            var jewelSockets = obj.GetProperty(PropertyInt.JewelSockets);
            if (jewelSockets.HasValue && jewelSockets.Value > 0)
            {
                commonParts.RemoveAll(p => p.StartsWith("Sockets ", StringComparison.Ordinal));
                commonParts.Add($"Sockets {jewelSockets.Value}");
            }

            if (obj.ItemType is ItemType.Weapon or ItemType.MeleeWeapon or ItemType.MissileWeapon or ItemType.Caster)
            {
                var lines = FormatWeaponDetailsMultiline(obj);
                var allLines = new List<string>(lines.Count + 1);
                var commonLine = new List<string>(1);
                AddIndentedLine(commonLine, commonParts);

                if (commonLine.Count > 0)
                {
                    allLines.Add(commonLine[0]);
                }
                allLines.AddRange(lines);

                allLines = allLines.Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
                var body = allLines.Count > 0 ? ("\n" + string.Join("\n", allLines)) : string.Empty;
                return header + body + $"\n  Seller: {sellerName} | expires in {expiresIn}";
            }

            if (obj.ItemType is ItemType.Armor)
            {
                var lines = FormatArmorDetailsMultiline(obj);
                var allLines = new List<string>(lines.Count + 1);
                var commonLine = new List<string>(1);
                AddIndentedLine(commonLine, commonParts);

                if (commonLine.Count > 0)
                {
                    allLines.Add(commonLine[0]);
                }
                allLines.AddRange(lines);

                allLines = allLines.Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
                var body = allLines.Count > 0 ? ("\n" + string.Join("\n", allLines)) : string.Empty;
                return header + body + $"\n  Seller: {sellerName} | expires in {expiresIn}";
            }

            if (obj.ItemType is ItemType.Jewelry)
            {
                var lines = FormatJewelryDetailsMultiline(obj);
                var allLines = new List<string>(lines.Count + 1);
                var commonLine = new List<string>(1);
                AddIndentedLine(commonLine, commonParts);

                if (commonLine.Count > 0)
                {
                    allLines.Add(commonLine[0]);
                }
                allLines.AddRange(lines);

                allLines = allLines.Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
                var body = allLines.Count > 0 ? ("\n" + string.Join("\n", allLines)) : string.Empty;
                return header + body + $"\n  Seller: {sellerName} | expires in {expiresIn}";
            }

            var details = obj.ItemType switch
            {
                _ => obj.WeenieClassName != null && obj.WeenieClassName.Contains("sigil", StringComparison.OrdinalIgnoreCase)
                    ? FormatSigilTrinketDetails(obj)
                    : null
            };

            var detailsText = string.IsNullOrWhiteSpace(details) ? "" : $" | {details}";
            var reqText = string.IsNullOrWhiteSpace(wieldReq) ? string.Empty : $" | {wieldReq}";
            return $"{header}{detailsText}{reqText} | {sellerName} | expires in {expiresIn}";
        }
        catch
        {
            return null;
        }
        finally
        {
            obj.Destroy();
        }
    }

    private static ACE.Server.WorldObjects.WorldObject? TryRecreateListingWorldObject(ACE.Database.Models.Shard.PlayerMarketListing listing)
    {
        if (listing.ItemBiotaId <= 0)
        {
            return null;
        }

        try
        {
            var biota = DatabaseManager.Shard.BaseDatabase.GetBiota(listing.ItemBiotaId, true);
            if (biota == null)
            {
                if (!string.IsNullOrWhiteSpace(listing.ItemSnapshotJson))
                {
                    return MarketListingSnapshotSerializer.TryRecreateWorldObjectFromSnapshot(listing.ItemSnapshotJson);
                }

                return null;
            }

            var entityBiota = Database.Adapter.BiotaConverter.ConvertToEntityBiota(biota);
            return WorldObjectFactory.CreateWorldObject(entityBiota);
        }
        catch
        {
            if (!string.IsNullOrWhiteSpace(listing.ItemSnapshotJson))
            {
                return MarketListingSnapshotSerializer.TryRecreateWorldObjectFromSnapshot(listing.ItemSnapshotJson);
            }

            return null;
        }
    }

    private static List<string> FormatWeaponDetailsMultiline(ACE.Server.WorldObjects.WorldObject obj)
    {
        // line 1 - Damage stats
        var lines = new List<string>(5);
        var line1 = new List<string>(6);
        var element = obj.GetProperty(PropertyInt.DamageType);
        if (element.HasValue)
        {
            line1.Add(((DamageType)element.Value).ToString());
        }

        // Show weapon damage as a range using variance: min = max * (1 - variance).
        var dmg = obj.GetProperty(PropertyInt.Damage);
        var variance = obj.GetProperty(PropertyFloat.DamageVariance);
        if (dmg.HasValue && dmg.Value != 0)
        {
            if (variance.HasValue)
            {
                var max = dmg.Value;
                var min = (int)Math.Round(max * (1.0 - variance.Value), MidpointRounding.AwayFromZero);
                min = Math.Clamp(min, 0, max);
                line1.Add($"Dmg {min}-{max}");
            }
            else
            {
                line1.Add($"Dmg {dmg.Value}");
            }
        }

        AppendPropertyFloatIfPresent(obj, line1, PropertyFloat.DamageMod, "D.Mod%", multiplyBy100: true, skipIfZero: true);

        if (obj.WeaponTime.HasValue)
        {
            line1.Add($"Speed {obj.WeaponTime.Value}");
        }

        if (line1.Count == 0)
        {
            line1.Add("Dmg -");
        }

        AddIndentedLine(lines, line1);

        // line 3 - Atk, Pdef, Mdef
        var line3 = new List<string>(10);
        AppendWeaponMultiplierAsBonusPercentIfPresent(obj, line3, PropertyFloat.WeaponOffense, "Atk");
        AppendWeaponMultiplierAsBonusPercentIfPresent(obj, line3, PropertyFloat.WeaponPhysicalDefense, "P.Def");
        AppendWeaponMultiplierAsBonusPercentIfPresent(obj, line3, PropertyFloat.WeaponMagicalDefense, "M.Def");
        AddIndentedLine(lines, line3);

        // line 4 - Imbue, critmulti, critfreq, ignorearmor, resistmod
        var line4 = new List<string>(10);
        var imbue = obj.GetImbuedEffects();
        if (imbue != ImbuedEffectType.Undef)
        {
            line4.Add($"Imbue {imbue}");
        }
        AppendWeaponCritMultiplierIfPresent(obj, line4);
        AppendWeaponCritFrequencyIfPresent(obj, line4);
        AppendWeaponIgnoreArmorIfPresent(obj, line4);
        AppendWeaponResistanceModIfPresent(obj, line4);
        AddIndentedLine(lines, line4);

        // line 5 - itemdifficulty, item spellcraft, proc spell, proc spec chance, spells
        var line5 = new List<string>(12);
        AppendPropertyIntIfPresent(obj, line5, PropertyInt.ItemDifficulty, "Diff");
        if (obj.ItemSpellcraft.HasValue)
        {
            line5.Add($"Spellcraft {obj.ItemSpellcraft.Value}");
        }
        var procSpell = obj.GetProperty(PropertyDataId.ProcSpell);
        if (procSpell.HasValue)
        {
            line5.Add($"Proc {new Spell((SpellId)procSpell.Value).Name}");
        }
        AppendPropertyFloatIfPresent(obj, line5, PropertyFloat.ProcSpellRate, "Proc%", multiplyBy100: true);

        var spellText = TryFormatSpells(obj);
        if (!string.IsNullOrWhiteSpace(spellText))
        {
            line5.Add(spellText);
        }

        AddIndentedLine(lines, line5);

        return lines;
    }

    private static List<string> FormatArmorDetailsMultiline(ACE.Server.WorldObjects.WorldObject obj)
    {
        var lines = new List<string>(6);

        var line1 = new List<string>(6);
        if (obj.ArmorLevel.HasValue)
        {
            line1.Add($"AL {obj.ArmorLevel.Value}");
        }
        if (obj.WardLevel.HasValue)
        {
            line1.Add($"WL {obj.WardLevel.Value}");
        }
        AddIndentedLine(lines, line1);

        var line2 = new List<string>(12);
        AppendPropertyFloatIfPresent(obj, line2, PropertyFloat.ArmorPhysicalDefMod, "P.Def", multiplyBy100: true, skipIfZero: true);
        AppendPropertyFloatIfPresent(obj, line2, PropertyFloat.ArmorMagicDefMod, "M.Def", multiplyBy100: true, skipIfZero: true);
        AppendPropertyFloatIfPresent(obj, line2, PropertyFloat.ArmorAttackMod, "Atk", multiplyBy100: true, skipIfZero: true);
        AppendPropertyFloatIfPresent(obj, line2, PropertyFloat.ArmorWarMagicMod, "War", multiplyBy100: true, skipIfZero: true);
        AppendPropertyFloatIfPresent(obj, line2, PropertyFloat.ArmorLifeMagicMod, "Life", multiplyBy100: true, skipIfZero: true);
        AddIndentedLine(lines, line2);

        var line3 = new List<string>(16);
        AppendPropertyFloatIfPresent(obj, line3, PropertyFloat.ArmorDualWieldMod, "Dual", multiplyBy100: true, skipIfZero: true);
        AppendPropertyFloatIfPresent(obj, line3, PropertyFloat.ArmorRunMod, "Run", multiplyBy100: true, skipIfZero: true);
        AppendPropertyFloatIfPresent(obj, line3, PropertyFloat.ArmorHealthRegenMod, "H.Regen", multiplyBy100: true, skipIfZero: true);
        AppendPropertyFloatIfPresent(obj, line3, PropertyFloat.ArmorStaminaRegenMod, "S.Regen", multiplyBy100: true, skipIfZero: true);
        AppendPropertyFloatIfPresent(obj, line3, PropertyFloat.ArmorManaRegenMod, "M.Regen", multiplyBy100: true, skipIfZero: true);
        AppendPropertyFloatIfPresent(obj, line3, PropertyFloat.ArmorShieldMod, "Shield", multiplyBy100: true, skipIfZero: true);
        AppendPropertyFloatIfPresent(obj, line3, PropertyFloat.ArmorPerceptionMod, "Perc", multiplyBy100: true, skipIfZero: true);
        AppendPropertyFloatIfPresent(obj, line3, PropertyFloat.ArmorThieveryMod, "Thiev", multiplyBy100: true, skipIfZero: true);
        AppendPropertyFloatIfPresent(obj, line3, PropertyFloat.ArmorDeceptionMod, "Decep", multiplyBy100: true, skipIfZero: true);
        AddIndentedLine(lines, line3);

        var line4 = new List<string>(12);
        AppendPropertyIntIfPresent(obj, line4, PropertyInt.ItemDifficulty, "Diff");

        var spellText = TryFormatSpells(obj);
        if (!string.IsNullOrWhiteSpace(spellText))
        {
            line4.Add(spellText);
        }

        AddIndentedLine(lines, line4);

        return lines;
    }

    private static List<string> FormatJewelryDetailsMultiline(ACE.Server.WorldObjects.WorldObject obj)
    {
        var lines = new List<string>(5);

        var line1 = new List<string>(4);
        if (obj.WardLevel.HasValue)
        {
            line1.Add($"Ward {obj.WardLevel.Value}");
        }
        AddIndentedLine(lines, line1);

        var line2 = new List<string>(10);
        AppendPropertyIntIfPresent(obj, line2, PropertyInt.DamageRating, "DR", skipIfZero: true);
        AppendPropertyIntIfPresent(obj, line2, PropertyInt.DamageResistRating, "DRR", skipIfZero: true);
        AppendPropertyIntIfPresent(obj, line2, PropertyInt.CritRating, "CR", skipIfZero: true);
        AppendPropertyIntIfPresent(obj, line2, PropertyInt.CritDamageRating, "CDR", skipIfZero: true);
        AppendPropertyIntIfPresent(obj, line2, PropertyInt.CritResistRating, "CRR", skipIfZero: true);
        AppendPropertyIntIfPresent(obj, line2, PropertyInt.CritDamageResistRating, "CDRR", skipIfZero: true);
        AddIndentedLine(lines, line2);

        var line3 = new List<string>(8);
        AppendPropertyIntIfPresent(obj, line3, PropertyInt.HealingBoostRating, "HBR", skipIfZero: true);
        AppendPropertyIntIfPresent(obj, line3, PropertyInt.WeaknessRating, "Weak", skipIfZero: true);
        AppendPropertyIntIfPresent(obj, line3, PropertyInt.NetherResistRating, "NRR", skipIfZero: true);
        AddIndentedLine(lines, line3);

        var line4 = new List<string>(12);
        AppendPropertyIntIfPresent(obj, line4, PropertyInt.ItemDifficulty, "Diff");
        if (obj.ItemSpellcraft.HasValue)
        {
            line4.Add($"Spellcraft {obj.ItemSpellcraft.Value}");
        }
        var procSpell = obj.GetProperty(PropertyDataId.ProcSpell);
        if (procSpell.HasValue)
        {
            line4.Add($"Proc {new Spell((SpellId)procSpell.Value).Name}");
        }
        AppendPropertyFloatIfPresent(obj, line4, PropertyFloat.ProcSpellRate, "Proc%", multiplyBy100: true);

        var spellText = TryFormatSpells(obj);
        if (!string.IsNullOrWhiteSpace(spellText))
        {
            line4.Add(spellText);
        }

        AddIndentedLine(lines, line4);

        return lines;
    }

    private static void AddIndentedLine(List<string> lines, List<string> parts)
    {
        if (parts.Count == 0)
        {
            return;
        }

        var text = string.Join(" | ", parts).Trim();
        if (text.Length == 0)
        {
            return;
        }

        lines.Add("  - " + text);
    }

    private static void AppendWeaponMultiplierAsBonusPercentIfPresent(
        ACE.Server.WorldObjects.WorldObject obj,
        List<string> parts,
        PropertyFloat prop,
        string label)
    {
        var val = obj.GetProperty(prop);
        if (!val.HasValue)
        {
            return;
        }

        var bonusPct = (val.Value - 1.0) * 100.0;
        bonusPct = Math.Round(bonusPct, 1, MidpointRounding.AwayFromZero);
        parts.Add($"{label} {bonusPct:+0.0;-0.0;0.0}%");
    }

    private static string? TryFormatSpells(ACE.Server.WorldObjects.WorldObject obj)
    {
        try
        {
            if (obj.Biota?.PropertiesSpellBook != null)
            {
                var spellIds = obj.Biota.PropertiesSpellBook.Keys.OrderBy(id => id).ToList();
                if (spellIds.Count > 0)
                {
                    var names = new List<string>(spellIds.Count);
                    foreach (var id in spellIds)
                    {
                        try
                        {
                            names.Add(new Spell(id, loadDB: false).Name);
                        }
                        catch
                        {
                            names.Add($"Spell {id}");
                        }
                    }

                    return "Spells: " + string.Join(", ", names);
                }
            }
        }
        catch
        {
            // ignore
        }

        return null;
    }

    private static void AppendWeaponCritMultiplierIfPresent(ACE.Server.WorldObjects.WorldObject obj, List<string> parts)
    {
        var val = obj.GetProperty(PropertyFloat.CriticalMultiplier);
        if (!val.HasValue)
        {
            return;
        }

        // Display as additive +% over 1.0 (e.g., 1.40 => +40%).
        var bonusPct = (val.Value - 1.0) * 100.0;
        parts.Add($"Crushing Blow {bonusPct:+0.##;-0.##;0}%");
    }

    private static void AppendWeaponCritFrequencyIfPresent(ACE.Server.WorldObjects.WorldObject obj, List<string> parts)
    {
        var val = obj.GetProperty(PropertyFloat.CriticalFrequency);
        if (!val.HasValue || val <= 0.1f)
        {
            return;
        }

        // Display as additive +% over 0.1 (e.g., 0.25 => +15%).
        var bonusPct = (val.Value - 0.1) * 100.0;
        parts.Add($"Biting Strike {bonusPct:+0.##;-0.##;0}%");
    }

    private static void AppendWeaponIgnoreArmorIfPresent(ACE.Server.WorldObjects.WorldObject obj, List<string> parts)
    {
        var val = obj.GetProperty(PropertyFloat.IgnoreArmor);
        if (!val.HasValue)
        {
            return;
        }

        var pct = (1 - val.Value) * 100.0;
        parts.Add($"Armor Cleave {pct:0.##}%");
    }

    private static void AppendWeaponResistanceModIfPresent(ACE.Server.WorldObjects.WorldObject obj, List<string> parts)
    {
        var val = obj.GetProperty(PropertyFloat.ResistanceModifier);
        if (!val.HasValue)
        {
            return;
        }

        var pct = val.Value * 100.0;
        parts.Add($"Resist Cleave {pct:0.##}%");
    }

    private static string? FormatJewelryDetails(ACE.Server.WorldObjects.WorldObject obj)
    {
        var parts = new List<string>(10);

        AppendCommonItemParts(obj, parts);

        if (obj.WardLevel.HasValue)
        {
            parts.Add($"Ward {obj.WardLevel.Value}");
        }

        // Ratings (if present) - keep labels short for Discord.
        AppendPropertyIntIfPresent(obj, parts, PropertyInt.DamageRating, "DR");
        AppendPropertyIntIfPresent(obj, parts, PropertyInt.DamageResistRating, "DRR");
        AppendPropertyIntIfPresent(obj, parts, PropertyInt.CritRating, "CR");
        AppendPropertyIntIfPresent(obj, parts, PropertyInt.CritDamageRating, "CDR");
        AppendPropertyIntIfPresent(obj, parts, PropertyInt.CritResistRating, "CRR");
        AppendPropertyIntIfPresent(obj, parts, PropertyInt.CritDamageResistRating, "CDRR");
        AppendPropertyIntIfPresent(obj, parts, PropertyInt.HealingBoostRating, "HBR");
        AppendPropertyIntIfPresent(obj, parts, PropertyInt.WeaknessRating, "Weak");
        AppendPropertyIntIfPresent(obj, parts, PropertyInt.NetherResistRating, "NRR");

        return JoinParts(parts);
    }

    private static string? FormatSigilTrinketDetails(ACE.Server.WorldObjects.WorldObject obj)
    {
        var parts = new List<string>(10);
        AppendCommonItemParts(obj, parts);

        var type = obj.GetProperty(PropertyInt.SigilTrinketType);
        var skill = obj.GetProperty(PropertyInt.SigilTrinketSkill);
        var effect = obj.GetProperty(PropertyInt.SigilTrinketEffectId);
        var maxTier = obj.GetProperty(PropertyInt.SigilTrinketMaxTier);
        if (type.HasValue)
        {
            parts.Add($"Type {type.Value}");
        }
        if (skill.HasValue)
        {
            parts.Add($"Skill {skill.Value}");
        }
        if (effect.HasValue)
        {
            parts.Add($"Fx {effect.Value}");
        }
        if (maxTier.HasValue)
        {
            parts.Add($"MaxT {maxTier.Value}");
        }

        AppendPropertyFloatIfPresent(obj, parts, PropertyFloat.SigilTrinketTriggerChance, "Trig%", multiplyBy100: true);
        AppendPropertyFloatIfPresent(obj, parts, PropertyFloat.SigilTrinketCooldown, "CD");
        if (obj.ItemLevel.HasValue)
        {
            parts.Add($"IL {obj.ItemLevel.Value}");
        }

        return JoinParts(parts);
    }

    private static void AppendCommonItemParts(ACE.Server.WorldObjects.WorldObject obj, List<string> parts)
    {
        if (obj.Workmanship.HasValue)
        {
            parts.Add($"Wk {obj.Workmanship.Value:0.##}");
        }

        var sockets = obj.JewelSockets;
        if (sockets.HasValue && sockets.Value > 0)
        {
            parts.Add($"Sockets {sockets.Value}");
        }

        var tink = obj.NumTimesTinkered;
        if (tink > 0)
        {
            parts.Add($"Tinks {tink}");
        }
    }

    private static void AppendPropertyFloatIfPresent(
        ACE.Server.WorldObjects.WorldObject obj,
        List<string> parts,
        PropertyFloat prop,
        string label,
        bool multiplyBy100 = false,
        bool skipIfZero = false)
    {
        var val = obj.GetProperty(prop);
        if (!val.HasValue)
        {
            return;
        }

        if (skipIfZero && Math.Abs(val.Value) < 0.001)
        {
            return;
        }

        var outVal = multiplyBy100 ? val.Value * 100.0 : val.Value;
        outVal = Math.Round(outVal, 1, MidpointRounding.AwayFromZero);
        parts.Add($"{label} {outVal:+0.0;-0.0;0.0}%");
    }

    private static void AppendPropertyIntIfPresent(
        ACE.Server.WorldObjects.WorldObject obj,
        List<string> parts,
        PropertyInt prop,
        string label,
        bool skipIfZero = false)
    {
        var val = obj.GetProperty(prop);
        if (!val.HasValue)
        {
            return;
        }

        if (skipIfZero && val.Value == 0)
        {
            return;
        }

        parts.Add($"{label} {val.Value}");
    }

    private static string? JoinParts(List<string> parts)
    {
        if (parts.Count == 0)
        {
            return null;
        }

        return string.Join(", ", parts);
    }

    private static int ResolveStackSize(ACE.Database.Models.Shard.PlayerMarketListing listing)
    {
        if (listing.ItemBiotaId <= 0)
        {
            return 1;
        }

        try
        {
            var biota = DatabaseManager.Shard.BaseDatabase.GetBiota(listing.ItemBiotaId, true);
            var stack = biota?.BiotaPropertiesInt?.FirstOrDefault(p => p.Type == (ushort)PropertyInt.StackSize)?.Value;
            return stack.HasValue && stack.Value > 0 ? stack.Value : 1;
        }
        catch
        {
            return 1;
        }
    }

    private static string ResolveItemName(ACE.Database.Models.Shard.PlayerMarketListing listing)
    {
        if (listing.ItemBiotaId > 0)
        {
            try
            {
                var biota = DatabaseManager.Shard.BaseDatabase.GetBiota(listing.ItemBiotaId, true);
                var name = biota?.BiotaPropertiesString?.FirstOrDefault(p => p.Type == (ushort)PropertyString.Name)?.Value;
                if (!string.IsNullOrWhiteSpace(name))
                {
                    return name;
                }
            }
            catch
            {
                // ignore
            }
        }

        var weenie = DatabaseManager.World.GetCachedWeenie(listing.ItemWeenieClassId);
        if (weenie?.PropertiesString != null
            && weenie.PropertiesString.TryGetValue(PropertyString.Name, out var weenieName)
            && !string.IsNullOrWhiteSpace(weenieName))
        {
            return weenieName;
        }

        return $"WCID {listing.ItemWeenieClassId}";
    }

    private static string FormatRemaining(TimeSpan remaining)
    {
        if (remaining.TotalDays >= 1)
        {
            return $"{(int)Math.Floor(remaining.TotalDays)}d {remaining.Hours}h";
        }

        if (remaining.TotalHours >= 1)
        {
            return $"{(int)Math.Floor(remaining.TotalHours)}h {remaining.Minutes}m";
        }

        if (remaining.TotalMinutes >= 1)
        {
            return $"{(int)Math.Floor(remaining.TotalMinutes)}m {remaining.Seconds}s";
        }

        return $"{Math.Max(0, remaining.Seconds)}s";
    }

    private static string ResolveReqLabelFromWeenie(uint wcid)
    {
        try
        {
            var weenie = DatabaseManager.World.GetCachedWeenie(wcid);
            if (weenie?.PropertiesInt != null && weenie.PropertiesInt.TryGetValue(PropertyInt.ItemType, out var it))
            {
                if (it == (int)ItemType.Jewelry || it == (int)ItemType.Clothing)
                {
                    return "L.Req";
                }
            }
        }
        catch
        {
            // ignore
        }

        return "W.Req";
    }

    private static List<string> BuildPages(List<string> lines, string header)
    {
        const int maxLen = 1800;

        var pages = new List<string>();
        var sb = new StringBuilder();

        void Flush()
        {
            if (sb.Length == 0)
            {
                return;
            }

            pages.Add(sb.ToString().TrimEnd());
            sb.Clear();
        }

        sb.AppendLine(header);
        sb.AppendLine();

        foreach (var line in lines)
        {
            var toAdd = line + "\n";
            if (sb.Length + toAdd.Length > maxLen)
            {
                Flush();
                sb.AppendLine(header);
                sb.AppendLine();
            }

            sb.Append(toAdd);
        }

        Flush();

        return pages.Count == 0 ? [header] : pages;
    }

    private sealed record ListingSortKey(int WeenieType, int ItemType, int SubType);

    private static ListingSortKey GetSortKey(ACE.Database.Models.Shard.PlayerMarketListing listing)
    {
        // Cheap path: use cached weenie info only (no Biota/worldobject recreation).
        var weenie = DatabaseManager.World.GetCachedWeenie(listing.ItemWeenieClassId);
        if (weenie == null)
        {
            return new ListingSortKey(int.MaxValue, int.MaxValue, int.MaxValue);
        }

        var weenieTypeInt = (int)weenie.WeenieType;

        var itemTypeInt = int.MaxValue;
        if (weenie.PropertiesInt != null && weenie.PropertiesInt.TryGetValue(PropertyInt.ItemType, out var it))
        {
            itemTypeInt = it;
        }

        var subType = 0;
        if (itemTypeInt is (int)ItemType.Weapon or (int)ItemType.MeleeWeapon or (int)ItemType.MissileWeapon or (int)ItemType.Caster)
        {
            if (weenie.PropertiesInt != null && weenie.PropertiesInt.TryGetValue(PropertyInt.WeaponSkill, out var ws))
            {
                subType = ws;
            }
        }
        else if (itemTypeInt == (int)ItemType.Armor)
        {
            if (weenie.PropertiesInt != null && weenie.PropertiesInt.TryGetValue(PropertyInt.ArmorType, out var at))
            {
                subType = at;
            }
        }

        return new ListingSortKey(weenieTypeInt, itemTypeInt, subType);
    }

    // Intentionally no expensive fallback.
}
