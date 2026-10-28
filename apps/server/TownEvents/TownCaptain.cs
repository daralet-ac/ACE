using System;
using System.Collections.Generic;
using System.Linq;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.Entity;
using ACE.Server.Managers;
using ACE.Server.Network.GameMessages.Messages;
using ACE.Server.WorldObjects;
using Serilog;

namespace ACE.Server.TownEvents;

/// <summary>
/// A town's Captain of the Guard: an NPC marked with the Template "Captain of the Guard", handled here the way the Market
/// Broker is. Using or talking to the Captain reports the town's guard supply and pays any reward the player has waiting
/// from a defense; giving the Captain pyreals or trade notes donates them to the supply. The town is the one nearest the
/// Captain. Hooks run on the player's thread, so anything touching the supply or claims is queued to TownEventManager.
/// </summary>
public static class TownCaptain
{
    private static readonly ILogger _log = Log.ForContext(typeof(TownCaptain));

    public const string TemplateName = "Captain of the Guard";

    // how long a defender has to claim their reward from the Captain
    public const double ClaimSeconds = 30 * 60;

    // how long the donor buff lasts, and the least donation that earns it: a share of level 1's cost (all of it), so a
    // small gift doesn't buy an hour of bonus XP. Every donation counts toward the supply whatever its size.
    public const double DonorBuffSeconds = 60 * 60;
    public const double DonorBuffMinimumShareOfLevelOne = 1.0;

    // the Captain counts as the town's this far from its centre
    private const float CaptainSearchRadius = 300f;

    public static bool IsCaptain(WorldObject npc) =>
        npc != null && string.Equals(npc.GetProperty(PropertyString.Template), TemplateName, StringComparison.OrdinalIgnoreCase);

    public static string GetTown(WorldObject captain) => captain?.Location == null ? null : Town.GetNearestTown(captain);

    /// <summary>
    /// Whether the town has a Captain standing near its centre right now, to hand rewards out. Call on the world thread.
    /// </summary>
    public static bool HasCaptain(string town) => Find(town) != null;

    /// <summary>
    /// The town's Captain standing near its centre right now, or null. Call on the world thread.
    /// </summary>
    public static Creature Find(string town) =>
        TownGeography.FindNear(town, CaptainSearchRadius, wo => wo is Creature { IsDead: false } && IsCaptain(wo)).OfType<Creature>().FirstOrDefault();

    /// <summary>
    /// The least donation that earns the donor buff in this town
    /// </summary>
    public static double GetDonorBuffMinimum(string town) => TownSupply.GetThreshold(town, 1) * DonorBuffMinimumShareOfLevelOne;

    /// <summary>
    /// Whether the player is in the town's own level band, the only players its donor buff is for: a higher-level player
    /// can't buy cheap XP from a starter town, and a lower one can't from a town above them
    /// </summary>
    public static bool IsOwnTier(Player player, string town)
    {
        var level = player.Level ?? 1;
        return level >= Town.GetTownMinLevel(town) && level <= Town.GetTownMaxLevel(town);
    }

    // ---------------------------------------------------------------- hooks (player's thread)

    public static void OnUse(Player player, WorldObject captain)
    {
        var town = GetTown(captain);
        if (town == null)
        {
            return;
        }

        TownEventManager.Enqueue(() =>
        {
            if (!TownEventManager.TryClaim(player, town, captain))
            {
                Report(player, captain, town);
            }
        });
    }

    public static void OnTalkDirect(Player player, WorldObject captain, string message)
    {
        var town = GetTown(captain);
        if (town == null)
        {
            return;
        }

        var wantsClaim = message != null && message.Contains("claim", StringComparison.OrdinalIgnoreCase);

        TownEventManager.Enqueue(() =>
        {
            if (wantsClaim)
            {
                if (!TownEventManager.TryClaim(player, town, captain))
                {
                    Tell(player, captain, "I have nothing waiting for you. Defend the town and come back to me afterwards.");
                }
                return;
            }

            Report(player, captain, town);
        });
    }

    /// <summary>
    /// The pyreals an item is worth as a donation: coins and trade notes at face value, for the amount given out of the
    /// stack. Anything else is worth nothing to the Captain.
    /// </summary>
    public static long GetDonationValue(WorldObject item, int amount)
    {
        if (item == null || item.WeenieType != WeenieType.Coin && !item.IsTradeNote)
        {
            return 0;
        }

        var stack = Math.Max(1, item.StackSize ?? 1);
        var given = Math.Clamp(amount, 1, stack);

        return (long)(item.Value ?? 0) * given / stack;
    }

