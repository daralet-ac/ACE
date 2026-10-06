using ACE.DatLoader;
using ACE.DatLoader.FileTypes;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Arena;
using ACE.Server.Entity;
using ACE.Server.Entity.Actions;
using ACE.Server.Managers;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.WorldObjects;

/// <summary>
/// What a duel in the arena does to a player (ArenaManager says when). These are called on the player's own thread.
/// </summary>
partial class Player
{
    /// <summary>
    /// They have arrived in the arena. Until the countdown ends they are a non-player killer, so nothing can harm them however it is done.
    /// Their harmful enchantments are taken away and their vitals filled; their beneficial ones stay.
    /// </summary>
    public void PrepareForArenaDuel()
    {
        if (PlayerKillerStatus != PlayerKillerStatus.NPK)
        {
            UpdateProperty(this, PropertyInt.PlayerKillerStatus, (int)PlayerKillerStatus.NPK, true);
        }

        EnchantmentManager.RemoveAllBadEnchantments();
        SetMaxVitals();
    }

    /// <summary>
    /// The countdown is over: they become a player killer lite, with full vitals
    /// </summary>
    public void BeginArenaDuel()
    {
        if (PlayerKillerStatus != PlayerKillerStatus.PKLite)
        {
            UpdateProperty(this, PropertyInt.PlayerKillerStatus, (int)PlayerKillerStatus.PKLite, true);
        }

        SetMaxVitals();
    }

    /// <summary>
    /// Puts back what the duel changed: their player killer status, when they were last in a player killer battle
    /// (so the duel doesn't hold up their recalls and logging out), and no harmful enchantments from it carry on outside
    /// </summary>
    public void RestoreAfterArena(PlayerKillerStatus status, double lastPkAttack)
    {
        if (PlayerKillerStatus != status)
        {
            UpdateProperty(this, PropertyInt.PlayerKillerStatus, (int)status, true);
        }

        LastPkAttackTimestamp = lastPkAttack;

        EnchantmentManager.RemoveAllBadEnchantments();
    }

    /// <summary>
    /// After a delay, puts back what the duel changed and takes them home. Someone who has been defeated is stood up again on the way,
    /// with three quarters of their vitals, as after a death. Someone who has left the arena some other way already only gets their status back.
    /// </summary>
    /// <param name="home">Where they were before the duel. If it is gone (or null), their lifestone, as when an instance closes.</param>
    public void ReturnFromArena(
        Position home,
        PlayerKillerStatus status,
        double lastPkAttack,
        uint arenaInstance,
        double delaySeconds
    )
    {
        var chain = new ActionChain();
        chain.AddDelaySeconds(delaySeconds);
        chain.AddAction(
            this,
            () =>
            {
                RestoreAfterArena(status, lastPkAttack);

                if (InstanceId != arenaInstance)
                {
                    return;
                }

                var destination = InstanceManager.FirstEnterablePosition(home, Sanctuary, Instantiation);

                if (IsInDeathProcess || IsDead)
                {
                    ThreadSafeTeleportOnDeath(destination, Landblock.PersistentInstance);
                    IsBusy = false;
                }
                else
                {
                    WorldManager.ThreadSafeTeleport(this, destination, instanceId: Landblock.PersistentInstance);
                }
            }
        );
        chain.EnqueueChain();
    }

    /// <summary>
    /// Die() for a fighter in the arena: they fall, but none of what dying does happens to them. No vitae, no enchantments lost,
    /// no corpse, no death counted. The arena takes them home once they have finished falling.
    /// </summary>
    private void DieInArena(DamageHistoryInfo topDamager)
    {
        IsInDeathProcess = true;
        suicideInProgress = false;

        UpdateVital(Health, 0);

        if (CombatMode == CombatMode.Magic && MagicState.IsCasting)
        {
            FailCast(false);
        }

        IsBusy = true;

        EnqueueBroadcastMotion(new Motion(MotionStance.NonCombat, MotionCommand.Dead));
        Session.Network.EnqueueSend(new GameMessagePrivateUpdateAttribute2ndLevel(this, Vital.Health, 0));

        var animLength = DatManager
            .PortalDat.ReadFromDat<MotionTable>(MotionTableId)
            .GetAnimationLength(MotionCommand.Dead);

        ArenaManager.OnFighterDefeated(this, topDamager, animLength + 1.0f);
    }
}
