namespace ACE.Server.Network.GameAction.Actions;

public static class GameActionAddSpellFavorite
{
    [GameAction(GameActionType.AddSpellFavorite)]
    public static void Handle(ClientMessage message, Session session)
    {
        var spellId = message.Payload.ReadUInt32();
        var spellBarPositionId = message.Payload.ReadUInt32();
        var spellBarId = message.Payload.ReadUInt32();

        session.Player.HandleActionAddSpellFavorite(spellId, spellBarPositionId, spellBarId);
    }
}
