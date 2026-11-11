using System;
using ACE.Common;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Entity;
using ACE.Server.Entity.Actions;
using ACE.Server.Entity.PlayerUi;
using ACE.Server.Managers;

namespace ACE.Server.WorldObjects;

/// <summary>
/// Rested experience (RestedXpRules has the sums and the words, docs/rested-xp.md the design): built up while logged out,
/// or in a town landblock out of combat mode, and used up by kill XP, which earns rested_xp_bonus more while it lasts.
/// </summary>
partial class Player
{
    /// <summary>The extra kill XP rested experience has given since login, for /rested and the plugin. Not saved.</summary>
    public long RestedXpEarnedThisSession { get; private set; }

    /// <summary>Rested experience left, or 0.</summary>
    public double RestedXp
    {
        get => GetProperty(PropertyFloat.RestedXp) ?? 0;
        set
        {
            if (value < 1)
            {
                RemoveProperty(PropertyFloat.RestedXp);
            }
            else
            {
                SetProperty(PropertyFloat.RestedXp, value);
            }
        }
    }

    /// <summary>When rested experience was last added up (unix time), or null for a character that never has.</summary>
    public double? RestedXpTimestamp
    {
        get => GetProperty(PropertyFloat.RestedXpTimestamp);
        set
        {
            if (!value.HasValue)
            {
                RemoveProperty(PropertyFloat.RestedXpTimestamp);
            }
            else
            {
                SetProperty(PropertyFloat.RestedXpTimestamp, value.Value);
            }
        }
    }

    /// <summary>What the player's current level costs, which rested experience is measured in.</summary>
    private ulong RestedXpLevelCost => GetXPBetweenLevels(Level ?? 1, (Level ?? 1) + 1);

    /// <summary>At the highest level with no XP past it: rested experience would never be used.</summary>
    private bool EarnsNoXp => IsMaxLevel && !PropertyManager.GetBool("allow_xp_at_max_level").Item;

    /// <summary>
    /// In a town's landblock (Town.TownLandblockIds, the check the road speed buff uses too), its buildings included, and
    /// not an instanced copy of one.
    /// </summary>
    private bool InTownForRest =>
        Location != null && InstanceId == 0 && Town.IsTownLandblock((Location.Landblock << 16) | 0xFFFF);

    /// <summary>Which of RestedXpRules' states the player is in now.</summary>
    private string GetRestedXpState(RestedXpRules.Settings settings, long max)
    {
        if (!settings.Enabled)
        {
            return RestedXpRules.Off;
        }

        if (EarnsNoXp)
        {
            return RestedXpRules.MaxLevel;
        }

        if (RestedXpRules.LevelsPerHour(Level ?? 1, settings) <= 0)
        {
            return RestedXpRules.NoRest;
        }

        if (RestedXp >= max)
        {
            return RestedXpRules.Full;
        }

        if (!InTownForRest)
        {
            return RestedXpRules.Away;
        }

        return CombatMode == CombatMode.NonCombat ? RestedXpRules.Resting : RestedXpRules.InCombat;
    }

    /// <summary>Everything /rested says, and the plugin's XP bar shows (in @playerui xp).</summary>
    public PlayerUiRested GetPlayerUiRested()
    {
        var settings = RestedXpRules.Settings.FromProperties();
        var levelCost = RestedXpLevelCost;
        var max = RestedXpRules.Max(levelCost, settings);

        return new PlayerUiRested(
            settings.Enabled,
            GetRestedXpState(settings, max),
            (long)Math.Floor(RestedXp),
            max,
            settings.Bonus,
            RestedXpRules.PerHour(levelCost, Level ?? 1, settings),
            RestedXpEarnedThisSession
        );
    }

    /// <summary>
    /// Every heartbeat, and at logout: the time since the last adds to the pool if the player is resting now. The time is
    /// noted whatever happens, so logging in again only counts the time logged out, even after a crash (the last save's
    /// time, at most player_save_interval out).
    /// </summary>
    private void RestedXpTick(double now)
    {
        var last = RestedXpTimestamp;
        RestedXpTimestamp = now;

        if (last == null)
        {
            return;
        }

        var settings = RestedXpRules.Settings.FromProperties();
        var levelCost = RestedXpLevelCost;
        var max = RestedXpRules.Max(levelCost, settings);

        if (GetRestedXpState(settings, max) != RestedXpRules.Resting)
        {
            return;
        }

        RestedXp = RestedXpRules.Accrue(RestedXp, now - last.Value, RestedXpRules.PerHour(levelCost, Level ?? 1, settings), max);
    }

    /// <summary>
    /// At login: the time logged out is rest wherever the player logged out, and a line says how rested they are. A
    /// character from before rested experience starts counting now.
    /// </summary>
    private void HandleRestedXpOnLogin()
    {
        var now = Time.GetUnixTime();
        var last = RestedXpTimestamp;
        RestedXpTimestamp = now;
        RestedXpEarnedThisSession = 0;

        var settings = RestedXpRules.Settings.FromProperties();

        if (!settings.Enabled || EarnsNoXp)
        {
            return;
        }

        var levelCost = RestedXpLevelCost;
        var max = RestedXpRules.Max(levelCost, settings);
        var before = RestedXp;

        if (last != null)
        {
            RestedXp = RestedXpRules.Accrue(before, now - last.Value, RestedXpRules.PerHour(levelCost, Level ?? 1, settings), max);
        }

        var pool = (long)Math.Floor(RestedXp);

        if (pool < 1)
        {
            return;
        }

        var gained = (long)Math.Floor(RestedXp - before);
        var awayText = gained > 0 ? $"While you were away you rested {gained:N0} experience. " : "";
        var message =
            $"{awayText}You have {pool:N0} rested experience: your kills earn {Math.Round(settings.Bonus * 100):0}% more "
            + "until it's used up. Type /rested to see more.";

        // after the login's own messages, as the shutdown notice is
        var actionChain = new ActionChain();
        actionChain.AddDelaySeconds(5.0f);
        actionChain.AddAction(this, () => SendMessage(message, ChatMessageType.Broadcast));
        actionChain.EnqueueChain();
    }

    /// <summary>
    /// The extra XP rested experience gives kill XP worth this much, taken from the pool. Run on the player's own thread
    /// (from GrantXP's queued action), as the heartbeat that adds to the pool is.
    /// </summary>
    private long ConsumeRestedXp(long amount)
    {
        var settings = RestedXpRules.Settings.FromProperties();

        if (!settings.Enabled || EarnsNoXp)
        {
            return 0;
        }

        var bonus = RestedXpRules.Bonus(RestedXp, amount, settings.Bonus);

        if (bonus <= 0)
        {
            return 0;
        }

        RestedXp -= bonus;
        RestedXpEarnedThisSession += bonus;

        if (RestedXp < 1)
        {
            SendMessage("You've used up your rested experience.", ChatMessageType.Broadcast);
        }

        return bonus;
    }
}
