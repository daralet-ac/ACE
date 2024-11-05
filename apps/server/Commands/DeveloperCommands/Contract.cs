using System;
using ACE.Database.Models.Shard;
using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Server.Commands.Handlers;
using ACE.Server.Network;
using ACE.Server.WorldObjects;

namespace ACE.Server.Commands.DeveloperCommands;

public class Contract
{
    [CommandHandler(
        "contract",
        AccessLevel.Developer,
        CommandHandlerFlag.RequiresWorld,
        1,
        "Query, stamp, and erase contracts on the targeted player",
        "[list | bestow | erase]\n"
            + "contract list - List the contracts for the targeted player\n"
            + "contract bestow - Stamps the specific contract on the targeted player. If this fails, it's probably because the contract is invalid.\n"
            + "contract erase - Erase the specific contract from the targeted player. If no quest flag is given, it erases the entire contract table for the targeted player.\n"
    )]
    public static void HandleContract(Session session, params string[] parameters)
    {
        if (parameters.Length == 0)
        {
            // todo: display help screen
            return;
        }

        var objectId = new ObjectGuid();

        if (session.Player.HealthQueryTarget.HasValue)
        {
            objectId = new ObjectGuid((uint)session.Player.HealthQueryTarget);
        }
        else if (session.Player.ManaQueryTarget.HasValue)
        {
            objectId = new ObjectGuid((uint)session.Player.ManaQueryTarget);
        }
        else if (session.Player.CurrentAppraisalTarget.HasValue)
        {
            objectId = new ObjectGuid((uint)session.Player.CurrentAppraisalTarget);
        }

        var wo = session.Player.CurrentLandblock?.GetObject(objectId);

        if (wo != null && wo is Player player)
        {
            if (parameters[0].Equals("list"))
            {
                var contractsHdr = $"Contract Registry for {player.Name} (0x{player.Guid}):\n";
                contractsHdr += "================================================\n";
                contractsHdr +=
                    $"Contracts.Count: {player.Character.GetContractsCount(player.CharacterDatabaseLock)}\n";
                contractsHdr += "================================================\n";
                var contracts = "";
                foreach (var contract in player.ContractManager.ContractTrackerTable)
                {
                    var contractTracker = contract.Value;
                    contracts +=
                        $"Contract Id: {contractTracker.Contract.ContractId} | Contract Name: {contractTracker.Contract.ContractName}\nStage: {contractTracker.Stage.ToString()}\n";

                    if (contractTracker.Stage == Network.Structure.ContractStage.InProgress)
                    {
                        var timeWhenDone = new TimeSpan(0, 0, (int)contractTracker.TimeWhenDone);

                        if (timeWhenDone == TimeSpan.MinValue || timeWhenDone.TotalSeconds == 0)
                        {
                            contracts += $"TimeWhenDone: Expired ({contractTracker.TimeWhenDone})\n";
                        }
                        else if (timeWhenDone == TimeSpan.MaxValue)
                        {
                            contracts += $"TimeWhenDone: Unlimited ({contractTracker.TimeWhenDone})\n";
                        }
                        else
                        {
                            contracts +=
                                $"TimeWhenDone: In {timeWhenDone:%d} days, {timeWhenDone:%h} hours, {timeWhenDone:%m} minutes and, {timeWhenDone:%s} seconds. ({(DateTime.UtcNow + timeWhenDone).ToLocalTime()})\n";
                        }
                    }

                    if (contractTracker.Stage == Network.Structure.ContractStage.DoneOrPendingRepeat)
                    {
                        var timeWhenRepeats = new TimeSpan(0, 0, (int)contractTracker.TimeWhenRepeats);

                        if (timeWhenRepeats == TimeSpan.MinValue || timeWhenRepeats.TotalSeconds == 0)
                        {
                            contracts += $"TimeWhenRepeats: Available ({contractTracker.TimeWhenDone})\n";
                        }
                        else if (timeWhenRepeats == TimeSpan.MaxValue)
                        {
                            contracts += $"TimeWhenRepeats: Unlimited ({contractTracker.TimeWhenDone})\n";
                        }
                        else
                        {
                            contracts +=
                                $"TimeWhenRepeats: In {timeWhenRepeats:%d} days, {timeWhenRepeats:%h} hours, {timeWhenRepeats:%m} minutes and, {timeWhenRepeats:%s} seconds. ({(DateTime.UtcNow + timeWhenRepeats).ToLocalTime()})\n";
                        }
                    }

                    contracts += "--====--\n";
                }

                session.Player.SendMessage($"{contractsHdr}{(contracts != "" ? contracts : "No contracts found.")}");
                return;
            }

            if (parameters[0].Equals("bestow"))
            {
                if (parameters.Length < 2)
                {
                    // delete all contracts?
                    // seems unsafe, maybe a confirmation?
                    return;
                }

                if (!uint.TryParse(parameters[1], out var contractId))
                {
                    return;
                }

                var datContract = player.ContractManager.GetContractFromDat(contractId);

                if (datContract == null)
                {
                    session.Player.SendMessage($"Unable to find contract for id {contractId} in dat file.");
                    return;
                }

                if (player.ContractManager.HasContract(contractId))
                {
                    session.Player.SendMessage(
                        $"{player.Name} already has the contract for \"{datContract.ContractName}\" ({contractId})"
                    );
                    return;
                }

                var hasContract = player.ContractManager.HasContract(contractId);
                if (!hasContract)
                {
                    player.ContractManager.Add(contractId);
                    session.Player.SendMessage(
                        $"Contract for \"{datContract.ContractName}\" ({contractId}) bestowed on {player.Name}"
                    );
                    return;
                }
                else
                {
                    session.Player.SendMessage($"Couldn't bestow {contractId} on {player.Name}");
                    return;
                }
            }

            if (parameters[0].Equals("erase"))
            {
                if (parameters.Length < 2)
                {
                    // delete all contracts?
                    // seems unsafe, maybe a confirmation?
                    session.Player.SendMessage(
                        $"You must specify a contract to delete, if you want to delete all contracts use the following command: /contract delete *"
                    );
                    return;
                }

                if (parameters[1] == "*")
                {
                    player.ContractManager.EraseAll();
                    session.Player.SendMessage($"All contracts deleted for {player.Name}.");
                    return;
                }

                if (!uint.TryParse(parameters[1], out var contractId))
                {
                    return;
                }

                var datContract = player.ContractManager.GetContractFromDat(contractId);

                if (datContract == null)
                {
                    session.Player.SendMessage($"Unable to find contract for id {contractId} in dat file.");
                    return;
                }

                if (!player.ContractManager.HasContract(contractId))
                {
                    session.Player.SendMessage(
                        $"{datContract.ContractName} ({contractId}) not found in {player.Name}'s registry."
                    );
                    return;
                }
                player.ContractManager.Erase(contractId);
                session.Player.SendMessage($"{datContract.ContractName} ({contractId}) deleted for {player.Name}.");
                return;
            }
        }
        else
        {
            if (wo == null)
            {
                session.Player.SendMessage($"Selected object (0x{objectId}) not found.");
            }
            else
            {
                session.Player.SendMessage($"Selected object {wo.Name} (0x{objectId}) is not a player.");
            }
        }
    }
}
