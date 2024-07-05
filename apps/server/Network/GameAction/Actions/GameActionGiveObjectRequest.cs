namespace ACE.Server.Network.GameAction.Actions;

public static class GameActionGiveObjectRequest
{
    [GameAction(GameActionType.GiveObjectRequest)]
    public static void Handle(ClientMessage message, Session session)
    {
        var targetGuid = message.Payload.ReadUInt32();
        var objectGuid = message.Payload.ReadUInt32();
        var amount = message.Payload.ReadInt32();

        session.Player.HandleActionGiveObjectRequest(targetGuid, objectGuid, amount);
    }
}
