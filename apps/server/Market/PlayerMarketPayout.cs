//using System;

//namespace ACE.Server.Market;

///// <summary>
///// Persistent record of money owed to a seller from a completed listing,
///// claimed later via the market lister NPC.
///// </summary>
//public sealed class PlayerMarketPayout
//{
//    public int Id { get; set; }

//    /// <summary>
//    /// The listing that generated this payout.
//    /// </summary>
//    public int ListingId { get; set; }

//    public uint SellerAccountId { get; set; }
//    public uint? SellerCharacterId { get; set; }

//    /// <summary>
//    /// Amount owed in the selected currency (pyreals or trade-note value).
//    /// </summary>
//    public int Amount { get; set; }

//    public MarketCurrencyType CurrencyType { get; set; }

//    public MarketPayoutStatus Status { get; set; } = MarketPayoutStatus.Pending;

//    public DateTime CreatedAtUtc { get; set; }

//    public DateTime? ClaimedAtUtc { get; set; }

//    /// <summary>
//    /// For defensive concurrency in the repository.
//    /// </summary>
//    public byte[]? RowVersion { get; set; }
//}

//public enum MarketPayoutStatus
//{
//    Pending = 0,
//    Claimed = 1,
//    Expired = 2,
//    Failed = 3
//}
