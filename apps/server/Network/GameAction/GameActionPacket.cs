using ACE.Server.Network.Enum;
using ACE.Server.Network.GameMessages;
using ACE.Server.Network.Managers;

namespace ACE.Server.Network.GameAction;

public static class GameActionPacket
{
    [GameMessage(GameMessageOpcode.GameAction, SessionState.WorldConnected)]
    public static void HandleGameAction(ClientMessage message, Session session)
    {
        // TODO: verify sequence
        var sequence = message.Payload.ReadUInt32();
        var opcode = message.Payload.ReadUInt32();

        InboundMessageManager.HandleGameAction((GameActionType)opcode, message, session);
    }
}
