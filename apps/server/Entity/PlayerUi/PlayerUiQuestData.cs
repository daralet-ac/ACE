using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using ACE.Database;
using ACE.Entity.Enum;
using ACE.Entity.Models;
using ACE.Server.Network.Structure;
using ACE.Server.WorldObjects;

namespace ACE.Server.Entity.PlayerUi;

/// <summary>
/// The player's contracts, for the plugin's Quests tab (@playerui quests): what the game's Contracts panel shows, with the
/// rest of each contract's entry in the portal.dat ContractTable (descriptions, the NPCs and where they are), which the
/// client is never sent; it only gets each contract's id, stage and timers.
/// </summary>
public static class PlayerUiQuestData
{
    private static readonly ConcurrentDictionary<(uint Cell, string Npc), Task<Vector2?>> NpcSpots =
        new ConcurrentDictionary<(uint, string), Task<Vector2?>>();

    public static PlayerUiQuests GetQuests(Player player)
    {
        var quests = new List<PlayerUiQuest>();

        foreach (var tracker in player.ContractManager.ContractTrackerTable.Values)
        {
            var contract = tracker.Contract;

            if (contract == null)
            {
                continue;
            }

            quests.Add(Describe(player, tracker));
        }

        return new PlayerUiQuests(quests);
    }

    private static PlayerUiQuest Describe(Player player, ContractTracker tracker)
    {
        var contract = tracker.Contract;
        var stage = tracker.Stage;

        int? solves = null;
        int? maxSolves = null;

        if (!string.IsNullOrWhiteSpace(contract.QuestflagProgress))
        {
            solves = player.QuestManager.GetCurrentSolves(contract.QuestflagProgress);
            var max = player.QuestManager.GetMaxSolves(contract.QuestflagProgress);
            maxSolves = max > 0 ? max : null;
        }

        return new PlayerUiQuest(
            contract.ContractId,
            contract.ContractName,
            Text(contract.Description),
            Text(contract.DescriptionProgress),
            StageWord(stage),
            solves,
            maxSolves,
            tracker.TimeWhenDone > 0 ? tracker.TimeWhenDone : null,
            tracker.TimeWhenRepeats > 0 ? tracker.TimeWhenRepeats : null,
            Text(contract.NameNPCStart),
            Text(contract.NameNPCEnd),
            Place(contract.LocationNPCStart, Text(contract.NameNPCStart)),
            Place(contract.LocationNPCEnd, Text(contract.NameNPCEnd)),
            Place(contract.LocationQuestArea),
            Player.CanAbandonContract(contract.ContractId, stage)
        );
    }

    /// <summary>
    /// The stage in one word: available (taken, not started), started, or done (finished, or waiting to be done again).
    /// A progress counter (ContractStage.ProgressCounter plus the count) is started.
    /// </summary>
    public static string StageWord(ContractStage stage)
    {
        return stage switch
        {
            ContractStage.Available => "available",
            ContractStage.DoneOrPendingRepeat => "done",
            _ => "started",
        };
    }

    private static string Text(string text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();

    /// <summary>A contract's place on the map, as a building or outdoor spot is placed; null for a dungeon or none.</summary>
    private static PlayerUiPlace Place(ACE.DatLoader.Entity.Position position, string npc = null)
    {
        if (position == null || !Town.IsOnMap(position.ObjCellID))
        {
            return null;
        }

        var spot = NpcSpot(position, npc);
        var coords = PlayerUiData.BuildingMapCoords(position.ObjCellID, spot.X, spot.Y);
        return new PlayerUiPlace(coords.X, coords.Y);
    }

    /// <summary>
    /// Where a contract's NPC really stands. The ContractTable's spots are only rough (Renald the Eldest's is 38 meters from
    /// him, outside his building), so the NPC's own spawn in that landblock is used when one has the name. Looking it up can
    /// reach the database for every weenie in the landblock, so it's done off the world thread, once, and the table's spot
    /// stands in until it's known.
    /// </summary>
    private static Vector2 NpcSpot(ACE.DatLoader.Entity.Position position, string npc)
    {
        var table = new Vector2(position.Frame.Origin.X, position.Frame.Origin.Y);

        if (npc == null)
        {
            return table;
        }

        var lookup = NpcSpots.GetOrAdd(
            (position.ObjCellID, npc),
            _ => Task.Run(() => FindNpcSpot(position.ObjCellID, npc, table))
        );

        return lookup.Status == TaskStatus.RanToCompletion && lookup.Result.HasValue ? lookup.Result.Value : table;
    }

    private static Vector2? FindNpcSpot(uint cell, string npc, Vector2 table)
    {
        var spawns = DatabaseManager
            .World.GetCachedInstancesByLandblock((ushort)(cell >> 16))
            .Where(i =>
                string.Equals(
                    DatabaseManager.World.GetCachedWeenie(i.WeenieClassId)?.GetName(),
                    npc,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            .Select(i => new Vector2(i.OriginX, i.OriginY));

        return Nearest(spawns, table);
    }

    /// <summary>The spot closest to another, for a name more than one spawn in a landblock shares; null if there are none.</summary>
    public static Vector2? Nearest(IEnumerable<Vector2> spots, Vector2 to)
    {
        Vector2? nearest = null;

        foreach (var spot in spots)
        {
            if (nearest == null || Vector2.Distance(spot, to) < Vector2.Distance(nearest.Value, to))
            {
                nearest = spot;
            }
        }

        return nearest;
    }

    /// <summary>
    /// Abandons one of the player's contracts, as the Contracts panel's Abandon does (Player.HandleActionAbandonContract,
    /// which keeps the starter contracts until they're done). Only the player's own, by its id.
    /// </summary>
    public static string Abandon(Player player, string word)
    {
        if (!uint.TryParse(word, out var contractId) || player.ContractManager.GetContract(contractId) == null)
        {
            return "You have no such quest.";
        }

        player.HandleActionAbandonContract(contractId);
        return null;
    }
}
