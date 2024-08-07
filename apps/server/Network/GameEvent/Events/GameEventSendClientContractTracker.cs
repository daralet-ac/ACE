using System;
using ACE.Database.Models.Shard;
using ACE.Server.Network.Structure;

namespace ACE.Server.Network.GameEvent.Events;

public class GameEventSendClientContractTracker : GameEventMessage
{
    public GameEventSendClientContractTracker(Session session, CharacterPropertiesContractRegistry contract)
        : base(GameEventType.SendClientContractTracker, GameMessageGroup.UIQueue, session, 52)
    {
        var contractTracker = new ContractTracker(session.Player, contract);

        Writer.Write(contractTracker);
        Writer.Write(Convert.ToUInt32(contractTracker.DeleteContract));
        Writer.Write(Convert.ToUInt32(contractTracker.SetAsDisplayContract));
    }
}
