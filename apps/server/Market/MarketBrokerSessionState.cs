using System;

namespace ACE.Server.Market;

public sealed class MarketBrokerSessionState
{
    public uint? PendingItemGuid { get; set; }
    public uint? PendingItemWeenieClassId { get; set; }
    public int? PendingPrice { get; set; }
    public DateTime? PendingExpiresAtUtc { get; set; }
}
