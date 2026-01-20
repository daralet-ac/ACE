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
        ContractId.Contract_323_Training_Academy,
        ContractId.Contract_324_Town_Elder__Holtburg,
        ContractId.Contract_325_Town_Elder__Shoushi,
        ContractId.Contract_326_Town_Elder__Yaraq,
        ContractId.Contract_327_Rumors__Holtburg,
        ContractId.Contract_328_Rumors__Shoushi,
        ContractId.Contract_329_Rumors__Yaraq,
        ContractId.Contract_330_Complete_3_Rumors,
        ContractId.Contract_331_Trade_Alliance_Quest__Holtburg,
        ContractId.Contract_332_Trade_Alliance_Quest__Shoushi,
        ContractId.Contract_333_Trade_Alliance_Quest__Yaraq,
        ContractId.Contract_334_Trophy_Pouch,
        ContractId.Contract_335_Horn_of_Hometown,
        ContractId.Contract_336_Capital_City__Cragstone,
        ContractId.Contract_337_Capital_City__Hebian_To,
        ContractId.Contract_338_Capital_City__Zaikhal,
        ContractId.Contract_339_Town_Attunement__Cragstone,
        ContractId.Contract_340_Town_Attunement__Hebian_To,
        ContractId.Contract_341_Town_Attunement__Zaikhal,
        ContractId.Contract_342_Portal_Magic,
        ContractId.Contract_362_Sigil_Slot__Blue
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
