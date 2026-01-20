using System.Collections.Generic;
using ACE.Entity.Enum;
using ACE.Server.Network.GameMessages.Messages;

namespace ACE.Server.WorldObjects;

partial class Player
{
    /// <summary>
    /// Contract IDs that cannot be abandoned by players
    /// </summary>
    private static readonly HashSet<ContractId> NonAbandonableContracts = new HashSet<ContractId>
    {
        ContractId.Contract_323_Training_Academy
    };

    public void HandleActionAbandonContract(uint contractId)
    {
        // Check if this contract can be abandoned
        if (NonAbandonableContracts.Contains((ContractId)contractId))
        {
            // Allow abandoning if the contract is Done
            var contractTracker = ContractManager.GetContractTracker(contractId);
            if (contractTracker == null || contractTracker.Stage != Network.Structure.ContractStage.DoneOrPendingRepeat)
            {
                var datContract = ContractManager.GetContractFromDat(contractId);
                var contractName = datContract?.ContractName ?? "This task";

                Session.Network.EnqueueSend(
                    new GameMessageSystemChat(
                        $"The '{contractName}' task cannot be abandoned until completed.",
                        ChatMessageType.Broadcast
                    )
                );
                return;
            }
        }

        ContractManager.Abandon(contractId);
    }
}
