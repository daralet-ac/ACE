using System;
using System.Collections.Generic;
using System.Linq;
using ACE.Database.Models.Shard;
using ACE.Server.Market;
using ACE.Server.WorldObjects;

public interface IPlayerMarketRepository
{
    PlayerMarketListing CreateListingFromWorldObject(
        Player seller,
        WorldObject item,
        int listedPrice,
        MarketCurrencyType currencyType,
        int vendorTier,
        int? wieldReq,
        int? itemTier,
        string inscription);

    IEnumerable<PlayerMarketListing> GetListingsForVendorTier(int vendorTier, DateTime nowUtc);

    IEnumerable<PlayerMarketListing> GetListingsForAccount(uint accountId, DateTime nowUtc);

    PlayerMarketListing? GetListingByItemGuid(uint itemGuid);

    void MarkListingSold(PlayerMarketListing listing, Player buyer);

    void CancelListing(PlayerMarketListing listing);

    void ExpireListings(DateTime nowUtc);

    PlayerMarketPayout CreatePayout(PlayerMarketListing listing);

    IEnumerable<PlayerMarketPayout> GetPendingPayouts(uint accountId);

    void MarkPayoutClaimed(PlayerMarketPayout payout);
}

/// <summary>
/// Temporary in-memory implementation of IPlayerMarketRepository.
/// Replace internals with real DB persistence when ready.
/// </summary>
public sealed class PlayerMarketRepository : IPlayerMarketRepository
{
    private readonly Dictionary<int, PlayerMarketListing> _listingsById = new();
    private readonly Dictionary<uint, PlayerMarketListing> _listingsByItemGuid = new();
    private readonly Dictionary<int, PlayerMarketPayout> _payoutsById = new();

    private int _nextListingId = 1;
    private int _nextPayoutId = 1;

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

        var listing = new PlayerMarketListing
        {
            Id = _nextListingId++,
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
            ExpiresAtUtc = now + MarketServiceLocator.Config.ListingLifetime
        };

        _listingsById[listing.Id] = listing;
        _listingsByItemGuid[listing.ItemGuid] = listing;

        return listing;
    }

    public IEnumerable<PlayerMarketListing> GetListingsForVendorTier(int vendorTier, DateTime nowUtc)
    {
        return _listingsById.Values.Where(l =>
            // Prefer ItemTier-based routing when available.
            // Fall back to MarketVendorTier for older listings where ItemTier is null.
            ((l.ItemTier.HasValue && l.ItemTier.Value == vendorTier)
             || (!l.ItemTier.HasValue && l.MarketVendorTier == vendorTier))
            && !l.IsSold
            && !l.IsCancelled
            && l.ExpiresAtUtc > nowUtc);
    }

    public IEnumerable<PlayerMarketListing> GetListingsForAccount(uint accountId, DateTime nowUtc)
    {
        return _listingsById.Values.Where(l =>
            l.SellerAccountId == accountId &&
            !l.IsCancelled &&
            !l.IsSold &&
            l.ExpiresAtUtc > nowUtc);
    }

    public PlayerMarketListing? GetListingByItemGuid(uint itemGuid)
    {
        _listingsByItemGuid.TryGetValue(itemGuid, out var listing);
        return listing;
    }

    public void MarkListingSold(PlayerMarketListing listing, Player buyer)
    {
        listing.IsSold = true;
        listing.SoldAtUtc = DateTime.UtcNow;
        listing.BuyerAccountId = buyer.Character.AccountId;
        listing.BuyerCharacterId = buyer.Character.Id;
        listing.BuyerName = buyer.Name;
    }

    public void CancelListing(PlayerMarketListing listing)
    {
        listing.IsCancelled = true;
    }

    public void ExpireListings(DateTime nowUtc)
    {
        foreach (var listing in _listingsById.Values)
        {
            if (!listing.IsSold && !listing.IsCancelled && listing.ExpiresAtUtc <= nowUtc)
            {
                listing.IsCancelled = true;
            }
        }
    }

    public PlayerMarketPayout CreatePayout(PlayerMarketListing listing)
    {
        var payout = new PlayerMarketPayout
        {
            Id = _nextPayoutId++,
            ListingId = listing.Id,
            SellerAccountId = listing.SellerAccountId,
            SellerCharacterId = listing.SellerCharacterId,
            Amount = listing.ListedPrice,
            CurrencyType = listing.CurrencyType,
            Status = (int)MarketPayoutStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };

        _payoutsById[payout.Id] = payout;
        return payout;
    }

    public IEnumerable<PlayerMarketPayout> GetPendingPayouts(uint accountId)
    {
        return _payoutsById.Values.Where(p =>
            p.SellerAccountId == accountId &&
            p.Status == (int)MarketPayoutStatus.Pending);
    }

    public void MarkPayoutClaimed(PlayerMarketPayout payout)
    {
        if (_payoutsById.TryGetValue(payout.Id, out var existing))
        {
            existing.Status = (int)MarketPayoutStatus.Claimed;
            existing.ClaimedAtUtc = DateTime.UtcNow;
        }
    }
}
