//using System.Collections.Generic;
//using ACE.Server.WorldObjects;

//public class PlayerMarketService
//{
//    private readonly IPlayerMarketRepository _repo;
//    private readonly PlayerMarketConfig _config;

//    public PlayerMarketService(IPlayerMarketRepository repo, PlayerMarketConfig config) { ... }

//    public bool TryListItemFromEmote(Player seller, WorldObject item, out string error);

//    public IEnumerable<PlayerMarketListing> GetListingsForVendor(Vendor vendor);

//    public IEnumerable<PlayerMarketListing> GetListingsForAccount(Player player);

//    public bool TryCancelListing(Player player, uint listingId, out string error);

//    public IEnumerable<PlayerMarketPayout> GetPendingPayouts(Player player);

//    public bool TryClaimPayout(Player player, uint payoutId, out string error);
//}
