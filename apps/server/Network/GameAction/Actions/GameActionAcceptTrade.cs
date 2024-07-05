using System;

namespace ACE.Server.Network.GameAction.Actions;

public static class GameActionAcceptTrade
{
    [GameAction(GameActionType.AcceptTrade)]
    public static void Handle(ClientMessage message, Session session)
    {
        var partnerGuid = message.Payload.ReadUInt32();
        var tradeStamp = message.Payload.ReadDouble();
        var tradeStatus = message.Payload.ReadUInt32();
        var initiatorGuid = message.Payload.ReadUInt32();
        var initatorAccepts = Convert.ToBoolean(message.Payload.ReadUInt32());
        var partnerAccepts = Convert.ToBoolean(message.Payload.ReadUInt32());

        session.Player.HandleActionAcceptTrade();
    }
}
