namespace ACE.Server.Network.GameAction.Actions;

public static class GameActionRemoveFriend
{
    [GameAction(GameActionType.RemoveFriend)]
    public static void Handle(ClientMessage message, Session session)
    {
        var friendGuid = message.Payload.ReadUInt32();

        session.Player.HandleActionRemoveFriend(friendGuid);
    }
}
