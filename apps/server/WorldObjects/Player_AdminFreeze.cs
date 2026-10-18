using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.WorldObjects;

partial class Player
{
    /// <summary>
    /// How long @freeze holds a player when no duration is given.
    /// </summary>
    public const double DefaultAdminFreezeSeconds = 600;

    /// <summary>
    /// Seconds left on an @freeze, or 0 when the player isn't frozen by staff. Saved, so a frozen player who logs out
    /// is still frozen for the time that was left when they come back.
    /// </summary>
    public double AdminFreezeDuration
    {
        get => GetProperty(PropertyFloat.AdminFreezeDuration) ?? 0;
        set
        {
            if (value <= 0)
            {
                RemoveProperty(PropertyFloat.AdminFreezeDuration);
            }
            else
            {
                SetProperty(PropertyFloat.AdminFreezeDuration, value);
            }
        }
    }

    public bool IsAdminFrozen => AdminFreezeDuration > 0;

    /// <summary>
    /// Freezes the player in place, the same way a delayed PK logoff does, until the time runs out or AdminUnfreeze is called.
    /// </summary>
    public void AdminFreeze(double seconds)
    {
        AdminFreezeDuration = seconds;

        IsFrozen = true;
        EnqueueBroadcastPhysicsState();

        SaveBiotaToDatabase();

        Session?.Network.EnqueueSend(
            new GameMessageSystemChat(
                "You have been frozen in place by an administrator.",
                ChatMessageType.WorldBroadcast
            )
        );
    }

    public void AdminUnfreeze()
    {
        AdminFreezeDuration = 0;

        // a delayed PK logoff freezes the player too, so leave them frozen until it finishes
        if (!PKLogout)
        {
            IsFrozen = false;
        }
        EnqueueBroadcastPhysicsState();

        SaveBiotaToDatabase();

        Session?.Network.EnqueueSend(
            new GameMessageSystemChat("You are no longer frozen.", ChatMessageType.WorldBroadcast)
        );
    }

    public void AdminFreezeTick()
    {
        if (!IsAdminFrozen)
        {
            return;
        }

        var remaining = AdminFreezeDuration - CachedHeartbeatInterval;

        if (remaining <= 0)
        {
            AdminUnfreeze();
        }
        else
        {
            AdminFreezeDuration = remaining;
        }
    }
}
