using System;

namespace ACE.Server.Market;

/// <summary>
/// Simple static access point for market-related services.
/// </summary>
public static class MarketServiceLocator
{
    private static IPlayerMarketRepository? _playerMarketRepository;
    private static ACE.Server.Market.PlayerMarketConfig? _config;  // fully qualified

    /// <summary>
    /// Market configuration (listing lifetime, limits, etc.).
    /// </summary>
    public static ACE.Server.Market.PlayerMarketConfig Config =>   // fully qualified
        _config ?? throw new InvalidOperationException(
            "MarketServiceLocator.Config has not been initialized."
        );

    /// <summary>
    /// Call this once during server startup to wire up the market subsystem.
    /// </summary>
    public static void Initialize(
        IPlayerMarketRepository playerMarketRepository,
        ACE.Server.Market.PlayerMarketConfig? config = null)       // fully qualified
    {
        _playerMarketRepository =
            playerMarketRepository ?? throw new ArgumentNullException(nameof(playerMarketRepository));

        _config = config ?? new ACE.Server.Market.PlayerMarketConfig();
    }

    /// <summary>
    /// Repository used for all persistence of listings and payouts.
    /// Must be initialized during server startup.
    /// </summary>
    public static IPlayerMarketRepository PlayerMarketRepository =>
        _playerMarketRepository ?? throw new InvalidOperationException(
            "MarketServiceLocator.PlayerMarketRepository has not been initialized."
        );
}
