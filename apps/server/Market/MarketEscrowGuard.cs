using System;
using System.Linq;
using ACE.Entity.Enum.Properties;
using ACE.Server.WorldObjects;

namespace ACE.Server.Market;

internal static class MarketEscrowGuard
{
    public static bool IsActiveListingBiotaId(uint biotaId)
    {
        if (biotaId == 0)
        {
            return false;
        }

        try
        {
            using var context = new ACE.Database.Models.Shard.ShardDbContext();
            var nowUtc = DateTime.UtcNow;
            return context.PlayerMarketListings.Any(l =>
                l.ItemBiotaId == biotaId
                && !l.IsSold
                && !l.IsCancelled
                && l.ReturnedAtUtc == null
                && l.ExpiresAtUtc > nowUtc);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Whether a listing's item is still held by the market. Listing it clears its container and wielder
    /// (MarketBroker.FinalizeConfirmedListing), so one with either is in someone's hands.
    /// </summary>
    public static bool IsInEscrow(ACE.Database.Models.Shard.Biota biota)
    {
        return biota.BiotaPropertiesIID?.Any(p =>
                p.Type == (ushort)PropertyInstanceId.Container || p.Type == (ushort)PropertyInstanceId.Wielder
            ) != true;
    }

    public static bool ShouldPreserveBiotaOnDestroy(WorldObject obj)
    {
        if (obj?.Biota == null)
        {
            return false;
        }

        // Only relevant for DB-backed objects.
        if (!obj.BiotaOriginatedFromOrHasBeenSavedToDatabase())
        {
            return false;
        }

        try
        {
            var biotaId = obj.Biota.Id;
            return IsActiveListingBiotaId((uint)biotaId);
        }
        catch
        {
            // If the DB check fails for any reason, fall back to existing behavior.
            return false;
        }
    }
}
