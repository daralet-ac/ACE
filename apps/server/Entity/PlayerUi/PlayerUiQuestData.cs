using System.Collections.Generic;
using System.Linq;
using ACE.Entity.Enum;
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
            Place(contract.LocationNPCStart),
            Place(contract.LocationNPCEnd),
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
    private static PlayerUiPlace Place(ACE.DatLoader.Entity.Position position)
    {
        if (position == null || !Town.IsOnMap(position.ObjCellID))
        {
            return null;
        }

        var coords = PlayerUiData.BuildingMapCoords(position.ObjCellID, position.Frame.Origin.X, position.Frame.Origin.Y);
        return new PlayerUiPlace(coords.X, coords.Y);
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
