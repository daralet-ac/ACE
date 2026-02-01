//using System;

//namespace ACE.Server.Market;

///// <summary>
///// Persistent record of a single player-listed item in the market.
///// </summary>
//public sealed class PlayerMarketListing
//{
//    public int Id { get; set; }

//    /// <summary>
//    /// Account that owns this listing.
//    /// </summary>
//    public uint SellerAccountId { get; set; }

//    /// <summary>
//    /// Character that created the listing (optional but useful for auditing/UI).
//    /// </summary>
//    public uint? SellerCharacterId { get; set; }

//    /// <summary>
//    /// Character name at time of listing (for inscription / display).
//    /// </summary>
//    public string? SellerName { get; set; }

//    /// <summary>
//    /// The guid of the world object being sold (Biota.Id / WorldObject.Guid.Full).
//    /// </summary>
//    public uint ItemGuid { get; set; }

//    /// <summary>
//    /// FK into Biota or other item snapshot mechanism.
//    /// If you are using the item guid as the biota id, this may be redundant.
//    /// </summary>
//    public uint ItemBiotaId { get; set; }

//    /// <summary>
//    /// Item weenie class id (for quick filtering / diagnostics).
//    /// </summary>
//    public uint ItemWeenieClassId { get; set; }

//    /// <summary>
//    /// The item's original value before listing.
//    /// </summary>
//    public int OriginalValue { get; set; }

//    /// <summary>
//    /// The price the seller set for this listing.
//    /// </summary>
//    public int ListedPrice { get; set; }

//    /// <summary>
//    /// Currency type for pricing and payout.
//    /// </summary>
//    public MarketCurrencyType CurrencyType { get; set; }

//    /// <summary>
//    /// Tier/vendor bucket this item belongs to (1-9 for tier vendors, 0 for non-tier).
//    /// </summary>
//    public int MarketVendorTier { get; set; }

//    /// <summary>
//    /// Cached item tier from PropertyInt.Tier (if any).
//    /// </summary>
//    public int? ItemTier { get; set; }

//    /// <summary>
//    /// Cached wield requirement from PropertyInt.WieldReq (if any).
//    /// </summary>
//    public int? WieldReq { get; set; }

//    /// <summary>
//    /// Optional inscription text stored for this listing (e.g. "Listed by X").
//    /// </summary>
//    public string? Inscription { get; set; }

//    /// <summary>
//    /// Original inscription text on the item (if any) so it can be restored.
//    /// </summary>
//    public string? OriginalInscription { get; set; }

//    /// <summary>
//    /// Creation timestamp (UTC).
//    /// </summary>
//    public DateTime CreatedAtUtc { get; set; }

//    /// <summary>
//    /// Expiration timestamp (UTC).
//    /// </summary>
//    public DateTime ExpiresAtUtc { get; set; }

//    /// <summary>
//    /// Set when the listing is cancelled by the seller or system.
//    /// </summary>
//    public bool IsCancelled { get; set; }

//    /// <summary>
//    /// Set when the listing has been purchased.
//    /// </summary>
//    public bool IsSold { get; set; }

//    /// <summary>
//    /// Convenience: true if the listing is neither sold nor cancelled and not yet expired.
//    /// </summary>
//    public bool IsActive(DateTime nowUtc) =>
//        !IsSold && !IsCancelled && nowUtc < ExpiresAtUtc;

//    /// <summary>
//    /// Buyer info (if sold).
//    /// </summary>
//    public uint? BuyerAccountId { get; set; }
//    public uint? BuyerCharacterId { get; set; }
//    public string? BuyerName { get; set; }

//    /// <summary>
//    /// When the listing was sold (if applicable).
//    /// </summary>
//    public DateTime? SoldAtUtc { get; set; }

//    /// <summary>
//    /// For defensive concurrency in the repository.
//    /// </summary>
//    public byte[]? RowVersion { get; set; }
//}
