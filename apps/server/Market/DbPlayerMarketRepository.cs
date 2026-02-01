using System;
using System.Collections.Generic;
using System.Linq;
using ACE.Database.Models.Shard;
using ACE.Server.WorldObjects;
using Microsoft.EntityFrameworkCore;

namespace ACE.Server.Market;

/// <summary>
/// Shard-database backed implementation of IPlayerMarketRepository.
/// </summary>
public sealed class DbPlayerMarketRepository : IPlayerMarketRepository
{
    public PlayerMarketListing CreateListingFromWorldObject(
        Player seller,
        WorldObject item,
        int listedPrice,
        MarketCurrencyType currencyType,
        int vendorTier,
        int? wieldReq,
        int? itemTier,
        string inscription)
    {
        var now = DateTime.UtcNow;

        using var context = new ShardDbContext();

        var listing = new PlayerMarketListing
        {
            SellerAccountId = seller.Character.AccountId,
            SellerCharacterId = seller.Character.Id,
            SellerName = seller.Name,
            ItemGuid = item.Guid.Full,
            ItemBiotaId = (uint)item.Biota.Id,
            ItemWeenieClassId = item.WeenieClassId,
            OriginalValue = item.Value ?? 0,
            ListedPrice = listedPrice,
            CurrencyType = (int)currencyType,
            MarketVendorTier = vendorTier,
            ItemTier = itemTier,
            WieldReq = wieldReq,
            Inscription = inscription,
            OriginalInscription = item.GetProperty(ACE.Entity.Enum.Properties.PropertyString.Inscription),
            CreatedAtUtc = now,
            ExpiresAtUtc = now + MarketServiceLocator.Config.ListingLifetime,
            IsCancelled = false,
            IsSold = false
        };

        context.PlayerMarketListings.Add(listing);
        context.SaveChanges();

        return listing;
    }

    public IEnumerable<PlayerMarketListing> GetListingsForVendorTier(int vendorTier, DateTime nowUtc)
    {
        using var context = new ShardDbContext();

        return context.PlayerMarketListings
            .AsNoTracking()
            .Where(l =>
                // Prefer ItemTier-based routing when available.
                // Fall back to MarketVendorTier for older listings where ItemTier is null.
                ((l.ItemTier.HasValue && l.ItemTier.Value == vendorTier)
                 || (!l.ItemTier.HasValue && l.MarketVendorTier == vendorTier))
                && !l.IsSold
                && !l.IsCancelled
                && l.ExpiresAtUtc > nowUtc)
            .ToList();
    }

    public IEnumerable<PlayerMarketListing> GetListingsForAccount(uint accountId, DateTime nowUtc)
    {
        using var context = new ShardDbContext();

        return context.PlayerMarketListings
            .AsNoTracking()
            .Where(l =>
                l.SellerAccountId == accountId
                && !l.IsCancelled
                && !l.IsSold
                && l.ExpiresAtUtc > nowUtc)
            .ToList();
    }

    public PlayerMarketListing? GetListingByItemGuid(uint itemGuid)
    {
        using var context = new ShardDbContext();

        return context.PlayerMarketListings
            .AsNoTracking()
            .FirstOrDefault(l => l.ItemGuid == itemGuid);
    }

    public void MarkListingSold(PlayerMarketListing listing, Player buyer)
    {
        using var context = new ShardDbContext();

        var entity = context.PlayerMarketListings.SingleOrDefault(l => l.Id == listing.Id);
        if (entity == null)
        {
            return;
        }

        entity.IsSold = true;
        entity.SoldAtUtc = DateTime.UtcNow;
        entity.BuyerAccountId = buyer.Character.AccountId;
        entity.BuyerCharacterId = buyer.Character.Id;
        entity.BuyerName = buyer.Name;

        context.SaveChanges();
    }

    public void CancelListing(PlayerMarketListing listing)
    {
        using var context = new ShardDbContext();

        var entity = context.PlayerMarketListings.SingleOrDefault(l => l.Id == listing.Id);
        if (entity == null)
        {
            return;
        }

        entity.IsCancelled = true;
        context.SaveChanges();
    }

    public void ExpireListings(DateTime nowUtc)
    {
        using var context = new ShardDbContext();

        var toExpire = context.PlayerMarketListings
            .Where(l => !l.IsSold && !l.IsCancelled && l.ExpiresAtUtc <= nowUtc)
            .ToList();

        if (toExpire.Count == 0)
        {
            return;
        }

        foreach (var listing in toExpire)
        {
            listing.IsCancelled = true;
        }

        context.SaveChanges();
    }

    public PlayerMarketPayout CreatePayout(PlayerMarketListing listing)
    {
        using var context = new ShardDbContext();

        var payout = new PlayerMarketPayout
        {
            ListingId = listing.Id,
            SellerAccountId = listing.SellerAccountId,
            SellerCharacterId = listing.SellerCharacterId,
            Amount = listing.ListedPrice,
            CurrencyType = listing.CurrencyType,
            Status = (int)MarketPayoutStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };

        context.PlayerMarketPayouts.Add(payout);
        context.SaveChanges();

        return payout;
    }

    public IEnumerable<PlayerMarketPayout> GetPendingPayouts(uint accountId)
    {
        using var context = new ShardDbContext();

        return context.PlayerMarketPayouts
            .AsNoTracking()
            .Where(p => p.SellerAccountId == accountId && p.Status == (int)MarketPayoutStatus.Pending)
            .ToList();
    }

    public void MarkPayoutClaimed(PlayerMarketPayout payout)
    {
        using var context = new ShardDbContext();

        var entity = context.PlayerMarketPayouts.SingleOrDefault(p => p.Id == payout.Id);
        if (entity == null)
        {
            return;
        }

        entity.Status = (int)MarketPayoutStatus.Claimed;
        entity.ClaimedAtUtc = DateTime.UtcNow;

        context.SaveChanges();
    }
}
