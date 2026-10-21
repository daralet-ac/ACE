using System;

namespace ACE.Server.Market;

public static class MarketListingEvents
{
    public static event Action<int>? ListingCreated;

    public static void RaiseListingCreated(int listingId)
    {
        if (listingId <= 0)
        {
            return;
        }

        try
        {
            ListingCreated?.Invoke(listingId);
        }
        catch
        {
            // event handlers should never break core server functionality
        }
    }

    /// <summary>
    /// A seller's listings or payouts changed: listed, sold, cancelled, returned, paid out or claimed. By seller account id.
    /// </summary>
    public static event Action<uint>? AccountChanged;

    public static void RaiseAccountChanged(uint accountId)
    {
        try
        {
            AccountChanged?.Invoke(accountId);
        }
        catch
        {
            // event handlers should never break core server functionality
        }
    }
}