    /// <summary>
    /// A donation has been taken from the player's inventory: add it to the town's supply, and give the buff if it
    /// earns one. Queued to the world thread.
    /// </summary>
    public static void OnDonation(Player player, WorldObject captain, long pyreals)
    {
        var town = GetTown(captain);
        if (town == null || pyreals <= 0)
        {
            return;
        }

        TownEventManager.Enqueue(() =>
        {
            var before = TownSupply.GetLevel(town);
            var after = TownSupply.Add(town, pyreals);

            _log.Information("[TOWN EVENT] {Town}: {Player} donated {Pyreals:N0} pyreals (supply level {Before} -> {After})", town, player.Name, pyreals, before, after);

            var ownTier = IsOwnTier(player, town);
            var earnsBuff = ownTier && pyreals >= GetDonorBuffMinimum(town);

            var thanks = after > before
                ? $"Thank you! That brings {town}'s guards up to supply level {after}."
                : $"Thank you. Every pyreal keeps {town}'s guards fed and armed.";

            if (earnsBuff)
            {
                player.GrantTownDonorXpBuff(DonorBuffSeconds);
                thanks += $" Go with the town's gratitude: you'll learn {Player.TownDonorXpBonus * 100:N0}% faster for the next hour.";
            }
            else if (ownTier)
            {
                thanks += $" A gift of {GetDonorBuffMinimum(town):N0} or more earns the town's gratitude: faster learning for an hour.";
            }
            else
            {
                thanks += $" The town's gratitude, faster learning for an hour, is for those of its own standing: levels {Math.Max(1, Town.GetTownMinLevel(town))} to {Town.GetTownMaxLevel(town)}.";
            }

            Tell(player, captain, thanks);
        });
    }

    // ---------------------------------------------------------------- talk (world thread)

    /// <summary>
    /// The town's supply level, how far it is to the next, what it loses an hour and how long the level lasts
    /// </summary>
    private static void Report(Player player, WorldObject captain, string town)
    {
        var level = TownSupply.GetLevel(town);
        var supply = TownSupply.GetSupply(town);
        var lines = new List<string>();

        lines.Add(
            level == 0
                ? $"{town}'s guards have no supply level to speak of. Pyreals given to me buy them food, arms and armor."
                : $"{town}'s guards are at supply level {level} of {TownSupply.MaxLevel}."
        );

        if (level < TownSupply.MaxLevel)
        {
            var toNext = Math.Ceiling(TownSupply.GetThreshold(town, level + 1) - supply);
            lines.Add($"{toNext:N0} more pyreals would bring them to level {level + 1}.");
        }

        var loss = TownSupply.GetHourlyLoss(town);
        if (level > 0 && loss > 0)
        {
            lines.Add($"We go through {loss:N0} pyreals of supply an hour, so level {level} lasts {Describe(TownSupply.GetSecondsUntilLevelDrops(town))} more.");
        }

        if (player.HasTownDonorXpBuff)
        {
            lines.Add($"The town's gratitude stays with you for {Describe(player.TownDonorXpBuffDuration)} more.");
        }

        foreach (var line in lines)
        {
            Tell(player, captain, line);
        }
    }

    private static string Describe(double seconds)
    {
        var span = TimeSpan.FromSeconds(Math.Max(60, seconds));
        if (span.TotalHours >= 1)
        {
            var hours = (int)span.TotalHours;
            return span.Minutes > 0 ? $"{hours} {(hours == 1 ? "hour" : "hours")} {span.Minutes} minutes" : $"{hours} {(hours == 1 ? "hour" : "hours")}";
        }

        return $"{span.Minutes} {(span.Minutes == 1 ? "minute" : "minutes")}";
    }

    public static void Tell(Player player, WorldObject captain, string text)
    {
        if (player?.Session == null)
        {
            return;
        }

        // some clients show an NPC's tell packet as "You think", so it goes as a tell-coloured line, as the Market Broker's do
        player.Session.Network.EnqueueSend(new GameMessageSystemChat($"{captain?.Name ?? TemplateName} tells you, \"{text}\"", ChatMessageType.Tell));
    }
}
