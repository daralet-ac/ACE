using ACE.Entity;
using ACE.Server.WorldObjects;

namespace ACE.Server.Entity;

/// <summary>
/// The owner of an instance (WorldInstance.Owner) can say where each player goes when they leave it: when they log out inside it,
/// when they use /instance leave, and when it is closed. A duel sends every fighter back to where they were before it, for instance.
/// </summary>
public interface IInstanceReturnPositions
{
    /// <summary>
    /// Where this player goes, or null for wherever the instance's template sends players
    /// </summary>
    Position GetReturnPosition(Player player);
}
