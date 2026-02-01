using ACE.Database.Models.Shard;
using ACE.Server.WorldObjects;

public static class PlayerMarketVendorFilter
{
    /// <summary>
    /// For now, use the vendor's Tier property directly for routing listings.
    /// </summary>
    public static int GetVendorTierFromVendor(Vendor vendor) => vendor.Tier ?? 0;

    public static bool VendorMatchesListing(Vendor vendor, PlayerMarketListing listing)
    {
        var vendorTier = GetVendorTierFromVendor(vendor);

        if (vendorTier == 0)
        {
            return listing.MarketVendorTier == 0;
        }

        return listing.MarketVendorTier == vendorTier;
    }
}
